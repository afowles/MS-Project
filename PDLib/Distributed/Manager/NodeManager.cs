using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.IO;

using Distributed.Network;
using Distributed.Logging;

[assembly: InternalsVisibleTo("StartManager")]

namespace Distributed.Manager
{

    /// <summary>
    /// Master Slave approach, will manage connections
    /// to nodes and pass of incoming clients. Look into
    /// possible UDP approach for no "master"
    /// </summary>
    internal class NodeManager
    {
        private TcpListener server;
        // Keeps the while loop running
        private bool DoneAccepting;
        // Indicates whether we still want to accept
        // connections. Different than DoneAccepting.
        private bool AcceptingConnections;

        private static string IpAddr = "?";
        private byte[] bytes = new byte[NetworkSendReceive.BUFFER_SIZE];
        public List<Proxy> Connections;
        private static int NextId;

        // Thread-safe, only accessed within lock
        private object NodeLock = new object();
        private List<NodeRef> ConnectedNodes;

        // This is thread safe
        public JobManager Jobs { get; private set;}

        // Handles Logging
        public Logger log { get; private set; }

        /// <summary>
        /// Creates a server listening
        /// on the given IP.
        /// </summary>
        /// <param name="ip">IP to listen on</param>
        public NodeManager(string ip)
        {
            Connections = new List<Proxy>();
            ConnectedNodes = new List<NodeRef>();
            NextId = 1;
            Jobs = new JobManager(this);
            log = Logger.ManagerLogInstance;

            try
            {
                IPAddress localAddr = IPAddress.Parse(ip);
                server = new TcpListener(localAddr, NetworkSendReceive.SERVER_PORT);
            }
            catch (SocketException e)
            {
                log.Log(String.Format("SocketException: { 0}", e));
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }  
        }

        /// <summary>
        /// Call to start listening
        /// on the server.
        /// </summary>
        public async Task StartListening()
        {
            log.Log("Starting Server");
            DoneAccepting = false;
            AcceptingConnections = true;
            server.Start();
            await AcceptConnections();
        }

        /// <summary>
        /// Tells the server to stop listening
        /// </summary>
        public void StopListening()
        {
            DoneAccepting = true;
            server.Stop();
        }

        /// <summary>
        /// Accepts connection from the server
        /// and passes them to a proxy.
        /// </summary>
        /// <returns></returns>
        private async Task AcceptConnections()
        {
            while (!DoneAccepting)
            {
                // check if we have a pending connection
                if (!server.Pending())
                {
                    // we don't wait a bit
                    Thread.Sleep(NetworkSendReceive.SERVER_SLEEP);
                    // skip to top
                    continue;
                }
                // it is possible we are done accepting connections
                // but not quite done with the while loop. During
                // cleanup we do not want to accept connections
                // but still need the server to be "running"
                if (AcceptingConnections)
                {
                    // await the client connection, this blocks
                    var client = await server.AcceptTcpClientAsync();
                    log.Log("Connected!");
                    // create a default proxy
                    var proxy = new Network.Proxy(new DefaultReceiver(this), new DefaultSender(this), client);
                    // add it to the list of connections
                    Connections.Add(proxy);
                }  
            }
        }
       

        public static void Main()
        {     
            // Grab the IP address
            Task getIp = GetLocalIPAddress();
            getIp.Wait();
            // create a NodeManager
            NodeManager m = new NodeManager(IpAddr);
            // Setup handle of ctrl c
            Console.CancelKeyPress += m.OnUserExit;
            // This should never finish until StillActive is set
            // to false
            Task t2 = m.StartListening();
            t2.Wait();
        }

        /// <summary>
        /// Gets the local IP address
        /// </summary>
        public static async Task GetLocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = await Dns.GetHostEntryAsync(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            IpAddr = localIP;
            Console.WriteLine(host);
            Console.WriteLine(localIP);
        }

        /// <summary>
        /// When the user exits the command line
        /// with Ctrl-C tell all connections to shutdown.
        /// Cleanup all threads and stop listening on incoming connections.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnUserExit(object sender, ConsoleCancelEventArgs e)
        {
            // set this to true so that we do not exit immediately
            e.Cancel = true;
            // don't allow any more connections while we clean up.
            AcceptingConnections = false;
            // Queue up the kill message
            // then join on the proxy (which joins on both
            // of its threads) after all have finished.
            // stop listening and quit. 
            foreach (Proxy p in Connections)
            {
                p.QueueDataEvent(new NodeManagerComm(
                    DataReceivedEventArgs.ConstructMessage("kill")));
                p.Join();
            }
            // stop listening for incoming connections
            // this exits Main() do this at the end.
            StopListening();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public void AddConnectedNode(Proxy p)
        {
            lock(NodeLock)
            {
                ConnectedNodes.Add(new NodeRef(p, NextId));
                NextId++;
            }
        }
        /// <summary>
        /// Send out the message that a job is available
        /// </summary>
        /// <param name="data"></param>
        public void SendJobOut(NodeManagerComm data)
        {
            lock(NodeLock)
            { 
                foreach (NodeRef node in ConnectedNodes)
                {
                    Console.WriteLine("Enqueing for Node");
                    string s = Path.GetFileName(data.args[1]) + "|";
                    for (int i = 2; i < data.args.Length - 1; i++)
                    {
                        s += data.args[i] + "|";
                    }
                    node.QueueDataEvent(new NodeManagerComm("file|" + s ));
                }
            }
            
        }

    }

    internal class NodeRef
    {
        private Proxy proxy;
        private int id_;

        public NodeRef(Proxy p, int id)
        {
            proxy = p;
            id_ = id;
        }

        public void QueueDataEvent(DataReceivedEventArgs d)
        {
            proxy.QueueDataEvent(d);
        }
    }

}


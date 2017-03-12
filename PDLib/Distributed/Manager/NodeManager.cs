using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Distributed.Network;
using System.Threading;
using System.IO;

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
        private bool StillActive;
        private static string IpAddr = "?";
        private byte[] bytes = new byte[NetworkSendReceive.BUFFER_SIZE];
        public List<Proxy> Connections;
        private static int NextId;

        // Thread-safe, only accessed within lock
        private object NodeLock = new object();
        private List<NodeRef> ConnectedNodes;

        // This is thread safe
        public JobManager Jobs { get; private set;}

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
            try
            {
                IPAddress localAddr = IPAddress.Parse(ip);
                server = new TcpListener(localAddr, NetworkSendReceive.SERVER_PORT);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
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
            Console.WriteLine("Starting");
            StillActive = true;
            server.Start();
            await AcceptConnections();
        }

        /// <summary>
        /// Tells the server to stop listening
        /// </summary>
        public void StopListening()
        {
            StillActive = false;
            server.Stop();
        }

        /// <summary>
        /// Accepts connection from the server
        /// and passes them to a proxy.
        /// </summary>
        /// <returns></returns>
        private async Task AcceptConnections()
        {
            while (StillActive)
            {
                // check if we have a pending connection
                if (!server.Pending())
                {
                    // we don't wait a bit
                    Thread.Sleep(500);
                    // skip to top
                    continue;
                }
                var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Connected!");
                // create a proxy
                var proxy = new Network.Proxy(new DefaultReceiver(this), new DefaultSender(this), client);
                // add it to the list of connected nodes
                Connections.Add(proxy);    
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
            //Console.CancelKeyPress += m.OnUserExit;
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
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnUserExit(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            foreach (Proxy p in Connections)
            {
                Console.WriteLine("Why");
                p.QueueDataEvent(new NodeManagerComm(
                    DataReceivedEventArgs.ConstructMessage("kill")));
            }
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


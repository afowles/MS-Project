using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.IO;

using Defcore.Distributed.Network;
using Defcore.Distributed.Logging;

[assembly: InternalsVisibleTo("StartManager")]
[assembly: InternalsVisibleTo("defcore")]

/// <summary>
/// Manager namespace contains the classes to handle
/// the logic involved with all aspects of the node server.
/// </summary>
namespace Defcore.Distributed.Manager
{

    /// <summary>
    /// Master class, will manage connections
    /// involving the adhoc cluster system.
    /// </summary>
    internal class NodeManager
    {
        private TcpListener server;
        // Keeps the while loop running
        private bool DoneAccepting;
        // Indicates whether we still want to accept
        // connections. Different than DoneAccepting.
        private bool AcceptingConnections;
        private static string IpAddr = "";
        
        // Keep track of all connections, not just Nodes
        public List<Proxy> Connections;
        // Next Id for a proxy
        private static int NextId;

        // Thread-safe, only accessed within lock
        private object NodeLock = new object();
        public Dictionary<int, NodeRef> ConnectedNodes;

        // This is thread safe internally
        public JobScheduler Scheduler { get; private set;}

        // Handles Logging
        public Logger log { get; private set; }

        /// <summary>
        /// Creates a server listening
        /// on the given IP.
        /// </summary>
        /// <param name="ip">IP to listen on</param>
        public NodeManager(string ip)
        {
            // create logging
            log = Logger.ManagerLogInstance;
            Connections = new List<Proxy>();
            ConnectedNodes = new Dictionary<int, NodeRef>();
            NextId = 1;
            // Create and start up a job scheduler
            Scheduler = new JobScheduler(this);
            Scheduler.Start();
            try
            {
                IPAddress localAddr = IPAddress.Parse(ip);
                server = new TcpListener(localAddr, NetworkSendReceive.SERVER_PORT);
            }
            catch (SocketException e)
            {
                log.Log(String.Format("SocketException: {0}", e));
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
                    // we don't so wait a bit
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
                    var proxy = new Proxy(new DefaultReceiver(this), new DefaultSender(this), client, NextId++);
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
            string localIP = "";
            host = await Dns.GetHostEntryAsync(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            IpAddr = localIP;
            Console.WriteLine("NodeManager IP is: " + localIP);
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
            // stop the scheduler
            Scheduler.Stop();
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
        /// Add a connection to a node
        /// </summary>
        /// <param name="p"></param>
        public void AddConnectedNode(Proxy p)
        {
            lock(NodeLock)
            {
                ConnectedNodes.Add(p.id, new NodeRef(p, p.id));
            }
        }

        /// <summary>
        /// Send out the message that a job is available
        /// </summary>
        /// <param name="job"></param>
        public void SendJobOut(JobRef job)
        {
            lock(NodeLock)
            {
                int section_id = 0;
                int num_nodes = ConnectedNodes.Values.Count;
                int req_nodes = job.RequestedNodes;
                foreach (NodeRef node in ConnectedNodes.Values)
                {
                    if (req_nodes == 0)
                    {
                        break;
                    }
                    Console.WriteLine("Enqueing for Node");
                    string s = job.JobId + "," + section_id + "," + num_nodes + "|";
                    s += Path.GetFileName(job.PathToDll) + "|";
                    foreach (string arg in job.UserArgs)
                    {
                        s += arg + "|";
                    }
                    node.QueueDataEvent(new NodeManagerComm("file|" + s ));
                    node.busy = true;
                    req_nodes--;
                    section_id++;
                }
            }
        }

        /// <summary>
        /// Get the number of nodes not
        /// currently working on a job.
        /// </summary>
        /// <returns></returns>
        public int GetAvailableNodes()
        {
            int available = 0;
            lock(NodeLock)
            {
                //TODO: this is not a great approach, this is called often
                foreach (NodeRef node in ConnectedNodes.Values)
                {
                    if (!node.busy)
                    {
                        available += 1;
                    }
                }
            }
            return available;
        }

        /// <summary>
        /// Send back the job results to the user
        /// </summary>
        /// <param name="data"></param>
        public void SendJobResults(DataReceivedEventArgs data)
        {
            JobRef j = Scheduler.GetJob(int.Parse(data.args[1]));
            foreach(Proxy p in Connections)
            {
                if (p.id == j.ProxyId)
                {
                    p.QueueDataEvent(new NodeManagerComm("results|" + data.args[2]));
                }
            }
            
        }

    }

    internal class NodeRef
    {
        private Proxy proxy; 
        public int id { get; private set; }

        public bool busy;

        public NodeRef(Proxy p, int id_)
        {
            proxy = p;
            id = id_;
            busy = false;
        }

        public void QueueDataEvent(DataReceivedEventArgs d)
        {
            proxy.QueueDataEvent(d);
        }
    }

}


using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Defcore.Distributed.Network;
using Defcore.Distributed.Logging;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("StartManager")]
[assembly: InternalsVisibleTo("defcore")]

namespace Defcore.Distributed.Manager
{

    /// <summary>
    /// Master class, will manage connections
    /// involving the adhoc cluster system.
    /// </summary>
    internal class NodeManager
    {
        private readonly TcpListener _server;
        // Keeps the while loop running
        private bool _doneAccepting;
        // Indicates whether we still want to accept
        // connections. Different than DoneAccepting.
        private bool _acceptingConnections;
        private static string _ipAddr = "";
        
        // Keep track of all connections, not just Nodes
        public List<Proxy> Connections;
        // Next Id for a proxy
        private static int _nextId;

        // Thread-safe, only accessed within lock
        private readonly object _nodeLock = new object();
        public Dictionary<int, NodeRef> ConnectedNodes;

        // This is thread safe internally
        public JobScheduler Scheduler { get;}

        // Handles Logging
        public Logger Logger { get; }

        /// <summary>
        /// Creates a server listening
        /// on the given IP.
        /// </summary>
        /// <param name="ip">IP to listen on</param>
        public NodeManager(string ip)
        {
            Logger.LogFileName = "NodeManagerLog.txt";
            Logger.LogFor = "Node Manager";
            // create logging
            Logger = Logger.LogInstance;
            
            Connections = new List<Proxy>();
            ConnectedNodes = new Dictionary<int, NodeRef>();
            _nextId = 1;
            // Create and start up a job scheduler
            Scheduler = new JobScheduler(this);
            Scheduler.Start();
            try
            {
                IPAddress localAddr = IPAddress.Parse(ip);
                _server = new TcpListener(localAddr, NetworkSendReceive.ServerPort);
            }
            catch (SocketException e)
            {
                Logger.Log(String.Format("SocketException: {0}", e));
            }
        }

        /// <summary>
        /// Call to start listening
        /// on the server.
        /// </summary>
        public async Task StartListening()
        {
            Logger.Log("Starting Server");
            _doneAccepting = false;
            _acceptingConnections = true;
            _server.Start();
            await AcceptConnections();
        }

        /// <summary>
        /// Tells the server to stop listening
        /// </summary>
        public void StopListening()
        {
            _doneAccepting = true;
            _server.Stop();
        }

        /// <summary>
        /// Accepts connection from the server
        /// and passes them to a proxy.
        /// </summary>
        /// <returns></returns>
        private async Task AcceptConnections()
        {
            while (!_doneAccepting)
            {
                // check if we have a pending connection
                if (!_server.Pending())
                {
                    // we don't so wait a bit
                    Thread.Sleep(NetworkSendReceive.ServerSleep);
                    // skip to top
                    continue;
                }
                // it is possible we are done accepting connections
                // but not quite done with the while loop. During
                // cleanup we do not want to accept connections
                // but still need the server to be "running"
                if (_acceptingConnections)
                {
                    // await the client connection, this blocks
                    var client = await _server.AcceptTcpClientAsync();
                    Logger.Log("Connected!");
                    // create a default proxy
                    var proxy = new Proxy(new DefaultReceiver(this), new DefaultSender(), client, _nextId++);
                    // add it to the list of connections
                    Connections.Add(proxy);
                }  
            }
        }
       

        public static void Main()
        {     
            // Grab the IP address
            Task getIp = GetLocalIpAddress();
            getIp.Wait();
            // create a NodeManager
            NodeManager m = new NodeManager(_ipAddr);
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
        public static async Task GetLocalIpAddress()
        {
            string localIp = "";
            var host = await Dns.GetHostEntryAsync(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                }
            }
            _ipAddr = localIp;
            Console.WriteLine("NodeManager IP is: " + localIp);
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
            _acceptingConnections = false;
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
            lock(_nodeLock)
            {
                ConnectedNodes.Add(p.Id, new NodeRef(p, p.Id));
            }
        }

        /// <summary>
        /// Send out the message that a job is available
        /// </summary>
        /// <param name="job"></param>
        public void SendJobOut(JobRef job)
        {
            lock(_nodeLock)
            {
                var sectionId = 0;
                var reqNodes = job.RequestedNodes;
                foreach (var node in ConnectedNodes.Values)
                {
                    if (reqNodes == 0)
                    {
                        break;
                    }
                    Console.WriteLine("Enqueing for Node");
                    job.SectionId = sectionId;
                    job.TotalNodes = ConnectedNodes.Values.Count;
                    node.QueueDataEvent(new NodeManagerComm("file|" + JsonConvert.SerializeObject(job)));
                    node.Busy = true;
                    reqNodes--;
                    sectionId++;
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
            lock(_nodeLock)
            {
                //TODO: this is not a great approach, this is called often
                foreach (NodeRef node in ConnectedNodes.Values)
                {
                    if (!node.Busy)
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
            JobRef j = Scheduler.GetJob(int.Parse(data.Args[1]));
            foreach(Proxy p in Connections)
            {
                if (p.Id == j.ProxyId)
                {
                    p.QueueDataEvent(new NodeManagerComm("results|" + data.Args[2]));
                }
            }
            
        }

    }

    internal class NodeRef
    {
        private readonly Proxy _proxy; 
        public int Id { get; private set; }

        public bool Busy;

        public NodeRef(Proxy p, int id)
        {
            _proxy = p;
            Id = id;
            Busy = false;
        }

        public void QueueDataEvent(DataReceivedEventArgs d)
        {
            _proxy.QueueDataEvent(d);
        }
    }

}


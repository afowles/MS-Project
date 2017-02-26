using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;

using Distributed.Network;
using Distributed.Files;

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
        // TODO this needs to be a thread safe collection.
        public List<Proxy> ConnectedNodes;
        //public Dictionary<int, string> jobs = new Dictionary<int, string>();
        public List<DataReceivedEventArgs> Jobs = new List<DataReceivedEventArgs>();

        /// <summary>
        /// Creates a server listening
        /// on the given IP.
        /// </summary>
        /// <param name="ip">IP to listen on</param>
        public NodeManager(string ip)
        {
            
            ConnectedNodes = new List<Network.Proxy>();
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
                var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Connected!");
                // create a proxy
                var proxy = new Network.Proxy(new DefaultReceiver(this), new DefaultSender(this), client);
                // add it to the list of connected nodes
                ConnectedNodes.Add(proxy);    
            }
        }
       

        public static void Main()
        {
            // Grab the IP address
            Task getIp = GetLocalIPAddress();
            getIp.Wait();
            // create a NodeManager
            NodeManager m = new NodeManager(IpAddr);
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

    }

    internal class NodeManagerComm : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "send", MessageType.Send },
                { "job", MessageType.NewJob },
                { "file", MessageType.File },
                { "fileread", MessageType.FileRead }
            };

        public enum MessageType
        {
            Unknown,
            Send,
            NewJob,
            File,
            FileRead
        }

        public NodeManagerComm(string msg)
            : base(msg)
        {
            foreach(String s in args)
            {
                Console.WriteLine(s);
            }
            MessageType m = MessageType.Unknown;
            MessageMap.TryGetValue(args[0], out m);
            Protocol = m;
        }

    }


    /// <summary>
    /// Node Manager Receiver, most of this is handled by
    /// the default receiver.
    /// </summary>
    internal class NodeManagerReceiver : AbstractReceiver
    {
        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new NodeManagerComm(data);
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            
        }

    }

    internal class NodeManagerSender : AbstractSender
    {
        ConcurrentQueue<DataReceivedEventArgs> MessageQueue = new ConcurrentQueue<DataReceivedEventArgs>();
        
        public NodeManager manager;

        public NodeManagerSender(NodeManager manager)
        {
            this.manager = manager;
        }
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            //TODO: log this instead
            Console.WriteLine("NodeManagerSender: HandleReceiverEvent called");
            MessageQueue.Enqueue(e);
        }

        public override void Run()
        {
            // testing
            while (true)
            {
                DataReceivedEventArgs data;
                if (MessageQueue.TryDequeue(out data))
                {
                    Console.WriteLine("in run nm sender: ");
                    foreach (string s in data.args) { Console.Write(s + " "); };
                    Console.WriteLine("");
                    
                    if (data is NodeManagerComm)
                    {
                        HandleNodeCommunication(data as NodeManagerComm);
                    }
                }
                
            }
        }

        private void HandleNodeCommunication(NodeManagerComm data)
        {
            Console.WriteLine(data.Protocol);
            switch (data.Protocol)
            {
                // sent by a job file
                case NodeManagerComm.MessageType.NewJob:
                    Console.WriteLine("Received Job File: " + data.args[1]);
                    // add it to the list of jobs for later
                    manager.Jobs.Add(data);
                    //String s = Console.ReadLine();
                    foreach (Proxy p in manager.ConnectedNodes)
                    {
                        if (p.connection == ConnectionType.NODE)
                        {
                            Console.WriteLine("Enquing for Node");
                            p.QueueDataEvent(new NodeManagerComm("file|" + data.args[1]));
                        }
                            
                    }

                    
                    break;
                // Send back to all nodes that a file will be sent, only a node
                // will know what to do with this message, all other connections
                // will ignore this.
                case NodeManagerComm.MessageType.File:
                    Console.WriteLine("Node Manager: sending file info");
                    SendMessage(new string[] { "file", Path.GetFileName(data.args[1])});
                    break;

                // the machine that submitted the job file
                // needs to be on the same computer that the
                // manager is running on as of now.
                case NodeManagerComm.MessageType.Send:
                    //Console.Write("Input File:");
                    Console.WriteLine("Sending file");
                    if (manager.Jobs.Count > 0)
                    {
                        Console.WriteLine("Writing file: " + manager.Jobs[0].args[1]);
                        foreach (Proxy p in manager.ConnectedNodes)
                        {
                            if (p.connection == ConnectionType.NODE)
                            {
                                Console.WriteLine("Sending to file to node");
                                FileWrite.WriteOut(p.iostream, manager.Jobs[0].args[1]);
                            }
                                
                        }
                        proxy.iostream.Flush();
                        Console.WriteLine("Sent file");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("No Jobs");
                        break;
                    }
                case NodeManagerComm.MessageType.FileRead:
                    Console.WriteLine("Node Manageer: Node has read file");
                    SendMessage(new string[] { "execute" });
                    break;

            }
        }
    }
}


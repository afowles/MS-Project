using System;
using System.Net;
using System.Net.Sockets;
using Distributed.Proxy;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Distributed.Default;
using Distributed.Files;

[assembly: InternalsVisibleTo("StartManager")]

namespace Distributed.Node
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
        
        public List<Proxy.Proxy> ConnectedNodes;
        public Dictionary<int, string> jobs = new Dictionary<int, string>();

        /// <summary>
        /// Creates a server listening
        /// on the given IP.
        /// </summary>
        /// <param name="ip">IP to listen on</param>
        public NodeManager(string ip)
        {
            
            ConnectedNodes = new List<Proxy.Proxy>();
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
                var proxy = new Proxy.Proxy(new DefaultReceiver(this), new DefaultSender(this), client);
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
                { "Send", MessageType.Send },
                { "job", MessageType.NewJob }
            };

        public enum MessageType
        {
            Send,
            NewJob,
            Unknown
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
            //Testing send
            //MessageQueue.Enqueue(new NodeComm("file"));
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
                    if (data is NodeManagerComm)
                    {
                        HandleNodeCommunication(data as NodeComm);
                    }
                }
                
            }
        }

        private void HandleNodeCommunication(NodeComm data)
        {
            switch (data.Protocol)
            {
                case NodeComm.MessageType.File:
                    Console.Write("Input Cmd:");
                    String s = Console.ReadLine();
                   
                    s += " " + 100;
                    Console.WriteLine(s);
                    byte[] os = System.Text.Encoding.ASCII.GetBytes(s);
                    proxy.iostream.Write(os, 0, os.Length);
                    proxy.iostream.Flush();

                    
                    break;

                case NodeComm.MessageType.Send:
                    //Console.Write("Input File:");
                    Console.WriteLine("Writing file: " + data.args[1]);
                    foreach (Proxy.Proxy p in manager.ConnectedNodes)
                    {
                        FileWrite.WriteOut(p.iostream, data.args[1]);
                    }
                    
                    proxy.iostream.Flush();
                    Console.WriteLine("Sent");
                    break;
                case NodeManager
            }
        }
    }
}


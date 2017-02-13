using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Distributed.Proxy;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Distributed.Default;

namespace Distributed.Node
{
    
    /// <summary>
    /// Master Slave approach, will manage connections
    /// to nodes and pass of incoming clients. Look into
    /// possible UDP approach for no "master"
    /// </summary>
    public class NodeManager
    {
        private TcpListener server;
        bool StillActive;
        static string IpAddr = "?";
        Byte[] bytes = new Byte[BufferSize];
        const int BufferSize = 1024;
        private List<Proxy.Proxy> ConnectedNodes;

        public NodeManager(string ip)
        {
            Int32 port = 12345;
            ConnectedNodes = new List<Proxy.Proxy>();
            try
            {
                IPAddress localAddr = IPAddress.Parse(ip);
                server = new TcpListener(localAddr, port);
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
                var proxy = new Proxy.Proxy(new DefaultReceiver(), new DefaultSender(), client);
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

    public class NodeManagerComm : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "Send", MessageType.Send },
            };

        public enum MessageType
        {
            Send,
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
    public class NodeManagerReceiver : AbstractReceiver
    {
        public override void Run()
        {
            // Grab the network IO stream from the proxy.
            NetworkStream iostream = proxy.iostream;
            // setup a byte buffer
            Byte[] bytes = new Byte[1024];
            String data;

            try
            {
                while (true)
                {
                    try
                    {
                        // check if we got something
                        if (!iostream.DataAvailable)
                        {
                            Thread.Sleep(1);
                        }
                        else if (iostream.Read(bytes, 0, bytes.Length) > 0)
                        {
                            data = System.Text.Encoding.ASCII.GetString(bytes);
                            //TODO: log this
                            Console.WriteLine("Received: {0}", data);
                            // clear out buffer
                            Array.Clear(bytes, 0, bytes.Length);

                            // Create a DataReceivedEvent
                            NodeComm d = new NodeComm(data);
                            OnDataReceived(d);
                            
                        }

                    }
                    catch (IOException e)
                    {
                        //TODO: handle this
                        Console.Write(e);
                    }
                }
            }
            catch (Exception e)
            {
                //TODO: handle this
                Console.Write(e);
            }
            finally
            {
                //TODO: find a way to do this 
                //iostream.Close();
            }
        }
    }

    public class NodeManagerSender : AbstractSender
    {
        ConcurrentQueue<DataReceivedEventArgs> MessageQueue = new ConcurrentQueue<DataReceivedEventArgs>();

        public NodeManagerSender()
        {
            //Testing send
            MessageQueue.Enqueue(new NodeComm("file"));
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
                    if (data is NodeComm)
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
                    Console.Write("Input File:");
                    String f = Console.ReadLine();
                    FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read);
                    BinaryReader binFile = new BinaryReader(fs);
                    int bytesSize = 0;
                    byte[] downBuffer = new byte[2048];
                    while ((bytesSize = binFile.Read(downBuffer, 0, downBuffer.Length)) > 0)
                    {
                        proxy.iostream.Write(downBuffer, 0, bytesSize);
                    }
                    fs.Dispose();
                    proxy.iostream.Flush();
                    Console.WriteLine("Sent");
                    break;
            }
        }
    }
}


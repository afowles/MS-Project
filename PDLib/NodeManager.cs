using System;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using Distributed.Proxy;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace Distributed.Node
{
    
    /// <summary>
    /// Master Slave approach, will manage connections
    /// to nodes and pass of incoming clients. Look into
    /// possible UDP approach for no "master"
    /// </summary>
    public class NodeManager
    {
        TcpListener server;
        bool StillActive;
        Byte[] bytes = new Byte[BufferSize];
        const int BufferSize = 1024;
        /// <summary>
        /// 
        /// </summary>
        public NodeManager()
        {
            Int32 port = 12345;
            // null assignment for finally block
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
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

        public async void StartListening()
        {
            Console.WriteLine("Starting");
            StillActive = true;
            server.Start();
            await AcceptConnections();
        }

        public void StopListening()
        {
            StillActive = false;
            server.Stop();
        }

        private async Task AcceptConnections()
        {
            while (StillActive)
            {
                var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Connected!");
                // Pass off to Proxy, no point in storing 
                // that proxy object ... yet
                new Proxy.Proxy(new NodeReceiver(), new NodeManagerSender(), client);    
            }
        }
        public static void Main()
        {
            NodeManager m = new NodeManager();
            m.StartListening();
            while (m.StillActive) { }
        }

    }

    public class NodeManagerComm : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "Send", MessageType.Send },
            };
        public string[] args { get; }

        public enum MessageType
        {
            Send,
            Unknown
        }

        public NodeManagerComm(string msg)
            : base(msg)
        {
            args = ParseMessage(msg.ToLower());
            foreach(String s in args)
            {
                Console.WriteLine(s);
            }
            MessageType m = MessageType.Unknown;
            MessageMap.TryGetValue(args[0], out m);
        }

        private string[] ParseMessage(string message)
        {
            return message.Split(new char[] { ' ' });

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
                            // just for test
                            data = data.ToUpper();

                            NodeComm d = new NodeComm(data);
                            OnDataReceived(d);
                            Console.WriteLine(d.Protocol);


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

        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            //TODO: log this instead
            Console.WriteLine("NodeSender: HandleReceiverEvent called");
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
                    String s = Console.ReadLine();
                    String f = Console.ReadLine();
                    FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read);
                    s += " " + fs.Length;
                    Console.WriteLine(s);
                    byte[] os = System.Text.Encoding.ASCII.GetBytes(s);
                    proxy.iostream.Write(os, 0, os.Length);
                    proxy.iostream.Flush();

                    BinaryReader binFile = new BinaryReader(fs);
                    int bytesSize = 0;
                    byte[] downBuffer = new byte[2048];
                    while ((bytesSize = binFile.Read(downBuffer, 0, downBuffer.Length)) > 0)
                    {
                        proxy.iostream.Write(downBuffer, 0, bytesSize);
                    }
                    fs.Dispose();
                    break;
            }
        }
    }
}


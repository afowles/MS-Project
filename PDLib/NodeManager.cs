using System;

using System.Net;
using System.Net.Sockets;

using Distributed.Proxy;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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

    public class NodeManagerReceiver : AbstractReceiver
    {
        public override void Run()
        {
            throw new NotImplementedException();
        }
    }

    public class NodeManagerSender : AbstractSender
    {
        ConcurrentQueue<string> MessageQueue = new ConcurrentQueue<string>();

        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            //TODO: log this instead
            Console.WriteLine("NodeSender: HandleReceiverEvent called");
            MessageQueue.Enqueue(e.message);
        }

        public override void Run()
        {
            // testing
            while (true)
            {
                string message;
                if (MessageQueue.TryDequeue(out message))
                {
                    Console.WriteLine("NodeManager Sending Message");
                    message.ToUpper();
                    byte[] o = System.Text.Encoding.ASCII.GetBytes(message);
                    proxy.iostream.Write(o, 0, o.Length);
                }    

            }
        }
    }
}


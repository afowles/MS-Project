using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

using Distributed.Proxy;

namespace Distributed.Node
{
    
    /// <summary>
    /// Master Slave approach, will manage connections
    /// to nodes and pass of incoming clients. Look into
    /// possible UDP approach for no "master"
    /// </summary>
    public class NodeManager
    {
        const int BufferSize = 1024;
        /// <summary>
        /// 
        /// </summary>
        public NodeManager()
        {

        }

        public static void Main()
        {
            Int32 port = 12345;
            // null assignment for finally block
            TcpListener server = null;
            try
            { 
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);
                server.Start();

                Byte[] bytes = new Byte[BufferSize];
                String data = null;

                while (true)
                {
                    Console.WriteLine("Starting");

                
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;
            
                    NetworkStream stream = client.GetStream();

                    // Pass off to Proxy, no point in storing 
                    // that proxy object ... yet
                    new Proxy.Proxy(new NodeReceiver(), new NodeSender(), client);

                    
            }
            }
            catch(SocketException e)
            {
              Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
               // Stop listening for new clients.
               server.Stop();
            }

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
        public override void Run()
        {
            throw new NotImplementedException();
        }
    }
}

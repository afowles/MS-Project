using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Probably won't keep this namespace name since
/// it has no meaning
/// </summary>
/// <remarks>
/// This xml code style is interesting...
/// </remarks>
/// <author>Adam Fowles</author>
namespace Distributed.Node
{
    
    /// <summary>
    /// Master Slave approach, will manage connections
    /// to nodes and pass of incoming clients. Look into
    /// possible UDP approach for no "master"
    /// </summary>
    public class NodeManager
    {
        
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

                Byte[] bytes = new Byte[256];
                String data = null;

                while (true)
                {
                    Console.WriteLine("Starting");

                
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;

            
                    NetworkStream stream = client.GetStream();

                    int i;

      
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
             
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);

                    
                        data = data.ToUpper();

                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                   
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: {0}", data);
                    }

                    // Pass off to Proxy
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
}

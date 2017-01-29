using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Distributed.Proxy;

namespace Distributed.Node
{
    /// <summary>
    /// 
    /// </summary>
    public class NodeReceiver : AbstractReceiver
    {
        private byte[] data;

        /// <summary>
        /// Run method for a node receiver looks
        /// for messages from the node manager. 
        /// </summary>
        /// <note> This is being run from its own thread</note>
        public override void Run()
        {
            // Grab the network IO stream from the proxy.
            NetworkStream iostream = proxy.iostream;
            Byte[] bytes = new Byte[1024];
            String data;

            try
            {
                //TODO: change this to something custom like !ShutdownEvent.WaitOne(0)
                // alternatively look for a specific shutdown message. 
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
                            Console.WriteLine("Received: {0}", data);


                            data = data.ToUpper();

                            byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                            // this should be sender
                            //iostream.Write(msg, 0, msg.Length);
                            Console.WriteLine("Sent: {0}", data);
                        }
                        else
                        {
                            //terminate
                        }
                    }
                    catch (IOException e)
                    {
                        // Handling something
                    }
                }
            }
            catch (Exception e)
            {
                // Handling something
            }
            finally
            {
                iostream.Close();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class NodeSender : AbstractSender
    {
        public override void Run()
        {
            // simply read in and send out, for test
            while (true)
            {
                String s = Console.ReadLine();
                byte[] o = System.Text.Encoding.ASCII.GetBytes(s);
                proxy.iostream.Write(o, 0, o.Length);
                
            }
        }
    }
}

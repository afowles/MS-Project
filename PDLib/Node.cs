using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Distributed.Proxy
{
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
            try
            {
                //TODO: change this to something like !ShutdownEvent.WaitOne(0)
                while (true)
                {
                    try
                    {
                        // check if we got something
                        if (!iostream.DataAvailable)
                        {
                            Thread.Sleep(1);
                        }
                        else if (iostream.Read(data, 0, data.Length) > 0)
                        {
                            // do something
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
}

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using Distributed.Proxy;

namespace Distributed.Node
{
    public class Node
    {
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            int port;
            if (!Int32.TryParse(args[1], out port))
            {
                Console.WriteLine("Port could not be parsed.");
                Environment.Exit(1);
            }
            Proxy.Proxy p = new Proxy.Proxy(new NodeReceiver(), new NodeSender(), args[0], port);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class NodeReceiver : AbstractReceiver
    {

        /// <summary>
        /// Run method for a node receiver looks
        /// for messages from the node manager. 
        /// </summary>
        /// <note> This is being run from its own thread</note>
        public override void Run()
        {
            // Grab the network IO stream from the proxy.
            NetworkStream iostream = proxy.iostream;
            // setup a byte buffer
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
                            //TODO: log this
                            Console.WriteLine("Received: {0}", data);
                            // clear out buffer
                            Array.Clear(bytes, 0, bytes.Length);
                            // just for test
                            data = data.ToUpper();

                            // Raise the event for received data
                            // the virtual method OnDataRecived wraps the actual call
                            // DataReceived?.Invoke(this, e);
                            OnDataReceived(new DataReceivedEventArgs(data));
                          
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

    /// <summary>
    /// 
    /// </summary>
    public class NodeSender : AbstractSender
    {
        ConcurrentQueue<string> MessageQueue = new ConcurrentQueue<string>();

        public int HandleReceiverEvent(string message)
        {
            return 0;
        }

        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("NodeSender: HandleReceiverEvent called");
            //MessageQueue.Enqueue(e.message);
        }

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

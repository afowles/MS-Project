using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using Distributed.Proxy;
using System.Collections.Generic;

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

    public class NodeComm : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap = 
            new Dictionary<string, MessageType> {
                { "file", MessageType.File },
                { "send", MessageType.Send }
            };
        public string[] args { get; }

        public enum MessageType
        {
            Unknown,
            File,
            Send
            
        }

        public NodeComm(string msg) 
            : base(msg)
        {
            args = ParseMessage(msg.ToLower());
            Console.WriteLine(args.ToString());
            MessageType m;
            MessageMap.TryGetValue(args[0], out m);
            Protocol = m;
        }

        private string[] ParseMessage(string message)
        {
            return message.Split(new char[] {' '});
            
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

                            NodeComm d = new NodeComm(data);
                            OnDataReceived(d);
                            Console.WriteLine(d.Protocol);
                            if (d.Protocol == NodeComm.MessageType.File)
                            {
                                byte[] downBuffer = new byte[2048];
                                int byteSize = 0;
                                Console.WriteLine("Reading file...");
                                //while (!iostream.DataAvailable) { }
                                //iostream.Read(file, 0, file.Length);
                                FileStream Fs = new FileStream("test.py", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                                while ((byteSize = iostream.Read(downBuffer, 0, downBuffer.Length)) > 0)
                                {
                                    Fs.Write(downBuffer, 0, byteSize);
                                   
                                }
                                Console.WriteLine("finished");
                                Fs.Dispose();
                            }
                            // Raise the event for received data
                            // the virtual method OnDataRecived wraps the actual call
                            // DataReceived?.Invoke(this, e);
                            
                          
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
            MessageQueue.Enqueue(e.message);
        }

        public override void Run()
        {
            // simply read in and send out, for test
            while (true)
            {
               
                string message;
                if (MessageQueue.TryDequeue(out message))
                {
                    Console.WriteLine("Node Sending Message");
                    message = "Send";
                    byte[] o = System.Text.Encoding.ASCII.GetBytes(message);
                    proxy.iostream.Write(o, 0, o.Length);
                }

            }

        }
    }
}

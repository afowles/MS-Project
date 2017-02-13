using Distributed.Node;
using Distributed.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Distributed.Default
{

    internal class DefaultReceiver : AbstractReceiver
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
                            
                            Console.WriteLine("Received: {0}", data);
                            // clear out buffer
                            Array.Clear(bytes, 0, bytes.Length);

                            // Create a DataReceivedEvent
                           
                            DefaultDataComm d = new DefaultDataComm(data);
                            OnDataReceived(d);
                            Console.WriteLine(d.Protocol);
                            if (d.Protocol == DefaultDataComm.MessageType.Node)
                            {
                                // hand off to proper sender and receiver
                                proxy.HandOffSendReceive(new NodeManagerReceiver(), new NodeManagerSender());
                                // leave and stop this thread.
                                break;
                            }
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

    internal class DefaultSender : AbstractSender
    {
        ConcurrentQueue<DefaultDataComm> MessageQueue = new ConcurrentQueue<DefaultDataComm>();
        public DefaultSender()
        {
            MessageQueue.Enqueue(new DefaultDataComm("id"));
        }
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            MessageQueue.Enqueue(e as DefaultDataComm);
        }

        public override void Run()
        {
            while (true)
            {
                DefaultDataComm data;
                if (MessageQueue.TryDequeue(out data))
                {
                    if (data.Protocol == DefaultDataComm.MessageType.Unknown)
                    {

                    }
                    if (data.Protocol == DefaultDataComm.MessageType.Id)
                    {                       
                        Console.WriteLine("requesting Id");
                        byte[] os = System.Text.Encoding.ASCII.GetBytes("id" + DefaultDataComm.endl);
                        proxy.iostream.Write(os, 0, os.Length);
                    }
                    else
                    {
                        // otherwise we know what kind it is,
                        // and the above receive method has
                        // already called "hand off" with new sender
                        // and receivers.
                        break;
                    }
                }

            }
        }
    }

    internal class DefaultDataComm : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        public const string endl = " end";
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "node", MessageType.Node },
                { "query", MessageType.Query },
                { "job", MessageType.Job },
                { "id", MessageType.Id }
            };
        
        public enum MessageType
        {
            Unknown,
            Id,
            Node,
            Query,
            Job
            
        }

        public DefaultDataComm(string msg)
            : base(msg)
        {
            MessageType m = MessageType.Unknown;
            Console.WriteLine("this is:" + args[0] + "?");
            MessageMap.TryGetValue(args[0], out m);
            Protocol = m;
        }

    }
}

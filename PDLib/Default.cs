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

    /// <summary>
    /// A default receiver for a master node before
    /// the connection type is known.
    /// </summary>
    /// <remarks>
    /// When a connection is made to a server there
    /// is no way of knowing immediately if that connection
    /// belongs. Default receivers know enough to hand off
    /// connection to a proper receiver. For the master node
    /// manager there could be a query, job or node connection.
    /// </remarks>
    internal class DefaultReceiver : AbstractReceiver
    {
        /// <summary>
        /// Default receiver uses a separate thread
        /// with this run method.
        /// </summary>
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
                        // Check if we got something
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
                            
                            switch(d.Protocol)
                            {
                                case DefaultDataComm.MessageType.Node:
                                    // hand off to proper sender and receiver
                                    proxy.HandOffSendReceive(new NodeManagerReceiver(), new NodeManagerSender());
                                    // leave and stop this thread.
                                    return;
                                case DefaultDataComm.MessageType.Job:

                                    return;
                                case DefaultDataComm.MessageType.Query:

                                    return;
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

    /// <summary>
    /// The default sender for the master node manager
    /// before hand off.
    /// </summary>
    internal class DefaultSender : AbstractSender
    {
        // ConcurrentQueue for TryDequeue method
        ConcurrentQueue<DefaultDataComm> MessageQueue = new ConcurrentQueue<DefaultDataComm>();

        /// <summary>
        /// Constructor to add initial message of id
        /// on start up that message will be send out to the connected host
        /// to gather identification info.
        /// </summary>
        public DefaultSender()
        {
            MessageQueue.Enqueue(new DefaultDataComm("id"));
        }

        /// <summary>
        /// Handles receive event, adds message to queue
        /// which is checked by running thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            MessageQueue.Enqueue(e as DefaultDataComm);
        }

        /// <summary>
        /// Default sender uses thread with run method.
        /// </summary>
        public override void Run()
        {
            while (true)
            {
                // grab the message
                DefaultDataComm data;
                if (MessageQueue.TryDequeue(out data))
                {
                    // if we don't know who this is, the first message
                    // is an Id, request id.
                    if (data.Protocol == DefaultDataComm.MessageType.Unknown 
                        || data.Protocol == DefaultDataComm.MessageType.Id)
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

    /// <summary>
    /// Object passed between Default Receiver
    /// </summary>
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
            MessageMap.TryGetValue(args[0], out m);
            Protocol = m;
        }

    }
}

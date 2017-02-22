using Distributed.Node;
using Distributed.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Distributed.Default
{

    /// <summary>
    /// Object passed between Default Receiver
    /// </summary>
    internal class DefaultDataComm : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
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
        public NodeManager manager;

        public DefaultReceiver(NodeManager manager)
        {
            this.manager = manager;
        }

        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new DefaultDataComm(data);
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            switch ((e as DefaultDataComm).Protocol)
            {
                case DefaultDataComm.MessageType.Node:
                case DefaultDataComm.MessageType.Job:
                case DefaultDataComm.MessageType.Query:

                    // hand off to proper sender and receiver
                    proxy.HandOffSendReceive(new NodeManagerReceiver(), new NodeManagerSender(manager));
                    // leave and stop this thread.
                    done = true;
                    return;
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
        private NodeManager manager;
        /// <summary>
        /// Constructor to add initial message of id
        /// on start up that message will be send out to the connected host
        /// to gather identification info.
        /// </summary>
        public DefaultSender(NodeManager manager)
        {
            this.manager = manager;
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
  
}

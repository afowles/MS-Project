using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Distributed.Network;
using Distributed.Files;

[assembly: InternalsVisibleTo("StartNode")]

/// <summary>
/// Classes in Distributed.Node are internal to PDLib
/// Uses outside of this assembly are not allowed,
/// other than by the designated entry point.
/// This file contains classes as part of the framework, not the library.
/// </summary>
namespace Distributed.Node
{
    /// <summary>
    /// A node in the network
    /// </summary>
    internal class Node
    {
        /// <summary>
        /// Entry point to starting up a node.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            int port;
            if (!Int32.TryParse(args[1], out port))
            {
                Console.WriteLine("Port could not be parsed.");
                Environment.Exit(1);
            }
            Console.WriteLine("Starting");
            Proxy p = new Proxy(new NodeReceiver(), new NodeSender(), args[0], port);
        }
    }

    /// <summary>
    /// The data received event for communication
    /// with a node.
    /// </summary>
    internal class NodeComm : DataReceivedEventArgs
    {
        
        public MessageType Protocol { get; }
        // a mapping of input strings to protocols
        private static Dictionary<string, MessageType> MessageMap = 
            new Dictionary<string, MessageType> {
                { "file", MessageType.File },
                { "send", MessageType.Send },
                { "id", MessageType.Id }
            };

        public enum MessageType
        {
            Unknown,
            File,
            Send,
            Id
        }

        /// <summary>
        /// Data object representing a receive
        /// event, takes in the string message
        /// sent over the network.
        /// </summary>
        /// <param name="msg">message from outside source</param>
        public NodeComm(string msg) 
            : base(msg)
        {
            
            MessageType m;
            MessageMap.TryGetValue(args[0], out m);
            Protocol = m;
        }
    }

    /// <summary>
    /// Receiver object for a Node, handles incoming
    /// messages and raised a data receive event.
    /// </summary>
    internal class NodeReceiver : AbstractReceiver
    {

        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new NodeComm(data);
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            NodeComm d = e as NodeComm;

            if (d.Protocol == NodeComm.MessageType.File)
            {
                FileRead.ReadInWriteOut(proxy.iostream, "test");
            }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    internal class NodeSender : AbstractSender
    {
        ConcurrentQueue<NodeComm> MessageQueue = new ConcurrentQueue<NodeComm>();

        public NodeSender()
        {
            //MessageQueue.Enqueue(new NodeComm("node"));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("NodeSender: HandleReceiverEvent called");
            MessageQueue.Enqueue(e as NodeComm);
        }

        public override void Run()
        {
            // simply read in and send out, for test
            while (true)
            {
                NodeComm data;
                string message;
                if (MessageQueue.TryDequeue(out data))
                {
                    Console.WriteLine("Node Sending Message");
                    if (data.Protocol == NodeComm.MessageType.File)
                    {
                        message = "send" + " " + DataReceivedEventArgs.endl;
                        byte[] o = System.Text.Encoding.ASCII.GetBytes(message);
                        proxy.iostream.Write(o, 0, o.Length);
                    }
                    if (data.Protocol == NodeComm.MessageType.Id )
                    {
                        Console.WriteLine("Sending Node");
                        byte[] b = System.Text.Encoding.ASCII.GetBytes("node" + " " + NodeComm.endl);
                        proxy.iostream.Write(b, 0, b.Length);
                    }
                    
                }

            }

        }
    }
}

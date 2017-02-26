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
        public Node()
        {

        }

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
            Console.WriteLine("Node: Starting");
            Node n = new Node();
            Proxy p = new Proxy(new NodeReceiver(n), new NodeSender(n), args[0], port);
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
                { "id", MessageType.Id },
                { "fileread", MessageType.FileRead },
                { "execute", MessageType.Execute }
            };

        public enum MessageType
        {
            Unknown,
            File,
            Send,
            Id,
            FileRead,
            Execute
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
        public Node MyNode;
        private List<String> jobs = new List<String>();

        public NodeReceiver(Node n)
        {
            MyNode = n;
        }

        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new NodeComm(data);
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            NodeComm d = e as NodeComm;

            if (d.Protocol == NodeComm.MessageType.File)
            {
                // This will block on the iostream until the file reading
                // is over. The next thing sent over the network must be a file.
                FileRead.ReadInWriteOut(proxy.iostream, d.args[1]);
                jobs.Add(d.args[1]);
                OnDataReceived(new NodeComm(NodeComm.ConstructMessage("fileread")));
            }
            if (d.Protocol == NodeComm.MessageType.Execute)
            {
                Console.WriteLine("Node: executing");
                JobLauncher j = new JobLauncher(jobs[0]);
                j.LaunchJob();
            }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    internal class NodeSender : AbstractSender
    {
        ConcurrentQueue<NodeComm> MessageQueue = new ConcurrentQueue<NodeComm>();
        Node MyNode;

        public NodeSender(Node n)
        {
            MyNode = n;
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
                if (MessageQueue.TryDequeue(out data))
                {
                    if (data.Protocol == NodeComm.MessageType.File)
                    {
                        Console.WriteLine("Node: requesting file");
                        SendMessage(new string[] { "send" });
                    }
                    if (data.Protocol == NodeComm.MessageType.Id )
                    {
                        Console.WriteLine("Node: sending id");
                        SendMessage(new string[] { "node" });
                    }
                    if (data.Protocol == NodeComm.MessageType.FileRead)
                    {
                        Console.WriteLine("Node: sending done reading");
                        SendMessage(new string[] { "fileread" });
                    }
                    
                }

            }

        }
    }
}

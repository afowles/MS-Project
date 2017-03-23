using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Distributed.Network;
using Distributed.Files;
using Distributed.Logging;
using Distributed.Manager;
using System.IO;
using System.Threading.Tasks;

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
        private Proxy proxy;
        public Logger log { get; private set; }

        /// <summary>
        /// Construct a node with proxy
        /// at host, port
        /// </summary>
        /// <param name="host">host for proxy to NodeManager</param>
        /// <param name="port">port for proxy to NodeManager</param>
        public Node(string host, int port)
        {
            proxy = new Proxy(new NodeReceiver(this), new NodeSender(this), host, port, 0);
            log = Logger.NodeLogInstance;
        }

        /// <summary>
        /// Handles cleanup and communcation on shutdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnUserExit(object sender, ConsoleCancelEventArgs e)
        {
            log.Log("Node - Shuting down, user hit ctrl-c");
            // set cancel to true so we can do our own cleanup
            e.Cancel = true;
            // queue up the quitting message to the server
            proxy.QueueDataEvent(new NodeComm(NodeComm.ConstructMessage("quit")));
            
            // join on both sending and receiving threads
            proxy.Join();
            
            
        }

        public void QueueDataEvent(DataReceivedEventArgs d)
        {
            proxy.QueueDataEvent(d);
        }

        /// <summary>
        /// Entry point to starting up a node.
        /// </summary>
        /// <param name="args">ip</param>
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            Node n = new Node(args[0], NetworkSendReceive.SERVER_PORT);
            foreach(var v in Environment.GetEnvironmentVariables().Keys)
            {
                n.log.Log("Key: " + v + " -> " + Environment.GetEnvironmentVariables()[v]);
            }
            Console.WriteLine(Environment.ProcessorCount);
            n.log.Log("Node: Starting");
            // handle ctrl c
            Console.CancelKeyPress += n.OnUserExit;

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
                { "execute", MessageType.Execute },
                { "finished", MessageType.Finished },
                { "shutdown", MessageType.Shutdown },
                { "quit", MessageType.Quit }

            };

        public enum MessageType
        {
            Unknown,
            File,
            Send,
            Id,
            FileRead,
            Execute,
            Finished,
            // shutdown is when the server asks
            Shutdown,
            // quit is when this application stops
            Quit
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
        private Node parent;
        //private Queue<DataReceivedEventArgs> jobs = new Queue<DataReceivedEventArgs>();
        private DataReceivedEventArgs job;

        /// <summary>
        /// Node receiver constructor, needs to know
        /// about container class Node.
        /// </summary>
        /// <param name="n">container class Node</param>
        public NodeReceiver(Node n)
        {
            parent = n;
        }

        /// <summary>
        /// Create the specific data received event for a node
        /// </summary>
        /// <param name="data">input data from socket</param>
        /// <returns></returns>
        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new NodeComm(data);
        }

        /// <summary>
        /// Handle any additional receiving the node might want to do
        /// in the receiver class.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            NodeComm receivedData = e as NodeComm;
            switch(receivedData.Protocol)
            {
                case NodeComm.MessageType.File:
                    // This will block on the iostream until the file reading
                    // is over. The next thing sent over the network must be a file.
                    FileRead.ReadInWriteOut(proxy.iostream, receivedData.args[2]);
                    job = receivedData;
                    // after we have finished reading the data, let node manager know
                    OnDataReceived(new NodeComm(NodeComm.ConstructMessage("fileread")));
                    break;
                case NodeComm.MessageType.Execute:
                    //TODO, this should be its own task/thread
                    parent.log.Log("Node: executing");
                    JobLauncher j = new JobLauncher(job, parent);
                    j.LaunchJob();
                    break;
                case NodeComm.MessageType.Shutdown:
                case NodeComm.MessageType.Quit:
                    // shutting down or quitting, we are done receving
                    DoneReceiving = true;
                    break;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class NodeSender : AbstractSender
    {
        private ConcurrentQueue<NodeComm> MessageQueue = new ConcurrentQueue<NodeComm>();
        private Node parent;

        public NodeSender(Node n)
        {
            parent = n;
        }

        /// <summary>
        /// Queue up a message to respond to in run() method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            parent.log.Log("NodeSender: HandleReceiverEvent called");
            MessageQueue.Enqueue(e as NodeComm);
        }

        /// <summary>
        /// Main work area for sending from a node
        /// </summary>
        public override void Run()
        {
            try
            {
                while (!DoneSending)
                {
                    NodeComm data;
                    if (MessageQueue.TryDequeue(out data))
                    {
                        switch (data.Protocol)
                        {
                            case NodeComm.MessageType.File:
                                parent.log.Log("Node: requesting file");
                                // ask for the file with jobid from data.args[1]
                                string[] s = data.args[1].Split(',');
                                SendMessage(new string[] { "send", s[0] });
                                break;
                            case NodeComm.MessageType.Id:
                                parent.log.Log("Node: sending id");
                                // Send the type of communication, in this case node
                                SendMessage(new string[] { "node" });
                                break;
                            case NodeComm.MessageType.FileRead:
                                // let the manager know we have finished reading
                                parent.log.Log("Node: sending done reading");
                                SendMessage(new string[] { "fileread" });
                                break;
                            case NodeComm.MessageType.Finished:
                                parent.log.Log("Node: finished executing sending results");
                                SendMessage(data.args);
                                break;
                            case NodeComm.MessageType.Shutdown:
                                DoneSending = true;
                                return;
                            case NodeComm.MessageType.Quit:
                                parent.log.Log("Sending quit");
                                SendMessage(new string[] { "nodequit" });
                                // hack to get final message to actually send...
                                Task t = new Task(() =>
                                {
                                    FlushSender();
                                });
                                // this will actually wait forever... 
                                // unless given a timeout TODO find out why
                                t.Wait(100);
                                DoneSending = true;
                                break;
                            default:
                                parent.log.Log("Bad message");
                                parent.log.Log(data.message);
                                break;
                        }
                    }
                    else
                    {
                        // Sleep to avoid high CPU usage
                        Thread.Sleep(NetworkSendReceive.IO_SLEEP);
                    }

                }
            }
            // unavoidable, if the server fails this will be throw
            catch(IOException e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public async void FlushSender()
        {
            await proxy.iostream.FlushAsync();
        }
    }
}

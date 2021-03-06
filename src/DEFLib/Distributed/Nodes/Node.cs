﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Defcore.Distributed.Network;
using Defcore.Distributed.IO;
using Defcore.Distributed.Logging;
using System.IO;
using System.Threading.Tasks;
using Defcore.Distributed.Manager;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("StartNode")]
[assembly: InternalsVisibleTo("defcore")]

namespace Defcore.Distributed.Nodes
{
    /// <summary>
    /// A node in the network
    /// </summary>
    internal class Node
    {
        private readonly Proxy _proxy;
        public Logger Logger { get; }

        /// <summary>
        /// Construct a node with proxy
        /// at host, port
        /// </summary>
        /// <param name="host">host for proxy to NodeManager</param>
        /// <param name="port">port for proxy to NodeManager</param>
        public Node(string host, int port)
        {
            _proxy = new Proxy(new NodeReceiver(this), new NodeSender(this), host, port, 0);
            Logger.LogFileName = "NodeLog.txt";
            Logger.LogFor = "Node";
            Logger = Logger.LogInstance;
        }

        /// <summary>
        /// Handles cleanup and communcation on shutdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnUserExit(object sender, ConsoleCancelEventArgs e)
        {
            Logger.Log("Node - Shuting down, user hit ctrl-c");
            // set cancel to true so we can do our own cleanup
            e.Cancel = true;
            // queue up the quitting message to the server
            _proxy.QueueDataEvent(new NodeComm(DataReceivedEventArgs.ConstructMessage("quit")));
            
            // join on both sending and receiving threads
            _proxy.Join();
            
            
        }

        public void QueueDataEvent(DataReceivedEventArgs d)
        {
            _proxy.QueueDataEvent(d);
        }

        /// <summary>
        /// Entry point to starting up a node.
        /// </summary>
        /// <param name="args">ip</param>
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            Node n = new Node(args[0], NetworkSendReceive.ServerPort);
            if (n._proxy.IOStream == null)
            {
                Console.WriteLine("Could not create proxy to network");
                return;
            }
            foreach(var v in Environment.GetEnvironmentVariables().Keys)
            {
                n.Logger.Log("Key: " + v + " -> " + Environment.GetEnvironmentVariables()[v]);
            }
            Console.WriteLine(Environment.ProcessorCount);
            n.Logger.Log("Node: Starting");
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
        private static readonly Dictionary<string, MessageType> MessageMap = 
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
            MessageMap.TryGetValue(Args[0], out m);
            Protocol = m;
        }
    }

    /// <summary>
    /// Receiver object for a Node, handles incoming
    /// messages and raised a data receive event.
    /// </summary>
    internal class NodeReceiver : AbstractReceiver
    {
        private readonly Node _parent;
        private JobRef _job;

        /// <summary>
        /// Node receiver constructor, needs to know
        /// about container class Node.
        /// </summary>
        /// <param name="n">container class Node</param>
        public NodeReceiver(Node n)
        {
            _parent = n;
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
            if (receivedData != null)
                switch(receivedData.Protocol)
                {
                    case NodeComm.MessageType.File:
                        // This will block on the IOStream until the file reading
                        // is over. The next thing sent over the network must be a file.
                        _job = JsonConvert.DeserializeObject<JobRef>(receivedData.Args[1]);
                        FileRead.ReadInWriteOut(Proxy.IOStream, _job.FileName);
                        //job = receivedData;
                        // after we have finished reading the data, let node manager know
                        OnDataReceived(new NodeComm(DataReceivedEventArgs.ConstructMessage("fileread")));
                        break;
                    case NodeComm.MessageType.Execute:
                        //TODO, this should be its own task/thread
                        _parent.Logger.Log("Node: executing");
                        JobLauncher j = new JobLauncher(_job, _parent);
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
        private readonly ConcurrentQueue<NodeComm> _messageQueue = new ConcurrentQueue<NodeComm>();
        private readonly Node _parent;

        public NodeSender(Node n)
        {
            _parent = n;
        }

        /// <summary>
        /// Queue up a message to respond to in run() method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            _parent.Logger.Log("NodeSender: HandleReceiverEvent called");
            _messageQueue.Enqueue(e as NodeComm);
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
                    if (_messageQueue.TryDequeue(out data))
                    {
                        switch (data.Protocol)
                        {
                            case NodeComm.MessageType.File:
                                _parent.Logger.Log("Node: requesting file");
                                // ask for the file with jobid from data.args[1]
                                //string[] s = data.args[1].Split(',');
                                var jobId = JsonConvert.DeserializeObject<JobRef>(data.Args[1]).JobId;
                                SendMessage(new [] { "send", jobId.ToString() });
                                break;
                            case NodeComm.MessageType.Id:
                                _parent.Logger.Log("Node: sending id");
                                // Send the type of communication, in this case node
                                SendMessage(new [] { "node" });
                                break;
                            case NodeComm.MessageType.FileRead:
                                // let the manager know we have finished reading
                                _parent.Logger.Log("Node: sending done reading");
                                SendMessage(new [] { "fileread" });
                                break;
                            case NodeComm.MessageType.Finished:
                                _parent.Logger.Log("Node: finished executing sending results");
                                SendMessage(data.Args);
                                break;
                            case NodeComm.MessageType.Shutdown:
                                DoneSending = true;
                                return;
                            case NodeComm.MessageType.Quit:
                                _parent.Logger.Log("Sending quit");
                                SendMessage(new [] { "nodequit" });
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
                                _parent.Logger.Log("Bad message");
                                _parent.Logger.Log(data.Message);
                                break;
                        }
                    }
                    else
                    {
                        // Sleep to avoid high CPU usage
                        Thread.Sleep(IOSleep);
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
            await Proxy.IOStream.FlushAsync();
        }
    }
}

using System;
using System.Collections.Generic;

using Distributed.Network;
using System.Collections.Concurrent;
using System.Threading;
using Distributed.Files;
using System.IO;

namespace Distributed.Manager
{
    internal class NodeManagerComm : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "send", MessageType.Send },
                { "job", MessageType.NewJob },
                { "file", MessageType.File },
                { "fileread", MessageType.FileRead },
                { "nodequit", MessageType.NodeQuit },
                { "connectionquit", MessageType.ConnectionQuit },
                { "kill", MessageType.Shutdown },
                { "submit", MessageType.SubmitJob },
                { "finished", MessageType.NodeFinished }
            };

        public enum MessageType
        {
            Unknown,
            Send,
            NewJob,
            File,
            FileRead,
            Shutdown,
            NodeQuit,
            ConnectionQuit,
            NodeFinished,
            SubmitJob

        }

        public NodeManagerComm(string msg)
            : base(msg)
        {
            MessageType m = MessageType.Unknown;
            MessageMap.TryGetValue(args[0], out m);
            Protocol = m;
        }

    }


    /// <summary>
    /// Node Manager Receiver, most of this is handled by
    /// the default receiver.
    /// </summary>
    internal class NodeManagerReceiver : AbstractReceiver
    {
        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new NodeManagerComm(data);
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            NodeManagerComm data = e as NodeManagerComm;
            switch(data.Protocol)
            {
                case NodeManagerComm.MessageType.Shutdown:
                case NodeManagerComm.MessageType.NodeQuit:
                case NodeManagerComm.MessageType.ConnectionQuit:
                    DoneReceiving = true;
                    break;
            }

        }

    }

    internal class NodeManagerSender : AbstractSender
    {
        ConcurrentQueue<DataReceivedEventArgs> MessageQueue = new ConcurrentQueue<DataReceivedEventArgs>();

        public NodeManager manager;

        public NodeManagerSender(NodeManager manager)
        {
            this.manager = manager;
        }
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            //TODO: log this instead
            Console.WriteLine("NodeManagerSender: HandleReceiverEvent called");
            MessageQueue.Enqueue(e);
        }

        public override void Run()
        {            
            while (!DoneSending)
            {
                DataReceivedEventArgs data;
                if (MessageQueue.TryDequeue(out data))
                {
                    if (data is NodeManagerComm)
                    {
                        HandleNodeCommunication(data as NodeManagerComm);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }

            }
        }

        private void HandleNodeCommunication(NodeManagerComm data)
        {
            Console.WriteLine("PROTOCOL:" + data.Protocol);
            switch (data.Protocol)
            {
                case NodeManagerComm.MessageType.SubmitJob:
                    SendMessage(new string[] { "submit" });
                    break;
                // sent by a job file
                case NodeManagerComm.MessageType.NewJob:
                    Console.WriteLine("Received Job File: " + data.args[1]);
                    // add it to the list of jobs for later
                    manager.Scheduler.AddJob(data);
                    break;

                case NodeManagerComm.MessageType.File:
                    Console.WriteLine("Node Manager: sending file info");
                    SendMessage(data.args);
                    break;

                // the machine that submitted the job file
                // needs to be on the same computer that the
                // manager is running on as of now.
                case NodeManagerComm.MessageType.Send:

                    string[] s = data.args[1].Split(',');
                    JobRef j = manager.Scheduler.GetJob(int.Parse(s[0]));
                    if (j != null)
                    {
                        Console.WriteLine("Sending file: " + Path.GetFileName(j.PathToDll));
                        //bool found = manager.Scheduler.GetJob(Path.GetFileName(data.args[1]), out d);
                        Console.WriteLine("Writing file: " + j.PathToDll);
                        FileWrite.WriteOut(proxy.iostream, j.PathToDll);
                        proxy.iostream.Flush();
                        Console.WriteLine("Sent file");
                    }
                    else
                    {
                        Console.WriteLine("No jobs... bad");
                    }
                    break;
                case NodeManagerComm.MessageType.FileRead:
                    Console.WriteLine("Node Manageer: Node has read file");
                    SendMessage(new string[] { "execute" });
                    break;
                case NodeManagerComm.MessageType.NodeFinished:
                    manager.ConnectedNodes[proxy.id].busy = false;
                    break;
                case NodeManagerComm.MessageType.NodeQuit:
                    // remove the entry associated with
                    // this proxy's id
                    manager.ConnectedNodes.Remove(proxy.id);
                    // goto case is not harmful (like a regular 'goto' in other languages)
                    goto case NodeManagerComm.MessageType.ConnectionQuit;
                case NodeManagerComm.MessageType.ConnectionQuit:
                    // remove the connection
                    manager.Connections.Remove(proxy);
                    DoneSending = true;
                    break;
                case NodeManagerComm.MessageType.Shutdown:
                    SendMessage(new string[] { "shutdown" });
                    // shutdown after we have sent the message
                    DoneSending = true;
                    break;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Defcore.Distributed.Network;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;

using Defcore.Distributed.IO;
using Newtonsoft.Json;

namespace Defcore.Distributed.Manager
{
    /// <summary>
    /// Data event class for incoming messages
    /// to the NodeManager.
    /// </summary>
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
                { "finished", MessageType.NodeFinished },
                { "results", MessageType.Results }
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
            Results,
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
        /// <summary>
        /// Create the specific DataReceivedEvent Args type
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Specific DataReceivedEventArgs type</returns>
        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new NodeManagerComm(data);
        }

        /// <summary>
        /// Handle any additional receiving that needs to be done.
        /// </summary>
        /// <param name="sender">unused</param>
        /// <param name="e">data event object from</param>
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

    /// <summary>
    /// Class to handle sending of responses to clients.
    /// </summary>
    internal class NodeManagerSender : AbstractSender
    {
        private ConcurrentQueue<DataReceivedEventArgs> MessageQueue 
            = new ConcurrentQueue<DataReceivedEventArgs>();
        public NodeManager manager;

        /// <summary>
        /// Constructor for NodeManagerSender
        /// </summary>
        /// <param name="manager">parent container class</param>
        public NodeManagerSender(NodeManager manager)
        {
            this.manager = manager;
        }

        /// <summary>
        /// Handle the receiving of data 
        /// from the NodeManagerReceiver
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            MessageQueue.Enqueue(e);
        }

        /// <summary>
        /// Main work loop for NodeManagerSender
        /// </summary>
        public override void Run()
        {
            try
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
            catch(IOException e)
            {
                manager.log.Log("NodeManagerSender caught exception: " + e.ToString());
            }
        }

        private void HandleNodeCommunication(NodeManagerComm data)
        {
            switch (data.Protocol)
            {
                case NodeManagerComm.MessageType.SubmitJob:
                    SendMessage(new string[] { "submit" });
                    break;
                // sent by a job file
                case NodeManagerComm.MessageType.NewJob:
                    Console.WriteLine("Received Job File: " + data.args[1]);
                    // add it to the list of jobs for later
                    var jobRef = JsonConvert.DeserializeObject<JobRef>(data.args[1]);
                    manager.Scheduler.AddJob(jobRef, proxy.id);
                    Console.WriteLine("Proxy id is:" + proxy.id);
                    break;

                case NodeManagerComm.MessageType.File:
                    Console.WriteLine("Node Manager: sending file info");
                    SendMessage(data.args);
                    break;

                // the machine that submitted the job file
                // needs to be on the same computer that the
                // manager is running on as of now.
                case NodeManagerComm.MessageType.Send:

                    var jobId = int.Parse(data.args[1]);
                    //var job = JsonConvert.DeserializeObject<JobRef>(data.args[1]);
                    var j = manager.Scheduler.GetJob(jobId);
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
                    manager.SendJobResults(data);
                    break;
                case NodeManagerComm.MessageType.Results:
                    // forward along the results to the user
                    SendMessage(data.args);
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
                case NodeManagerComm.MessageType.Unknown:
                    break;
                default:
                    // should never get here, unknown handles it
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

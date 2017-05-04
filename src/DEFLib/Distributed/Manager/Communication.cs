using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;

using Defcore.Distributed.IO;
using Defcore.Distributed.Network;
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
        private static readonly Dictionary<string, MessageType> MessageMap =
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
            MessageType m;
            MessageMap.TryGetValue(Args[0], out m);
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
            var data = e as NodeManagerComm;
            if (data == null) return;
            switch(data.Protocol)
            {
                case NodeManagerComm.MessageType.Shutdown:
                case NodeManagerComm.MessageType.NodeQuit:
                case NodeManagerComm.MessageType.ConnectionQuit:
                    DoneReceiving = true;
                    break;
                case NodeManagerComm.MessageType.Unknown:
                    break;
            }
        }
    }

    /// <summary>
    /// Class to handle sending of responses to clients.
    /// </summary>
    internal class NodeManagerSender : AbstractSender
    {
        private readonly ConcurrentQueue<DataReceivedEventArgs> _messageQueue 
            = new ConcurrentQueue<DataReceivedEventArgs>();
        public NodeManager Manager;

        /// <summary>
        /// Constructor for NodeManagerSender
        /// </summary>
        /// <param name="manager">parent container class</param>
        public NodeManagerSender(NodeManager manager)
        {
            Manager = manager;
        }

        /// <summary>
        /// Handle the receiving of data 
        /// from the NodeManagerReceiver
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            _messageQueue.Enqueue(e);
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
                    if (_messageQueue.TryDequeue(out data))
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
                Manager.Logger.Log("NodeManagerSender caught exception: " + e);
            }
        }

        private void HandleNodeCommunication(NodeManagerComm data)
        {
            switch (data.Protocol)
            {
                case NodeManagerComm.MessageType.SubmitJob:
                    SendMessage(new [] { "submit" });
                    break;
                // sent by a job file
                case NodeManagerComm.MessageType.NewJob:
                    Manager.Logger.Log("Received Job File: " + data.Args[1]);
                    // add it to the list of jobs for later
                    var jobRef = JsonConvert.DeserializeObject<JobRef>(data.Args[1]);
                    Manager.Scheduler.AddJob(jobRef, Proxy.Id);
                    Manager.Logger.Log("Proxy id is:" + Proxy.Id);
                    break;

                case NodeManagerComm.MessageType.File:
                    Manager.Logger.Log("Node Manager: sending file info");
                    SendMessage(data.Args);
                    break;

                // the machine that submitted the job file
                // needs to be on the same computer that the
                // manager is running on as of now.
                case NodeManagerComm.MessageType.Send:

                    var jobId = int.Parse(data.Args[1]);
                    //var job = JsonConvert.DeserializeObject<JobRef>(data.args[1]);
                    var j = Manager.Scheduler.GetJob(jobId);
                    if (j != null)
                    {
                        Manager.Logger.Log("Sending file: " + Path.GetFileName(j.PathToDll));
                        //bool found = manager.Scheduler.GetJob(Path.GetFileName(data.args[1]), out d);
                        Manager.Logger.Log("Writing file: " + j.PathToDll);
                        FileWrite.WriteOut(Proxy.IOStream, j.PathToDll);
                        Proxy.IOStream.Flush();
                        Manager.Logger.Log("Sent file");
                    }
                    else
                    {
                        Manager.Logger.Log("No jobs... bad");
                    }
                    break;
                case NodeManagerComm.MessageType.FileRead:
                    Manager.Logger.Log("Node Manageer: Node has read file");
                    SendMessage(new [] { "execute" });
                    break;
                case NodeManagerComm.MessageType.NodeFinished:
                    Manager.ConnectedNodes[Proxy.Id].Busy = false;
                    Manager.SendJobResults(data);
                    break;
                case NodeManagerComm.MessageType.Results:
                    // forward along the results to the user
                    SendMessage(data.Args);
                    break;
                case NodeManagerComm.MessageType.NodeQuit:
                    // remove the entry associated with
                    // this proxy's id
                    Manager.ConnectedNodes.Remove(Proxy.Id);
                    // goto case is not harmful (like a regular 'goto' in other languages)
                    goto case NodeManagerComm.MessageType.ConnectionQuit;
                case NodeManagerComm.MessageType.ConnectionQuit:
                    // remove the connection
                    Manager.Connections.Remove(Proxy);
                    DoneSending = true;
                    break;
                case NodeManagerComm.MessageType.Shutdown:
                    SendMessage(new [] { "shutdown" });
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

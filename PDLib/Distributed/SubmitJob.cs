using System;
using Distributed.Network;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SubmitJob")]

namespace Distributed
{
    internal class SubmitJob
    {
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            Console.WriteLine("Starting Job Launcher");
            Proxy p = new Proxy(new JobReceiver(), 
                new JobSender(args), args[0], NetworkSendReceive.SERVER_PORT);
        }
    }

    internal sealed class JobEventArgs : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "id", MessageType.Id },
                { "accept", MessageType.Accept },
                { "shutdown", MessageType.Shutdown }
            };

        public enum MessageType
        {
            Unknown,
            Id,
            Accept,
            Shutdown
        }

        public JobEventArgs(string msg) : base(msg)
        {
            MessageType m = MessageType.Unknown;
            MessageMap.TryGetValue(args[0], out m);
            Protocol = m;
        }
    }

    internal sealed class JobReceiver : AbstractReceiver
    {

        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new JobEventArgs(data);
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            JobEventArgs data = e as JobEventArgs;
            if (data.Protocol == JobEventArgs.MessageType.Shutdown)
            {
                DoneReceiving = true;
            }
        }
    }

    internal sealed class JobSender : AbstractSender
    {
        ConcurrentQueue<JobEventArgs> MessageQueue = new ConcurrentQueue<JobEventArgs>();

        private string PathToDLL;
        private string[] UserArgs;

        public JobSender(string[] args)
        {
            PathToDLL = args[1];
            // arg[0] is ip, arg[1] is dll
            UserArgs = new string[args.Length - 2];
            for (int i = 2; i < args.Length; i++)
            {
                UserArgs[i - 2] = args[i];
            }  
        }

        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            MessageQueue.Enqueue(e as JobEventArgs);
        }

        public override void Run()
        {
            // simply read in and send out, for test
            while (true)
            {
                JobEventArgs data;
                if (MessageQueue.TryDequeue(out data))
                {
                    Console.WriteLine("Job Sending Message");
                    if (data.Protocol == JobEventArgs.MessageType.Id)
                    {
                        Console.WriteLine("Sending Job Id");
                        SendMessage(new string[] { "job" });
                        // TODO, do this with message passing instead
                        // guessing at set up time
                        Thread.Sleep(2000);
                        Console.WriteLine("Sending Job");
                        // job in this context corresponds to job manager.
                        string[] args = new string[UserArgs.Length + 2];
                        args[0] = "job";
                        args[1] = PathToDLL;
                        int i = 2;
                        foreach(string s in UserArgs)
                        {
                            args[i] = s;
                            i++;
                        }
                        SendMessage(args);
                    }
                    if (data.Protocol == JobEventArgs.MessageType.Shutdown)
                    {

                    }

                }
                else
                {
                    Thread.Sleep(100);
                }

            }
        }
    }
}

using System;
using Distributed.Proxy;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Distributed.Node;

namespace Distributed
{
    class JobLauncher
    {
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            Console.WriteLine("Starting Job Launcher");
            Proxy.Proxy p = new Proxy.Proxy(new JobReceiver(args), 
                new JobSender(), args[0], NetworkSendReceive.SERVER_PORT);
        }
    }

    internal sealed class JobEventArgs : NodeManagerComm
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "id", MessageType.Id }
            };

        public new enum MessageType
        {
            Unknown,
            Id
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
        private string PathToDLL;

        public JobReceiver(string[] args)
        {
            PathToDLL = args[1];
            // TODO... pass along the other args the user wants to give
            // to their program.
        }

        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new JobEventArgs(data);
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            
        }
    }

    internal sealed class JobSender : AbstractSender
    {
        ConcurrentQueue<JobEventArgs> MessageQueue = new ConcurrentQueue<JobEventArgs>();

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
                string message;
                if (MessageQueue.TryDequeue(out data))
                {
                    Console.WriteLine("Job Sending Message");
                    if (data.Protocol == JobEventArgs.MessageType.Id)
                    {
                        Console.WriteLine("Sending Job Id");
                        byte[] b = System.Text.Encoding.ASCII.GetBytes("job" + " " + DataReceivedEventArgs.endl);
                        proxy.iostream.Write(b, 0, b.Length);
                    }

                }

            }
        }
    }
}

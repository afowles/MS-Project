using System;
using Distributed.Network;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SubmitJob")]

namespace Distributed
{
    internal class JobLauncher
    {
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            Console.WriteLine("Starting Job Launcher");
            Network.Proxy p = new Network.Proxy(new JobReceiver(), 
                new JobSender(args), args[0], NetworkSendReceive.SERVER_PORT);
        }
    }

    internal sealed class JobEventArgs : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "id", MessageType.Id },
                { "accept", MessageType.Accept }
            };

        public enum MessageType
        {
            Unknown,
            Id,
            Accept
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
            
        }
    }

    internal sealed class JobSender : AbstractSender
    {
        ConcurrentQueue<JobEventArgs> MessageQueue = new ConcurrentQueue<JobEventArgs>();

        private string PathToDLL;

        public JobSender(string[] args)
        {
            PathToDLL = args[1];
            // TODO... pass along the other args the user wants to give
            // to their program.
            //
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
                        byte[] b = System.Text.Encoding.ASCII.GetBytes("job" + " " + DataReceivedEventArgs.endl);
                        proxy.iostream.Write(b, 0, b.Length);
                        // TODO, do this with message passing instead
                        // guessing at set up time
                        Thread.Sleep(1000);
                        // job in this context corresponds to job manager.
                        byte[] b2 = System.Text.Encoding.ASCII.GetBytes("job" + " " + 
                            PathToDLL + " " + DataReceivedEventArgs.endl);
                    }

                }

            }
        }
    }
}

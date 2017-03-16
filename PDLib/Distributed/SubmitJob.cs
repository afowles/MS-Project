using System;
using Distributed.Network;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

[assembly: InternalsVisibleTo("SubmitJob")]

namespace Distributed
{
    /// <summary>
    /// Class to submit a job to the cluster
    /// </summary>
    internal class SubmitJob
    {
        /// <summary>
        /// Main method that is invoked by SubmitJob Program
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            Console.WriteLine("Starting Job Launcher");
            Console.WriteLine("Username is: " + GetUserName());
            // create a proxy, this application only has one so id of 0
            Proxy p = new Proxy(new JobReceiver(), 
                new JobSender(args), args[0], NetworkSendReceive.SERVER_PORT, 0);
            
        }

        /// <summary>
        /// Get the user name
        /// </summary>
        /// <remarks>
        /// This is a hack until the time
        /// .NET core flushes out the Enviroment
        /// namespace with proper username information.
        /// "whoami" command should work on linux/mac/windows
        /// although it would be proper to use "id -un"
        /// at this point in time.
        /// </remarks>
        /// <returns></returns>
        public static string GetUserName()
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = "whoami";
            // redirect standard in, not using currently
            startInfo.RedirectStandardOutput = true;
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = startInfo;

            // setup and allow an event
            process.EnableRaisingEvents = true;
            // for building output from standard out
            StringBuilder outputBuilder = new StringBuilder();
            // add event handling for process
            process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler
            (
                delegate (object sender, System.Diagnostics.DataReceivedEventArgs e)
                {
                    // append the output data to a string
                    outputBuilder.Append(e.Data);

                }
            );
            string error;
            try
            {
                // start
                process.Start();
                // read is async
                process.BeginOutputReadLine();
                // wait for user process to end
                process.WaitForExit();
                process.CancelOutputRead();
            }
            catch (Exception e)
            {
                error = e.ToString();
                Console.WriteLine(error);
                outputBuilder.Clear();
                outputBuilder.Append("Unknown");

            }

            return outputBuilder.ToString();
        }
    }

    internal sealed class JobEventArgs : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "id", MessageType.Id },
                { "accept", MessageType.Accept },
                { "submit" , MessageType.Submit },
                { "shutdown", MessageType.Shutdown }
            };

        public enum MessageType
        {
            Unknown,
            Id,
            Accept,
            Submit,
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
            switch(data.Protocol)
            {
                case JobEventArgs.MessageType.Shutdown:
                    DoneReceiving = true;
                    break;
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
            while (!DoneSending)
            {
                JobEventArgs data;
                if (MessageQueue.TryDequeue(out data))
                {
                    Console.WriteLine("Job Sending Message");
                    switch (data.Protocol)
                    {
                        case JobEventArgs.MessageType.Id:
                            Console.WriteLine("Sending Job Id");
                            SendMessage(new string[] { "job" });
                            break;
                        case JobEventArgs.MessageType.Submit:
                            Console.WriteLine("Sending Job");
                            // job in this context corresponds to job manager.
                            string[] args = new string[UserArgs.Length + 3];
                            // get the username of whoever is running this job
                            string username = SubmitJob.GetUserName();
                            args[0] = "job";
                            args[1] = username;
                            args[2] = PathToDLL;
                            int i = 3;
                            foreach (string s in UserArgs)
                            {
                                args[i] = s;
                                i++;
                            }
                            SendMessage(args);
                            break;
                        case JobEventArgs.MessageType.Shutdown:
                            DoneSending = true;
                            break;
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

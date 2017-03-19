using System;
using Distributed.Network;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using Distributed.Logging;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("SubmitJob")]

namespace Distributed
{
    /// <summary>
    /// Class to submit a job to the cluster
    /// </summary>
    internal class SubmitJob
    {
        private Proxy proxy;
        public Logger log { get; private set; }

        /// <summary>
        /// Construct a node with proxy
        /// at host, port
        /// </summary>
        /// <param name="host">host for proxy to NodeManager</param>
        /// <param name="port">port for proxy to NodeManager</param>
        public SubmitJob(string host, int port, string[] args)
        {
            proxy = new Proxy(new JobReceiver(), new JobSender(args), host, port, 0);
            log = Logger.NodeLogInstance;
        }

        /// <summary>
        /// Main method that is invoked by SubmitJob Program
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // try to connect to the NodeManager
            Console.WriteLine("Starting Job Launcher");
            Console.WriteLine("Username is: " + GetUserName());
            SubmitJob sj = new SubmitJob(args[0], NetworkSendReceive.SERVER_PORT, args);
            Console.CancelKeyPress += sj.OnUserExit;
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

        /// <summary>
        /// Handles cleanup and communcation on shutdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnUserExit(object sender, ConsoleCancelEventArgs e)
        {
            log.Log("Submitjob - Shuting down, user hit ctrl-c");
            // set cancel to true so we can do our own cleanup
            e.Cancel = true;
            // queue up the quitting message to the server
            proxy.QueueDataEvent(new JobEventArgs("quit"));
            // join on both sending and receiving threads
            proxy.Join();

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
                { "shutdown", MessageType.Shutdown },
                { "quit", MessageType.Quit },
                { "results", MessageType.Results }
            };

        public enum MessageType
        {
            Unknown,
            Id,
            Accept,
            Submit,
            Results,
            Shutdown,
            Quit
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
                case JobEventArgs.MessageType.Quit:
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
                        case JobEventArgs.MessageType.Results:
                            Console.WriteLine(data.args[1]);
                            break;
                        case JobEventArgs.MessageType.Shutdown:
                            DoneSending = true;
                            break;
                        case JobEventArgs.MessageType.Quit:
                            SendMessage(new string[] { "connectionquit" });
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
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }

            }
        }

        public async void FlushSender()
        {
            await proxy.iostream.FlushAsync();
        }
    }
}

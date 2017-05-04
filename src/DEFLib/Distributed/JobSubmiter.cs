using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Text;
using Defcore.Distributed.Logging;
using Defcore.Distributed.Network;
using System.Threading.Tasks;
using Defcore.Distributed.Assembly;
using Defcore.Distributed.IO;
using Defcore.Distributed.Jobs;
using Defcore.Distributed.Manager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("SubmitJob")]
[assembly: InternalsVisibleTo("defcore")]
namespace Defcore.Distributed
{
    /// <summary>
    /// Class to submit a job to the cluster
    /// </summary>
    internal class SubmitJob
    {
        private readonly Proxy _proxy;
        private static string _ipAddr = "";
        public Logger Logger { get; }


        /// <summary>
        /// construct a submit node at host and port
        /// with job reference information.
        /// </summary>
        /// <param name="host">host for proxy to NodeManager</param>
        /// <param name="port">port for proxy to NodeManager</param>
        /// <param name="job">job reference for the job to be submitted</param>
        public SubmitJob(string host, int port, JobRef job)
        {
            _proxy = new Proxy(new JobReceiver(), new JobSender(job,this), host, port, 0);
            Logger.LogFileName = "SubmitLogFile.txt";
            Logger.LogFor = "Submit";
            Logger = Logger.LogInstance;
            Logger.Log("Starting Job Launcher");
            Logger.Log("Username is: " + GetUserName());
        }

        /// <summary>
        /// Main method that is invoked by SubmitJob Program
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var getIp = GetLocalIpAddress();
            getIp.Wait();

            // try to connect to the NodeManager
            
            var jobRef = new JobRef();
            var loader = new CoreLoader<Job>(args[0]);
            var userArgs = new string[args.Length - 1];
            for (var i = 1; i < args.Length; i++)
            {
                userArgs[i - 1] = args[i];
            }
            // setup the job reference
            jobRef.RequestedNodes = (int)loader.CallMethod("RequestedNodes", new object[]{});
            jobRef.Username = GetUserName();
            jobRef.PathToDll = args[0];
            jobRef.FileName = Path.GetFileName(args[0]);
            Console.WriteLine(jobRef.FileName);
            jobRef.UserArgs = userArgs;
            // call the initial method
            loader.CallMethod("RunInitialTask", new object[]{});
            var sj = new SubmitJob(_ipAddr, NetworkSendReceive.ServerPort, jobRef);
            Console.CancelKeyPress += sj.OnUserExit;
        }

        /// <summary>
        /// Gets the local IP address
        /// </summary>
        public static async Task GetLocalIpAddress()
        {
            string localIp = "";
            var host = await Dns.GetHostEntryAsync(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                }
            }
            _ipAddr = localIp;
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
                var error = e.ToString();
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
            Logger.Log("Submitjob - Shuting down, user hit ctrl-c");
            // set cancel to true so we can do our own cleanup
            e.Cancel = true;
            // queue up the quitting message to the server
            _proxy.QueueDataEvent(new JobEventArgs("quit"));
            // join on both sending and receiving threads
            _proxy.Join();

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
            MessageMap.TryGetValue(Args[0], out m);
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
        private readonly ConcurrentQueue<JobEventArgs> _messageQueue = new ConcurrentQueue<JobEventArgs>();

        private readonly JobRef _job;
        private CoreLoader<Job> _loader;
        private SubmitJob _parent;

        public JobSender(JobRef job, SubmitJob parent)
        {
            _parent = parent;
            _job = job;
            _loader = new CoreLoader<Job>(_job.PathToDll);
        }

        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            _messageQueue.Enqueue(e as JobEventArgs);
        }

        public override void Run()
        {
            int count = 0;
            while (!DoneSending)
            {
                JobEventArgs data;
                if (_messageQueue.TryDequeue(out data))
                {
                    _parent.Logger.Log("Job Sending Message");
                    switch (data.Protocol)
                    {
                        case JobEventArgs.MessageType.Id:
                            _parent.Logger.Log("Sending Job Id");
                            SendMessage(new string[] { "job" });
                            break;
                        case JobEventArgs.MessageType.Submit:
                            _parent.Logger.Log("Sending Job");
                            // job in this context corresponds to job manager.
                            var args = new List<string> {"job", JsonConvert.SerializeObject(_job)};
                            SendMessage(args.ToArray());

                            break;
                        case JobEventArgs.MessageType.Results:
                            var j = JArray.Parse(data.Args[1]);
                            var userOut = JsonConvert.DeserializeObject<UserOutput>(j[0].ToString());
                            _parent.Logger.Log(userOut.ConsoleOutput);

                            if (j.Count > 1)
                            {
                                for (var i = 1; i < j.Count; i++)
                                {
                                    var result = JobResult.DeserializeString(j[i].ToString());
                                    _loader.CallMethod("AddResult", new object[] {result});
                                }
                            }
                            count++;
                            if (count == _job.RequestedNodes)
                            {
                                _loader.CallMethod("RunFinalTask", new object[] { });
                                _parent.Logger.Log("Done");
                                Proxy.QueueDataEvent(new JobEventArgs("shutdown"));
                                Environment.Exit(0);
                            }
                            break;
                        case JobEventArgs.MessageType.Shutdown:
                            DoneSending = true;
                            break;
                        case JobEventArgs.MessageType.Quit:
                            SendMessage(new string[] { "connectionquit" });
                            // hack to get final message to actually send...
                            Task t = new Task(FlushSender);
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
            await Proxy.IOStream.FlushAsync();
        }
    }
}

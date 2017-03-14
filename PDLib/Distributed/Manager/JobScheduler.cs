using System.Collections.Generic;
using System.IO;
using System.Threading;

using Distributed.Network;

namespace Distributed.Manager
{
    /// <summary>
    /// A thread safe Job Scheduler
    /// </summary>
    class JobScheduler
    {
        // Could use a concurrent dictionary but other variables
        // such as count need a lock anyway.
        private Dictionary<string, DataReceivedEventArgs> Jobs;
        public int JobCount { get; private set; }
        // lock object for thread safe operations
        private object JobLock = new object();
        private bool DoneScheduling;
        private NodeManager manager;
        private Queue<JobRef> JobQueue;
        private Thread thread;

        public JobScheduler(NodeManager m)
        {
            JobCount = 0;
            Jobs = new Dictionary<string, DataReceivedEventArgs>();
            JobQueue = new Queue<JobRef>();
            DoneScheduling = false;
            manager = m;
        }

        public void Start()
        {
            thread = new Thread(Run);
            thread.Start();
        }

        public void Stop()
        {
            DoneScheduling = true;
            thread.Join();
        }

        public void Run()
        {
            while(!DoneScheduling)
            {
                if (AvailableResources())
                {
                    manager.SendJobOut(JobQueue.Dequeue());
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        private bool AvailableResources()
        {
            if (JobQueue.Count > 0 &&
                manager.GetAvailableNodes() >= JobQueue.Peek().RequestedNodes)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Add a job based on the file name, and arguments
        /// contained in the DataReceivedEventArgs
        /// </summary>
        /// <param name="job"> job to add</param>
        public void AddJob(DataReceivedEventArgs job)
        {
            lock(JobLock)
            {
                // debug
                manager.log.Log("Adding job: " + Path.GetFileName(job.args[1]));
                JobQueue.Enqueue(new JobRef(job));
                string filename = Path.GetFileName(job.args[1]);
                if (Jobs.ContainsKey(filename))
                {
                    // TODO, need to add something more specific to this
                    Jobs.Remove(filename);
                }
                Jobs.Add(filename, job);
                JobCount++;
            }
        }
        
        /// <summary>
        /// Get the job based on the filename
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public bool GetJob(string jobName, 
            out DataReceivedEventArgs d)
        {
            lock(JobLock)
            {
                return Jobs.TryGetValue(jobName, out d);
            }
        }
    }

    class JobRef
    {
        public int RequestedNodes { get; private set; }
        public string PathToDll { get; private set; }
        public string[] UserArgs { get; private set; }

        /// <summary>
        /// Construct a Job Reference
        /// </summary>
        /// <param name="data"></param>
        public JobRef(DataReceivedEventArgs data)
        {
            ParseJobMessage(data);
            RequestedNodes = 1;
        }

        /// <summary>
        /// Parse the message containing
        /// information about the job.
        /// </summary>
        /// <remarks>
        /// looks like:
        ///     job|[path to dll]|[user args ...]|end
        /// </remarks>
        /// <param name="data"></param>
        private void ParseJobMessage(DataReceivedEventArgs data)
        {
            PathToDll = data.args[1];
            UserArgs = new string[data.args.Length - 3];
            for (int i = 0; i < data.args.Length - 3; i++)
            {
                UserArgs[i] = data.args[i + 2];
            }
        }
    }
}

using System.Collections.Generic;
using System.Threading;

using Defcore.Distributed.Network;
using System;

namespace Defcore.Distributed.Manager
{
    /// <summary>
    /// A thread safe Job Scheduler
    /// </summary>
    internal class JobScheduler
    {
        // manager associated with JobScheduler
        private NodeManager manager;

        // Could use a concurrent dictionary but other variables
        // need a lock anyway, keep track of users and their jobs
        private Dictionary<string, List<JobRef>> Jobs;
        // the queue for waiting jobs, TODO: maybe make priority queue instead
        private Queue<JobRef> JobQueue;
        
        public int JobCount { get; private set; }

        // lock object for thread safe operations
        private object JobLock = new object();

        // keeps the scheduler alive
        private bool DoneScheduling;

        // Thread that acts as the independent scheudler
        private Thread thread;
        // id tracker for jobs
        private static int id;

        public JobScheduler(NodeManager m)
        {
            JobCount = 0;
            Jobs = new Dictionary<string, List<JobRef>>();
            JobQueue = new Queue<JobRef>();
            DoneScheduling = false;
            manager = m;
            id = 1;
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
                    lock (JobLock)
                    {
                        manager.SendJobOut(JobQueue.Dequeue());
                    }
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        /// <summary>
        /// Check if there are resources availble
        /// to run another job. Meaning the number
        /// of nodes requested is less than or equal to
        /// the number of availble nodes.
        /// </summary>
        /// <remarks>
        /// Requested nodes is capped at total nodes
        /// available.
        /// </remarks>
        /// <returns></returns>
        private bool AvailableResources()
        {
            // if we have at least one job
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
        /// <param name="proxy_id">who the job is for</param>
        public void AddJob(DataReceivedEventArgs job, int proxy_id)
        {
            lock(JobLock)
            {
                JobRef j = new JobRef(job, id++, proxy_id);
                // debug
                manager.log.Log("Adding job: " + j);
                // add the job to the queue
                JobQueue.Enqueue(j);
                // keep track of users and their job(s)
                // if a user has already submitted a job before
                if (Jobs.ContainsKey(j.Username))
                { 
                    // add this job to their list
                    Jobs[j.Username].Add(j);
                }
                else
                {
                    List<JobRef> jl = new List<JobRef>();
                    jl.Add(j);
                    Jobs.Add(j.Username, jl);
                }
                
                JobCount++;
            }
        }

        public JobRef GetJob(int job_id)
        {
            foreach(List<JobRef> jobs in Jobs.Values )
            {
                foreach(JobRef job in jobs)
                {
                    if (job.JobId == job_id)
                    {
                        return job;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// A class for a reference to a job
    /// </summary>
    internal class JobRef
    {
        // how many nodes did this job request
        public int RequestedNodes { get; private set; }
        // path to the dll on the users system
        public string PathToDll { get; private set; }
        // command line arguments the user passed
        public string[] UserArgs { get; private set; }
        // username associated with job
        public string Username { get; private set; }
        // the jobs id (unique)
        public int JobId;

        public int ProxyId;

        // TODO might want an Enum status instead
        private bool completed = false;

        /// <summary>
        /// Construct a Job Reference
        /// </summary>
        /// <param name="data"></param>
        public JobRef(DataReceivedEventArgs data, 
            int id, int proxy_id)
        {
            ParseJobMessage(data);
            JobId = id;
            ProxyId = proxy_id;
        }

        /// <summary>
        /// Parse the message containing
        /// information about the job.
        /// </summary>
        /// <remarks>
        /// looks like:
        ///     job|[username]|[path to dll]|[user args ...]|end
        /// </remarks>
        /// <param name="data"></param>
        private void ParseJobMessage(DataReceivedEventArgs data)
        {
            Username = data.args[1];
            PathToDll = data.args[2];
            RequestedNodes = int.Parse(data.args[3]);
            if (RequestedNodes > 4)
            {
                RequestedNodes = 4;
            }
            Console.WriteLine("Requested = ");
            UserArgs = new string[data.args.Length - 5];
            for (int i = 0; i < data.args.Length - 5; i++)
            {
                UserArgs[i] = data.args[i + 4];
            }
        }

        /// <summary>
        /// Overridden to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Job: " + JobId + " for user: " + Username;
        }
    }
}

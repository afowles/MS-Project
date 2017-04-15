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
        /// <param name="proxyId">who the job is for</param>
        public void AddJob(JobRef job, int proxyId)
        {
            lock(JobLock)
            {
                //JobRef j = new JobRef(job, id++, proxy_id);
                // debug
                job.JobId = id++;
                job.ProxyId = proxyId;
                manager.log.Log("Adding job: " + job);
                // add the job to the queue
                JobQueue.Enqueue(job);
                // keep track of users and their job(s)
                // if a user has already submitted a job before
                if (Jobs.ContainsKey(job.Username))
                { 
                    // add this job to their list
                    Jobs[job.Username].Add(job);
                }
                else
                {
                    var jl = new List<JobRef> {job};
                    Jobs.Add(job.Username, jl);
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
}

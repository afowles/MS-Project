using System.Collections.Generic;
using System.Threading;


namespace Defcore.Distributed.Manager
{
    /// <summary>
    /// A thread safe Job Scheduler
    /// </summary>
    internal class JobScheduler
    {
        // manager associated with JobScheduler
        private readonly NodeManager _manager;
        // Could use a concurrent dictionary but other variables
        // need a lock anyway, keep track of users and their jobs
        private readonly Dictionary<string, List<JobRef>> _jobs;
        // the queue for waiting jobs, TODO: maybe make priority queue instead
        private readonly Queue<JobRef> _jobQueue;
        
        public int JobCount { get; private set; }

        // lock object for thread safe operations
        private readonly object _jobLock = new object();

        // keeps the scheduler alive
        private bool _doneScheduling;

        // Thread that acts as the independent scheudler
        private Thread _thread;
        // id tracker for jobs
        private static int _id;

        public JobScheduler(NodeManager m)
        {
            JobCount = 0;
            _jobs = new Dictionary<string, List<JobRef>>();
            _jobQueue = new Queue<JobRef>();
            _doneScheduling = false;
            _manager = m;
            _id = 1;
        }

        public void Start()
        {
            _thread = new Thread(Run);
            _thread.Start();
        }

        public void Stop()
        {
            _doneScheduling = true;
            _thread.Join();
        }

        public void Run()
        {
            while(!_doneScheduling)
            {
                if (AvailableResources())
                {
                    lock (_jobLock)
                    {
                        _manager.SendJobOut(_jobQueue.Dequeue());
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
            if (_jobQueue.Count > 0 &&
                _manager.GetAvailableNodes() >= _jobQueue.Peek().RequestedNodes)
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
            lock(_jobLock)
            {
                //JobRef j = new JobRef(job, id++, proxy_id);
                // debug
                job.JobId = _id++;
                job.ProxyId = proxyId;
                _manager.Logger.Log("Adding job: " + job);
                // add the job to the queue
                _jobQueue.Enqueue(job);
                // keep track of users and their job(s)
                // if a user has already submitted a job before
                if (_jobs.ContainsKey(job.Username))
                { 
                    // add this job to their list
                    _jobs[job.Username].Add(job);
                }
                else
                {
                    var jl = new List<JobRef> {job};
                    _jobs.Add(job.Username, jl);
                }
                
                JobCount++;
            }
        }

        public JobRef GetJob(int jobId)
        {
            foreach(List<JobRef> jobs in _jobs.Values )
            {
                foreach(JobRef job in jobs)
                {
                    if (job.JobId == jobId)
                    {
                        return job;
                    }
                }
            }
            return null;
        }
    }
}

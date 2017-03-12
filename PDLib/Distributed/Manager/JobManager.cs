using Distributed.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Distributed.Manager
{
    /// <summary>
    /// A thread safe Manager of Jobs
    /// </summary>
    class JobManager
    {
        // Could use a concurrent dictionary but other variables
        // such as count need a lock anyway.
        private Dictionary<string, DataReceivedEventArgs> Jobs;
        public int JobCount { get; private set; }
        // lock object for thread safe operations
        private object JobLock = new object();
        private bool done;
        private NodeManager manager;

        public JobManager(NodeManager m)
        {
            JobCount = 0;
            Jobs = new Dictionary<string, DataReceivedEventArgs>();
            done = false;
            manager = m;
        }

        public void run()
        {
            while(!done)
            {
                if (QueryJobs())
                {

                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        private bool QueryJobs()
        {
            
            return true;
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
                Console.WriteLine("Adding job: " + Path.GetFileName(job.args[1]));
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
}

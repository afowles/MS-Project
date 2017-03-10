using Distributed.Network;
using System.Collections.Generic;
using System.IO;

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

        public JobManager()
        {
            JobCount = 0;
            Jobs = new Dictionary<string, DataReceivedEventArgs>();
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
                Jobs.Add(Path.GetFileName(job.args[1]), job);
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

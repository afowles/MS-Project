using System;
using System.Collections.Generic;

namespace Defcore.Distributed.Jobs
{
    public abstract class JobTask
    {
        public List<JobResult> TaskResults { get; }

        protected JobTask()
        {
            TaskResults = new List<JobResult>();
        }

        public void AddResult(JobResult j)
        {
            TaskResults.Add(j);
        }

        public abstract void Main(string[] args);

    }
}

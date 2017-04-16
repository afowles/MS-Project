using System.Collections.Generic;

namespace Defcore.Distributed.Jobs
{
    /// <summary>
    /// A JobTask is a unit of work performed on some node
    /// JobTasks are setup by a job and then executed on some
    /// number of nodes based on Job schedule.
    /// </summary>
    public abstract class JobTask
    {
        /// <summary>
        /// The tasks list of results
        /// </summary>
        public List<JobResult> TaskResults { get; }

        /// <summary>
        /// Empty constructor, initializes job result list
        /// </summary>
        protected JobTask()
        {
            TaskResults = new List<JobResult>();
        }

        /// <summary>
        /// Add a result
        /// </summary>
        /// <param name="j"></param>
        public void AddResult(JobResult j)
        {
            TaskResults.Add(j);
        }

        /// <summary>
        /// Main method is what is called out of each task
        /// when the job executes.
        /// </summary>
        /// <param name="args"></param>
        public abstract void Main(string[] args);

    }
}

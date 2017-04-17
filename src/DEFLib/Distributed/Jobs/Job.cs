using System.Collections.Generic;

namespace Defcore.Distributed.Jobs
{
    /// <summary>
    /// The abstract class that allows a user program to
    /// operate within the distributed system setup by defcore.
    /// Users must extend this class within their program exactly once.
    /// </summary>
    public abstract class Job
    {
        protected Schedule TaskSchedule;
        private readonly List<JobTask> _tasks = new List<JobTask>();

        public List<JobResult> Results { get; }

        /// <summary>
        /// The number of nodes a user wishes to request for their program
        /// This number will default to the total number of nodes 
        /// present if too large a number is provided.
        /// Defaults to 1.
        /// </summary>
        public int RequestedNodes { get; set; }

        /// <summary>
        /// Empty constructor for a job
        /// </summary>
        protected Job()
        {
            TaskSchedule = Schedule.Fixed;
            Results = new List<JobResult>();
            RequestedNodes = 1;
        }

        /// <summary>
        /// The main program to be run to setup some number
        /// of task programs. It is advised that large computations
        /// are not executed within here but rather the job task objects.
        /// </summary>
        /// <param name="args"></param>
        public abstract void Main(string[] args);
        
        /// <summary>
        /// Add a JobTask to the list to be executed.
        /// </summary>
        /// <param name="task">The jobtask to add</param>
        public void AddTask(JobTask task)
        {
            _tasks.Add(task);
        }

        /// <summary>
        /// Add a JobResult to the list of results
        /// </summary>
        /// <param name="result">The job result</param>
        public void AddResult(JobResult result)
        {
            Results.Add(result);
        }

        /// <summary>
        /// Get the actual class name of this class.
        /// </summary>
        /// <returns></returns>
        public string GetClassName()
        {
            return GetType().Name;
        }

        /// <summary>
        /// Start a job task at the given index
        /// </summary>
        /// <param name="index">index of the jobtask 
        /// within the list of tasks</param>
        public void StartTask(int index)
        {
            if (_tasks.Count > index)
            {
                _tasks[index].Main(new[] {index.ToString()});
            }
        }

        /// <summary>
        /// Add all the results from a task to this Jobs
        /// result list.
        /// </summary>
        public void CompileResults()
        {
            foreach (var t in _tasks)
            {
                Results.AddRange(t.TaskResults);
            }
        }

        /// <summary>
        /// Get the nuber of tasks setup
        /// </summary>
        /// <returns></returns>
        public int GetNumberTasks() { return _tasks.Count; }

        /// <summary>
        /// Get the task execution schedule
        /// </summary>
        /// <returns></returns>
        public Schedule GetSchedule() { return TaskSchedule; }

        /// <summary>
        /// Run a final task at the end, on the node that
        /// submitted the job.
        /// </summary>
        /// <remarks>This method will be called once all nodes
        /// have finished and will be run on the node that submitted
        /// the job.</remarks>
        public virtual void RunFinalTask(){}

        /// <summary>
        /// Run a section of code on exactly one node
        /// </summary>
        /// <remarks>This method will be called once by only one node</remarks>
        public virtual void RunOnce() {}
    }

    /// <summary>
    /// The schedule to execute their jobs
    /// </summary>
    public enum Schedule
    {
        Fixed,
        Leapfrog
    }
}

using System.Collections.Generic;

namespace Defcore.Distributed.Jobs
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class Job
    {
        protected Schedule TaskSchedule;
        private readonly List<JobTask> _tasks = new List<JobTask>();
        public List<JobResult> Results { get; }

        public int RequestedNodes { get; set; }

        protected Job()
        {
            TaskSchedule = Schedule.FIXED;
            Results = new List<JobResult>();
            RequestedNodes = 1;
        }

        public abstract void Main(string[] args);
        
        public void AddTask(JobTask task)
        {
            _tasks.Add(task);
        }

        public void AddResult(JobResult result)
        {
            Results.Add(result);
        }

        public string GetClassName()
        {
            return this.GetType().Name;
        }
        public void StartTask(int index)
        {
            if (_tasks.Count > index)
            {
                _tasks[index].Main(new string[] {index.ToString()});
            }
        }

        public void CompileResults()
        {
            foreach (var t in _tasks)
            {
                Results.AddRange(t.TaskResults);
            }
        }

        public int GetNumberTasks() { return _tasks.Count; }
        public Schedule GetSchedule() { return TaskSchedule; }
    }

    public enum Schedule
    {
        FIXED,
        LEAPFROG
    }
}

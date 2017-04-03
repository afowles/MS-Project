using System.Collections.Generic;

namespace Distributed.Library
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class Job
    {
        protected Schedule TaskSchedule;

        protected Job()
        {
            TaskSchedule = Schedule.FIXED;
        }

        private readonly List<JobTask> _tasks = new List<JobTask>();

        public abstract void Main(string[] args);
        
        public void AddTask(JobTask task)
        {
            _tasks.Add(task);
        }
        public string GetClassName()
        {
            return this.GetType().Name;
        }
        public void StartTask(int index)
        {
            _tasks[index].Main(new string[] { index.ToString() });
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

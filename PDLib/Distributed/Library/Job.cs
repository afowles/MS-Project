using System;
using System.Collections.Generic;

using Distributed.Network;


namespace Distributed.Library
{
    public abstract class Job
    {
        protected Schedule TaskSchedule;

        public Job()
        {

        }

        private List<JobTask> tasks = new List<JobTask>();

        public abstract void Main(string[] args);
        
        public void AddTask(JobTask task)
        {
            tasks.Add(task);
        }
        public string GetClassName()
        {
            return this.GetType().Name;
        }
        public void startTask(int index)
        {
            tasks[index].Main(new string[] { index.ToString() });
        }

        public int GetNumberTasks() { return tasks.Count; }

        // User will override if they want a different schedule
        public Schedule GetSchedule() { return Schedule.FIXED; }
    }

    public enum Schedule
    {
        FIXED,
        LEAPFROG
    }
}

using System;
using System.Collections.Generic;

using Distributed.Network;


namespace Distributed
{
    public abstract class Job
    {
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
        //public int GetNumberTasks() { return tasks.Count; }
    }


}

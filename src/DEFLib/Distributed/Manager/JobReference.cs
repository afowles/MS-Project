using System;
using System.Collections.Generic;
using System.Text;
using Defcore.Distributed.Network;

namespace Defcore.Distributed.Manager
{
    public class JobRef
    {
        // how many nodes did this job request
        public int RequestedNodes { get; set; }
        // path to the dll on the users system
        public string PathToDll { get; set; }
        // command line arguments the user passed
        public string[] UserArgs { get; set; }
        // username associated with job
        public string Username { get; set; }
        // the jobs id (unique)
        public int JobId { get; set; }

        public int ProxyId;

        // TODO might want an Enum status instead
        private bool completed = false;

        // Node state identifiers
        public int SectionId { get; set; }
        public int TotalNodes { get; set; }

        /// <summary>
        /// Overridden to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Job: " + JobId + " for user: " + Username;
        }
    }
}

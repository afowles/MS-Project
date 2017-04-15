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

        /// <summary>
        /// Construct a Job Reference
        /// </summary>
        /// <param name="data"></param>
        public JobRef()
        {
            //ParseJobMessage(data);
            //JobId = id;
            //ProxyId = proxy_id;
        }

        /// <summary>
        /// Parse the message containing
        /// information about the job.
        /// </summary>
        /// <remarks>
        /// looks like:
        ///     job|[username]|[path to dll]|[user args ...]|end
        /// </remarks>
        /// <param name="data"></param>
        private void ParseJobMessage(DataReceivedEventArgs data)
        {
            Username = data.args[1];
            PathToDll = data.args[2];
            RequestedNodes = int.Parse(data.args[3]);
            if (RequestedNodes > 4)
            {
                RequestedNodes = 4;
            }
            Console.WriteLine("Requested = ");
            UserArgs = new string[data.args.Length - 5];
            for (int i = 0; i < data.args.Length - 5; i++)
            {
                UserArgs[i] = data.args[i + 4];
            }
        }

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

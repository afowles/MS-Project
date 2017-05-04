
namespace Defcore.Distributed.Manager
{
    /// <summary>
    /// A reference to a job, contains all the information
    /// related to a job object.
    /// </summary>
    public class JobRef
    {
        /// <summary>
        /// How many nodes did the user program request
        /// </summary>
        public int RequestedNodes { get; set; }
        /// <summary>
        /// The full path to the users assembly on their system 
        /// </summary>
        public string PathToDll { get; set; }
        /// <summary>
        /// The users command line arguments to their program
        /// </summary>
        public string[] UserArgs { get; set; }
        /// <summary>
        /// The username associated with the job
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// The job id
        /// </summary>
        public int JobId { get; set; }


        public string FileName { get; set; }

        /// <summary>
        /// The proxy id
        /// </summary>
        public int ProxyId;

        // TODO might want an Enum status instead
        //private bool completed = false;

        /// <summary>
        /// The section id for the node executing some
        /// section of work. Set before runtime
        /// </summary>
        public int SectionId { get; set; }
        /// <summary>
        /// The total nodes in use for this job.
        /// Set before runtime.
        /// </summary>
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

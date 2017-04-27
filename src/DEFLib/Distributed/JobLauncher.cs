using Defcore.Distributed.Nodes;
using System;
using System.IO;
using System.Text;
using Defcore.Distributed.Manager;

namespace Defcore.Distributed
{
    /// <summary>
    /// Class to launch a job
    /// </summary>
    internal class JobLauncher
    {
        private readonly string _dllName;
        private readonly JobRef _job;
        private readonly Node _parent;

        /// <summary>
        /// Constructor for a Job launcher
        /// </summary>
        /// <param name="job">the job to be launched</param>
        /// <param name="p">the parent node</param>
        public JobLauncher(JobRef job, Node p)
        {
            _job = job;
            _dllName = Path.GetFileName(_job.PathToDll);
            _parent = p;
        }

        /// <summary>
        /// Launch a job by creating a new process
        /// that user the loaders and executes the users dll
        /// </summary>
        public void LaunchJob()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            // what is actually run is the dotnet process with
            // the loader application loading their dll which
            // is located in the StartNode directory since that is where
            // it was read in.
            // argumnets to donet run
            // redirect standard output so we can send it back
            // redirect standard in, not using currently
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = System.Reflection.Assembly.GetEntryAssembly().Location + " load " + _dllName,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                // run in the loader directory
                WorkingDirectory = ".",
                CreateNoWindow = true
            };
            // give it the arguments required to section up work
            processStartInfo.Arguments += " " + _job.JobId + "," + _job.SectionId + "," + _job.TotalNodes;

            // Add in the user arguments
            foreach (var arg in _job.UserArgs)
            {
                processStartInfo.Arguments += " " + arg;
            }
            
            //processStartInfo.UseShellExecute = false;
            // setup the process info
            process.StartInfo = processStartInfo;
            // setup and allow an event
            process.EnableRaisingEvents = true;
            // for building output from standard out
            var outputBuilder = new StringBuilder();
            // add event handling for process
            process.OutputDataReceived += delegate (object sender, System.Diagnostics.DataReceivedEventArgs e)
            {
                outputBuilder.Append(e.Data);
            };
            try
            {
                // start
                process.Start();
                // read is async
                process.BeginOutputReadLine();
                // wait for user process to end
                process.WaitForExit();
                process.CancelOutputRead();
            }
            // if there was an error with the loader process or building the loader
            // internal user exceptions will be caught by the loader programs
            catch(Exception e)
            {
                var error = e.ToString();
                Console.WriteLine(error);
            }
            _parent.Logger.Log("JobLauncher: Process finished with ExitCode: " + process.ExitCode);
            // use the output
            var output = outputBuilder.ToString();

            _parent.QueueDataEvent(new NodeComm("finished|" 
                + _job.JobId + "|" + output));
        }
    }
}

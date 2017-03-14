using Distributed.Network;
using System;
using System.Text;

namespace Distributed.Manager
{
    /// <summary>
    /// Class to launch a job
    /// </summary>
    class JobLauncher
    {
        private string dllPath;
        private DataReceivedEventArgs data;

        /// <summary>
        /// Constructor for a Job launcher
        /// </summary>
        /// <param name="d"></param>
        public JobLauncher(DataReceivedEventArgs d)
        {
            dllPath = d.args[1];
            data = d;
        }

        /// <summary>
        /// Launch a job by creating a new process
        /// that user the loaders and executes the users dll
        /// </summary>
        public void LaunchJob()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo();
            // what is actually run is the dotnet process with
            // the loader application loading their dll which
            // is located in the StartNode directory since that is where
            // it was read in.
            processStartInfo.FileName = "dotnet";
            // argumnets to donet run
            processStartInfo.Arguments = "run ../StartNode/" + dllPath;
            // redirect standard output so we can send it back
            processStartInfo.RedirectStandardOutput = true;
            // redirect standard in, not using currently
            processStartInfo.RedirectStandardInput = true;

            // Add in the user arguments
            // arg[0] is a message type, arg[1] is the dll path
            for (int i = 2; i < data.args.Length; i++ )
            {
                processStartInfo.Arguments += " " + data.args[i];
            }
            // run in the loader directory
            processStartInfo.WorkingDirectory = "../Loader/";

            // cmd line only
            processStartInfo.CreateNoWindow = true;
            //processStartInfo.UseShellExecute = false;
            // setup the process info
            process.StartInfo = processStartInfo;
            // setup and allow an event
            process.EnableRaisingEvents = true;
            // for building output from standard out
            StringBuilder outputBuilder = new StringBuilder(); ;
            // add event handling for process
            process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler
            (
                delegate (object sender, System.Diagnostics.DataReceivedEventArgs e)
                {
                    // append the output data to a string
                    outputBuilder.Append(e.Data);
                    
                }
            );
            string error;
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
                error = e.ToString();
            }

            // use the output
            string output = outputBuilder.ToString();
            
            Console.WriteLine("Output finsihed!:");
            Console.WriteLine(output);

        }
    }
}

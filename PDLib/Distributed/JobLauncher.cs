using Distributed.Network;

namespace Distributed
{
    class JobLauncher
    {
        private string dllPath;
        private DataReceivedEventArgs data;

        public JobLauncher(DataReceivedEventArgs d)
        {
            dllPath = d.args[1];
            data = d;
        }

        /// <summary>
        /// Launch a job by creating a new process
        /// that loaders and executes the users dll
        /// </summary>
        public void LaunchJob()
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "dotnet";
            p.StartInfo.Arguments = "run ../StartNode/" + dllPath;
            for (int i = 2; i < data.args.Length; i++ )
            {
                p.StartInfo.Arguments += " " + data.args[i];
            }
            p.StartInfo.WorkingDirectory = "../Loader/";
            p.Start();
            p.WaitForExit();
        }
    }
}

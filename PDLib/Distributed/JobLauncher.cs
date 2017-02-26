using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Distributed
{
    class JobLauncher
    {
        string dllPath;
        public JobLauncher(string dllPath)
        {
            this.dllPath = dllPath;
        }

        public void LaunchJob()
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "dotnet";
            p.StartInfo.Arguments = "run ../StartNode/" + dllPath;
            p.StartInfo.WorkingDirectory = "../Loader/";
            p.Start();
            p.WaitForExit();
        }
    }
}

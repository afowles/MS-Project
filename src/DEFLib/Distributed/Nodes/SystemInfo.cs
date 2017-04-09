using System;


namespace Defcore.Distributed.Nodes
{
    class NodeSystemInfo
    {
        public int Cores
        {
            get
            {
                return Environment.ProcessorCount;
            }
            private set { }
        }
        public string OS
        {
            get
            {
                //return Environment.OSVersion.VersionString;
                // TODO: figure out this for .NET core
                return "";
            }
        }

        public int RunningJobs { get; private set; }

        public NodeSystemInfo()
        {
            
        }

        public override string ToString()
        {
            return "Operating System: " + OS + Environment.NewLine +
            "Number of Cores: " + Cores;
        }

        public static void Main(string[] args)
        {
            
        }

    }
}

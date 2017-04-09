using System;
using System.Reflection;
using System.Runtime.Loader;
using Defcore.Distributed.Jobs;
using System.Diagnostics;
using System.IO;

using Defcore.Distributed.Assembly;

/// <summary>
/// Loader test app, this will be migrated into 
/// </summary>
namespace Loader
{
    /// <summary>
    /// Class to load a user program in and run it.
    /// </summary>
    public class Loader
    {
        public static void Main(string[] args)
        {
            JobExecuter.Main(args);
        }
    }
}

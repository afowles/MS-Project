using System;
using System.Reflection;

namespace Distributed.Assembly
{
    /// <summary>
    /// An interface to an assembly loader
    /// </summary>
    /// <remarks>Loading assembly in .NET framework vs .NET Core
    /// is a different task completely, supporting both frameworks
    /// now requires different implementations. See Loader.cs in PDLib_Core</remarks>
    interface ILoader
    {
    }

    public class Loader : ILoader
    {
        public Loader(string assemlyPath)
        {
            
        }
    }
}

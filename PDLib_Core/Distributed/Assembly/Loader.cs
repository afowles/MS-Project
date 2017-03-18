using System;
using System.Reflection;
using System.Runtime.Loader;

using Distributed.Library;

namespace Distributed.Assembly
{
    /// <summary>
    /// 
    /// </summary>
    public class CoreLoader : ILoader
    {
        private System.Reflection.Assembly assembly;
        private Type JobClassType;
        private object JobInstance;

        /// <summary>
        /// Constructor for the CoreLoader
        /// </summary>
        /// <param name="assemblyPath">path to the assembly (dll)</param>
        public CoreLoader(string assemblyPath)
        {
            assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);  
        }

        /// <summary>
        /// Find a job class from an assembly,
        /// if that job class does not exist in the assembly
        /// return false.
        /// </summary>
        public bool FindJobClass()
        {
            Type[] types;
            // try and grab all the types from
            // an assembly. It is possible that
            // a load error will occur, if that happens
            // a work around is taking the types from the
            // exception itself.
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            // loop through all the types
            // in the assmebly and try and find the job
            // class
            foreach (Type t in types)
            {
                
                try
                {
                    var instance = Activator.CreateInstance(t);
                    if (instance is Job)
                    {
                        JobClassType = t;
                        JobInstance = instance;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Not a job");
                    }
                }
                catch(MissingMemberException e)
                {
                    Console.WriteLine("SHIT: this is" + e.ToString());
                }
            }
            return false;
        }

        /// <summary>
        /// Call a method with the specified params
        /// for an object of the Job Class
        /// </summary>
        /// <param name="method"></param>
        /// <param name="methodParams"></param>
        public object CallMethod(string method, object[] methodParams)
        {
            MethodInfo methodInfo = JobClassType.GetMethod(method);
            Console.WriteLine("gets here");
            if (methodInfo != null)
            {
                object result = null;
                ParameterInfo[] parameters = methodInfo.GetParameters();
                Console.WriteLine("gets here");
                result = methodInfo.Invoke(JobInstance, methodParams);
                Console.WriteLine(result);
                return result;
            }
            else
            {
                Console.WriteLine("Error with method load");
                return null;
            }
        }
    }
}

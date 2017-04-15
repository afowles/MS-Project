using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Defcore.Distributed.Assembly
{
    /// <summary>
    /// CoreLoader loads an assembly and creates an instance
    /// from which to call methods based on the generic type T.
    /// </summary>
    /// <typeparam name="T">The type to create an instance for</typeparam>
    public class CoreLoader<T>
    {
        private readonly System.Reflection.Assembly _assembly;
        private T _instance;

        /// <summary>
        /// Create and load the assembly with the given path
        /// </summary>
        /// <remarks>The full path to the assembly must be provided</remarks>
        /// <param name="assemblyPath">Full path to the assembly (dll)</param>
        /// <exception cref="TypeLoadException">If the generic type could not be found</exception>
        public CoreLoader(string assemblyPath)
        {
            _assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            if (!FindClassType())
            {
                throw (new TypeLoadException());
            }
        }

        
        /// <summary>
        /// Find a class from an assembly and create an instance
        /// of it. If that job class does not exist in the assembly
        /// return false.
        /// </summary>
        private bool FindClassType()
        {
            Type[] types;
            // Try and grab all the types from
            // an assembly. It is possible that
            // a load error will occur, if that happens
            // a work around is taking the types from the
            // exception itself.
            try
            {
                types = _assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            // Loop through all the types in the
            // assmebly and try and find the job class
            foreach (var t in types)
            {
                try
                {
                    // create an instance of type to try
                    var instance = Activator.CreateInstance(t);
                    T tempType = (T)instance;
                    if (tempType != null)
                    {
                        _instance = tempType;
                        return true;
                    }
                    else
                    {
                        //Console.WriteLine("Not a job");
                    }
                }
                catch(MissingMemberException) { }
                catch(InvalidCastException) { }
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
            
            MethodInfo methodInfo = _instance.GetType().GetMethod(method);
            //Console.WriteLine("gets here");
            if (methodInfo != null)
            {
                object result = null;
                ParameterInfo[] parameters = methodInfo.GetParameters();
                //Console.WriteLine("gets here");
                result = methodInfo.Invoke(_instance, methodParams);
                //Console.WriteLine(result);
                return result;
            }
            else
            {
                Console.WriteLine("Error with method load");
                return null;
            }
        }

        /// <summary>
        /// Get a property from the job object
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public object GetProperty(string property)
        {
            object result = null;
            try
            {
                result = typeof(T).GetProperty(property).GetValue(_instance, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return result;
        }
    }
}

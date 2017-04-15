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
        /// <exception cref="TypeLoadException">Thrown if the generic type could not be found</exception>
        public CoreLoader(string assemblyPath)
        {
            _assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            if (!FindClassType())
            {
                throw (new TypeLoadException("Could not load type: " + typeof(T)));
            }
        }

        
        /// <summary>
        /// Finds the generic class from an assembly and create an instance
        /// of it. If that class does not exist in the assembly
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
            // assmebly and try and find the generic class
            foreach (var t in types)
            {
                try
                {
                    // create an instance of type to try
                    var instance = Activator.CreateInstance(t);
                    var tempType = (T)instance;
                    if (tempType == null) {continue;}
                    _instance = tempType;
                    return true;
                }
                catch(MissingMemberException) { }
                catch(InvalidCastException) { }
            }
            return false;
        }

        /// <summary>
        /// Call a method with the specified params
        /// for an object of the generic class
        /// </summary>
        /// <param name="method">the method name</param>
        /// <param name="methodParams">an array of object which 
        /// contains that methods arguments parameters</param>
        /// <exception cref="TargetException">If something goes 
        /// wrong invoking the method</exception>
        public object CallMethod(string method, object[] methodParams)
        {
            
            var methodInfo = _instance.GetType().GetMethod(method);
            var result = methodInfo?.Invoke(_instance, methodParams);
            return result;
        }

        /// <summary>
        /// Get a property from the generic object
        /// </summary>
        /// <param name="property">the properties name</param>
        /// <returns>the property as an object</returns>
        public object GetProperty(string property)
        {
            var result = typeof(T).GetProperty(property).GetValue(_instance, null);
            return result;
        }
    }
}

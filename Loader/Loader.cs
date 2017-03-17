using System;
using System.Reflection;
using System.Runtime.Loader;
using Distributed.Library;
using System.Diagnostics;
using System.IO;

using Distributed.Assembly;
/// <summary>
/// Loader test app, this will be migrated into 
/// </summary>
namespace Loader
{
    public class Loader
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Got here with arguments: ");
            foreach(string so in args)
            {
                Console.WriteLine(so);
            }
            string s = Directory.GetCurrentDirectory();
            Console.WriteLine(s + "\\" + args[0]);
            // Create an assembly loader object
            CoreLoader c = new CoreLoader(s + "\\" + args[0]);
            // Find the Job Class
            int[] n = ParseTokens(args);
            c.FindJobClass();
            c.CallMethod("Main", new object[] { args });
            Console.WriteLine("Gets Here");
            for (int i = 0; i < 2; i++)
            {
                Console.WriteLine("Trying to start a task");
                c.CallMethod("startTask", new object[] { i });
            }
        }

        public static int[] ParseTokens(string[] args)
        {
            // jobid, sectionid, number of nodes
            int[] result = new int[3];
            // split on comma
            string[] s = args[1].Split(',');
            foreach(string so in s)
            {
                Console.WriteLine(so);
            }
            result[0] = int.Parse(s[0]);
            result[1] = int.Parse(s[1]);
            result[2] = int.Parse(s[2]);
            return result;

        }

        public static void Main2(string[] args)
        {
            Process p = new Process();
            var x = AssemblyLoadContext.Default.LoadFromAssemblyPath(
                @"C:\Users\Adam\Documents\Visual Studio 2015\Projects\MS_Project\AppUsingPDLib_Core\bin\Debug\netcoreapp1.1\AppUsingPDLib_Core.dll");
            Type[] types;
            try
            {
               types = x.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            Job userjob = null;

            foreach (Type t in types)
            {
                try
                {
                    var myInstance = Activator.CreateInstance(t);
                    MethodInfo methodInfo = t.GetMethod("Main");
                    if (methodInfo != null)
                    {
                        object result = null;
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                            // This works fine
                        result = methodInfo.Invoke(myInstance, new object[] { new string[] { "" } });
                        Console.WriteLine(myInstance.GetType());
                        //Console.WriteLine((int)result);
                        Console.WriteLine(result);
                    }

                    methodInfo = t.GetMethod("startTask");
                    if (methodInfo != null)
                    {
                        object result = null;
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        // This works fine
                        result = methodInfo.Invoke(myInstance, new object[] { 1 });
                        Console.WriteLine(myInstance.GetType());
                        //Console.WriteLine((int)result);
                        Console.WriteLine(result);
                    }
                    if (myInstance is Job)
                    {
                        userjob = myInstance as Job;
                    }
                }
                catch(MissingMemberException)
                { }
                
            }

            //Console.WriteLine(userjob?.GetClassName());
            //Console.WriteLine(userjob.doWork(5));
            Console.ReadKey();
        }
    }
}

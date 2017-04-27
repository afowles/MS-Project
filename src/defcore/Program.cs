using System;
using System.IO;
using Defcore.Distributed;
using Defcore.Distributed.Assembly;
using Microsoft.Extensions.CommandLineUtils;

using Defcore.Distributed.Manager;
using Defcore.Distributed.Nodes;

namespace defcore
{
    /// <summary>
    /// The main launcher program for defcore
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main method, run with --help for details
        /// </summary>
        /// <param name="args">program arguments</param>
        public static void Main(string[] args)
        {
            var commandLineApp = new CommandLineApplication(false)
            {
                Name = "defcore"
            };

            // Set up the command for running the server        
            commandLineApp.Command("server", c =>
            {
                c.HelpOption("-?|-h|--help");
                c.Description = "Runs the NodeManager server on a local IP address";
                // specifiy and option
                var directoryOption = c.Option("-d|--directory <PATH>", 
                    "Path to the working directory of the server, input will be relative to current directory", CommandOptionType.SingleValue);
                c.OnExecute(() =>
                    {
                        if (directoryOption.HasValue())
                        {
                            SetOrCreateWorkingDirectory(directoryOption.Value());
                        }
                        NodeManager.Main();
                        return 0;
                    }
                );
            });
            // set up a command for running the node
            commandLineApp.Command("node", c =>
            {
                c.HelpOption("-?|-h|--help");
                c.Description = "Runs the Node on a local IP address";
                
                // specifiy and option
                var directoryOption = c.Option("-d|--directory <PATH>",
                    "Path to the working directory of the node, input will be relative to current directory", CommandOptionType.SingleValue);
                var ipArgument = c.Argument("[IP]", "The IP address for the server");
                c.OnExecute(() =>
                {
                    if (directoryOption.HasValue())
                    {
                        SetOrCreateWorkingDirectory(directoryOption.Value());
                    }
                    if (ipArgument.Values.Count == 0)
                    {
                        Console.WriteLine("Must provide an ip address, use --help for more details");
                        return 1;
                    }
                    Node.Main(new []{ipArgument.Value});
                    return 0;
                });
            });

            // set up a command for submitting a job
            commandLineApp.Command("submit", c =>
            {
                c.HelpOption("-?|-h|--help");
                c.Description = "submits a job to the system";

                // specifiy and option
                var directoryOption = c.Option("-d|--directory <PATH>",
                    "Path to the working directory, input will be relative to current directory", CommandOptionType.SingleValue);
                //var ipArgument = c.Argument("[IP]", "The IP address for the server");
                var program = c.Argument("program", "User program and arguments", true);
                c.OnExecute(() =>
                {
                    if (directoryOption.HasValue())
                    {
                        SetOrCreateWorkingDirectory(directoryOption.Value());
                    }
                    SubmitJob.Main(program.Values.ToArray());
                    return 0;
                });
            });

            // setup a command for loading an executable
            commandLineApp.Command("load", c =>
            {
                c.HelpOption("-?|-h|--help");
                c.Description = "loads a job to and executes it";

                var program = c.Argument("arguments", "program arguments", true);
                c.OnExecute(() =>
                {
                    JobExecuter.Main(program.Values.ToArray());
                    return 0;
                });
            });

            commandLineApp.HelpOption("-? | -h | --help");
            commandLineApp.OnExecute(() =>
            {
                if (args.Length == 0)
                {
                    Console.WriteLine(commandLineApp.GetHelpText());
                }
                return 0;
            });

            try
            {
                commandLineApp.Execute(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        /// <summary>
        /// Set or create the working directory
        /// </summary>
        /// <param name="directory">the directory to set</param>
        public static void SetOrCreateWorkingDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            Directory.SetCurrentDirectory(directory);
        }
    }
}
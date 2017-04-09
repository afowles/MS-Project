using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using Microsoft.Extensions.CommandLineUtils;

using Defcore.Distributed.Manager;

namespace defcore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var commandLineApp = new CommandLineApplication(false)
            {
                Name = "defcore"
            };

            // Set up the command for running the server        
            commandLineApp.Command("server", c =>
            {
                c.Description = "Runs the NodeManager server on a local IP address";
                c.OnExecute(() =>
                    {
                        NodeManager.Main();
                        return 0;
                    }
                );
            });

            // test
            commandLineApp.Command("calc", c =>
            {
                c.Description = "Evaluates arguments with the operation specified.";

                var operationOption = c.Option("-o|--operation <OPERATION>",
                    "You can add or multiply the terms specified using 'add' or 'mul'.", CommandOptionType.SingleValue);
                var termsArg = c.Argument("[terms]", "The numbers to use as a term", true);
                c.HelpOption("-?|-h|--help");
                c.OnExecute(() =>
                {
                    // check to see if we got what we were expecting
                    if (!operationOption.HasValue())
                    {
                        Console.WriteLine("No operation specified.");
                        return 1;
                    }
                    if (termsArg.Values.Count != 2)
                    {
                        Console.WriteLine("You must specify exactly 2 terms.");
                        return 1;
                    }

                    // perform the operation
                    var operation = operationOption.Value();
                    var term1 = int.Parse(termsArg.Values[0]);
                    var term2 = int.Parse(termsArg.Values[1]);
                    if (operation.ToLower() == "mul")
                    {
                        var result = term1 * term2;
                        Console.WriteLine($" {term1} x {term2} = {result}");
                    }
                    else
                    {
                        var result = term1 + term2;
                        Console.WriteLine($" {term1} + {term2} = {result}");
                    }
                    return 0;
                });
            });
            
            CommandOption greeting = commandLineApp.Option(
              "-$|-g |--greeting <greeting>",
              "The greeting to display. The greeting supports"
              + " a format string where {fullname} will be "
              + "substituted with the full name.",
              CommandOptionType.SingleValue);
            CommandOption uppercase = commandLineApp.Option(
              "-u | --uppercase", "Display the greeting in uppercase.",
              CommandOptionType.NoValue);
            commandLineApp.HelpOption("-? | -h | --help");
            commandLineApp.VersionOption("-v | --version", "1.0.0");
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
    }
}
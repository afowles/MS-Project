# MS-Project
Masters Project

A distributed library and framework for .NET Core applications

Parallel and Distributed Library (PDLib) contains framework code for deploying
applications in a distributed way along with a library for writing distributed programs.

All code is built against .NET Core 1.1 and .NET Framework 4.5.2

Separate projects are used for each target, src resides in the framework projects with both \_Core projects compiling against them.

# Building .NET Core Projects
After checking out, run "dotnet restore" in each of the following projects:
Loader, PDLib_Core, StartManager, StartNode and SubmitJob

Then run "dotnet build" in any of the projects to run it, this will build the library if it has not been built yet.

Start the NodeManager first ('dotnet run' in that project), record the IP address for use with Nodes.

Start each Node.

Submit a dll to be run, note: submit a job on the same system that the manager is running on so it can find the proper file.


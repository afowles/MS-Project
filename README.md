# MS-Project
Masters Project

A distributed library and framework for .NET Core applications

Parallel and Distributed Library (PDLib) contains framework code for deploying
applications in a distributed way along with a library for writing distributed programs.

All code is built against .NET Core 1.1 and .NET Framework 4.5.2

Separate projects are used for each target, src resides in the framework projects with both \_Core projects compiling against them.

# Build .NET Core
After checking out, run "dotnet restore" in the PDLib_Core
and in AppUsingPDLib_Core.

Then run "dotnet build" in /AppUsingPDLib_Core, this will build the library if it has not been built yet.

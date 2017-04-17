#!/bin/bash          
echo Building examples
cd ../src/Examples/
dotnet build -c=Release Primes.csproj
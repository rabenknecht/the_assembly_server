#!bin/bash

# I run the server, clearing storage and using the questions of the repository

cd src
dotnet build
cd ..
dotnet src/bin/Debug/net10.0/the_assembly_server.dll -q questions/uncategorized.txt -c -ne --users testUser1 testUser2 testUser3
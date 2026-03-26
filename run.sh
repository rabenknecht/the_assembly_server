#!bin/bash

# I run the server, clearing storage and using the questions of the repository

# I also create some testusers that use the password "test"

cd src
dotnet build
cd ..

dotnet src/bin/Debug/net10.0/the_assembly_server.dll clear serverDirectory

dotnet src/bin/Debug/net10.0/the_assembly_server.dll newUser serverDirectory testUser1 test
dotnet src/bin/Debug/net10.0/the_assembly_server.dll newUser serverDirectory testUser2 test
dotnet src/bin/Debug/net10.0/the_assembly_server.dll newUser serverDirectory testUser3 test

dotnet src/bin/Debug/net10.0/the_assembly_server.dll refQuestions serverDirectory questions/uncategorized.txt

dotnet src/bin/Debug/net10.0/the_assembly_server.dll questionCount serverDirectory

dotnet src/bin/Debug/net10.0/the_assembly_server.dll run serverDirectory
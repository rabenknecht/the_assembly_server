#!bin/bash

# I build the project, prepare the persistent storage with a referenced questionFile, a new entry
# and test users and run the server

cd src
dotnet build
cd ..

# Clear server storage
dotnet src/bin/Debug/net10.0/the_assembly_server.dll clear serverDirectory

# Create a few test users with the password "test"
dotnet src/bin/Debug/net10.0/the_assembly_server.dll newUser serverDirectory testUser1 test
dotnet src/bin/Debug/net10.0/the_assembly_server.dll newUser serverDirectory testUser2 test
dotnet src/bin/Debug/net10.0/the_assembly_server.dll newUser serverDirectory testUser3 test

# Reference a questionFile included in the project
dotnet src/bin/Debug/net10.0/the_assembly_server.dll refQuestions serverDirectory questions/uncategorized.txt

# Create a new entry from any of the questions in the questionFile referenced above
dotnet src/bin/Debug/net10.0/the_assembly_server.dll newEntry serverDirectory

# Print the total number of questions for good measure
dotnet src/bin/Debug/net10.0/the_assembly_server.dll questionCount serverDirectory

# Run the server
dotnet src/bin/Debug/net10.0/the_assembly_server.dll run serverDirectory
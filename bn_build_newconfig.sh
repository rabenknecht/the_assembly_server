#!bin/bash

# I build this project and generate the default configuration file on
# the path that should be used by all other bash scripts

# . util_scripts/remove_core_objbin.sh
dotnet run --project src/the_assembly_server.csproj newconfig .test_config.json
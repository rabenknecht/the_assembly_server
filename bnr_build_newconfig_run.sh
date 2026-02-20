#!bin/bash

# I build this project, generate the default configuration file on
# the path that should be used by all other bash scripts and
# run the server on the newly generated json script!

. util_scripts/remove_core_objbin.sh
dotnet run newconfig .test_config.json
dotnet bin/Debug/net10.0/the_assembly_server.dll run .test_config.json
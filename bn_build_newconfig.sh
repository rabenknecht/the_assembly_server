#!bin/bash

# I build this project and generate the default configuration file on
# the path that should be used by all other bash scripts

. util_scripts/remove_core_objbin.sh
dotnet run newconfig .test_config.json
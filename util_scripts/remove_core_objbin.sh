#!bin/bash

# I remove the obj/ and bin/ directories from the the_assembly_core directory
# no idea what creates them in the first place, but they fuck up our builds and have no purpose,
# so of they go

if [ -d "the_assembly_core/obj" ]; then
    rm -r "the_assembly_core/obj"
    echo removed /the_assembly_core/obj
fi

if [ -d "the_assembly_core/bin" ]; then
    rm -r "the_assembly_core/bin"
    echo removed /the_assembly_core/bin
fi
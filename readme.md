# About

WIP

# Pulling the project

Copy-paste the following command in your bash console.

It will create a dedicated "the_assembly_server" directory containing the repository.

```
git clone https://github.com/rabenknecht/the_assembly_server.git && cd the_assembly_server && git submodule init && git submodule update
```

# Building and running

run.sh is the script that builds, setups and runs the server.
A dotnet SDK supporting net10.0 is required for building and running the project.

You can check out the run.sh for an example on how to use the servers CLI.
The CLI contains help-texts explaining how to use it.

# Testing

t_test_server.sh is the script that runs automatic tests.

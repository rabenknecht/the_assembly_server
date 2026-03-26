using System.CommandLine;
using TheAssembly.Core;
using static System.Console;

namespace TheAssembly.Server;

public static class Shell
{
    public static void Main(string[] args)
    {
        var dirArg = new Argument<string>("directory")
        {
            Description = "The directory used as persistent storage for the server.",
        };
        var userArg = new Argument<string>("username")
        {
            Description = "The username of the user to create",
        };
        var passArg = new Argument<string>("password")
        {
            Description = "The password of the user to create",
            DefaultValueFactory = _ => "",
        };
        var urlOption = new Option<Uri[]>("--url", "-u")
        {
            Description = "The urls over which the server should be accessable.",
            DefaultValueFactory = _ => [ new Uri("http://localhost:2023/") ],
        };

        var runCmd = new Command("run", "Runs the server.")
        {
            dirArg,
            urlOption,
        };
        var clearCmd = new Command("clear", "Clears the server storage.")
        {
            dirArg,
        };
        var newUserCmd = new Command("newUser", "Creates a new user.")
        {
            dirArg,
            userArg,
            passArg,
        };

        runCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(dirArg), out var storage))
            {
                return;
            }

            var server = new Server(p.GetValue(urlOption)!.Select(u => u.ToString()), storage);
            server.RunForever();
        });
        clearCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(dirArg), out var storage))
            {
                return;
            }

            storage.Clear();
            WriteLine("Server storage cleared!");
        });
        newUserCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(dirArg), out var storage))
            {
                return;
            }

            var result = storage.UserStorage.Add(p.GetValue(userArg), p.GetValue(passArg));
            if (result != UserStorage.Error.None)
            {
                Error.WriteLine("Could not add user: " + result.ToString());
            }
        });

        var rootCmd = new RootCommand();
        rootCmd.Subcommands.Add(runCmd);
        rootCmd.Subcommands.Add(clearCmd);
        rootCmd.Subcommands.Add(newUserCmd);
        rootCmd.Parse(args).Invoke();
        return;


        static bool TryCreateStorage(string? dir, out ServerStorage storage)
        {
            if (dir == null)
            {
                Error.WriteLine("Invalid storage location");
                storage = null!;
                return false;
            }

            storage = ServerStorage.CreateIn(dir)!;
            return storage != null;
        }
    }
}

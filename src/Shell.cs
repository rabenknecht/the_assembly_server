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

        var rootCmd = new RootCommand();
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

        rootCmd.Subcommands.Add(runCmd);
        rootCmd.Subcommands.Add(clearCmd);
        rootCmd.Subcommands.Add(newUserCmd);

        runCmd.SetAction(p =>
        {
            var storage = new ServerStorage(p.GetValue(dirArg)!);
            var server = new Server(p.GetValue(urlOption)!.Select(u => u.ToString()), storage);
            server.RunForever();
        });
        clearCmd.SetAction(p =>
        {
            var storage = new ServerStorage(p.GetValue(dirArg)!);
            storage.Clear();
            WriteLine("Server storage cleared!");
        });
        newUserCmd.SetAction(p =>
        {
            var storage = new ServerStorage(p.GetValue(dirArg)!);
            WriteLine("WIP");
        });

        rootCmd.Parse(args).Invoke();
    }
}

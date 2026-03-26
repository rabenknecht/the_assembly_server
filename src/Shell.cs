using System.CommandLine;
using TheAssembly.Core;
using static System.Console;

namespace TheAssembly.Server;

public static class Shell
{
    public static void Main(string[] args)
    {
        var serverDirArg = new Argument<string>("serverDirectory")
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
        var questionFilesArg = new Argument<string[]>("questionFiles")
        {
            Description = "The questionFiles that the server should be able to fetch questions from. "
                + "For more info about formatting, check the repositories readme",
        };
        var urlOption = new Option<Uri[]>("--url", "-u")
        {
            Description = "The urls over which the server should be accessable.",
            DefaultValueFactory = _ => [ new Uri("http://localhost:2023/") ],
        };

        var runCmd = new Command("run", "Runs the server.")
        {
            serverDirArg,
            urlOption,
        };
        runCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(serverDirArg), out var storage))
            {
                return;
            }

            var server = new Server(p.GetValue(urlOption)!.Select(u => u.ToString()), storage);
            server.RunForever();
        });

        var clearCmd = new Command("clear", "Clears the server storage.")
        {
            serverDirArg,
        };
        clearCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(serverDirArg), out var storage))
            {
                return;
            }

            storage.Clear();
            WriteLine("Server storage cleared!");
        });

        var newUserCmd = new Command("newUser", "Creates a new user.")
        {
            serverDirArg,
            userArg,
            passArg,
        };
        newUserCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(serverDirArg), out var storage))
            {
                return;
            }

            var result = storage.UserStorage.Add(p.GetValue(userArg), p.GetValue(passArg));
            if (result != UserStorage.Error.None)
            {
                Error.WriteLine("Could not add user: " + result.ToString());
            }
        });

        var refQuestionsCmd = new Command("refQuestions", "References questionFiles on the server.")
        {
            serverDirArg,
            questionFilesArg,
        };
        refQuestionsCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(serverDirArg), out var storage))
            {
                return;
            }

            foreach (var file in p.GetValue(questionFilesArg) ?? [])
            {
                var result = storage.GeneralQuestionStorage.TryRegisterQuestionFile(file);
                if (result != GeneralQuestionStorage.Error.None)
                {
                    Error.WriteLine($"Failed to register questionFile \"{file}\": {result}");
                }
            }
        });

        var rootCmd = new RootCommand();
        rootCmd.Subcommands.Add(runCmd);
        rootCmd.Subcommands.Add(clearCmd);
        rootCmd.Subcommands.Add(newUserCmd);
        rootCmd.Subcommands.Add(refQuestionsCmd);
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

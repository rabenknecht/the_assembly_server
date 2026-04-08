using System.CommandLine;
using static System.Console;

namespace TheAssembly.Server;

public static class Shell
{
    private static readonly Argument<string> _serverDirArg = new("serverDirectory")
    {
        Description = "The directory used as persistent storage for the server.",
    };

    private static readonly Argument<string[]> _questionFilesArg = new("questionFiles")
    {
        Description = "The questionFiles that the server should be able to fetch questions from.\n"
            + "The repositories readme contains more info about questionFIle formatting",
    };

    private static readonly Option<Uri[]> _urlOption = new("--url", "-u")
    {
        Description = "The urls over which the server should be accessable.",
        DefaultValueFactory = _ => [ new Uri("http://localhost:2023/") ],
    };

    private static readonly Argument<string> _userArg = new("username")
    {
        Description = "The username of the user",
    };

    private static readonly Argument<string> _passArg = new("password")
    {
        Description = "The password of the user",
        DefaultValueFactory = _ => "",
    };


    public static void Main(string[] args)
    {
        var runCmd = new Command("run", "Runs the server.")
        {
            _serverDirArg,
            _urlOption,
        };
        runCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(_serverDirArg), out var storage))
            {
                return;
            }

            var server = new Server(p.GetValue(_urlOption)!.Select(u => u.ToString()), storage);
            // TODO: How to run async stuff in sync?
            _ = server.RunAsync();
            while (true)
            {
                Thread.Sleep(int.MaxValue);
            }
        });


        var clearCmd = new Command("clear", "Clears the server storage.")
        {
            _serverDirArg,
        };
        clearCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(_serverDirArg), out var storage))
            {
                return;
            }

            storage.Clear();
            WriteLine("Server storage cleared!");
        });


        var newUserCmd = new Command("newUser", "Creates a new user.")
        {
            _serverDirArg,
            _userArg,
            _passArg,
        };
        newUserCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(_serverDirArg), out var storage))
            {
                return;
            }

            var result = storage.UserStorage.Add(p.GetValue(_userArg), p.GetValue(_passArg));
            if (result != UserStorage.Error.None)
            {
                Error.WriteLine("Could not add user: " + result.ToString());
            }
        });


        var refQuestionsCmd = new Command("refQuestions", "References questionFiles on the server.")
        {
            _serverDirArg,
            _questionFilesArg,
        };
        refQuestionsCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(_serverDirArg), out var storage))
            {
                return;
            }

            foreach (var file in p.GetValue(_questionFilesArg) ?? [])
            {
                var result = storage.GeneralQuestionStorage.TryRegisterQuestionFile(file);
                if (result != GeneralQuestionStorage.Error.None)
                {
                    Error.WriteLine($"Failed to register questionFile \"{file}\": {result}");
                }
            }
        });


        var questionCountCmd = new Command("questionCount", "Prints the total number of questions currently accessable to the server.")
        {
            _serverDirArg,
        };
        questionCountCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(_serverDirArg), out var storage))
            {
                return;
            }

            WriteLine($"QUESTIONCOUNT:");
            WriteLine($"\tTotal:{storage.GeneralQuestionStorage.TotalQuestionCount}");
            WriteLine($"\tUnused:{storage.UniqueQuestionStorage.UnusedQuestionsCount}");
            WriteLine($"\tUsed:{storage.UniqueQuestionStorage.UsedQuestionsCount}");
        });


        var newEntryCmd = new Command("newEntry", "Creates a new entry from a random question that has not been used so far.")
        {
            _serverDirArg,
        };
        newEntryCmd.SetAction(p =>
        {
            if (!TryCreateStorage(p.GetValue(_serverDirArg), out var storage))
            {
                return;
            }

            if (!storage.NewRandomEntry())
            {
                Error.WriteLine("Could not create new entry: OutOfQuestions");
            }
        });


        var rootCmd = new RootCommand();
        rootCmd.Subcommands.Add(runCmd);
        rootCmd.Subcommands.Add(clearCmd);
        rootCmd.Subcommands.Add(newUserCmd);
        rootCmd.Subcommands.Add(refQuestionsCmd);
        rootCmd.Subcommands.Add(questionCountCmd);
        rootCmd.Subcommands.Add(newEntryCmd);
        rootCmd.Parse(args).Invoke();
        return;


        static bool TryCreateStorage(string? dir, out ServerStorage storage)
        {
            if (dir == null)
            {
                Error.WriteLine("Invalid persistent storage location");
                storage = null!;
                return false;
            }

            storage = ServerStorage.CreateIn(dir)!;
            return storage != null;
        }
    }
}

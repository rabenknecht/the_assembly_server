using static System.Console;

namespace TheAssembly.Server;

public static class Shell
{
    private const string DEFAULT_URL = "http://localhost:2023/";
    private const string DEFAULT_SERVERDIR = "serverDirectory";
    private const string OPTION_PREFIX = "-";

    public static void Main(string[] args)
    {
        var argList = new List<string>(args);

        if (ExtractOptionNoArgs(argList, "-h"))
        {
            WriteLine();
            WriteLine($"All options are executed from top to bottom.");
            WriteLine($"For each option, only the first entry is executed, the rest ignored");
            WriteLine();
            WriteLine($"-h                          Prints this text and prevents all other commands from being executed.");
            WriteLine();
            WriteLine($"-u [url1] [url2] ...        Sets urls through which the server can be accessed.");
            WriteLine($"                            Defaults to {DEFAULT_URL} when no urls are passed, or when omitted.");
            WriteLine();
            WriteLine($"-q [file1] [file2] ...      Sets the files from which the server fetches questions.");
            WriteLine($"                            Each file can be used by multiple servers.");
            WriteLine($"                            Optional.");
            WriteLine();
            WriteLine($"-d [directory]              Sets the directory in which the server saves its data for persistence.");
            WriteLine($"                            The server loads data from where when restarted.");
            WriteLine($"                            Defaults to {DEFAULT_SERVERDIR} when no directory is passed, or when omitted.");
            WriteLine();
            WriteLine($"--log                       Logs any requests to and the responses from the server.");
            WriteLine($"                            Never logs IP-Adresses of request senders.");
            WriteLine();
            WriteLine($"-c                          Clears the servers persistent storage.");
            WriteLine();
            WriteLine($"--users [user1] [user2] ... Creates new user accounts with no user passwords.");
            WriteLine($"                            Optional.");
            WriteLine();
            WriteLine($"-ne                         Immediately loads a new random entry from the passed questions.");
            WriteLine();
            return;
        }


        var urls = ExtractOptionMultiArg(argList, "-u", OPTION_PREFIX);
        if (urls == null)
        {
            urls = [ DEFAULT_URL ];
            WriteLine($"No urls have been passed as argument. Using {DEFAULT_URL} instead.");
        }


        var questionFiles = ExtractOptionMultiArg(argList, "-q", OPTION_PREFIX) ?? [];
        if (questionFiles.Count == 0)
        {
            WriteLine("No questionFiles have been passed as argument. The server will still run "
                + "and allow joining or viewing past entries, but no new entries can be generated.");
        }


        var serverDir = ExtractOptionSingleArg(argList, "-d", OPTION_PREFIX);
        if (serverDir == null)
        {
            serverDir = DEFAULT_SERVERDIR;
            WriteLine($"No directory for the server has been passed. Using {DEFAULT_SERVERDIR} instead.");
        }


        var server = new Server(urls, serverDir, questionFiles, ExtractOptionNoArgs(argList, "--log"));


        if (questionFiles.Count != 0)
        {
            WriteLine($"Loaded a total of {server.QuestionCount} questions");
        }


        if (ExtractOptionNoArgs(argList, "-c"))
        {
            server.ClearStorage();
        }


        foreach (var user in ExtractOptionMultiArg(argList, "--users", OPTION_PREFIX) ?? [])
        {
            if (!server.TryAddUser(user, ""))
            {
                WriteLine($"Could not add user {user}. Ignoring them instead");
            }
        }


        if (ExtractOptionNoArgs(argList, "-ne"))
        {
            if (!server.NewRandomEntry())
            {
                WriteLine($"Could not create a new random entry. Either no question files "
                    + "have been passed, or all questions have already been picked");
            }
        }


        if (argList.Count != 0)
        {
            WriteLine($"Some arguments could not be parsed: {string.Join(", ", argList.Select(s => $"\"{s}\""))}");
        }


        new Thread(() => 
        {
            while (true)
            {
                try
                {
                    server.RunForever();
                }
                catch (Exception e)
                {
                    Error.WriteLine("Exception thrown while running server: " + e);
                    WriteLine("Restarting server...");
                }
            }
        }).Start();
        WriteLine("Console started.");
        WriteLine("    Write any commands to modify the server during runtime.");
        WriteLine("    Call \"help\" to see all available console commands");
        while (true)
        {
            var input = ReadLine();
            
            if (input == "help")
            {
                WriteLine($"    help                        Prints this text.");
                WriteLine($"    newentry                    Immediately loads a new random entry from the questions the server has access to.");
                WriteLine($"    daily newentry [hh:mm:ss]   Loads a new random entry from the questions the server has access at [hh:mm:ss] local time every day.");
            }
            else if (input == "newentry")
            {
                server.NewRandomEntry();
            }
            else if (input != null && input.StartsWith("daily newentry ")
                && TimeOnly.TryParseExact(input[15..], "HH:mm:ss", out var time))
            {
                server.DailyNewRandomEntry(time);
            }
            else
            {
                WriteLine("Unknown command. Use \"help\" to see list of available commands");
            }
        }
    }


    private static bool ExtractOptionNoArgs(List<string> argList, string option)
    {
        return argList.Remove(option);
    }


    private static string? ExtractOptionSingleArg(List<string> argList, string option, string optionPrefix)
    {
        for (int i = 0; i < argList.Count - 1; i++)
        {
            if (argList[i] == option)
            {
                argList.RemoveAt(i);
                var result = argList[i];
                if (result.StartsWith(optionPrefix))
                {
                    return null;
                }
                else
                {
                    argList.RemoveAt(i);
                    return result;
                }
            }
        }

        return null;
    }


    private static ICollection<string>? ExtractOptionMultiArg(List<string> argList, string option, string optionPrefix)
    {
        for (int i = 0; i < argList.Count - 1; i++)
        {
            if (argList[i] == option)
            {
                argList.RemoveAt(i);
                var result = new List<string>();
                while (i < argList.Count && !argList[i].StartsWith(optionPrefix))
                {
                    result.Add(argList[i]);
                    argList.RemoveAt(i);
                }
                return result;
            }
        }

        return null;
    }
}

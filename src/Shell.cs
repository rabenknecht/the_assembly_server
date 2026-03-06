using static System.Console;

namespace TheAssembly.Server;

public static class Shell
{
    private const string DEFAULT_URL = "http://localhost:2023/";
    private const string DEFAULT_SERVERDIR = "serverDirectory";


    public static void Main(string[] args)
    {
        var argList = new List<string>(args);

        if (ExtractOptionNoArgs(argList, "-h"))
        {
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
            WriteLine($"-c                          Clears the servers persistent storage.");
            WriteLine();
            WriteLine($"-ne                         Immediately loads a new random entry from the passed questions.");
            return;
        }

        var urls = ExtractOptionMultiArg(argList, "-u", "-");
        if (urls == null)
        {
            urls = [ DEFAULT_URL ];
            WriteLine($"No urls have been passed as argument. Using {DEFAULT_URL} instead.");
        }

        var questionFiles = ExtractOptionMultiArg(argList, "-q", "-") ?? [];
        if (questionFiles.Count == 0)
        {
            WriteLine("No questionFiles have been passed as argument. The server will still run "
                + "and allow joining or viewing past entries, but no new entries can be generated.");
        }

        var serverDir = ExtractOptionSingleArg(argList, "-d");
        if (serverDir == null)
        {
            serverDir = DEFAULT_SERVERDIR;
            WriteLine($"No directory for the server has been passed. Using {DEFAULT_SERVERDIR} instead.");
        }

        var server = new Server(urls, serverDir, questionFiles);
        
        if (ExtractOptionNoArgs(argList, "-c"))
        {
            server.ClearStorage();
            WriteLine("Server persistent storage cleared.");
        }

        if (ExtractOptionNoArgs(argList, "-ne"))
        {
            if (server.NewRandomEntry())
            {
                WriteLine("New random entry loaded.");
            }
            else
            {
                WriteLine($"Could not create a new random entry. Either no question files "
                    + "have been passed, or all questions have already been picked");
            }
        }

        if (argList.Count != 0)
        {
            WriteLine($"Some arguments could not be parsed: {string.Join(", ", argList.Select(s => $"\"{s}\""))}");
        }

        server.RunForever();
    }


    private static bool ExtractOptionNoArgs(List<string> argList, string option)
    {
        return argList.Remove(option);
    }


    private static string? ExtractOptionSingleArg(List<string> argList, string option)
    {
        for (int i = 0; i < argList.Count - 1; i++)
        {
            if (argList[i] == option)
            {
                var result = argList[i + 1];
                argList.RemoveAt(i);
                argList.RemoveAt(i);
                return result;
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

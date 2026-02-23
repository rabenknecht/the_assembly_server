using System.Net;

namespace TheAssembly.Server;

public static class Shell
{
    private const string DEFAULT_CONFIG = "config.json";
    private const string COMMAND_NEWCONFIG = "newconfig";

    public static void Main(string[] args)
    {
        if ((args.Length == 1 || args.Length == 2) && args[0] == COMMAND_NEWCONFIG)
        {
            NewConfig(args.GetOr(1, DEFAULT_CONFIG));
        }
        else if ((args.Length == 1 || args.Length == 2) && args[0] == "run")
        {
            SetupAndRun(args.GetOr(1, DEFAULT_CONFIG));
        }
        else
        {
            PrintHelp();
        }
    }


    private static void PrintHelp()
    {
        Console.WriteLine("TODO: This should be a helptext. If you see this, hit Rabenknecht with a shovel so they actually write this shit");
    }


    private static void NewConfig(string newConfigFile)
    {
        try
        {
            File.WriteAllText(newConfigFile, Config.DefaultSerialized);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while writing the configuration file ({newConfigFile}):\n{e}");
            return;
        }

        Console.WriteLine($"Successfully created default configuration file at \"{newConfigFile}\"");
    }


    private static void SetupAndRun(string configFile)
    {
        string rawConfigData;
        Config? config;

        try
        {
            rawConfigData = File.ReadAllText(configFile);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while accessing the configuration file ({configFile}):\n{e}");
            return;
        }

        try
        {
            config = Config.Deserialize(rawConfigData);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while parsing the content of the configuration file ({configFile}):\n{e}");
            return;
        }

        if (config == null)
        {
            Console.Error.WriteLine($"Parsing config file failed: Illegal json. Use \"{COMMAND_NEWCONFIG}\" to generate a default template for the config file");
            return;
        }

        if (File.Exists(config.DatabankPath))
        {
            Console.Error.WriteLine($"Databank path of config file is a file!");
            return;
        }

        Console.WriteLine($"Successfully parsed configuration file at \"{configFile}\"");
        Run(config);
    }


    private static void Run(Config config)
    {
        var storage = new ServerStorage(config.DatabankPath);
        var server = new Server(config.UrlPrefixes, storage);
        server.RunForever();
    }
}

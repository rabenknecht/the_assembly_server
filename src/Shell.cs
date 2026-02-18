using System.Text.Json;

namespace TheAssembly.Server;

public class Shell
{
    private const string DEFAULT_CONFIG = "config.json";


    public static void Main(string[] args)
    {
        if ((args.Length == 1 || args.Length == 2) && args[0] == "newconfig")
        {
            NewConfig(args.GetOr(1, DEFAULT_CONFIG));
        }
        else if ((args.Length == 1 || args.Length == 2) && args[0] == "run")
        {
            Run(args.GetOr(1, DEFAULT_CONFIG));
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
            File.WriteAllText(newConfigFile, Config.DefaultJson);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while writing the configuration file ({newConfigFile}):\n{e}");
            return;
        }

        Console.WriteLine($"Successfully created default configuration file at {newConfigFile}");
    }


    private static void Run(string configFile)
    {
        string configData;
        Config config;

        try
        {
            configData = File.ReadAllText(configFile);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while accessing the configuration file ({configFile}):\n{e}");
            return;
        }

        try
        {
            config = JsonSerializer.Deserialize<Config>(configData);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while parsing the content of the configuration file ({configFile}):\n{e}");
            return;
        }

        Console.WriteLine($"Successfully read configuration file at {configFile}");

        var databank = new FileBasedDatabank();
    }
}

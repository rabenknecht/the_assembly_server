using System.Text.Json;

namespace TheAssembly.Server;

public class Shell
{
    private const string NEWCONFIG_IDENTIFIER = "--newConfig";
    private const string DEFAULT_CONFIG = "config.json";

    public static void Main(string[] args)
    {
        if (args.Any(s => s == NEWCONFIG_IDENTIFIER))
        {
            var newConfigFile = args.ExtractOption(NEWCONFIG_IDENTIFIER).Or(DEFAULT_CONFIG);

            try
            {
                File.WriteAllText(newConfigFile, Config.DefaultJson);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Exception while writing the configuration file ({newConfigFile}):\n{e}");
            }

            return;
        }


        var configFile = args.GetOr(0, DEFAULT_CONFIG);
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

        
    }
}

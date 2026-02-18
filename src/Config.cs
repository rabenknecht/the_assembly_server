using System.Text.Json;

namespace TheAssembly.Server;

public struct Config
{
    public string DatabankPath;
    public string[] ServerPrefixes;


    public static Config Default => new()
    {
        DatabankPath = "databank",
        ServerPrefixes = new []
        {
            "http://localhost:2302"
        }
    };

    public static string DefaultJson => JsonSerializer.Serialize(Default);
}

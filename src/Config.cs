using System.Text.Json;

namespace TheAssembly.Server;

public struct Config
{
    // "Fun" fact: System.Text.Json *somehow* does not support parsing fields!
    // I *need* to make them properties. Have not tested if only
    // a implemented getter is enough...
    public string DatabankPath { get; set; }
    public string[] ServerPrefixes { get; set; }


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

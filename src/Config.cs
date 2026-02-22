using System.Text.Json;

namespace TheAssembly.Server;

public class Config
{
    // "Fun" fact: System.Text.Json *somehow* does not support parsing fields!
    // I *need* to make them properties. Have not tested if only
    // a implemented getter is enough...
    public string DatabankPath { get; set; } = "";
    public string[] UrlPrefixes { get; set; } = [];


    public static Config? Deserialize(string from) => JsonSerializer.Deserialize<Config>(from);


    public static Config Default => new()
    {
        DatabankPath = "databank",
        UrlPrefixes = new []
        {
            "http://localhost:2302/"
        }
    };

    public static string DefaultSerialized => JsonSerializer.Serialize(Default);
}

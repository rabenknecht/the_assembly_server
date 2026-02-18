using System.Text.Json;

namespace TheAssembly.Server;

public struct Config
{
    public string DatabankPath;


    public static Config Default => new() { DatabankPath = "databank"};

    public static string DefaultJson => JsonSerializer.Serialize(Default);
}

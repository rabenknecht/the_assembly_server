using System.Text.Json;

namespace TheAssembly.Server;

public struct Config
{
    public string DatabankPath;
    public int Port;


    public static Config Default => new() { DatabankPath = "databank", Port = 2302 };

    public static string DefaultJson => JsonSerializer.Serialize(Default);
}

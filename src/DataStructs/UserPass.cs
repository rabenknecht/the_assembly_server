using System.Text.Json;

namespace TheAssembly.Server;

public class UserPass
{
    public string User { get; }  = "";
    public string Password { get; }  = "";


    public static bool TryDeserialize(byte[] data, out UserPass userPass)
    {
        userPass = JsonSerializer.Deserialize<UserPass>(data)!;
        return userPass != null;
    }
}

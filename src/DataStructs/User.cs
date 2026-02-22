using System.Text;
using System.Text.Json;

namespace TheAssembly.Server;

public class User
{
    public string Name { get; set; } = "";
    public string[] BelongsToGroups { get; set; } = [];


    public User() {}


    public User(string name, string[] belongsToGroups)
    {
        Name = name;
        BelongsToGroups = belongsToGroups;
    }


    public static User? Deserialize(byte[] from) => JsonSerializer.Deserialize<User>(from);


    public static byte[] Serialize(User user) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(user));
}

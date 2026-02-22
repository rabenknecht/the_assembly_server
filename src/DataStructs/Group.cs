using System.Text;
using System.Text.Json;

namespace TheAssembly.Server;

public class Group
{
    public string[] ContainsUsers { get; set; } = [];
    public string[] ContainsEntryIds { get; set; } = [];
    public string Name { get; set; } = "";


    public static Group? Deserialize(byte[] from) => JsonSerializer.Deserialize<Group>(from);


    public static byte[] Serialize(Group group) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(group));
}

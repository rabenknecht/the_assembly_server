using System.Text;
using System.Text.Json;

namespace TheAssembly.Server;

public class Group
{
    public string Name { get; set; } = "";
    public string[] ContainsUsers { get; set; } = [];
    public string[] ContainsEntryIds { get; set; } = []; // Last entry is the active entry
    public string[] NextQuestionIds { get; set; } = [];


    public static Group? Deserialize(byte[] from) => JsonSerializer.Deserialize<Group>(from);


    public static byte[] Serialize(Group group) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(group));
}

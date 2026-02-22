using System.Text;
using System.Text.Json;

namespace TheAssembly.Server;

public class Entry
{
    public string Id { get; set; }
    public string BelongsToGroup { get; set; }
    public Question Question { get; set; }
    public UserVote[] Votes { get; set; }


    public static Entry? Deserialize(byte[] from) => JsonSerializer.Deserialize<Entry>(from);


    public static byte[] Serialize(Entry entry) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry));
}

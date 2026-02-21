using System.Text;
using System.Text.Json;

namespace TheAssembly.Server;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long[] BelongsToGroupIds { get; set; } = [];


    public User() {}


    public User(long id, string name, long[] belongsToGroupIds)
    {
        Id = id;
        Name = name;
        BelongsToGroupIds = belongsToGroupIds;
    }


    public static User? Deserialize(byte[] from)
    {
        return JsonSerializer.Deserialize<User>(from);
    }


    public static User? Deserialize(Stream from)
    {
        return JsonSerializer.Deserialize<User>(from);
    }


    public static bool TryDeserialize(Stream from, out User user)
    {
        user = Deserialize(from) ?? new User();
        return user != null;
    }


    public static byte[] Serialize(User user) => user.Serialize();


    public byte[] Serialize()
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
    }
}

using System.Text;
using System.Text.Json;

namespace TheAssembly.Server;

public class Question
{
    // TODO: Allow to auto-add Users of a group to the Question.
    public string Name { get; }
    public string[] VoteOptions { get; }


    public static Question? Deserialize(byte[] from) => JsonSerializer.Deserialize<Question>(from);


    public static byte[] Serialize(Question question) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(question));
}

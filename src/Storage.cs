using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TheAssembly.Server;

public class Storage
{

    public Storage(string basePath)
    {
        if (!Directory.Exists(basePath)) throw new ArgumentException($"No directory with path {basePath} exists");
        _basePath = basePath;
    }


    public IEnumerable<long> UserIds =>
        Directory.GetFiles(UserDirectoryPath)
            .Where(s => long.TryParse(s, out _))
            .Select(long.Parse);


    public bool TryGetEncodedUser(long userId, [NotNull] out byte[] result)
    {
        var path = UserFilePath(userId);

        if (!File.Exists(path))
        {
            result = [];
            return false;
        }

        result = File.ReadAllBytes(path);
#if DEBUG
        if (User.Deserialize(result) == null) throw new Exception("Illegal encoding for user");
#endif
        return true;
    }


    public byte[]? GetEncodedUser(long userId)
    {
        if (TryGetEncodedUser(userId, out var result)) return result;
        return null;
    }


    public bool TryGetUser(long userId, out User user)
    {
        if (!TryGetEncodedUser(userId, out var rawData))
        {
            user = new User();
            return false;
        }

        user = User.Deserialize(rawData)!;
        if (user != null) return true;
        user = new User();
        return false;
    }


    public User? GetUser(long userId)
    {
        var rawData = GetEncodedUser(userId);
        if (rawData == null) return null;
        return User.Deserialize(rawData);
    }


    public User? GetUser(long? userId)
    {
        if (userId == null) return null;
        return GetUser(userId.Value);
    }


    /// <summary>
    /// Saves a new User instance on userId.
    /// Replaces the old instance if a User with the passed userId already exists.
    /// Updates the id of the passed User to be equal to the passed userId.
    /// </summary>
    public void UpdateUser(long userId, User user)
    {
        user.Id = userId;
        var path = UserFilePath(userId);
        var rawData = JsonSerializer.Serialize(user);
        File.WriteAllText(path, rawData);
    }


    private readonly string _basePath;
    private string UserDirectoryPath => Path.Combine(_basePath, "userData");
    private string UserFilePath(long userId) => Path.Combine(UserDirectoryPath, userId.ToString());
}

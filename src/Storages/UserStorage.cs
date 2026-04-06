using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using TheAssembly.Core;

namespace TheAssembly.Server;

/// <summary>
/// All accessors validate beforehand if passed usernames are legal.
/// </summary>
public class UserStorage
{
    /// <param name="baseDir">Which directory can the PassStorage use to save passwords.</param>
    public UserStorage(string baseDir)
    {
        _baseDir = baseDir ?? throw new ArgumentNullException(nameof(baseDir));
    }


    /// <returns>If the username is legal, and has no pass already in storage</returns>
    public bool CanBeNew(string? user)
    {
        if (!IsUserLegal(user)) return false;
        return !File.Exists(PassFileFor(user!));
    }


    public bool Exists(string? user)
    {
        if (!IsUserLegal(user)) return false;
        return File.Exists(PassFileFor(user!));
    }


    /// <summary>
    /// Updates the pass for the passed user. Creates a new entry if the user
    /// had no password before.
    /// <param/>
    /// Returns false if the passed user is illegal.
    /// </summary>
    public Error AddOrUpdate(string? user, string? pass)
    {
        if (!IsUserLegal(user)) return Error.InvalidUsername;

        var salt = RandomNumberGenerator.GetBytes(SALT_LENGTH);
        var hash = HashPass(pass ?? "", salt, user!);
        File.WriteAllBytes(PassFileFor(user!), salt.Concat(hash));

        return Error.None;
    }


    /// <summary>
    /// Creates a new entry if the user was not added before. Fails if the user already has a pass,
    /// or if the username is invalid.
    /// </summary>
    public Error Add(string? user, string? pass)
    {
        if (!IsUserLegal(user)) return Error.InvalidUsername;

        var passFileFor = PassFileFor(user!);
        if (File.Exists(passFileFor)) return Error.UserAlreadyExists;

        var salt = RandomNumberGenerator.GetBytes(SALT_LENGTH);
        var hash = HashPass(pass ?? "", salt, user!);
        File.WriteAllBytes(PassFileFor(user!), salt.Concat(hash));

        return Error.None;
    }


    /// <returns>False if the password is incorrect, or no entry exists</returns>
    public bool Correct(string? user, string? pass)
    {
        if (!IsUserLegal(user)) return false;
        var passFile = PassFileFor(user!);
        if (!File.Exists(passFile)) return false;

        var stored = File.ReadAllBytes(PassFileFor(user!));
        var salt = stored.SubArray(0, SALT_LENGTH);
        var expectedHash = stored.SubArray(SALT_LENGTH, stored.Length - SALT_LENGTH);
        var actualHash = HashPass(pass ?? "", salt, user!);
        return Enumerable.SequenceEqual(expectedHash, actualHash);
    }


    public IEnumerable<string> EnumerateUsers =>
        Directory.EnumerateFiles(_baseDir)
            .Select(p => p.Split(Path.DirectorySeparatorChar)[^1]);


    public ICollection<string> CollectUsers => [.. EnumerateUsers];


    public void Clear()
    {
        foreach (var file in Directory.GetFiles(_baseDir)) File.Delete(file);
    }


    public bool IsUserLegal(string? user)
    {
        const string legalChars = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_-";
        return user != null && user.All(legalChars.Contains);
    }


    public enum Error
    {
        None = 0,
        InvalidUsername = 0x1,
        UserAlreadyExists = 0x2,
    }


    private string PassFileFor(string user) => Path.Combine(_baseDir, user);


    private byte[] HashPass(string pass, byte[] salt, string user)
    {
        // Argon2 doesn't like empty passes, so we add a symbol to gurantee non empty passes
        var argon2 = new Argon2d(Encoding.UTF8.GetBytes(pass + "x"));

        // TODO: Set params outside program!
        argon2.DegreeOfParallelism = 16;
        argon2.MemorySize = 8192;
        argon2.Iterations = 40;
        argon2.Salt = salt;
        argon2.AssociatedData = Encoding.UTF8.GetBytes(user);

        return argon2.GetBytes(32);
    }


    private readonly string _baseDir;

    private const int SALT_LENGTH = 16;
}

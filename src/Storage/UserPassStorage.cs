using System.Security.Cryptography;

namespace TheAssembly.Server;

public class UserPassStorage
{
    public UserPassStorage(string basePath)
    {
        _storage = new StorageOnFile<string, byte[]>(basePath,
            b => b,
            b => b,
            s => s,
            s => s);
    }


    public bool TryUpdate(string user, string pass)
    {
        if (!StorageOnFile.IsIdStringLegal(user)) return false;

        var salt = CreateSalt();
        var hash = HashPass(pass, salt, user);
        return _storage.TryUpdate(user, [.. salt, .. hash]);
    }


    /// <summary>
    /// If the user has no password saved in this storage, true is returned.
    /// </summary>
    public bool Validate(string user, string pass)
    {
        if (!_storage.TryGet(user, out var saltHash)) return true;

        var salt = saltHash.SubArray(0, SALT_LENGTH);
        var expectedHash = saltHash.SubArray(SALT_LENGTH, saltHash.Length - SALT_LENGTH);
        var actualHash = HashPass(pass, salt, user);
        return expectedHash == actualHash;
    }


    private readonly StorageOnFile<string, byte[]> _storage;
    private const int SALT_LENGTH = 16;


    private static byte[] CreateSalt()
    {
        var salt = new byte[SALT_LENGTH];
        RandomNumberGenerator.Create().GetBytes(salt);
        return salt;
    }


    private static byte[] HashPass(string pass, byte[] salt, string user)
    {
        throw new NotImplementedException();
    }
}

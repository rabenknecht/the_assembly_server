namespace TheAssembly.Server;

/// <summary>
/// All accessors validate beforehand if passed usernames are legal.
/// </summary>
internal class PassStorage
{
    /// <param name="baseDir">Which directory can the PassStorage use to save passwords.</param>
    public PassStorage(string baseDir)
    {
        _baseDir = baseDir ?? throw new ArgumentNullException(nameof(baseDir));
    }


    /// <returns>If the username is legal, and has no pass already in storage</returns>
    public bool CanBeNew(string? user)
    {
        if (!IsUserLegal(user)) return false;
        return !File.Exists(PassFileFor(user!));
    }


    /// <summary>
    /// Updates the pass for the passed user. Creates a new entry if the user
    /// had no password before.
    /// <param/>
    /// Returns false if the passed user is illegal.
    /// </summary>
    /// <exception cref="ArgumentException">When user has a illegal name.</exception>
    public bool Update(string? user, string? pass)
    {
        if (!IsUserLegal(user)) return false;

        // TODO: DO NOT SAVE PASSES IN PLAINTEXT!!!!!!
        File.WriteAllText(PassFileFor(user!), pass);

        return true;
    }


    public bool CorrectPass(string? user, string? pass)
    {
        if (!IsUserLegal(user)) return false;
        // TODO: DO NOT SAVE PASSES IN PLAINTEXT!!!!!!
        return File.ReadAllText(PassFileFor(user!)) == pass;
    }


    public IEnumerable<string> EnumerateUsers => Directory.EnumerateFiles(_baseDir);


    public ICollection<string> CollectUsers => Directory.GetFiles(_baseDir);


    public void Clear()
    {
        foreach (var file in Directory.GetFiles(_baseDir)) File.Delete(file);
    }


    public bool IsUserLegal(string? user)
    {
        const string legalChars = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_-";
        return user != null && user.All(legalChars.Contains);
    }


    private string PassFileFor(string user) => Path.Combine(_baseDir, user);


    private readonly string _baseDir;
}

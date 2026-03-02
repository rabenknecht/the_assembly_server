namespace TheAssembly.Server;

internal class PassStorage
{
    public PassStorage(string baseDir)
    {
        _baseDir = baseDir;
    }


    public bool HasPass(string user)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Update the pass for user that already exists, or create a new user and update its pass.
    /// </summary>
    public void Update(string user, string pass)
    {
        // TODO: Check for allowed symbols
        throw new NotImplementedException();
    }


    public bool CorrectPass(string user, string pass)
    {
        // TODO: Check for allowed symbols
        throw new NotImplementedException();
    }


    public IEnumerable<string> Users => throw new NotImplementedException();


    public void Clear()
    {
        throw new NotImplementedException();
    }


    private readonly string _baseDir;
}

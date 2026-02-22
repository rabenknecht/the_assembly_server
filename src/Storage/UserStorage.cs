namespace TheAssembly.Server;

public class UserStorage : StorageOnFile<string, User>
{
    public UserStorage(string basePath) :
        base(basePath, User.Serialize, User.Deserialize!, s => s)
    {
    }
}

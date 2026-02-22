namespace TheAssembly.Server;

public class UserStorage : Storage<string, User>
{
    public UserStorage(string basePath) :
        base(basePath, User.Serialize, User.Deserialize!, s => s)
    {
    }
}

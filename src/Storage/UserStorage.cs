namespace TheAssembly.Server;

public class UserStorage : Storage<long, User>
{
    public UserStorage(string basePath) :
        base(basePath, User.Serialize, User.Deserialize!, long.Parse)
    {
    }
}

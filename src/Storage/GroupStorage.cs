namespace TheAssembly.Server;

public class GroupStorage : Storage<string, Group>
{
    public GroupStorage(string basePath) :
        base(basePath, Group.Serialize, Group.Deserialize!, s => s)
    {
    }
}

namespace TheAssembly.Server;

public class GroupStorage : StorageOnFile<string, Group>
{
    public GroupStorage(string basePath) :
        base(basePath, Group.Serialize, Group.Deserialize!, s => s)
    {
    }
}

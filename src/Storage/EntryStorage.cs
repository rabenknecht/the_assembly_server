namespace TheAssembly.Server;

public class EntryStorage : Storage<string, Entry>
{
    public EntryStorage(string basePath) :
        base(basePath, Entry.Serialize, Entry.Deserialize!, s => s)
    {
    }
}

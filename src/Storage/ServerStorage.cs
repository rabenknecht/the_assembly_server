namespace TheAssembly.Server;

public class ServerStorage
{
    public ServerStorage(string basePath)
    {
        Users = new UserStorage(Path.Combine(basePath, "users"));
        Questions = new QuestionStorage(Path.Combine(basePath, "questions"));
        Groups = new GroupStorage(Path.Combine(basePath, "groups"));
        Entries = new EntryStorage(Path.Combine(basePath, "entries"));
    }

    public readonly UserStorage Users;
    public readonly QuestionStorage Questions;
    public readonly GroupStorage Groups;
    public readonly EntryStorage Entries;
}

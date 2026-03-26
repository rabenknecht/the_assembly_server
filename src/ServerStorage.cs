namespace TheAssembly.Server;

/// <summary>
/// Persistent storage used by the server to store users and their passwords, entries and alike.
/// <para/>
/// Can be used to modify the storage without saving it in a server instance.
/// <para/>
/// Can be used while another thread or application instance already has a ServerStorage instance.
/// </summary>
public class ServerStorage : IDisposable
{
    /// <param name="directory">The directory in which to setup the ServerStorage.
    /// Can have existing files and directory from a pervious ServerStorage instance.</param>
    /// <returns>Null when the directory passed has a illegal file structure</returns>
    public static ServerStorage? CreateIn(string directory)
    {
        var userDir = Path.Combine(directory, "passwords");
        var usedQuestionsFile = Path.Combine(directory, "usedQuestions");
        var questionReferenceFile = Path.Combine(directory, "questionFileRefs");
        var entryFile = Path.Combine(directory, "entries");

        if (Directory.Exists(directory))
        {
            var existingDirs = Directory.EnumerateDirectories(directory).ToList();
            var existingFiles = Directory.EnumerateFiles(directory).ToList();

            existingDirs.Remove(userDir);
            existingFiles.Remove(usedQuestionsFile);
            existingFiles.Remove(questionReferenceFile);
            existingFiles.Remove(entryFile);

            if (existingDirs.Count != 0 || existingFiles.Count != 0)
            {
                return null;
            }
        }
        else
        {
            Directory.CreateDirectory(directory);
        }

        if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
        if (!File.Exists(usedQuestionsFile)) File.Create(usedQuestionsFile).Close();
        if (!File.Exists(questionReferenceFile)) File.Create(questionReferenceFile).Close();
        // EntryStorage creates its own file!
        // if (!File.Exists(entryFile)) File.Create(entryFile).Close();

        var result = new ServerStorage();
        result.UserStorage = new UserStorage(userDir);
        result.EntryStorage = new EntryStorage(entryFile);
        result.GeneralQuestionStorage = new GeneralQuestionStorage(questionReferenceFile);
        result.UniqueQuestionStorage = new UniqueQuestionStorage(result.GeneralQuestionStorage, usedQuestionsFile);
        return result;
    }


    public UserStorage UserStorage { get; private set; }
    public EntryStorage EntryStorage { get; private set; }
    public GeneralQuestionStorage GeneralQuestionStorage { get; private set; }
    public UniqueQuestionStorage UniqueQuestionStorage { get; private set; }


    /// <summary>
    /// Clears all stored data in the server directory. Does not clear questionFiles.
    /// </summary>
    public void Clear()
    {
        UserStorage.Clear();
        EntryStorage.Clear();
        UniqueQuestionStorage.Clear();
    }


    /// <summary>Can be called in a separate thread to the server running.</summary>
    /// <returns>False if loading a new question failed.
    /// Usually happens when we server ran out of unique question.</returns>
    public bool NewRandomEntry()
    {
        if (!UniqueQuestionStorage.TryGetRandom(out var question)) return false;

        var split = question.Split('\n');
        var entry = new EntryRecord
        (
            split[0],
            DateTimeOffset.Now,
            split.Skip(1)
                .SelectMany(s => s.Trim() == ":u" ? UserStorage.EnumerateUsers : [s])
                .Select(v => new VoteOptionRecord(v, []))
                .ToArray()
        );

        EntryStorage.AddLast(entry);
        return true;
    }


    /// <param name="whenLocal">At which local time should the ServerStorage automatically load a new random entry
    /// (See NewRandomEntry())?
    /// <para/>
    /// This member is exclusive to THIS ServerStorage instance in this runtime and will only create new entries as long as this
    /// instance is not disposed, other ServerStorages with the same directory will NOT generate new random entries.
    /// </param>
    public void DailyNewRandomEntry(TimeOnly whenLocal)
    {
        _dailyNewEntryTimes.Add(whenLocal);
        _dailyNewEntryRunningThreads.Add(new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(whenLocal - TimeOnly.FromDateTime(DateTime.Now));
                NewRandomEntry();
            }
        }));
        _dailyNewEntryRunningThreads[^1].Start();
    }


    public IEnumerable<TimeOnly> IterDailyNewRandomEntries()
    {
        return _dailyNewEntryTimes;
    }


    public void StopAllDailyNewRandomEntry()
    {
        foreach (var t in _dailyNewEntryRunningThreads)
        {
            // TODO: Read now thread interruption acts
            t.Interrupt();
        }
        _dailyNewEntryRunningThreads.Clear();
        _dailyNewEntryTimes.Clear();
    }


    public void Dispose()
    {
        StopAllDailyNewRandomEntry();
    }


    private ServerStorage()
    {
    }


    private readonly List<Thread> _dailyNewEntryRunningThreads = new();
    private readonly List<TimeOnly> _dailyNewEntryTimes = new();
}

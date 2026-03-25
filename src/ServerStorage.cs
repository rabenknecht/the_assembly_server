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
    /// <param name="questionFiles">The files used to fetch questions in the GeneralQuestionStorage.
    /// See GeneralQuestionStorage.cs for more info on question files.</param>
    /// <exception cref="ArgumentException">When the file and directory structure of fileStorage is invalid</exception>
    public ServerStorage(string directory, params string[] questionFiles)
    {
        var passDir = Path.Combine(directory, "passwords");
        var usedQuestionsFile = Path.Combine(directory, "usedQuestions");
        var entryFile = Path.Combine(directory, "entries");

        if (File.Exists(passDir)) throw new ArgumentException("Invalid fileStorage structure");
        if (Directory.Exists(usedQuestionsFile)) throw new ArgumentException("Invalid fileStorage structure");

        if (!Directory.Exists(passDir)) Directory.CreateDirectory(passDir);
        if (!File.Exists(usedQuestionsFile)) File.Create(usedQuestionsFile).Close();
        // EntryStorage automatically generates its file
        // if (!File.Exists(entryFile)) File.Create(entryFile).Close();

        UserStorage = new UserStorage(passDir);
        GeneralQuestionStorage = new GeneralQuestionStorage(questionFiles);
        UniqueQuestionStorage = new UniqueQuestionStorage(GeneralQuestionStorage, usedQuestionsFile);
        EntryStorage = new EntryStorage(entryFile);
    }


    public readonly UserStorage UserStorage;
    public readonly EntryStorage EntryStorage;
    public readonly GeneralQuestionStorage GeneralQuestionStorage;
    public readonly UniqueQuestionStorage UniqueQuestionStorage;


    /// <summary>
    /// Clears all stored data in the server directory. Does not clear questionFiles.
    /// </summary>
    public void ClearStorage()
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


    private readonly List<Thread> _dailyNewEntryRunningThreads = new();
    private readonly List<TimeOnly> _dailyNewEntryTimes = new();
}

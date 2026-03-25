namespace TheAssembly.Server;

public class ServerStorage
{
    public ServerStorage(string fileStorage, params string[] questionFiles)
    {
        var passDir = Path.Combine(fileStorage, "passwords");
        var usedQuestionsFile = Path.Combine(fileStorage, "usedQuestions");
        var entryFile = Path.Combine(fileStorage, "entries");

        if (File.Exists(passDir)) throw new ArgumentException("Invalid fileStorage structure");
        if (Directory.Exists(usedQuestionsFile)) throw new ArgumentException("Invalid fileStorage structure");

        if (!Directory.Exists(passDir)) Directory.CreateDirectory(passDir);
        if (!File.Exists(usedQuestionsFile)) File.Create(usedQuestionsFile).Close();
        // EntryStorage automatically generates its file
        // if (!File.Exists(entryFile)) File.Create(entryFile).Close();

        PassStorage = new PassStorage(passDir);
        QuestionStorage = new QuestionStorage(questionFiles);
        QuestionGetter = new UniqueQuestionGetter(QuestionStorage, usedQuestionsFile);
        EntryStorage = new EntryStorage(entryFile);
    }


    public readonly PassStorage PassStorage;
    public readonly EntryStorage EntryStorage;
    public readonly QuestionStorage QuestionStorage;
    public readonly UniqueQuestionGetter QuestionGetter;


    /// <summary>
    /// Clears all stored data in the server directory. Does not clear questionFiles.
    /// </summary>
    public void ClearStorage()
    {
        PassStorage.Clear();
        EntryStorage.Clear();
        QuestionGetter.Clear();
    }


    /// <summary>Can be called in a separate thread to the server running.</summary>
    /// <returns>False if loading a new question failed.
    /// Usually happens when we server ran out of unique question.</returns>
    public bool NewRandomEntry()
    {
        if (!QuestionGetter.TryGetRandom(out var question)) return false;

        var split = question.Split('\n');
        var entry = new EntryRecord
        (
            split[0],
            DateTimeOffset.Now,
            split.Skip(1)
                .SelectMany(s => s.Trim() == ":u" ? PassStorage.EnumerateUsers : [s])
                .Select(v => new VoteOptionRecord(v, []))
                .ToArray()
        );

        EntryStorage.AddLast(entry);
        return true;
    }


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


    private readonly List<Thread> _dailyNewEntryRunningThreads = new();
    private readonly List<TimeOnly> _dailyNewEntryTimes = new();
}

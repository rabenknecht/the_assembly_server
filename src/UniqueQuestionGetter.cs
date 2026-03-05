namespace TheAssembly.Server;

internal class UniqueQuestionGetter
{
    /// <param name="storage">From where to fetch questions.</param>
    /// <param name="filePath">The file is needed to store what questions are already used or still available.</param>
    public UniqueQuestionGetter(QuestionStorage storage, string filePath)
    {
        _storage = storage;
        _filePath = filePath;
        _alreadyUsed = new List<int>(File.ReadAllBytes(filePath).BytesToInts());
    }


    public string GetRandom()
    {
        var index = Random.Shared.Next(_storage.QuestionCount - _alreadyUsed.Count);
        return GetIgnoringAlreadyUsed(index);
    }


    private string GetIgnoringAlreadyUsed(int index)
    {
        var usedBefore = _alreadyUsed.Count(i => i <= index);
        var actualIndex = index + usedBefore;

        _alreadyUsed.Add(actualIndex);
        File.AppendAllBytes(_filePath, BitConverter.GetBytes(actualIndex));

        return _storage[actualIndex];
    }


    private readonly string _filePath;
    private readonly QuestionStorage _storage;
    private readonly List<int> _alreadyUsed;
}

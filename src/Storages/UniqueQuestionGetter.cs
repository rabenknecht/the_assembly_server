using TheAssembly.Core;

namespace TheAssembly.Server;

internal class UniqueQuestionGetter
{
    /// <param name="storage">From where to fetch questions.</param>
    /// <param name="filePath">The file is needed to store what questions are already used or still available.</param>
    public UniqueQuestionGetter(QuestionStorage storage, string filePath)
    {
        _storage = storage;
        FilePath = filePath;

        var alreadyUsedFileBytes = File.ReadAllBytes(filePath);
        if ((alreadyUsedFileBytes.Length & 0b111) != 0) // divisible by 8?
        {
            throw new ArgumentException("Incorrect byte count of used questions file (must be divisible through 8)", nameof(filePath));
        }

        _alreadyUsed = new List<int>[_storage.FileCount];
        for (var i = 0; i < _alreadyUsed.Length; i++)
        {
            _alreadyUsed[i] = [];
        }

        foreach (var (fileIndex, questionIndex) in alreadyUsedFileBytes.BytesToInts().Group2())
        {
            if (fileIndex < 0 || fileIndex >= _storage.FileCount
                || questionIndex < 0 || questionIndex >= _storage.QuestionCount(fileIndex))
            {
                Console.WriteLine($"The used questions file appears to indicate a used question out of bounds. Ignoring it. "
                    + $"fileIndex:{fileIndex}/{_storage.FileCount}, "
                    + $"questionIndex:{questionIndex}/{_storage.QuestionCountOr(fileIndex, -1)}");
            }
            else
            {
                _alreadyUsed[fileIndex].Add(questionIndex);
            }
        }
    }


    public readonly string FilePath;


    /// <returns>If a unique random question could be fetched from the storage.
    /// This usually fails when all questions are already used!</returns>
    public bool TryGetRandom(out string result)
    {
        if (TotalUsedQuestions == _storage.TotalQuestionCount)
        {
            result = null!;
            return false;
        }

        // I know this means that not every question has the
        // exact same chance of appearing, and I don't care. This is good enough
        var unusedFile = Random.Shared.Next(_storage.FileCount - AlreadyUsedFiles().Count());
        var actualFile = UnusedToActualFile(unusedFile);
        var unusedQuestion = Random.Shared.Next(_storage.QuestionCount(unusedFile) - _alreadyUsed[actualFile].Count);
        var actualQuestion = UnusedToActualQuestion(unusedQuestion, actualFile);

        _alreadyUsed[actualFile].Add(actualQuestion);
        File.AppendAllBytes(FilePath, BitConverter.GetBytes(actualFile));
        File.AppendAllBytes(FilePath, BitConverter.GetBytes(actualQuestion));

        result = _storage.GetQuestion(actualFile, actualQuestion);
        return true;
    }


    private IEnumerable<int> AlreadyUsedFiles()
    {
        return Enumerable.Range(0, _alreadyUsed.Length).Where(i => _alreadyUsed[i].Count == _storage.QuestionCount(i));
    }


    private int UnusedToActualFile(int unusedFileIndex)
    {
        foreach (var i in AlreadyUsedFiles())
        {
            if (i <= unusedFileIndex)
            {
                unusedFileIndex++;
            }
        }
        return unusedFileIndex;
    }


    private int UnusedToActualQuestion(int unusedQuestionIndex, int actualFileIndex)
    {
        _alreadyUsed[actualFileIndex].Sort();
        foreach (var i in _alreadyUsed[actualFileIndex])
        {
            if (i <= unusedQuestionIndex)
            {
                unusedQuestionIndex++;
            }
        }
        return unusedQuestionIndex;
    }


    private int TotalUsedQuestions => _alreadyUsed.Sum(x => x.Count);


    private readonly QuestionStorage _storage;
    // _alreadyUsed[fileIndex] yields the used questionIndices for that specified file.
    private readonly List<int>[] _alreadyUsed;
}

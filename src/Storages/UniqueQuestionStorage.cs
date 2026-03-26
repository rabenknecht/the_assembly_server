using TheAssembly.Core;

namespace TheAssembly.Server;

/// <summary>
/// Tracks which questions have already been used in a file, therefore allowing access
/// to used or unused questions.
/// </summary>
public class UniqueQuestionStorage
{
    /// <param name="storage">From where to fetch questions.</param>
    /// <param name="filePath">The file is needed to store what questions are already used or still available.</param>
    public UniqueQuestionStorage(GeneralQuestionStorage storage, string filePath)
    {
        _storage = storage;
        FilePath = filePath;
    }


    public readonly string FilePath;


    /// <returns>If a unique random question could be fetched from the storage.
    /// This usually fails when all questions are already used!</returns>
    public bool TryGetRandom(out string result)
    {
        if (UsedQuestionsCount == _storage.TotalQuestionCount)
        {
            result = null!;
            return false;
        }

        // I know this means that not every question has the
        // exact same chance of appearing, and I don't care. This is good enough
        var unusedFile = Random.Shared.Next(_storage.FileCount - EnumAlreadyUsedFilesIndices().Count());
        var actualFile = UnusedToActualFile(unusedFile);
        var unusedQuestion = Random.Shared.Next(_storage.QuestionCount(unusedFile) - GetAlreadyUsed()[actualFile].Count);
        var actualQuestion = UnusedToActualQuestion(unusedQuestion, actualFile);

        File.AppendAllBytes(FilePath, BitConverter.GetBytes(actualFile));
        File.AppendAllBytes(FilePath, BitConverter.GetBytes(actualQuestion));

        result = _storage.GetQuestion(actualFile, actualQuestion);
        return true;
    }


    public int UsedQuestionsCount
    {
        get
        {
            return GetAlreadyUsed().Sum(x => x.Count);
        }
    }


    public int UnusedQuestionsCount => _storage.TotalQuestionCount - UsedQuestionsCount;

    public void Clear()
    {
        File.WriteAllBytes(FilePath, []);
    }


    private IEnumerable<int> EnumAlreadyUsedFilesIndices()
    {
        return Enumerable.Range(0, GetAlreadyUsed().Length).Where(i => GetAlreadyUsed()[i].Count == _storage.QuestionCount(i));
    }


    private int UnusedToActualFile(int unusedFileIndex)
    {
        foreach (var i in EnumAlreadyUsedFilesIndices())
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
        GetAlreadyUsed()[actualFileIndex].Sort();
        foreach (var i in GetAlreadyUsed()[actualFileIndex])
        {
            if (i <= unusedQuestionIndex)
            {
                unusedQuestionIndex++;
            }
        }
        return unusedQuestionIndex;
    }


    /// <summary>
    /// Ensures that the alreadyUsed array is always up to date.
    /// <para/>
    /// _alreadyUsed[fileIndex] yields the used questionIndices for that specified file.
    /// </summary>
    private List<int>[] GetAlreadyUsed()
    {
        var newLastAlreadyUsedWrite = File.GetLastWriteTime(FilePath);
        if (newLastAlreadyUsedWrite == _lastAlreadyUsedWrite)
        {
            return _alreadyUsed;
        }
        else
        {
            _lastAlreadyUsedWrite = newLastAlreadyUsedWrite;

            var alreadyUsedFileBytes = File.ReadAllBytes(FilePath);
            if ((alreadyUsedFileBytes.Length & 0b111) != 0) // divisible by 8?
            {
                throw new InvalidOperationException("Incorrect byte count of UsedQuestionsFile! (must be divisible through 8)");
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
                    // TODO: Better handle this, if we ever impl question file deletion in GeneralQuestionStorage, this does enable weird bugs...
                    Console.WriteLine($"The used questions file appears to indicate a used question out of bounds. Ignoring it. "
                        + $"fileIndex:{fileIndex}/{_storage.FileCount}, "
                        + $"questionIndex:{questionIndex}/{_storage.QuestionCountOr(fileIndex, -1)}");
                }
                else
                {
                    _alreadyUsed[fileIndex].Add(questionIndex);
                }
            }

            return _alreadyUsed;
        }
    }


    private readonly GeneralQuestionStorage _storage;
    // Use GetAlreadyUsed to access this instead to ensure this stays updated!
    private List<int>[] _alreadyUsed = [];
    private DateTime? _lastAlreadyUsedWrite;
}

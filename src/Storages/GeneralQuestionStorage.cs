using System.Text;

namespace TheAssembly.Server;

// The format of a questionFile is as follows:
// QUESTION1?
// VOTEOPTION1
// VOTEOPTION2
// ...
// LASTVOTEOPTION
//
// QUESTION2?
// VOTEOPTION1
// ...
// LASTVOTEOPTION

// Use ":u" as a vote option to insert the users of the storage that uses said question.
// All members are trimmed before use
// More than 2 linebreaks to split questions breaks this storage.

/// <summary>
/// Storage used to access individual questions via indexing of multiple files and the questions they contain.
/// <para/>
/// Question files can have new questions added outside this application by appending them to any used question file.
/// <para/>
/// Question files can have their questions modified outside this application. Modifications will not affect existing entries.
/// <para/>
/// Question files cannot be removed outside this application without breaking the UniqueQuestionStorage.
/// </summary>
public class GeneralQuestionStorage
{
    // TODO: Allow for clearing of the registered questions!

    /// <param name="referenceFile">The reference file is used to persistently load and save
    /// the references to the questionFiles loaded into this GeneralQuestionStorage in order
    /// of their indices.</param>
    public GeneralQuestionStorage(string referenceFile)
    {
        if (!File.Exists(referenceFile))
        {
            throw new ArgumentException("Passed referenceFile path does not point to a file!");
        }

        _referenceFile = referenceFile;
    }


    public IEnumerable<string> FilePaths => GetSingleFiles().Select(f => f.FilePath);


    public int FileCount => GetSingleFiles().Count;


    public int QuestionCount(int fileIndex)
    {
        if (fileIndex < 0 || fileIndex >= FileCount)
        {
            throw new IndexOutOfRangeException();
        }

        return GetSingleFiles()[fileIndex].QuestionCount;
    }


    public int QuestionCountOr(int fileIndex, int or)
    {
        if (fileIndex < 0 || fileIndex >= FileCount)
        {
            return or;
        }

        return GetSingleFiles()[fileIndex].QuestionCount;
    }


    public int TotalQuestionCount => GetSingleFiles().Sum(f => f.QuestionCount);


    public string GetQuestion(int fileIndex, int questionIndex)
    {
        if (fileIndex < 0 || fileIndex >= FileCount
            || questionIndex < 0 || questionIndex >= QuestionCount(fileIndex))
        {
            throw new IndexOutOfRangeException();
        }

        return GetSingleFiles()[fileIndex][questionIndex];
    }


    public bool TryRegisterQuestionFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        File.AppendAllText(_referenceFile, $"{filePath}\n");
        return true;
    }


    private IList<SingleFile> GetSingleFiles()
    {
        var newLastReferenceFileWrite = File.GetLastWriteTime(_referenceFile);
        if (newLastReferenceFileWrite != _lastReferenceFileWrite)
        {
            _lastReferenceFileWrite = newLastReferenceFileWrite;

            _bufferedSingleFiles = File.ReadAllText(_referenceFile)
                .Split("\n")[..^1] // Last line will always be empty
                .Select(f => new SingleFile(f))
                .ToArray();
        }

        return _bufferedSingleFiles;
    }


    private readonly string _referenceFile;
    // _lastRefFileWrite == null => This will immediately get updated in GetSingleFiles => Not null
    private IList<SingleFile> _bufferedSingleFiles = null!;
    private DateTime? _lastReferenceFileWrite;


    private class SingleFile
    {
        public SingleFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Passed file does not exist");
            FilePath = filePath;
        }


        public int QuestionCount
        {
            get
            {
                UpdateBuffers();
                return _bufferedQuestions.Length;
            }
        }


        public string this[int index]
        {
            get
            {
                UpdateBuffers();
                if (index < 0 || index >= _bufferedQuestions.Length) throw new IndexOutOfRangeException();
                return _bufferedQuestions[index];
            }
        }


        // Buffering makes sense:
        // Loading new Questions happens rarely => This instance gets replaced rarely
        // Additionally, new questions will also be added rarely
        private void UpdateBuffers()
        {
            var lastWrite = File.GetLastWriteTime(FilePath);
            if (lastWrite == _bufferedSinceLastWrite) return;

            _bufferedSinceLastWrite = lastWrite;
            var questionLines = File.ReadAllLines(FilePath);
            var formattedQuestions = questionLines
                .Select(s => s.Trim(' ', '\t'))
                .Aggregate(new StringBuilder(), (l, r) => l.AppendLine(r))
                .ToString();
            _bufferedQuestions = formattedQuestions.Trim('\n').Split("\n\n");
        }


        public readonly string FilePath;
        private DateTime? _bufferedSinceLastWrite;
        private string[] _bufferedQuestions = null!;
    }
}

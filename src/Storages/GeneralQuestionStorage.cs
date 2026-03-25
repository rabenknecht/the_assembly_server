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
    public GeneralQuestionStorage(IEnumerable<string> filePaths)
    {
        _singleFiles = filePaths.Select(p => new SingleFile(p)).ToArray();
    }


    public IEnumerable<string> FilePaths => _singleFiles.Select(f => f.FilePath);


    public int FileCount => _singleFiles.Length;


    public int QuestionCount(int fileIndex)
    {
        if (fileIndex < 0 || fileIndex >= FileCount) throw new IndexOutOfRangeException();
        return _singleFiles[fileIndex].QuestionCount;
    }


    public int QuestionCountOr(int fileIndex, int or)
    {
        if (fileIndex < 0 || fileIndex >= FileCount) return or;
        return _singleFiles[fileIndex].QuestionCount;
    }


    public int TotalQuestionCount => _singleFiles.Sum(f => f.QuestionCount);


    public string GetQuestion(int fileIndex, int questionIndex)
    {
        if (fileIndex < 0 || fileIndex >= FileCount
            || questionIndex < 0 || questionIndex >= QuestionCount(fileIndex))
            throw new IndexOutOfRangeException();

        return _singleFiles[fileIndex][questionIndex];
    }


    private SingleFile[] _singleFiles;


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

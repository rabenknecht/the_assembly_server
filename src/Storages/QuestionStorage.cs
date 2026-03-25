using System.Text;

namespace TheAssembly.Server;

/// <summary>
/// THIS CLASS DOES NOT SUPPORT MULTITHREADED ACCESSING!!!!!
/// </summary>
public class QuestionStorage
{
    public QuestionStorage(IEnumerable<string> filePaths)
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

using System.Text;

namespace TheAssembly.Server;

/// <summary>
/// THIS CLASS DOES NOT SUPPORT MULTITHREADED ACCESSING!!!!!
/// </summary>
internal class QuestionStorage
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


    /// <returns>If the resulting file and questionIndex are in bounds of the QuestionStorage</returns>
    public bool TotalToFileQuestionIndex(int totalIndex, out int fileIndex, out int questionIndex)
    {
        if (totalIndex < 0)
        {
            fileIndex = -1;
            questionIndex = (int) totalIndex;
            return false;
        }

        foreach (var (i, f) in _singleFiles.Index())
        {
            if (totalIndex >= f.QuestionCount)
                totalIndex -= f.QuestionCount;
            else
            {
                fileIndex = i;
                questionIndex = totalIndex;
                return true;
            }
        }

        fileIndex = FileCount;
        questionIndex = totalIndex;
        return false;
    }


    private SingleFile[] _singleFiles;


    private class SingleFile
    {
        public SingleFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Passed file does not exist");

            FilePath = filePath;
            using var stream = File.OpenRead(filePath);
            _fileEndExclusive = 0;
            _questions = [];
        }


        public readonly string FilePath;


        public int QuestionCount
        {
            get
            {
                LoadQuestions(_fileEndExclusive);
                return _questions.Count;
            }
        }


        public string this[int index]
        {
            get
            {
                using var stream = File.OpenRead(FilePath);
                stream.Seek(_fileEndExclusive, SeekOrigin.Begin);
                LoadQuestions(stream);

                var (start, end) = _questions[index];
                var length = (int) (end - start); // Guranteed to be <=_buffer.Count
                stream.Seek(start, SeekOrigin.Begin);
                stream.ReadExactly(_buffer, 0, length);
                return Encoding.UTF8.GetString(_buffer, 0, length);
            }
        }

        private void LoadQuestions(long pointerStart)
        {
            using var stream = File.OpenRead(FilePath);
            stream.Seek(pointerStart, SeekOrigin.Begin);
            LoadQuestions(stream);
        }


        /// <summary>Passed Stream expected to be disposed externally.</summary>
        private void LoadQuestions(FileStream readStream)
        {
            // Skip over all linebreaks, they break implementation below
            // They also allow questions to be separated by 2 or more linebreaks!
            int b;
            while ((b = readStream.ReadByte()) == '\n') ;

            int prevByte = readStream.ReadByte();
            if (prevByte == -1)
            {
                _fileEndExclusive = readStream.Position - 1;
                return;
            }

            int curByte = readStream.ReadByte();
            long startInclusive = readStream.Position - 3;
            long endExclusive;

            while (true)
            {
                if (curByte == -1)
                {
                    endExclusive = readStream.Position - 1;
                    // Setting _fileEndExclusive redundant: next iteration will update it
                    break;
                }

                if (curByte == '\n' && prevByte == '\n')
                {
                    endExclusive = readStream.Position - 2;
                    // Setting _fileEndExclusive redundant: next iteration will update it
                    break;
                }

                prevByte = curByte;
                curByte = readStream.ReadByte();
            }

            // We just ignore questions that are too long for the buffer. Who the fuck creates a 1024 symbol long question!?
            if ((endExclusive - startInclusive) <= _buffer.Length) _questions.Add((startInclusive, endExclusive));
            else Console.WriteLine("Detected a question that is longer than the buffer to read it. Ignoring it. "
                + "Hit Rabenknecht so they actually include metadata for the question in question"); // TODO

            LoadQuestions(readStream);
        }


        private long _fileEndExclusive;
        private List<(long startInclusive, long endExclusive)> _questions;
        private static byte[] _buffer = new byte[1024];
    }
}

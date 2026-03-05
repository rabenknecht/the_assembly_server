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


    public int QuestionCount => _singleFiles.Sum(f => f.QuestionCount);


    public string this[int index]
    {
        get
        {
            if (index < 0 || index >= QuestionCount) throw new IndexOutOfRangeException();

            foreach (var f in _singleFiles)
            {
                if (f.QuestionCount > index) return f[index];
                else index -= f.QuestionCount;
            }

            throw new Exception("SHOULD NEVER HAPPEN");
        }
    }


    private SingleFile[] _singleFiles;


    private class SingleFile
    {
        public SingleFile(string filePath)
        {
            _filePath = filePath;
            using var stream = File.OpenRead(filePath);
            _fileEndExclusive = stream.Length;
            _questions = [];
        }


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
                using var stream = File.OpenRead(_filePath);
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
            using var stream = File.OpenRead(_filePath);
            stream.Seek(pointerStart, SeekOrigin.Begin);
            LoadQuestions(stream);
        }


        /// <summary>Passed Stream expected to be disposed externally.</summary>
        private void LoadQuestions(FileStream readStream)
        {
            // Skip over all linebreaks, they break implementation below
            // They also allow questions to be separated by 2 or more linebreaks!
            int b;
            while ((b = readStream.ReadByte()) != '\n') ;

            long startInclusive = readStream.Position;
            long endExclusive;

            int prevByte = readStream.ReadByte();
            if (prevByte == -1) return;
            int curByte = readStream.ReadByte();

            while (true)
            {
                if (curByte == -1)
                {
                    endExclusive = readStream.Position;
                    break;
                }

                if (curByte == '\n' && prevByte == '\n')
                {
                    endExclusive = readStream.Position - 1;
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


        private readonly string _filePath;
        private long _fileEndExclusive;
        private List<(long startInclusive, long endExclusive)> _questions;
        private static byte[] _buffer = new byte[1024];
    }
}

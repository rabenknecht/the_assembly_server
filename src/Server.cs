using System.Net;
using System.Text;

namespace TheAssembly.Server;


public class Server
{
    public Server(string url, string fileStorage, params string[] questionFiles) : this([url], fileStorage, questionFiles) {}


    public Server(IEnumerable<string> urls, string fileStorage, IEnumerable<string> questionFiles)
    {
        _listener = new HttpListener();
        // I have no fucking idea how HttpListeners actually authenticate Basic.
        // It always ends up forbidding every connection, so we just authenticate
        // the user ourselves
        _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
        _urls = [.. urls];

        var passDir = Path.Combine(fileStorage, "passwords");
        var usedQuestionsFile = Path.Combine(fileStorage, "usedQuestions");
        if (File.Exists(passDir)) throw new ArgumentException("Invalid fileStorage structure");
        if (Directory.Exists(usedQuestionsFile)) throw new ArgumentException("Invalid fileStorage structure");
        if (!Directory.Exists(passDir)) Directory.CreateDirectory(passDir);
        if (!File.Exists(usedQuestionsFile)) File.Create(usedQuestionsFile).Close();

        _passStorage = new PassStorage(passDir);
        _questionStorage = new QuestionStorage(questionFiles);
        _questionGetter = new UniqueQuestionGetter(_questionStorage, usedQuestionsFile);
    }


    public void RunAsync()
    {
        var t = new Thread(RunForever);
        t.Name = "Server Worker";
        t.Start();
    }


    /// <summary>
    /// NEVER RETURNS!
    /// </summary>
    /// <exception cref="InvalidOperationException">If the server is already running</exception>
    public void RunForever()
    {
        if (_listener.IsListening) throw new InvalidOperationException("Server is already running!");

        foreach (var url in _urls) _listener.Prefixes.Add(url);
        _listener.Start();
        Console.WriteLine("UserServer started. Access me with the following URLs " + string.Join(", ", _listener.Prefixes));

        while (true)
        {
            // We will multithread at a later point
            var context = _listener.GetContext();
            var request = context.Request;
            var response = context.Response;
            var authUser = GetAuthenticatedUser(request);
            var requestContent = ExtractRequestContent(request);

            // users.POST
            if (request.HttpMethod == "POST"
                && CheckLocalRequestUrl(request, "users")
                && requestContent.TryJsonDeserialize<JoinRecord>(out var joinRecord)
                && _passStorage.CanBeNew(joinRecord.user))
            {
                _passStorage.Update(joinRecord.user, joinRecord.password);
                response.StatusCode = (int) HttpStatusCode.OK;
            }

            // users.GET
            else if (request.HttpMethod == "GET"
                && CheckLocalRequestUrl(request, "users")
                && authUser != null)
            {
                var responseString = string.Join('\n', _passStorage.EnumerateUsers);

                response.StatusCode = (int) HttpStatusCode.OK;
                response.OutputStream.Write(Encoding.UTF8.GetBytes(responseString));
            }

            else
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }

            response.Close();
        }
    }


    public bool IsRunning() => _listener.IsListening;


    public void StopIfRunning()
    {
        if (IsRunning()) _listener.Stop();
    }


    /// <summary>
    /// Clears all stored data.
    /// </summary>
    public void ClearStorage()
    {
        _passStorage.Clear();
    }


    /// <summary>
    /// Should be called when questions have been moved or removed from the questionFiles.
    /// Also clears the usedQuestionsFile.
    /// </summary>
    public void ReloadQuestions()
    {
        _questionStorage = new QuestionStorage(_questionStorage.FilePaths);
        File.WriteAllBytes(_questionGetter.FilePath, []);
        _questionGetter = new UniqueQuestionGetter(_questionStorage, _questionGetter.FilePath);
    }


    /// <returns>False if loading a new question failed.
    /// Usually happens when we server ran out of unique question.</returns>
    public bool NewRandomEntry()
    {
        if (!_questionGetter.TryGetRandom(out var question)) return false;

        // TODO: Save the new entry!
        return true;
    }


    /// <summary>
    /// Returns the authenticated user of the request.
    /// </summary>
    private string? GetAuthenticatedUser(HttpListenerRequest request)
    {
        var authHeader = request.Headers[nameof(HttpRequestHeader.Authorization)];
        if (authHeader == null) return null;
        if (!authHeader.TryBasicAuthHeaderToUserPass(out var user, out var pass)) return null;
        if (!_passStorage.CorrectOrNoPass(user, pass)) return null;
        return user;
    }


    private string? ExtractRequestContent(HttpListenerRequest request)
    {
        if (!request.HasEntityBody) return null;
        using var stream = request.InputStream;
        using var reader = new StreamReader(stream, request.ContentEncoding);
        return reader.ReadToEnd();
    }


    private bool CheckLocalRequestUrl(HttpListenerRequest request, string localUrl)
    {
        return _urls.Any(u =>
        {
            var expected = $"{u}{localUrl}";
            return expected == request.Url?.AbsoluteUri;
        });
    }


    private readonly HttpListener _listener;
    private readonly IReadOnlyCollection<string> _urls;
    private readonly PassStorage _passStorage;
    private QuestionStorage _questionStorage;
    private UniqueQuestionGetter _questionGetter;
}

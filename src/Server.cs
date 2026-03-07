using System.Net;
using System.Text;

namespace TheAssembly.Server;


public class Server
{
    public Server(string url, string fileStorage, params string[] questionFiles) : this([url], fileStorage, questionFiles) {}


    public Server(IEnumerable<string> urls, string fileStorage, IEnumerable<string> questionFiles)
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


        _passStorage = new PassStorage(passDir);
        _questionStorage = new QuestionStorage(questionFiles);
        _questionGetter = new UniqueQuestionGetter(_questionStorage, usedQuestionsFile);
        _entryStorage = new EntryStorage(entryFile);


        _listener = new HttpListener();
        // I have no fucking idea how HttpListeners actually authenticate Basic.
        // It always ends up forbidding every connection, so we just authenticate
        // the user ourselves
        _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
        _urls = [.. urls];
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
        Console.WriteLine("Server started. Access me with the following URLs: " 
            + string.Join(", ", _listener.Prefixes.Select(s => $"\"{s}\"")));

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
            else if (authUser != null
                && request.HttpMethod == "GET"
                && CheckLocalRequestUrl(request, "users"))
            {
                var responseString = string.Join('\n', _passStorage.EnumerateUsers);

                response.StatusCode = (int) HttpStatusCode.OK;
                response.OutputStream.Write(Encoding.UTF8.GetBytes(responseString));
            }

            // entry/current.GET
            else if (authUser != null
                && request.HttpMethod == "GET"
                && CheckLocalRequestUrl(request, "entry/current")
                && _entryStorage.TryGetLastJson(out var entryJson))
            {
                response.StatusCode = (int) HttpStatusCode.OK;
                response.OutputStream.Write(Encoding.UTF8.GetBytes(entryJson));
            }

            // entry.GET
            else if (authUser != null
                && request.HttpMethod == "GET"
                && CheckLocalRequestUrl(request, "entry"))
            {
                var entriesJson = _entryStorage.GetAllExceptLastJson();
                response.StatusCode = (int) HttpStatusCode.OK;
                response.OutputStream.Write(Encoding.UTF8.GetBytes(entriesJson));
            }

            else if (authUser != null
                && request.HttpMethod == "POST"
                && CheckLocalRequestUrl(request, "entry/vote")
                && !_entryStorage.IsEmpty)
            {
                var currentEntry = _entryStorage.GetLast()!;
                var toVote = currentEntry.voteOptions!.FirstOrDefault(v => v.votingWhat == requestContent);
                if (toVote == null)
                {
                    response.StatusCode = (int) HttpStatusCode.NotFound;
                }
                else
                {
                    RemoveOldVotes(authUser, currentEntry);
                    toVote.votedBy = toVote.votedBy!.Add(authUser);
                    _entryStorage.UpdateLast(currentEntry);
                    response.StatusCode = (int) HttpStatusCode.OK;
                }
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
    /// Clears all stored data in the server directory.
    /// </summary>
    public void ClearStorage()
    {
        _passStorage.Clear();
        _entryStorage.Clear();
        _questionGetter.Clear();
    }


    /// <param name="endsWhen">Defaults to max time.</param>
    /// <returns>False if loading a new question failed.
    /// Usually happens when we server ran out of unique question.</returns>
    public bool NewRandomEntry(DateTimeOffset? endsWhen = null)
    {
        if (!_questionGetter.TryGetRandom(out var question)) return false;

        var split = question.Split('\n');
        var entry = new EntryRecord
        (
            split[0],
            DateTimeOffset.Now,
            split.Skip(1)
                .SelectMany(s => s.Trim() == ":u" ? _passStorage.EnumerateUsers : [s])
                .Select(v => new VoteOptionRecord(v, []))
                .ToArray()
        );

        _entryStorage.AddLast(entry);
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


    private void RemoveOldVotes(string ofUser, EntryRecord inEntry)
    {
        foreach (var voteOption in inEntry.voteOptions!)
        {
            voteOption.votedBy = voteOption.votedBy!
                .Where(s => s != ofUser)
                .ToArray();
        }
    }


    private readonly HttpListener _listener;
    private readonly IReadOnlyCollection<string> _urls;
    private readonly PassStorage _passStorage;
    private QuestionStorage _questionStorage;
    private UniqueQuestionGetter _questionGetter;
    private EntryStorage _entryStorage;
}

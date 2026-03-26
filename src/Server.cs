using System.Net;
using System.Text;
using TheAssembly.Core;

namespace TheAssembly.Server;


// TODO: Remove logging

public class Server
{
    public Server(string url, ServerStorage storage, bool shouldLog = false)
        : this([url], storage, shouldLog)
    {
    }


    public Server(IEnumerable<string> urls, ServerStorage storage, bool shouldLog = false)
    {
        _storage = storage;
        _shouldLog = shouldLog;
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
            string responseContent = "";

            if (_shouldLog)
            {
                Console.WriteLine($"[{DateTime.Now}] [IN] url:{request.Url?.AbsoluteUri} method:{request.HttpMethod} "
                    + $"authUser:{authUser} requestContent:{requestContent?.Replace("\n", "\\n")}");
            }

            // users.POST
            if (request.HttpMethod == "POST"
                && CheckLocalRequestUrl(request, "users")
                && requestContent.TryJsonDeserialize<JoinRecord>(out var joinRecord)
                && _storage.UserStorage.Add(joinRecord.user, joinRecord.password) != UserStorage.Error.None)
            {
                response.StatusCode = (int) HttpStatusCode.OK;
            }

            // users.GET
            else if (authUser != null
                && request.HttpMethod == "GET"
                && CheckLocalRequestUrl(request, "users"))
            {
                responseContent += string.Join('\n', _storage.UserStorage.EnumerateUsers);
                response.StatusCode = (int) HttpStatusCode.OK;
            }

            // entry/current.GET
            else if (authUser != null
                && request.HttpMethod == "GET"
                && CheckLocalRequestUrl(request, "entry/current")
                && _storage.EntryStorage.TryGetLastJson(out var entryJson))
            {
                response.StatusCode = (int) HttpStatusCode.OK;
                responseContent += entryJson;
            }

            // entry.GET
            else if (authUser != null
                && request.HttpMethod == "GET"
                && CheckLocalRequestUrl(request, "entry"))
            {
                response.StatusCode = (int) HttpStatusCode.OK;
                responseContent += _storage.EntryStorage.GetAllExceptLastJson();
            }

            // entry.vote.POST
            else if (authUser != null
                && request.HttpMethod == "POST"
                && CheckLocalRequestUrl(request, "entry/vote")
                && !_storage.EntryStorage.IsEmpty)
            {
                var currentEntry = _storage.EntryStorage.GetLast()!;
                var toVote = currentEntry.voteOptions!.FirstOrDefault(v => v.votingWhat == requestContent);
                if (toVote == null)
                {
                    response.StatusCode = (int) HttpStatusCode.NotFound;
                }
                else
                {
                    RemoveOldVotes(authUser, currentEntry);
                    toVote.votedBy = toVote.votedBy!.Add(authUser);
                    _storage.EntryStorage.UpdateLast(currentEntry);
                    response.StatusCode = (int) HttpStatusCode.OK;
                }
            }

            else
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }

            if (_shouldLog)
            {
                Console.WriteLine($"[{DateTime.Now}] [OUT] statusCode:{response.StatusCode} "
                    + $"responseContent:{responseContent.Replace("\n", "\\n")}");
            }

            response.OutputStream.Write(Encoding.UTF8.GetBytes(responseContent));
            response.Close();
        }
    }


    public bool IsRunning() => _listener.IsListening;


    public void StopIfRunning()
    {
        if (IsRunning()) _listener.Stop();
    }


    /// <summary>
    /// Returns the authenticated user of the request.
    /// </summary>
    private string? GetAuthenticatedUser(HttpListenerRequest request)
    {
        var authHeader = request.Headers[nameof(HttpRequestHeader.Authorization)];
        if (authHeader == null) return null;
        if (!authHeader.TryBasicAuthHeaderToUserPass(out var user, out var pass)) return null;
        if (!_storage.UserStorage.Correct(user, pass)) return null;
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


    private readonly bool _shouldLog;
    private readonly HttpListener _listener;
    private readonly IReadOnlyCollection<string> _urls;
    private readonly ServerStorage _storage;
}

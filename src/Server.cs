using System.Net;
using System.Text;

namespace TheAssembly.Server;


public class Server
{
    public Server(string url, string fileStorage) : this([url], fileStorage) {}


    public Server(IEnumerable<string> urls, string fileStorage)
    {
        _listener = new HttpListener();
        // I have no fucking idea how HttpListeners actually authenticate Basic.
        // It always ends up forbidding every connection, so we just authenticate
        // the user ourselves
        _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
        _urls = [.. urls];

        if (!Directory.Exists(fileStorage)) Directory.CreateDirectory(fileStorage);
        _passStorage = new PassStorage(fileStorage);
    }


    public void RunAsync()
    {
        new Thread(RunForever).Start();
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
    private readonly PassStorage _passStorage;
    private readonly IReadOnlyCollection<string> _urls;
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace TheAssembly.Server;


public class Server
{
    public Server(string url, string fileStorage) : this([url], fileStorage) {}


    public Server(IEnumerable<string> urls, string fileStorage)
    {
        _listener = new HttpListener();
        _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
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
            var authUser = IsAuthenticatedAs(request);

            var localPath = request.Url?.AbsolutePath[1..].ToLower().Split("/") ?? [];
            var requestMethod = request.HttpMethod.ToLower();

            if (localPath == null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }

            else if (requestMethod == "post"
                && localPath.Length == 1 && localPath[0] == "users"
                && request.InputStream.TryJsonDeserialize(out JoinRecord joinRecord)
                && IsUsernameLegal(joinRecord.User)
                && !_passStorage.HasPass(joinRecord.User))
            {
                _passStorage.Update(joinRecord.User, joinRecord.Password);
                response.StatusCode = (int) HttpStatusCode.OK;
            }

            else if (requestMethod == "get"
                && localPath.Length == 1 && localPath[0] == "users"
                && authUser != null)
            {
                response.StatusCode = (int) HttpStatusCode.OK;
                response.OutputStream.Write(
                    _passStorage.Users
                        .SelectMany(s => Encoding.UTF8.GetBytes($"{s}\n"))
                        .ToArray());
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
    private string? IsAuthenticatedAs(HttpListenerRequest request)
    {
        var authHeader = request.Headers[nameof(HttpRequestHeader.Authorization)];
        if (authHeader == null) return null;
        if (!authHeader.TryBasicAuthHeaderToUserPass(out var user, out var pass)) return null;
        if (!_passStorage.CorrectPass(user, pass)) return null;
        return user;
    }


    private bool IsUsernameLegal(string userName)
    {
        const string legalChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_-";
        return userName.All(legalChars.Contains);
    }


    private readonly HttpListener _listener;
    private readonly PassStorage _passStorage;
    private readonly IReadOnlyCollection<string> _urls;
}

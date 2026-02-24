using System.Net;

namespace TheAssembly.Server;


public class Server
{
    public Server(string url, ServerStorage storage) : this([url], storage) {}


    public Server(IEnumerable<string> urls, ServerStorage storage)
    {
        _storage = storage;
        _listener = new HttpListener();
        foreach (var url in urls) _listener.Prefixes.Add(url);
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

        _listener.Start();
        Console.WriteLine("UserServer started. Access me at " + string.Join(", ", _listener.Prefixes));

        while (true)
        {
            // We will multithread at a later point
            var context = _listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            var localPath = request.Url?.AbsolutePath[1..].ToLower().Split("/") ?? [];
            var requestMethod = request.HttpMethod.ToLower();
            var requestTokens = JoinedToken.Deserialize(request.InputStream);

            if (localPath == null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }

            // Create new account:
            else if (localPath.GetOr(0, "") == "join" && requestMethod == "post"
                && requestTokens.TokenCount() == 1
                && UserPass.TryDeserialize(requestTokens.GetEncodedToken(0), out var userPass)
                && !_storage.UserPasses.HasPassword(userPass.User) // We do not want random people to arbitrarily change anyones password lol
                && _storage.UserPasses.TryUpdate(userPass.User, userPass.Password))
            {
                response.StatusCode = (int) HttpStatusCode.OK;
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


    private readonly HttpListener _listener;
    private readonly ServerStorage _storage;
}

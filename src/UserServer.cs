using System.Net;

namespace TheAssembly.Server;


/// <summary>
/// GET on BASEURL/{id} responds with the serialized User.
/// </summary>
public class UserServer
{
    public UserServer(string url, UserStorage storage) : this([url], storage) {}


    public UserServer(IEnumerable<string> urls, UserStorage storage)
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
            // TODO: Check user authentication, and if user and requested user share at least one group

            var localPath = request.Url?.AbsolutePath[1..].ToLower().Split("/") ?? [];
            var requestMethod = request.HttpMethod.ToLower();

            long requestedId;
            byte[] rawData;
            User requestedUser;

            if (localPath == null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }
            else if (requestMethod == "get" && localPath.Length == 2
                && localPath[0] == "user" && long.TryParse(localPath[1], out requestedId)
                && _storage.TryGetEncoded(requestedId, out rawData))
            {
                response.StatusCode = (int) HttpStatusCode.OK;
                response.OutputStream.Write(rawData);
            }
            else if (requestMethod == "post" && localPath.Length == 3
                && localPath[0] == "user" && localPath[2] == "default"
                && long.TryParse(localPath[1], out requestedId))
            {
                _storage.Update(requestedId, new User(requestedId, "DefaultUser", []));
                response.StatusCode = (int) HttpStatusCode.OK;
            }
            else if (requestMethod == "post" && localPath.Length == 2
                && localPath[0] == "user" && long.TryParse(localPath[1], out requestedId)
                && User.TryDeserialize(request.InputStream, out requestedUser))
            {
                _storage.Update(requestedId, requestedUser);
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
    private readonly UserStorage _storage;
}

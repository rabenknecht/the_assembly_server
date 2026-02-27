using System.Net;

namespace TheAssembly.Server;


public class Server
{
    public Server(string url, string fileStorage) : this([url], fileStorage) {}


    public Server(IEnumerable<string> urls, string fileStorage)
    {
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
        Console.WriteLine("UserServer started. Access me with the following URLs " + string.Join(", ", _listener.Prefixes));

        while (true)
        {
            // We will multithread at a later point
            var context = _listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            var localPath = request.Url?.AbsolutePath[1..].ToLower().Split("/") ?? [];
            var requestMethod = request.HttpMethod.ToLower();

            if (localPath == null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
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
}

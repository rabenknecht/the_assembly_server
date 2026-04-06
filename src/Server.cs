using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using TheAssembly.Core;

namespace TheAssembly.Server;


public class Server
{
    public Server(string url, ServerStorage storage)
        : this([url], storage)
    {
    }


    public Server(IEnumerable<string> urls, ServerStorage storage)
    {
        _urls = [.. urls.Select(s => new Uri(s))];
        _storage = storage;
    }


    public async Task RunAsync()
    {
        await RunUsers();
    }


    private async Task RunUsers()
    {
        var listener = new HttpListener();
        foreach (var url in _urls)
        {
            listener.Prefixes.Add($"{url}users/");
        }
        listener.Start();

        while (true)
        {
            var context = await listener.GetContextAsync();
            var request = context.Request;
            using var response = context.Response;

            if (request.Url == null || listener.Prefixes.Contains(request.Url.ToString()))
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }
            else if (request.HttpMethod == "POST")
            {
                var joinRecord = await GetRequestContentDeserialized<JoinRecord>(request);

                if (joinRecord == null)
                {
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
                    return;
                }

                var addError = _storage.UserStorage.Add(joinRecord.user, joinRecord.password);

                if (addError == UserStorage.Error.InvalidUsername)
                {
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
                }
                else if (addError == UserStorage.Error.UserAlreadyExists)
                {
                    response.StatusCode = (int) HttpStatusCode.Forbidden;
                }
                else if (addError == UserStorage.Error.None)
                {
                    response.StatusCode = (int) HttpStatusCode.OK;
                }
                else
                {
                    response.StatusCode = (int) HttpStatusCode.NotImplemented;
                }
            }
            else if (request.HttpMethod == "GET")
            {
                if (GetAuthorizedUser(request) == null)
                {
                    response.StatusCode = (int) HttpStatusCode.Unauthorized;
                }
                else
                {
                    response.StatusCode = (int) HttpStatusCode.OK;
                    await SetResponseContent(response, string.Join('\n', _storage.UserStorage.EnumerateUsers));
                }
            }
            else
            {
                response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
            }
        }
    }


    /// <summary>
    /// Recommended to be called only once to collect the content into a variable
    /// </summary>
    private async Task<string> GetRequestContent(HttpListenerRequest of)
    {
        using var stream = of.InputStream;
        using var reader = new StreamReader(stream, ENCODING);
        return await reader.ReadToEndAsync();
    }


    /// <summary>
    /// Recommended to be called only once to collect the content into a variable
    /// </summary>
    private async Task<T?> GetRequestContentDeserialized<T>(HttpListenerRequest of)
        where T : class
    {
        Debug.Assert(ENCODING == Encoding.UTF8, $"Json deserialization does not support non utf8");

        if (of.ContentEncoding != Encoding.UTF8)
        {
            return null;
        }

        using var stream = of.InputStream;
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }


    /// <summary>
    /// Recommended to be called only once
    /// </summary>
    private async Task SetResponseContent(HttpListenerResponse of, string toWhat)
    {
        using var stream = of.OutputStream;
        using var writer = new StreamWriter(stream, ENCODING);
        await writer.WriteAsync(toWhat);
    }


    /// <returns>The user those authentication credentials have been passed along a HttpListenerRequest</returns>
    private string? GetAuthorizedUser(HttpListenerRequest ofRequest)
    {
        var authHeader = ofRequest.Headers[nameof(HttpRequestHeader.Authorization)];
        return authHeader != null
            && authHeader.TryBasicAuthHeaderToUserPass(out var user, out var pass)
            && _storage.UserStorage.Correct(user, pass)
            ? user
            : null;
    }


    // JsonSerializer only supports UTF8, so we don't accept anything else
    private static readonly Encoding ENCODING = Encoding.UTF8;


    private readonly Uri[] _urls;
    private readonly ServerStorage _storage;
}

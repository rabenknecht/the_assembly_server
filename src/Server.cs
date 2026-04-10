using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Linq;
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


    /// <summary>
    /// NEVER FINISHES
    /// </summary>
    public async Task RunAsync()
    {
        _ = RunCurrentEntries();
        _ = RunEntries();
        await RunUsers();
    }


    /// <summary>
    /// NEVER FINISHES
    /// </summary>
    private async Task RunUsers()
    {
        var listener = GetStartedListener("users/");

        while (true)
        {
            var context = await listener.GetContextAsync();
            var request = context.Request;
            using var response = context.Response;

            if (request.Url == null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }
            else if (request.HttpMethod == "POST")
            {
                var joinRecord = await GetRequestContentDeserialized<JoinRecord>(request);

                if (joinRecord == null)
                {
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
                    continue;
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
    /// NEVER FINISHES
    /// </summary>
    private async Task RunCurrentEntries()
    {
        var listener = GetStartedListener("entry/current/");

        while (true)
        {
            var context = await listener.GetContextAsync();
            var request = context.Request;
            using var response = context.Response;

            if (request?.Url == null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }
            else if (request.HttpMethod == "POST")
            {
                var authUser = GetAuthorizedUser(request);

                if (authUser == null)
                {
                    response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    continue;
                }

                var lastEntry = await _storage.EntryStorage.GetLast();

                if (lastEntry == null)
                {
                    // TODO: Better code? Also test that
                    response.StatusCode = (int) HttpStatusCode.NotAcceptable;
                    continue;
                }

                var voteRequest = await GetRequestContent(request);
                var voteRequestIndex = lastEntry.voteOptions!.IndexOf(v => v.votingWhat == voteRequest);

                if (voteRequestIndex == -1)
                {
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
                    continue;
                }

                lastEntry.RemoveVote(authUser);
                lastEntry.Vote(authUser, voteRequestIndex);

                response.StatusCode = (int) HttpStatusCode.OK;
            }
            else if (request.HttpMethod == "GET")
            {
                var authUser = GetAuthorizedUser(request);

                if (authUser == null)
                {
                    response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    continue;
                }

                var lastEntryJson = await _storage.EntryStorage.GetLastJson();

                if (lastEntryJson == null)
                {
                    response.StatusCode = (int) HttpStatusCode.NoContent;
                    continue;
                }

                await SetResponseContent(response, lastEntryJson);
                response.StatusCode = (int) HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
            }
        }
    }


    private async Task RunEntries()
    {
        var listener = GetStartedListener("entry/");

        while (true)
        {
            var context = await listener.GetContextAsync();
            var request = context.Request;
            using var response = context.Response;

            if (request.Url == null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }
            else if (request.HttpMethod == "GET")
            {
                if (GetAuthorizedUser(request) == null)
                {
                    response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    continue;
                }

                await SetResponseContent(response, await _storage.EntryStorage.GetAllExceptLastJson());
                response.StatusCode = (int) HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
            }
        }
    }


    private HttpListener GetStartedListener(string postfix)
    {
        Debug.Assert(!postfix.StartsWith('/'));
        Debug.Assert(postfix.EndsWith('/'));

        var listener = new HttpListener();
        foreach (var url in _urls)
        {
            listener.Prefixes.Add($"{url}{postfix}");
        }
        listener.Start();
        return listener;
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

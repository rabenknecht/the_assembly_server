using System.Net.Http.Headers;

namespace TheAssembly.Server.Test;

public static class TestUtil
{
    public static HttpClient AddBasicAuthHeader(this HttpClient client, string clientid, string clientsecret)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{clientid}:{clientsecret}");
        return client;
    }
}
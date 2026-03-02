using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TheAssembly.Server;

public static class Util
{
    public static T GetOr<T>(this T[] array, int index, T or)
    {
        if (index < 0 || index >= array.Length) return or;
        return array[index];
    }


    public static T[] SubArray<T>(this T[] array, int startInclusive, int length)
    {
        var result = new T[length];
        Array.Copy(array, startInclusive, result, 0, length);
        return result;
    }


    public static bool TryJsonDeserialize<T>(this Stream stream, out T result)
        where T : class
    {
        result = JsonSerializer.Deserialize<T>(stream)!;
        return result != null;
    }


    // TODO: Move into core, or even just to the client
    public static HttpClient AddBasicAuthenticationHeader(this HttpClient client, string clientid, string clientsecret)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{clientid}:{clientsecret}");
        return client;
    }


    public static bool TryBasicAuthHeaderToUserPass(this string basicAuthHeader, out string user, out string pass)
    {
        user = null!;
        pass = null!;
        var split = basicAuthHeader.Split(':');
        if (split.Length != 2) return false;
        user = split[0];
        pass = split[1];
        return true;
    }
}

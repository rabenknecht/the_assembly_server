using System.Net;
using System.Net.Http.Headers;

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


    public static HttpClient AddBasicAuthenticationHeader(this HttpClient client, string clientid, string clientsecret)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{clientid}:{clientsecret}");
        return client;
    }
}

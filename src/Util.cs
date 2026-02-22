using System.Net;

namespace TheAssembly.Server;

public static class Util
{
    public static T GetOr<T>(this T[] array, int index, T or)
    {
        if (index < 0 || index >= array.Length) return or;
        return array[index];
    }


    public static long? ExtractUserId(this HttpListenerRequest request)
    {
        var cookie = request.Cookies.SingleOrDefault(c => c != null && c.Name == "userId");
        if (cookie == null) return null;
        if (long.TryParse(cookie.Value, out var userId)) return userId;
        return null;
    }
}

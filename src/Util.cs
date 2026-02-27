using System.Net;

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
}

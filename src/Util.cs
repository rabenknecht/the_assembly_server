namespace TheAssembly.Server;

public static class Util
{
    public static T GetOr<T>(this T[] array, int index, T or)
    {
        if (index < 0 || index >= array.Length) return or;
        return array[index];
    }
}

namespace TheAssembly.Server;

public static class Util
{
    public static T GetOr<T>(this T[] array, int index, T or)
    {
        return  index >= 0 && index < array.Length ? array[index] : or;
    }


    public static string? ExtractOption(this string[] args, string identifier)
    {
        var i = args.IndexOf(identifier);
        if (i == -1 || i >= args.Length - 1) return null;
        return args[i + 1];
    }


    public static T Or<T>(this T? obj, T or)
    {
        return obj == null ? or : obj;
    }
}

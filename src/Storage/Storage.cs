using System.Diagnostics.CodeAnalysis;

namespace TheAssembly.Server;

public class Storage<TId, TStored>
{
    /// <param name="idToString">Defaults to object.ToString()</param>
    /// <exception cref="ArgumentException">If basePath is not a directory</exception>
    public Storage(string basePath,
        Func<TStored, byte[]> serializer,
        Func<byte[], TStored> deserializer,
        Func<string, TId> idParser,
        Func<TId, string>? idToString = null)
    {
        ArgumentNullException.ThrowIfNull(basePath);
        ArgumentNullException.ThrowIfNull(idParser);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(deserializer);
        if (!Directory.Exists(basePath)) throw new ArgumentException($"No directory with path {basePath} exists");

        _basePath = basePath;
        _serializer = serializer;
        _deserializer = deserializer;
        _idParser = idParser;
        _idToString = idToString ?? (i => i!.ToString()!);
    }


    public IEnumerable<TId> Ids => Directory.GetFiles(_basePath).Select(_idParser);


    /// <param name="id">Cannot be null</param>
    public bool TryGetEncoded(TId id, [NotNull] out byte[] result)
    {
        var path = InstanceFilePath(id);

        if (!File.Exists(path))
        {
            result = [];
            return false;
        }

        result = File.ReadAllBytes(path);
        return true;
    }


    /// <param name="id">Cannot be null</param>
    public byte[]? GetEncoded(TId id)
    {
        if (TryGetEncoded(id, out var result)) return result;
        return null;
    }


    /// <param name="id">Cannot be null</param>
    /// <param name="stored">No gurantees should be made about the result when false is returned.
    /// Can be null when the method returns false</param>
    /// <returns></returns>
    public bool TryGet(TId id, out TStored stored)
    {
        if (!TryGetEncoded(id, out var rawData))
        {
            stored = default!;
            return false;
        }

        stored = _deserializer(rawData);
        return true;
    }


    /// <param name="id">Cannot be null</param>
    public TStored? Get(TId id)
    {
        var rawData = GetEncoded(id);
        if (rawData == null) return default;
        return _deserializer(rawData);
    }


    /// <summary>
    /// Saves a new instance on id.
    /// Replaces the old instance if a instance with the passed id already exists.
    /// </summary>
    /// <param name="id">Cannot be null</param>
    public void Update(TId id, TStored stored)
    {
        var path = InstanceFilePath(id);
        var rawData = _serializer(stored);
        File.WriteAllBytes(path, rawData);
    }


    private readonly Func<TStored, byte[]> _serializer;
    private readonly Func<byte[], TStored> _deserializer;
    private readonly Func<string, TId> _idParser;
    private readonly Func<TId, string> _idToString;
    private readonly string _basePath;

    /// <param name="id">The id of the instance those file path should be returned</param>
    /// <returns>The path to the file containing the instance with the passed id</returns>
    private string InstanceFilePath(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        var idString = _idToString(id) ?? throw new NullReferenceException("idToString result is null");
        return Path.Combine(_basePath, idString);
    }
}

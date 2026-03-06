using System.Text.Json;

namespace TheAssembly.Server;

public class EntryStorage
{
    public EntryStorage(string filePath)
    {
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize<EntryRecord[]>([]));
        }

        FilePath = filePath;
    }


    /// <param name="question">The question to add. First line is actual question, next ones vote options</param>
    public void AddLast(EntryRecord entry)
    {
        var saved = GetSaved();
        var newSaved = saved.Add(entry);
        var serialized = JsonSerializer.Serialize(newSaved);
        File.WriteAllText(FilePath, serialized);
    }


    public string? GetLastJson()
    {
        var last = GetLast();
        if (last == null) return null;
        return JsonSerializer.Serialize(last);
    }


    public bool TryGetLastJson(out string result)
    {
        result = GetLastJson()!;
        return result != null;
    }


    public EntryRecord? GetLast()
    {
        return GetSaved().GetLastOr(null);
    }


    public string GetAllExceptLastJson()
    {
        var saved = GetSaved();
        return JsonSerializer.Serialize(saved.SubArray(0, saved.Length - 1));
    }


    public void UpdateLast(EntryRecord entry)
    {
        var saved = GetSaved();
        if (saved.Length == 0) throw new InvalidOperationException("No entry stored");
        saved[^1] = entry;
        var serialized = JsonSerializer.Serialize(saved);
        File.WriteAllText(FilePath, serialized);
    }


    public int EntryCount => GetSaved().Length;


    public bool IsEmpty => GetSaved().Length == 0;


    public void Clear()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize<EntryRecord[]>([]));
    }


    public readonly string FilePath;


    private EntryRecord[] GetSaved()
    {
        var serialized = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<EntryRecord[]>(serialized)
            ?? throw new FormatException("Entryfile corrupted");
    }
}

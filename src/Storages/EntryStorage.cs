using System.Text.Json;
using TheAssembly.Core;

namespace TheAssembly.Server;

public class EntryStorage
{
    internal EntryStorage(string filePath)
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


    /// <returns>The last entries json, or null when no entries are stored.</returns>
    public string? GetLastJson()
    {
        var last = GetLast();
        if (last == null) return null;
        return JsonSerializer.Serialize(last);
    }


    public EntryRecord? GetLast()
    {
        return GetSaved().GetLastOr(null);
    }


    /// <returns>The json-array of all EntryRecords stored in this storage,
    /// ignoring the last element. Returns an empty json array when no or only
    /// one element exists.</returns>
    public string GetAllExceptLastJson()
    {
        // TODO: Test edge case: Get entry/ when no entries exist
        var saved = GetSaved();
        return saved.Length == 0
            ? JsonSerializer.Serialize<EntryRecord[]>([])
            : JsonSerializer.Serialize(saved.SubArray(0, saved.Length - 1));
    }


    public void UpdateLast(EntryRecord entry)
    {
        var saved = GetSaved();
        if (saved.Length == 0) throw new InvalidOperationException("No entry stored");
        saved[^1] = entry;
        var serialized = JsonSerializer.Serialize(saved);
        File.WriteAllText(FilePath, serialized);
    }


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

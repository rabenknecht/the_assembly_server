using System.Text.Json;
using TheAssembly.Core;

namespace TheAssembly.Server;

public class EntryStorage
{
    internal EntryStorage(string currentFile, string nonCurrentFile)
    {
        if (!File.Exists(currentFile))
        {
            File.Create(currentFile).Close();
        }

        if (!File.Exists(nonCurrentFile))
        {
            File.WriteAllText(nonCurrentFile, "[");
        }

        _currentFile = currentFile;
        _nonCurrentFile = nonCurrentFile;
    }


    /// <param name="question">The question to add. First line is actual question, next ones vote options</param>
    public void AddLast(EntryRecord entry)
    {
        var prevCurrentJson = File.ReadAllText(_currentFile);
        File.WriteAllText(_currentFile, JsonSerializer.Serialize(entry));

        if (IsNonCurrentEmpty())
        {
            File.AppendAllText(_nonCurrentFile, prevCurrentJson);
        }
        else
        {
            File.AppendAllText(_nonCurrentFile, "," + prevCurrentJson);
        }
    }


    /// <returns>The last entries json, or null when no entries are stored.</returns>
    public async Task<string?> GetLastJson()
    {
        return await File.ReadAllTextAsync(_currentFile);
    }


    public async Task<EntryRecord?> GetLast()
    {
        return JsonSerializer.Deserialize<EntryRecord>(await File.ReadAllTextAsync(_currentFile));
    }


    /// <returns>The json-array of all EntryRecords stored in this storage,
    /// ignoring the last element. Returns an empty json array when no or only
    /// one element exists.</returns>
    public async Task<string> GetAllExceptLastJson()
    {
        return await File.ReadAllTextAsync(_nonCurrentFile) + "]";
    }


    public async Task UpdateLast(EntryRecord entry)
    {
        if (!IsCurrentEmpty())
        {
            await File.WriteAllTextAsync(_currentFile, JsonSerializer.Serialize(entry));
        }
    }


    public void Clear()
    {
        File.WriteAllText(_currentFile, "");
        File.WriteAllText(_nonCurrentFile, "[");
    }


    /// <summary>
    /// The file that contains the json for the current entry.
    /// </summary>
    private readonly string _currentFile;

    /// <summary>
    /// The file that contains the json for all non current entries.
    /// <para/>
    /// This file will always miss the last ']' to allow for easy appending of new entries.
    /// </summary>
    private readonly string _nonCurrentFile;


    private bool IsCurrentEmpty()
    {
        using var handle = File.OpenRead(_currentFile);
        return handle.Length != 0;
    }


    private bool IsNonCurrentEmpty()
    {
        using var handle = File.OpenRead(_nonCurrentFile);
        return handle.Length > 1; // Ignore the '[' when the file has no elements
    }
}

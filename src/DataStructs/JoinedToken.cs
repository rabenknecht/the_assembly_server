namespace TheAssembly.Server;

public class JoinedToken
{
    /// <summary>
    /// Required 2 times to indicate a delimiter.
    /// </summary>
    private const byte DELIMITER = (byte) '\n';


    public JoinedToken(IEnumerable<byte[]> tokens)
    {
        _tokens = [.. tokens];
        foreach (var token in _tokens)
        {
            for (int i = 1; i < token.Length; i++)
            {
                if (token[i - 1] == DELIMITER && token[i] == DELIMITER) throw new ArgumentException("A token uses a delimiter!");
            }
        }
    }


    public JoinedToken(params byte[][] tokens) : this((IEnumerable<byte[]>) tokens) {}


    public byte[] GetEncodedToken(int index) => _tokens[index];


    public int TokenCount() => _tokens.Length;


    public static JoinedToken? Deserialize(byte[] data)
    {
        var tokens = new List<byte[]>();

        var newTokenStartInclusive = 0;
        for (var i = 1; i < data.Length; i++)
        {
            if (data[i - 1] == DELIMITER && data[i] == DELIMITER)
            {
                var newTokenEndExclusive = i - 1;
                tokens.Add(data.SubArray(newTokenStartInclusive, newTokenEndExclusive - newTokenStartInclusive));
                newTokenStartInclusive = i + 1;
            }
        }

        // We will forget the last token otherwise!
        tokens.Add(data.SubArray(newTokenStartInclusive, data.Length - newTokenStartInclusive));

        return new JoinedToken(tokens);
    }


    public byte[] Serialize()
    {
        if (_tokens.Length == 0) return [];

        var neededDelimiters = _tokens.Length - 1;
        var tokenBytes = _tokens.Sum(b => b.Length);

        var result = new byte[tokenBytes + neededDelimiters * 2];
        Array.Copy(_tokens[0], result, _tokens[0].Length);

        var resultI = _tokens[0].Length;
        foreach (var token in _tokens.Skip(1))
        {
            result[resultI] = (byte) DELIMITER;
            result[resultI + 1] = (byte) DELIMITER;
            Array.Copy(token, 0, result, resultI + 2, token.Length);
            resultI += token.Length + 2;
        }

        return result;
    }


    private byte[][] _tokens;
}

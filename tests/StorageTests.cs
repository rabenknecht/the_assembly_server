using System.Text;

namespace TheAssembly.Server.Test;

public class StorageTests
{
    public void _1_UpdateGet()
    {
        var storage = new Storage<long, int>(
            TestUtil.GenerateTestDir(),
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        storage.Update(83751, 2);

        var expected = 2;
        var actual = storage.Get(83751);

        // TODO: Assert
    }


    public void _1_GetNonExisting()
    {
        var storage = new Storage<long, int>(
            TestUtil.GenerateTestDir(),
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        var expected = 0;
        var actual = storage.Get(83751);

        // TODO: Assert
    }


    public void _1_TryGetNonExisting()
    {
        var storage = new Storage<long, int>(
            TestUtil.GenerateTestDir(),
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        var expectedReturn = false;
        var actualReturn = storage.TryGet(78472922, out _);

        // Do NOT test the out-ed stored value, as we state that the return value is not guranteed
        // TODO: Assert
    }


    public void _1_UpdateTryGet()
    {
        var storage = new Storage<long, int>(
            TestUtil.GenerateTestDir(),
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        storage.Update(-1000, -50);

        var expectedReturn = true;
        var expectedOuted = -50;
        var actualReturn = storage.TryGet(-1000, out var actualOuted);

        // Do NOT test the out-ed stored value, as we state that the return value is not guranteed
        // TODO: Assert
    }


    public void _2_OverwrittingUpdateGet()
    {
        var storage = new Storage<long, int>(
            TestUtil.GenerateTestDir(),
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        storage.Update(100, 2);
        storage.Update(100, 3);

        var expected = 3;
        var actual = storage.Get(100);

        // TODO: Assert
    }
}

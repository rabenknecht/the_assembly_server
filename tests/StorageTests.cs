using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TheAssembly.Server.Test;

[TestClass]
public class StorageTests
{
    private string TestDir = "/tmp/the_assembly_tests/server/storage_tests/";


    [TestInitialize]
    public void TestDirInit()
    {
        if (Directory.Exists(TestDir)) Directory.Delete(TestDir, true);
        Directory.CreateDirectory(TestDir);
    }


    [TestMethod]
    public void _1_UpdateGet()
    {
        var storage = new Storage<long, int>(
            TestDir,
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        storage.Update(83751, 2);

        var expected = 2;
        var actual = storage.Get(83751);
        Assert.AreEqual(expected, actual);
    }


    [TestMethod]
    public void _1_GetNonExisting()
    {
        var storage = new Storage<long, int>(
            TestDir,
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        var expected = 0;
        var actual = storage.Get(83751);
        Assert.AreEqual(expected, actual);
    }


    [TestMethod]
    public void _1_TryGetNonExisting()
    {
        var storage = new Storage<long, int>(
            TestDir,
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        // Do NOT test the out-ed stored value, as we state that the return value is not guranteed
        var expectedReturn = false;
        var actualReturn = storage.TryGet(78472922, out _);
        Assert.AreEqual(expectedReturn, actualReturn);
    }


    [TestMethod]
    public void _1_UpdateTryGet()
    {
        var storage = new Storage<long, int>(
            TestDir,
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        storage.Update(-1000, -50);

        var expectedReturn = true;
        var expectedOuted = -50;
        var actualReturn = storage.TryGet(-1000, out var actualOuted);
        Assert.AreEqual(expectedReturn, actualReturn);
        Assert.AreEqual(expectedOuted, actualOuted);
    }


    [TestMethod]
    public void _2_OverwrittingUpdateGet()
    {
        var storage = new Storage<long, int>(
            TestDir,
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        storage.Update(100, 2);
        storage.Update(100, 3);

        var expected = 3;
        var actual = storage.Get(100);
        Assert.AreEqual(expected, actual);
    }


    [TestMethod]
    public void _2_UpdateGetPersistent()
    {
        var storage = new Storage<long, int>(
            TestDir,
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        storage.Update(-1, 3);

        storage = new Storage<long, int>(
            TestDir,
            x => Encoding.UTF8.GetBytes(x.ToString()),
            x => int.Parse(Encoding.UTF8.GetString(x)),
            long.Parse);

        var expected = 3;
        var actual = storage.Get(-1);
        Assert.AreEqual(expected, actual);
    }
}

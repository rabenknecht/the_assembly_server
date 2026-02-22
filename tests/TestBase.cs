namespace TheAssembly.Server.Test;

public class TestBase
{
    protected readonly string TestDir = "/tmp/the_assembly_tests/server/storage_tests/";


    [TestInitialize]
    public void TestDirInit()
    {
        if (Directory.Exists(TestDir)) Directory.Delete(TestDir, true);
        Directory.CreateDirectory(TestDir);
    }
}

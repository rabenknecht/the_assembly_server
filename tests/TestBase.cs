namespace TheAssembly.Server.Test;

public class TestBase
{
    protected readonly string TestDir = "/tmp/the_assembly_tests/server/storage_tests/";
    protected readonly string LocalhostUrl = "http://localhost:2302/";


    [TestInitialize]
    public void TestDirInit()
    {
        if (Directory.Exists(TestDir)) Directory.Delete(TestDir, true);
        Directory.CreateDirectory(TestDir);
        TestInitialize();
    }


    protected virtual void TestInitialize() {}
}

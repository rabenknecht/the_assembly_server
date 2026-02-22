namespace TheAssembly.Server.Test;

public static class TestUtil
{
    private static ulong _testDirCounter;
    public static string GenerateTestDir() =>
        $"/tmp/the_assembly_server_testdir/{_testDirCounter++}";
}

namespace TheAssembly.Server.Test;


[TestClass]
public class ServerTests : TestBase
{
    // Always non null as TestInitialize is always executed before any test
    private HttpClient _client = null!;
    private Server _server = null!;

    protected override void TestInitialize()
    {
        base.TestInitialize();

        _client = new HttpClient() { BaseAddress = new Uri(LocalhostUrl) };

        _server?.StopIfRunning();
        _server = new Server(LocalhostUrl, TestDir);
        _server.RunAsync();
    }


    [TestMethod]
    public void Join_NewUser_OK()
    {
        // TODO: How to wait for async operations in tests!?
        var response = await _client.PostAsync("join", )
    }

    [TestMethod]
    public void Join_ExistingUser_NotFound()
    {

    }

    [TestMethod]
    public void Users_Unauthenticated_NotFound()
    {

    }

    [TestMethod]
    public void Users_Authenticated_ReturnsUsers()
    {

    }

    [TestMethod]
    public void DoIExist_NonExistingUser_NotFound()
    {

    }

    [TestMethod]
    public void DoIExist_ExistingUser_OK()
    {

    }
}
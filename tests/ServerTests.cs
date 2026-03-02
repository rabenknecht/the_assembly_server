using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TheAssembly.Server.Test;


[TestClass]
public class ServerTests
{
    public TestContext TestContext { get; set; }


    // Always non null as TestInitialize is always executed before any test
    private static HttpClient _client = null!;
    private static Server _server = null!;


    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        const string Url = "http://localhost:2023/3/";
        _server = new Server(Url, "/tmp/the_assembly_testing/servertests/serverDir/");
        _client = new HttpClient() { BaseAddress = new Uri(Url) };

        _server.RunAsync();
    }


    [TestInitialize]
    public void TestInit()
    {
        _server.ClearStorage();
        _client.DefaultRequestHeaders.Authorization = null;
    }


    [TestMethod]
    public async Task UsersPOST_SingleUser_OK()
    {
        var response = await JoinPost(new JoinRecord("_1test", "ö12-*#"));
        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task UsersPOST_SecondNewUser_OK()
    {
        await JoinPost(new JoinRecord("_balls123", "shat"));
        var response = await JoinPost(new JoinRecord("ilawte", ""));

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task UsersPOST_ExistingUser_NotOK()
    {
        // TODO: Check if password is unchanged...
        await JoinPost(new JoinRecord("1", "jidw-w.a-,"));
        var response = await JoinPost(new JoinRecord("1", "newPass"));

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task UsersPOST_OverrideOldEntry_OldPasswordOK()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));
        await JoinPost(new JoinRecord("-test3", "shat"));

        _client.AddBasicAuthHeader("-test3", "hello");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);
        var actualUnsplit = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        CollectionAssert.AreEquivalent(
            new string[] { "test1", "_test2", "-test3" },
            actualUnsplit.Split('\n'));
    }

    [TestMethod]
    public async Task UsersPOST_OverrideOldEntry_NewPasswordNotOK()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));
        await JoinPost(new JoinRecord("-test3", "shat"));

        _client.AddBasicAuthHeader("-test3", "shat");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task UsersGET_Unauthenticated_NotOK()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));

        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task UsersGET_IncorrectlyAuthenticated_NotOK()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));

        _client.AddBasicAuthHeader("-test3", "hellp");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task UsersGET_Authenticated_ReturnsUsers()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));

        // The authentication header appears to get lost in transmission?
        _client.AddBasicAuthHeader("-test3", "hello");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);
        var actualUnsplit = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        CollectionAssert.AreEquivalent(
            new string[] { "test1", "_test2", "-test3" },
            actualUnsplit.Split('\n'));
    }


    private async Task<HttpResponseMessage> JoinPost(JoinRecord joinRecord)
    {
        return await _client.PostAsJsonAsync("users", joinRecord, TestContext.CancellationToken);
    }
}
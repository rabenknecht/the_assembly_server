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
    public async Task Join_SingleUser_OK()
    {
        var response = await JoinPost(new JoinRecord("_1test", "ö12-*#"));
        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task Join_SecondNewUser_OK()
    {
        await JoinPost(new JoinRecord("_balls123", "shat"));
        var response = await JoinPost(new JoinRecord("ilawte", ""));

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task Join_ExistingUser_NotOK()
    {
        // TODO: Check if password is unchanged...
        await JoinPost(new JoinRecord("1", "jidw-w.a-,"));
        var response = await JoinPost(new JoinRecord("1", "newPass"));

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }


    [TestMethod]
    public async Task DoIExist_NonExistingUser_NotOK()
    {
        await JoinPost(new JoinRecord("sas", "fast"));

        _client.AddBasicAuthenticationHeader("sass", "");
        var response = await _client.GetAsync("doIExist", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task DoIExist_ExistingUser_OK()
    {
        await JoinPost(new JoinRecord("sas", "fast"));

        _client.AddBasicAuthenticationHeader("sas", "awfiwajfoj");
        var response = await _client.GetAsync("doIExist", TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }


    [TestMethod]
    public async Task Users_Unauthenticated_NotOK()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));

        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task Users_IncorrectlyAuthenticated_NotOK()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));

        _client.AddBasicAuthenticationHeader("-test3", "hellp");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Users_Authenticated_ReturnsUsers()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));

        _client.AddBasicAuthenticationHeader("-test3", "hello");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("test1\n_test2\n-test3", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Join_OverrideOldEntry_OldPasswordOK()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));
        await JoinPost(new JoinRecord("-test3", "shat"));

        _client.AddBasicAuthenticationHeader("-test3", "hello");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("test1\n_test2\n-test3", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Join_OverrideOldEntry_NewPasswordNotOK()
    {
        await JoinPost(new JoinRecord("test1", ""));
        await JoinPost(new JoinRecord("_test2", ""));
        await JoinPost(new JoinRecord("-test3", "hello"));
        await JoinPost(new JoinRecord("-test3", "shat"));

        _client.AddBasicAuthenticationHeader("-test3", "shat");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    private async Task<HttpResponseMessage> JoinPost(JoinRecord joinRecord)
    {
        return await _client.PostAsJsonAsync("users", joinRecord, TestContext.CancellationToken);
    }
}
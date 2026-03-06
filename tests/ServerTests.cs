using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TheAssembly.Server.Test;


[TestClass]
public class ServerTests
{
    public TestContext TestContext { get; set; }


    // Always non null as TestInitialize is always executed before any test
    private static HttpClient _client = null!;
    private static Server _server = null!;

    private const string TEST_DIR = "/tmp/the_assembly_testing/servertests";
    private const string SERVER_DIR = $"{TEST_DIR}/serverDir";
    private const string QUESTION_FILE = $"{TEST_DIR}/questionFile";
    private const string Url = "http://localhost:2023/3/";


    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        if (Directory.Exists(TEST_DIR)) Directory.Delete(TEST_DIR, true);
        if (Directory.Exists(SERVER_DIR)) Directory.Delete(SERVER_DIR, true);

        Directory.CreateDirectory(TEST_DIR); // We need the testdir for the questionfile
        File.WriteAllText(QUESTION_FILE, string.Empty);

        _server = new Server(Url, SERVER_DIR, QUESTION_FILE);
        _client = new HttpClient() { BaseAddress = new Uri(Url) };

        _server.RunAsync();
    }


    [TestInitialize]
    public void TestInit()
    {
        File.WriteAllText(QUESTION_FILE, string.Empty);
        _server.ClearStorage();
        _server.ReloadQuestions();
        _client.DefaultRequestHeaders.Authorization = null;
    }


    [TestMethod]
    public async Task UsersPOST_SingleUser_OK()
    {
        var response = await UserPost(new JoinRecord("_1test", "ö12-*#"));
        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task UsersPOST_SecondNewUser_OK()
    {
        await UserPost(new JoinRecord("_balls123", "shat"));
        var response = await UserPost(new JoinRecord("ilawte", ""));

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task UsersPOST_ExistingUser_NotOK()
    {
        // TODO: Check if password is unchanged...
        await UserPost(new JoinRecord("1", "jidw-w.a-,"));
        var response = await UserPost(new JoinRecord("1", "newPass"));

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task UsersPOST_OverrideOldEntry_OldPasswordOK()
    {
        await UserPost(new JoinRecord("test1", ""));
        await UserPost(new JoinRecord("_test2", ""));
        await UserPost(new JoinRecord("-test3", "hello"));
        await UserPost(new JoinRecord("-test3", "shat"));

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
        await UserPost(new JoinRecord("test1", ""));
        await UserPost(new JoinRecord("_test2", ""));
        await UserPost(new JoinRecord("-test3", "hello"));
        await UserPost(new JoinRecord("-test3", "shat"));

        _client.AddBasicAuthHeader("-test3", "shat");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task UsersGET_Unauthenticated_NotOK()
    {
        await UserPost(new JoinRecord("test1", ""));
        await UserPost(new JoinRecord("_test2", ""));
        await UserPost(new JoinRecord("-test3", "hello"));

        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task UsersGET_IncorrectlyAuthenticated_NotOK()
    {
        await UserPost(new JoinRecord("test1", ""));
        await UserPost(new JoinRecord("_test2", ""));
        await UserPost(new JoinRecord("-test3", "hello"));

        _client.AddBasicAuthHeader("-test3", "hellp");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task UsersGET_Authenticated_ReturnsUsers()
    {
        await UserPost(new JoinRecord("test1", ""));
        await UserPost(new JoinRecord("_test2", ""));
        await UserPost(new JoinRecord("-test3", "hello"));

        // The authentication header appears to get lost in transmission?
        _client.AddBasicAuthHeader("-test3", "hello");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);
        var actualUnsplit = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        CollectionAssert.AreEquivalent(
            new string[] { "test1", "_test2", "-test3" },
            actualUnsplit.Split('\n'));
    }


    [TestMethod]
    public async Task NewRandomQuestion_WhenQuestionsAreAvailable_ReturnsTrue()
    {
        File.WriteAllText(QUESTION_FILE, "Q1\n"
            + "Q1V1\n"
            + "Q1V2\n"
            + "\n"
            + "Q2\n"
            + "Q2V1\n"
            + "Q2V2\n"
            + "\n"
            + "Q3\n"
            + "Q3V1\n"
            + "Q3V2\n"
            + "\n"
            + "Q4\n"
            + "Q4V1\n"
            + "Q4V2");
        Assert.IsTrue(_server.NewRandomEntry());
        Assert.IsTrue(_server.NewRandomEntry());
        Assert.IsTrue(_server.NewRandomEntry());
        Assert.IsTrue(_server.NewRandomEntry());
    }


    [TestMethod]
    public async Task NewRandomQuestion_NoQuestionsLeft_ReturnsFalse()
    {
        File.WriteAllText(QUESTION_FILE, "Q1\n"
            + "Q1V1\n"
            + "Q1V2\n"
            + "\n"
            + "Q2\n"
            + "Q2V1\n"
            + "Q2V2\n"
            + "\n"
            + "Q3\n"
            + "Q3V1\n"
            + "Q3V2\n"
            + "\n"
            + "Q4\n"
            + "Q4V1\n"
            + "Q4V2");
        _server.NewRandomEntry();
        _server.NewRandomEntry();
        _server.NewRandomEntry();
        _server.NewRandomEntry();
        Assert.IsFalse(_server.NewRandomEntry());
    }


    [TestMethod]
    public async Task EntryCurrent_NoEntryGenerated_NotOK()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        var response = await _client.GetAsync("entry/current", TestContext.CancellationToken);
        
        Assert.AreNotEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }


    [TestMethod]
    public async Task EntryCurrent_QuestionWithConstantVoteOptions_ReturnsUnvotedQuestion()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(QUESTION_FILE, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes");
        _server.NewRandomEntry();

        var response = await _client.GetAsync("entry/current", TestContext.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var actualEntry = JsonSerializer.Deserialize<EntryRecord>(responseText);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.IsNotNull(actualEntry);
        Assert.IsNotNull(actualEntry.voteOptions);
        CollectionAssert.AllItemsAreNotNull(actualEntry.voteOptions);
        Assert.AreEqual("Do you still love me?", actualEntry.question);
        CollectionAssert.AreEquivalent(
            new string[] { "No", "Yes", "Sometimes" },
            actualEntry.voteOptions.Select(v => v.voteOption).ToList());
        Assert.IsTrue(actualEntry.voteOptions.All(v => v.votedBy!.Length == 0));
    }


    [TestMethod]
    public async Task EntryCurrent_QuestionWithLastSymbolLinebreak_CorrectVoteOptions()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(QUESTION_FILE, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes\n");
        _server.NewRandomEntry();

        var response = await _client.GetAsync("entry/current", TestContext.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var actualEntry = JsonSerializer.Deserialize<EntryRecord>(responseText);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.IsNotNull(actualEntry);
        Assert.IsNotNull(actualEntry.voteOptions);
        CollectionAssert.AllItemsAreNotNull(actualEntry.voteOptions);
        Assert.AreEqual("Do you still love me?", actualEntry.question);
        CollectionAssert.AreEquivalent(
            new string[] { "No", "Yes", "Sometimes" },
            actualEntry.voteOptions.Select(v => v.voteOption).ToList());
        Assert.IsTrue(actualEntry.voteOptions.All(v => v.votedBy!.Length == 0));
    }


    [TestMethod]
    public async Task EntryCurrent_QuestionWithUserVoteOptions_ReturnsUnvotedQuestion()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user1", ""));
        _client.AddBasicAuthHeader("user1", "");

        await UserPost(new JoinRecord("user2", ""));
        await UserPost(new JoinRecord("user3", ""));

        File.WriteAllText(QUESTION_FILE, "Userquestion?\n"
            + ":u\n"
            + "Nobody");
        _server.NewRandomEntry();

        var response = await _client.GetAsync("entry/current", TestContext.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var actualEntry = JsonSerializer.Deserialize<EntryRecord>(responseText);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.IsNotNull(actualEntry);
        Assert.IsNotNull(actualEntry.voteOptions);
        CollectionAssert.AllItemsAreNotNull(actualEntry.voteOptions);
        Assert.AreEqual("Userquestion?", actualEntry.question);
        CollectionAssert.AreEquivalent(
            new string[] { "Nobody", "user1", "user2", "user3" },
            actualEntry.voteOptions.Select(v => v.voteOption).ToList());
        Assert.IsTrue(actualEntry.voteOptions.All(v => v.votedBy!.Length == 0)); // No one voted
    }


    [TestMethod]
    public async Task Entry_ForcedSinglePastEntry_ReturnsUnvotedQuestion()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(QUESTION_FILE, "Who sucks more?\n"
            + "Price\n"
            + "Picard\n"
            + "Susane\n"
            + "Your mother");
        _server.NewRandomEntry();

        // Add new question, move first question into general entry storage
        File.AppendAllText(QUESTION_FILE, "Test\n"
            + "test1\n"
            + "test2\n");
        _server.NewRandomEntry();

        var response = await _client.GetAsync("entry", TestContext.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var actualEntries = JsonSerializer.Deserialize<EntryRecord[]>(responseText);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.IsNotNull(actualEntries);
        Assert.HasCount(1, actualEntries);

        var actualEntry = actualEntries[0];
        Assert.IsNotNull(actualEntry);
        Assert.IsNotNull(actualEntry.voteOptions);
        CollectionAssert.AllItemsAreNotNull(actualEntry.voteOptions);
        Assert.AreEqual("Who sucks more?", actualEntry.question);
        CollectionAssert.AreEquivalent(
            new string[] { "Price", "Picard", "Susane", "Your mother" },
            actualEntry.voteOptions.Select(v => v.voteOption).ToList());
        Assert.IsTrue(actualEntry.voteOptions.All(v => v.votedBy!.Length == 0));
    }


    [TestMethod]
    public async Task Entry_MultiplePastEntries_ReturnsAllQuestions()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(QUESTION_FILE, "Q1\n"
            + "Q1V1\n"
            + "Q1V2\n"
            + "\n"
            + "Q2\n"
            + "Q2V1\n"
            + "Q2V2\n"
            + "\n"
            + "Q3\n"
            + "Q3V1\n"
            + "Q3V2\n"
            + "\n"
            + "Q4\n"
            + "Q4V1\n"
            + "Q4V2");
        _server.NewRandomEntry();
        _server.NewRandomEntry();
        _server.NewRandomEntry();
        _server.NewRandomEntry();

        var response = await _client.GetAsync("entry", TestContext.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var actualEntries = JsonSerializer.Deserialize<EntryRecord[]>(responseText);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        Assert.IsNotNull(actualEntries);
        Assert.HasCount(3, actualEntries);
        CollectionAssert.IsSubsetOf
        (
            actualEntries.Select(e => e.question).ToList(),
            new string[] { "Q1", "Q2", "Q3", "Q4" },
            "entry returns incorrect questions"
        );
        CollectionAssert.IsSubsetOf // Good enough. Users will notice incoherent voteoptions anyway
        (
            actualEntries.SelectMany(e => e.voteOptions!).Select(v => v.voteOption).ToList(),
            new string[] { "Q1V1", "Q1V2", "Q2V1", "Q2V2", "Q3V1", "Q3V2", "Q4V1", "Q4V2" },
            "entry returns incorrect voteOptions"
        );
    }


    private async Task<HttpResponseMessage> UserPost(JoinRecord joinRecord)
    {
        return await _client.PostAsJsonAsync("users", joinRecord, TestContext.CancellationToken);
    }
}
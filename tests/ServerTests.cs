using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TheAssembly.Core;

namespace TheAssembly.Server.Test;


[TestClass]
public class ServerTests
{
    public TestContext TestContext { get; set; }


    // Always non null as TestInitialize is always executed before any test
    private static HttpClient _client = null!;
    private static ServerStorage _storage = null!;
    private static ServerStorage _altStorage = null!;
    private static Server _server = null!;

    private static string _questionFile = null!;

    private const string URL = "http://localhost:2023/28974/";


    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        var testDir = Directory.CreateTempSubdirectory().FullName;
        var serverDir = Path.Combine(testDir, "serverStorage");
        _questionFile = Path.Combine(testDir, "questions");

        Directory.CreateDirectory(serverDir);
        File.WriteAllText(_questionFile, "");

        _storage = ServerStorage.CreateIn(serverDir)!;
        _altStorage = ServerStorage.CreateIn(serverDir)!;
        _server = new Server(URL, _storage);
        _client = new HttpClient() { BaseAddress = new Uri(URL) };

        _storage.GeneralQuestionStorage.TryRegisterQuestionFile(_questionFile);

        _ = _server.RunAsync();
    }


    [TestInitialize]
    public void TestInit()
    {
        File.WriteAllText(_questionFile, string.Empty);
        _storage.Clear();
        _client.DefaultRequestHeaders.Authorization = null;
    }


    [TestMethod]
    public async Task PostingBadUser()
    {
        var response = await _client.PostAsJsonAsync("users",
            new VoteOptionRecord("user", [ "pass" ]),
            TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.BadRequest, (int) response.StatusCode);
    }


    [TestMethod]
    public async Task PostingSingleUser()
    {
        var response = await UserPost(new JoinRecord("_1test", "ö12-*#"));

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task PostingSecondUniqueUser()
    {
        await UserPost(new JoinRecord("_balls123", "shat"));
        var response = await UserPost(new JoinRecord("ilawte", ""));

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task PostingSecondSameUser()
    {
        await UserPost(new JoinRecord("1", "jidw-w.a-,"));
        var response = await UserPost(new JoinRecord("1", "newPass"));

        Assert.AreEqual((int) HttpStatusCode.Forbidden, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task PostingUserIllegalUsername()
    {
        var response = await UserPost(new JoinRecord(">", "newPass"));

        Assert.AreEqual((int) HttpStatusCode.BadRequest, (int) response.StatusCode);
    }

    [TestMethod]
    public async Task PostingSecondSameUserAcceptsOldPass()
    {
        // Can fail because getting users is incorrect

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
    public async Task PostingSecondSameUserDoesntAcceptNewPass()
    {
        // Can fail because getting users is incorrect

        await UserPost(new JoinRecord("test1", ""));
        await UserPost(new JoinRecord("_test2", ""));
        await UserPost(new JoinRecord("-test3", "hello"));
        await UserPost(new JoinRecord("-test3", "shat"));

        _client.AddBasicAuthHeader("-test3", "shat");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.Unauthorized, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task GettingUsersUnauthenticated()
    {
        await UserPost(new JoinRecord("test1", ""));
        await UserPost(new JoinRecord("_test2", ""));
        await UserPost(new JoinRecord("-test3", "hello"));

        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.Unauthorized, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task GettingUsersIncorrectlyAuthenticated()
    {
        await UserPost(new JoinRecord("test1", ""));
        await UserPost(new JoinRecord("_test2", ""));
        await UserPost(new JoinRecord("-test3", "hello"));

        _client.AddBasicAuthHeader("-test3", "hellp");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.Unauthorized, (int) response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }


    [TestMethod]
    public async Task GettingSingleUser()
    {
        await UserPost(new JoinRecord("-test3", "hello"));

        // The authentication header appears to get lost in transmission?
        _client.AddBasicAuthHeader("-test3", "hello");
        var response = await _client.GetAsync("users", TestContext.CancellationToken);
        var actualUnsplit = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        CollectionAssert.AreEquivalent(
            new string[] { "-test3" },
            actualUnsplit.Split('\n'));
    }


    [TestMethod]
    public async Task GettingMultipleUsers()
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
    public async Task NewRandomEntryWhenQuestionsAvailable()
    {
        File.WriteAllText(_questionFile, "Q1\n"
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
        Assert.IsTrue(_storage.NewRandomEntry());
        Assert.IsTrue(_storage.NewRandomEntry());
        Assert.IsTrue(_storage.NewRandomEntry());
        Assert.IsTrue(_storage.NewRandomEntry());
    }


    [TestMethod]
    public async Task NewRandomEntryWhenNoQuestionsAvailable()
    {
        File.WriteAllText(_questionFile, "Q1\n"
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
        _storage.NewRandomEntry();
        _storage.NewRandomEntry();
        _storage.NewRandomEntry();
        _storage.NewRandomEntry();
        Assert.IsFalse(_storage.NewRandomEntry());
    }


    [TestMethod]
    public async Task NewRandomEntryWhenQuestionsAvailableAltStorageMixed()
    {
        File.WriteAllText(_questionFile, "Q1\n"
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
        Assert.IsTrue(_storage.NewRandomEntry());
        Assert.IsTrue(_altStorage.NewRandomEntry());
        Assert.IsTrue(_altStorage.NewRandomEntry());
        Assert.IsTrue(_storage.NewRandomEntry());
    }


    [TestMethod]
    public async Task NewRandomEntryWhenNoQuestionsAvailableAltStorageMixed()
    {
        File.WriteAllText(_questionFile, "Q1\n"
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
        _storage.NewRandomEntry();
        _altStorage.NewRandomEntry();
        _altStorage.NewRandomEntry();
        _storage.NewRandomEntry();
        Assert.IsFalse(_storage.NewRandomEntry());
    }


    [TestMethod]
    public async Task GetCurrentEntryWhenNoExisting()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        var response = await _client.GetAsync("entry/current", TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.NoContent, (int) response.StatusCode);
    }


    [TestMethod]
    public async Task GetCurrentEntryUnauthorized()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", "test"));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(_questionFile, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes");
        _storage.NewRandomEntry();

        var response = await _client.GetAsync("entry/current", TestContext.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.Unauthorized, (int) response.StatusCode);
        Assert.AreEqual("", responseText);
    }


    [TestMethod]
    public async Task GetCurrentEntryWhenExisting()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(_questionFile, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes");
        _storage.NewRandomEntry();

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
            actualEntry.voteOptions.Select(v => v.votingWhat).ToList());
        Assert.IsTrue(actualEntry.voteOptions.All(v => v.votedBy!.Length == 0));
    }


    [TestMethod]
    public async Task GetCurrentEntryDifficultQuestionFile()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(_questionFile, "\tDo you still love me?\n"
            + "    No\n"
            + "Yes    \n"
            + "Sometimes\n ");
        _storage.NewRandomEntry();

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
            actualEntry.voteOptions.Select(v => v.votingWhat).ToList());
        Assert.IsTrue(actualEntry.voteOptions.All(v => v.votedBy!.Length == 0));
    }


    [TestMethod]
    public async Task GetCurrentEntryWithUserVoteOptions()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user1", ""));
        _client.AddBasicAuthHeader("user1", "");

        await UserPost(new JoinRecord("user2", ""));
        await UserPost(new JoinRecord("user3", ""));

        File.WriteAllText(_questionFile, "Userquestion?\n"
            + ":u\n"
            + "Nobody");
        _storage.NewRandomEntry();

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
            actualEntry.voteOptions.Select(v => v.votingWhat).ToList());
        Assert.IsTrue(actualEntry.voteOptions.All(v => v.votedBy!.Length == 0)); // No one voted
    }


    [TestMethod]
    public async Task GetPastEntry()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(_questionFile, "Who sucks more?\n"
            + "Price\n"
            + "Picard\n"
            + "Susane\n"
            + "Your mother");
        _storage.NewRandomEntry();

        File.AppendAllText(_questionFile, "\n\nTest\n"
            + "test1\n"
            + "test2\n");
        _storage.NewRandomEntry();

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
            actualEntry.voteOptions.Select(v => v.votingWhat).ToList());
        Assert.IsTrue(actualEntry.voteOptions.All(v => v.votedBy!.Length == 0));
    }


    [TestMethod]
    public async Task GetMultiplePastEntries()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(_questionFile, "Q1\n"
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
        _storage.NewRandomEntry();
        _storage.NewRandomEntry();
        _storage.NewRandomEntry();
        _storage.NewRandomEntry();

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
            "entry returns incorrect questions: " + string.Join(", ", actualEntries.Select(e => e.question))
        );
        CollectionAssert.IsSubsetOf // Good enough. Users will notice incoherent voteoptions anyway
        (
            actualEntries.SelectMany(e => e.voteOptions!).Select(v => v.votingWhat).ToList(),
            new string[] { "Q1V1", "Q1V2", "Q2V1", "Q2V2", "Q3V1", "Q3V2", "Q4V1", "Q4V2" },
            "entry returns incorrect voteOptions"
        );
    }


    [TestMethod]
    public async Task Vote()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(_questionFile, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes\n");
        _storage.NewRandomEntry();

        var response = await _client.PostAsync("entry/vote", new StringContent("Yes", Encoding.UTF8), TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }


    [TestMethod]
    public async Task VoteIncorrectOption()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(_questionFile, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes\n");
        _storage.NewRandomEntry();

        var response = await _client.PostAsync("entry/vote", new StringContent("Wiener", Encoding.UTF8), TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.BadRequest, (int) response.StatusCode);
    }


    [TestMethod]
    public async Task OverwriteVote()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user", ""));
        _client.AddBasicAuthHeader("user", "");

        File.WriteAllText(_questionFile, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes\n");
        _storage.NewRandomEntry();

        await _client.PostAsync("entry/vote", new StringContent("No", Encoding.UTF8), TestContext.CancellationToken);
        var response = await _client.PostAsync("entry/vote", new StringContent("Yes", Encoding.UTF8), TestContext.CancellationToken);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
    }


    [TestMethod]
    public async Task CurrentEntryShowsAllVotes()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user1", ""));
        await UserPost(new JoinRecord("user2", ""));
        await UserPost(new JoinRecord("user3", ""));
        await UserPost(new JoinRecord("user4", ""));
        await UserPost(new JoinRecord("user5", ""));
        await UserPost(new JoinRecord("user6", ""));

        File.WriteAllText(_questionFile, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes\n");
        _storage.NewRandomEntry();

        _client.AddBasicAuthHeader("user1", "");
        await _client.PostAsync("entry/vote", new StringContent("Yes", Encoding.UTF8), TestContext.CancellationToken);

        _client.AddBasicAuthHeader("user6", "");
        await _client.PostAsync("entry/vote", new StringContent("Yes", Encoding.UTF8), TestContext.CancellationToken);

        _client.AddBasicAuthHeader("user3", "");
        await _client.PostAsync("entry/vote", new StringContent("No", Encoding.UTF8), TestContext.CancellationToken);


        var response = await _client.GetAsync("entry/current", TestContext.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var actual = JsonSerializer.Deserialize<EntryRecord>(responseText);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        var yesVoteOption = actual!.voteOptions!.First(v => v.votingWhat == "Yes");
        CollectionAssert.AreEquivalent(new string[] { "user1", "user6" }, yesVoteOption.votedBy);
        var noVoteOption = actual!.voteOptions!.First(v => v.votingWhat == "No");
        CollectionAssert.AreEquivalent(new string[] { "user3" }, noVoteOption.votedBy);
        var sometimesVoteOption = actual!.voteOptions!.First(v => v.votingWhat == "Sometimes");
        CollectionAssert.AreEquivalent(new string[] { }, sometimesVoteOption.votedBy);
    }


    [TestMethod]
    public async Task CurrentEntryShowOverwrittenVote()
    {
        // Create user and authenticate client
        await UserPost(new JoinRecord("user1", ""));

        File.WriteAllText(_questionFile, "Do you still love me?\n"
            + "No\n"
            + "Yes\n"
            + "Sometimes\n");
        _storage.NewRandomEntry();

        _client.AddBasicAuthHeader("user1", "");
        await _client.PostAsync("entry/vote", new StringContent("Yes", Encoding.UTF8), TestContext.CancellationToken);
        await _client.PostAsync("entry/vote", new StringContent("No", Encoding.UTF8), TestContext.CancellationToken);


        var response = await _client.GetAsync("entry/current", TestContext.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var actual = JsonSerializer.Deserialize<EntryRecord>(responseText);

        Assert.AreEqual((int) HttpStatusCode.OK, (int) response.StatusCode);
        var yesVoteOption = actual!.voteOptions!.First(v => v.votingWhat == "Yes");
        CollectionAssert.AreEquivalent(new string[] { }, yesVoteOption.votedBy);
        var noVoteOption = actual!.voteOptions!.First(v => v.votingWhat == "No");
        CollectionAssert.AreEquivalent(new string[] { "user1" }, noVoteOption.votedBy);
        var sometimesVoteOption = actual!.voteOptions!.First(v => v.votingWhat == "Sometimes");
        CollectionAssert.AreEquivalent(new string[] { }, sometimesVoteOption.votedBy);
    }


    private async Task<HttpResponseMessage> UserPost(JoinRecord joinRecord)
    {
        return await _client.PostAsJsonAsync("users", joinRecord, TestContext.CancellationToken);
    }
}

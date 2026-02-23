using System.Text;

namespace TheAssembly.Server.Test;

[TestClass]
public class JoinedTokenTests
{
    [TestMethod]
    public void Simple()
    {
        var expectedData1 = new User("Stipan", ["None", "YourMother"]).Serialize();
        var expectedData2 = Encoding.ASCII.GetBytes("HelloWorld");

        var oldJoined = new JoinedToken(expectedData1, expectedData2);
        var serializedJoined = oldJoined.Serialize();
        var newJoined = JoinedToken.Deserialize(serializedJoined);

        Assert.IsNotNull(newJoined);
        Assert.AreEqual(2, newJoined.TokenCount());
        Assert.IsTrue(expectedData1.SequenceEqual(newJoined.GetEncodedToken(0)), "Expected data not equal actual data");
        Assert.IsTrue(expectedData2.SequenceEqual(newJoined.GetEncodedToken(1)), "Expected data not equal actual data");
    }


    [TestMethod]
    public void EmptyTokens()
    {
        var expectedData1 = Array.Empty<byte>();
        var expectedData2 = Array.Empty<byte>();

        var oldJoined = new JoinedToken(expectedData1, expectedData2);
        var serializedJoined = oldJoined.Serialize();
        var newJoined = JoinedToken.Deserialize(serializedJoined);

        Assert.IsNotNull(newJoined);
        Assert.AreEqual(2, newJoined.TokenCount());
        Assert.IsTrue(expectedData1.SequenceEqual(newJoined.GetEncodedToken(0)), "Expected data not equal actual data");
        Assert.IsTrue(expectedData2.SequenceEqual(newJoined.GetEncodedToken(1)), "Expected data not equal actual data");
    }
}

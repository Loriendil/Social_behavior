using RelationshipCore.Nodes;

namespace RelationshipCore.Tests;

public class NodeTests
{
    [Fact]
    public void HandleMessage_DoesNotThrow()
    {
        var node = new Node(1);

        var exception = Record.Exception(() => node.HandleMessage(new StubMessage()));

        Assert.Null(exception);
    }

    private sealed class StubMessage : IMessage
    {
    }
}

using RelationshipCore.Graphs;
using RelationshipCore.Nodes;

namespace RelationshipCore.Tests;

public class GraphTests
{
    [Fact]
    public void AddNode_RegistersNodeOnce()
    {
        var graph = new Graph();
        var alice = new Node(1);

        graph.AddNode(alice);
        graph.AddNode(alice);

        Assert.True(graph.IsGraphed(alice));
        Assert.Equal(1, graph.Count);
    }

    [Fact]
    public void RemoveNode_UnregistersNode()
    {
        var graph = new Graph();
        var alice = new Node(1);
        graph.AddNode(alice);

        var removed = graph.RemoveNode(alice);

        Assert.True(removed);
        Assert.False(graph.IsGraphed(alice));
    }

    [Fact]
    public void GetNode_ReturnsNullWhenMissing()
    {
        var graph = new Graph();

        Assert.Null(graph.GetNode(42));
    }

    [Fact]
    public void GetNode_ReturnsRegisteredNode()
    {
        var graph = new Graph();
        var alice = new Node(1);
        graph.AddNode(alice);

        Assert.Same(alice, graph.GetNode(1));
    }
}

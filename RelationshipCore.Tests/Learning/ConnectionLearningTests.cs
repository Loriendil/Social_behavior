using RelationshipCore.Dynamics;
using RelationshipCore.Edges;
using RelationshipCore.Graphs;
using RelationshipCore.Learning;
using RelationshipCore.Nodes;

namespace RelationshipCore.Tests.Learning;

public class ConnectionLearningTests
{
    [Fact]
    public void Learn_NoExistingKnowledge_AdoptsReceivedConnection()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var charlie = new Node(3);
        var received = new Edge(bob, charlie, new SocialRelation(liking: 0.6f, dominance: 0f, familiarity: 0f, solidarity: 0f));

        bool adopted = ConnectionLearning.Learn(graph, alice, received, RelationshipWeights.SocialRelation);

        Assert.True(adopted);
        Assert.Same(received, graph.GetNodeEdge(alice, bob, charlie));
    }

    [Fact]
    public void Learn_StoresAsIndirectEdge_WhenLearnerIsThirdParty()
    {
        // Дисс. О'Коннора, раздел 3.1.1: Alice узнаёт о (возможно, неточном) отношении Tom->Charles
        // — это косвенное ребро у Alice, не прямое, и не обязано совпадать с реальным.
        var graph = new DeepGraph();
        var alice = new Node(1);
        var tom = new Node(2);
        var charles = new Node(3);
        var received = new Edge(tom, charles, new SocialRelation(liking: 0.8f, dominance: 0f, familiarity: 0f, solidarity: 0f));

        ConnectionLearning.Learn(graph, alice, received, RelationshipWeights.SocialRelation);

        Assert.Single(graph.GetIndirectEdges(alice));
        Assert.Empty(graph.GetDirectEdges(alice));
    }

    [Fact]
    public void Learn_ReceivedStrongerThanExisting_ReplacesIt()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var charlie = new Node(3);

        var weak = new Edge(bob, charlie, new SocialRelation(liking: 0.1f, dominance: 0f, familiarity: 0f, solidarity: 0f));
        graph.AddEdge(alice, weak);

        var strong = new Edge(bob, charlie, new SocialRelation(liking: -0.9f, dominance: 0f, familiarity: 0f, solidarity: 0f));
        bool adopted = ConnectionLearning.Learn(graph, alice, strong, RelationshipWeights.SocialRelation);

        Assert.True(adopted);
        var current = (SocialRelation)graph.GetNodeEdge(alice, bob, charlie)!.Relationship;
        Assert.Equal(-0.9f, current.Liking);
    }

    [Fact]
    public void Learn_ReceivedWeakerThanExisting_KeepsExisting()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var charlie = new Node(3);

        var strong = new Edge(bob, charlie, new SocialRelation(liking: -0.9f, dominance: 0f, familiarity: 0f, solidarity: 0f));
        graph.AddEdge(alice, strong);

        var weak = new Edge(bob, charlie, new SocialRelation(liking: 0.1f, dominance: 0f, familiarity: 0f, solidarity: 0f));
        bool adopted = ConnectionLearning.Learn(graph, alice, weak, RelationshipWeights.SocialRelation);

        Assert.False(adopted);
        var current = (SocialRelation)graph.GetNodeEdge(alice, bob, charlie)!.Relationship;
        Assert.Equal(-0.9f, current.Liking);
    }

    [Fact]
    public void Learn_ReceivedEqualWeightToExisting_KeepsExisting()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var charlie = new Node(3);

        var first = new Edge(bob, charlie, new SocialRelation(liking: 0.5f, dominance: 0f, familiarity: 0f, solidarity: 0f));
        graph.AddEdge(alice, first);

        var sameWeight = new Edge(bob, charlie, new SocialRelation(liking: -0.5f, dominance: 0f, familiarity: 0f, solidarity: 0f));
        bool adopted = ConnectionLearning.Learn(graph, alice, sameWeight, RelationshipWeights.SocialRelation);

        Assert.False(adopted);
        var current = (SocialRelation)graph.GetNodeEdge(alice, bob, charlie)!.Relationship;
        Assert.Equal(0.5f, current.Liking);
    }

    [Fact]
    public void Learn_ViaConnectionMessage_DelegatesToEdgeOverload()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var charlie = new Node(3);
        var received = new Edge(bob, charlie, new SocialRelation(liking: 0.6f, dominance: 0f, familiarity: 0f, solidarity: 0f));

        bool adopted = ConnectionLearning.Learn(graph, alice, new ConnectionMessage(received), RelationshipWeights.SocialRelation);

        Assert.True(adopted);
        Assert.Same(received, graph.GetNodeEdge(alice, bob, charlie));
    }

    [Fact]
    public void RelationshipWeights_SocialRelation_SumsAbsoluteMagnitudes()
    {
        var relation = new SocialRelation(liking: -0.5f, dominance: 0.3f, familiarity: 0.2f, solidarity: 0.1f);

        float weight = RelationshipWeights.SocialRelation(relation);

        Assert.Equal(1.1f, weight, precision: 3);
    }
}

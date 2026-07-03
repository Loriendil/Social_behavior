using RelationshipCore.Dynamics;
using RelationshipCore.Edges;
using RelationshipCore.Graphs;
using RelationshipCore.Nodes;

namespace RelationshipCore.Tests;

public class DeepGraphTests
{
    private static readonly IRelationship Friend = new NamedRelationship("FRIEND");
    private static readonly IRelationship Enemy = new NamedRelationship("ENEMY");

    [Fact]
    public void AddDirectEdge_IsRetrievableAsDirectEdge()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);

        graph.AddDirectEdge(new Edge(alice, bob, Friend));

        Assert.True(graph.HasEdge(alice, bob));
        Assert.Single(graph.GetDirectEdges(alice));
        Assert.Empty(graph.GetIndirectEdges(alice));
    }

    [Fact]
    public void AddEdge_WithDifferentOwner_IsIndirect()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var carol = new Node(3);

        // Керол считает (возможно, ошибочно), что Alice и Bob враги — косвенное ребро
        graph.AddEdge(carol, new Edge(alice, bob, Enemy));

        Assert.Empty(graph.GetDirectEdges(carol));
        Assert.Single(graph.GetIndirectEdges(carol));
        Assert.False(graph.HasEdge(alice, bob));
    }

    [Fact]
    public void AddEdge_Duplicate_UpdatesRelationshipInsteadOfDuplicating()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);

        graph.AddDirectEdge(new Edge(alice, bob, Friend));
        graph.AddDirectEdge(new Edge(alice, bob, Enemy));

        var edges = graph.GetDirectEdges(alice);

        Assert.Single(edges);
        Assert.True(edges[0].Relationship.Matches(Enemy));
    }

    [Fact]
    public void AddCommonEdge_CreatesBothDirections()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);

        graph.AddCommonEdge(alice, bob, Friend, Friend);

        Assert.True(graph.HasEdge(alice, bob));
        Assert.True(graph.HasEdge(bob, alice));
    }

    [Fact]
    public void WithRelationship_ReturnsMatchingTargets()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var carol = new Node(3);

        graph.AddDirectEdge(new Edge(alice, bob, Friend));
        graph.AddDirectEdge(new Edge(alice, carol, Enemy));

        var friends = graph.WithRelationship(alice, Friend);

        Assert.Single(friends);
        Assert.Equal(bob.EntityId, friends[0].EntityId);
    }

    [Fact]
    public void WithRelationshipTo_ReturnsMatchingSources()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var carol = new Node(3);

        graph.AddDirectEdge(new Edge(alice, carol, Friend));
        graph.AddDirectEdge(new Edge(bob, carol, Enemy));

        var friendsOfCarol = graph.WithRelationshipTo(carol, Friend);

        Assert.Single(friendsOfCarol);
        Assert.Equal(alice.EntityId, friendsOfCarol[0].EntityId);
    }

    [Fact]
    public void AddEdge_DifferentRelationshipTypes_SamePair_CoexistWithoutClobbering()
    {
        // WideEdge (дисс. О'Коннора §6.2.6): структурное MEMBER (NamedRelationship) и динамическое
        // SocialRelation слоя Ochs между одной и той же парой узлов не должны конфликтовать за слот.
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var member = new NamedRelationship("MEMBER");
        var social = new SocialRelation(liking: 0.5f, dominance: 0f, familiarity: 0f, solidarity: 0f);

        graph.AddDirectEdge(new Edge(alice, bob, member));
        graph.AddDirectEdge(new Edge(alice, bob, social));

        Assert.Equal(2, graph.GetEdges(alice, alice, bob).Count);
        Assert.True(graph.GetEdge<NamedRelationship>(alice, bob)!.Relationship.Matches(member));
        Assert.Equal(social, graph.GetEdge<SocialRelation>(alice, bob)!.Relationship);
    }

    [Fact]
    public void AddEdge_SameRelationshipTypeAgain_StillUpsertsWithoutDuplicating()
    {
        // Регрессия: обновление ОДНОГО И ТОГО ЖЕ типа Relationship должно по-прежнему заменять
        // существующее ребро, а не плодить дубликаты внутри одного типа.
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);

        graph.AddDirectEdge(new Edge(alice, bob, new SocialRelation(0.1f, 0f, 0f, 0f)));
        graph.AddDirectEdge(new Edge(alice, bob, new SocialRelation(0.9f, 0f, 0f, 0f)));

        Assert.Single(graph.GetEdges(alice, alice, bob));
        var relation = (SocialRelation)graph.GetEdge<SocialRelation>(alice, bob)!.Relationship;
        Assert.Equal(0.9f, relation.Liking);
    }

    [Fact]
    public void GetEdge_Untyped_ReturnsFirstMatchRegardlessOfType()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        graph.AddDirectEdge(new Edge(alice, bob, Friend));
        graph.AddDirectEdge(new Edge(alice, bob, new SocialRelation(0.2f, 0f, 0f, 0f)));

        // Untyped GetEdge не гарантирует, КАКОЙ из двух типов вернётся — только что ребро есть.
        Assert.NotNull(graph.GetEdge(alice, bob));
        Assert.Equal(2, graph.GetEdges(alice, alice, bob).Count);
    }

    [Fact]
    public void RemoveDirectEdge_RemovesIt()
    {
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);
        var edge = new Edge(alice, bob, Friend);
        graph.AddDirectEdge(edge);

        var removed = graph.RemoveDirectEdge(edge);

        Assert.True(removed);
        Assert.False(graph.HasEdge(alice, bob));
    }
}

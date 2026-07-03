using RelationshipCore.Edges;
using RelationshipCore.Graphs;
using RelationshipCore.Nodes;

namespace RelationshipCore.Tests;

public class HistoryEdgeTests
{
    private static readonly IRelationship Friend = new NamedRelationship("FRIEND");
    private static readonly IRelationship Enemy = new NamedRelationship("ENEMY");
    private static readonly IRelationship Rival = new NamedRelationship("RIVAL");

    [Fact]
    public void Constructor_SetsInitialRelationshipAsCurrentAndAsOnlyHistoryEntry()
    {
        var alice = new Node(1);
        var bob = new Node(2);

        var edge = new HistoryEdge(alice, bob, Friend);

        Assert.True(edge.Relationship.Matches(Friend));
        Assert.Single(edge.History);
        Assert.Equal(1, edge.Count);
    }

    [Fact]
    public void SettingRelationship_AppendsToHistoryInsteadOfReplacing()
    {
        var alice = new Node(1);
        var bob = new Node(2);
        var edge = new HistoryEdge(alice, bob, Friend);

        edge.Relationship = Enemy;
        edge.Relationship = Rival;

        Assert.Equal(3, edge.Count);
        Assert.True(edge.Relationship.Matches(Rival)); // текущее — последнее установленное
    }

    [Fact]
    public void History_ReturnsAllValuesOldestToNewest()
    {
        var alice = new Node(1);
        var bob = new Node(2);
        var edge = new HistoryEdge(alice, bob, Friend);

        edge.Relationship = Enemy;
        edge.Relationship = Rival;

        Assert.True(edge.History[0].Matches(Friend));
        Assert.True(edge.History[1].Matches(Enemy));
        Assert.True(edge.History[2].Matches(Rival));
    }

    [Fact]
    public void PreviousRelationship_ReturnsCorrectPastValue()
    {
        var alice = new Node(1);
        var bob = new Node(2);
        var edge = new HistoryEdge(alice, bob, Friend);
        edge.Relationship = Enemy;
        edge.Relationship = Rival;

        Assert.True(edge.PreviousRelationship(1)!.Matches(Enemy));
        Assert.True(edge.PreviousRelationship(2)!.Matches(Friend));
    }

    [Fact]
    public void PreviousRelationship_BeyondHistoryLength_ReturnsNull()
    {
        var alice = new Node(1);
        var bob = new Node(2);
        var edge = new HistoryEdge(alice, bob, Friend);

        Assert.Null(edge.PreviousRelationship(1));
        Assert.Null(edge.PreviousRelationship(100));
    }

    [Fact]
    public void DeepGraph_AddEdge_OnExistingHistoryEdge_AppendsRatherThanReplacing()
    {
        // Ключевая интеграция: DeepGraph.AddEdge вызывает Relationship-сеттер УЖЕ НАЙДЕННОГО ребра.
        // Если это ребро — HistoryEdge, а апдейт пришёл через обычный throwaway Edge (как это
        // делает SocialDynamicsEngine.UpdateRelation), запись в историю происходит автоматически,
        // без изменений вызывающего кода.
        var graph = new DeepGraph();
        var alice = new Node(1);
        var bob = new Node(2);

        graph.AddDirectEdge(new HistoryEdge(alice, bob, Friend));
        graph.AddDirectEdge(new Edge(alice, bob, Enemy)); // throwaway Edge, не HistoryEdge

        var stored = graph.GetEdge(alice, bob);
        Assert.IsType<HistoryEdge>(stored);

        var historyEdge = (HistoryEdge)stored!;
        Assert.Equal(2, historyEdge.Count);
        Assert.True(historyEdge.History[0].Matches(Friend));
        Assert.True(historyEdge.Relationship.Matches(Enemy));
    }
}

using RelationshipCore.Dynamics;
using RelationshipCore.Graphs;
using RelationshipCore.Nodes;
using RelationshipCore.Simulation;
using RelationshipCore.Tests.Dynamics;

namespace RelationshipCore.Tests.Simulation;

public class SocialRoleTableTests
{
    [Fact]
    public void Resolve_NoMatchingRoles_ReturnsNeutral()
    {
        var table = new SocialRoleTable();

        var relation = table.Resolve(new[] { "cop" }, new[] { "gangster" });

        FloatAssert.Approximately(0f, relation.Liking);
        FloatAssert.Approximately(0f, relation.Dominance);
        FloatAssert.Approximately(0f, relation.Familiarity);
        FloatAssert.Approximately(0f, relation.Solidarity);
    }

    [Fact]
    public void Resolve_SingleMatchingPair_ReturnsExactRelation()
    {
        var table = new SocialRoleTable();
        table.Set("cop", "gangster", new SocialRelation(liking: -0.5f, dominance: 0.3f, familiarity: 0.2f, solidarity: 0f));

        var relation = table.Resolve(new[] { "cop" }, new[] { "gangster" });

        FloatAssert.Approximately(-0.5f, relation.Liking);
        FloatAssert.Approximately(0.3f, relation.Dominance);
    }

    [Fact]
    public void Resolve_MultipleRoles_AveragesApplicablePairs()
    {
        var table = new SocialRoleTable();
        table.Set("cop", "gangster", new SocialRelation(liking: -0.6f, dominance: 0f, familiarity: 0f, solidarity: 0f));
        table.Set("veteran", "gangster", new SocialRelation(liking: -0.2f, dominance: 0f, familiarity: 0f, solidarity: 0f));

        var relation = table.Resolve(new[] { "cop", "veteran" }, new[] { "gangster" });

        FloatAssert.Approximately(-0.4f, relation.Liking);
    }

    [Fact]
    public void ApplyTo_CreatesIndependentEdgesInBothDirections()
    {
        var table = new SocialRoleTable();
        table.Set("cop", "gangster", new SocialRelation(liking: -0.5f, dominance: 0.3f, familiarity: 0f, solidarity: 0f));
        table.Set("gangster", "cop", new SocialRelation(liking: -0.5f, dominance: -0.3f, familiarity: 0f, solidarity: 0f));

        var graph = new DeepGraph();
        var cop = new Node(1);
        var gangster = new Node(2);
        graph.AddNode(cop);
        graph.AddNode(gangster);

        table.ApplyTo(graph, cop, gangster, new[] { "cop" }, new[] { "gangster" });

        var copToGangster = (SocialRelation)graph.GetEdge(cop, gangster)!.Relationship;
        var gangsterToCop = (SocialRelation)graph.GetEdge(gangster, cop)!.Relationship;

        FloatAssert.Approximately(0.3f, copToGangster.Dominance);
        FloatAssert.Approximately(-0.3f, gangsterToCop.Dominance);
    }
}

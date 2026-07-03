using RelationshipCore.Dynamics;
using RelationshipCore.Edges;
using RelationshipCore.Graphs;
using RelationshipCore.Groups;
using RelationshipCore.Nodes;

namespace RelationshipCore.Tests.Groups;

public class GroupRelationsTests
{
    [Fact]
    public void MembersOf_ReturnsOnlyNodesWithMemberEdgeToGroup()
    {
        var graph = new DeepGraph();
        var group = new Node(1);
        var m1 = new Node(2);
        var m2 = new Node(3);
        var outsider = new Node(4);

        graph.AddDirectEdge(new Edge(m1, group, MemberRelationship.Instance));
        graph.AddDirectEdge(new Edge(m2, group, MemberRelationship.Instance));
        graph.AddDirectEdge(new Edge(outsider, group, new SocialRelation(liking: 0.9f, dominance: 0f, familiarity: 0f, solidarity: 0f)));

        var members = GroupRelations.MembersOf(graph, group);

        Assert.Equal(2, members.Count);
        Assert.Contains(members, n => n.EntityId == m1.EntityId);
        Assert.Contains(members, n => n.EntityId == m2.EntityId);
        Assert.DoesNotContain(members, n => n.EntityId == outsider.EntityId);
    }

    [Fact]
    public void GroupsOf_ReturnsGroupsMemberBelongsTo()
    {
        var graph = new DeepGraph();
        var group = new Node(1);
        var member = new Node(2);
        graph.AddDirectEdge(new Edge(member, group, MemberRelationship.Instance));

        var groups = GroupRelations.GroupsOf(graph, member);

        Assert.Single(groups);
        Assert.Equal(group.EntityId, groups[0].EntityId);
    }

    [Fact]
    public void HasInheritedRelationship_NoDirectRelation_InheritsFromGroup()
    {
        // "враг группы — враг каждого члена" (дисс. О'Коннора, рис. 3.3).
        var graph = new DeepGraph();
        var group = new Node(1);
        var member = new Node(2);
        var enemy = new Node(3);
        var enemyPattern = new SocialRelationPattern { Liking = FloatRange.AtMost(-0.5f) };

        graph.AddDirectEdge(new Edge(member, group, MemberRelationship.Instance));
        graph.AddDirectEdge(new Edge(group, enemy, new SocialRelation(liking: -0.9f, dominance: 0f, familiarity: 0f, solidarity: 0f)));

        Assert.True(GroupRelations.HasInheritedRelationship(graph, member, enemy, enemyPattern));
    }

    [Fact]
    public void HasInheritedRelationship_DirectRelationMatchesWithoutNeedingGroup()
    {
        var graph = new DeepGraph();
        var member = new Node(1);
        var personalEnemy = new Node(2);
        var enemyPattern = new SocialRelationPattern { Liking = FloatRange.AtMost(-0.5f) };

        graph.AddDirectEdge(new Edge(member, personalEnemy, new SocialRelation(liking: -0.8f, dominance: 0f, familiarity: 0f, solidarity: 0f)));

        Assert.True(GroupRelations.HasInheritedRelationship(graph, member, personalEnemy, enemyPattern));
    }

    [Fact]
    public void HasInheritedRelationship_NoDirectAndNoGroupMatch_ReturnsFalse()
    {
        var graph = new DeepGraph();
        var group = new Node(1);
        var member = new Node(2);
        var stranger = new Node(3);
        var enemyPattern = new SocialRelationPattern { Liking = FloatRange.AtMost(-0.5f) };

        graph.AddDirectEdge(new Edge(member, group, MemberRelationship.Instance));
        graph.AddDirectEdge(new Edge(group, stranger, new SocialRelation(liking: 0.1f, dominance: 0f, familiarity: 0f, solidarity: 0f)));

        Assert.False(GroupRelations.HasInheritedRelationship(graph, member, stranger, enemyPattern));
    }

    [Fact]
    public void Broadcast_DeliversMessageToMembersOnly()
    {
        var graph = new DeepGraph();
        var group = new Node(1);
        var m1 = new RecordingNode(2);
        var m2 = new RecordingNode(3);
        var outsider = new RecordingNode(4);

        graph.AddDirectEdge(new Edge(m1, group, MemberRelationship.Instance));
        graph.AddDirectEdge(new Edge(m2, group, MemberRelationship.Instance));

        var message = new TestMessage();
        GroupRelations.Broadcast(graph, group, message);

        Assert.Same(message, m1.LastReceived);
        Assert.Same(message, m2.LastReceived);
        Assert.Null(outsider.LastReceived);
    }

    private sealed class RecordingNode : Node
    {
        public RecordingNode(int entityId) : base(entityId)
        {
        }

        public IMessage? LastReceived { get; private set; }

        public override void HandleMessage(IMessage message) => LastReceived = message;
    }

    private sealed class TestMessage : IMessage
    {
    }
}

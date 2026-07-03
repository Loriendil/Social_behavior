using RelationshipCore.Dynamics;
using RelationshipCore.Edges;
using RelationshipCore.Graphs;
using RelationshipCore.Nodes;
using RelationshipCore.Simulation;

namespace RelationshipCore.Tests.Simulation;

public class SocialDynamicsEngineTests
{
    private static readonly ActionId Arrest = new(1);
    private static readonly ActionId Offer = new(2);

    private static SocialDynamicsEngine CreateEngine(out DeepGraph graph, out ActionDictionary actions)
    {
        graph = new DeepGraph();
        actions = new ActionDictionary();
        return new SocialDynamicsEngine(graph, actions);
    }

    [Fact]
    public void RegisterNpc_SameEntityIdTwice_ReturnsSameState()
    {
        var engine = CreateEngine(out _, out _);

        var first = engine.RegisterNpc(1);
        var second = engine.RegisterNpc(1, new Personality(0.9f, 0.9f));

        Assert.Same(first, second);
        FloatAssertEqualPersonality(Personality.Neutral, second.Personality);
    }

    [Fact]
    public void GetState_Unregistered_Throws()
    {
        var engine = CreateEngine(out _, out _);

        Assert.Throws<InvalidOperationException>(() => engine.GetState(42));
    }

    [Fact]
    public void Perceive_NegativeEventAgainstSelfInterest_ProducesFearOrDistress()
    {
        var engine = CreateEngine(out var graph, out var actions);
        actions.SetEffect(Arrest, -0.8f);

        var burglar = engine.RegisterNpc(1);
        var policeman = engine.RegisterNpc(2);
        graph.AddNode(new Node(1));
        graph.AddNode(new Node(2));

        burglar.Appraisal.SetAttitude(1, 0.9f); // высокое самоуважение — арест плохого для себя события

        engine.Perceive(1, new GameEvent(agentId: 2, action: Arrest, patientId: 1, dc: 0.8f), time: 0f);

        Assert.True(burglar.Emotions[EmotionKind.Fear] > 0f);
    }

    [Fact]
    public void Perceive_UpdatesRelationTowardCounterparty()
    {
        var engine = CreateEngine(out var graph, out var actions);
        actions.SetEffect(Arrest, -0.8f);

        var burglar = engine.RegisterNpc(1);
        engine.RegisterNpc(2);
        var burglarNode = new Node(1);
        var policemanNode = new Node(2);
        graph.AddNode(burglarNode);
        graph.AddNode(policemanNode);
        graph.AddCommonEdge(burglarNode, policemanNode, SocialRelation.Neutral, SocialRelation.Neutral);

        burglar.Appraisal.SetAttitude(1, 0.9f);

        engine.Perceive(1, new GameEvent(agentId: 2, action: Arrest, patientId: 1, dc: 0.8f), time: 0f);

        var relation = (SocialRelation)graph.GetEdge(burglarNode, policemanNode)!.Relationship;
        Assert.True(relation.Liking < 0f);
    }

    [Fact]
    public void Perceive_NonMonotonicTime_DoesNotRewindLastUpdateTime()
    {
        // Регрессия: DecayToTime раньше безусловно перезаписывал LastUpdateTime, даже если
        // переданное time было меньше предыдущего — следующий вызов после этого затухал бы
        // эмоции сильнее, чем реально прошло времени (см. верификацию Этапа 3, 2026-07-03).
        var engine = CreateEngine(out var graph, out var actions);
        actions.SetEffect(Arrest, -1f);

        var npc = engine.RegisterNpc(1);
        npc.Appraisal.SetAttitude(1, 1f);
        graph.AddNode(new Node(1));
        graph.AddNode(new Node(2));

        engine.Perceive(1, new GameEvent(agentId: 2, action: Arrest, patientId: 1, dc: 1f), time: 10f);
        float distressAtTen = npc.Emotions[EmotionKind.Distress];

        // Событие "из прошлого" (time=5 < 10) не должно сдвинуть LastUpdateTime назад.
        engine.Perceive(1, new GameEvent(agentId: 2, action: Arrest, patientId: 1, dc: 1f), time: 5f);

        // Следующий вызов сразу после исходного time=10 (dt=0.1) должен затухнуть лишь чуть-чуть,
        // а не так, как если бы LastUpdateTime откатился на 5 (что дало бы dt=5.1).
        engine.Perceive(1, new GameEvent(agentId: 2, action: Arrest, patientId: 1, dc: 0f), time: 10.1f);

        float expectedMinimalDecay = distressAtTen * MathF.Exp(-0.1f * 0.1f);
        Assert.True(npc.Emotions[EmotionKind.Distress] > expectedMinimalDecay - 0.05f);
    }

    [Fact]
    public void Perceive_SelfInflictedEvent_DoesNotThrowAndSkipsSelfRelation()
    {
        var engine = CreateEngine(out var graph, out var actions);
        actions.SetEffect(Offer, 1f);

        var pc = engine.RegisterNpc(1);
        pc.Appraisal.SetAttitude(1, 0.5f);
        graph.AddNode(new Node(1));

        engine.Perceive(1, new GameEvent(agentId: 1, action: Offer, patientId: 1, dc: 1f), time: 0f);

        Assert.True(pc.Emotions[EmotionKind.Joy] > 0f);
    }

    [Fact]
    public void Perceive_SecondEventLater_DecaysEmotionsBeforeMerging()
    {
        var engine = CreateEngine(out var graph, out var actions);
        actions.SetEffect(Arrest, -1f);
        actions.SetEffect(Offer, 0f);

        var npc = engine.RegisterNpc(1);
        npc.Appraisal.SetAttitude(1, 1f);
        npc.Appraisal.SetAttitude(2, 0f);
        graph.AddNode(new Node(1));
        graph.AddNode(new Node(2));

        engine.Perceive(1, new GameEvent(agentId: 2, action: Arrest, patientId: 1, dc: 1f), time: 0f);
        float distressRightAfter = npc.Emotions[EmotionKind.Distress];

        // Второе событие ничего не меняет для distress (attitude к patient=2 равен 0), значит
        // единственный источник изменения distress между вызовами — затухание.
        engine.Perceive(1, new GameEvent(agentId: 1, action: Offer, patientId: 2, dc: 1f), time: 5f);

        Assert.True(npc.Emotions[EmotionKind.Distress] < distressRightAfter);
    }

    [Fact]
    public void ObserveExpression_ExpresserFear_IncreasesObserverDominance()
    {
        var engine = CreateEngine(out var graph, out var actions);
        actions.SetEffect(Arrest, -1f);

        var observer = engine.RegisterNpc(1);
        var expresser = engine.RegisterNpc(2);
        var observerNode = new Node(1);
        var expresserNode = new Node(2);
        graph.AddNode(observerNode);
        graph.AddNode(expresserNode);
        graph.AddCommonEdge(observerNode, expresserNode, SocialRelation.Neutral, SocialRelation.Neutral);

        expresser.Appraisal.SetAttitude(2, 1f);
        engine.Perceive(2, new GameEvent(agentId: 1, action: Arrest, patientId: 2, dc: 1f), time: 0f);

        engine.ObserveExpression(1, 2, time: 0f);

        var relation = (SocialRelation)graph.GetEdge(observerNode, expresserNode)!.Relationship;
        Assert.True(relation.Dominance > 0f);
    }

    [Fact]
    public void ObserveExpression_MatchingDistress_IncreasesSolidarity()
    {
        var engine = CreateEngine(out var graph, out var actions);
        actions.SetEffect(Arrest, -1f);

        var alice = engine.RegisterNpc(1);
        var bob = engine.RegisterNpc(2);
        engine.RegisterNpc(3);
        var aliceNode = new Node(1);
        var bobNode = new Node(2);
        graph.AddNode(aliceNode);
        graph.AddNode(bobNode);
        graph.AddNode(new Node(3));
        graph.AddCommonEdge(aliceNode, bobNode, SocialRelation.Neutral, SocialRelation.Neutral);

        alice.Appraisal.SetAttitude(1, 1f);
        bob.Appraisal.SetAttitude(2, 1f);
        engine.Perceive(1, new GameEvent(agentId: 3, action: Arrest, patientId: 1, dc: 1f), time: 0f);
        engine.Perceive(2, new GameEvent(agentId: 3, action: Arrest, patientId: 2, dc: 1f), time: 0f);

        engine.ObserveExpression(1, 2, time: 0f);

        var relation = (SocialRelation)graph.GetEdge(aliceNode, bobNode)!.Relationship;
        Assert.True(relation.Solidarity > 0f);
        // Раздел IV-B статьи: совпадение выражаемых эмоций поднимает liking "побочным эффектом" от solidarity.
        Assert.True(relation.Liking > 0f);
    }

    [Fact]
    public void Perceive_WhenEdgeSeededAsHistoryEdge_PreservesFullRelationHistoryWithoutEngineChanges()
    {
        // SocialDynamicsEngine.UpdateRelation всегда пишет через throwaway `new Edge(...)` — она
        // не знает и не должна знать про HistoryEdge. Если стартовое ребро создано как HistoryEdge,
        // история пишется автоматически (см. HistoryEdgeTests.DeepGraph_AddEdge_OnExistingHistoryEdge_...).
        var engine = CreateEngine(out var graph, out var actions);
        actions.SetEffect(Arrest, -1f);
        actions.SetEffect(Offer, 1f);

        var burglar = engine.RegisterNpc(1);
        engine.RegisterNpc(2);
        var burglarNode = new Node(1);
        var policemanNode = new Node(2);
        graph.AddNode(burglarNode);
        graph.AddNode(policemanNode);

        var initial = SocialRelation.Neutral;
        graph.AddDirectEdge(new HistoryEdge(burglarNode, policemanNode, initial));

        burglar.Appraisal.SetAttitude(1, 1f);
        engine.Perceive(1, new GameEvent(agentId: 2, action: Arrest, patientId: 1, dc: 1f), time: 0f);
        engine.Perceive(1, new GameEvent(agentId: 2, action: Offer, patientId: 1, dc: 1f), time: 1f);

        var stored = graph.GetEdge(burglarNode, policemanNode);
        Assert.IsType<HistoryEdge>(stored);

        var historyEdge = (HistoryEdge)stored!;
        Assert.Equal(3, historyEdge.Count); // начальное + 2 апдейта от Perceive
        Assert.Same(initial, historyEdge.History[0]);
        Assert.Equal(historyEdge.Relationship, graph.GetEdge<SocialRelation>(burglarNode, policemanNode)!.Relationship);
    }

    private static void FloatAssertEqualPersonality(Personality expected, Personality actual)
    {
        Assert.Equal(expected.Extraversion, actual.Extraversion);
        Assert.Equal(expected.Neuroticism, actual.Neuroticism);
    }
}

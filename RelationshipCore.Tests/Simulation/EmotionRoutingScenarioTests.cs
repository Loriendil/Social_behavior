using RelationshipCore.Dynamics;
using RelationshipCore.Graphs;
using RelationshipCore.Nodes;
using RelationshipCore.Simulation;
using RelationshipCore.Tests.Dynamics;

namespace RelationshipCore.Tests.Simulation;

/// <summary>
/// Раунд правок 2, задача 1: эталонный сценарий из ручного теста 2026-07-07 (TASK_fixes_round2.md),
/// воспроизведённый как интеграционный тест. Три персонажа, четыре события, dc=1, t=0 везде.
/// Проверяет, что маршрутизация социального эффекта эмоции адресуется исключительно causeId =
/// evt.AgentId, и что самопричинённые эмоции (causeId == perceiverId) НЕ создают/не меняют ребро
/// отношения вовсе (раньше fallback ошибочно откатывал эффект на пациента — "обвинение жертвы").
/// Точные числа ниже вычислены исправленным движком; в самой задаче они даны с "≈" (авторская
/// прикидка вручную) — здесь важны знаки и то, что 1→2 не тронуто, а не совпадение до сотых.
/// </summary>
public class EmotionRoutingScenarioTests
{
    private static (SocialDynamicsEngine Engine, DeepGraph Graph) BuildScenario()
    {
        var graph = new DeepGraph();
        var actions = new ActionDictionary();
        var engine = new SocialDynamicsEngine(graph, actions);

        foreach (var id in new[] { 1, 2, 3 })
        {
            graph.AddNode(new Node(id));
        }

        engine.RegisterNpc(1, new Personality(0.60f, 0.54f));
        engine.RegisterNpc(2, new Personality(0.10f, -0.23f));
        engine.RegisterNpc(3, new Personality(0.32f, -0.66f));

        engine.GetState(1).Appraisal.SetAttitude(1, 0.90f);
        engine.GetState(1).Appraisal.SetAttitude(2, 0.20f);
        engine.GetState(2).Appraisal.SetAttitude(1, -0.10f);
        engine.GetState(2).Appraisal.SetAttitude(2, 0.22f);
        engine.GetState(3).Appraisal.SetAttitude(3, 0.67f);
        engine.GetState(3).Appraisal.SetAttitude(1, 0.80f);

        actions.SetEffect(new ActionId(1), 0.50f);
        actions.SetEffect(new ActionId(2), -0.56f);

        engine.GetState(1).Appraisal.SetPraise(new ActionId(1), 1.00f);
        engine.GetState(2).Appraisal.SetPraise(new ActionId(2), 0.50f);
        engine.GetState(3).Appraisal.SetPraise(new ActionId(0), 0.00f);

        // ⟨p=1,a=1,act=2,pat=2⟩: самопричинённое (agent==perceiver) вредное действие против NPC2.
        engine.Perceive(1, new GameEvent(agentId: 1, action: new ActionId(2), patientId: 2, dc: 1f), time: 0f);
        // ⟨p=2,a=1,act=1,pat=1⟩: NPC2 воспринимает "выгодное для NPC1" действие NPC1 против себя.
        engine.Perceive(2, new GameEvent(agentId: 1, action: new ActionId(1), patientId: 1, dc: 1f), time: 0f);
        // ⟨p=3,a=2,act=1,pat=1⟩: NPC3 наблюдает выгодное действие NPC2 против NPC1.
        engine.Perceive(3, new GameEvent(agentId: 2, action: new ActionId(1), patientId: 1, dc: 1f), time: 0f);
        // ⟨p=3,a=1,act=2,pat=2⟩: NPC3 наблюдает вредное действие NPC1 против NPC2 (не задевает NPC3 — attitude/praise к нему не заданы).
        engine.Perceive(3, new GameEvent(agentId: 1, action: new ActionId(2), patientId: 2, dc: 1f), time: 0f);

        return (engine, graph);
    }

    [Fact]
    public void Scenario_EmotionsMatchTaskExpectations()
    {
        var (engine, _) = BuildScenario();

        FloatAssert.Approximately(0.4826f, engine.GetState(1).Emotions[EmotionKind.Distress], 0.01f);
        FloatAssert.Approximately(0.2655f, engine.GetState(2).Emotions[EmotionKind.Distress], 0.01f);
        FloatAssert.Approximately(0.7540f, engine.GetState(3).Emotions[EmotionKind.Joy], 0.01f);

        // Задача 2: praise₃(action 1) не задан у владельца 3 (есть только action 0) — Admiration не должна возникать.
        Assert.Equal(0f, engine.GetState(3).Emotions[EmotionKind.Admiration]);
    }

    [Fact]
    public void Scenario_SelfCausedHarm_NeverCreatesRelationEdge()
    {
        // Главная проверка задачи 1: перceiver=1=agent причинил вред NPC2 и испытал distress —
        // раньше fallback ошибочно понижал liking/dominance 1->2 ("обвинение жертвы"). Теперь
        // самопричинённая эмоция социального эффекта не даёт вовсе — ребро 1->2 не создаётся.
        var (_, graph) = BuildScenario();

        var edge = graph.GetEdge<RelationshipCore.Dynamics.SocialRelation>(graph.GetNode(1)!, graph.GetNode(2)!);
        Assert.Null(edge);
    }

    [Fact]
    public void Scenario_RelationTowardCause_UnaffectedByRoutingFix()
    {
        var (_, graph) = BuildScenario();

        // 2->1: distress вызван причиной-агентом 1 — этот канал не менялся задачей 1.
        var rel21 = (RelationshipCore.Dynamics.SocialRelation)graph.GetEdge<RelationshipCore.Dynamics.SocialRelation>(graph.GetNode(2)!, graph.GetNode(1)!)!.Relationship;
        Assert.True(rel21.Liking < 0f);
        Assert.True(rel21.Dominance < 0f);
        Assert.Equal(0f, rel21.Solidarity); // клампится снизу на 0 (Solidarity ∈ [0,1])

        // 3->2: joy вызвана причиной-агентом 2 (не пациентом 1).
        var rel32 = (RelationshipCore.Dynamics.SocialRelation)graph.GetEdge<RelationshipCore.Dynamics.SocialRelation>(graph.GetNode(3)!, graph.GetNode(2)!)!.Relationship;
        Assert.True(rel32.Liking > 0f);
        Assert.Equal(0f, rel32.Dominance); // admiration исчезает после задачи 2 — dominance не сдвигается

        // 3->1: fortunes-of-others исключены — NPC3 не формирует отношение к пациенту события.
        var edge31 = graph.GetEdge<RelationshipCore.Dynamics.SocialRelation>(graph.GetNode(3)!, graph.GetNode(1)!);
        if (edge31 is not null)
        {
            var rel31 = (RelationshipCore.Dynamics.SocialRelation)edge31.Relationship;
            Assert.Equal(0f, rel31.Liking);
            Assert.Equal(0f, rel31.Dominance);
        }
    }

    [Fact]
    public void Scenario_FamiliarityStaysFrozenEverywhere()
    {
        var (_, graph) = BuildScenario();

        foreach (var from in new[] { 1, 2, 3 })
        {
            foreach (var to in new[] { 1, 2, 3 })
            {
                if (from == to) continue;
                var edge = graph.GetEdge<RelationshipCore.Dynamics.SocialRelation>(graph.GetNode(from)!, graph.GetNode(to)!);
                if (edge?.Relationship is RelationshipCore.Dynamics.SocialRelation rel)
                {
                    Assert.Equal(0f, rel.Familiarity);
                }
            }
        }
    }
}

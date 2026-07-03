using RelationshipCore.Dynamics;
using RelationshipCore.Graphs;
using RelationshipCore.Nodes;
using RelationshipCore.Simulation;

namespace RelationshipCore.Tests.Simulation;

/// <summary>
/// Интеграционные тесты на эталонных сценариях раздела IV-B статьи Ochs (Этап 4). Сверены не
/// только с пересказом в CLAUDE.md, но и напрямую с текстом статьи (стр. 291, 295) и рисунками
/// 9-11 (прочитаны как изображения через PyMuPDF, см. «Как читать PDF» в CLAUDE.md). Проверяем
/// качественную динамику (направления изменений), а не точные числа — точных чисел статья не даёт.
/// </summary>
public class OchsReferenceScenarioTests
{
    // ===================== Сценарий 1: допрос грабителя (стр. 291-292, рис. 9-10) =====================

    private const int Burglar = 1;
    private const int Policeman = 2;
    private const int Kidnapper = 3; // отсутствующее третье лицо — не участник разговора, не узел с NpcState

    private static readonly ActionId Arrest = new(1);
    private static readonly ActionId OfferCoffee = new(2);
    private static readonly ActionId RecallKidnapping = new(3);

    private static (SocialDynamicsEngine Engine, DeepGraph Graph, Node BurglarNode, Node PolicemanNode) CreateBurglarScenario(
        Personality burglarPersonality = default)
    {
        var graph = new DeepGraph();
        var actions = new ActionDictionary();
        actions.SetEffect(Arrest, -0.8f);
        actions.SetEffect(OfferCoffee, 0.4f);
        actions.SetEffect(RecallKidnapping, -0.9f);

        var engine = new SocialDynamicsEngine(graph, actions);
        var burglar = engine.RegisterNpc(Burglar, burglarPersonality);
        engine.RegisterNpc(Policeman);

        var burglarNode = new Node(Burglar);
        var policemanNode = new Node(Policeman);
        graph.AddNode(burglarNode);
        graph.AddNode(policemanNode);

        // Стартовое отношение (раздел IV-B, рис. 10): грабитель подчинён и недолюбливает полицейского.
        graph.AddCommonEdge(
            burglarNode, policemanNode,
            new SocialRelation(liking: -0.4f, dominance: -0.3f, familiarity: 0.1f, solidarity: 0f),
            SocialRelation.Neutral);

        burglar.Appraisal.SetAttitude(Burglar, 0.9f); // самоуважение — события, задевающие "меня", важны

        return (engine, graph, burglarNode, policemanNode);
    }

    private static SocialRelation BurglarRelationOf((SocialDynamicsEngine Engine, DeepGraph Graph, Node BurglarNode, Node PolicemanNode) s) =>
        (SocialRelation)s.Graph.GetEdge(s.BurglarNode, s.PolicemanNode)!.Relationship;

    [Fact]
    public void BurglarInterrogation_Arrest_DecreasesLikingAndDominance()
    {
        var s = CreateBurglarScenario();
        var before = BurglarRelationOf(s);

        // Утверждение 1: арест → страх (effect отрицательный, attitude к себе положительный).
        s.Engine.Perceive(Burglar, new GameEvent(Policeman, Arrest, Burglar, dc: 0.8f), time: 0f);

        var after = BurglarRelationOf(s);
        Assert.True(after.Liking < before.Liking);
        Assert.True(after.Dominance < before.Dominance);
    }

    [Fact]
    public void BurglarInterrogation_Coffee_IncreasesLiking()
    {
        var s = CreateBurglarScenario();
        s.Engine.Perceive(Burglar, new GameEvent(Policeman, Arrest, Burglar, dc: 0.8f), time: 0f);
        var afterArrest = BurglarRelationOf(s);

        // Утверждения 3-4: предложение кофе → надежда (desirable, но не полная уверенность) → liking растёт.
        s.Engine.Perceive(Burglar, new GameEvent(Policeman, OfferCoffee, Burglar, dc: 0.6f), time: 1f);

        var afterCoffee = BurglarRelationOf(s);
        Assert.True(afterCoffee.Liking > afterArrest.Liking);
    }

    [Fact]
    public void BurglarInterrogation_KidnappingRecallByThirdParty_TriggersDistressWithoutAffectingLikingTowardPoliceman()
    {
        var s = CreateBurglarScenario();
        s.Engine.RegisterNpc(Kidnapper);
        s.Graph.AddNode(new Node(Kidnapper));

        s.Engine.Perceive(Burglar, new GameEvent(Policeman, Arrest, Burglar, dc: 0.8f), time: 0f);
        var before = BurglarRelationOf(s);

        // Утверждение 5 (стр. 291): "since the policeman is not responsible for this negative
        // emotion (he is not the kidnapper), the event has no impact on the degree of liking."
        // Агент события — похититель, а не полицейский, поэтому FromOwnEmotion не трогает ребро
        // burglar->policeman вовсе (не нулевой, а именно ПРОПУЩЕННЫЙ, self/other-независимый апдейт).
        s.Engine.Perceive(Burglar, new GameEvent(Kidnapper, RecallKidnapping, Burglar, dc: 1f), time: 2f);

        var burglarState = s.Engine.GetState(Burglar);
        Assert.True(burglarState.Emotions[EmotionKind.Distress] > 0.5f);

        var after = BurglarRelationOf(s);
        Assert.Equal(before.Liking, after.Liking);
        Assert.Equal(before.Dominance, after.Dominance);
    }

    [Fact]
    public void BurglarInterrogation_ObservingPolicemansCongruentDistress_IncreasesSolidarityLikingAndDominance()
    {
        var s = CreateBurglarScenario();
        s.Engine.RegisterNpc(Kidnapper);
        s.Graph.AddNode(new Node(Kidnapper));

        s.Engine.Perceive(Burglar, new GameEvent(Policeman, Arrest, Burglar, dc: 0.8f), time: 0f);
        s.Engine.Perceive(Burglar, new GameEvent(Kidnapper, RecallKidnapping, Burglar, dc: 1f), time: 2f);

        // Полицейский тоже сочувственно воспринимает эту историю ("expression of distress").
        var policeman = s.Engine.GetState(Policeman);
        policeman.Appraisal.SetAttitude(Burglar, 0.3f);
        s.Engine.Perceive(Policeman, new GameEvent(Kidnapper, RecallKidnapping, Burglar, dc: 1f), time: 2f);

        var before = BurglarRelationOf(s);

        // Утверждение (стр. 291): совпадение выражаемого distress → solidarity растёт "и, побочным
        // эффектом, liking"; наблюдение чужого distress отдельно поднимает dominance (рис. 5, "emotions
        // expressed by j" — этот источник не связан с вопросом атрибуции вины, поэтому однозначен).
        s.Engine.ObserveExpression(Burglar, Policeman, time: 2f);

        var after = BurglarRelationOf(s);
        Assert.True(after.Solidarity > before.Solidarity);
        Assert.True(after.Liking > before.Liking);
        Assert.True(after.Dominance > before.Dominance);
    }

    [Fact]
    public void BurglarInterrogation_NeuroticPersonality_AmplifiesNegativeEmotionMoreThanNeutral()
    {
        var neutral = CreateBurglarScenario(Personality.Neutral);
        var neurotic = CreateBurglarScenario(new Personality(extraversion: 0f, neuroticism: 0.8f));

        var evt = new GameEvent(Policeman, Arrest, Burglar, dc: 0.8f);
        neutral.Engine.Perceive(Burglar, evt, time: 0f);
        neurotic.Engine.Perceive(Burglar, evt, time: 0f);

        float fearNeutral = neutral.Engine.GetState(Burglar).Emotions[EmotionKind.Fear];
        float fearNeurotic = neurotic.Engine.GetState(Burglar).Emotions[EmotionKind.Fear];

        Assert.True(fearNeurotic > fearNeutral);
    }

    // ===================== Сценарий 2: собеседование (стр. 292-295, рис. 11) =====================

    private const int Director = 1;
    private const int PC = 2;
    private const int GoodSellerConcept = 100;
    private const int MoneyConcept = 101;
    private const int LifeCircumstances = 999; // безличный "агент" фоновых событий (финансовый кризис и т.п.)

    private static readonly ActionId ClaimExperience = new(10);
    private static readonly ActionId ExpressHardship = new(11);
    private static readonly ActionId ExpressFearOfJoblessness = new(12);
    private static readonly ActionId OfferUnpaidMonth = new(13);

    private static (SocialDynamicsEngine Engine, DeepGraph Graph, Node DirectorNode, Node PcNode) CreateJobInterviewScenario()
    {
        var graph = new DeepGraph();
        var actions = new ActionDictionary();
        actions.SetEffect(ClaimExperience, 0.7f);
        actions.SetEffect(ExpressHardship, -0.6f);
        actions.SetEffect(ExpressFearOfJoblessness, -0.6f);
        actions.SetEffect(OfferUnpaidMonth, 0.8f);

        var engine = new SocialDynamicsEngine(graph, actions);
        var director = engine.RegisterNpc(Director);
        engine.RegisterNpc(PC);

        var directorNode = new Node(Director);
        var pcNode = new Node(PC);
        graph.AddNode(directorNode);
        graph.AddNode(pcNode);
        graph.AddNode(new Node(LifeCircumstances));

        // Раздел IV-B (стр. 292): "the initial value of the director's dominance is low... the
        // initial value of solidarity is null."
        graph.AddCommonEdge(
            directorNode, pcNode,
            new SocialRelation(liking: 0f, dominance: -0.3f, familiarity: 0f, solidarity: 0f),
            SocialRelation.Neutral);

        director.Appraisal.SetAttitude(GoodSellerConcept, 0.8f);
        director.Appraisal.SetAttitude(MoneyConcept, 0.6f);

        return (engine, graph, directorNode, pcNode);
    }

    private static SocialRelation DirectorRelationOf((SocialDynamicsEngine Engine, DeepGraph Graph, Node DirectorNode, Node PcNode) s) =>
        (SocialRelation)s.Graph.GetEdge(s.DirectorNode, s.PcNode)!.Relationship;

    [Fact]
    public void JobInterview_PcClaimsExperience_TriggersHopeAndIncreasesLiking()
    {
        var s = CreateJobInterviewScenario();
        var before = DirectorRelationOf(s);

        // Утверждение 2 (стр. 295): "triggers an emotion of hope for the director... this emotion,
        // caused by the PC, induces an increase of the degree of liking" — dc<1: обещание, не факт.
        s.Engine.Perceive(Director, new GameEvent(PC, ClaimExperience, GoodSellerConcept, dc: 0.6f), time: 0f);

        Assert.True(s.Engine.GetState(Director).Emotions[EmotionKind.Hope] > 0f);
        var after = DirectorRelationOf(s);
        Assert.True(after.Liking > before.Liking);
    }

    [Fact]
    public void JobInterview_MatchingSadnessExpressions_IncreasesDirectorSolidarity()
    {
        var s = CreateJobInterviewScenario();
        var director = s.Engine.GetState(Director);
        var pc = s.Engine.GetState(PC);

        director.Appraisal.SetAttitude(Director, 0.9f);
        pc.Appraisal.SetAttitude(PC, 0.9f);

        // Утверждения 3-4: и директор, и PC выражают огорчение из-за финансовых трудностей.
        s.Engine.Perceive(Director, new GameEvent(agentId: LifeCircumstances, ExpressHardship, patientId: Director, dc: 1f), time: 1f);
        s.Engine.Perceive(PC, new GameEvent(agentId: LifeCircumstances, ExpressHardship, patientId: PC, dc: 1f), time: 1f);

        var before = DirectorRelationOf(s);
        s.Engine.ObserveExpression(Director, PC, time: 1f);
        var after = DirectorRelationOf(s);

        Assert.True(after.Solidarity > before.Solidarity);
    }

    [Fact]
    public void JobInterview_PcExpressesFear_IncreasesDirectorDominance()
    {
        var s = CreateJobInterviewScenario();
        var pc = s.Engine.GetState(PC);
        pc.Appraisal.SetAttitude(PC, 0.9f);

        // Утверждение 5: PC боится не получить работу — выражение fear (рис. 5, "emotions expressed by j").
        s.Engine.Perceive(PC, new GameEvent(agentId: LifeCircumstances, ExpressFearOfJoblessness, patientId: PC, dc: 0.6f), time: 2f);

        var before = DirectorRelationOf(s);
        s.Engine.ObserveExpression(Director, PC, time: 2f);
        var after = DirectorRelationOf(s);

        Assert.True(after.Dominance > before.Dominance);
    }

    [Fact]
    public void JobInterview_MoneyOffer_TriggersJoyAndIncreasesLikingFurther()
    {
        var s = CreateJobInterviewScenario();
        s.Engine.Perceive(Director, new GameEvent(PC, ClaimExperience, GoodSellerConcept, dc: 0.6f), time: 0f);
        var beforeOffer = DirectorRelationOf(s);

        // Утверждение 6 (стр. 295): "triggers an emotion of joy for the director since the director
        // has a positive attitude toward the concept of money, and the effect of the action offer is
        // also positive" — dc=1 (PC явно соглашается).
        s.Engine.Perceive(Director, new GameEvent(PC, OfferUnpaidMonth, MoneyConcept, dc: 1f), time: 3f);

        Assert.True(s.Engine.GetState(Director).Emotions[EmotionKind.Joy] > 0f);
        var afterOffer = DirectorRelationOf(s);
        Assert.True(afterOffer.Liking > beforeOffer.Liking);
    }
}

using RelationshipCore.Dynamics;
using RelationshipCore.Dynamics.Rules;
using RelationshipCore.Edges;
using RelationshipCore.Graphs;

namespace RelationshipCore.Simulation;

/// <summary>
/// Оркестратор (см. раздел «Архитектура объединения слоёв»): единственное место, где встречаются
/// слой графа O'Connor (DeepGraph) и слой чистой математики Ochs (Dynamics/Dynamics.Rules).
/// Хранит реестр <see cref="NpcState"/> по EntityId, обрабатывает восприятие событий и наблюдение
/// чужих эмоций, записывает результат как SocialRelation на прямом рёбре графа.
/// </summary>
public sealed class SocialDynamicsEngine
{
    private readonly Dictionary<int, NpcState> _npcs = new();

    public SocialDynamicsEngine(DeepGraph graph, ActionDictionary actions)
    {
        Graph = graph;
        Actions = actions;
    }

    public DeepGraph Graph { get; }

    public ActionDictionary Actions { get; }

    /// <summary>Регистрирует NPC в реестре движка. Повторная регистрация того же EntityId возвращает уже существующее состояние (как Graph.AddNode).</summary>
    public NpcState RegisterNpc(int entityId, Personality personality = default)
    {
        if (_npcs.TryGetValue(entityId, out var existing))
        {
            return existing;
        }

        var state = new NpcState(entityId, personality);
        _npcs[entityId] = state;
        return state;
    }

    public NpcState GetState(int entityId) =>
        _npcs.TryGetValue(entityId, out var state)
            ? state
            : throw new InvalidOperationException($"NPC {entityId} не зарегистрирован в SocialDynamicsEngine.");

    /// <summary>
    /// Восприятие события perceiverId (раздел IV статьи Ochs): стимул (рис. 2) → модуляция
    /// личностью → слияние с затухшими старыми эмоциями (e(t)=max(триггер, затухшее)) →
    /// обновление отношения perceiver к "второй стороне" события собственной эмоцией (рис. 4-5,
    /// FromOwnEmotion). Если perceiver сам — вторая сторона (perceiver == agent == patient),
    /// отношение не обновляется — обновлять нечего.
    /// </summary>
    public void Perceive(int perceiverId, GameEvent evt, float time)
    {
        var perceiver = GetState(perceiverId);
        DecayToTime(perceiver, time);

        var stimulus = EmotionRules.ComputeStimulus(evt, Actions, perceiver.Appraisal, perceiverId);
        var triggered = PersonalityRules.Modulate(perceiver.Personality, stimulus);

        perceiver.Emotions = DecayRules.Merge(perceiver.Emotions, triggered);

        int counterparty = perceiverId == evt.AgentId ? evt.PatientId : evt.AgentId;
        UpdateRelation(perceiverId, counterparty, SocialRelationRules.FromOwnEmotion(triggered));
    }

    /// <summary>
    /// Наблюдение observerId за текущим выраженным эмоциональным состоянием expresserId (рис. 5
    /// "emotions expressed by j" + рис. 6 совпадение/несовпадение выражаемых эмоций). Не создаёт
    /// новых эмоций у observer — только считывает уже накопленное (с ленивым затуханием)
    /// состояние обеих сторон и обновляет отношение observer -&gt; expresser.
    /// </summary>
    public void ObserveExpression(int observerId, int expresserId, float time)
    {
        var observer = GetState(observerId);
        var expresser = GetState(expresserId);

        DecayToTime(observer, time);
        DecayToTime(expresser, time);

        var delta = SocialRelationRules.FromObservedExpression(expresser.Emotions)
            + SocialRelationRules.FromEmotionalCoincidence(observer.Emotions, expresser.Emotions);

        UpdateRelation(observerId, expresserId, delta);
    }

    private static void DecayToTime(NpcState state, float time)
    {
        // LastUpdateTime продвигается только вперёд: если вызывающий передаст time из
        // прошлого (например, события пришли не по порядку), dt <= 0 и мы просто ничего не
        // делаем, вместо того чтобы отмотать часы назад — иначе следующий вызов посчитал бы
        // задержку от заниженного LastUpdateTime и затух бы эмоции сильнее, чем реально прошло.
        float dt = time - state.LastUpdateTime;
        if (dt > 0f)
        {
            state.Emotions = DecayRules.Decay(state.Emotions, dt);
            state.LastUpdateTime = time;
        }
    }

    private void UpdateRelation(int fromId, int toId, SocialRelationDelta delta)
    {
        if (fromId == toId)
        {
            return;
        }

        var from = Graph.GetNode(fromId) ?? throw new InvalidOperationException($"Узел {fromId} отсутствует в графе.");
        var to = Graph.GetNode(toId) ?? throw new InvalidOperationException($"Узел {toId} отсутствует в графе.");

        // Типизированный поиск — на паре узлов может параллельно жить и другое Relationship
        // (например, структурное MEMBER слоя O'Connor, Этап 5), untyped GetEdge мог бы вернуть его.
        var current = Graph.GetEdge<SocialRelation>(from, to)?.Relationship as SocialRelation ?? SocialRelation.Neutral;
        var updated = SocialRelationRules.Apply(current, delta);

        Graph.AddDirectEdge(new Edge(from, to, updated));
    }
}

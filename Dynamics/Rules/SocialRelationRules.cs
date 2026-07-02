using RelationshipCore.Dynamics;

namespace RelationshipCore.Dynamics.Rules;

/// <summary>
/// Обновление SocialRelation от эмоций (рис. 4-6 статьи Ochs). Три независимых источника
/// изменений применяются к ребру владельца отношения (наблюдателя i, ребро i-&gt;j) отдельными
/// методами, чтобы движок (Этап 3) мог комбинировать их по ситуации:
///
/// 1. UpdateFromOwnEmotion — эмоция, которую наблюдатель САМ испытал из-за действия j
///    (напр. страх грабителя при аресте), обновляет его же ребро к j: liking по знаку
///    испытанной эмоции; dominance — pride/anger поднимают собственную доминантность,
///    fear/distress/admiration/shame снижают её (см. сценарий "грабитель" в CLAUDE.md).
/// 2. UpdateFromObservedExpression — эмоция, которую наблюдатель ВИДИТ у j (выражение страха
///    собеседником и т.п.), комплементарно обновляет его dominance к j (если j выглядит менее
///    доминантным/подчинённым, наблюдатель ощущает себя более доминантным) и solidarity
///    (негативная эмоция у j снижает солидарность наблюдателя к j).
/// 3. UpdateSolidarityFromCoincidence — совпадение эмоций, которые выражают обе стороны,
///    поднимает solidarity (сближает после общего переживания).
///
/// Familiarity явно не запускается отдельным правилом — статья описывает её рост как
/// косвенный, через liking (см. UpdateFamiliarityFromLikingShift).
/// </summary>
public static class SocialRelationRules
{
    public const float DefaultGain = 0.3f;

    public static SocialRelation UpdateFromOwnEmotion(
        SocialRelation current, EmotionVector ownEmotion, float gain = DefaultGain)
    {
        float likingDelta = EmotionValence.NetValence(ownEmotion) * gain;
        float dominanceDelta = EmotionValence.NetDominanceShift(ownEmotion) * gain;

        return new SocialRelation(
            liking: ApplyBounded(current.Liking, likingDelta),
            dominance: ApplyBounded(current.Dominance, dominanceDelta),
            familiarity: current.Familiarity,
            solidarity: current.Solidarity);
    }

    public static SocialRelation UpdateFromObservedExpression(
        SocialRelation current, EmotionVector expresserEmotion, float gain = DefaultGain)
    {
        // Комплементарно UpdateFromOwnEmotion: то, что снижает доминантность выражающего,
        // повышает доминантность наблюдателя (см. "выражение страха собеседником -> своя dominance растёт").
        float dominanceDelta = -EmotionValence.NetDominanceShift(expresserEmotion) * gain;
        float solidarityDelta = -EmotionValence.Sum(expresserEmotion, EmotionValence.Negative) * gain;

        return new SocialRelation(
            liking: current.Liking,
            dominance: ApplyBounded(current.Dominance, dominanceDelta),
            familiarity: current.Familiarity,
            solidarity: ApplyBounded(current.Solidarity, solidarityDelta));
    }

    public static SocialRelation UpdateSolidarityFromCoincidence(
        SocialRelation current, EmotionVector ownEmotion, EmotionVector otherEmotion, float gain = DefaultGain)
    {
        float coincidence = 0f;
        foreach (var kind in EmotionValence.All)
        {
            coincidence += MathF.Min(ownEmotion[kind], otherEmotion[kind]);
        }

        return new SocialRelation(
            liking: current.Liking,
            dominance: current.Dominance,
            familiarity: current.Familiarity,
            solidarity: ApplyBounded(current.Solidarity, coincidence * gain));
    }

    /// <summary>Familiarity растёт косвенно — вместе с любым изменением liking, вне зависимости от знака.</summary>
    public static SocialRelation UpdateFamiliarityFromLikingShift(
        SocialRelation before, SocialRelation after, float gain = DefaultGain)
    {
        float familiarityDelta = MathF.Abs(after.Liking - before.Liking) * gain;

        return new SocialRelation(
            liking: after.Liking,
            dominance: after.Dominance,
            familiarity: ApplyBounded(after.Familiarity, familiarityDelta),
            solidarity: after.Solidarity);
    }

    /// <summary>
    /// g_sr: пологая у краёв ±1 функция обновления — на экстремумах отношение меняется
    /// медленнее (раздел IV статьи Ochs). Синусоидальное затухание множителя к delta.
    /// </summary>
    internal static float ApplyBounded(float current, float delta)
    {
        float dampening = MathF.Cos(current * MathF.PI / 2f);
        return Math.Clamp(current + (delta * dampening), -1f, 1f);
    }
}

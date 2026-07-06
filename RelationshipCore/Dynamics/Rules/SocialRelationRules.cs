using RelationshipCore.Dynamics;

namespace RelationshipCore.Dynamics.Rules;

/// <summary>
/// Обновление SocialRelation от эмоций (раздел IV-D статьи Ochs, рис. 4-6). Следует структуре
/// статьи: φ_sr(relation, e_d_i(e), e_exp_j(t)) = g_sr(relation, f_sr(e_d_i(e), e_exp_j(t))) —
/// сначала несколько источников изменения складываются в один вектор дельты (f_sr, методы
/// From*), затем дельта применяется к текущему отношению ОДИН раз (g_sr, метод Apply). Так
/// пологое затухание у краёв диапазона (см. ApplyBoundedSigned/Unit) срабатывает один раз за
/// взаимодействие, а не по разу на каждый источник эмоции.
///
/// Источники (рис. 4-6):
/// 1. FromOwnEmotion — эмоция, которую сам испытал из-за действия j (напр. страх грабителя при
///    аресте): рис. 4 → liking, рис. 5 верхняя часть → dominance, рис. 6 нижняя часть → solidarity.
/// 2. FromObservedExpression — эмоция, которую ВЫРАЖАЕТ j (страх/дистресс собеседника): рис. 5
///    нижняя часть → dominance. Только эти две эмоции, никакой иной связи в статье нет.
/// 3. FromEmotionalCoincidence — совпадение/несовпадение выражаемых joy/hope/distress/fear между
///    сторонами: рис. 6 верхняя часть → solidarity, и (раздел IV-B, текст иллюстративного примера
///    допроса грабителя, сверено также по рис. 10) тем же приращением — побочный эффект на liking.
/// </summary>
public static class SocialRelationRules
{
    public const float DefaultGain = 0.3f;

    public static SocialRelationDelta FromOwnEmotion(EmotionVector ownEmotion) => new(
        liking: EmotionValence.Sum(ownEmotion, EmotionValence.LikingPositive)
            - EmotionValence.Sum(ownEmotion, EmotionValence.LikingNegative),
        dominance: EmotionValence.Sum(ownEmotion, EmotionValence.DominancePositive)
            - EmotionValence.Sum(ownEmotion, EmotionValence.DominanceNegative),
        solidarity: -EmotionValence.Sum(ownEmotion, EmotionValence.SolidarityNegative));

    public static SocialRelationDelta FromObservedExpression(EmotionVector expresserEmotion) => new(
        dominance: EmotionValence.Sum(expresserEmotion, EmotionValence.DominanceExpressedPositive));

    /// <summary>
    /// Раздел IV-B статьи (иллюстративный сценарий допроса грабителя, стр. 291) прямым текстом:
    /// "the congruence of the triggered emotion of the burglar and the emotion expressed by the
    /// policeman... induce an increase of the solidarity AND, BY SIDE EFFECT, of the degree of
    /// liking" — подтверждено также визуально по рис. 10 (значения liking и solidarity растут
    /// на одном и том же шаге сценария). Раздел IV-D-4 (формулы) отдельной формулы для этого
    /// побочного эффекта не даёт, поэтому используем то же приращение, что и для solidarity.
    /// </summary>
    public static SocialRelationDelta FromEmotionalCoincidence(EmotionVector ownEmotion, EmotionVector otherEmotion)
    {
        float solidarityDelta = 0f;

        foreach (var kind in EmotionValence.CoincidenceKinds)
        {
            solidarityDelta += MathF.Min(ownEmotion[kind], otherEmotion[kind]);
        }

        foreach (var (own, other) in EmotionValence.IncongruentPairs)
        {
            solidarityDelta -= MathF.Min(ownEmotion[own], otherEmotion[other]);
        }

        return new SocialRelationDelta(liking: solidarityDelta, solidarity: solidarityDelta);
    }

    /// <summary>
    /// g_sr: применяет накопленную (сложенную из нескольких источников) дельту к текущему отношению.
    /// Familiarity сюда сознательно не входит (см. «Осознанные трактовки статьи Ochs» в CLAUDE.md,
    /// раздел IV-D статьи vs. III-D): эмоции на неё не влияют вообще, ни прямо, ни косвенно через
    /// liking — единственный канал изменения familiarity в этой модели — явная передача информации,
    /// см. <see cref="FamiliarityFromInformationTransfer"/>.
    /// </summary>
    public static SocialRelation Apply(SocialRelation current, SocialRelationDelta delta, float gain = DefaultGain) => new(
        liking: ApplyBoundedSigned(current.Liking, delta.Liking * gain),
        dominance: ApplyBoundedSigned(current.Dominance, delta.Dominance * gain),
        familiarity: current.Familiarity,
        solidarity: ApplyBoundedUnit(current.Solidarity, delta.Solidarity * gain));

    /// <summary>
    /// Явная точка расширения (пока не подключена к SocialDynamicsEngine): рост familiarity от
    /// передачи информации (раздел III-D статьи — единственный признанный статьёй канал изменения
    /// familiarity; сама статья говорит, что этот механизм "не представлен в этой работе", поэтому
    /// конкретная формула — расширение, не цитата). confidentiality ∈ [0,1] — насколько значимой
    /// была раскрытая информация; рост монотонный и пологий у краёв, как и остальные g_sr-функции.
    /// </summary>
    public static SocialRelation FamiliarityFromInformationTransfer(
        SocialRelation current, float confidentiality, float gain = DefaultGain)
    {
        float clampedConfidentiality = Math.Clamp(confidentiality, 0f, 1f);
        float updatedFamiliarity = ApplyBoundedUnit(current.Familiarity, clampedConfidentiality * gain);

        return new SocialRelation(
            liking: current.Liking,
            dominance: current.Dominance,
            familiarity: updatedFamiliarity,
            solidarity: current.Solidarity);
    }

    /// <summary>
    /// Нижний порог демпфера: без него ApplyBoundedUnit в точке current=0 (типичное стартовое
    /// значение familiarity/solidarity) даёт множитель ровно 0 — отношение никогда не сдвинулось
    /// бы с нейтральной точки. Порог сохраняет качественное свойство "труднее меняться у краёв",
    /// не допуская полной блокировки.
    /// </summary>
    private const float MinDampening = 0.15f;

    /// <summary>
    /// g_sr для liking/dominance ∈[-1,1]: пологая у краёв ±1 (статья прямо предлагает синусоиду
    /// как пример реализации g_sr, не давая точной формулы) — центр диапазона 0.
    /// </summary>
    internal static float ApplyBoundedSigned(float current, float delta)
    {
        float dampening = MathF.Max(MinDampening, MathF.Cos(current * MathF.PI / 2f));
        return Math.Clamp(current + (delta * dampening), -1f, 1f);
    }

    /// <summary>Тот же принцип для familiarity/solidarity ∈[0,1]: пологая у ОБОИХ краёв (0 и 1), центр 0.5.</summary>
    internal static float ApplyBoundedUnit(float current, float delta)
    {
        float dampening = MathF.Max(MinDampening, MathF.Sin(current * MathF.PI));
        return Math.Clamp(current + (delta * dampening), 0f, 1f);
    }
}

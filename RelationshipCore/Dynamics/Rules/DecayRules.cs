using RelationshipCore.Dynamics;

namespace RelationshipCore.Dynamics.Rules;

/// <summary>Экспоненциальное затухание эмоций во времени (раздел IV-A статьи Ochs: e(t) = e(t-1) * exp(-decayRate * dt)).</summary>
public static class DecayRules
{
    /// <summary>В статье decayRate = 0.1 для всех десяти эмоций.</summary>
    public const float DefaultDecayRate = 0.1f;

    private static readonly EmotionKind[] AllKinds = (EmotionKind[])Enum.GetValues(typeof(EmotionKind));

    public static EmotionVector Decay(EmotionVector emotions, float deltaTime, float decayRate = DefaultDecayRate)
    {
        float factor = MathF.Exp(-decayRate * deltaTime);
        var result = emotions;

        foreach (var kind in AllKinds)
        {
            result = result.With(kind, emotions[kind] * factor);
        }

        return result;
    }

    /// <summary>
    /// При новом событии e(t) = max(триггер, затухшее старое значение) — новый стимул
    /// "перебивает" остаточную эмоцию только если он сильнее.
    /// </summary>
    public static EmotionVector Merge(EmotionVector decayedOld, EmotionVector trigger)
    {
        var result = decayedOld;

        foreach (var kind in AllKinds)
        {
            result = result.With(kind, MathF.Max(decayedOld[kind], trigger[kind]));
        }

        return result;
    }
}

using RelationshipCore.Dynamics;

namespace RelationshipCore.Dynamics.Rules;

/// <summary>
/// Правила триггера эмоций (рис. 2 статьи Ochs) и интенсивности стимула (раздел IV-A).
/// Чистые функции: вход — событие + объективный/субъективный словарь, выход — вектор стимула
/// (ещё не модулированный личностью — см. PersonalityRules).
/// </summary>
public static class EmotionRules
{
    /// <summary>
    /// Вычисляет вектор эмоционального стимула, вызванного событием evt у сущности perceiverId.
    /// </summary>
    public static EmotionVector ComputeStimulus(
        GameEvent evt, ActionDictionary actions, Appraisal perceiverAppraisal, int perceiverId)
    {
        var result = EmotionVector.Zero;

        float effect = actions.GetEffect(evt.Action);
        float attitude = perceiverAppraisal.GetAttitude(evt.PatientId);
        float desirability = MathF.Sign(effect) * MathF.Sign(attitude);

        if (desirability != 0f)
        {
            float intensity = Average(MathF.Abs(attitude), MathF.Abs(effect));
            result = ApplyDesirabilityTrigger(result, desirability, evt.Dc, intensity);
        }

        float praise = perceiverAppraisal.GetPraise(evt.Action);
        if (praise != 0f)
        {
            bool perceiverIsAgent = perceiverId == evt.AgentId;
            result = ApplyPraiseTrigger(result, praise, perceiverIsAgent);
        }

        return result;
    }

    /// <summary>
    /// dc=1 (очевидец) → joy/distress; dc∈(0,1) (ожидание) → hope/fear, масштабировано dc;
    /// dc=0 (ожидаемое не случилось) → relief (не случилось плохое) / disappointment (не случилось хорошее).
    /// </summary>
    private static EmotionVector ApplyDesirabilityTrigger(
        EmotionVector vector, float desirability, float dc, float intensity)
    {
        bool desirable = desirability > 0f;

        if (dc >= 1f)
        {
            return vector.With(desirable ? EmotionKind.Joy : EmotionKind.Distress, intensity);
        }

        if (dc > 0f)
        {
            return vector.With(desirable ? EmotionKind.Hope : EmotionKind.Fear, intensity * dc);
        }

        return vector.With(desirable ? EmotionKind.Disappointment : EmotionKind.Relief, intensity);
    }

    /// <summary>
    /// praise + роль воспринимающего в событии: сам совершил похвальное/постыдное действие →
    /// pride/shame; чужое похвальное/постыдное действие → admiration/anger.
    /// </summary>
    private static EmotionVector ApplyPraiseTrigger(EmotionVector vector, float praise, bool perceiverIsAgent)
    {
        bool praiseworthy = praise > 0f;
        float intensity = MathF.Abs(praise);

        EmotionKind kind = (perceiverIsAgent, praiseworthy) switch
        {
            (true, true) => EmotionKind.Pride,
            (true, false) => EmotionKind.Shame,
            (false, true) => EmotionKind.Admiration,
            (false, false) => EmotionKind.Anger,
        };

        return vector.With(kind, intensity);
    }

    private static float Average(float a, float b) => (a + b) / 2f;
}

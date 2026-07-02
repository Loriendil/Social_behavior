using RelationshipCore.Dynamics;
using RelationshipCore.Dynamics.Rules;

namespace RelationshipCore.Tests.Dynamics;

public class DecayRulesTests
{
    [Fact]
    public void Decay_AppliesExponentialFormula()
    {
        var emotions = EmotionVector.Single(EmotionKind.Joy, 1f);

        var decayed = DecayRules.Decay(emotions, deltaTime: 1f);

        FloatAssert.Approximately(MathF.Exp(-0.1f), decayed[EmotionKind.Joy]);
    }

    [Fact]
    public void Decay_AtZeroTime_LeavesEmotionsUnchanged()
    {
        var emotions = EmotionVector.Single(EmotionKind.Joy, 0.5f);

        var decayed = DecayRules.Decay(emotions, deltaTime: 0f);

        FloatAssert.Approximately(0.5f, decayed[EmotionKind.Joy]);
    }

    [Fact]
    public void Merge_TakesComponentwiseMaximum()
    {
        var decayedOld = EmotionVector.Single(EmotionKind.Joy, 0.2f).With(EmotionKind.Fear, 0.6f);
        var trigger = EmotionVector.Single(EmotionKind.Joy, 0.5f).With(EmotionKind.Fear, 0.1f);

        var merged = DecayRules.Merge(decayedOld, trigger);

        FloatAssert.Approximately(0.5f, merged[EmotionKind.Joy]);
        FloatAssert.Approximately(0.6f, merged[EmotionKind.Fear]);
    }
}

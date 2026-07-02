using RelationshipCore.Dynamics;
using RelationshipCore.Dynamics.Rules;

namespace RelationshipCore.Tests.Dynamics;

public class PersonalityRulesTests
{
    [Fact]
    public void Modulate_NeutralPersonality_LeavesStimulusUnchanged()
    {
        var stimulus = EmotionVector.Single(EmotionKind.Joy, 0.4f);

        var modulated = PersonalityRules.Modulate(Personality.Neutral, stimulus);

        FloatAssert.Approximately(0.4f, modulated[EmotionKind.Joy]);
    }

    [Fact]
    public void Modulate_MaxExtraversion_AmplifiesJoyByHalf()
    {
        var stimulus = EmotionVector.Single(EmotionKind.Joy, 0.4f);
        var personality = new Personality(extraversion: 1f, neuroticism: 0f);

        var modulated = PersonalityRules.Modulate(personality, stimulus);

        FloatAssert.Approximately(0.6f, modulated[EmotionKind.Joy]);
    }

    [Fact]
    public void Modulate_MinExtraversion_DampensJoyByHalf()
    {
        var stimulus = EmotionVector.Single(EmotionKind.Joy, 0.4f);
        var personality = new Personality(extraversion: -1f, neuroticism: 0f);

        var modulated = PersonalityRules.Modulate(personality, stimulus);

        FloatAssert.Approximately(0.2f, modulated[EmotionKind.Joy]);
    }

    [Fact]
    public void Modulate_MaxNeuroticism_AmplifiesDistressByHalf()
    {
        var stimulus = EmotionVector.Single(EmotionKind.Distress, 0.4f);
        var personality = new Personality(extraversion: 0f, neuroticism: 1f);

        var modulated = PersonalityRules.Modulate(personality, stimulus);

        FloatAssert.Approximately(0.6f, modulated[EmotionKind.Distress]);
    }

    [Fact]
    public void Modulate_DoesNotAffectAdmirationOrAnger()
    {
        var stimulus = EmotionVector.Single(EmotionKind.Admiration, 0.5f).With(EmotionKind.Anger, 0.5f);
        var personality = new Personality(extraversion: 1f, neuroticism: 1f);

        var modulated = PersonalityRules.Modulate(personality, stimulus);

        FloatAssert.Approximately(0.5f, modulated[EmotionKind.Admiration]);
        FloatAssert.Approximately(0.5f, modulated[EmotionKind.Anger]);
    }

    [Fact]
    public void Modulate_ClampsAtOne()
    {
        var stimulus = EmotionVector.Single(EmotionKind.Joy, 0.9f);
        var personality = new Personality(extraversion: 1f, neuroticism: 0f);

        var modulated = PersonalityRules.Modulate(personality, stimulus);

        Assert.Equal(1f, modulated[EmotionKind.Joy]);
    }
}

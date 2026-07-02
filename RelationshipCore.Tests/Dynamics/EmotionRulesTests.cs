using RelationshipCore.Dynamics;
using RelationshipCore.Dynamics.Rules;

namespace RelationshipCore.Tests.Dynamics;

public class EmotionRulesTests
{
    private const int PerceiverId = 1;
    private const int AgentId = 2;
    private const int PatientId = 3;
    private static readonly ActionId Action = new(100);

    [Theory]
    [InlineData(0.6f, 0.4f, EmotionKind.Joy)]
    [InlineData(0.6f, -0.4f, EmotionKind.Distress)]
    public void ComputeStimulus_Eyewitness_TriggersJoyOrDistress(float effect, float attitude, EmotionKind expectedKind)
    {
        var (actions, appraisal) = BuildDictionaries(effect, attitude);
        var evt = new GameEvent(AgentId, Action, PatientId, dc: 1f);

        var stimulus = EmotionRules.ComputeStimulus(evt, actions, appraisal, PerceiverId);

        float expectedIntensity = (MathF.Abs(attitude) + MathF.Abs(effect)) / 2f;
        FloatAssert.Approximately(expectedIntensity, stimulus[expectedKind]);
    }

    [Theory]
    [InlineData(0.6f, 0.4f, EmotionKind.Hope)]
    [InlineData(0.6f, -0.4f, EmotionKind.Fear)]
    public void ComputeStimulus_ExpectedEvent_TriggersHopeOrFearScaledByDc(float effect, float attitude, EmotionKind expectedKind)
    {
        var (actions, appraisal) = BuildDictionaries(effect, attitude);
        var evt = new GameEvent(AgentId, Action, PatientId, dc: 0.5f);

        var stimulus = EmotionRules.ComputeStimulus(evt, actions, appraisal, PerceiverId);

        float expectedIntensity = (MathF.Abs(attitude) + MathF.Abs(effect)) / 2f * 0.5f;
        FloatAssert.Approximately(expectedIntensity, stimulus[expectedKind]);
    }

    [Theory]
    [InlineData(0.6f, 0.4f, EmotionKind.Disappointment)]
    [InlineData(0.6f, -0.4f, EmotionKind.Relief)]
    public void ComputeStimulus_ExpectedEventDidNotHappen_TriggersDisappointmentOrRelief(float effect, float attitude, EmotionKind expectedKind)
    {
        var (actions, appraisal) = BuildDictionaries(effect, attitude);
        var evt = new GameEvent(AgentId, Action, PatientId, dc: 0f);

        var stimulus = EmotionRules.ComputeStimulus(evt, actions, appraisal, PerceiverId);

        float expectedIntensity = (MathF.Abs(attitude) + MathF.Abs(effect)) / 2f;
        FloatAssert.Approximately(expectedIntensity, stimulus[expectedKind]);
    }

    [Theory]
    [InlineData(0f, 0.4f, EmotionKind.Joy)]
    [InlineData(0f, -0.4f, EmotionKind.Distress)]
    public void ComputeStimulus_ZeroEffect_StillCountsAsNonNegativeForDesirability(float effect, float attitude, EmotionKind expectedKind)
    {
        // Рис. 2: ветка триггера использует "effect >= 0", а не "effect > 0" — effect == 0
        // должен по-прежнему давать эмоцию (в паре со знаком attitude), а не гасить желательность в 0.
        var (actions, appraisal) = BuildDictionaries(effect, attitude);
        var evt = new GameEvent(AgentId, Action, PatientId, dc: 1f);

        var stimulus = EmotionRules.ComputeStimulus(evt, actions, appraisal, PerceiverId);

        Assert.True(stimulus[expectedKind] > 0f);
    }

    [Fact]
    public void ComputeStimulus_NeutralAttitude_TriggersNoDesirabilityEmotion()
    {
        var (actions, appraisal) = BuildDictionaries(effect: 0.6f, attitude: 0f);
        var evt = new GameEvent(AgentId, Action, PatientId, dc: 1f);

        var stimulus = EmotionRules.ComputeStimulus(evt, actions, appraisal, PerceiverId);

        Assert.Equal(0f, stimulus[EmotionKind.Joy]);
        Assert.Equal(0f, stimulus[EmotionKind.Distress]);
        Assert.Equal(0f, stimulus[EmotionKind.Hope]);
        Assert.Equal(0f, stimulus[EmotionKind.Fear]);
    }

    [Theory]
    [InlineData(true, 0.7f, EmotionKind.Pride)]
    [InlineData(true, -0.7f, EmotionKind.Shame)]
    [InlineData(false, 0.7f, EmotionKind.Admiration)]
    [InlineData(false, -0.7f, EmotionKind.Anger)]
    public void ComputeStimulus_Praise_TriggersPrideShameAdmirationOrAnger(bool perceiverIsAgent, float praise, EmotionKind expectedKind)
    {
        var actions = new ActionDictionary();
        var appraisal = new Appraisal();
        appraisal.SetPraise(Action, praise);

        int agentId = perceiverIsAgent ? PerceiverId : AgentId;
        var evt = new GameEvent(agentId, Action, PatientId, dc: 1f);

        var stimulus = EmotionRules.ComputeStimulus(evt, actions, appraisal, PerceiverId);

        FloatAssert.Approximately(MathF.Abs(praise), stimulus[expectedKind]);
    }

    [Fact]
    public void ComputeStimulus_Praise_OnlyTriggersForEyewitness()
    {
        // Рис. 2: ветки pride/admiration/shame/anger помечены dc=1 — не должны срабатывать при dc<1.
        var actions = new ActionDictionary();
        var appraisal = new Appraisal();
        appraisal.SetPraise(Action, 0.7f);
        var evt = new GameEvent(PerceiverId, Action, PatientId, dc: 0.5f);

        var stimulus = EmotionRules.ComputeStimulus(evt, actions, appraisal, PerceiverId);

        Assert.Equal(0f, stimulus[EmotionKind.Pride]);
    }

    private static (ActionDictionary Actions, Appraisal Appraisal) BuildDictionaries(float effect, float attitude)
    {
        var actions = new ActionDictionary();
        actions.SetEffect(Action, effect);

        var appraisal = new Appraisal();
        appraisal.SetAttitude(PatientId, attitude);

        return (actions, appraisal);
    }
}

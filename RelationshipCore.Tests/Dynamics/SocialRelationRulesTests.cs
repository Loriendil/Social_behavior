using RelationshipCore.Dynamics;
using RelationshipCore.Dynamics.Rules;

namespace RelationshipCore.Tests.Dynamics;

public class SocialRelationRulesTests
{
    [Fact]
    public void UpdateFromOwnEmotion_PositiveEmotion_IncreasesLiking()
    {
        var current = SocialRelation.Neutral;
        var joy = EmotionVector.Single(EmotionKind.Joy, 0.8f);

        var updated = SocialRelationRules.UpdateFromOwnEmotion(current, joy);

        Assert.True(updated.Liking > current.Liking);
    }

    [Fact]
    public void UpdateFromOwnEmotion_NegativeEmotion_DecreasesLiking()
    {
        var current = SocialRelation.Neutral;
        var fear = EmotionVector.Single(EmotionKind.Fear, 0.8f);

        var updated = SocialRelationRules.UpdateFromOwnEmotion(current, fear);

        Assert.True(updated.Liking < current.Liking);
    }

    [Fact]
    public void UpdateFromOwnEmotion_Fear_DecreasesOwnDominance()
    {
        var current = SocialRelation.Neutral;
        var fear = EmotionVector.Single(EmotionKind.Fear, 0.8f);

        var updated = SocialRelationRules.UpdateFromOwnEmotion(current, fear);

        Assert.True(updated.Dominance < current.Dominance);
    }

    [Fact]
    public void UpdateFromOwnEmotion_Pride_IncreasesOwnDominance()
    {
        var current = SocialRelation.Neutral;
        var pride = EmotionVector.Single(EmotionKind.Pride, 0.8f);

        var updated = SocialRelationRules.UpdateFromOwnEmotion(current, pride);

        Assert.True(updated.Dominance > current.Dominance);
    }

    [Fact]
    public void UpdateFromObservedExpression_InterlocutorFear_IncreasesOwnDominance()
    {
        // "выражение страха собеседником -> своя dominance растёт" (сценарий грабителя, CLAUDE.md)
        var current = SocialRelation.Neutral;
        var interlocutorFear = EmotionVector.Single(EmotionKind.Fear, 0.8f);

        var updated = SocialRelationRules.UpdateFromObservedExpression(current, interlocutorFear);

        Assert.True(updated.Dominance > current.Dominance);
    }

    [Fact]
    public void UpdateFromObservedExpression_InterlocutorPride_DecreasesOwnDominance()
    {
        var current = SocialRelation.Neutral;
        var interlocutorPride = EmotionVector.Single(EmotionKind.Pride, 0.8f);

        var updated = SocialRelationRules.UpdateFromObservedExpression(current, interlocutorPride);

        Assert.True(updated.Dominance < current.Dominance);
    }

    [Fact]
    public void UpdateFromObservedExpression_InterlocutorNegativeEmotion_DecreasesSolidarity()
    {
        var current = SocialRelation.Neutral;
        var distress = EmotionVector.Single(EmotionKind.Distress, 0.6f);

        var updated = SocialRelationRules.UpdateFromObservedExpression(current, distress);

        Assert.True(updated.Solidarity < current.Solidarity);
    }

    [Fact]
    public void UpdateSolidarityFromCoincidence_MatchingEmotions_IncreasesSolidarity()
    {
        var current = SocialRelation.Neutral;
        var own = EmotionVector.Single(EmotionKind.Distress, 0.6f);
        var other = EmotionVector.Single(EmotionKind.Distress, 0.5f);

        var updated = SocialRelationRules.UpdateSolidarityFromCoincidence(current, own, other);

        Assert.True(updated.Solidarity > current.Solidarity);
    }

    [Fact]
    public void UpdateSolidarityFromCoincidence_NoOverlap_LeavesSolidarityUnchanged()
    {
        var current = SocialRelation.Neutral;
        var own = EmotionVector.Single(EmotionKind.Joy, 0.6f);
        var other = EmotionVector.Single(EmotionKind.Distress, 0.6f);

        var updated = SocialRelationRules.UpdateSolidarityFromCoincidence(current, own, other);

        FloatAssert.Approximately(current.Solidarity, updated.Solidarity);
    }

    [Fact]
    public void UpdateFamiliarityFromLikingShift_GrowsRegardlessOfLikingDirection()
    {
        var before = SocialRelation.Neutral;
        var afterPositive = new SocialRelation(liking: 0.5f, dominance: 0f, familiarity: 0f, solidarity: 0f);
        var afterNegative = new SocialRelation(liking: -0.5f, dominance: 0f, familiarity: 0f, solidarity: 0f);

        var updatedPositive = SocialRelationRules.UpdateFamiliarityFromLikingShift(before, afterPositive);
        var updatedNegative = SocialRelationRules.UpdateFamiliarityFromLikingShift(before, afterNegative);

        Assert.True(updatedPositive.Familiarity > 0f);
        FloatAssert.Approximately(updatedPositive.Familiarity, updatedNegative.Familiarity);
    }

    [Fact]
    public void ApplyBounded_ChangesLessNearExtremes()
    {
        float deltaNearCenter = SocialRelationRules.ApplyBounded(0f, 0.5f) - 0f;
        float deltaNearEdge = SocialRelationRules.ApplyBounded(0.95f, 0.5f) - 0.95f;

        Assert.True(deltaNearEdge < deltaNearCenter);
    }
}

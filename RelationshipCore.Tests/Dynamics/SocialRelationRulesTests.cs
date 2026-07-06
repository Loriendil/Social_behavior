using RelationshipCore.Dynamics;
using RelationshipCore.Dynamics.Rules;

namespace RelationshipCore.Tests.Dynamics;

public class SocialRelationRulesTests
{
    [Fact]
    public void FromOwnEmotion_PositiveEmotion_IncreasesLiking()
    {
        var delta = SocialRelationRules.FromOwnEmotion(EmotionVector.Single(EmotionKind.Joy, 0.8f));

        Assert.True(delta.Liking > 0f);
    }

    [Fact]
    public void FromOwnEmotion_NegativeEmotion_DecreasesLiking()
    {
        var delta = SocialRelationRules.FromOwnEmotion(EmotionVector.Single(EmotionKind.Fear, 0.8f));

        Assert.True(delta.Liking < 0f);
    }

    [Fact]
    public void FromOwnEmotion_ReliefAndDisappointment_DoNotAffectLiking()
    {
        // Рис. 4 не включает relief/disappointment в набор, влияющий на liking.
        var relief = SocialRelationRules.FromOwnEmotion(EmotionVector.Single(EmotionKind.Relief, 0.8f));
        var disappointment = SocialRelationRules.FromOwnEmotion(EmotionVector.Single(EmotionKind.Disappointment, 0.8f));

        FloatAssert.Approximately(0f, relief.Liking);
        FloatAssert.Approximately(0f, disappointment.Liking);
    }

    [Fact]
    public void FromOwnEmotion_Fear_DecreasesOwnDominance()
    {
        var delta = SocialRelationRules.FromOwnEmotion(EmotionVector.Single(EmotionKind.Fear, 0.8f));

        Assert.True(delta.Dominance < 0f);
    }

    [Fact]
    public void FromOwnEmotion_Pride_IncreasesOwnDominance()
    {
        var delta = SocialRelationRules.FromOwnEmotion(EmotionVector.Single(EmotionKind.Pride, 0.8f));

        Assert.True(delta.Dominance > 0f);
    }

    [Fact]
    public void FromOwnEmotion_NegativeSolidaritySet_DecreasesSolidarity()
    {
        var delta = SocialRelationRules.FromOwnEmotion(EmotionVector.Single(EmotionKind.Disappointment, 0.6f));

        Assert.True(delta.Solidarity < 0f);
    }

    [Fact]
    public void FromObservedExpression_InterlocutorFearOrDistress_IncreasesDominance()
    {
        // "выражение страха собеседником -> своя dominance растёт" (рис. 5, "emotions expressed by j")
        var fear = SocialRelationRules.FromObservedExpression(EmotionVector.Single(EmotionKind.Fear, 0.8f));
        var distress = SocialRelationRules.FromObservedExpression(EmotionVector.Single(EmotionKind.Distress, 0.8f));

        Assert.True(fear.Dominance > 0f);
        Assert.True(distress.Dominance > 0f);
    }

    [Fact]
    public void FromObservedExpression_OtherEmotions_DoNotAffectDominance()
    {
        // Рис. 5 "emotions expressed by j" содержит только fear/distress — pride/anger туда не входят.
        var delta = SocialRelationRules.FromObservedExpression(EmotionVector.Single(EmotionKind.Pride, 0.8f));

        FloatAssert.Approximately(0f, delta.Dominance);
    }

    [Fact]
    public void FromEmotionalCoincidence_MatchingEmotions_IncreasesSolidarity()
    {
        var own = EmotionVector.Single(EmotionKind.Distress, 0.6f);
        var other = EmotionVector.Single(EmotionKind.Distress, 0.5f);

        var delta = SocialRelationRules.FromEmotionalCoincidence(own, other);

        Assert.True(delta.Solidarity > 0f);
    }

    [Fact]
    public void FromEmotionalCoincidence_MatchingEmotions_AlsoIncreasesLikingAsSideEffect()
    {
        // Раздел IV-B статьи (стр. 291): "...induce an increase of the solidarity and, by side
        // effect, of the degree of liking" — подтверждено также по рис. 10 (burglar-policeman).
        var own = EmotionVector.Single(EmotionKind.Distress, 0.6f);
        var other = EmotionVector.Single(EmotionKind.Distress, 0.5f);

        var delta = SocialRelationRules.FromEmotionalCoincidence(own, other);

        Assert.True(delta.Liking > 0f);
        FloatAssert.Approximately(delta.Solidarity, delta.Liking);
    }

    [Fact]
    public void FromEmotionalCoincidence_IncongruentEmotions_DecreasesSolidarity()
    {
        // Рис. 6: joy у i и distress у j одновременно — явно перечисленная несовпадающая пара.
        var own = EmotionVector.Single(EmotionKind.Joy, 0.6f);
        var other = EmotionVector.Single(EmotionKind.Distress, 0.6f);

        var delta = SocialRelationRules.FromEmotionalCoincidence(own, other);

        Assert.True(delta.Solidarity < 0f);
    }

    [Fact]
    public void FromEmotionalCoincidence_IncongruentEmotions_AlsoDecreasesLikingAsSideEffect()
    {
        var own = EmotionVector.Single(EmotionKind.Joy, 0.6f);
        var other = EmotionVector.Single(EmotionKind.Distress, 0.6f);

        var delta = SocialRelationRules.FromEmotionalCoincidence(own, other);

        Assert.True(delta.Liking < 0f);
    }

    [Fact]
    public void FromEmotionalCoincidence_UnrelatedEmotions_LeavesSolidarityUnchanged()
    {
        // Pride не входит в набор эмоций, участвующих в сравнении совпадения/несовпадения.
        var own = EmotionVector.Single(EmotionKind.Pride, 0.6f);
        var other = EmotionVector.Single(EmotionKind.Pride, 0.6f);

        var delta = SocialRelationRules.FromEmotionalCoincidence(own, other);

        FloatAssert.Approximately(0f, delta.Solidarity);
    }

    [Fact]
    public void Apply_CombinesMultipleDeltaSourcesInOneBoundedStep()
    {
        var current = SocialRelation.Neutral;
        var ownFear = SocialRelationRules.FromOwnEmotion(EmotionVector.Single(EmotionKind.Fear, 0.8f));
        var observedFear = SocialRelationRules.FromObservedExpression(EmotionVector.Single(EmotionKind.Fear, 0.8f));

        var updated = SocialRelationRules.Apply(current, ownFear + observedFear);

        // Свой страх снижает dominance, наблюдаемый страх собеседника её поднимает — эффекты должны частично гасить друг друга.
        Assert.True(updated.Liking < current.Liking);
    }

    [Fact]
    public void Apply_ClampsLikingAndDominanceToSignedRange()
    {
        var current = new SocialRelation(liking: 0.99f, dominance: 0.99f, familiarity: 0f, solidarity: 0f);
        var delta = new SocialRelationDelta(liking: 10f, dominance: 10f);

        var updated = SocialRelationRules.Apply(current, delta);

        Assert.True(updated.Liking <= 1f);
        Assert.True(updated.Dominance <= 1f);
    }

    [Fact]
    public void Apply_ClampsSolidarityToUnitRange()
    {
        var current = new SocialRelation(liking: 0f, dominance: 0f, familiarity: 0f, solidarity: 0.99f);
        var delta = new SocialRelationDelta(solidarity: 10f);

        var updated = SocialRelationRules.Apply(current, delta);

        Assert.InRange(updated.Solidarity, 0f, 1f);
    }

    [Theory]
    [InlineData(10f, 10f, 10f)]
    [InlineData(-10f, -10f, -10f)]
    [InlineData(0f, 0f, 0f)]
    public void Apply_NeverChangesFamiliarityRegardlessOfDelta(float liking, float dominance, float solidarity)
    {
        // Задача 4 (заморозка familiarity): статья противоречит сама себе (раздел III-D говорит,
        // что эмоции не влияют на familiarity напрямую; раздел IV-A буквально прочитанный —
        // предполагает обратное). Принятое решение — следовать III-D: ни один источник дельты
        // (f_sr) не должен сдвигать familiarity, при любой силе emotional delta.
        var current = new SocialRelation(liking: 0.2f, dominance: -0.1f, familiarity: 0.37f, solidarity: 0.6f);
        var delta = new SocialRelationDelta(liking, dominance, solidarity);

        var updated = SocialRelationRules.Apply(current, delta);

        FloatAssert.Approximately(current.Familiarity, updated.Familiarity);
    }

    [Fact]
    public void FamiliarityFromInformationTransfer_ZeroConfidentiality_LeavesFamiliarityUnchanged()
    {
        var current = new SocialRelation(liking: 0f, dominance: 0f, familiarity: 0.4f, solidarity: 0f);

        var updated = SocialRelationRules.FamiliarityFromInformationTransfer(current, confidentiality: 0f);

        FloatAssert.Approximately(current.Familiarity, updated.Familiarity);
    }

    [Fact]
    public void FamiliarityFromInformationTransfer_PositiveConfidentiality_GrowsMonotonicallyWithinUnitRange()
    {
        var current = SocialRelation.Neutral; // familiarity = 0

        var afterLowConfidentiality = SocialRelationRules.FamiliarityFromInformationTransfer(current, confidentiality: 0.2f);
        var afterHighConfidentiality = SocialRelationRules.FamiliarityFromInformationTransfer(current, confidentiality: 0.9f);

        Assert.True(afterLowConfidentiality.Familiarity > current.Familiarity);
        Assert.True(afterHighConfidentiality.Familiarity > afterLowConfidentiality.Familiarity);
        Assert.InRange(afterHighConfidentiality.Familiarity, 0f, 1f);
    }

    [Fact]
    public void FamiliarityFromInformationTransfer_DoesNotAffectOtherDimensions()
    {
        var current = new SocialRelation(liking: 0.3f, dominance: -0.2f, familiarity: 0f, solidarity: 0.5f);

        var updated = SocialRelationRules.FamiliarityFromInformationTransfer(current, confidentiality: 1f);

        FloatAssert.Approximately(current.Liking, updated.Liking);
        FloatAssert.Approximately(current.Dominance, updated.Dominance);
        FloatAssert.Approximately(current.Solidarity, updated.Solidarity);
    }

    [Fact]
    public void ApplyBoundedSigned_ChangesLessNearExtremes()
    {
        float deltaNearCenter = SocialRelationRules.ApplyBoundedSigned(0f, 0.5f) - 0f;
        float deltaNearEdge = SocialRelationRules.ApplyBoundedSigned(0.95f, 0.5f) - 0.95f;

        Assert.True(deltaNearEdge < deltaNearCenter);
    }

    [Fact]
    public void ApplyBoundedUnit_ChangesLessNearBothExtremes()
    {
        float deltaNearCenter = SocialRelationRules.ApplyBoundedUnit(0.5f, 0.3f) - 0.5f;
        float deltaNearZero = SocialRelationRules.ApplyBoundedUnit(0.02f, 0.3f) - 0.02f;
        float deltaNearOne = SocialRelationRules.ApplyBoundedUnit(0.98f, 0.3f) - 0.98f;

        Assert.True(deltaNearZero < deltaNearCenter);
        Assert.True(deltaNearOne < deltaNearCenter);
    }
}

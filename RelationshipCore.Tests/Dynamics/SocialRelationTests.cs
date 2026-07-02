using RelationshipCore.Dynamics;

namespace RelationshipCore.Tests.Dynamics;

public class SocialRelationTests
{
    [Fact]
    public void Constructor_ClampsLikingAndDominanceToSignedRange()
    {
        var relation = new SocialRelation(liking: 1.5f, dominance: -1.5f, familiarity: 0f, solidarity: 0f);

        Assert.Equal(1f, relation.Liking);
        Assert.Equal(-1f, relation.Dominance);
    }

    [Fact]
    public void Constructor_ClampsFamiliarityAndSolidarityToUnitRange()
    {
        // Статья определяет familiarity и solidarity как ∈[0,1] — не [-1,1], в отличие от liking/dominance.
        var relation = new SocialRelation(liking: 0f, dominance: 0f, familiarity: -0.5f, solidarity: 1.5f);

        Assert.Equal(0f, relation.Familiarity);
        Assert.Equal(1f, relation.Solidarity);
    }

    [Fact]
    public void Matches_AnotherSocialRelation_UsesEpsilonTolerance()
    {
        var a = new SocialRelation(0.5f, 0.1f, 0.2f, 0.3f);
        var b = new SocialRelation(0.5f, 0.1f, 0.2f, 0.3f);
        var c = new SocialRelation(0.9f, 0.1f, 0.2f, 0.3f);

        Assert.True(a.Matches(b));
        Assert.False(a.Matches(c));
    }

    [Fact]
    public void Matches_Pattern_ChecksRangesPerDimension()
    {
        var friend = new SocialRelation(liking: 0.8f, dominance: 0f, familiarity: 0.5f, solidarity: 0.5f);
        var enemy = new SocialRelation(liking: -0.8f, dominance: 0f, familiarity: 0.5f, solidarity: 0.5f);
        var friendsPattern = new SocialRelationPattern { Liking = FloatRange.AtLeast(0.3f) };

        Assert.True(friend.Matches(friendsPattern));
        Assert.False(enemy.Matches(friendsPattern));
    }

    [Fact]
    public void Matches_PatternWithNullDimension_TreatsItAsAnyValue()
    {
        var relation = new SocialRelation(liking: -0.9f, dominance: 0.9f, familiarity: 0f, solidarity: 0f);
        var dominanceOnlyPattern = new SocialRelationPattern { Dominance = FloatRange.AtLeast(0.5f) };

        Assert.True(relation.Matches(dominanceOnlyPattern));
    }
}

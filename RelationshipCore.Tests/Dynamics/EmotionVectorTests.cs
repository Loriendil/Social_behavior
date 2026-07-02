using RelationshipCore.Dynamics;

namespace RelationshipCore.Tests.Dynamics;

public class EmotionVectorTests
{
    [Fact]
    public void Zero_HasAllEmotionsAtZero()
    {
        foreach (EmotionKind kind in Enum.GetValues(typeof(EmotionKind)))
        {
            Assert.Equal(0f, EmotionVector.Zero[kind]);
        }
    }

    [Fact]
    public void Single_SetsOnlyOneEmotion()
    {
        var vector = EmotionVector.Single(EmotionKind.Fear, 0.7f);

        Assert.Equal(0.7f, vector[EmotionKind.Fear]);
        Assert.Equal(0f, vector[EmotionKind.Joy]);
    }

    [Fact]
    public void With_UpdatesOneEmotionAndPreservesOthers()
    {
        var vector = EmotionVector.Single(EmotionKind.Joy, 0.3f);

        var updated = vector.With(EmotionKind.Fear, 0.4f);

        Assert.Equal(0.3f, updated[EmotionKind.Joy]);
        Assert.Equal(0.4f, updated[EmotionKind.Fear]);
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(1.5f, 1f)]
    public void With_ClampsToZeroOneRange(float input, float expected)
    {
        var vector = EmotionVector.Zero.With(EmotionKind.Joy, input);

        Assert.Equal(expected, vector[EmotionKind.Joy]);
    }
}

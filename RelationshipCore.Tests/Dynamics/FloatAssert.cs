namespace RelationshipCore.Tests.Dynamics;

internal static class FloatAssert
{
    public static void Approximately(float expected, float actual, float tolerance = 0.001f) =>
        Assert.True(MathF.Abs(expected - actual) < tolerance, $"expected {expected}, got {actual}");
}

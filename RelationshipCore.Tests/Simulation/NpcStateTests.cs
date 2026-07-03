using RelationshipCore.Dynamics;
using RelationshipCore.Simulation;

namespace RelationshipCore.Tests.Simulation;

public class NpcStateTests
{
    [Fact]
    public void Personality_CanBeChangedAfterConstruction()
    {
        // Нужно редактору персонажей: менять личность уже созданного NPC, а не только при регистрации.
        var state = new NpcState(1, new Personality(0f, 0f));

        state.Personality = new Personality(0.8f, -0.5f);

        Assert.Equal(0.8f, state.Personality.Extraversion);
        Assert.Equal(-0.5f, state.Personality.Neuroticism);
    }
}

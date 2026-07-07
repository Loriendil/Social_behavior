using System.ComponentModel;
using System.Runtime.CompilerServices;
using RelationshipCore.Dynamics;
using RelationshipCore.Simulation;

namespace RelationshipSimulator;

/// <summary>Строка DataGrid для одного NPC — тонкая обёртка над NpcState, пишет изменения сразу в движок.</summary>
public sealed class NpcRow : INotifyPropertyChanged
{
    private readonly NpcState _state;

    public NpcRow(NpcState state)
    {
        _state = state;
    }

    public int EntityId => _state.EntityId;

    public float Extraversion
    {
        get => _state.Personality.Extraversion;
        set
        {
            _state.Personality = new Personality(value, _state.Personality.Neuroticism);
            OnPropertyChanged();
        }
    }

    public float Neuroticism
    {
        get => _state.Personality.Neuroticism;
        set
        {
            _state.Personality = new Personality(_state.Personality.Extraversion, value);
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

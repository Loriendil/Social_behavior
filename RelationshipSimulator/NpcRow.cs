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

    /// <summary>
    /// Attitude к самому себе — самое нагруженное значение Appraisal во всех сценариях статьи
    /// Ochs (самоуважение: "плохое для меня" событие вызывает страх/дистресс). Без него события
    /// с patient == EntityId никогда не триггерят эмоцию (attitude==0 — ни одна ветка не срабатывает).
    /// </summary>
    public float SelfAttitude
    {
        get => _state.Appraisal.GetAttitude(_state.EntityId);
        set
        {
            _state.Appraisal.SetAttitude(_state.EntityId, value);
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

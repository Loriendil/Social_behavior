using System.ComponentModel;
using System.Runtime.CompilerServices;
using RelationshipCore.Dynamics;
using RelationshipCore.Simulation;

namespace RelationshipSimulator;

/// <summary>
/// Строка DataGrid для одной записи attitude(владелец, целевая сущность) — задача 1
/// (attitudes к произвольным сущностям, не только к себе; owner==target — это attitude "к себе",
/// единственное хранилище для него после задачи 3 раунда правок 2 — колонка "Attitude к себе" на
/// вкладке "Персонажи" удалена). OwnerId и TargetEntityId редактируемы, поэтому NpcState
/// владельца ищется через движок в момент чтения/записи Attitude, а не хранится захваченной
/// ссылкой. <paramref name="isDuplicate"/> — обратный вызов в MainWindow, проверяющий, не
/// совпадёт ли пара (OwnerId, TargetEntityId) с уже существующей строкой; если совпадёт, edit
/// отклоняется (значение возвращается к прежнему) — задача 3, "запретить дублирующие строки".
/// </summary>
public sealed class AttitudeRow : INotifyPropertyChanged
{
    private readonly SocialDynamicsEngine _engine;
    private readonly Func<AttitudeRow, int, int, bool>? _isDuplicate;
    private int _ownerId;
    private int _targetEntityId;

    public AttitudeRow(SocialDynamicsEngine engine, int ownerId, int targetEntityId, Func<AttitudeRow, int, int, bool>? isDuplicate = null)
    {
        _engine = engine;
        _ownerId = ownerId;
        _targetEntityId = targetEntityId;
        _isDuplicate = isDuplicate;
    }

    public int OwnerId
    {
        get => _ownerId;
        set
        {
            if (_isDuplicate?.Invoke(this, value, _targetEntityId) == true)
            {
                OnPropertyChanged(); // откатить UI к прежнему значению
                return;
            }

            _ownerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Attitude));
        }
    }

    public int TargetEntityId
    {
        get => _targetEntityId;
        set
        {
            if (_isDuplicate?.Invoke(this, _ownerId, value) == true)
            {
                OnPropertyChanged(); // откатить UI к прежнему значению
                return;
            }

            _targetEntityId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Attitude));
        }
    }

    public float Attitude
    {
        get => TryGetOwner(out var owner) ? owner.Appraisal.GetAttitude(_targetEntityId) : 0f;
        set
        {
            if (TryGetOwner(out var owner))
            {
                owner.Appraisal.SetAttitude(_targetEntityId, value);
            }

            OnPropertyChanged();
        }
    }

    private bool TryGetOwner(out NpcState owner)
    {
        try
        {
            owner = _engine.GetState(_ownerId);
            return true;
        }
        catch (InvalidOperationException)
        {
            owner = null!;
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

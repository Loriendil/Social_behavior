using RelationshipCore.Dynamics;

namespace RelationshipCore.Simulation;

/// <summary>
/// Состояние одного NPC для динамики Ochs: личность, текущий вектор эмоций и субъективные
/// оценки (Appraisal). Не хранится в Node — не у всех узлов графа есть личность (предметы,
/// локации); живёт в реестре <see cref="SocialDynamicsEngine"/>, адресуется по EntityId.
/// </summary>
public sealed class NpcState
{
    public NpcState(int entityId, Personality personality = default)
    {
        EntityId = entityId;
        Personality = personality;
        Appraisal = new Appraisal();
        Emotions = EmotionVector.Zero;
        LastUpdateTime = 0f;
    }

    public int EntityId { get; }

    /// <summary>
    /// Изменяемо (как и Emotions/LastUpdateTime — NpcState реестровая запись, а не immutable
    /// value-объект): редактор персонажей должен уметь менять личность уже созданного NPC.
    /// Сам Personality остаётся immutable-структурой — меняется лишь то, на какое значение
    /// ссылается контейнер.
    /// </summary>
    public Personality Personality { get; set; }

    public Appraisal Appraisal { get; }

    public EmotionVector Emotions { get; set; }

    /// <summary>Момент времени, к которому уже применено затухание Emotions — опора для ленивого decay при следующем обращении.</summary>
    public float LastUpdateTime { get; set; }
}

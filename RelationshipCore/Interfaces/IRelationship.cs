namespace RelationshipCore;

/// <summary>Отношение — атрибут ребра, а не само ребро (см. раздел 3.1.1 диссертации O'Connor).</summary>
public interface IRelationship
{
    bool Matches(IRelationship other);
}

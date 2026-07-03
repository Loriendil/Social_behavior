namespace RelationshipCore.Groups;

/// <summary>
/// Членство узла в группе (дисс. О'Коннора, раздел 3.4.1, рис. 3.3: "MEMBER" relationship к
/// узлу-группе). Марker-тип без параметров — структурный факт слоя O'Connor (состоит/не состоит),
/// а не непрерывная динамика Ochs, поэтому не SocialRelation.
/// </summary>
public sealed class MemberRelationship : IRelationship
{
    public static readonly MemberRelationship Instance = new();

    private MemberRelationship()
    {
    }

    public bool Matches(IRelationship other) => other is MemberRelationship;

    public override string ToString() => "MEMBER";
}

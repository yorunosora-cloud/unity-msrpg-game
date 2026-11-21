/// <summary>
/// 스킬 슬롯 상수 (설계도 22 §4-1).
/// 슬롯 0=Q(기본기), 1=R, 2=T, 3=F, 4=V, 5=G, 6=B.
/// SkillDef.slot이 -1(레거시)이면 슬롯 = 배열 인덱스 + 1.
/// </summary>
public static class SkillSlots
{
    public const int Basic = 0;   // Q
    public const int Count = 7;   // 0..6

    static readonly string[] Labels = { "Q", "R", "T", "F", "V", "G", "B" };

    /// <summary>슬롯의 키 라벨. 범위 밖이면 "?".</summary>
    public static string KeyLabel(int slot)
        => (slot >= 0 && slot < Labels.Length) ? Labels[slot] : "?";

    /// <summary>slot이 -1(레거시)인 스킬의 슬롯: 배열 인덱스 + 1.</summary>
    public static int LegacySlot(int arrayIndex) => arrayIndex + 1;
}

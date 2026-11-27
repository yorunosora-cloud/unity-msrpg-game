/// <summary>시너지 난이도 튜닝(구현계획 23-B). 보스전에서 표식 지속시간을 줄인다.</summary>
public static class SynergyTuning
{
    /// <summary>표식 지속시간 배율. 1=기본, 보스 0.6, 최소 0.4.</summary>
    public static float MarkDurationMult = 1f;

    public static void Reset() => MarkDurationMult = 1f;
}

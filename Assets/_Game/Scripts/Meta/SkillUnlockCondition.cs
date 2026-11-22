using System.Globalization;

public enum SkillUnlockConditionType { Always, DamageTakenPercent, PromotionRankAtLeast, StoryFlag }

/// <summary>
/// 스킬 추가 해금 조건 (설계도 18 §3-6, §8-2). 형식 "조건타입:파라미터".
/// 빈칸·알 수 없는 타입·잘못된 숫자는 Always로 폴백. 이 단계는 파싱까지만(평가는 18-B).
/// </summary>
public struct SkillUnlockCondition
{
    public SkillUnlockConditionType type;
    public float  numericParam;   // DamageTakenPercent=0~1, PromotionRankAtLeast=랭크
    public string stringParam;    // StoryFlag id

    public static SkillUnlockCondition Parse(string raw)
    {
        var always = new SkillUnlockCondition { type = SkillUnlockConditionType.Always };
        if (string.IsNullOrWhiteSpace(raw)) return always;

        int i = raw.IndexOf(':');
        if (i <= 0 || i >= raw.Length - 1) return always;
        string kind = raw.Substring(0, i).Trim();
        string arg  = raw.Substring(i + 1).Trim();

        switch (kind)
        {
            case "DamageTakenPercent":
                return float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float d)
                    ? new SkillUnlockCondition { type = SkillUnlockConditionType.DamageTakenPercent, numericParam = d } : always;
            case "PromotionRankAtLeast":
                return float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float p)
                    ? new SkillUnlockCondition { type = SkillUnlockConditionType.PromotionRankAtLeast, numericParam = p } : always;
            case "StoryFlag":
                return new SkillUnlockCondition { type = SkillUnlockConditionType.StoryFlag, stringParam = arg };
            default:
                return always;
        }
    }
}

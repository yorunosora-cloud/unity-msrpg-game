using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격 1타의 공통 처리 (기본기·스킬 공용). 피해 파이프라인:
/// atk' = baseAtk × 위력 배율(공격 행동당 1회 계산, 소모형 포함) × 타격 공식(대상별) × 강점 태그 배율 → ReceiveHit → HitInfo 기록.
/// 설계도 22 §4-4, §4-7. 호출 쪽이 모아 둔 hits를 행동이 끝나면 CombatCharacter.NotifyHits로 한 번에 알린다.
/// </summary>
public static class HitPipeline
{
    public static void Apply(ISkillTarget target, int baseAtk, CombatCharacter caster, float hitMult,
                             float skillRange, Vector3 playerPos, string skillId, bool isBasic,
                             int actionId, List<HitInfo> hits, string[] strengthTags = null)
    {
        if (target == null) return;
        if (target is Object o && o == null) return;     // 이미 파괴된 대상
        if (target.IsDead) return;

        float formula = caster.FormulaMultiplier(target, skillRange, playerPos);
        float tagMult = TagRuleTable.Multiplier(strengthTags, target.Attributes);   // 강점 태그(설계도 18)
        int   atk     = Mathf.RoundToInt(baseAtk * hitMult * formula * tagMult);

        var   enemy  = target as Enemy;
        int   before = enemy != null && enemy.Unit != null ? enemy.Unit.Hp : -1;
        Vector3 point = target.Transform.position;       // ReceiveHit 후 파괴될 수 있어 미리 저장

        target.ReceiveHit(caster.Element, atk);

        int dealt = before >= 0 ? before - enemy.Unit.Hp : atk;   // 보스 등은 공격값으로 근사
        hits.Add(new HitInfo
        {
            target = target, damage = dealt, sourceSkillId = skillId, isBasic = isBasic,
            hitPoint = point, actionId = actionId,
        });
    }
}

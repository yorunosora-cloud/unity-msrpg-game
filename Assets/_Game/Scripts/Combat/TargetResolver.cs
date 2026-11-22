using System.Collections.Generic;

/// <summary>효과의 대상 범위 (설계도 22 §4-5). 아군 = 대기 중인 파티원 + 자신.</summary>
public enum TargetScope
{
    EnemyArea,      // 스킬 형태 안의 적
    Self,           // 시전자(활성 캐릭터)
    Bench,          // 대기 아군(기절 제외). targetCap=1이고 대기 아군이 없으면 자신(fallbackSelf)
    SelfAndBench,   // 자신 + 대기 아군 (targetCap = 자신 포함 최대 인원)
    HitTarget,      // 적중한 대상(OnHit 패시브 전용 — 적 대상이라 아군 해석에서는 비어 있음)
    Attacker,       // 공격자(OnHitTaken 패시브 전용)
}

/// <summary>아군 대상 선택 규칙 (순수 정적, 테스트 가능).</summary>
public static class TargetResolver
{
    /// <summary>
    /// 범위에 해당하는 아군(CombatCharacter) 목록. EnemyArea는 빈 목록(적은 Executor가 형태로 선택).
    /// targetCap: 0=제한 없음. 대기 아군은 HP 비율 낮은 순, 기절·시전자 제외.
    /// </summary>
    public static List<CombatCharacter> Resolve(Party party, CombatCharacter caster,
                                                TargetScope scope, int targetCap)
    {
        var result = new List<CombatCharacter>();
        if (caster == null || scope == TargetScope.EnemyArea
            || scope == TargetScope.HitTarget || scope == TargetScope.Attacker) return result;

        if (scope == TargetScope.Self)
        {
            result.Add(caster);
            return result;
        }

        var bench = new List<CombatCharacter>();
        if (party != null)
        {
            foreach (var m in party.Members)
                if (m != null && m != caster && !m.IsDowned) bench.Add(m);
        }
        bench.Sort((a, b) => ((float)a.Hp / a.MaxHp).CompareTo((float)b.Hp / b.MaxHp));

        if (scope == TargetScope.Bench)
        {
            if (bench.Count == 0)
            {
                if (targetCap == 1) result.Add(caster);   // fallbackSelf
                return result;
            }
            int n = targetCap > 0 ? System.Math.Min(targetCap, bench.Count) : bench.Count;
            for (int i = 0; i < n; i++) result.Add(bench[i]);
            return result;
        }

        // SelfAndBench: 자신 + 대기 아군, cap은 자신 포함
        result.Add(caster);
        int slots = targetCap > 0 ? targetCap - 1 : bench.Count;
        for (int i = 0; i < bench.Count && i < slots; i++) result.Add(bench[i]);
        return result;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 표식 연계 발동(설계도 03 §3-3, 구현계획 23-B).
/// 소모자가 트리거 스킬로 표식 걸린 적을 맞히면 표식이 소멸하고 연계기(별도 스킬)가 추가로 터진다.
/// 표식은 <b>행동 시작 시점</b> 기준이라, 트리거 스킬이 방금 건 표식으로 자기 연계를 일으키지 않는다.
/// </summary>
public static class SynergyResolver
{
    static SynergyComboTable _table;
    static bool _tableLoaded;

    /// <summary>연계 정의 테이블. 지정하지 않으면 Resources/SynergyComboTable을 한 번 읽는다(테스트는 직접 지정).</summary>
    public static SynergyComboTable Table
    {
        get
        {
            if (!_tableLoaded) { _table = Resources.Load<SynergyComboTable>("SynergyComboTable"); _tableLoaded = true; }
            return _table;
        }
        set { _table = value; _tableLoaded = value != null; }
    }

    /// <summary>연계 성공 시 발행(연출·로그·보상 UI용).</summary>
    public static event Action<SynergyComboDef, CombatCharacter> ComboTriggered;

    static int _depth;   // 연계기 실행 중 재귀 발동 방지

    /// <summary>행동 시작 시점의 표식 상태. 트리거가 아니면 null.</summary>
    public sealed class Probe
    {
        public SynergyComboDef Combo;
        public List<Enemy> Marked;
    }

    public static Probe Begin(SkillDef skill, CombatCharacter caster)
    {
        if (_depth > 0 || skill == null || caster == null) return null;
        var def = Table?.Find(caster.Id, skill.id);
        if (def == null || def.comboSkill == null || string.IsNullOrEmpty(def.markId)) return null;

        var marked = new List<Enemy>();
        foreach (var e in Enemy.All.ToArray())
            if (e != null && !((IDamageable)e).IsDead && e.HasMark(def.markId)) marked.Add(e);
        return marked.Count == 0 ? null : new Probe { Combo = def, Marked = marked };
    }

    /// <summary>행동이 끝난 뒤 호출. 맞힌 적(없으면 스킬 형태 안의 적) 중 표식이 있던 적이 있으면 연계를 일으킨다.</summary>
    public static void Finish(Probe probe, SkillDef skill, int skillIndex, CombatCharacter caster, Transform origin, List<HitInfo> hits)
    {
        if (probe == null) return;

        var reached = new HashSet<ISkillTarget>();
        if (hits != null && hits.Count > 0)
        {
            foreach (var h in hits) if (h.target != null) reached.Add(h.target);
        }
        else
        {
            var eff = SkillScaling.Compute(skill, caster.SkillLevel(Mathf.Max(0, skillIndex)));
            foreach (var (t, _) in SkillExecutor.CollectInShape(skill, eff, origin)) reached.Add(t);
        }

        bool fired = false;
        foreach (var e in probe.Marked)
        {
            if (e == null || !reached.Contains(e) || !e.HasMark(probe.Combo.markId)) continue;
            e.RemoveMark(probe.Combo.markId);
            fired = true;
        }
        if (!fired) return;

        int idx = caster.IndexOfSkill(probe.Combo.comboSkill);
        _depth++;
        try { SkillExecutor.Execute(probe.Combo.comboSkill, Mathf.Max(0, idx), caster, origin); }
        finally { _depth--; }

        ComboReward.Award(MetaState.StudyMaterials, caster.Element);
        ComboTriggered?.Invoke(probe.Combo, caster);
    }
}

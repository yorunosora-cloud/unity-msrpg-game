using System;
using UnityEngine;

/// <summary>표식 연계 정의 1건(skill_combos.csv 1행, 구현계획 23-B).</summary>
[Serializable]
public class SynergyComboDef
{
    public string   id;               // "combo_vx_v"
    public string   comboName;        // "속도 도출 (v=Δx/Δt)"
    public string   formula;          // "v = Δx/Δt"
    public string   markId;           // 적이 가져야 할 표식
    public string   consumerId;       // 소모자 캐릭터 id
    public string   triggerSkillId;   // 이 스킬이 표식 걸린 적을 맞히면 발동
    public SkillDef comboSkill;       // 연계기(skillType=Combo)
}

/// <summary>연계 정의 모음. 임포터가 Resources/SynergyComboTable.asset으로 만든다.</summary>
public class SynergyComboTable : ScriptableObject
{
    public SynergyComboDef[] combos;

    /// <summary>소모자·트리거 스킬이 맞는 연계(없으면 null).</summary>
    public SynergyComboDef Find(string consumerId, string triggerSkillId)
    {
        if (combos == null) return null;
        foreach (var c in combos)
            if (c.consumerId == consumerId && c.triggerSkillId == triggerSkillId) return c;
        return null;
    }
}

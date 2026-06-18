using System;
using UnityEngine;

/// <summary>
/// 1명의 런타임 전투 액터. 순수 C# 클래스 (MonoBehaviour 아님).
/// CharacterDef(정적 정의) + OwnedCharacter(영속 인스턴스)를 합쳐 전투에 필요한
/// 스탯·HP/MP·EXP 성장을 관리한다. OwnedCharacter 참조를 공유해 레벨/EXP 변경이
/// MetaState.Roster를 통해 저장에 자동 반영된다.
/// </summary>
public class CombatCharacter
{
    readonly CharacterDef   _def;
    readonly OwnedCharacter _owned;

    int _hp;
    int _mp;
    int _nextExp; // 현재 레벨에서 다음 레벨까지 필요한 EXP 임계값

    // ── 스킬 쿨다운 · 버프 상태 ────────────────────────────────────────────
    float[] _cooldowns;      // 스킬 인덱스별 남은 쿨타임 (초). 길이 = skills.Length
    float   _atkBuffMult  = 1f;  // 공격력 버프 배율 (1f = 변화 없음)
    float   _atkBuffTimer = 0f;  // 버프 남은 시간 (0 이하면 비활성)

    public CombatCharacter(CharacterDef def, OwnedCharacter owned)
    {
        _def     = def;
        _owned   = owned;
        _nextExp = ComputeNextExp(_owned.level);

        // 세션 시작 시 HP/MP 풀
        var s = StatGrowth.ComputeStats(_def, _owned.level);
        _hp = s.hp;
        _mp = s.mp;

        // 스킬 쿨다운 배열 초기화 (skills가 null이면 빈 배열)
        _cooldowns = new float[_def.skills?.Length ?? 0];
    }

    // ── 식별 정보 ──────────────────────────────────────────────────────────

    public Continent Element     => _def.continent;
    public string    DisplayName => _def.nameKo;
    public Color     TintColor   => _def.portraitColor;
    public int       Level       => _owned.level;
    public string    Id          => _def.id;

    // ── 스킬 조회 ─────────────────────────────────────────────────────────

    /// <summary>보유 스킬 수 (인덱스 = 키 슬롯 E=0, R=1 … G=5).</summary>
    public int SkillCount => _def.skills?.Length ?? 0;

    /// <summary>인덱스 i의 SkillDef. 범위 밖이거나 null이면 null 반환.</summary>
    public SkillDef SkillAt(int i)
        => (_def.skills != null && i >= 0 && i < _def.skills.Length) ? _def.skills[i] : null;

    /// <summary>인덱스 i의 남은 쿨타임 (초). 0이면 준비 완료.</summary>
    public float CooldownRemaining(int i)
        => (i >= 0 && i < _cooldowns.Length) ? Math.Max(0f, _cooldowns[i]) : 0f;

    /// <summary>
    /// 인덱스 i의 스킬이 해금되어 있는지 확인.
    /// unlockedSkillIds가 비어 있으면 index 0만 기본 해금으로 간주한다.
    /// </summary>
    public bool IsUnlocked(int i)
    {
        var skill = SkillAt(i);
        if (skill == null) return false;

        if (_owned.unlockedSkillIds == null || _owned.unlockedSkillIds.Count == 0)
            return i == 0;

        return _owned.unlockedSkillIds.Contains(skill.id);
    }

    /// <summary>인덱스 i의 스킬을 해금한다. 이미 해금되어 있으면 무시.</summary>
    public void Unlock(int i)
    {
        var skill = SkillAt(i);
        if (skill == null) return;
        Unlock(skill.id);
    }

    /// <summary>skillId 로 스킬을 해금한다. 이미 해금되어 있으면 무시.</summary>
    public void Unlock(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return;
        if (_owned.unlockedSkillIds == null)
            _owned.unlockedSkillIds = new System.Collections.Generic.List<string>();
        if (!_owned.unlockedSkillIds.Contains(skillId))
            _owned.unlockedSkillIds.Add(skillId);
        OnChanged?.Invoke("unlock");
    }

    // ── 스탯 (현재 레벨 기준, 레벨업 시 재계산) ───────────────────────────

    public int MaxHp => StatGrowth.ComputeStats(_def, _owned.level).hp;
    public int MaxMp => StatGrowth.ComputeStats(_def, _owned.level).mp;
    /// <summary>현재 레벨 공격력 × 버프 배율 (버프 없으면 1.0).</summary>
    public int Atk   => (int)(StatGrowth.ComputeStats(_def, _owned.level).atk * _atkBuffMult);
    public int Def   => StatGrowth.ComputeStats(_def, _owned.level).def;

    // ── HP/MP 상태 ─────────────────────────────────────────────────────────

    public int  Hp       => _hp;
    public int  Mp       => _mp;
    public bool IsDowned => _hp <= 0;

    // ── EXP ───────────────────────────────────────────────────────────────

    public int Exp     => _owned.exp;
    public int NextExp => _nextExp;

    // ── 이벤트 ────────────────────────────────────────────────────────────

    /// <summary>HP/MP/레벨 등 상태 변화 시 발생. reason = "hp"|"mp"|"exp"|"levelup"|"restore".</summary>
    public event Action<string> OnChanged;

    // ── 스킬 · 쿨다운 · 버프 조작 ────────────────────────────────────────

    /// <summary>매 프레임 쿨타임 감소. PlayerSkills.Update()에서 호출.</summary>
    public void TickCooldowns(float dt)
    {
        for (int i = 0; i < _cooldowns.Length; i++)
        {
            if (_cooldowns[i] > 0f)
                _cooldowns[i] = Math.Max(0f, _cooldowns[i] - dt);
        }
    }

    /// <summary>매 프레임 버프 타이머 감소. 만료 시 배율 초기화 + OnChanged("buff").</summary>
    public void TickBuffs(float dt)
    {
        if (_atkBuffTimer <= 0f) return;
        _atkBuffTimer -= dt;
        if (_atkBuffTimer <= 0f)
        {
            _atkBuffTimer = 0f;
            _atkBuffMult  = 1f;
            OnChanged?.Invoke("buff");
        }
    }

    /// <summary>인덱스 i의 스킬이 쿨타임 만료 상태인지.</summary>
    public bool IsReady(int i)
        => i >= 0 && i < _cooldowns.Length && _cooldowns[i] <= 0f;

    /// <summary>인덱스 i의 쿨타임을 seconds로 설정.</summary>
    public void StartCooldown(int i, float seconds)
    {
        if (i >= 0 && i < _cooldowns.Length)
            _cooldowns[i] = seconds;
        OnChanged?.Invoke("cooldown");
    }

    /// <summary>MP 회복 (MaxMp 초과 시 클램프). 평타/자연회복 틱에서 호출.</summary>
    public void RecoverMp(int amount)
    {
        if (amount <= 0) return;
        _mp = Math.Min(MaxMp, _mp + amount);
        OnChanged?.Invoke("mp");
    }

    /// <summary>공격력 버프 적용 (중복 시 더 긴 쪽으로 갱신).</summary>
    public void ApplyAtkBuff(float multiplier, float duration)
    {
        _atkBuffMult  = multiplier;
        _atkBuffTimer = Math.Max(_atkBuffTimer, duration);
        OnChanged?.Invoke("buff");
    }

    /// <summary>인덱스 i 스킬을 발동할 수 있는지 확인.</summary>
    public bool CanCast(int i)
    {
        var skill = SkillAt(i);
        return skill != null && !IsDowned && IsUnlocked(i) && Mp >= skill.mpCost && IsReady(i);
    }

    /// <summary>
    /// 인덱스 i 스킬을 발동한다. 성공 시 SkillDef 반환, 실패(MP부족/쿨다운/기절) 시 null.
    /// 효과 적용은 호출자(PlayerSkills)가 SkillExecutor로 수행한다.
    /// </summary>
    public SkillDef CastSkill(int i)
    {
        if (!CanCast(i)) return null;
        var skill = SkillAt(i);
        UseMp(skill.mpCost);
        StartCooldown(i, skill.cooldown);
        AddProficiency(i);   // 시전 성공 시 숙련도 +1
        return skill;
    }

    // ── 스킬 레벨 / 숙련도 ───────────────────────────────────────────────

    /// <summary>인덱스 i 스킬의 현재 레벨 (1~5). 엔트리 없으면 1.</summary>
    public int SkillLevel(int i)
    {
        var skill = SkillAt(i);
        if (skill == null) return 1;
        return _owned.GetSkillProgress(skill.id).level;
    }

    /// <summary>인덱스 i 스킬의 현재 레벨에서 누적된 숙련도.</summary>
    public int SkillProficiency(int i)
    {
        var skill = SkillAt(i);
        if (skill == null) return 0;
        return _owned.GetSkillProgress(skill.id).proficiency;
    }

    /// <summary>인덱스 i 스킬의 현재 레벨→다음 레벨 숙련도 임계값.</summary>
    public int SkillThreshold(int i)
    {
        return SkillScaling.Threshold(SkillLevel(i));
    }

    /// <summary>인덱스 i 스킬이 레벨업 가능한지. 숙련도 충족 &amp; 만렙 미만.</summary>
    public bool CanLevelUpSkill(int i)
    {
        if (SkillAt(i) == null) return false;
        int lv = SkillLevel(i);
        if (lv >= SkillScaling.MaxLevel) return false;
        return SkillProficiency(i) >= SkillScaling.Threshold(lv);
    }

    /// <summary>
    /// 인덱스 i 스킬의 숙련도를 +1 한다.
    /// 현재 레벨 임계값에서 캡. 만렙(Lv.5)이면 무시.
    /// 시전 성공 시(CastSkill) 자동 호출된다.
    /// </summary>
    public void AddProficiency(int i)
    {
        var skill = SkillAt(i);
        if (skill == null) return;
        int lv = SkillLevel(i);
        if (lv >= SkillScaling.MaxLevel) return;

        var prog = _owned.GetSkillProgress(skill.id);
        int cap  = SkillScaling.Threshold(lv);
        prog.proficiency = Math.Min(cap, prog.proficiency + 1);
        OnChanged?.Invoke("prof");
    }

    /// <summary>
    /// 인덱스 i 스킬을 레벨업한다.
    /// 레벨 +1, 숙련도 -= 임계값(carry), 난이도 보너스 가산.
    /// CanLevelUpSkill(i) == false이면 false 반환.
    /// </summary>
    public bool LevelUpSkill(int i, ProblemDifficulty solvedAt)
    {
        if (!CanLevelUpSkill(i)) return false;

        var skill = SkillAt(i);
        var prog  = _owned.GetSkillProgress(skill.id);
        int threshold = SkillScaling.Threshold(prog.level);

        prog.proficiency -= threshold;   // carry (남은 숙련도 유지)
        prog.level++;

        // 난이도 보너스: 직전 임계값 기준
        int bonus = SkillScaling.ProficiencyBonus(solvedAt, threshold);
        int newThreshold = SkillScaling.Threshold(prog.level);
        if (bonus > 0 && prog.level < SkillScaling.MaxLevel)
            prog.proficiency = Math.Min(newThreshold, prog.proficiency + bonus);

        OnChanged?.Invoke("skilllevel");
        return true;
    }

    /// <summary>
    /// 인덱스 i 스킬의 숙련도를 10% 감소한다 (3문제 모두 오답 시 패널티).
    /// proficiency = floor(proficiency × 0.9).
    /// </summary>
    public void PenalizeProficiency(int i)
    {
        var skill = SkillAt(i);
        if (skill == null) return;
        var prog = _owned.GetSkillProgress(skill.id);
        prog.proficiency = (int)(prog.proficiency * 0.9f);
        OnChanged?.Invoke("prof");
    }

    // ── 전투 조작 ─────────────────────────────────────────────────────────

    public void Damage(int amount)
    {
        _hp = Math.Max(0, _hp - amount);
        OnChanged?.Invoke("hp");
    }

    public void Heal(int amount)
    {
        _hp = Math.Min(MaxHp, _hp + amount);
        OnChanged?.Invoke("hp");
    }

    public bool UseMp(int amount)
    {
        if (_mp < amount) return false;
        _mp -= amount;
        OnChanged?.Invoke("mp");
        return true;
    }

    /// <summary>HP/MP를 현재 레벨 기준 최대치로 완전 회복. 버프·쿨다운도 초기화.</summary>
    public void RestoreFull()
    {
        var s = StatGrowth.ComputeStats(_def, _owned.level);
        _hp = s.hp;
        _mp = s.mp;

        // 쿨다운·버프 초기화
        for (int i = 0; i < _cooldowns.Length; i++) _cooldowns[i] = 0f;
        _atkBuffMult  = 1f;
        _atkBuffTimer = 0f;

        OnChanged?.Invoke("restore");
    }

    // ── 성장 ──────────────────────────────────────────────────────────────

    /// <summary>EXP 획득 → 레벨업(등급별 상한 적용). 레벨업 시 HP/MP 풀 회복.</summary>
    public void GainExp(int amount)
    {
        if (amount <= 0) return;

        int cap = StatGrowth.LevelCap(_def.rarity);
        if (_owned.level >= cap) return; // 상한 도달 시 EXP 무시

        _owned.exp += amount;
        bool leveledUp = false;

        while (_owned.exp >= _nextExp && _owned.level < cap)
        {
            _owned.exp  -= _nextExp;
            _owned.level++;
            _nextExp     = ComputeNextExp(_owned.level);

            // 레벨업: 최대치도 올라가므로 HP/MP 재계산 후 풀 회복
            var s = StatGrowth.ComputeStats(_def, _owned.level);
            _hp       = s.hp;
            _mp       = s.mp;
            leveledUp = true;
        }

        // 상한 도달 시 잔여 EXP 리셋
        if (_owned.level >= cap)
            _owned.exp = 0;

        OnChanged?.Invoke(leveledUp ? "levelup" : "exp");
    }

    /// <summary>
    /// 자원 소모 레벨업용 — EXP 없이 레벨 1 증가. 상한 도달 시 false 반환.
    /// K 패널에서 StudyMaterial 소모 후 호출된다.
    /// </summary>
    public bool DirectLevelUp()
    {
        int cap = StatGrowth.LevelCap(_def.rarity);
        if (_owned.level >= cap) return false;

        _owned.level++;
        _owned.exp  = 0;
        _nextExp    = ComputeNextExp(_owned.level);

        var s = StatGrowth.ComputeStats(_def, _owned.level);
        _hp = s.hp;
        _mp = s.mp;

        OnChanged?.Invoke("levelup");
        return true;
    }

    // ── 내부 유틸 ─────────────────────────────────────────────────────────

    static int ComputeNextExp(int level) => StatGrowth.NextExp(level);
}

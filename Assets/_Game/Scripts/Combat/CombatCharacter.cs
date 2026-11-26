using System;
using System.Collections.Generic;
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

    /// <summary>상태 효과 컨테이너 (버프·디버프·도트·스턴·보호막). 설계도 22 §4-4.</summary>
    public StatusHolder Status { get; } = new StatusHolder();

    const string LegacyAtkBuffId = "legacy_atk_buff";

    // ── 폼 상태 (설계도 22 §4-6, §4-6-1) ──
    string            _formId = string.Empty;
    FormAutoEvaluator _auto;
    float             _revertTimer = -1f;   // 자동 복귀 남은 시간(활성일 때만 감소). <0 이면 없음
    string            _revertTo;
    float             _sinceHit = 999f;     // 마지막 피격 후 경과(초)
    PassiveRunner     _passives;
    bool              _hasStartUnlocked;   // startUnlocked 스킬이 하나라도 있으면 새 해금 모델

    public CombatCharacter(CharacterDef def, OwnedCharacter owned)
    {
        _def     = def;
        _owned   = owned;
        _nextExp = ComputeNextExp(_owned.level);
        _formId  = DefaultFormId;
        _auto    = new FormAutoEvaluator(_def.autoRules);
        if (_def.skills != null)
            foreach (var sk in _def.skills) if (sk != null && sk.startUnlocked) { _hasStartUnlocked = true; break; }
        _passives = new PassiveRunner(this, _def.skills);
        _passives.OnFire += f => PassiveFired?.Invoke(f);
        if (_def.counters != null)
            foreach (var cs in _def.counters)
                if (!string.IsNullOrEmpty(cs.id)) { _counterSpecs.Add(cs); _counters[cs.id] = new SkillCounter(cs); }

        // 세션 시작 시 HP/MP 풀
        var s = Stats();
        _hp = s.hp;
        _mp = s.mp;

        // 스킬 쿨다운 배열 초기화 (skills가 null이면 빈 배열)
        _cooldowns = new float[_def.skills?.Length ?? 0];
    }

    // ── 스킬 카운터 (23-A) ──

    readonly Dictionary<string, SkillCounter> _counters = new();
    readonly List<CounterSpec> _counterSpecs = new();
    readonly DistinctSkillWindow _recent = new DistinctSkillWindow(60f);

    /// <summary>테스트용 시계 주입. null이면 Time.time.</summary>
    public static Func<float> Clock;
    static float Now => Clock != null ? Clock() : Time.time;

    public SkillCounter Counter(string id) => id != null && _counters.TryGetValue(id, out var c) ? c : null;
    public int CounterValue(string id) => Counter(id)?.Value ?? 0;

    /// <summary>최근 windowSeconds초 안에 시전한 서로 다른 스킬 수.</summary>
    public int RecentDistinctSkills(float windowSeconds) => _recent.Count(Now, windowSeconds);

    void FireCounters(CounterTrigger ev)
    {
        foreach (var s in _counterSpecs)
        {
            var c = _counters[s.id];
            if ((s.resetOn & ev) != 0) c.Clear();
            if ((s.gainOn & ev) != 0) c.Add(1);
        }
    }

    void SyncCounterMilestones()
    {
        foreach (var s in _counterSpecs)
        {
            if (string.IsNullOrEmpty(s.milestoneSkillId) || _def.skills == null) continue;
            for (int i = 0; i < _def.skills.Length; i++)
            {
                var sk = _def.skills[i];
                if (sk == null || sk.id != s.milestoneSkillId) continue;
                _counters[s.id].SetMilestone(sk.milestoneLevel > 0 && SkillLevel(i) >= sk.milestoneLevel);
                break;
            }
        }
    }

    void ClearCounters()
    {
        foreach (var c in _counters.Values) c.Clear();
        _recent.Clear();
    }

    // ── 식별 정보 ──────────────────────────────────────────────────────────

    public Continent Element     => _def.continent;
    public string    DisplayName => _def.nameKo;
    public Color     TintColor   => CurrentForm != null ? CurrentForm.tint : _def.portraitColor;

    // ── 폼 조회 ──
    CharacterStats Stats() => StatGrowth.ComputeStats(_def, _owned.level, _formId);

    public bool HasForms => _def.forms != null && _def.forms.Length > 0;

    /// <summary>기본 폼 id(isDefault, 없으면 첫 폼). 폼이 없으면 빈 문자열.</summary>
    public string DefaultFormId
    {
        get
        {
            if (!HasForms) return string.Empty;
            foreach (var f in _def.forms) if (f != null && f.isDefault) return f.formId;
            return _def.forms[0].formId;
        }
    }

    CharacterForm FindForm(string id)
    {
        if (!HasForms || string.IsNullOrEmpty(id)) return null;
        foreach (var f in _def.forms) if (f != null && f.formId == id) return f;
        return null;
    }

    public CharacterForm   CurrentForm   => FindForm(_formId);
    public CharacterRole   CurrentRole   => CurrentForm != null ? CurrentForm.role   : _def.role;
    public Gender          CurrentGender => CurrentForm != null ? CurrentForm.gender : _def.gender;
    public string          FormNameKo    => CurrentForm != null ? CurrentForm.nameKo : string.Empty;

    /// <summary>둔화 하한: 이동속도 배율은 이 값 아래로 내려가지 않는다 (설계도 §4-13).</summary>
    public const float MinMoveSpeedMultiplier = 0.3f;

    /// <summary>플레이어 이동속도에 곱할 배율(둔화 반영, 30% 바닥).</summary>
    public float MoveSpeedMultiplier => Math.Max(MinMoveSpeedMultiplier, StatMult(StatKind.MoveSpeed));

    /// <summary>이동·기본기·스킬·폼 전환·파티 전환이 가능한 상태(기절·스턴이 아님).</summary>
    public bool CanAct => !IsDowned && !Status.IsStunned;

    // ── 패시브 (설계도 22 §4-7) ──

    /// <summary>소속 파티(Party 생성자가 설정). 대기 아군 대상 효과·파티 존재 수정자에 쓰인다.</summary>
    public Party Party { get; internal set; }

    /// <summary>패시브가 월드에 일으켜야 할 효과(PassiveHost가 구독해 실행).</summary>
    public event Action<PassiveFire> PassiveFired;

    /// <summary>레벨 스케일 전 기본 공격력(패시브 피해 비례 스택의 기준).</summary>
    public int BaseAtk => Stats().atk;

    /// <summary>능력치 배율 = 상태 효과 × 패시브(스택·상시 수정자) × 파티 존재 수정자.</summary>
    public float StatMult(StatKind k)
    {
        float lost = MaxHp > 0 ? (1f - (float)_hp / MaxHp) * 100f : 0f;
        float m = Status.Mult(k) * _passives.StatMult(k, lost);
        if (Party != null) m *= Party.PresenceMult(k);
        return m;
    }

    /// <summary>패시브 스택 수(디버그·HUD·테스트용).</summary>
    public int PassiveStacks(string skillId) => _passives.Stacks(skillId);

    /// <summary>공격 행동(기본기·스킬) 시작 시 1회 호출: 위력 배율을 계산하고 소모형 보너스(가속도·양자화)를 소모한다.</summary>
    public float HitMultiplier(bool isBasic)
    {
        float m = StatMult(StatKind.SkillPower) * StatMult(StatKind.HitDamage);
        if (isBasic) m *= StatMult(StatKind.BasicPower);
        Status.ConsumeHitEffects();
        m *= _passives.ConsumeCapStacks();
        return m;
    }

    /// <summary>대상별 타격 공식 배율(로렌츠·도플러·일·회전축).</summary>
    public float FormulaMultiplier(ISkillTarget target, float skillRange, Vector3 playerPos)
        => _passives.FormulaMult(target, skillRange, playerPos);

    /// <summary>공격 행동의 적중을 알린다(같은 행동은 actionId 동일). OnHit 패시브·스택 처리.</summary>
    public void NotifyHits(List<HitInfo> hits, bool isBasic)
    {
        if (hits == null || hits.Count == 0) return;
        OnCombatActivity?.Invoke();
        _passives.NotifyHits(hits, isBasic);
        FireCounters(CounterTrigger.OnHit | CounterTrigger.OnAction | (isBasic ? CounterTrigger.OnBasicAttack : CounterTrigger.None));
    }

    /// <summary>적중·피격 등 전투 활동 발생(전투 시작 위치 기록용).</summary>
    public event Action OnCombatActivity;

    /// <summary>공격 행동 id 발급.</summary>
    static int _nextActionId;
    public static int NewActionId() => ++_nextActionId;

    public IEnumerable<(string id, ModifierState mod)> PartyWideModifiers() => _passives.PartyWideModifiers();
    public bool HasPartyWideModifiers => _passives.HasPartyWide;
    public IEnumerable<(float radius, StatKind stat, float value)> EnemyAuras() => _passives.EnemyAuras();

    /// <summary>마지막으로 피해를 받은 뒤 경과 시간(초). 탄소 동소 변환의 "최근 피격" 판정용.</summary>
    public float SecondsSinceHitTaken => _sinceHit;
    public int       Level       => _owned.level;
    public string    Id          => _def.id;

    // ── 스킬 조회 ─────────────────────────────────────────────────────────

    /// <summary>보유 스킬 수 (인덱스 = 키 슬롯 E=0, R=1 … G=5).</summary>
    public int SkillCount => _def.skills?.Length ?? 0;

    /// <summary>인덱스 i의 SkillDef. 범위 밖이거나 null이면 null 반환.</summary>
    public SkillDef SkillAt(int i)
        => (_def.skills != null && i >= 0 && i < _def.skills.Length) ? _def.skills[i] : null;

    // ── 슬롯 해석 (설계도 22 §4-1) ─────────────────────────────────────────

    /// <summary>현재 폼 id. 폼이 없는 캐릭터는 빈 문자열.</summary>
    public string CurrentFormId => _formId;

    /// <summary>
    /// 인덱스 i 스킬의 슬롯(0~6). SkillDef.slot이 -1(레거시)이면 i+1.
    /// 스킬이 없으면 -1.
    /// </summary>
    public int SlotOf(int i)
    {
        var s = SkillAt(i);
        if (s == null) return -1;
        return s.slot >= 0 ? s.slot : SkillSlots.LegacySlot(i);
    }

    /// <summary>
    /// 슬롯 s를 채우는 스킬의 배열 인덱스. 현재 폼과 맞는(formReq가 비었거나 같은) 첫 스킬.
    /// 없으면 -1. 이후 CastSkill/CanCast 등 인덱스 기반 API에 그대로 넘긴다.
    /// </summary>
    public int IndexForSlot(int slot)
    {
        if (_def.skills == null) return -1;
        for (int i = 0; i < _def.skills.Length; i++)
        {
            var s = _def.skills[i];
            if (s == null) continue;
            if (s.skillType != SkillKind.Active) continue;   // 폼 전환(Z/X)·패시브는 슬롯 키로 발동하지 않는다
            if (SlotOf(i) != slot) continue;
            if (!FormReqMatches(s.formReq)) continue;
            return i;
        }
        return -1;
    }

    /// <summary>formReq가 비었거나, 쉼표로 구분한 폼 중 현재 폼이 있으면 true.</summary>
    public bool FormReqMatches(string req)
    {
        if (string.IsNullOrEmpty(req)) return true;
        foreach (var part in req.Split(','))
            if (part.Trim() == _formId) return true;
        return false;
    }

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

        if (skill.startUnlocked) return true;                       // 처음부터 사용 가능 (설계도 18 §8-2)

        bool hasIds = _owned.unlockedSkillIds != null && _owned.unlockedSkillIds.Count > 0;
        if (_hasStartUnlocked)                                      // 새 모델: 인덱스 0 자동 해금 없음
            return hasIds && _owned.unlockedSkillIds.Contains(skill.id);

        if (!hasIds) return i == 0;                                 // 레거시: 인덱스 0만 기본
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

    public int MaxHp => Stats().hp;
    public int MaxMp => Stats().mp;
    /// <summary>현재 레벨 공격력 × 상태 효과 배율 (없으면 1.0).</summary>
    public int Atk   => (int)(Stats().atk * StatMult(StatKind.Atk));
    public int Def   => (int)(Stats().def * StatMult(StatKind.Def));

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

    /// <summary>
    /// 상태 효과 진행. isActive = 현재 활성 캐릭터인지(버프는 활성일 때만 흐름).
    /// 도트 피해는 이 캐릭터에게 적용된다. 효과 수가 바뀌면 OnChanged("buff").
    /// </summary>
    public void TickStatus(float dt, bool isActive)
    {
        _sinceHit += dt;
        _passives.Tick(dt, isActive);
        foreach (var c in _counters.Values) c.Tick(dt, isActive);
        SyncCounterMilestones();
        int before = Status.Count;
        Status.Tick(dt, isActive, d => Damage(d));
        if (Status.Count != before) OnChanged?.Invoke("buff");
    }

    /// <summary>활성 캐릭터 한 명만 쓰는 호환용 틱 (= TickStatus(dt, true)).</summary>
    public void TickBuffs(float dt) => TickStatus(dt, true);

    /// <summary>상태 효과 부여. ownerActive=false이면 이로운 효과는 예약된다.</summary>
    public void ApplyStatus(StatusEffect e, bool ownerActive)
    {
        if (IsSlowedBy(e) && IsSlowImmune) return;   // 둔화 면역: 이동속도 감소 디버프만 무시
        Status.Apply(e, ownerActive);
        OnChanged?.Invoke("buff");
        if (!e.IsBeneficial) _passives.NotifySelfDebuffed();
    }

    /// <summary>둔화 면역 패시브가 활성인지(설계도 18 §8-5 G3).</summary>
    public bool IsSlowImmune => _passives.IsSlowImmune;

    static bool IsSlowedBy(StatusEffect e)
        => e.Kind == StatusKind.StatMod && e.Stat == StatKind.MoveSpeed && !e.IsBeneficial && e.Value < 1f;

    /// <summary>해로운 상태 효과 해제.</summary>
    public void Cleanse()
    {
        Status.Cleanse();
        OnChanged?.Invoke("buff");
    }

    /// <summary>활성이 되는 순간 호출 — 예약 버프 시작.</summary>
    public void OnSwitchIn()
    {
        _auto.OnSwitchIn();   // 자동 전환 카운터 초기화 (복귀 타이머는 유지)
        Status.OnSwitchIn();
        OnChanged?.Invoke("buff");
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

    /// <summary>
    /// MP 자연회복 1틱 (기절 상태면 무시). 회복량 = max(1, round(MaxMp × 초당비율 × 틱초)).
    /// 기존 PlayerSkills의 활성 캐릭터 전용 회복식과 동일 — 파티 전원(대기 포함)에 쓰도록 이동.
    /// </summary>
    public void RegenMpTick(float percentPerSecond, float tickSeconds)
    {
        if (IsDowned) return;
        int amt = Math.Max(1, Mathf.RoundToInt(MaxMp * percentPerSecond * tickSeconds));
        RecoverMp(amt);
    }

    /// <summary>공격력 버프 적용 (같은 버프는 더 강한 수치·더 긴 시간으로 갱신). 활성 캐릭터 자신용.</summary>
    public void ApplyAtkBuff(float multiplier, float duration)
    {
        Status.Apply(new StatusEffect
        {
            Id = LegacyAtkBuffId, Kind = StatusKind.StatMod, Stat = StatKind.Atk,
            Value = multiplier, Duration = duration, IsBeneficial = true,
        }, ownerActive: true);
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
        StartCooldown(i, SkillScaling.EffectiveCooldown(skill, SkillLevel(i)));
        AddProficiency(i);   // 시전 성공 시 숙련도 +1
        _recent.Record(skill.id, Now);
        FireCounters(CounterTrigger.OnSkillCast);
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
        if (_hp <= 0)
        {
            Status.Clear();   // 기절 시 모든 상태(예약 버프 포함) 소멸
            _passives.Clear();
            if (HasForms && _formId != DefaultFormId) ChangeForm(DefaultFormId, FormChangeCause.Revert);
        }
        else _passives.NotifyHp((float)_hp / MaxHp);   // 임계 통과 판정(영벡터)
        OnChanged?.Invoke("hp");
    }

    /// <summary>
    /// 적의 공격 등 외부 피해 수신: 받는 피해 배율 → 보호막 흡수 → HP 감소. 실제 HP 감소량 반환.
    /// </summary>
    public int TakeHit(int rawDamage, ISkillTarget attacker = null)
    {
        _sinceHit = 0f;
        int dmg = (int)Math.Round(rawDamage * StatMult(StatKind.DamageTaken));
        if (dmg < 0) dmg = 0;
        int taken = dmg;                  // 보호막 흡수 전 피해(패시브 반사 등의 기준)
        dmg = Status.Absorb(dmg);
        int before = _hp;
        if (dmg > 0) Damage(dmg);
        else OnChanged?.Invoke("buff");   // 보호막만 소모된 경우 표시 갱신
        int dealt = before - _hp;
        OnCombatActivity?.Invoke();
        _passives.NotifyHitTaken(new HitInfo { attacker = attacker, damage = taken });
        FireCounters(CounterTrigger.OnHitTaken);
        return dealt;
    }

    public void Heal(int amount)
    {
        _hp = Math.Min(MaxHp, _hp + amount);
        _passives.NotifyHp((float)_hp / MaxHp);
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
        string prevForm = _formId;
        _formId      = DefaultFormId;
        _revertTimer = -1f;
        _auto.OnSwitchIn();
        var s = Stats();
        _hp = s.hp;
        _mp = s.mp;

        // 쿨다운·버프 초기화
        for (int i = 0; i < _cooldowns.Length; i++) _cooldowns[i] = 0f;
        Status.Clear();
        _passives.Clear();
        ClearCounters();

        if (prevForm != _formId) OnFormChanged?.Invoke(prevForm, _formId, FormChangeCause.Revert);
        OnChanged?.Invoke("restore");
    }

    // ── 폼 전환 (설계도 22 §4-6-1) ─────────────────────────────────────────

    /// <summary>폼이 바뀌었을 때(이전, 새, 원인).</summary>
    public event Action<string, string, FormChangeCause> OnFormChanged;

    /// <summary>
    /// 폼 변경. HP·MP는 비율로 이월, 쿨타임·상태 효과는 유지. 같은 폼·없는 폼이면 false.
    /// 복귀 타이머는 초기화된다(수동 전환 스킬이 필요하면 직후에 다시 건다).
    /// </summary>
    public bool ChangeForm(string toForm, FormChangeCause cause)
    {
        if (FindForm(toForm) == null || toForm == _formId) return false;

        string from = _formId;
        int oldMaxHp = MaxHp, oldMaxMp = MaxMp;
        _formId = toForm;
        _hp = FormMath.CarryOver(_hp, oldMaxHp, MaxHp);
        _mp = FormMath.CarryOver(_mp, oldMaxMp, MaxMp);
        _revertTimer = -1f;
        _revertTo    = null;
        _auto.NotifyFormChanged();

        OnFormChanged?.Invoke(from, toForm, cause);
        OnChanged?.Invoke("form");
        _passives.NotifyFormChange(from, toForm);
        return true;
    }

    /// <summary>
    /// 전환 키(0=Z, 1=X)로 수동 폼 전환 시도. 스턴·기절 중 불가, 해금·쿨타임·mp 필요
    /// (재입력 복귀는 mp 무소모, 쿨타임은 적용). 성공 시 사용한 스킬 인덱스, 실패 시 -1.
    /// </summary>
    public int TryFormChange(int keyIndex, bool isMoving)
    {
        if (IsDowned || Status.IsStunned || !HasForms || _def.skills == null) return -1;

        var ctx = new FormContext { secondsSinceHitTaken = _sinceHit, isMoving = isMoving };
        for (int i = 0; i < _def.skills.Length; i++)
        {
            var s = _def.skills[i];
            if (s == null || s.skillType != SkillKind.FormChange || s.formChange == null) continue;
            if (s.formChange.keyIndex != keyIndex) continue;
            if (!IsUnlocked(i) || !IsReady(i)) continue;

            string to = FormChangeResolver.Resolve(s.formChange, _formId, DefaultFormId, ctx, out bool isRevert);
            if (to == null || FindForm(to) == null) continue;

            int cost = isRevert ? 0 : s.mpCost;
            if (Mp < cost) continue;

            if (cost > 0) UseMp(cost);
            StartCooldown(i, SkillScaling.EffectiveCooldown(s, SkillLevel(i)));
            if (!isRevert) AddProficiency(i);

            ChangeForm(to, isRevert ? FormChangeCause.Revert : FormChangeCause.Manual);
            if (!isRevert) StartRevertTimerIfNeeded(s.formChange, to);
            return i;
        }
        return -1;
    }

    void StartRevertTimerIfNeeded(FormChangeSpec spec, string enteredForm)
    {
        if (spec.revertAfterSeconds <= 0f) return;
        bool starts = spec.autoRevertForms != null && spec.autoRevertForms.Length > 0
            ? Array.IndexOf(spec.autoRevertForms, enteredForm) >= 0
            : enteredForm == spec.toForm;
        if (!starts) return;
        _revertTimer = spec.revertAfterSeconds;
        _revertTo    = string.IsNullOrEmpty(spec.revertToForm) ? DefaultFormId : spec.revertToForm;
    }

    /// <summary>
    /// 활성 캐릭터 전용 틱: 자동 복귀 타이머 + 자동 전환 평가(스턴과 무관). 대기 중에는 호출하지 않는다 → 일시정지.
    /// </summary>
    public void TickForm(float dt, CombatMotionState motion)
    {
        if (IsDowned) return;
        _passives.TickMotion(dt, motion);   // 이동 램프 등 활성 캐릭터 전용 틱
        if (_counterSpecs.Count > 0)
        {
            float gain = StatMult(StatKind.CounterGain);
            foreach (var s in _counterSpecs)
                if ((s.gainOn & CounterTrigger.MovingSecond) != 0)
                    _counters[s.id].AccumulateMoving(dt, motion.isMoving, gain);
        }
        if (!HasForms) return;

        if (_revertTimer > 0f)
        {
            _revertTimer -= dt;
            if (_revertTimer <= 0f)
            {
                string to = _revertTo;
                _revertTimer = -1f;
                ChangeForm(to, FormChangeCause.Revert);
                return;
            }
        }

        if (_auto.HasRules)
        {
            string to = _auto.Evaluate(dt, motion, _formId);
            if (to != null) ChangeForm(to, FormChangeCause.Auto);
        }
    }

    /// <summary>적 타격 1회 등록(자동 폼 규칙의 연속 타격 카운터). attrs==null이면 연속이 끊긴다. P5에서 연결.</summary>
    public void RegisterHit(IEnemyAttributes attrs) => _auto.RegisterHit(attrs);

    /// <summary>전투 구역 진입·이탈·씬 전환 시: 상태·스택·폼 초기화. HP·MP·쿨타임은 유지 (설계도 §4-12).</summary>
    public void ResetCombatState()
    {
        Status.Clear();
        _passives.Clear();
        ClearCounters();
        _auto.OnSwitchIn();
        _revertTimer = -1f;
        if (HasForms && _formId != DefaultFormId) ChangeForm(DefaultFormId, FormChangeCause.Revert);
        OnChanged?.Invoke("buff");
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
            var s = Stats();
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

        var s = Stats();
        _hp = s.hp;
        _mp = s.mp;

        OnChanged?.Invoke("levelup");
        return true;
    }

    // ── 내부 유틸 ─────────────────────────────────────────────────────────

    static int ComputeNextExp(int level) => StatGrowth.NextExp(level);
}

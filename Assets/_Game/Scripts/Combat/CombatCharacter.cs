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

    public CombatCharacter(CharacterDef def, OwnedCharacter owned)
    {
        _def     = def;
        _owned   = owned;
        _nextExp = ComputeNextExp(_owned.level);

        // 세션 시작 시 HP/MP 풀
        var s = StatGrowth.ComputeStats(_def, _owned.level);
        _hp = s.hp;
        _mp = s.mp;
    }

    // ── 식별 정보 ──────────────────────────────────────────────────────────

    public Continent Element     => _def.continent;
    public string    DisplayName => _def.nameKo;
    public Color     TintColor   => _def.portraitColor;
    public int       Level       => _owned.level;
    public string    Id          => _def.id;

    // ── 스탯 (현재 레벨 기준, 레벨업 시 재계산) ───────────────────────────

    public int MaxHp => StatGrowth.ComputeStats(_def, _owned.level).hp;
    public int MaxMp => StatGrowth.ComputeStats(_def, _owned.level).mp;
    public int Atk   => StatGrowth.ComputeStats(_def, _owned.level).atk;
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

    /// <summary>HP/MP를 현재 레벨 기준 최대치로 완전 회복.</summary>
    public void RestoreFull()
    {
        var s = StatGrowth.ComputeStats(_def, _owned.level);
        _hp = s.hp;
        _mp = s.mp;
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

    // ── 내부 유틸 ─────────────────────────────────────────────────────────

    /// <summary>주어진 레벨에서 다음 레벨까지 필요한 EXP 임계값. PlayerStats와 동일 공식.</summary>
    static int ComputeNextExp(int level)
        => (int)Math.Floor(100.0 * Math.Pow(1.45, level - 1));
}

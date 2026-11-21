using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 22 P2 — Stage B: CombatCharacter·Party 와 StatusHolder 통합.</summary>
public class CombatP2IntegrationTests
{
    static CombatCharacter MakeChar(string name = "c")
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = name; def.nameKo = name; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.skills = new SkillDef[0];
        def.baseStats = new CharacterStats { hp = 200, atk = 100, def = 10, spd = 1, mp = 50 };
        return new CombatCharacter(def, new OwnedCharacter { id = name, level = 1, exp = 0 });
    }

    static StatusEffect AtkBuff(float mult, float dur)
        => new StatusEffect { Id = "atkb", Kind = StatusKind.StatMod, Stat = StatKind.Atk,
                              Value = mult, Duration = dur, IsBeneficial = true };

    static StatusEffect Dot(float perSec, float dur)
        => new StatusEffect { Id = "burn", Kind = StatusKind.Dot, Value = perSec, Duration = dur };

    // ── CombatCharacter ───────────────────────────────────────────────────

    [Test]
    public void Atk_ReflectsStatusBuff()
    {
        var c = MakeChar();
        int baseAtk = c.Atk;
        c.Status.Apply(AtkBuff(1.5f, 5f), true);
        Assert.AreEqual((int)(baseAtk * 1.5f), c.Atk);
    }

    [Test]
    public void Def_ReflectsStatusDebuff()
    {
        var c = MakeChar();
        int baseDef = c.Def;
        c.Status.Apply(new StatusEffect { Id = "d", Kind = StatusKind.StatMod, Stat = StatKind.Def,
                                          Value = 0.5f, Duration = 5f }, true);
        Assert.AreEqual((int)(baseDef * 0.5f), c.Def);
    }

    [Test]
    public void TickStatus_BuffOnBench_StartsAfterSwitchIn()
    {
        var c = MakeChar();
        int baseAtk = c.Atk;
        c.Status.Apply(AtkBuff(2f, 3f), false);
        c.TickStatus(10f, false);
        Assert.AreEqual(baseAtk, c.Atk, "예약 중에는 효과 없음");
        c.OnSwitchIn();
        Assert.AreEqual(baseAtk * 2, c.Atk);
    }

    [Test]
    public void TickStatus_Dot_DamagesCharacter()
    {
        var c = MakeChar();
        int hp0 = c.Hp;
        c.Status.Apply(Dot(10f, 5f), false);
        c.TickStatus(2f, false);
        Assert.AreEqual(hp0 - 20, c.Hp);
    }

    [Test]
    public void TickStatus_DotKillsCharacter_ClearsStatuses()
    {
        var c = MakeChar();
        c.Status.Apply(AtkBuff(2f, 30f), true);
        c.Status.Apply(Dot(1000f, 5f), true);
        c.TickStatus(1f, true);
        Assert.IsTrue(c.IsDowned);
        Assert.AreEqual(0, c.Status.Count, "기절 시 모든 상태 소멸");
    }

    [Test]
    public void Damage_Downing_ClearsStatuses()
    {
        var c = MakeChar();
        c.Status.Apply(AtkBuff(2f, 30f), false);
        c.Damage(c.MaxHp);
        Assert.AreEqual(0, c.Status.Count, "예약 버프도 기절 시 소멸");
    }

    [Test]
    public void RestoreFull_ClearsStatuses()
    {
        var c = MakeChar();
        c.Status.Apply(AtkBuff(2f, 30f), true);
        c.RestoreFull();
        Assert.AreEqual(0, c.Status.Count);
    }

    [Test]
    public void TakeHit_AppliesDamageTakenMultiplier()
    {
        var c = MakeChar();
        c.Status.Apply(new StatusEffect { Id = "dr", Kind = StatusKind.StatMod, Stat = StatKind.DamageTaken,
                                          Value = 0.5f, Duration = 5f, IsBeneficial = true }, true);
        int hp0 = c.Hp;
        c.TakeHit(40);
        Assert.AreEqual(hp0 - 20, c.Hp);
    }

    [Test]
    public void TakeHit_ShieldAbsorbsFirst()
    {
        var c = MakeChar();
        c.Status.Apply(new StatusEffect { Id = "sh", Kind = StatusKind.Shield, Value = 30f,
                                          Duration = 5f, IsBeneficial = true }, true);
        int hp0 = c.Hp;
        c.TakeHit(50);
        Assert.AreEqual(hp0 - 20, c.Hp, "보호막 30 흡수 → 20만 HP 감소");
    }

    [Test]
    public void TakeHit_PlainDamage_NoStatus()
    {
        var c = MakeChar();
        int hp0 = c.Hp;
        c.TakeHit(15);
        Assert.AreEqual(hp0 - 15, c.Hp);
    }

    // ── Party ─────────────────────────────────────────────────────────────

    [Test]
    public void Party_Tick_ActiveBuffRuns_BenchActiveBuffPaused()
    {
        var a = MakeChar("a"); var b = MakeChar("b");
        var party = new Party(new[] { a, b });
        a.Status.Apply(AtkBuff(2f, 3f), true);
        b.Status.Apply(AtkBuff(2f, 3f), true);       // b는 이미 시작된 버프(비활성이라 일시정지되어야 함)
        party.Tick(5f);
        Assert.AreEqual(0, a.Status.Count, "활성 a의 버프는 만료");
        Assert.AreEqual(1, b.Status.Count, "대기 b의 버프는 일시정지");
    }

    [Test]
    public void Party_SwitchTo_ActivatesPendingBuffImmediately()
    {
        var a = MakeChar("a"); var b = MakeChar("b");
        var party = new Party(new[] { a, b });
        int baseAtk = b.Atk;
        b.Status.Apply(AtkBuff(2f, 3f), false);
        Assert.AreEqual(baseAtk, b.Atk);
        party.SwitchTo(1);
        Assert.AreEqual(baseAtk * 2, b.Atk, "전환 즉시 버프 시작");
    }

    [Test]
    public void Party_AutoSwitchToNext_ActivatesPendingBuff()
    {
        var a = MakeChar("a"); var b = MakeChar("b");
        var party = new Party(new[] { a, b });
        int baseAtk = b.Atk;
        b.Status.Apply(AtkBuff(2f, 3f), false);
        a.Damage(a.MaxHp);
        party.AutoSwitchToNext();
        Assert.AreEqual(baseAtk * 2, b.Atk);
    }

    [Test]
    public void Party_Tick_BenchDotKeepsRunning()
    {
        var a = MakeChar("a"); var b = MakeChar("b");
        var party = new Party(new[] { a, b });
        int hp0 = b.Hp;
        b.Status.Apply(Dot(10f, 5f), false);
        party.Tick(1f);
        Assert.AreEqual(hp0 - 10, b.Hp, "해로운 효과는 대기 중에도 실시간");
    }

    [Test]
    public void Party_RestoreAll_ClearsStatuses()
    {
        var a = MakeChar("a"); var b = MakeChar("b");
        var party = new Party(new[] { a, b });
        b.Status.Apply(AtkBuff(2f, 30f), false);
        party.RestoreAll();
        Assert.AreEqual(0, b.Status.Count);
    }
}

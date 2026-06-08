using NUnit.Framework;
using UnityEngine;

/// <summary>스킬·MP·쿨다운·버프 순수 로직 EditMode 테스트.</summary>
public class SkillTests
{
    // ════════════════════════════════════════════════════════════════════════
    // 헬퍼
    // ════════════════════════════════════════════════════════════════════════

    static CharacterDef MakeDef(int mp = 100, SkillDef[] skills = null)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id        = "test_skill_char";
        def.nameKo    = "테스트";
        def.rarity    = Rarity.N;
        def.continent = Continent.Physics;
        def.skills    = skills;
        def.baseStats = new CharacterStats { hp = 200, atk = 50, def = 10, spd = 10, mp = mp };
        return def;
    }

    static OwnedCharacter MakeOwned() => new OwnedCharacter { id = "test", level = 1, exp = 0 };

    static CombatCharacter MakeChar(int mp = 100, SkillDef[] skills = null)
        => new CombatCharacter(MakeDef(mp, skills), MakeOwned());

    static SkillDef MakeStrike(int mpCost = 20, float cd = 1f, float mult = 1.5f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id              = "test_strike";
        s.nameKo          = "테스트 강타";
        s.effectKind      = SkillEffectKind.Strike;
        s.mpCost          = mpCost;
        s.cooldown        = cd;
        s.damageMultiplier = mult;
        s.range           = 3f;
        s.halfAngle       = 60f;
        return s;
    }

    static SkillDef MakeHealBuff(int mpCost = 30, float cd = 5f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id               = "test_healbuff";
        s.nameKo           = "테스트 회복";
        s.effectKind       = SkillEffectKind.HealBuff;
        s.mpCost           = mpCost;
        s.cooldown         = cd;
        s.healPercent      = 0.20f;
        s.buffAtkMultiplier = 1.50f;
        s.buffDuration     = 4f;
        return s;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SkillCount · SkillAt
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void SkillCount_NoSkills_Zero()
    {
        var ch = MakeChar(skills: null);
        Assert.AreEqual(0, ch.SkillCount);
    }

    [Test]
    public void SkillAt_OutOfRange_Null()
    {
        var skill = MakeStrike();
        var ch    = MakeChar(skills: new[] { skill });
        Assert.IsNull(ch.SkillAt(-1));
        Assert.IsNull(ch.SkillAt(1));
        Assert.AreSame(skill, ch.SkillAt(0));
    }

    // ════════════════════════════════════════════════════════════════════════
    // CanCast · CastSkill — MP 부족
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CastSkill_InsufficientMp_ReturnsNull()
    {
        var skill = MakeStrike(mpCost: 999);           // MP 한참 부족
        var ch    = MakeChar(mp: 50, skills: new[] { skill });

        var result = ch.CastSkill(0);
        Assert.IsNull(result, "MP 부족 시 CastSkill은 null이어야 한다");
    }

    [Test]
    public void CastSkill_InsufficientMp_MpUnchanged()
    {
        var skill = MakeStrike(mpCost: 999);
        var ch    = MakeChar(mp: 50, skills: new[] { skill });
        int before = ch.Mp;

        ch.CastSkill(0);
        Assert.AreEqual(before, ch.Mp, "실패 시 MP가 변해서는 안 된다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // CanCast · CastSkill — 성공
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CastSkill_Success_ReturnsDef()
    {
        var skill = MakeStrike(mpCost: 20);
        var ch    = MakeChar(mp: 100, skills: new[] { skill });

        var result = ch.CastSkill(0);
        Assert.AreSame(skill, result, "성공 시 CastSkill은 해당 SkillDef를 반환해야 한다");
    }

    [Test]
    public void CastSkill_Success_MpDecreased()
    {
        var skill = MakeStrike(mpCost: 20);
        var ch    = MakeChar(mp: 100, skills: new[] { skill });

        ch.CastSkill(0);
        Assert.AreEqual(80, ch.Mp, "성공 시 mpCost만큼 MP가 줄어야 한다");
    }

    [Test]
    public void CastSkill_Success_CooldownStarted()
    {
        var skill = MakeStrike(mpCost: 20, cd: 3f);
        var ch    = MakeChar(mp: 100, skills: new[] { skill });

        ch.CastSkill(0);
        Assert.Greater(ch.CooldownRemaining(0), 0f, "성공 후 쿨타임이 시작돼야 한다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 쿨다운 중 재발동 불가
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CastSkill_DuringCooldown_ReturnsNull()
    {
        var skill = MakeStrike(mpCost: 10, cd: 5f);
        var ch    = MakeChar(mp: 200, skills: new[] { skill });

        ch.CastSkill(0); // 첫 발동 → 쿨다운 시작
        var second = ch.CastSkill(0);
        Assert.IsNull(second, "쿨다운 중 재발동은 null이어야 한다");
    }

    [Test]
    public void TickCooldowns_CooldownDecreases()
    {
        var skill = MakeStrike(mpCost: 10, cd: 4f);
        var ch    = MakeChar(mp: 100, skills: new[] { skill });

        ch.CastSkill(0);
        ch.TickCooldowns(2f); // 2초 경과

        float remaining = ch.CooldownRemaining(0);
        Assert.AreEqual(2f, remaining, 0.01f, "2초 틱 후 남은 쿨다운은 약 2초여야 한다");
    }

    [Test]
    public void TickCooldowns_AfterExpiry_IsReady()
    {
        var skill = MakeStrike(mpCost: 10, cd: 2f);
        var ch    = MakeChar(mp: 100, skills: new[] { skill });

        ch.CastSkill(0);
        ch.TickCooldowns(3f); // 쿨다운(2초) 초과

        Assert.IsTrue(ch.IsReady(0), "쿨다운 만료 후 IsReady는 true여야 한다");
    }

    [Test]
    public void CastSkill_AfterCooldownExpires_Succeeds()
    {
        var skill = MakeStrike(mpCost: 10, cd: 1f);
        var ch    = MakeChar(mp: 200, skills: new[] { skill });

        ch.CastSkill(0);
        ch.TickCooldowns(2f); // 쿨다운 만료

        var result = ch.CastSkill(0);
        Assert.IsNotNull(result, "쿨다운 만료 후 재발동은 성공해야 한다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 기절 중 발동 불가
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CastSkill_WhenDowned_ReturnsNull()
    {
        var skill = MakeStrike(mpCost: 10);
        var ch    = MakeChar(mp: 100, skills: new[] { skill });

        ch.Damage(ch.MaxHp * 10); // 강제 기절
        Assert.IsTrue(ch.IsDowned);
        Assert.IsNull(ch.CastSkill(0), "기절 중에는 스킬 발동 불가");
    }

    // ════════════════════════════════════════════════════════════════════════
    // RecoverMp
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void RecoverMp_IncreasesMP()
    {
        var skill = MakeStrike(mpCost: 30);
        var ch    = MakeChar(mp: 100, skills: new[] { skill });

        ch.CastSkill(0); // MP 30 소모
        ch.RecoverMp(15);
        Assert.AreEqual(85, ch.Mp);
    }

    [Test]
    public void RecoverMp_ClampsToMaxMp()
    {
        var ch = MakeChar(mp: 100, skills: new SkillDef[0]);
        ch.RecoverMp(999);
        Assert.AreEqual(ch.MaxMp, ch.Mp, "MaxMp를 초과해서는 안 된다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // ApplyAtkBuff · TickBuffs
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void ApplyAtkBuff_IncreasesAtk()
    {
        var ch   = MakeChar(mp: 100);
        int base_ = ch.Atk;

        ch.ApplyAtkBuff(2.0f, 5f);
        Assert.AreEqual(base_ * 2, ch.Atk, "버프 적용 후 Atk는 배율이 반영돼야 한다");
    }

    [Test]
    public void TickBuffs_AfterExpiry_AtkReturnsToBase()
    {
        var ch   = MakeChar(mp: 100);
        int base_ = ch.Atk;

        ch.ApplyAtkBuff(2.0f, 3f);
        ch.TickBuffs(5f); // 버프 만료

        Assert.AreEqual(base_, ch.Atk, "버프 만료 후 Atk는 원래대로 돌아와야 한다");
    }

    [Test]
    public void TickBuffs_BeforeExpiry_BuffActive()
    {
        var ch = MakeChar(mp: 100);
        ch.ApplyAtkBuff(1.5f, 5f);
        ch.TickBuffs(2f); // 아직 3초 남음

        int base_ = MakeChar(mp: 100).Atk; // 버프 없는 캐릭터 기준 Atk
        Assert.Greater(ch.Atk, base_, "버프 유지 중에는 Atk가 높아야 한다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // RestoreFull — 쿨다운·버프 초기화
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void RestoreFull_ClearsBuffAndCooldown()
    {
        var skill = MakeStrike(mpCost: 10, cd: 10f);
        var ch    = MakeChar(mp: 100, skills: new[] { skill });

        ch.CastSkill(0);              // 쿨다운 시작
        ch.ApplyAtkBuff(2f, 10f);    // 버프 적용
        ch.RestoreFull();

        Assert.IsTrue(ch.IsReady(0),  "RestoreFull 후 쿨다운은 초기화돼야 한다");
        Assert.AreEqual(ch.MaxMp, ch.Mp, "MP가 풀 회복돼야 한다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 여러 스킬 슬롯 독립 쿨다운
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void MultipleSlots_IndependentCooldowns()
    {
        var s0 = MakeStrike(mpCost: 10, cd: 5f);
        var s1 = MakeStrike(mpCost: 10, cd: 2f);
        var ch = MakeChar(mp: 200, skills: new[] { s0, s1 });

        ch.CastSkill(0);
        ch.CastSkill(1);
        ch.TickCooldowns(3f);

        Assert.IsFalse(ch.IsReady(0), "슬롯0 (5초 CD)은 아직 쿨다운 중이어야 한다");
        Assert.IsTrue(ch.IsReady(1),  "슬롯1 (2초 CD)은 쿨다운이 만료돼야 한다");
    }
}

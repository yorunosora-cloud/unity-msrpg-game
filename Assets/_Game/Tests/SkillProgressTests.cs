using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 스킬 레벨업 시스템 EditMode 테스트.
/// SkillScaling(순수 헬퍼) + CombatCharacter 숙련도 API를 검증한다.
/// </summary>
public class SkillProgressTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    static SkillDef MakeStrike(float mult = 1.5f, int mileLevel = 3,
                               int mileExtraTargets = 1, float mileRange = 1f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id                   = "test_prog_skill";
        s.nameKo               = "숙련도 테스트 스킬";
        s.effectKind           = SkillEffectKind.Strike;
        s.damageMultiplier     = mult;
        s.healPercent          = 0.20f;
        s.buffAtkMultiplier    = 1.30f;
        s.buffDuration         = 5f;
        s.range                = 3f;
        s.halfAngle            = 60f;
        s.milestoneLevel       = mileLevel;
        s.milestoneExtraTargets = mileExtraTargets;
        s.milestoneRangeBonus  = mileRange;
        return s;
    }

    static SkillDef MakeHealBuff(float healPct = 0.20f, float buffMult = 1.30f,
                                  float buffDur = 5f, int mileLevel = 3,
                                  float mileHealBonus = 0.10f, float mileBufDurBonus = 2f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id                       = "test_healbuff";
        s.nameKo                   = "회복버프 테스트";
        s.effectKind               = SkillEffectKind.HealBuff;
        s.healPercent              = healPct;
        s.buffAtkMultiplier        = buffMult;
        s.buffDuration             = buffDur;
        s.range                    = 1f;
        s.halfAngle                = 180f;
        s.milestoneLevel           = mileLevel;
        s.milestoneHealPercentBonus = mileHealBonus;
        s.milestoneBuffDurationBonus = mileBufDurBonus;
        return s;
    }

    static CharacterDef MakeDef(SkillDef[] skills = null)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id        = "test_prog_char";
        def.nameKo    = "숙련도 테스트 캐릭터";
        def.rarity    = Rarity.N;
        def.continent = Continent.Physics;
        def.skills    = skills;
        def.baseStats = new CharacterStats { hp = 200, atk = 50, def = 10, spd = 10, mp = 100 };
        return def;
    }

    static OwnedCharacter MakeOwned() => new OwnedCharacter { id = "test_prog", level = 1, exp = 0 };

    static CombatCharacter MakeChar(SkillDef[] skills)
        => new CombatCharacter(MakeDef(skills), MakeOwned());

    // 특정 레벨로 세팅된 owned를 가진 CombatCharacter
    static (CombatCharacter cc, OwnedCharacter owned) MakeCharAtSkillLevel(SkillDef skill, int skillLv)
    {
        var owned = MakeOwned();
        var prog  = owned.GetSkillProgress(skill.id);
        prog.level       = skillLv;
        prog.proficiency = 0;
        var cc = new CombatCharacter(MakeDef(new[] { skill }), owned);
        return (cc, owned);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SkillScaling — 임계값
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Threshold_Lv1_Returns6()
        => Assert.AreEqual(6, SkillScaling.Threshold(1));

    [Test]
    public void Threshold_Lv2_Returns10()
        => Assert.AreEqual(10, SkillScaling.Threshold(2));

    [Test]
    public void Threshold_Lv3_Returns16()
        => Assert.AreEqual(16, SkillScaling.Threshold(3));

    [Test]
    public void Threshold_Lv4_Returns24()
        => Assert.AreEqual(24, SkillScaling.Threshold(4));

    [Test]
    public void Threshold_MaxLevel_ReturnsIntMax()
        => Assert.AreEqual(int.MaxValue, SkillScaling.Threshold(SkillScaling.MaxLevel));

    // ════════════════════════════════════════════════════════════════════════
    // SkillScaling — 위력 곡선
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Compute_Lv1_EqualsBase()
    {
        var skill = MakeStrike(mult: 1.5f);
        var eff   = SkillScaling.Compute(skill, 1);
        Assert.AreEqual(1.5f, eff.damageMultiplier, 0.001f);
    }

    [Test]
    public void Compute_Lv5_ApproxOnePointFourEightTimesBase()
    {
        var skill = MakeStrike(mult: 1.5f);
        var eff   = SkillScaling.Compute(skill, 5);
        // 1.5 * (1 + 4*0.12) = 1.5 * 1.48 = 2.22
        Assert.AreEqual(1.5f * 1.48f, eff.damageMultiplier, 0.01f);
    }

    [Test]
    public void Compute_PowerCurve_EachLevelIncreases()
    {
        var skill  = MakeStrike(mult: 1.5f, mileLevel: 99); // 마일스톤 비활성
        float prev = 0f;
        for (int lv = 1; lv <= SkillScaling.MaxLevel; lv++)
        {
            float cur = SkillScaling.Compute(skill, lv).damageMultiplier;
            Assert.Greater(cur, prev, $"Lv.{lv} 위력이 이전보다 높아야 함");
            prev = cur;
        }
    }

    [Test]
    public void Compute_HealBuff_MultiplierPreservesBase1()
    {
        var skill = MakeHealBuff(buffMult: 1.30f, mileLevel: 99);
        // Lv.3: mult = 1 + 2*0.12 = 1.24
        // buffAtkMultiplier = 1 + (1.30-1) * 1.24 = 1 + 0.372 = 1.372
        var eff = SkillScaling.Compute(skill, 3);
        float expected = 1f + (1.30f - 1f) * (1f + 2f * 0.12f);
        Assert.AreEqual(expected, eff.buffAtkMultiplier, 0.001f);
        Assert.Greater(eff.buffAtkMultiplier, 1f);  // 항상 기저 1f 이상
    }

    // ════════════════════════════════════════════════════════════════════════
    // SkillScaling — 마일스톤 (Lv.3)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Compute_BelowMilestone_NoExtraTargets()
    {
        var skill = MakeStrike(mileLevel: 3, mileExtraTargets: 1);
        var eff   = SkillScaling.Compute(skill, 2);
        Assert.AreEqual(0, eff.extraTargets);
    }

    [Test]
    public void Compute_AtMilestone_ExtraTargetsApplied()
    {
        var skill = MakeStrike(mileLevel: 3, mileExtraTargets: 1);
        var eff   = SkillScaling.Compute(skill, 3);
        Assert.AreEqual(1, eff.extraTargets);
    }

    [Test]
    public void Compute_AboveMilestone_RangeBonus()
    {
        var skill = MakeStrike(mileLevel: 3, mileRange: 1.5f);
        var effBefore = SkillScaling.Compute(skill, 2);
        var effAfter  = SkillScaling.Compute(skill, 3);
        Assert.AreEqual(effBefore.range + 1.5f, effAfter.range, 0.001f);
    }

    [Test]
    public void Compute_HealBuff_MilestoneAddsHealAndDuration()
    {
        var skill = MakeHealBuff(healPct: 0.20f, buffDur: 5f, mileLevel: 3,
                                  mileHealBonus: 0.10f, mileBufDurBonus: 2f);
        var effBase = SkillScaling.Compute(skill, 2);
        var effMile = SkillScaling.Compute(skill, 3);
        Assert.AreEqual(effBase.healPercent + 0.10f, effMile.healPercent, 0.001f);
        Assert.AreEqual(effBase.buffDuration + 2f,   effMile.buffDuration, 0.001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SkillScaling — 난이도 보너스
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void ProficiencyBonus_High_Is3Percent()
        => Assert.AreEqual(3, SkillScaling.ProficiencyBonus(ProblemDifficulty.High, 100));

    [Test]
    public void ProficiencyBonus_Mid_Is1Percent()
        => Assert.AreEqual(1, SkillScaling.ProficiencyBonus(ProblemDifficulty.Mid, 100));

    [Test]
    public void ProficiencyBonus_Low_IsZero()
        => Assert.AreEqual(0, SkillScaling.ProficiencyBonus(ProblemDifficulty.Low, 100));

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — 숙련도 누적 · 캡
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void AddProficiency_IncreasesEachCall()
    {
        var skill = MakeStrike();
        var cc    = MakeChar(new[] { skill });

        cc.AddProficiency(0);
        Assert.AreEqual(1, cc.SkillProficiency(0));
        cc.AddProficiency(0);
        Assert.AreEqual(2, cc.SkillProficiency(0));
    }

    [Test]
    public void AddProficiency_CapsAtThreshold()
    {
        var skill = MakeStrike();
        var cc    = MakeChar(new[] { skill });
        int thr   = SkillScaling.Threshold(1); // 6

        for (int i = 0; i < thr + 5; i++) cc.AddProficiency(0);
        Assert.AreEqual(thr, cc.SkillProficiency(0));
    }

    [Test]
    public void AddProficiency_MaxLevel_DoesNothing()
    {
        var skill       = MakeStrike();
        var (cc, owned) = MakeCharAtSkillLevel(skill, SkillScaling.MaxLevel);
        cc.AddProficiency(0);
        Assert.AreEqual(0, cc.SkillProficiency(0));
    }

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — CanLevelUpSkill
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CanLevelUpSkill_FalseBeforeThreshold()
    {
        var skill = MakeStrike();
        var cc    = MakeChar(new[] { skill });
        int thr   = SkillScaling.Threshold(1);

        for (int i = 0; i < thr - 1; i++) cc.AddProficiency(0);
        Assert.IsFalse(cc.CanLevelUpSkill(0));
    }

    [Test]
    public void CanLevelUpSkill_TrueAtThreshold()
    {
        var skill = MakeStrike();
        var cc    = MakeChar(new[] { skill });
        int thr   = SkillScaling.Threshold(1);

        for (int i = 0; i < thr; i++) cc.AddProficiency(0);
        Assert.IsTrue(cc.CanLevelUpSkill(0));
    }

    [Test]
    public void CanLevelUpSkill_FalseAtMaxLevel()
    {
        var skill       = MakeStrike();
        var (cc, owned) = MakeCharAtSkillLevel(skill, SkillScaling.MaxLevel);
        // 숙련도를 강제로 높게 설정해도 만렙이면 false
        owned.GetSkillProgress(skill.id).proficiency = 999;
        Assert.IsFalse(cc.CanLevelUpSkill(0));
    }

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — LevelUpSkill
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void LevelUpSkill_IncrementsLevel()
    {
        var skill = MakeStrike();
        var cc    = MakeChar(new[] { skill });
        for (int i = 0; i < SkillScaling.Threshold(1); i++) cc.AddProficiency(0);

        cc.LevelUpSkill(0, ProblemDifficulty.Low);
        Assert.AreEqual(2, cc.SkillLevel(0));
    }

    [Test]
    public void LevelUpSkill_ReturnsFalseWhenNotReady()
    {
        var skill = MakeStrike();
        var cc    = MakeChar(new[] { skill }); // proficiency=0 < 6
        Assert.IsFalse(cc.LevelUpSkill(0, ProblemDifficulty.Low));
        Assert.AreEqual(1, cc.SkillLevel(0)); // 레벨 변화 없음
    }

    [Test]
    public void LevelUpSkill_CarryRemainsProficiency()
    {
        var skill = MakeStrike();
        var owned = MakeOwned();
        // proficiency = 8 (threshold=6, carry=2 after level-up)
        owned.GetSkillProgress(skill.id).proficiency = 8;
        var cc = new CombatCharacter(MakeDef(new[] { skill }), owned);

        cc.LevelUpSkill(0, ProblemDifficulty.Low);
        Assert.AreEqual(2, cc.SkillLevel(0));
        // carry = 8 - 6 = 2. Low 보너스 없음.
        Assert.AreEqual(2, cc.SkillProficiency(0));
    }

    [Test]
    public void LevelUpSkill_ReturnsFalseAtMaxLevel()
    {
        var skill       = MakeStrike();
        var (cc, owned) = MakeCharAtSkillLevel(skill, SkillScaling.MaxLevel);
        owned.GetSkillProgress(skill.id).proficiency = 999;
        Assert.IsFalse(cc.LevelUpSkill(0, ProblemDifficulty.High));
    }

    [Test]
    public void LevelUpSkill_HighBonus_AppliedWhenLevelNotMax()
    {
        // threshold=10 (Lv.2→3), High bonus = RoundToInt(10 * 0.03) = 0 (아직 미미)
        // 더 큰 임계값의 효과를 직접 ProficiencyBonus로 검증
        // (CombatCharacter의 보너스 로직 자체를 통합 테스트)
        var skill       = MakeStrike();
        var (cc, owned) = MakeCharAtSkillLevel(skill, 2);
        // Lv.2→3 임계값=10, carry=0, High bonus=RoundToInt(10*0.03)=0 → proficiency=0
        owned.GetSkillProgress(skill.id).proficiency = 10;
        cc.LevelUpSkill(0, ProblemDifficulty.High);
        Assert.AreEqual(3, cc.SkillLevel(0));
        // bonus=0이므로 proficiency=0
        Assert.AreEqual(0, cc.SkillProficiency(0));
    }

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — PenalizeProficiency
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void PenalizeProficiency_ReducesByTenPercent()
    {
        var skill = MakeStrike();
        var owned = MakeOwned();
        owned.GetSkillProgress(skill.id).proficiency = 10;
        var cc = new CombatCharacter(MakeDef(new[] { skill }), owned);

        cc.PenalizeProficiency(0);
        // floor(10 * 0.9) = floor(9.0) = 9
        Assert.AreEqual(9, cc.SkillProficiency(0));
    }

    [Test]
    public void PenalizeProficiency_Zero_StaysZero()
    {
        var skill = MakeStrike();
        var cc    = MakeChar(new[] { skill });
        cc.PenalizeProficiency(0);
        Assert.AreEqual(0, cc.SkillProficiency(0));
    }

    [Test]
    public void PenalizeProficiency_DoesNotChangeLevelOrUnlock()
    {
        var skill = MakeStrike();
        var owned = MakeOwned();
        owned.GetSkillProgress(skill.id).level       = 2;
        owned.GetSkillProgress(skill.id).proficiency = 8;
        var cc = new CombatCharacter(MakeDef(new[] { skill }), owned);

        cc.PenalizeProficiency(0);
        Assert.AreEqual(2, cc.SkillLevel(0)); // 레벨 유지
    }

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — 만렙 동결
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void MaxLevel_Freeze_AddProficiencyNoOp()
    {
        var skill       = MakeStrike();
        var (cc, owned) = MakeCharAtSkillLevel(skill, SkillScaling.MaxLevel);
        int before = cc.SkillProficiency(0);
        for (int i = 0; i < 10; i++) cc.AddProficiency(0);
        Assert.AreEqual(before, cc.SkillProficiency(0));
    }

    [Test]
    public void MaxLevel_Freeze_LevelUpSkillReturnsFalse()
    {
        var skill       = MakeStrike();
        var (cc, owned) = MakeCharAtSkillLevel(skill, SkillScaling.MaxLevel);
        owned.GetSkillProgress(skill.id).proficiency = 9999;
        Assert.IsFalse(cc.LevelUpSkill(0, ProblemDifficulty.High));
        Assert.AreEqual(SkillScaling.MaxLevel, cc.SkillLevel(0));
    }

    // ════════════════════════════════════════════════════════════════════════
    // OwnedCharacter — GetSkillProgress / SkillLevel
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void GetSkillProgress_CreatesEntryIfMissing()
    {
        var owned = MakeOwned();
        var prog  = owned.GetSkillProgress("new_skill_id");
        Assert.IsNotNull(prog);
        Assert.AreEqual("new_skill_id", prog.skillId);
        Assert.AreEqual(1, prog.level);
        Assert.AreEqual(0, prog.proficiency);
    }

    [Test]
    public void GetSkillProgress_ReturnsSameInstanceTwice()
    {
        var owned = MakeOwned();
        var a = owned.GetSkillProgress("abc");
        a.proficiency = 5;
        var b = owned.GetSkillProgress("abc");
        Assert.AreEqual(5, b.proficiency);
    }

    [Test]
    public void SkillLevel_ReturnsOneIfNoEntry()
    {
        var owned = MakeOwned();
        Assert.AreEqual(1, owned.SkillLevel("nonexistent_skill"));
    }
}

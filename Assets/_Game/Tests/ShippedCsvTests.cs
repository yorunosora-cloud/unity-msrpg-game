using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 설계도 18-B — Stage D: 저장소에 실제로 들어 있는 CSV(Resources/Data)를 빌더로 돌려
/// 오류가 없고 캐릭터 문서(.md)의 수치와 일치하며 전투에서 실제로 동작하는지 확인한다.
/// 대표 4명: 포르테(힘)·루미에르(빛)·마시오&이네르셔(질량·관성)·크로노(시간).
/// </summary>
public class ShippedCsvTests
{
    static ImportResult _res;

    static ImportResult Load()
    {
        if (_res != null) return _res;
        string dir = Path.Combine(Application.dataPath, "_Game/Resources/Data");
        var sheets = new Dictionary<string, string>();
        foreach (var name in CharacterCsvBuilder.AllSheets)
        {
            string p = Path.Combine(dir, name);
            if (File.Exists(p)) sheets[name] = File.ReadAllText(p, Encoding.UTF8);
        }
        return _res = CharacterCsvBuilder.Build(sheets);
    }

    static CharacterDef Char(string id) => Load().characters.Single(c => c.id == id);
    static SkillDef Skill(string id) => Load().skills.Single(s => s.id == id);

    static CombatCharacter MakeCombat(string id, params string[] unlock)
    {
        var def = Char(id);
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1 });
        foreach (var s in unlock) c.Unlock(s);
        c.TickStatus(0f, true);
        return c;
    }

    // ════════════════════════════════════════════════════════════════════════
    // 전체
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void ShippedCsv_ImportsWithoutErrorsOrWarnings()
    {
        var r = Load();
        Assert.IsFalse(r.report.HasErrors, string.Join("\n", r.report.Errors));
        Assert.AreEqual(0, r.report.Warnings.Count, string.Join("\n", r.report.Warnings));
    }

    [Test]
    public void ShippedCsv_Counts()
    {
        var r = Load();
        Assert.AreEqual(4, r.characters.Count);
        Assert.AreEqual(29, r.skills.Count, "포르테 7 + 루미에르 11 + 마시오&이네르셔 6 + 크로노 5");
        Assert.AreEqual(8, r.tagRules.Length);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 포르테(힘) — 문서 대조
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Forte_CatalogAndSkillsMatchDoc()
    {
        var c = Char("forte_r");
        Assert.AreEqual("포르테", c.nameKo);
        Assert.AreEqual(Rarity.R, c.rarity);
        Assert.AreEqual(CharacterRole.Tanker, c.role);
        Assert.AreEqual(Gender.Male, c.gender);
        Assert.AreEqual("newton-empire", c.country);
        Assert.AreEqual(2975, c.baseStats.hp);
        Assert.AreEqual(9, c.baseStats.atk);
        Assert.AreEqual(26, c.baseStats.def);
        Assert.AreEqual(9, c.baseStats.spd);
        Assert.AreEqual(13, c.baseStats.mp);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }, c.skills.Select(s => s.slot).ToArray());
    }

    [Test]
    public void Forte_Friction_DamagePlusSlow_WithMilestone()
    {
        var s = Skill("forte_s1");
        Assert.AreEqual(5, s.mpCost);
        Assert.AreEqual(7f, s.cooldown);
        Assert.AreEqual(3f, s.range);
        Assert.AreEqual(45f, s.halfAngle);
        var fx = s.effects;
        Assert.AreEqual(SkillEffectType.Damage, fx[0].type);
        Assert.AreEqual(0.8f, fx[0].value, 0.0001f);
        Assert.AreEqual(SkillEffectType.Slow, fx[1].type);
        Assert.AreEqual(0.7f, fx[1].value, 0.0001f, "이동속도 −30%");
        Assert.AreEqual(4f, fx[1].duration);
        Assert.AreEqual(6f, fx[1].milestoneDuration, "Lv.3 지속시간 4→6초");
    }

    [Test]
    public void Forte_ElasticLine_And_GravitationPull()
    {
        var line = Skill("forte_s4");
        Assert.AreEqual(SkillShape.Line, line.shape);
        Assert.AreEqual(5f, line.range);
        Assert.AreEqual(0.6f, line.lineWidth, 0.0001f);
        Assert.AreEqual(2f, line.effects[0].value);
        Assert.AreEqual(2.5f, line.effects[0].milestoneValue, "Lv.3 위력 2.0→2.5");

        var grav = Skill("forte_s5");
        Assert.AreEqual(SkillShape.Circle, grav.shape);
        Assert.AreEqual(4f, grav.radius);
        Assert.IsTrue(grav.effects.Any(e => e.type == SkillEffectType.Pull));
        Assert.AreEqual(1f, grav.milestoneRangeBonus, "Lv.3 반경 4→5");

        var buoy = Skill("forte_s6");
        Assert.AreEqual(SkillEffectType.Stun, buoy.effects[0].type);
        Assert.AreEqual(2f, buoy.effects[0].duration);
        Assert.AreEqual(2.5f, buoy.effects[0].milestoneDuration);
    }

    [Test]
    public void Forte_ActionReaction_FiresKnockbackOnAttacker()
    {
        var c = MakeCombat("forte_r", "forte_s3");
        var fires = new List<PassiveFire>();
        c.PassiveFired += f => fires.Add(f);
        c.TakeHit(10);
        Assert.AreEqual(1, fires.Count);
        Assert.AreEqual(SkillEffectType.Knockback, fires[0].effect.type);
        Assert.AreEqual(TargetScope.Attacker, fires[0].effect.target);
        Assert.AreEqual(2f, fires[0].value);
    }

    [Test]
    public void Forte_OnlyBasicAttackIsUnlockedAtStart()
    {
        var c = MakeCombat("forte_r");
        Assert.IsTrue(c.IsUnlocked(0));
        for (int i = 1; i < c.SkillCount; i++) Assert.IsFalse(c.IsUnlocked(i), $"slot {c.SlotOf(i)}");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 루미에르 — 수동 토글 폼
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Lumiere_FormsAndStatsMatchDoc()
    {
        var c = Char("lumiere_ssr");
        Assert.AreEqual(2, c.forms.Length);
        var p = c.forms.Single(f => f.formId == "Particle");
        var w = c.forms.Single(f => f.formId == "Wave");
        Assert.IsTrue(p.isDefault);
        Assert.AreEqual(CharacterRole.Dealer, p.role);
        Assert.AreEqual(Gender.Male, p.gender);
        Assert.AreEqual(35, p.baseStats.atk);
        Assert.AreEqual(CharacterRole.Supporter, w.role);
        Assert.AreEqual(Gender.Female, w.gender);
        Assert.AreEqual(30, w.baseStats.mp);
        Assert.AreEqual(2000, p.baseStats.hp);
        Assert.AreEqual(2000, w.baseStats.hp, "hp는 폼 공통");
    }

    [Test]
    public void Lumiere_ZKeyTogglesForm_AndSlotResolutionFollowsForm()
    {
        var c = MakeCombat("lumiere_ssr");
        Assert.AreEqual("Particle", c.CurrentFormId);
        Assert.AreEqual("lumiere_s1p", c.SkillAt(c.IndexForSlot(1)).id, "입자성의 R = 광전효과");
        Assert.AreEqual("lumiere_s2p", c.SkillAt(c.IndexForSlot(2)).id);

        Assert.GreaterOrEqual(c.TryFormChange(0, false), 0, "Z = 이중성");
        Assert.AreEqual("Wave", c.CurrentFormId);
        Assert.AreEqual("lumiere_s1w", c.SkillAt(c.IndexForSlot(1)).id, "파동성의 R = 굴절");
        Assert.AreEqual("lumiere_s2w", c.SkillAt(c.IndexForSlot(2)).id);
        Assert.AreEqual(CharacterRole.Supporter, c.CurrentRole);
    }

    [Test]
    public void Lumiere_PhotoelectricEffect_LineDamagePlusOneSecondStun()
    {
        var s = Skill("lumiere_s1p");
        Assert.AreEqual(SkillShape.Line, s.shape);
        Assert.AreEqual(5f, s.range);
        Assert.AreEqual(0.5f, s.lineWidth, 0.0001f);
        Assert.AreEqual("Particle", s.formReq);
        Assert.AreEqual(1.5f, s.effects[0].value);
        Assert.AreEqual(SkillEffectType.Stun, s.effects[1].type);
        Assert.AreEqual(1f, s.effects[1].duration);
        Assert.AreEqual(1.5f, s.effects[1].milestoneDuration, "마비시간 +0.5초");
    }

    [Test]
    public void Lumiere_Interference_HalvesDamageTaken_DoublesDamageDealt_OnlyInWaveForm()
    {
        var c = MakeCombat("lumiere_ssr", "lumiere_s4w", "lumiere_s0");
        Assert.AreEqual(1f, c.StatMult(StatKind.DamageTaken), 0.0001f, "입자성에서는 간섭 없음");
        c.TryFormChange(0, false);
        Assert.AreEqual(0.5f, c.StatMult(StatKind.DamageTaken), 0.0001f);
        Assert.AreEqual(2f, c.HitMultiplier(false), 0.0001f);
    }

    [Test]
    public void Lumiere_Quantization_NeedsStoryFlagCondition_AndConsumesAtFourStacks()
    {
        var s = Skill("lumiere_s4p");
        Assert.AreEqual("StoryFlag:newtonia_quantum_revolution_appeared", s.unlockCond);
        Assert.AreEqual(SkillUnlockConditionType.StoryFlag, SkillUnlockCondition.Parse(s.unlockCond).type);

        var c = MakeCombat("lumiere_ssr", "lumiere_s4p");
        for (int a = 1; a <= 4; a++) c.NotifyHits(new List<HitInfo> { new HitInfo { actionId = a, damage = 5 } }, false);
        Assert.AreEqual(2.5f, c.HitMultiplier(false), 0.0001f, "4스택 → 다음 타격 ×2.5");
    }

    [Test]
    public void Lumiere_Compton_AndDoppler_AreWiredCorrectly()
    {
        var c = MakeCombat("lumiere_ssr", "lumiere_s3p");
        var fires = new List<PassiveFire>();
        c.PassiveFired += f => fires.Add(f);
        c.NotifyHits(new List<HitInfo> { new HitInfo { actionId = 1, damage = 5 }, new HitInfo { actionId = 1, damage = 5 } }, false);
        Assert.AreEqual(2, fires.Count, "적중한 대상마다");
        Assert.AreEqual(1f, fires[0].effect.areaRadius);
        Assert.AreEqual(0.5f, fires[0].value, 0.0001f);

        var d = Skill("lumiere_s5w").passive;
        Assert.AreEqual(PassiveFormulaId.Doppler, d.formula);
        Assert.AreEqual(2000f, d.value);
        Assert.AreEqual(PassiveTriggerType.TargetMoving, d.trigger);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 마시오 & 이네르셔 — 자동 폼
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Massio_FormsAutoRulesAndSlotsMatchDoc()
    {
        var c = Char("massio_inersha_r");
        Assert.AreEqual(2975, c.forms.Single(f => f.formId == "Mass").baseStats.hp);
        Assert.AreEqual(1700, c.forms.Single(f => f.formId == "Inertia").baseStats.hp, "hp는 폼별 독립");
        Assert.IsTrue(c.forms.Single(f => f.formId == "Mass").isDefault);
        Assert.AreEqual(2, c.autoRules.Length);
        Assert.AreEqual(FormAutoCond.IdleFor, c.autoRules[0].cond);
        Assert.AreEqual(2f, c.autoRules[0].param);
        Assert.AreEqual("Mass", c.autoRules[0].toForm);
        Assert.AreEqual(FormAutoCond.MovingFor, c.autoRules[1].cond);
        Assert.AreEqual("Inertia", c.autoRules[1].toForm);
    }

    [Test]
    public void Massio_AutoSwitchesAfterTwoSeconds_CarriesHpPercent_AndBasicAttackFollowsForm()
    {
        var c = MakeCombat("massio_inersha_r");
        Assert.AreEqual("Mass", c.CurrentFormId);
        Assert.AreEqual("massio_inersha_s0", c.SkillAt(c.IndexForSlot(0)).id, "마시오의 Q = 대검 베기");
        c.Damage(2975 - 40);                                          // 40 / 2975

        c.TickForm(1f, new CombatMotionState { isMoving = true });
        Assert.AreEqual("Mass", c.CurrentFormId, "이동 1초는 아직");
        c.TickForm(1.1f, new CombatMotionState { isMoving = true });
        Assert.AreEqual("Inertia", c.CurrentFormId, "이동 2초 → 이네르셔");
        Assert.AreEqual(23, c.Hp, "문서 예시: 40/2975 → 약 23");
        Assert.AreEqual("massio_inersha_s3", c.SkillAt(c.IndexForSlot(0)).id, "이네르셔의 Q = 대도 강타");

        var spec = BasicAttack.Resolve(c, 2.2f, 60f, 0.5f);
        Assert.AreEqual(1.8f, spec.range, 0.0001f);
        Assert.AreEqual(40f, spec.halfAngle, 0.0001f);
        Assert.AreEqual(0.6f, spec.cooldown, 0.0001f);
    }

    [Test]
    public void Massio_StaticInertia_GivesMassFormDefenseAndEnemyAura()
    {
        var c = MakeCombat("massio_inersha_r", "massio_inersha_s1");
        Assert.AreEqual(31, c.Def, "def 26 × 1.2");
        Assert.AreEqual(0.9f, c.MoveSpeedMultiplier, 0.0001f);
        var auras = c.EnemyAuras().ToList();
        Assert.AreEqual(1, auras.Count);
        Assert.AreEqual(6f, auras[0].radius);
        Assert.AreEqual(0.85f, auras[0].value, 0.0001f);
    }

    [Test]
    public void Massio_MassConservation_HalvesDamageTakenForFiveSeconds()
    {
        var s = Skill("massio_inersha_s2");
        Assert.AreEqual(SkillEffectType.DamageTakenMult, s.effects[0].type);
        Assert.AreEqual(0.5f, s.effects[0].value);
        Assert.AreEqual(5f, s.effects[0].duration);
        Assert.AreEqual(7f, s.effects[0].milestoneDuration);
    }

    [Test]
    public void Massio_InertiaForce_UsesHighSpeedTag()
    {
        var s = Skill("massio_inersha_s5");
        CollectionAssert.AreEqual(new[] { "고속" }, s.strengthTags);
        Assert.AreEqual("Inertia", s.formReq);
        Assert.IsNotNull(TagRuleTable.Current.Find("고속"));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 크로노 — 타격 공식·파티 존재
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Chrono_BasicSkillsMatchDoc()
    {
        var c = Char("chrono_sr");
        Assert.AreEqual(CharacterRole.Supporter, c.role);
        Assert.AreEqual(2000, c.baseStats.hp);
        var q = Skill("chrono_s0");
        Assert.IsTrue(q.startUnlocked);
        Assert.AreEqual(0.8f, q.damageMultiplier);
        Assert.AreEqual(1, q.milestoneExtraTargets);
        Assert.AreEqual(2f, Skill("chrono_s2").damageMultiplier);
        Assert.AreEqual(15f, Skill("chrono_s2").milestoneHalfAngleBonus);
    }

    [Test]
    public void Chrono_TimeDilation_UsesLorentzWithC2000()
    {
        var p = Skill("chrono_s3").passive;
        Assert.AreEqual(PassiveTriggerType.TargetMoving, p.trigger);
        Assert.AreEqual(PassiveFormulaId.Lorentz, p.formula);
        Assert.AreEqual(2000f, p.value);
    }

    [Test]
    public void Chrono_ProperTime_SpeedsUpCooldownsForTheWholeParty_IncludingBench()
    {
        var chrono = MakeCombat("chrono_sr", "chrono_s4");
        var other  = MakeCombat("forte_r");
        var party  = new Party(new[] { other, chrono });             // 크로노는 대기
        other.StartCooldown(0, 10f);
        party.Tick(2f);
        Assert.AreEqual(10f - 2f * 1.1765f, other.CooldownRemaining(0), 0.01f, "쿨타임 −15%(진행 배율 1/0.85)");
    }
}

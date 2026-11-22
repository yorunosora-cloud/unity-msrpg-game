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
        Assert.AreEqual(9, r.characters.Count, "대표 4 + 물리 1묶음 5(벡토르·벨로시아·액셀·모멘타·임펄사)");
        Assert.AreEqual(5, r.characters.Sum(c => c.conceptQuestions?.Length ?? 0), "개념 문제: 포르테 2 + 대표 나머지 각 1 (이관 캐릭터는 아직 교육 데이터 없음)");
        Assert.AreEqual(50, r.skills.Count, "대표 29 + 벡토르 4 + 벨로시아 4 + 액셀 4 + 모멘타 5 + 임펄사 4");
        Assert.AreEqual(8, r.tagRules.Length);
    }

    [Test]
    public void EveryShippedCharacter_HasUnlockedBasicSkill_WhenItHasOne()
    {
        foreach (var c in Load().characters)
        {
            var q = c.skills.Where(sk => sk.slot == SkillSlots.Basic && sk.skillType == SkillKind.Active).ToArray();
            if (q.Length == 0) continue;   // 폼별 Q가 없는 캐릭터(루미에르)는 기본 평타를 쓴다
            Assert.IsTrue(q.Any(sk => sk.startUnlocked), c.id + "의 기본기는 처음부터 열려 있어야 한다");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // 물리 이관 1묶음: 벡토르·벨로시아·액셀·모멘타·임펄사
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Batch1_Stats_MatchDocuments()
    {
        void Check(string id, int hp, int atk, int def, int spd, int mp, Rarity r, CharacterRole role)
        {
            var c = Char(id);
            Assert.AreEqual((hp, atk, def, spd, mp), (c.baseStats.hp, c.baseStats.atk, c.baseStats.def, c.baseStats.spd, c.baseStats.mp), id);
            Assert.AreEqual(r, c.rarity, id); Assert.AreEqual(role, c.role, id);
            Assert.AreEqual(Continent.Physics, c.continent, id);
        }
        Check("vektor_sr", 2000, 38, 7, 22, 15, Rarity.SR, CharacterRole.Dealer);
        Check("velocia_sr", 2000, 27, 10, 28, 15, Rarity.SR, CharacterRole.Dealer);
        Check("axel_sr", 2000, 42, 10, 13, 15, Rarity.SR, CharacterRole.Dealer);
        Check("momenta_sr", 2000, 20, 20, 20, 20, Rarity.SR, CharacterRole.AllRounder);
        Check("impulsa_sr", 2000, 35, 10, 20, 15, Rarity.SR, CharacterRole.Dealer);
    }

    [Test]
    public void Batch1_Vektor_ZeroVector_TeleportsAndCleansesBelowFortyPercent()
    {
        var s = Skill("vektor_s3");
        Assert.AreEqual(SkillKind.Passive, s.skillType);
        Assert.AreEqual(PassiveTriggerType.HpBelowPercent, s.passive.trigger);
        Assert.AreEqual(0.4f, s.passive.value, 0.0001f);
        Assert.AreEqual(60f, s.passive.internalCooldown);
        CollectionAssert.AreEqual(new[] { SkillEffectType.Teleport, SkillEffectType.Cleanse }, s.passive.effects.Select(e => e.type));
        Assert.AreEqual(1.8f, Skill("vektor_s1").effects[0].value); Assert.AreEqual(2.2f, Skill("vektor_s1").effects[0].milestoneValue);
    }

    [Test]
    public void Batch1_Velocia_UniformMotion_RampsMoveSpeedAndCooldownRate()
    {
        var s = Skill("velocia_s3");
        Assert.AreEqual(PassiveTriggerType.SelfMoving, s.passive.trigger);
        var m = s.passive.modifiers;
        Assert.AreEqual(2, m.Length);
        Assert.IsTrue(m.All(x => x.source == ModifierSource.MovingRamp && x.value == 0.03f && x.cap == 0.3f && x.decayPerSecond == 0.05f));
        CollectionAssert.AreEquivalent(new[] { StatKind.MoveSpeed, StatKind.CooldownRate }, m.Select(x => x.stat));
        Assert.AreEqual(-0.1f, Skill("velocia_s0").milestoneCooldownBonus, 0.0001f);
        Assert.AreEqual(-0.5f, Skill("velocia_s1").milestoneCooldownBonus, 0.0001f);
    }

    [Test]
    public void Batch1_Axel_NewtonSecondLaw_UsesHighMassTag_AndCounterBuffsNextHit()
    {
        CollectionAssert.AreEqual(new[] { "고질량" }, Skill("axel_s1").strengthTags);
        var c = Skill("axel_s3");
        Assert.AreEqual(PassiveTriggerType.OnHitTaken, c.passive.trigger);
        var e = c.passive.effects[0];
        Assert.AreEqual((SkillEffectType.StatBuff, StatKind.HitDamage, 1.6f, 1f), (e.type, e.stat, e.value, e.duration));
        Assert.IsTrue(e.consumeOnHit);
        Assert.AreEqual(0.2f, Skill("axel_s0").milestoneDamageBonus, 0.0001f);
    }

    [Test]
    public void Batch1_Momenta_MomentumConservation_IsDamageProportionalStack_CappedAt30Percent()
    {
        var st = Skill("momenta_s4").passive.stack;
        Assert.AreEqual(0.1f, st.damageRatio, 0.0001f);
        Assert.AreEqual(0.3f, st.cap, 0.0001f);
        Assert.AreEqual(StatKind.Atk, st.stat);
        Assert.AreEqual(StackDecay.ResetAfterIdle, st.decay);
        Assert.AreEqual(5f, st.decaySeconds);
        Assert.AreEqual(3, Skill("momenta_s1").effects[0].targetCap, "탄성충돌: 직선상 최대 3명");
        Assert.IsTrue(Skill("momenta_s2").effects.Any(e => e.type == SkillEffectType.Knockback && e.value == 1.5f));
    }

    [Test]
    public void Batch1_Impulsa_Recoil_IsBackwardDash_AndJFtStacksSkillPower()
    {
        var r = Skill("impulsa_s1");
        Assert.AreEqual(-3f, r.dashDistance); Assert.AreEqual(0.2f, r.dashTime, 0.0001f);
        var st = Skill("impulsa_s3").passive.stack;
        Assert.AreEqual((5, 0.08f, 3f, StatKind.SkillPower), (st.maxStacks, st.perStackValue, st.decaySeconds, st.stat));
        Assert.AreEqual(1f, Skill("impulsa_s0").milestoneRangeBonus);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 교육 레이어 (character_codex.csv, 18-C)
    // ════════════════════════════════════════════════════════════════════════

    static readonly string[] Representative = { "forte_r", "lumiere_ssr", "massio_inersha_r", "chrono_sr" };
    static IEnumerable<CharacterDef> RepresentativeChars() => Load().characters.Where(c => Representative.Contains(c.id));

    [Test]
    public void Codex_AllRepresentativeCharactersHaveEducationContent()
    {
        foreach (var c in RepresentativeChars())
        {
            Assert.IsFalse(string.IsNullOrEmpty(c.basicExplanation), c.id + " basicExplanation");
            Assert.IsFalse(string.IsNullOrEmpty(c.advancedExplanation), c.id + " advancedExplanation");
            Assert.IsFalse(string.IsNullOrEmpty(c.realPrinciple), c.id + " realPrinciple");
            Assert.IsFalse(string.IsNullOrEmpty(c.sourceRef), c.id + " sourceRef");
            Assert.GreaterOrEqual(c.conceptQuestions.Length, 1, c.id + " 개념 문제");
            foreach (var q in c.conceptQuestions)
            {
                Assert.GreaterOrEqual(q.choices.Length, 2);
                Assert.That(q.answerIndex, Is.InRange(0, q.choices.Length - 1));
            }
            Assert.AreEqual(1, c.affinityQuestions.Length, c.id + " 호감도 질문");
            Assert.AreEqual(2, c.affinityQuestions[0].choices.Length, "호감도 질문은 2지선다");
        }
    }

    [Test]
    public void Codex_ForteHasTwoQuestions_AndAnswersPointAtTheRightChoices()
    {
        var forte = Char("forte_r");
        Assert.AreEqual(2, forte.conceptQuestions.Length);
        StringAssert.Contains("서로 다른 물체", forte.conceptQuestions[0].choices[forte.conceptQuestions[0].answerIndex]);
        Assert.AreEqual("마찰력", forte.conceptQuestions[1].choices[forte.conceptQuestions[1].answerIndex]);

        var lum = Char("lumiere_ssr");
        StringAssert.Contains("진동수", lum.conceptQuestions[0].choices[lum.conceptQuestions[0].answerIndex]);
        var mas = Char("massio_inersha_r");
        StringAssert.Contains("관성", mas.conceptQuestions[0].choices[mas.conceptQuestions[0].answerIndex]);
        var chr = Char("chrono_sr");
        StringAssert.Contains("더 느리게", chr.conceptQuestions[0].choices[chr.conceptQuestions[0].answerIndex]);
    }

    [Test]
    public void Codex_ShippedContentWorksEndToEnd_ThroughCodexConcept()
    {
        var def = Char("massio_inersha_r");
        var owned = new OwnedCharacter { id = def.id };
        Assert.IsFalse(CodexConcept.IsAdvancedOpen(def, owned));
        Assert.IsFalse(CodexConcept.TryAnswer(def, owned, 0, 0), "오답(승객에게 앞쪽 힘)");
        Assert.IsTrue(CodexConcept.TryAnswer(def, owned, 0, def.conceptQuestions[0].answerIndex));
        Assert.IsTrue(owned.advancedCodexUnlocked);
    }

    [Test]
    public void Codex_AffinityPick_IsExactlyOneOfTheTwoChoices()
    {
        foreach (var c in RepresentativeChars())
            Assert.IsTrue(c.affinityQuestions[0].IsConceptPick(0) ^ c.affinityQuestions[0].IsConceptPick(1), c.id);
        Assert.AreEqual(0, Char("lumiere_ssr").affinityQuestions[0].conceptPick, "둘 다 맞다 = 첫 번째 선택지");
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
        Assert.AreEqual(SkillEffectType.Launch, buoy.effects[0].type, "부력 = 띄우기(스턴 + 연출)");
        Assert.AreEqual(1.5f, buoy.effects[0].value, "띄우는 높이 1.5m");
        Assert.AreEqual(2f, buoy.effects[0].duration);
        Assert.AreEqual(2.5f, buoy.effects[0].milestoneDuration);

        Assert.AreEqual(3f, line.dashDistance, "탄성력 돌진 3m");
        Assert.AreEqual(0.15f, line.dashTime, 0.0001f);
        Assert.AreEqual(0f, grav.dashDistance, "다른 스킬은 돌진 없음");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 스키마 갭 해소 데이터 (설계도 18 §8-5 #3·4·6·7)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Gap_Lumiere_Radiation_AutoSwitchesToShortWaveWithin3m()
    {
        var rad = Skill("lumiere_s2w");
        Assert.AreEqual(3f, rad.autoSwitchDistance);
        Assert.AreEqual(SkillEffectKind.Strike, rad.altEffectKind, "단파는 단일 대상");
        Assert.AreEqual(3f, rad.altRange);
        Assert.AreEqual(40f, rad.altHalfAngle);
        Assert.AreEqual(1.6f, rad.altDamageMultiplier, 0.0001f);
        Assert.AreEqual(SkillEffectKind.Aoe, rad.effectKind, "장파는 기본 광역");
    }

    [Test]
    public void Gap_Lumiere_Refraction_UsesAccuracyNotAttack()
    {
        var e = Skill("lumiere_s1w").effects[0];
        Assert.AreEqual(SkillEffectType.StatDebuff, e.type);
        Assert.AreEqual(StatKind.Accuracy, e.stat);
        Assert.AreEqual(0.8f, e.value, 0.0001f);
    }

    [Test]
    public void Gap_Lumiere_LightSpeedInvariance_IsSlowImmune()
    {
        var m = Skill("lumiere_s5p").passive.modifiers;
        Assert.AreEqual(1, m.Length);
        Assert.IsTrue(m[0].immuneToSlow);
        Assert.AreEqual(1.1f, m[0].value, 0.0001f, "이동속도 +10% 유지");
    }

    [Test]
    public void Gap_Massio_GreatBladeStrike_CooldownDropsAtLevel3()
    {
        var s = Skill("massio_inersha_s3");
        Assert.AreEqual(0.6f, SkillScaling.EffectiveCooldown(s, 1), 0.0001f);
        Assert.AreEqual(0.4f, SkillScaling.EffectiveCooldown(s, 3), 0.0001f);
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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 18-B — Stage B: 시트 → CharacterDef/SkillDef 매핑·교차 검증 (CharacterCsvBuilder).</summary>
public class CharacterCsvBuilderTests
{
    const string Cat = CharacterCsvBuilder.Catalog;
    const string Frm = CharacterCsvBuilder.Forms;
    const string Aut = CharacterCsvBuilder.AutoRules;
    const string Skl = CharacterCsvBuilder.Skills;
    const string Eff = CharacterCsvBuilder.Effects;
    const string Fch = CharacterCsvBuilder.FormChange;
    const string Fst = CharacterCsvBuilder.FormChangeStates;
    const string Pas = CharacterCsvBuilder.Passives;
    const string Mod = CharacterCsvBuilder.Modifiers;
    const string Tag = CharacterCsvBuilder.TagRules;

    static ImportResult Build(params (string sheet, string csv)[] sheets)
    {
        var d = new Dictionary<string, string>();
        foreach (var s in sheets) d[s.sheet] = s.csv;
        return CharacterCsvBuilder.Build(d);
    }

    const string CatHeader = "id,nameKo,continent,rarity,role,gender,hp,atk,def,spd,mp\n";
    const string CatRow    = "hero_r,영웅,Physics,R,Dealer,Female,1000,50,20,10,100\n";
    static readonly (string, string) OneChar = (Cat, CatHeader + CatRow);

    static void NoErrors(ImportResult r)
        => Assert.IsFalse(r.report.HasErrors, string.Join("\n", r.report.Errors));

    // ════════════════════════════════════════════════════════════════════════
    // 기본 · 카탈로그
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Empty_NoCharacters_NoErrors()
    {
        var r = Build();
        NoErrors(r);
        Assert.AreEqual(0, r.characters.Count);
    }

    [Test]
    public void Catalog_MapsAllFields()
    {
        var r = Build((Cat,
            "id,nameKo,nameEn,continent,country,rarity,role,gender,dexNumber,hp,atk,def,spd,mp,mass,velocity,temp,charge,personality,weapon,concept,basicAttackMotion,appearance,loreKo,portraitColor,gachaObtainable,acquireCondition\n" +
            "forte_r,포르테,Forte,Physics,newton,SR,Tanker,Male,7,2975,30,40,50,60,10,20,30,-40,강직함,대검,근력,heavy_swing,덩치,옛날 옛적,#FF8000,false,스토리 해금\n"));
        NoErrors(r);
        var c = r.characters.Single();
        Assert.AreEqual("forte_r", c.id);
        Assert.AreEqual("포르테", c.nameKo);
        Assert.AreEqual("Forte", c.nameEn);
        Assert.AreEqual(Continent.Physics, c.continent);
        Assert.AreEqual("newton", c.country);
        Assert.AreEqual(Rarity.SR, c.rarity);
        Assert.AreEqual(CharacterRole.Tanker, c.role);
        Assert.AreEqual(Gender.Male, c.gender);
        Assert.AreEqual(7, c.dexNumber);
        Assert.AreEqual(2975, c.baseStats.hp);
        Assert.AreEqual(30, c.baseStats.atk);
        Assert.AreEqual(40, c.baseStats.def);
        Assert.AreEqual(50, c.baseStats.spd);
        Assert.AreEqual(60, c.baseStats.mp);
        Assert.AreEqual(10, c.attributes.mass);
        Assert.AreEqual(-40, c.attributes.charge);
        Assert.AreEqual("강직함", c.personality);
        Assert.AreEqual("대검", c.weapon);
        Assert.AreEqual("근력", c.concept);
        Assert.AreEqual("heavy_swing", c.basicAttackMotion);
        Assert.AreEqual("덩치", c.appearance);
        Assert.AreEqual("옛날 옛적", c.loreKo);
        Assert.AreEqual(1f, c.portraitColor.r, 0.01f);
        Assert.IsFalse(c.gachaObtainable);
        Assert.AreEqual("스토리 해금", c.acquireCondition);
        Assert.AreEqual("forte_r", c.name, "에셋 이름 = id");
    }

    [Test]
    public void Catalog_Defaults_WhenOptionalColumnsMissing()
    {
        var r = Build(OneChar);
        NoErrors(r);
        var c = r.characters.Single();
        Assert.IsTrue(c.gachaObtainable);
        Assert.AreEqual(0, c.dexNumber);
        Assert.AreEqual(0, c.skills.Length);
    }

    [Test]
    public void Catalog_BadEnum_ReportsSheetLineAndAllowedValues()
    {
        var r = Build((Cat, CatHeader + "x_r,이름,Physix,R,Dealer,Female,1,1,1,1,1\n"));
        Assert.IsTrue(r.report.HasErrors);
        StringAssert.Contains("[character_catalog.csv:2]", r.report.Errors[0]);
        StringAssert.Contains("continent", r.report.Errors[0]);
        StringAssert.Contains("Physics", r.report.Errors[0]);
    }

    [Test]
    public void Catalog_MissingRequiredColumn_AndDuplicateId_AreErrors()
    {
        var r1 = Build((Cat, "id,nameKo\nx,y\n"));
        Assert.IsTrue(r1.report.HasErrors);
        StringAssert.Contains("continent", string.Join("\n", r1.report.Errors));

        var r2 = Build((Cat, CatHeader + CatRow + CatRow));
        Assert.IsTrue(r2.report.HasErrors);
        StringAssert.Contains("hero_r", string.Join("\n", r2.report.Errors));
    }

    [Test]
    public void Catalog_UnknownColumn_IsOnlyAWarning()
    {
        var r = Build((Cat, CatHeader.TrimEnd() + ",zzz\n" + CatRow.TrimEnd() + ",1\n"));
        NoErrors(r);
        Assert.AreEqual(1, r.report.Warnings.Count);
        StringAssert.Contains("zzz", r.report.Warnings[0]);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 폼 · 자동 규칙
    // ════════════════════════════════════════════════════════════════════════

    const string FrmHeader = "ownerId,formId,nameKo,role,gender,isDefault,hp,atk,def,spd,mp,tint\n";

    [Test]
    public void Forms_DefaultFormStatsBecomeBaseStats_AndFormsAreMapped()
    {
        var r = Build(OneChar, (Frm, FrmHeader +
            "hero_r,Mass,마시오,Tanker,Female,true,2975,10,20,30,40,#0000FF\n" +
            "hero_r,Inertia,이네르셔,Dealer,Male,false,1700,11,21,31,41,#FF0000\n"));
        NoErrors(r);
        var c = r.characters.Single();
        Assert.AreEqual(2, c.forms.Length);
        Assert.AreEqual("Mass", c.forms[0].formId);
        Assert.IsTrue(c.forms[0].isDefault);
        Assert.AreEqual(CharacterRole.Dealer, c.forms[1].role);
        Assert.AreEqual(Gender.Male, c.forms[1].gender);
        Assert.AreEqual(1700, c.forms[1].baseStats.hp);
        Assert.AreEqual(1f, c.forms[1].tint.r, 0.01f);
        Assert.AreEqual(2975, c.baseStats.hp, "기본 폼 스탯이 캐릭터 baseStats");
        Assert.AreEqual(CharacterRole.Tanker, c.role, "role·gender도 기본 폼 기준");
        Assert.AreEqual(Gender.Female, c.gender);
    }

    [Test]
    public void Forms_ZeroOrMultipleDefaults_AreErrors()
    {
        var none = Build(OneChar, (Frm, FrmHeader + "hero_r,A,a,Dealer,Male,false,1,1,1,1,1,\nhero_r,B,b,Dealer,Male,false,1,1,1,1,1,\n"));
        Assert.IsTrue(none.report.HasErrors);
        var two = Build(OneChar, (Frm, FrmHeader + "hero_r,A,a,Dealer,Male,true,1,1,1,1,1,\nhero_r,B,b,Dealer,Male,true,1,1,1,1,1,\n"));
        Assert.IsTrue(two.report.HasErrors);
    }

    [Test]
    public void Forms_UnknownOwner_AndDuplicateFormId_AreErrors()
    {
        var r1 = Build(OneChar, (Frm, FrmHeader + "nobody,A,a,Dealer,Male,true,1,1,1,1,1,\n"));
        Assert.IsTrue(r1.report.HasErrors);
        StringAssert.Contains("nobody", string.Join("\n", r1.report.Errors));
        var r2 = Build(OneChar, (Frm, FrmHeader + "hero_r,A,a,Dealer,Male,true,1,1,1,1,1,\nhero_r,A,a,Dealer,Male,false,1,1,1,1,1,\n"));
        Assert.IsTrue(r2.report.HasErrors);
    }

    static readonly (string, string) TwoForms = (Frm, FrmHeader +
        "hero_r,Idle,정지,Tanker,Female,true,100,1,1,1,1,\nhero_r,Move,이동,Dealer,Female,false,100,1,1,1,1,\n");

    [Test]
    public void AutoRules_AreMapped()
    {
        var r = Build(OneChar, TwoForms, (Aut,
            "ownerId,order,cond,param,statKey,statCompare,statThreshold,consecutive,toForm,priority\n" +
            "hero_r,2,MovingFor,2,,,,,Move,1\n" +
            "hero_r,1,HitTargetStat,,temp,AtLeast,50,3,Idle,2\n"));
        NoErrors(r);
        var rules = r.characters.Single().autoRules;
        Assert.AreEqual(2, rules.Length);
        Assert.AreEqual(FormAutoCond.HitTargetStat, rules[0].cond, "order 순서");
        Assert.AreEqual("temp", rules[0].statKey);
        Assert.AreEqual(FormCompare.AtLeast, rules[0].statCompare);
        Assert.AreEqual(50f, rules[0].statThreshold);
        Assert.AreEqual(3, rules[0].consecutive);
        Assert.AreEqual(FormAutoCond.MovingFor, rules[1].cond);
        Assert.AreEqual(2f, rules[1].param);
        Assert.AreEqual("Move", rules[1].toForm);
    }

    [Test]
    public void AutoRules_UnknownToForm_OrOwnerWithoutForms_AreErrors()
    {
        var r1 = Build(OneChar, TwoForms, (Aut, "ownerId,cond,toForm\nhero_r,IdleFor,Nope\n"));
        Assert.IsTrue(r1.report.HasErrors);
        var r2 = Build(OneChar, (Aut, "ownerId,cond,toForm\nhero_r,IdleFor,Idle\n"));
        Assert.IsTrue(r2.report.HasErrors, "폼이 없는 캐릭터에는 자동 규칙을 둘 수 없다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 스킬
    // ════════════════════════════════════════════════════════════════════════

    const string SklHeader = "skillId,ownerId,slot,nameKo,skillType,effectKind,startUnlocked,formReq\n";

    [Test]
    public void Skills_MapAllFields_AndSortedBySlot()
    {
        var r = Build(OneChar, (Skl,
            "skillId,ownerId,slot,nameKo,descKo,skillType,startUnlocked,unlockCond,effectKind,mpCost,cooldown,shape,range,halfAngle,lineWidth,radius,damageMultiplier,healPercent,buffAtkMultiplier,buffDuration,strengthTags,skillMotion,factNoteKo,gameNoteKo,milestoneLevel,milestoneDescKo,milestoneExtraTargets,milestoneRangeBonus,milestoneHalfAngleBonus,milestoneHealPercentBonus,milestoneBuffDurationBonus\n" +
            "hero_s2,hero_r,2,둘째,설명2,Active,false,StoryFlag:x,Aoe,12,6.5,Circle,3,45,2,4,1.5,0.2,1.3,5,고온;고질량,spin,사실,게임,4,마일,1,0.5,10,0.05,1,\n" +
            "hero_s0,hero_r,0,기본기,설명0,Active,true,,Strike,0,0.8,Cone,2.5,60,1,3,1,0,1,0,,slash,,,3,,0,0,0,0,0\n"));
        NoErrors(r);
        var c = r.characters.Single();
        Assert.AreEqual(new[] { "hero_s0", "hero_s2" }, c.skills.Select(s => s.id).ToArray(), "slot 오름차순 배치");

        var s = c.skills[1];
        Assert.AreEqual("hero_s2", s.name);
        Assert.AreEqual(2, s.slot);
        Assert.AreEqual("둘째", s.nameKo);
        Assert.AreEqual("설명2", s.descKo);
        Assert.AreEqual(SkillKind.Active, s.skillType);
        Assert.IsFalse(s.startUnlocked);
        Assert.AreEqual("StoryFlag:x", s.unlockCond);
        Assert.AreEqual(SkillEffectKind.Aoe, s.effectKind);
        Assert.AreEqual(12, s.mpCost);
        Assert.AreEqual(6.5f, s.cooldown);
        Assert.AreEqual(SkillShape.Circle, s.shape);
        Assert.AreEqual(3f, s.range);
        Assert.AreEqual(45f, s.halfAngle);
        Assert.AreEqual(2f, s.lineWidth);
        Assert.AreEqual(4f, s.radius);
        Assert.AreEqual(1.5f, s.damageMultiplier);
        Assert.AreEqual(0.2f, s.healPercent);
        Assert.AreEqual(1.3f, s.buffAtkMultiplier);
        Assert.AreEqual(5f, s.buffDuration);
        CollectionAssert.AreEqual(new[] { "고온", "고질량" }, s.strengthTags);
        Assert.AreEqual("spin", s.skillMotion);
        Assert.AreEqual("사실", s.factNoteKo);
        Assert.AreEqual("게임", s.gameNoteKo);
        Assert.AreEqual(4, s.milestoneLevel);
        Assert.AreEqual("마일", s.milestoneDescKo);
        Assert.AreEqual(1, s.milestoneExtraTargets);
        Assert.AreEqual(0.5f, s.milestoneRangeBonus);
        Assert.AreEqual(10f, s.milestoneHalfAngleBonus);
        Assert.AreEqual(0.05f, s.milestoneHealPercentBonus);
        Assert.AreEqual(1f, s.milestoneBuffDurationBonus);
        Assert.IsTrue(c.skills[0].startUnlocked);
        Assert.AreEqual(3, c.skills[0].milestoneLevel);
    }

    [Test]
    public void Skills_UnknownOwner_DuplicateId_BadSlot_AreErrors()
    {
        var a = Build(OneChar, (Skl, SklHeader + "s1,nobody,1,n,Active,Strike,true,\n"));
        Assert.IsTrue(a.report.HasErrors);
        var b = Build(OneChar, (Skl, SklHeader + "s1,hero_r,1,n,Active,Strike,true,\ns1,hero_r,2,n,Active,Strike,true,\n"));
        Assert.IsTrue(b.report.HasErrors);
        var c = Build(OneChar, (Skl, SklHeader + "s1,hero_r,7,n,Active,Strike,true,\n"));
        Assert.IsTrue(c.report.HasErrors);
        StringAssert.Contains("slot", string.Join("\n", c.report.Errors));
    }

    [Test]
    public void Skills_SameSlotConflict_UnlessFormsDiffer()
    {
        var conflict = Build(OneChar, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,\nb,hero_r,1,n,Active,Strike,true,\n"));
        Assert.IsTrue(conflict.report.HasErrors);
        StringAssert.Contains("슬롯", string.Join("\n", conflict.report.Errors));

        var ok = Build(OneChar, TwoForms, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,Idle\nb,hero_r,1,n,Active,Strike,true,Move\n"));
        NoErrors(ok);

        var overlap = Build(OneChar, TwoForms, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,\nb,hero_r,1,n,Active,Strike,true,Move\n"));
        Assert.IsTrue(overlap.report.HasErrors, "폼 제한이 없는 스킬은 모든 폼과 겹친다");
    }

    [Test]
    public void Skills_PassivesDoNotOccupyKeySlots()
    {
        var r = Build(OneChar, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,\np,hero_r,1,n,Passive,Strike,true,\n"),
                      (Pas, "skillId,trigger\np,None\n"));
        NoErrors(r);
    }

    [Test]
    public void Skills_FormReq_UsesSemicolonList_JoinedWithComma_AndValidatesForms()
    {
        var ok = Build(OneChar, TwoForms, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,Idle;Move\n"));
        NoErrors(ok);
        Assert.AreEqual("Idle,Move", ok.characters.Single().skills[0].formReq);

        var bad = Build(OneChar, TwoForms, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,Nope\n"));
        Assert.IsTrue(bad.report.HasErrors);
        var noForms = Build(OneChar, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,Idle\n"));
        Assert.IsTrue(noForms.report.HasErrors, "폼이 없는 캐릭터의 스킬에 formReq를 둘 수 없다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 효과 목록
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Effects_AttachInOrder_AndMapAllFields()
    {
        var r = Build(OneChar, (Skl, SklHeader + "fx,hero_r,1,n,Active,Strike,true,\n"), (Eff,
            "skillId,order,type,target,stat,value,duration,maxStacks,targetCap,areaRadius,milestoneValue,milestoneDuration,whenForm,consumeOnHit\n" +
            "fx,2,Slow,EnemyArea,,0.4,3,,,,0.3,4,,\n" +
            "fx,1,StatBuff,SelfAndBench,Atk,1.15,5,2,4,1.5,1.2,6,,true\n"));
        NoErrors(r);
        var fx = r.characters.Single().skills[0].effects;
        Assert.AreEqual(2, fx.Length);
        Assert.AreEqual(SkillEffectType.StatBuff, fx[0].type, "order 순서");
        Assert.AreEqual(TargetScope.SelfAndBench, fx[0].target);
        Assert.AreEqual(StatKind.Atk, fx[0].stat);
        Assert.AreEqual(1.15f, fx[0].value);
        Assert.AreEqual(5f, fx[0].duration);
        Assert.AreEqual(2, fx[0].maxStacks);
        Assert.AreEqual(4, fx[0].targetCap);
        Assert.AreEqual(1.5f, fx[0].areaRadius);
        Assert.AreEqual(1.2f, fx[0].milestoneValue);
        Assert.AreEqual(6f, fx[0].milestoneDuration);
        Assert.IsTrue(fx[0].consumeOnHit);
        Assert.AreEqual(SkillEffectType.Slow, fx[1].type);
        Assert.AreEqual(TargetScope.EnemyArea, fx[1].target, "target 기본 = EnemyArea");
    }

    [Test]
    public void Effects_UnknownSkill_AndBadType_AreErrors()
    {
        var a = Build(OneChar, (Eff, "skillId,type\nnope,Damage\n"));
        Assert.IsTrue(a.report.HasErrors);
        var b = Build(OneChar, (Skl, SklHeader + "fx,hero_r,1,n,Active,Strike,true,\n"), (Eff, "skillId,type\nfx,Explode\n"));
        Assert.IsTrue(b.report.HasErrors);
        StringAssert.Contains("Explode", string.Join("\n", b.report.Errors));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 폼 전환 스킬
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void FormChange_MapsSpec_AndStates()
    {
        var r = Build(OneChar, TwoForms,
            (Skl, SklHeader + "z,hero_r,0,전환,FormChange,Strike,true,\n"),
            (Fch, "skillId,keyIndex,mode,fromForms,toForm,sequence,revertAfterSeconds,revertToForm,autoRevertForms,pressAgainReverts\n" +
                  "z,1,StateBased,Idle,,,8,Idle,Move,true\n"),
            (Fst, "skillId,order,cond,param,toForm,priority\nz,2,Always,,Idle,1\nz,1,RecentlyHit,3,Move,3\n"));
        NoErrors(r);
        var spec = r.characters.Single().skills[0].formChange;
        Assert.IsNotNull(spec);
        Assert.AreEqual(1, spec.keyIndex);
        Assert.AreEqual(FormChangeMode.StateBased, spec.mode);
        CollectionAssert.AreEqual(new[] { "Idle" }, spec.fromForms);
        Assert.AreEqual(8f, spec.revertAfterSeconds);
        Assert.AreEqual("Idle", spec.revertToForm);
        CollectionAssert.AreEqual(new[] { "Move" }, spec.autoRevertForms);
        Assert.IsTrue(spec.pressAgainReverts);
        Assert.AreEqual(2, spec.stateRules.Length);
        Assert.AreEqual(FormStateCond.RecentlyHit, spec.stateRules[0].cond, "order 순서");
        Assert.AreEqual(3f, spec.stateRules[0].param);
        Assert.AreEqual(3, spec.stateRules[0].priority);
    }

    [Test]
    public void FormChange_MissingRow_WrongSkillType_UnknownForm_AreErrors()
    {
        var missing = Build(OneChar, TwoForms, (Skl, SklHeader + "z,hero_r,0,전환,FormChange,Strike,true,\n"));
        Assert.IsTrue(missing.report.HasErrors, "FormChange 스킬에는 skill_formchange 행이 필요");

        var wrongType = Build(OneChar, TwoForms, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,\n"),
            (Fch, "skillId,mode\na,Toggle\n"));
        Assert.IsTrue(wrongType.report.HasErrors);

        var badForm = Build(OneChar, TwoForms, (Skl, SklHeader + "z,hero_r,0,전환,FormChange,Strike,true,\n"),
            (Fch, "skillId,mode,sequence\nz,Toggle,Idle;Nope\n"));
        Assert.IsTrue(badForm.report.HasErrors);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 패시브 · 수정자
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Passive_MapsSpec_StackEffectsAndModifiers()
    {
        var r = Build(OneChar,
            (Skl, SklHeader + "p,hero_r,1,패시브,Passive,Strike,true,\n"),
            (Pas, "skillId,trigger,value,hitCount,internalCooldown,formula,milestoneValue,stackId,maxStacks,perStackValue,decay,decaySeconds,stackStat,damageRatio,cap,milestonePerStackValue,milestoneMaxStacks\n" +
                  "p,OnHitTaken,0.4,PerAction,60,Lorentz,0.4,diel,5,-0.03,DecayOneEvery,3,DamageTaken,0.1,0.3,-0.04,6\n"),
            (Eff, "skillId,order,type,target,value,duration\np,1,Knockback,Attacker,2,\n"),
            (Mod, "skillId,order,source,stat,value,cap,decayPerSecond,partyWide,enemyAuraRadius,milestoneValue,milestoneCap\n" +
                  "p,1,MovingRamp,MoveSpeed,0.03,0.3,0.05,false,0,0.04,0.4\n"));
        NoErrors(r);
        var s = r.characters.Single().skills[0];
        Assert.AreEqual(SkillKind.Passive, s.skillType);
        var p = s.passive;
        Assert.AreEqual(PassiveTriggerType.OnHitTaken, p.trigger);
        Assert.AreEqual(0.4f, p.value);
        Assert.AreEqual(HitCountMode.PerAction, p.hitCount);
        Assert.AreEqual(60f, p.internalCooldown);
        Assert.AreEqual(PassiveFormulaId.Lorentz, p.formula);
        Assert.AreEqual(0.4f, p.milestoneValue);
        Assert.AreEqual("diel", p.stack.stackId);
        Assert.AreEqual(5, p.stack.maxStacks);
        Assert.AreEqual(-0.03f, p.stack.perStackValue, 0.0001f);
        Assert.AreEqual(StackDecay.DecayOneEvery, p.stack.decay);
        Assert.AreEqual(3f, p.stack.decaySeconds);
        Assert.AreEqual(StatKind.DamageTaken, p.stack.stat);
        Assert.AreEqual(0.1f, p.stack.damageRatio, 0.0001f);
        Assert.AreEqual(0.3f, p.stack.cap, 0.0001f);
        Assert.AreEqual(-0.04f, p.stack.milestonePerStackValue, 0.0001f);
        Assert.AreEqual(6, p.stack.milestoneMaxStacks);
        Assert.AreEqual(1, p.effects.Length, "패시브 스킬의 효과 행은 passive.effects로");
        Assert.AreEqual(SkillEffectType.Knockback, p.effects[0].type);
        Assert.IsTrue(s.effects == null || s.effects.Length == 0, "스킬 자체 effects는 비어 있다");
        Assert.AreEqual(1, p.modifiers.Length);
        Assert.AreEqual(ModifierSource.MovingRamp, p.modifiers[0].source);
        Assert.AreEqual(StatKind.MoveSpeed, p.modifiers[0].stat);
        Assert.AreEqual(0.03f, p.modifiers[0].value, 0.0001f);
        Assert.AreEqual(0.3f, p.modifiers[0].cap, 0.0001f);
        Assert.AreEqual(0.05f, p.modifiers[0].decayPerSecond, 0.0001f);
        Assert.AreEqual(0.04f, p.modifiers[0].milestoneValue, 0.0001f);
        Assert.AreEqual(0.4f, p.modifiers[0].milestoneCap, 0.0001f);
    }

    [Test]
    public void Passive_MissingRow_WrongSkillType_ModifierOnNonPassive_AreErrors()
    {
        var missing = Build(OneChar, (Skl, SklHeader + "p,hero_r,1,n,Passive,Strike,true,\n"));
        Assert.IsTrue(missing.report.HasErrors);

        var wrong = Build(OneChar, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,\n"), (Pas, "skillId,trigger\na,OnHit\n"));
        Assert.IsTrue(wrong.report.HasErrors);

        var mod = Build(OneChar, (Skl, SklHeader + "a,hero_r,1,n,Active,Strike,true,\n"), (Mod, "skillId,source,stat\na,Constant,Atk\n"));
        Assert.IsTrue(mod.report.HasErrors);
    }

    [Test]
    public void Modifiers_PartyWideAndAura_AreMapped()
    {
        var r = Build(OneChar, (Skl, SklHeader + "p,hero_r,1,n,Passive,Strike,true,\n"),
            (Pas, "skillId,trigger\np,PartyPresence\n"),
            (Mod, "skillId,source,stat,value,partyWide,enemyAuraRadius\np,PartyPresence,CooldownRate,1.15,true,0\np,Constant,MoveSpeed,0.85,false,6\n"));
        NoErrors(r);
        var m = r.characters.Single().skills[0].passive.modifiers;
        Assert.IsTrue(m[0].partyWide);
        Assert.AreEqual(6f, m[1].enemyAuraRadius);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 태그 사전
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void TagRules_MappedWithDirection()
    {
        var r = Build((Tag, "tagId,displayKo,iconId,targetAttr,direction,baseMult,coeffPerUnit,refValue,capMult\n" +
                            "고온,열에 취약,fire,temp,high,1,0.001,300,2\n저온,냉기,ice,temp,low,1,0.002,300,1.8\n"));
        NoErrors(r);
        Assert.AreEqual(2, r.tagRules.Length);
        Assert.IsTrue(r.tagRules[0].directionHigh);
        Assert.IsFalse(r.tagRules[1].directionHigh);
        Assert.AreEqual("temp", r.tagRules[0].targetAttr);
        Assert.AreEqual(1.6f, r.tagRules[0].Evaluate(new AttributeProfile { temp = 900 }), 0.0001f);
    }

    [Test]
    public void TagRules_BadDirectionOrAttr_AreErrors()
    {
        var r = Build((Tag, "tagId,targetAttr,direction\na,temp,up\nb,heat,high\n"));
        Assert.AreEqual(2, r.report.Errors.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 통합: 여러 오류 동시 보고 · 실제 전투 동작
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void MultipleErrors_AreAllReportedTogether()
    {
        var r = Build(OneChar, (Skl, SklHeader + "a,nobody,1,n,Active,Strike,true,\nb,hero_r,9,n,Active,Boom,true,\n"));
        Assert.GreaterOrEqual(r.report.Errors.Count, 3);
    }

    [Test]
    public void BuiltPassive_ActuallyWorksInCombat()
    {
        var r = Build(OneChar,
            (Skl, SklHeader + "p,hero_r,1,충격량,Passive,Strike,true,\n"),
            (Pas, "skillId,trigger,hitCount,maxStacks,perStackValue,decay,decaySeconds,stackStat\np,OnHit,PerAction,5,0.08,ResetAfterIdle,3,SkillPower\n"));
        NoErrors(r);
        var def = r.characters.Single();
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1 });
        c.TickStatus(0f, true);
        c.NotifyHits(new List<HitInfo> { new HitInfo { actionId = 1, damage = 10 } }, false);
        Assert.AreEqual(1, c.PassiveStacks("p"));
        Assert.AreEqual(1.08f, c.HitMultiplier(false), 0.0001f);
    }

    [Test]
    public void BuiltForms_ActuallyWorkInCombat()
    {
        var r = Build(OneChar, TwoForms,
            (Aut, "ownerId,cond,param,toForm,priority\nhero_r,MovingFor,0.5,Move,1\n"));
        NoErrors(r);
        var def = r.characters.Single();
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1 });
        Assert.AreEqual("Idle", c.CurrentFormId);
        c.TickForm(0.6f, new CombatMotionState { isMoving = true });
        Assert.AreEqual("Move", c.CurrentFormId);
    }
}

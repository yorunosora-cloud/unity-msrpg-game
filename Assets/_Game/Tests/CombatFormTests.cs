using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 22 P3 — Stage B: CombatCharacter 폼 상태·전환·자동 복귀·초기화.</summary>
public class CombatFormTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    static CharacterForm F(string id, int hp, int atk, bool isDefault = false,
                           CharacterRole role = CharacterRole.Dealer, Gender gender = Gender.Female, Color? tint = null)
        => new CharacterForm
        {
            formId = id, nameKo = id, role = role, gender = gender, isDefault = isDefault,
            baseStats = new CharacterStats { hp = hp, atk = atk, def = 10, spd = 10, mp = 100 },
            tint = tint ?? Color.white,
        };

    static SkillDef FormSkill(FormChangeSpec spec, int mp = 3, float cd = 4f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "fc_" + Random.Range(0, 99999); s.nameKo = "form";
        s.skillType = SkillKind.FormChange; s.slot = 0; s.formChange = spec;
        s.mpCost = mp; s.cooldown = cd;
        return s;
    }

    static SkillDef PlainSkill(int slot, string formReq = null)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "p_" + Random.Range(0, 99999); s.nameKo = "plain"; s.slot = slot; s.formReq = formReq;
        s.cooldown = 1f;
        return s;
    }

    static CombatCharacter Make(CharacterForm[] forms, FormAutoRule[] rules = null, params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "c" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N;
        def.continent = Continent.Physics; def.portraitColor = Color.gray;
        def.baseStats = new CharacterStats { hp = 500, atk = 50, def = 5, spd = 5, mp = 50 };
        def.forms = forms; def.autoRules = rules; def.skills = skills;
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1, exp = 0 });
        for (int i = 0; i < skills.Length; i++) c.Unlock(i);
        return c;
    }

    static CombatMotionState Idle => new CombatMotionState { speedRatio = 0, isMoving = false };
    static CombatMotionState Moving => new CombatMotionState { speedRatio = 0.5f, isMoving = true };

    // ════════════════════════════════════════════════════════════════════════
    // 폼 기본 정보
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void NoForms_BehavesAsBefore()
    {
        var c = Make(null);
        Assert.AreEqual(string.Empty, c.CurrentFormId);
        Assert.AreEqual(500, c.MaxHp);
        Assert.AreEqual(Color.gray, c.TintColor);
        Assert.IsFalse(c.ChangeForm("X", FormChangeCause.Manual));
    }

    [Test]
    public void Forms_DefaultFormChosen_AndStatsComeFromIt()
    {
        var c = Make(new[] { F("A", 1000, 10), F("B", 2000, 99, isDefault: true) });
        Assert.AreEqual("B", c.CurrentFormId);
        Assert.AreEqual(2000, c.MaxHp);
        Assert.AreEqual(99, c.Atk);
        Assert.AreEqual(2000, c.Hp, "시작 HP는 기본 폼 최대치");
    }

    [Test]
    public void Forms_NoDefaultFlag_UsesFirst()
    {
        var c = Make(new[] { F("A", 1000, 10), F("B", 2000, 99) });
        Assert.AreEqual("A", c.CurrentFormId);
    }

    [Test]
    public void Forms_RoleGenderTint_ArePerForm()
    {
        var c = Make(new[]
        {
            F("A", 1000, 10, true, CharacterRole.Tanker, Gender.Female, Color.red),
            F("B", 1000, 10, false, CharacterRole.Dealer, Gender.Male, Color.blue),
        });
        Assert.AreEqual(CharacterRole.Tanker, c.CurrentRole);
        Assert.AreEqual(Gender.Female, c.CurrentGender);
        Assert.AreEqual(Color.red, c.TintColor);
        c.ChangeForm("B", FormChangeCause.Manual);
        Assert.AreEqual(CharacterRole.Dealer, c.CurrentRole);
        Assert.AreEqual(Gender.Male, c.CurrentGender);
        Assert.AreEqual(Color.blue, c.TintColor);
        Assert.AreEqual("B", c.FormNameKo);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ChangeForm — 이월
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChangeForm_CarriesHpPercent_FromDoc()
    {
        var c = Make(new[] { F("Mass", 2975, 10, true), F("Inertia", 1700, 10) });
        c.Damage(2975 - 40);                                    // 40 / 2975
        c.ChangeForm("Inertia", FormChangeCause.Auto);
        Assert.AreEqual(23, c.Hp);
        Assert.AreEqual(1700, c.MaxHp);
    }

    [Test]
    public void ChangeForm_CarriesMpPercent()
    {
        var c = Make(new[] { F("A", 1000, 10, true), F("B", 1000, 10) });
        c.UseMp(50);                                            // 50 / 100
        c.ChangeForm("B", FormChangeCause.Manual);
        Assert.AreEqual(50, c.Mp);
    }

    [Test]
    public void ChangeForm_KeepsCooldowns_AndStatus()
    {
        var skill = PlainSkill(1);
        var c = Make(new[] { F("A", 1000, 10, true), F("B", 1000, 10) }, null, skill);
        c.StartCooldown(0, 7f);
        c.Status.Apply(new StatusEffect { Id = "b", Kind = StatusKind.StatMod, Stat = StatKind.Atk, Value = 2f, Duration = 9f, IsBeneficial = true }, true);
        c.ChangeForm("B", FormChangeCause.Manual);
        Assert.AreEqual(7f, c.CooldownRemaining(0), 0.001f);
        Assert.AreEqual(1, c.Status.Count);
    }

    [Test]
    public void ChangeForm_FiresEvents_AndRejectsSameOrUnknown()
    {
        var c = Make(new[] { F("A", 1000, 10, true), F("B", 1000, 10) });
        string from = null, to = null; FormChangeCause cause = default; int changed = 0;
        c.OnFormChanged += (f, t, cs) => { from = f; to = t; cause = cs; };
        c.OnChanged += r => { if (r == "form") changed++; };
        Assert.IsFalse(c.ChangeForm("A", FormChangeCause.Manual), "같은 폼");
        Assert.IsFalse(c.ChangeForm("Z", FormChangeCause.Manual), "없는 폼");
        Assert.IsTrue(c.ChangeForm("B", FormChangeCause.Auto));
        Assert.AreEqual("A", from); Assert.AreEqual("B", to); Assert.AreEqual(FormChangeCause.Auto, cause);
        Assert.AreEqual(1, changed);
    }

    [Test]
    public void Downed_ReturnsToDefaultForm()
    {
        var c = Make(new[] { F("A", 1000, 10, true), F("B", 1000, 10) });
        c.ChangeForm("B", FormChangeCause.Manual);
        c.Damage(c.MaxHp);
        Assert.AreEqual("A", c.CurrentFormId);
        Assert.IsTrue(c.IsDowned);
    }

    [Test]
    public void RestoreFull_ResetsToDefaultForm_AndFullStats()
    {
        var c = Make(new[] { F("A", 1000, 10, true), F("B", 3000, 10) });
        c.ChangeForm("B", FormChangeCause.Manual);
        c.Damage(100);
        c.RestoreFull();
        Assert.AreEqual("A", c.CurrentFormId);
        Assert.AreEqual(1000, c.Hp);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 슬롯 해석 보완
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void IndexForSlot_Slot0_ExcludesFormChangeSkills()
    {
        var fc    = FormSkill(new FormChangeSpec { mode = FormChangeMode.Toggle, sequence = new[] { "A", "B" } });
        var basic = PlainSkill(0);
        var c = Make(new[] { F("A", 1000, 10, true), F("B", 1000, 10) }, null, fc, basic);
        Assert.AreEqual(1, c.IndexForSlot(0), "Q는 기본기만(폼 전환 스킬 제외)");
    }

    [Test]
    public void IndexForSlot_FormReq_AcceptsCommaSeparatedForms()
    {
        var s = PlainSkill(1, "Protium, Deuterium");
        var c = Make(new[] { F("Protium", 1000, 10, true), F("Deuterium", 1000, 10), F("Tritium", 1000, 10) }, null, s);
        Assert.AreEqual(0, c.IndexForSlot(1));
        c.ChangeForm("Tritium", FormChangeCause.Manual);
        Assert.AreEqual(-1, c.IndexForSlot(1));
        c.ChangeForm("Deuterium", FormChangeCause.Manual);
        Assert.AreEqual(0, c.IndexForSlot(1));
    }

    // ════════════════════════════════════════════════════════════════════════
    // TryFormChange — 수동 전환
    // ════════════════════════════════════════════════════════════════════════

    static CombatCharacter Hydrogen(out SkillDef skill)
    {
        skill = FormSkill(new FormChangeSpec
        {
            keyIndex = 0, mode = FormChangeMode.Sequential,
            sequence = new[] { "Protium", "Deuterium", "Tritium" },
            autoRevertForms = new[] { "Tritium" }, revertAfterSeconds = 12.3f, revertToForm = "Protium",
        }, mp: 3, cd: 4f);
        return Make(new[] { F("Protium", 2125, 10, true), F("Deuterium", 2125, 10), F("Tritium", 2125, 10) }, null, skill);
    }

    [Test]
    public void TryFormChange_Sequential_SpendsMp_StartsCooldown()
    {
        var c = Hydrogen(out _);
        int idx = c.TryFormChange(0, false);
        Assert.AreEqual(0, idx);
        Assert.AreEqual("Deuterium", c.CurrentFormId);
        Assert.AreEqual(97, c.Mp, "mp 3 소모");
        Assert.AreEqual(4f, c.CooldownRemaining(0), 0.001f);
    }

    [Test]
    public void TryFormChange_OnCooldown_Fails()
    {
        var c = Hydrogen(out _);
        c.TryFormChange(0, false);
        Assert.AreEqual(-1, c.TryFormChange(0, false));
        Assert.AreEqual("Deuterium", c.CurrentFormId);
    }

    [Test]
    public void TryFormChange_NotEnoughMp_Fails()
    {
        var c = Hydrogen(out _);
        c.UseMp(99);                                            // mp 1 (< 3)
        Assert.AreEqual(-1, c.TryFormChange(0, false));
    }

    [Test]
    public void TryFormChange_Stunned_OrDowned_Fails()
    {
        var c = Hydrogen(out _);
        c.Status.Apply(new StatusEffect { Id = "stun", Kind = StatusKind.Stun, Duration = 5f }, true);
        Assert.AreEqual(-1, c.TryFormChange(0, false), "스턴 중 금지");
        c.Status.Clear();
        c.Damage(c.MaxHp);
        Assert.AreEqual(-1, c.TryFormChange(0, false), "기절 중 금지");
    }

    [Test]
    public void TryFormChange_WrongKey_OrLockedSkill_Fails()
    {
        var c = Hydrogen(out _);
        Assert.AreEqual(-1, c.TryFormChange(1, false), "X에는 전환 스킬 없음");
    }

    [Test]
    public void TryFormChange_AtLastSequenceForm_Fails()
    {
        var c = Hydrogen(out _);
        c.ChangeForm("Tritium", FormChangeCause.Manual);
        Assert.AreEqual(-1, c.TryFormChange(0, false));
    }

    // ── 자동 복귀 타이머 (수소 삼중수소 12.3초) ──

    [Test]
    public void AutoRevert_Tritium_ReturnsToProtiumAfter12Point3Seconds_WhileActive()
    {
        var c = Hydrogen(out _);
        c.TryFormChange(0, false);                              // → Deuterium (복귀 타이머 없음)
        c.TickCooldowns(10f);
        c.TryFormChange(0, false);                              // → Tritium (타이머 시작)
        Assert.AreEqual("Tritium", c.CurrentFormId);
        c.TickForm(12f, Idle);
        Assert.AreEqual("Tritium", c.CurrentFormId, "12초에는 아직");
        c.TickForm(0.4f, Idle);
        Assert.AreEqual("Protium", c.CurrentFormId, "12.3초 경과 → 복귀");
    }

    [Test]
    public void AutoRevert_Deuterium_HasNoTimer()
    {
        var c = Hydrogen(out _);
        c.TryFormChange(0, false);
        c.TickForm(100f, Idle);
        Assert.AreEqual("Deuterium", c.CurrentFormId);
    }

    [Test]
    public void AutoRevert_TimerOnlyRunsWhenTicked_BenchPausesIt()
    {
        var c = Hydrogen(out _);
        c.ChangeForm("Deuterium", FormChangeCause.Manual);
        c.TickCooldowns(10f);
        c.TryFormChange(0, false);                              // Tritium, 12.3s
        c.TickForm(10f, Idle);
        // 대기: TickForm 호출 없음(활성일 때만 호출됨)
        c.OnSwitchIn();                                         // 입장해도 타이머는 유지(리셋 아님)
        c.TickForm(2.2f, Idle);
        Assert.AreEqual("Tritium", c.CurrentFormId, "남은 2.3초 중 2.2초");
        c.TickForm(0.2f, Idle);
        Assert.AreEqual("Protium", c.CurrentFormId);
    }

    // ── 허브(산소): 재입력 복귀는 mp 무소모·쿨타임만 ──

    [Test]
    public void Hub_PressAgain_Reverts_WithoutMp_ButNeedsCooldown()
    {
        var spec = new FormChangeSpec
        {
            keyIndex = 0, mode = FormChangeMode.Hub, fromForms = new[] { "O2" }, toForm = "O",
            revertAfterSeconds = 8f, revertToForm = "O2", pressAgainReverts = true,
        };
        var skill = FormSkill(spec, mp: 3, cd: 4f);
        var c = Make(new[] { F("O2", 2125, 10, true), F("O", 2125, 10), F("O3", 2125, 10) }, null, skill);

        c.TryFormChange(0, false);
        Assert.AreEqual("O", c.CurrentFormId);
        Assert.AreEqual(97, c.Mp);
        Assert.AreEqual(-1, c.TryFormChange(0, false), "쿨타임 중에는 재입력도 불가");
        c.TickCooldowns(4f);
        int mpBefore = c.Mp;
        Assert.AreEqual(0, c.TryFormChange(0, false));
        Assert.AreEqual("O2", c.CurrentFormId);
        Assert.AreEqual(mpBefore, c.Mp, "복귀는 mp 무소모");
    }

    // ── 상태연동(탄소): 최근 피격 ──

    [Test]
    public void StateBased_RecentHit_GivesDiamond()
    {
        var spec = new FormChangeSpec
        {
            keyIndex = 0, mode = FormChangeMode.StateBased, fromForms = new[] { "Graphite" },
            pressAgainReverts = true, revertToForm = "Graphite",
            stateRules = new[]
            {
                new FormStateRule { cond = FormStateCond.RecentlyHit, param = 3f, toForm = "Diamond", priority = 3 },
                new FormStateRule { cond = FormStateCond.Moving, toForm = "Graphene", priority = 2 },
                new FormStateRule { cond = FormStateCond.Always, toForm = "Fullerene", priority = 1 },
            },
        };
        var c = Make(new[] { F("Graphite", 1000, 10, true), F("Diamond", 1000, 10), F("Graphene", 1000, 10), F("Fullerene", 1000, 10) },
                     null, FormSkill(spec));
        c.TakeHit(5);
        c.TickStatus(1f, true);                                 // 1초 경과
        c.TryFormChange(0, true);
        Assert.AreEqual("Diamond", c.CurrentFormId, "최근 3초 내 피격이 이동보다 우선");
    }

    [Test]
    public void StateBased_NoRecentHit_Moving_GivesGraphene()
    {
        var spec = new FormChangeSpec
        {
            keyIndex = 0, mode = FormChangeMode.StateBased, fromForms = new[] { "Graphite" },
            stateRules = new[]
            {
                new FormStateRule { cond = FormStateCond.RecentlyHit, param = 3f, toForm = "Diamond", priority = 3 },
                new FormStateRule { cond = FormStateCond.Moving, toForm = "Graphene", priority = 2 },
            },
        };
        var c = Make(new[] { F("Graphite", 1000, 10, true), F("Diamond", 1000, 10), F("Graphene", 1000, 10) }, null, FormSkill(spec));
        c.TickStatus(10f, true);
        c.TryFormChange(0, true);
        Assert.AreEqual("Graphene", c.CurrentFormId);
    }

    [Test]
    public void SecondsSinceHitTaken_ResetsOnHit_AndGrowsWithTicks()
    {
        var c = Make(new[] { F("A", 1000, 10, true) });
        Assert.Greater(c.SecondsSinceHitTaken, 100f, "피격 이력 없음 = 매우 큼");
        c.TakeHit(1);
        Assert.AreEqual(0f, c.SecondsSinceHitTaken, 0.001f);
        c.TickStatus(5f, false);
        Assert.AreEqual(5f, c.SecondsSinceHitTaken, 0.001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 자동 전환 (TickForm)
    // ════════════════════════════════════════════════════════════════════════

    static FormAutoRule[] InertiaRules() => new[]
    {
        new FormAutoRule { cond = FormAutoCond.IdleFor,   param = 2f, toForm = "Mass",    priority = 1 },
        new FormAutoRule { cond = FormAutoCond.MovingFor, param = 2f, toForm = "Inertia", priority = 1 },
    };

    [Test]
    public void AutoForm_Idle2Seconds_SwitchesToMass_WithAutoCause()
    {
        var c = Make(new[] { F("Inertia", 1700, 10, true), F("Mass", 2975, 10) }, InertiaRules());
        FormChangeCause cause = default;
        c.OnFormChanged += (f, t, cs) => cause = cs;
        c.TickForm(1f, Idle);
        Assert.AreEqual("Inertia", c.CurrentFormId);
        c.TickForm(1.1f, Idle);
        Assert.AreEqual("Mass", c.CurrentFormId);
        Assert.AreEqual(FormChangeCause.Auto, cause);
    }

    [Test]
    public void AutoForm_StillWorksWhileStunned()
    {
        var c = Make(new[] { F("Inertia", 1700, 10, true), F("Mass", 2975, 10) }, InertiaRules());
        c.Status.Apply(new StatusEffect { Id = "stun", Kind = StatusKind.Stun, Duration = 30f }, true);
        c.TickForm(2.1f, Idle);
        Assert.AreEqual("Mass", c.CurrentFormId, "자동 전환은 스턴과 무관(§4-6-1 ④)");
    }

    [Test]
    public void AutoForm_OnSwitchIn_ResetsCounters()
    {
        var c = Make(new[] { F("Inertia", 1700, 10, true), F("Mass", 2975, 10) }, InertiaRules());
        c.TickForm(1.5f, Idle);
        c.OnSwitchIn();
        c.TickForm(1f, Idle);
        Assert.AreEqual("Inertia", c.CurrentFormId);
    }

    [Test]
    public void AutoForm_DownedCharacter_DoesNotEvaluate()
    {
        var c = Make(new[] { F("Inertia", 1700, 10, true), F("Mass", 2975, 10) }, InertiaRules());
        c.Damage(c.MaxHp);
        c.TickForm(5f, Idle);
        Assert.AreEqual("Inertia", c.CurrentFormId);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Party.ResetCombatState (§4-12)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Party_ResetCombatState_ClearsStatusAndForm_KeepsHpMpCooldown()
    {
        var skill = PlainSkill(1);
        var a = Make(new[] { F("A", 1000, 10, true), F("B", 1000, 10) }, null, skill);
        var b = Make(null);
        var party = new Party(new[] { a, b });
        a.ChangeForm("B", FormChangeCause.Manual);
        a.Status.Apply(new StatusEffect { Id = "b", Kind = StatusKind.StatMod, Stat = StatKind.Atk, Value = 2f, Duration = 9f, IsBeneficial = true }, true);
        b.Status.Apply(new StatusEffect { Id = "b", Kind = StatusKind.StatMod, Stat = StatKind.Atk, Value = 2f, Duration = 9f, IsBeneficial = true }, false);
        a.Damage(300); a.StartCooldown(0, 6f);

        party.ResetCombatState();

        Assert.AreEqual("A", a.CurrentFormId);
        Assert.AreEqual(0, a.Status.Count);
        Assert.AreEqual(0, b.Status.Count, "예약 버프도 소멸");
        Assert.AreEqual(700, a.Hp, "HP 유지(비율 이월)");
        Assert.AreEqual(6f, a.CooldownRemaining(0), 0.001f, "쿨타임 유지");
    }
}

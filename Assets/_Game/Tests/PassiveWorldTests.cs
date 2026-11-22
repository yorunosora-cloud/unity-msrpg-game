using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>설계도 22 P4 — Stage C: 월드 연결(PassiveHost·타격/피격 경로·적 마크·오라).</summary>
public class PassiveWorldTests : InputTestFixture
{
    Keyboard   _kb;
    GameObject _player;
    readonly List<GameObject> _spawned = new();

    public override void Setup()
    {
        base.Setup();
        _kb = InputSystem.AddDevice<Keyboard>();
        UIManager.Close();
        PlayerRuntime.Active = null; PlayerRuntime.Party = null; PlayerRuntime.Motion = default;
    }

    public override void TearDown()
    {
        foreach (var g in _spawned) if (g != null) Object.Destroy(g);
        _spawned.Clear();
        if (_player != null) Object.Destroy(_player);
        PlayerRuntime.Active = null; PlayerRuntime.Party = null;
        base.TearDown();
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    static SkillEffectEntry E(SkillEffectType t, TargetScope target, float value = 0f, float dur = 0f,
                              float area = 0f, int maxStacks = 0)
        => new SkillEffectEntry { type = t, target = target, value = value, duration = dur, areaRadius = area, maxStacks = maxStacks };

    static SkillDef Pas(string id, PassiveSpec spec)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = id; s.nameKo = id; s.skillType = SkillKind.Passive; s.slot = 1; s.passive = spec;
        return s;
    }

    static CombatCharacter Make(params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "pw" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 5000, atk = 40, def = 10, spd = 5, mp = 100 };
        def.skills = skills;
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1, exp = 0 });
        for (int i = 0; i < skills.Length; i++) c.Unlock(i);
        c.TickStatus(0f, true);
        return c;
    }

    GameObject MakePlayer(CombatCharacter c, out Party party)
    {
        party = new Party(new[] { c });
        PlayerRuntime.Active = c; PlayerRuntime.Party = party;
        var go = new GameObject("TestPlayer"); go.tag = "Player";
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
        go.AddComponent<PlayerSkills>();      // PassiveHost 자동 추가
        go.AddComponent<PlayerCombat>();
        _player = go;
        return go;
    }

    Enemy SpawnEnemy(Vector3 pos, float spd = 0f, bool freeze = true)
    {
        var go = new GameObject("E"); go.transform.position = pos;
        var e = go.AddComponent<Enemy>();
        e.SetWeakness(Continent.Math);
        typeof(Enemy).GetField("maxHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(e, 5000);
        e.SetSpd(spd);
        _spawned.Add(go);
        return e;
    }

    static void Freeze(Enemy e) => e.ApplyStatus(new StatusEffect { Id = "freeze", Kind = StatusKind.Stun, Duration = 999f });

    IEnumerator Tap(UnityEngine.InputSystem.Controls.KeyControl key)
    {
        Press(key); yield return null; yield return null;
        Release(key); yield return null;
    }

    // ════════════════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator PlayerSkills_AddsPassiveHost()
    {
        var c = Make();
        MakePlayer(c, out _);
        yield return null;
        Assert.IsNotNull(_player.GetComponent<PassiveHost>());
    }

    [UnityTest]
    public IEnumerator BasicAttack_RaisesOnHit_AndLorentzFormulaScalesDamage()
    {
        var c = Make(
            Pas("impulse", new PassiveSpec { trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerAction,
                stack = new StackSpec { stackId = "k", maxStacks = 5, perStackValue = 0.08f, decay = StackDecay.ResetAfterIdle, decaySeconds = 3f, stat = StatKind.SkillPower } }),
            Pas("lorentz", new PassiveSpec { formula = PassiveFormulaId.Lorentz, value = 2000f }));
        MakePlayer(c, out _);
        var enemy = SpawnEnemy(new Vector3(0, 0, 1.5f), spd: 1900f);
        yield return null; yield return null;
        Freeze(enemy);
        int hp0 = enemy.Unit.Hp;

        yield return Tap(_kb.qKey);

        Assert.AreEqual(1, c.PassiveStacks("impulse"), "기본기 적중 → OnHit 스택");
        int loss = hp0 - enemy.Unit.Hp;
        Assert.That(loss, Is.InRange(120, 128), "γ(1900/2000)≈3.2 → atk≈128, 방어 4 → 약 124");
    }

    [UnityTest]
    public IEnumerator Skill_EffectsPath_RaisesOnHit_AndAppliesHitMultiplier()
    {
        var skill = ScriptableObject.CreateInstance<SkillDef>();
        skill.id = "dmg"; skill.shape = SkillShape.Circle; skill.radius = 3f; skill.cooldown = 1f;
        skill.effects = new[] { E(SkillEffectType.Damage, TargetScope.EnemyArea, 1f) };
        var c = Make(skill, Pas("carbon", new PassiveSpec { trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerAction,
            stack = new StackSpec { stackId = "k", maxStacks = 4, perStackValue = 0.5f, decay = StackDecay.ResetAfterIdle, decaySeconds = 3f, stat = StatKind.SkillPower } }));
        MakePlayer(c, out _);
        var enemy = SpawnEnemy(new Vector3(0, 0, 1.5f));
        yield return null; yield return null;
        Freeze(enemy);

        int hp0 = enemy.Unit.Hp;
        SkillExecutor.Execute(skill, 0, c, _player.transform);          // 스택 0 → ×1.0 → 36
        int first = hp0 - enemy.Unit.Hp;
        Assert.AreEqual(1, c.PassiveStacks("carbon"));

        hp0 = enemy.Unit.Hp;
        SkillExecutor.Execute(skill, 0, c, _player.transform);          // 스택 1 → ×1.5 → 60 atk → 56
        int second = hp0 - enemy.Unit.Hp;
        Assert.Greater(second, first, "스택이 쌓이면 위력 증가");
        Assert.AreEqual(2, c.PassiveStacks("carbon"));
    }

    [UnityTest]
    public IEnumerator Skill_LegacyStrike_RaisesOnHit()
    {
        var strike = ScriptableObject.CreateInstance<SkillDef>();
        strike.id = "legacy"; strike.effectKind = SkillEffectKind.Strike; strike.range = 5f; strike.halfAngle = 90f;
        strike.damageMultiplier = 1f; strike.cooldown = 1f;
        var c = Make(strike, Pas("p", new PassiveSpec { trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerAction,
            stack = new StackSpec { stackId = "k", maxStacks = 5, perStackValue = 0.1f, decay = StackDecay.ResetAfterIdle, decaySeconds = 3f, stat = StatKind.SkillPower } }));
        MakePlayer(c, out _);
        var enemy = SpawnEnemy(new Vector3(0, 0, 2f));
        yield return null; yield return null;
        Freeze(enemy);

        SkillExecutor.Execute(strike, 0, c, _player.transform);
        Assert.AreEqual(1, c.PassiveStacks("p"));
    }

    [UnityTest]
    public IEnumerator HitTaken_ReflectAndKnockback_AreExecutedOnAttacker()
    {
        var c = Make(Pas("reaction", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHitTaken,
            effects = new[] { E(SkillEffectType.Reflect, TargetScope.Attacker, 0.2f), E(SkillEffectType.Knockback, TargetScope.Attacker, 2f) },
        }));
        MakePlayer(c, out _);
        var enemy = SpawnEnemy(new Vector3(0, 0, 3f));
        yield return null; yield return null;
        Freeze(enemy);
        int hp0 = enemy.Unit.Hp;
        Vector3 before = enemy.transform.position;

        c.TakeHit(100, enemy);

        Assert.AreEqual(hp0 - 16, enemy.Unit.Hp, "반사 20 → 방어 4 제외 16");
        Assert.AreEqual(2f, Vector3.Distance(before, enemy.transform.position), 0.01f, "작용반작용: 공격자 넉백 2.0");
        Assert.Greater(enemy.transform.position.z, before.z, "플레이어 반대 방향으로");
    }

    [UnityTest]
    public IEnumerator Compton_AreaDamage_HitsNeighboursOnly_AndDoesNotRecurse()
    {
        var c = Make(
            Pas("compton", new PassiveSpec { trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerTarget,
                effects = new[] { E(SkillEffectType.Damage, TargetScope.HitTarget, 0.5f, area: 1f) } }),
            Pas("count", new PassiveSpec { trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerAction,
                stack = new StackSpec { stackId = "k", maxStacks = 10, perStackValue = 0.01f, decay = StackDecay.ResetAfterIdle, decaySeconds = 9f, stat = StatKind.SkillPower } }));
        MakePlayer(c, out _);
        var a = SpawnEnemy(new Vector3(0, 0, 2.0f));     // 기본기 사거리 안
        var b = SpawnEnemy(new Vector3(0, 0, 2.9f));     // 기본기 사거리 밖, A에서 0.9
        yield return null; yield return null;
        Freeze(a); Freeze(b);
        int aHp = a.Unit.Hp, bHp = b.Unit.Hp;

        yield return Tap(_kb.qKey);

        Assert.AreEqual(aHp - 36, a.Unit.Hp, "A는 기본기만 받는다(광역 중복 없음)");
        Assert.AreEqual(bHp - 16, b.Unit.Hp, "B는 콤프턴 광역(atk×0.5=20 → 16)");
        Assert.AreEqual(1, c.PassiveStacks("count"), "콤프턴 유발 피해는 OnHit을 다시 일으키지 않는다");
    }

    [UnityTest]
    public IEnumerator ZeroVector_TeleportsToCombatStartPosition()
    {
        var c = Make(Pas("zero", new PassiveSpec
        {
            trigger = PassiveTriggerType.HpBelowPercent, value = 0.4f, internalCooldown = 60f,
            effects = new[] { E(SkillEffectType.Teleport, TargetScope.Self) },
        }));
        MakePlayer(c, out var party);
        var enemy = SpawnEnemy(new Vector3(0, 0, 1.5f));
        yield return null; yield return null;
        Freeze(enemy);

        yield return Tap(_kb.qKey);                                   // 교전 시작 → 앵커 기록(원점)
        Assert.IsTrue(party.Anchor.HasAnchor);

        var cc = _player.GetComponent<CharacterController>();
        cc.enabled = false; _player.transform.position = new Vector3(10, 0, 0); cc.enabled = true;
        c.Damage((int)(c.MaxHp * 0.7f));                              // 40% 아래로

        yield return null;
        Assert.AreEqual(0f, _player.transform.position.x, 0.05f, "전투 시작 위치로 복귀");
    }

    [UnityTest]
    public IEnumerator Aura_SlowsEnemiesInsideRadius_OnlyWhileActive()
    {
        var c = Make(Pas("aura", new PassiveSpec
        {
            modifiers = new[] { new PassiveModifierSpec { source = ModifierSource.Constant, stat = StatKind.MoveSpeed, value = 0.85f, enemyAuraRadius = 6f } },
        }));
        MakePlayer(c, out _);
        var near = SpawnEnemy(new Vector3(3, 0, 0));
        var far  = SpawnEnemy(new Vector3(9, 0, 0));
        yield return null; yield return null;
        Freeze(near); Freeze(far);
        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(0.85f, near.Status.Mult(StatKind.MoveSpeed), 0.001f);
        Assert.AreEqual(1f,    far.Status.Mult(StatKind.MoveSpeed),  0.001f, "반경 밖은 영향 없음");
    }

    [UnityTest]
    public IEnumerator Axis_MarkAppliedOnHit_ThenDistanceFormula_AndMarkExpires()
    {
        var c = Make(Pas("axis", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerTarget,
            formula = PassiveFormulaId.AxisDistance, value = 0.3f,
            effects = new[] { E(SkillEffectType.Mark, TargetScope.HitTarget, 0f, dur: 0.6f) },
        }));
        MakePlayer(c, out _);
        var enemy = SpawnEnemy(new Vector3(0, 0, 1.5f));
        yield return null; yield return null;
        Freeze(enemy);

        yield return Tap(_kb.qKey);
        Assert.IsTrue(enemy.HasMark("axis"), "적중한 적이 회전축 마크를 받는다");
        // 적이 얼기 전 몇 프레임 움직였을 수 있으므로 실제 위치 기준으로 거리를 만든다
        Vector3 ep = enemy.transform.position;
        Assert.AreEqual(1.3f, c.FormulaMultiplier(enemy, 3f, ep - new Vector3(0, 0, 5f)), 0.001f, "거리 5 → +30%");
        Assert.AreEqual(1.0f, c.FormulaMultiplier(enemy, 3f, ep), 0.001f, "거리 0 → +0%");

        yield return new WaitForSeconds(0.8f);
        Assert.IsFalse(enemy.HasMark("axis"), "지속시간 후 마크 소멸");
    }

    [UnityTest]
    public IEnumerator Oxidation_EnemyDamageTakenStacks_CapAtThree()
    {
        var c = Make(Pas("ox", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerTarget,
            effects = new[] { E(SkillEffectType.DamageTakenMult, TargetScope.HitTarget, 1.03f, dur: 5f, maxStacks: 3) },
        }));
        MakePlayer(c, out _);
        var enemy = SpawnEnemy(new Vector3(0, 0, 1.5f));
        yield return null; yield return null;

        for (int i = 0; i < 4; i++)
            c.NotifyHits(new List<HitInfo> { new HitInfo { target = enemy, actionId = CombatCharacter.NewActionId(), damage = 5 } }, false);
        yield return null;

        Assert.AreEqual(1.09f, enemy.Status.Mult(StatKind.DamageTaken), 0.001f, "스택 3 × 3% = +9%");
    }

    [UnityTest]
    public IEnumerator Enemy_ApproachSign_PositiveWhenChasingPlayer()
    {
        var c = Make();
        MakePlayer(c, out _);
        _player.transform.position = new Vector3(0, 0, 8f);
        var enemy = SpawnEnemy(Vector3.zero, freeze: false);
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(1f, enemy.ApproachSign(_player.transform.position), "플레이어를 향해 이동 중 → +1");
        Assert.AreEqual(-1f, enemy.ApproachSign(new Vector3(0, 0, -8f)), "반대편 기준에서는 후퇴 → −1");
    }

    [UnityTest]
    public IEnumerator TakeHit_RecordsCombatAnchor()
    {
        var c = Make();
        MakePlayer(c, out var party);
        _player.transform.position = new Vector3(4, 0, 4);
        yield return null;
        c.TakeHit(5);
        Assert.IsTrue(party.Anchor.HasAnchor);
        Assert.AreEqual(4f, party.Anchor.StartPosition.x, 0.01f);
    }
}

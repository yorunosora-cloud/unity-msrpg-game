using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>23-C — 기본기(Q)가 효과 목록을 쓴다: 출혈·카운터 소모·적중 시 아군 효과·크리티컬(가득 시 추가 피해).</summary>
public class BasicEffectsTests : InputFixtureBase
{
    Keyboard _kb;
    readonly List<GameObject> _spawned = new();

    public override void Setup()
    {
        base.Setup();
        _kb = InputSystem.AddDevice<Keyboard>();
        UIManager.Close();
        PlayerRuntime.Active = null; PlayerRuntime.Party = null;
    }

    public override void TearDown()
    {
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
        PlayerRuntime.Active = null; PlayerRuntime.Party = null;
        base.TearDown();
    }

    static SkillDef Q(params SkillEffectEntry[] effects)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "q"; s.nameKo = "q"; s.slot = 0; s.startUnlocked = true; s.mpCost = 0; s.cooldown = 0.5f;
        s.shape = SkillShape.Cone; s.range = 4f; s.halfAngle = 60f; s.damageMultiplier = 1f; s.effectKind = SkillEffectKind.Strike;
        s.effects = effects;
        return s;
    }

    static SkillEffectEntry Dmg(float v, string counter = null, CounterMode mode = CounterMode.None, float coeff = 0f, bool consume = false, bool full = false)
        => new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = v, counterId = counter, counterMode = mode, counterCoeff = coeff, counterConsume = consume, requireCounterFull = full };

    static CombatCharacter Make(SkillDef q, CounterSpec[] counters = null)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "bq" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 500, atk = 100, def = 10, spd = 5, mp = 100 };
        def.skills = new[] { q }; def.counters = counters;
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1 });
        c.Unlock(0); c.TickStatus(0f, true);
        return c;
    }

    Enemy Spawn(float z = 1.5f)
    {
        var go = new GameObject("E"); go.transform.position = new Vector3(0, 0, z); _spawned.Add(go);
        var e = go.AddComponent<Enemy>();
        typeof(Enemy).GetField("maxHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(e, 1000000);
        return e;
    }

    Transform Origin() { var o = new GameObject("Caster"); _spawned.Add(o); return o.transform; }

    // ── 직접 실행 ─────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator ExecuteBasic_AppliesDamageAndBleed_AndCountsAsABasicAttack()
    {
        var q = Q(Dmg(0.8f), new SkillEffectEntry { type = SkillEffectType.Dot, target = TargetScope.EnemyArea, value = 0.15f, duration = 3f });
        var counter = new CounterSpec { id = "b", maxStacks = 5, gainOn = CounterTrigger.OnBasicAttack };
        var c = Make(q, new[] { counter });
        var e = Spawn(); yield return null;
        int before = e.CurrentHp;
        SkillExecutor.ExecuteBasic(q, 0, c, Origin());
        Assert.Less(e.CurrentHp, before, "피해");
        Assert.IsTrue(e.Status.Has("q:Dot:Atk"), "출혈(도트) 부여");
        Assert.AreEqual(1, c.CounterValue("b"), "기본기 적중으로 집계된다(OnBasicAttack)");
    }

    [UnityTest]
    public IEnumerator ExecuteBasic_CounterScalesDamage_AndConsumes()
    {
        var q = Q(Dmg(1f, "charge", CounterMode.Linear, 0.1f, consume: true));
        var spec = new CounterSpec { id = "charge", maxStacks = 10, gainOn = CounterTrigger.MovingSecond };
        var c0 = Make(q, new[] { spec });
        var e0 = Spawn(); yield return null;
        int b0 = e0.CurrentHp; SkillExecutor.ExecuteBasic(q, 0, c0, Origin()); int d0 = b0 - e0.CurrentHp;

        var c5 = Make(q, new[] { spec }); c5.Counter("charge").Add(5);
        var e5 = Spawn(1.2f); yield return null;
        int b5 = e5.CurrentHp; SkillExecutor.ExecuteBasic(q, 0, c5, Origin()); int d5 = b5 - e5.CurrentHp;
        Assert.Greater(d5, d0, "스택 5 → 위력 +50%");
        Assert.AreEqual(0, c5.CounterValue("charge"), "기본기가 스택을 소모");
    }

    [UnityTest]
    public IEnumerator ExecuteBasic_AllyEffectsApplyOnlyWhenSomethingWasHit()
    {
        var q = Q(Dmg(1f), new SkillEffectEntry { type = SkillEffectType.RestoreMp, target = TargetScope.Self, value = 0.1f });
        var c = Make(q);
        c.UseMp(60);
        int mp = c.Mp;
        SkillExecutor.ExecuteBasic(q, 0, c, Origin());   // 적 없음
        Assert.AreEqual(mp, c.Mp, "빗나가면 아군 효과 없음");

        var e = Spawn(); yield return null;
        SkillExecutor.ExecuteBasic(q, 0, c, Origin());
        Assert.Greater(c.Mp, mp, "적중하면 MP 회복");
    }

    [UnityTest]
    public IEnumerator ExecuteBasic_CritEveryThirdHit_ViaFullCounterExtraDamage()
    {
        // 업쿼크: 두 번 적중으로 가득(2) → 세 번째 타격에 추가 피해(=크리티컬), 그 뒤 초기화
        var q = Q(Dmg(1f), Dmg(1f, "up", full: true, consume: true));
        var spec = new CounterSpec { id = "up", maxStacks = 2, gainOn = CounterTrigger.OnHit };
        var c = Make(q, new[] { spec });
        var e = Spawn(); yield return null;
        var o = Origin();
        var dmg = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            int b = e.CurrentHp; SkillExecutor.ExecuteBasic(q, 0, c, o); dmg.Add(b - e.CurrentHp);
        }
        Assert.Greater(dmg[2], dmg[0] * 1.6f, "세 번째는 약 두 배");
        Assert.AreEqual(dmg[0], dmg[1], dmg[0] * 0.1f + 1f);
        Assert.AreEqual(dmg[0], dmg[3], dmg[0] * 0.1f + 1f, "초기화 후 네 번째는 다시 일반 피해");
    }

    // ── Q 키 연결 ─────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator QKey_WithEffects_UsesTheEffectListInsteadOfThePlainBasic()
    {
        var q = Q(Dmg(1f), new SkillEffectEntry { type = SkillEffectType.Dot, target = TargetScope.EnemyArea, value = 0.15f, duration = 3f });
        var active = Make(q);
        PlayerRuntime.Active = active; PlayerRuntime.Party = new Party(new[] { active });
        var player = new GameObject("TestPlayer"); player.tag = "Player"; _spawned.Add(player);
        player.AddComponent<PlayerSkills>();
        player.AddComponent<PlayerCombat>();
        var e = Spawn(1.5f);
        yield return null; yield return null;

        for (int attempt = 0; attempt < 3 && !e.Status.Has("q:Dot:Atk"); attempt++)
        {
            Press(_kb.qKey); yield return null; yield return null; Release(_kb.qKey); yield return null;
        }
        Assert.IsTrue(e.Status.Has("q:Dot:Atk"), "Q 키가 효과 목록(출혈)을 적용한다");
    }
}

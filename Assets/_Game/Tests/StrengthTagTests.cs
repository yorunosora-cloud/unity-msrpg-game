using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>설계도 18-A — Stage B·C: 해금 모델, 적 속성, 강점 태그 배율, 정밀 처치 등급.</summary>
public class StrengthTagTests : InputFixtureBase
{
    Keyboard   _kb;
    GameObject _player;
    readonly System.Collections.Generic.List<GameObject> _spawned = new();

    public override void Setup()
    {
        base.Setup();
        _kb = InputSystem.AddDevice<Keyboard>();
        UIManager.Close();
        TagRuleTable.ResetForTests();
        PlayerRuntime.Active = null; PlayerRuntime.Party = null;
    }

    public override void TearDown()
    {
        foreach (var g in _spawned) if (g != null) Object.Destroy(g);
        _spawned.Clear();
        if (_player != null) Object.Destroy(_player);
        PlayerRuntime.Active = null; PlayerRuntime.Party = null;
        TagRuleTable.ResetForTests();
        base.TearDown();
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    static SkillDef Strike(string id, string[] tags = null, int slot = -1)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = id; s.nameKo = id; s.slot = slot; s.effectKind = SkillEffectKind.Strike;
        s.range = 6f; s.halfAngle = 90f; s.damageMultiplier = 1f; s.cooldown = 0.2f;
        s.strengthTags = tags;
        return s;
    }

    static CombatCharacter Make(params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "st" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 5000, atk = 40, def = 10, spd = 5, mp = 100 };
        def.skills = skills;
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1, exp = 0 });
        for (int i = 0; i < skills.Length; i++) c.Unlock(i);
        return c;
    }

    GameObject MakePlayer(CombatCharacter c)
    {
        PlayerRuntime.Active = c; PlayerRuntime.Party = new Party(new[] { c });
        var go = new GameObject("TestPlayer"); go.tag = "Player";
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
        go.AddComponent<PlayerSkills>();
        go.AddComponent<PlayerCombat>();
        _player = go;
        return go;
    }

    Enemy SpawnEnemy(Vector3 pos, int hp = 5000, AttributeProfile attrs = default)
    {
        var go = new GameObject("E"); go.transform.position = pos;
        var e = go.AddComponent<Enemy>();
        e.SetWeakness(Continent.Math);
        typeof(Enemy).GetField("maxHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(e, hp);
        e.SetAttributes(attrs);
        _spawned.Add(go);
        return e;
    }

    static void Freeze(Enemy e) => e.ApplyStatus(new StatusEffect { Id = "freeze", Kind = StatusKind.Stun, Duration = 999f });

    IEnumerator Tap(UnityEngine.InputSystem.Controls.KeyControl key)
    {
        Press(key); yield return null; yield return null;
        Release(key); yield return null;
    }

    int Loss(Enemy e, int hp0) => hp0 - e.Unit.Hp;

    // ════════════════════════════════════════════════════════════════════════
    // 스키마 · 해금 모델
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void SkillDef_HasNewFields_WithSafeDefaults()
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        Assert.IsTrue(s.strengthTags == null || s.strengthTags.Length == 0);
        Assert.IsFalse(s.startUnlocked);
        Assert.IsTrue(string.IsNullOrEmpty(s.unlockCond));
        Assert.IsTrue(string.IsNullOrEmpty(s.skillMotion));
    }

    static CombatCharacter MakeUnlockChar(params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "u" + Random.Range(0, 99999); def.baseStats = new CharacterStats { hp = 10, atk = 1, def = 1, spd = 1, mp = 1 };
        def.skills = skills;
        return new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1 });
    }

    [Test]
    public void Unlock_StartUnlockedSkills_AreOpenFromTheStart_OthersNeedUnlock()
    {
        var a = Strike("a"); a.startUnlocked = true;
        var b = Strike("b");
        var c = Strike("c"); c.startUnlocked = true;
        var ch = MakeUnlockChar(a, b, c);
        Assert.IsTrue(ch.IsUnlocked(0));
        Assert.IsFalse(ch.IsUnlocked(1), "startUnlocked가 아닌 스킬은 R&E 해금 전까지 잠김");
        Assert.IsTrue(ch.IsUnlocked(2));
        ch.Unlock("b");
        Assert.IsTrue(ch.IsUnlocked(1));
    }

    [Test]
    public void Unlock_LegacyCharacter_KeepsIndexZeroRule()
    {
        var ch = MakeUnlockChar(Strike("a"), Strike("b"));
        Assert.IsTrue(ch.IsUnlocked(0));
        Assert.IsFalse(ch.IsUnlocked(1));
    }

    [Test]
    public void Unlock_WithStartUnlockedPresent_IndexZeroIsNotAutomaticallyOpen()
    {
        var a = Strike("a");                       // 인덱스 0이지만 startUnlocked 아님
        var b = Strike("b"); b.startUnlocked = true;
        var ch = MakeUnlockChar(a, b);
        Assert.IsFalse(ch.IsUnlocked(0), "새 모델에서는 인덱스 0 자동 해금 규칙이 없다");
        Assert.IsTrue(ch.IsUnlocked(1));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 적 속성
    // ════════════════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator Enemy_Attributes_AreSettable_AndExposedViaInterface()
    {
        var e = SpawnEnemy(Vector3.zero, attrs: new AttributeProfile { mass = 5, velocity = 6, temp = 7, charge = -8 });
        yield return null;
        ISkillTarget t = e;
        Assert.AreEqual(7, t.Attributes.temp);
        Assert.AreEqual(-8, t.Attributes.charge);
    }

    [UnityTest]
    public IEnumerator Boss_Attributes_DefaultToZero()
    {
        var go = new GameObject("Boss"); _spawned.Add(go);
        var boss = go.AddComponent<Boss>();
        var def = ScriptableObject.CreateInstance<BossDef>(); def.maxHp = 100;
        typeof(Boss).GetField("_def", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boss, def);
        yield return null;
        Assert.AreEqual(default(AttributeProfile), ((ISkillTarget)boss).Attributes);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 강점 태그 배율 (스킬)
    // ════════════════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator Tag_HotEnemyTakesMoreThanRoomTemperatureEnemy()
    {
        var skill = Strike("fire", new[] { "고온" });
        var c = Make(skill);
        MakePlayer(c);
        var hot  = SpawnEnemy(new Vector3(0, 0, 2), attrs: new AttributeProfile { temp = 900 });
        yield return null; yield return null;
        Freeze(hot);
        int h0 = hot.Unit.Hp;
        SkillExecutor.Execute(skill, 0, c, _player.transform);
        int hotLoss = Loss(hot, h0);

        Object.Destroy(hot.gameObject);
        yield return null;
        var warm = SpawnEnemy(new Vector3(0, 0, 2), attrs: new AttributeProfile { temp = 300 });
        yield return null; yield return null;
        Freeze(warm);
        int w0 = warm.Unit.Hp;
        c.TickCooldowns(5f);
        SkillExecutor.Execute(skill, 0, c, _player.transform);
        int warmLoss = Loss(warm, w0);

        Assert.AreEqual(36, warmLoss, "상온(300) = 배율 1.0 → atk 40 − def 4");
        Assert.AreEqual(60, hotLoss, "고온(900) = ×1.6 → atk 64 − def 4");
    }

    [UnityTest]
    public IEnumerator Tag_NoTags_IsUnchangedFromBefore()
    {
        var skill = Strike("plain");
        var c = Make(skill);
        MakePlayer(c);
        var e = SpawnEnemy(new Vector3(0, 0, 2), attrs: new AttributeProfile { temp = 900, mass = 900 });
        yield return null; yield return null;
        Freeze(e);
        int h0 = e.Unit.Hp;
        SkillExecutor.Execute(skill, 0, c, _player.transform);
        Assert.AreEqual(36, Loss(e, h0), "태그가 없으면 속성과 무관(회귀 없음)");
    }

    [UnityTest]
    public IEnumerator Tag_UnknownTagIgnored_AndMultipleTagsMultiply()
    {
        var unknown = Strike("u", new[] { "없는태그" });
        var multi   = Strike("m", new[] { "고온", "고질량" });
        var c = Make(unknown, multi);
        MakePlayer(c);
        var e = SpawnEnemy(new Vector3(0, 0, 2), attrs: new AttributeProfile { temp = 900, mass = 300 });
        yield return null; yield return null;
        Freeze(e);

        int h0 = e.Unit.Hp;
        SkillExecutor.Execute(unknown, 0, c, _player.transform);
        Assert.AreEqual(36, Loss(e, h0), "모르는 태그는 무시");

        c.TickCooldowns(5f);
        h0 = e.Unit.Hp;
        SkillExecutor.Execute(multi, 1, c, _player.transform);
        // 고온 1.6 × 고질량 min(1+0.005×200, 1.8)=1.8 = 2.88 → atk 115 → 111
        Assert.AreEqual(111, Loss(e, h0), "두 태그의 배율이 곱해진다");
    }

    [UnityTest]
    public IEnumerator Tag_AppliesToEffectsPathDamage()
    {
        var skill = ScriptableObject.CreateInstance<SkillDef>();
        skill.id = "fx"; skill.shape = SkillShape.Circle; skill.radius = 4f; skill.cooldown = 1f;
        skill.strengthTags = new[] { "고온" };
        skill.effects = new[] { new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 1f } };
        var c = Make(skill);
        MakePlayer(c);
        var e = SpawnEnemy(new Vector3(0, 0, 2), attrs: new AttributeProfile { temp = 900 });
        yield return null; yield return null;
        Freeze(e);
        int h0 = e.Unit.Hp;
        SkillExecutor.Execute(skill, 0, c, _player.transform);
        Assert.AreEqual(60, Loss(e, h0));
    }

    [Test]
    public void BasicAttack_Resolve_CarriesSlotZeroStrengthTags()
    {
        var basic = Strike("basic", new[] { "고온" }, slot: 0);
        var c = Make(basic);
        var spec = BasicAttack.Resolve(c, 2.2f, 60f, 0.5f);
        CollectionAssert.AreEqual(new[] { "고온" }, spec.strengthTags);
        Assert.IsNull(BasicAttack.Resolve(Make(), 2.2f, 60f, 0.5f).strengthTags);
    }

    [UnityTest]
    public IEnumerator Tag_AppliesToBasicAttack()
    {
        var basic = Strike("basic", new[] { "고온" }, slot: 0);
        basic.range = 3f; basic.cooldown = 0.1f;
        var c = Make(basic);
        MakePlayer(c);
        var e = SpawnEnemy(new Vector3(0, 0, 1.5f), attrs: new AttributeProfile { temp = 900 });
        yield return null; yield return null;
        Freeze(e);
        int h0 = e.Unit.Hp;
        yield return Tap(_kb.qKey);
        Assert.AreEqual(60, Loss(e, h0), "기본기도 강점 태그를 읽는다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 정밀 처치 등급
    // ════════════════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator Kill_ExactDamage_IsPerfectGrade()
    {
        var e = SpawnEnemy(new Vector3(0, 0, 50), hp: 36);          // atk 40 − def 4 = 36
        yield return null;
        e.ReceiveHit(Continent.Physics, 40);
        Assert.AreEqual(2.0f, e.LastKillGrade, 0.0001f);
    }

    [UnityTest]
    public IEnumerator Kill_Overkill_GradesByRatio()
    {
        var good = SpawnEnemy(new Vector3(0, 0, 50), hp: 33);       // 3/33 ≈ 9% → 1.5
        var fair = SpawnEnemy(new Vector3(0, 0, 51), hp: 29);       // 7/29 ≈ 24% → 1.2
        var norm = SpawnEnemy(new Vector3(0, 0, 52), hp: 20);       // 16/20 = 80% → 1.0
        yield return null;
        good.ReceiveHit(Continent.Physics, 40);
        fair.ReceiveHit(Continent.Physics, 40);
        norm.ReceiveHit(Continent.Physics, 40);
        Assert.AreEqual(1.5f, good.LastKillGrade, 0.0001f);
        Assert.AreEqual(1.2f, fair.LastKillGrade, 0.0001f);
        Assert.AreEqual(1.0f, norm.LastKillGrade, 0.0001f);
    }

    [UnityTest]
    public IEnumerator Kill_GradeUsesFinalDamage_IncludingVulnerability()
    {
        var e = SpawnEnemy(new Vector3(0, 0, 50), hp: 72);
        yield return null;
        e.ApplyStatus(new StatusEffect { Id = "v", Kind = StatusKind.StatMod, Stat = StatKind.DamageTaken, Value = 2f, Duration = 9f });
        e.ReceiveHit(Continent.Physics, 40);                         // 36 × 2 = 72 = 정확히 HP
        Assert.AreEqual(2.0f, e.LastKillGrade, 0.0001f);
    }

    [UnityTest]
    public IEnumerator NonLethalHit_DoesNotSetGrade()
    {
        var e = SpawnEnemy(new Vector3(0, 0, 50), hp: 500);
        yield return null;
        e.ReceiveHit(Continent.Physics, 40);
        Assert.AreEqual(1.0f, e.LastKillGrade, 0.0001f, "처치하지 않으면 기본값");
    }

    [Test]
    public void RewardAmount_AppliesGradeWithMinimumOne()
    {
        Assert.AreEqual(4, Enemy.RewardAmount(40, 1.0f));
        Assert.AreEqual(8, Enemy.RewardAmount(40, 2.0f));
        Assert.AreEqual(6, Enemy.RewardAmount(40, 1.5f));
        Assert.AreEqual(1, Enemy.RewardAmount(0, 1.0f), "최소 1");
    }
}

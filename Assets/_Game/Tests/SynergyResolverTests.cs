using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>23-B 단계 B — 표식 연계 발동 판정·실행.</summary>
public class SynergyResolverTests
{
    readonly List<GameObject> _spawned = new();
    readonly List<SynergyComboDef> _fired = new();
    SynergyComboTable _table;

    [SetUp]
    public void SetUp()
    {
        _fired.Clear();
        SynergyResolver.ComboTriggered += OnCombo;
    }

    [TearDown]
    public void TearDown()
    {
        SynergyResolver.ComboTriggered -= OnCombo;
        SynergyResolver.Table = null;
        SynergyTuning.Reset();
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
    }

    void OnCombo(SynergyComboDef d, CombatCharacter c) => _fired.Add(d);

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    static SkillDef Trigger(string id, bool alsoMarksSameId = false, bool withDamage = true)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = id; s.nameKo = id; s.slot = 1; s.mpCost = 0; s.cooldown = 0f; s.effectKind = SkillEffectKind.Strike;
        s.shape = SkillShape.Cone; s.range = 6f; s.halfAngle = 60f;
        var list = new List<SkillEffectEntry>();
        if (withDamage) list.Add(new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 1f });
        else list.Add(new SkillEffectEntry { type = SkillEffectType.RestoreMp, target = TargetScope.Self, value = 0.01f });
        if (alsoMarksSameId) list.Add(new SkillEffectEntry { type = SkillEffectType.Mark, target = TargetScope.EnemyArea, markId = "mk", duration = 8f });
        s.effects = list.ToArray();
        return s;
    }

    static SkillDef ComboSkill(float dmg = 2.5f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "combo_skill"; s.nameKo = "연계기"; s.skillType = SkillKind.Combo; s.slot = 0; s.mpCost = 0; s.cooldown = 0f;
        s.shape = SkillShape.Cone; s.range = 3f; s.halfAngle = 180f;   // 트리거(6)보다 좁아 범위 밖 적은 연계기를 맞지 않는다
        s.effects = new[] { new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = dmg } };
        return s;
    }

    CombatCharacter Make(string charId, SkillDef trigger, SkillDef combo, out SynergyComboDef def)
    {
        var cd = ScriptableObject.CreateInstance<CharacterDef>();
        cd.id = charId; cd.nameKo = "c"; cd.rarity = Rarity.N; cd.continent = Continent.Physics;
        cd.baseStats = new CharacterStats { hp = 500, atk = 100, def = 10, spd = 5, mp = 100 };
        cd.skills = new[] { trigger, combo };
        var c = new CombatCharacter(cd, new OwnedCharacter { id = charId, level = 1 });
        c.Unlock(0); c.Unlock(1); c.UseMp(50); c.TickStatus(0f, true);
        def = new SynergyComboDef { id = "combo_test", comboName = "테스트 · F=ma", formula = "F=ma", markId = "mk", consumerId = charId, triggerSkillId = trigger.id, comboSkill = combo };
        _table = ScriptableObject.CreateInstance<SynergyComboTable>();
        _table.combos = new[] { def };
        SynergyResolver.Table = _table;
        return c;
    }

    Enemy Spawn(float x = 0f, float z = 2f)
    {
        var go = new GameObject("E"); go.transform.position = new Vector3(x, 0, z); _spawned.Add(go);
        var e = go.AddComponent<Enemy>();
        typeof(Enemy).GetField("maxHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(e, 1000000);
        return e;
    }

    Transform Origin() { var o = new GameObject("Caster"); _spawned.Add(o); return o.transform; }

    // ── 발동 ──────────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator MarkedEnemyHitByTrigger_ConsumesMark_FiresComboDamage_AndEvent()
    {
        var trig = Trigger("trig"); var combo = ComboSkill();
        var c = Make("consumer", trig, combo, out _);
        var plain = Spawn(0f, 5f); var marked = Spawn(0.5f, 2f); yield return null;   // plain: 트리거 범위 안·연계기 범위 밖
        marked.AddMark("mk", 8f);

        int pb = plain.CurrentHp, mb = marked.CurrentHp;
        SkillExecutor.Execute(trig, 0, c, Origin());
        int plainDmg = pb - plain.CurrentHp, markedDmg = mb - marked.CurrentHp;

        Assert.IsFalse(marked.HasMark("mk"), "표식 소멸");
        Assert.AreEqual(1, _fired.Count, "이벤트 1회");
        Assert.Greater(markedDmg, plainDmg * 2f, "연계기(2.5배)가 추가로 터진다");
    }

    [UnityTest]
    public IEnumerator NoMark_NoCombo()
    {
        var trig = Trigger("trig"); var c = Make("consumer", trig, ComboSkill(), out _);
        var e = Spawn(); yield return null;
        SkillExecutor.Execute(trig, 0, c, Origin());
        Assert.AreEqual(0, _fired.Count);
    }

    [UnityTest]
    public IEnumerator WrongConsumer_OrWrongTriggerSkill_DoesNotFire()
    {
        var trig = Trigger("trig"); var other = Trigger("other");
        var c = Make("consumer", trig, ComboSkill(), out var def);
        var e = Spawn(); yield return null; e.AddMark("mk", 8f);

        def.consumerId = "somebody_else"; _table.combos[0] = def;
        SkillExecutor.Execute(trig, 0, c, Origin());
        Assert.AreEqual(0, _fired.Count, "소모자 불일치");
        Assert.IsTrue(e.HasMark("mk"), "표식 유지");

        def.consumerId = "consumer"; def.triggerSkillId = "other"; _table.combos[0] = def;
        SkillExecutor.Execute(trig, 0, c, Origin());
        Assert.AreEqual(0, _fired.Count, "트리거 스킬 불일치");
    }

    [UnityTest]
    public IEnumerator MarkedEnemyOutsideTheSkillShape_IsUntouched()
    {
        var trig = Trigger("trig"); var c = Make("consumer", trig, ComboSkill(), out _);
        var near = Spawn(0f, 2f); var far = Spawn(0f, 30f); yield return null;
        far.AddMark("mk", 8f);
        SkillExecutor.Execute(trig, 0, c, Origin());
        Assert.AreEqual(0, _fired.Count);
        Assert.IsTrue(far.HasMark("mk"));
    }

    [UnityTest]
    public IEnumerator SeveralMarkedEnemies_AllLoseTheMark_ComboFiresOnce()
    {
        var trig = Trigger("trig"); var c = Make("consumer", trig, ComboSkill(), out _);
        var a = Spawn(-0.5f, 2f); var b = Spawn(0.5f, 2f); yield return null;
        a.AddMark("mk", 8f); b.AddMark("mk", 8f);
        SkillExecutor.Execute(trig, 0, c, Origin());
        Assert.IsFalse(a.HasMark("mk")); Assert.IsFalse(b.HasMark("mk"));
        Assert.AreEqual(1, _fired.Count);
    }

    [UnityTest]
    public IEnumerator TheTriggersOwnFreshMark_DoesNotTriggerItself()
    {
        var trig = Trigger("trig", alsoMarksSameId: true); var c = Make("consumer", trig, ComboSkill(), out _);
        var e = Spawn(); yield return null;   // 사전 표식 없음
        SkillExecutor.Execute(trig, 0, c, Origin());
        Assert.AreEqual(0, _fired.Count, "방금 건 표식으로 자기 연계를 일으키지 않는다");
        Assert.IsTrue(e.HasMark("mk"), "새 표식은 남는다");
    }

    [UnityTest]
    public IEnumerator FieldSkillWithoutHits_UsesTheSkillShape_ToFindMarkedEnemies()
    {
        var trig = Trigger("trig", withDamage: false); var c = Make("consumer", trig, ComboSkill(), out _);
        var e = Spawn(); yield return null; e.AddMark("mk", 8f);
        SkillExecutor.Execute(trig, 0, c, Origin());
        Assert.AreEqual(1, _fired.Count, "피해가 없는 필드형 스킬은 형태 안의 적으로 판정(열에너지)");
        Assert.IsFalse(e.HasMark("mk"));
    }

    [UnityTest]
    public IEnumerator ComboSkill_DoesNotRetrigger_Recursively()
    {
        var trig = Trigger("trig"); var combo = ComboSkill();
        var c = Make("consumer", trig, combo, out var def);
        def.triggerSkillId = combo.id; _table.combos[0] = def;     // 연계기 자신이 트리거인 잘못된 데이터
        var e = Spawn(); yield return null; e.AddMark("mk", 8f);
        Assert.DoesNotThrow(() => SkillExecutor.Execute(combo, 1, c, Origin()));
        Assert.LessOrEqual(_fired.Count, 1, "재귀로 무한 발동하지 않는다");
    }

    [Test]
    public void Table_FindsByConsumerAndTriggerSkill()
    {
        var t = ScriptableObject.CreateInstance<SynergyComboTable>();
        t.combos = new[] { new SynergyComboDef { id = "a", consumerId = "x", triggerSkillId = "s1" }, new SynergyComboDef { id = "b", consumerId = "x", triggerSkillId = "s2" } };
        Assert.AreEqual("b", t.Find("x", "s2").id);
        Assert.IsNull(t.Find("x", "s3"));
        Assert.IsNull(t.Find("y", "s1"));
        Assert.IsNull(new SynergyComboTable().Find("x", "s1"), "비어 있어도 안전");
    }
}

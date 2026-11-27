using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>23-B 단계 C — 출시 CSV의 표식 연계 4건을 실제 데이터로 끝까지 검증한다(표식 → 트리거 → 연계기).</summary>
public class SynergyShippedTests
{
    readonly List<GameObject> _spawned = new();
    readonly List<string> _fired = new();
    ImportResult _res;

    [SetUp]
    public void SetUp()
    {
        _res = ShippedCsvTests.Load();
        var table = ScriptableObject.CreateInstance<SynergyComboTable>();
        table.combos = _res.combos.ToArray();
        SynergyResolver.Table = table;
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

    void OnCombo(SynergyComboDef d, CombatCharacter c) => _fired.Add(d.id);

    CombatCharacter Caster(string charId)
    {
        var def = _res.characters.Single(c => c.id == charId);
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1 });
        for (int i = 0; i < def.skills.Length; i++) c.Unlock(i);
        c.TickStatus(0f, true);
        return c;
    }

    Enemy Spawn(float z = 1.5f)
    {
        var go = new GameObject("E"); go.transform.position = new Vector3(0, 0, z); _spawned.Add(go);
        var e = go.AddComponent<Enemy>();
        typeof(Enemy).GetField("maxHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(e, 10000000);
        return e;
    }

    void Cast(CombatCharacter c, string skillId)
    {
        int idx = System.Array.FindIndex(_res.characters.Single(x => x.id == c.Id).skills, s => s.id == skillId);
        var origin = new GameObject("Caster"); _spawned.Add(origin);
        SkillExecutor.Execute(c.SkillAt(idx), idx, c, origin.transform);
    }

    [Test]
    public void FourCombos_AreShipped_WithTheirComboSkills()
    {
        CollectionAssert.AreEquivalent(new[] { "combo_vx_v", "combo_v_a", "combo_J_dp", "combo_W_dke" }, _res.combos.Select(c => c.id));
        Assert.IsTrue(_res.combos.All(c => c.comboSkill != null && c.comboSkill.skillType == SkillKind.Combo));
        Assert.AreEqual(2.5f, _res.combos.Single(c => c.id == "combo_vx_v").comboSkill.effects[0].value);
        Assert.AreEqual(3f, _res.combos.Single(c => c.id == "combo_v_a").comboSkill.effects[0].value);
        Assert.AreEqual(2.8f, _res.combos.Single(c => c.id == "combo_W_dke").comboSkill.effects[0].value);
        Assert.AreEqual(3.5f, _res.combos.Single(c => c.id == "combo_W_dke").comboSkill.radius);
    }

    [UnityTest]
    public IEnumerator Chain_Vektor_To_Velocia_To_Axel_FiresBothCombos()
    {
        var e = Spawn(); yield return null;

        Cast(Caster("vektor_sr"), "vektor_s2");            // 변위벡터합성
        Assert.IsTrue(e.HasMark("vektor_disp"));

        Cast(Caster("velocia_sr"), "velocia_s1");          // 순간속도 → 속도 도출 + 속도 표식
        CollectionAssert.AreEqual(new[] { "combo_vx_v" }, _fired);
        Assert.IsFalse(e.HasMark("vektor_disp"), "변위 표식 소모");
        Assert.IsTrue(e.HasMark("velocia_v"), "같은 행동에서 새 속도 표식이 걸린다(자기 연계는 일으키지 않음)");

        Cast(Caster("axel_sr"), "axel_s2");                // 등가속운동 → 가속도 도출
        CollectionAssert.AreEqual(new[] { "combo_vx_v", "combo_v_a" }, _fired);
        Assert.IsFalse(e.HasMark("velocia_v"));
    }

    [UnityTest]
    public IEnumerator Impulsa_To_Momenta_FiresTheImpulseMomentumTheorem()
    {
        var e = Spawn(); yield return null;
        Cast(Caster("impulsa_sr"), "impulsa_s2");
        Assert.AreEqual(6f, e.MarkRemaining("impulsa_j"), 0.05f);
        Cast(Caster("momenta_sr"), "momenta_s2");
        CollectionAssert.AreEqual(new[] { "combo_J_dp" }, _fired);
        Assert.IsFalse(e.HasMark("impulsa_j"));
    }

    [UnityTest]
    public IEnumerator Worka_To_Energia_ThermalEnergyFieldSkill_FiresTheWorkEnergyTheorem()
    {
        var e = Spawn(); yield return null;
        Cast(Caster("worka_r"), "worka_s2");
        Assert.IsTrue(e.HasMark("worka_w"));
        Cast(Caster("energia_sr"), "energia_s3k");          // 피해 없는 장판형 → 형태 안의 적으로 판정
        CollectionAssert.AreEqual(new[] { "combo_W_dke" }, _fired);
        Assert.IsFalse(e.HasMark("worka_w"));
    }

    [UnityTest]
    public IEnumerator WrongCharacter_CannotConsumeAnotherPairsMark()
    {
        var e = Spawn(); yield return null;
        Cast(Caster("vektor_sr"), "vektor_s2");
        Cast(Caster("axel_sr"), "axel_s2");                 // 액셀은 속도 표식용 — 변위 표식에는 반응하지 않는다
        Assert.AreEqual(0, _fired.Count);
        Assert.IsTrue(e.HasMark("vektor_disp"));
    }
}

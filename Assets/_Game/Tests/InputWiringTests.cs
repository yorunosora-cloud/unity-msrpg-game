using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>
/// 설계도 22 P1 — 입력 연결 통합 테스트.
/// Input System 가상 키보드로 R/T/F/V/G/B·1~5·Q 입력이 실제 컴포넌트
/// (PlayerSkills · PartyController · PlayerCombat)를 거쳐 동작하는지 검증한다.
/// </summary>
public class InputWiringTests : InputFixtureBase
{
    Keyboard   _kb;
    GameObject _player;
    GameObject _enemy;

    public override void Setup()
    {
        base.Setup();
        _kb = InputSystem.AddDevice<Keyboard>();
        UIManager.Close();   // 다른 테스트가 열어 둔 패널 상태 초기화
        PlayerRuntime.Active = null;
        PlayerRuntime.Party  = null;
    }

    public override void TearDown()
    {
        if (_enemy  != null) Object.Destroy(_enemy);
        if (_player != null) Object.Destroy(_player);
        PlayerRuntime.Active = null;
        PlayerRuntime.Party  = null;
        base.TearDown();
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    static SkillDef MakeSkill(string id, int slot = -1, float cd = 5f,
                              float range = 3f, float halfAngle = 60f, float mult = 1f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = id; s.nameKo = id; s.slot = slot;
        s.effectKind = SkillEffectKind.Strike;
        s.mpCost = 0; s.cooldown = cd;
        s.range = range; s.halfAngle = halfAngle; s.damageMultiplier = mult;
        return s;
    }

    static CombatCharacter MakeChar(params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "wire_" + Random.Range(0, 99999);
        def.nameKo = "배선테스트"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.skills = skills;
        def.baseStats = new CharacterStats { hp = 500, atk = 50, def = 10, spd = 10, mp = 100 };
        return new CombatCharacter(def, new OwnedCharacter { id = "t", level = 1, exp = 0 });
    }

    IEnumerator Tap(UnityEngine.InputSystem.Controls.KeyControl key)
    {
        Press(key);
        yield return null; yield return null;   // 2프레임 유지: 입력 처리·Update 순서에 덜 민감
        Release(key);
        yield return null;
    }


    /// <summary>
    /// 키를 눌러 done()이 될 때까지 최대 3번 시도한다. 전체 스위트 실행 시 입력 이벤트가 가끔 누락되는
    /// 테스트 하네스 특성을 흡수하기 위한 것(게임 로직과 무관). done이 이미 참이면 더 누르지 않는다.
    /// </summary>
    IEnumerator TapUntil(UnityEngine.InputSystem.Controls.KeyControl key, System.Func<bool> done)
    {
        for (int attempt = 0; attempt < 3 && !done(); attempt++)
            yield return Tap(key);
    }

    GameObject MakePlayer(CombatCharacter active, Party party, bool withSkills, bool withCombat)
    {
        PlayerRuntime.Active = active;
        PlayerRuntime.Party  = party;
        var go = new GameObject("TestPlayer");
        go.tag = "Player";
        if (withSkills) go.AddComponent<PlayerSkills>();   // BoxCharacterBuilder 자동 추가
        if (withCombat) go.AddComponent<PlayerCombat>();
        return go;
    }

    // ════════════════════════════════════════════════════════════════════════
    // 스킬 키: R/T/F/V/G/B → 슬롯 1~6, 이전 키 U는 더 이상 동작하지 않는다
    // ════════════════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator SkillKey_R_CastsSlot1_UKeyDoesNot()
    {
        var active = MakeChar(MakeSkill("a"));                 // 레거시: 슬롯 1 = R
        _player = MakePlayer(active, new Party(new[] { active }), withSkills: true, withCombat: false);
        yield return null;

        yield return Tap(_kb.uKey);
        Assert.AreEqual(0f, active.CooldownRemaining(0), 0.0001f, "U 키는 더 이상 스킬을 발동하지 않는다");

        yield return Tap(_kb.rKey);
        Assert.Greater(active.CooldownRemaining(0), 0f, "R 키가 슬롯 1(첫 스킬)을 발동한다");
    }

    [UnityTest]
    public IEnumerator SkillKey_T_CastsSlot2_WhenUnlocked()
    {
        var first  = MakeSkill("a");
        var second = MakeSkill("b");
        var active = MakeChar(first, second);                   // 레거시: 슬롯 1=R, 슬롯 2=T
        _player = MakePlayer(active, new Party(new[] { active }), withSkills: true, withCombat: false);
        yield return null;

        yield return Tap(_kb.tKey);
        Assert.AreEqual(0f, active.CooldownRemaining(1), 0.0001f, "잠긴 스킬은 T 키로도 발동하지 않는다");

        active.Unlock(1);
        yield return Tap(_kb.tKey);
        Assert.Greater(active.CooldownRemaining(1), 0f, "해금 후 T 키가 슬롯 2를 발동한다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 파티: 1~5 키 전환, 전환 쿨타임 1초
    // ════════════════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator PartyKeys_Digit5_SwitchesToFifthMember_ThenCooldownBlocksDigit1()
    {
        _player = new GameObject("TestPlayer");
        _player.tag = "Player";
        _player.AddComponent<PlayerSkills>();       // Party.Tick(전환 쿨타임 진행) 담당
        _player.AddComponent<PartyController>();    // CharacterDatabase에서 파티 구성
        yield return null;                          // Start 실행
        yield return null;

        var party = PlayerRuntime.Party;
        Assert.IsNotNull(party, "PartyController가 파티를 구성해야 한다");
        Assert.AreEqual(5, party.Members.Count, "파티는 최대 5명까지 구성된다");

        yield return TapUntil(_kb.digit5Key, () => party.ActiveIndex == 4);
        Assert.AreEqual(4, party.ActiveIndex, "5 키로 다섯 번째 멤버로 전환");

        yield return Tap(_kb.digit1Key);
        Assert.AreEqual(4, party.ActiveIndex, "전환 쿨타임(1초) 중에는 전환되지 않는다");

        yield return new WaitForSeconds(1.2f);
        yield return TapUntil(_kb.digit1Key, () => party.ActiveIndex == 0);
        Assert.AreEqual(0, party.ActiveIndex, "쿨타임이 지나면 1 키로 첫 멤버로 전환");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 기본기 Q: 캐릭터별 사거리 (슬롯 0 스킬) vs 레거시 고정 사거리
    // ════════════════════════════════════════════════════════════════════════

    GameObject SpawnEnemyAhead(float distance)
    {
        var go = new GameObject("TestEnemy");
        go.transform.position = new Vector3(0f, 0f, distance);   // 플레이어(원점)의 전방(+Z)
        go.AddComponent<Enemy>();
        return go;
    }

    [UnityTest]
    public IEnumerator BasicAttack_Q_Slot0Skill_ReachesFartherThanLegacyRange()
    {
        // 슬롯 0 기본기: 사거리 4.5, 배율 2 → 거리 3.5의 적을 타격(레거시 고정 사거리 2.2로는 불가)
        var basic  = MakeSkill("basic", slot: 0, cd: 0.1f, range: 4.5f, halfAngle: 60f, mult: 2f);
        var active = MakeChar(basic);
        _player = MakePlayer(active, new Party(new[] { active }), withSkills: false, withCombat: true);
        _enemy  = SpawnEnemyAhead(3.5f);
        yield return null;
        yield return null;                                       // Enemy.Start (EnemyUnit 생성)

        var enemy = _enemy.GetComponent<Enemy>();
        Assert.AreEqual(enemy.Unit.MaxHp, enemy.Unit.Hp, "시작 시 적 체력 가득");

        yield return Tap(_kb.qKey);
        Assert.IsTrue(_enemy == null || enemy.Unit.Hp < enemy.Unit.MaxHp,
                      "슬롯 0 기본기(사거리 4.5)가 거리 3.5의 적을 타격해야 한다");
    }

    [UnityTest]
    public IEnumerator BasicAttack_Q_LegacyCharacter_UsesFixedRange_MissesFarEnemy()
    {
        // 슬롯 0 스킬이 없는 레거시 캐릭터: 인스펙터 고정 사거리 2.2 → 거리 3.5의 적은 맞지 않는다
        var active = MakeChar(MakeSkill("a"));                   // 슬롯 1(R)만 있음
        _player = MakePlayer(active, new Party(new[] { active }), withSkills: false, withCombat: true);
        _enemy  = SpawnEnemyAhead(3.5f);
        yield return null;
        yield return null;

        var enemy = _enemy.GetComponent<Enemy>();
        yield return Tap(_kb.qKey);
        Assert.AreEqual(enemy.Unit.MaxHp, enemy.Unit.Hp, "레거시 고정 사거리(2.2) 밖의 적은 맞지 않는다");
    }
}

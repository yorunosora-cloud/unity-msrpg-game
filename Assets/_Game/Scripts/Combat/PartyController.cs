using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 파티 빌드 및 1/2/3 키 캐릭터 교체 관리. Player GameObject에 부착.
/// - Start(): 로스터 앞 3명(없으면 DB 앞 3명)으로 Party 생성 → PlayerRuntime 갱신
/// - Update(): 1/2/3 키 입력 → Party.SwitchTo()
/// - 활성 캐릭터 기절 감지 → AutoSwitchToNext(). 전멸 시 PlayerDeath에 위임.
/// </summary>
[RequireComponent(typeof(BoxCharacterBuilder))]
public class PartyController : MonoBehaviour
{
    BoxCharacterBuilder _builder;
    Party               _party;

    void Awake()
    {
        _builder = GetComponent<BoxCharacterBuilder>();
    }

    void Start()
    {
        BuildParty();

        // 서버 로드 후 로스터가 갱신될 수 있으므로 재구성
        if (MetaState.Roster != null)
            MetaState.Roster.OnChanged += OnRosterChanged;
    }

    void OnDestroy()
    {
        if (MetaState.Roster != null)
            MetaState.Roster.OnChanged -= OnRosterChanged;
        UnsubscribeActiveChanged();
    }

    void OnRosterChanged()
    {
        // 서버 로드 완료 시 로스터가 채워진 경우 파티 재구성
        if (MetaState.Roster != null && MetaState.Roster.Owned.Count > 0)
            BuildParty();
    }

    void BuildParty()
    {
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (db == null)
        {
            Debug.LogWarning("[PartyController] CharacterDatabase를 Resources에서 찾을 수 없습니다.");
            return;
        }

        var members = new List<CombatCharacter>();

        // 1순위: 로스터 앞 3명
        var roster = MetaState.Roster;
        if (roster != null)
        {
            foreach (var owned in roster.Owned)
            {
                if (members.Count >= 3) break;
                var def = db.ById(owned.id);
                if (def == null) continue;
                members.Add(new CombatCharacter(def, owned));
            }
        }

        // 폴백: DB 앞 3명 (로스터가 비었거나 로드 전)
        if (members.Count == 0)
        {
            int fallbackCount = 0;
            foreach (var def in db.All)
            {
                if (def == null) continue;
                if (fallbackCount >= 3) break;
                var owned = new OwnedCharacter { id = def.id, level = 1, exp = 0 };
                members.Add(new CombatCharacter(def, owned));
                fallbackCount++;
            }
        }

        if (members.Count == 0)
        {
            Debug.LogWarning("[PartyController] 파티 구성 가능한 캐릭터가 없습니다.");
            return;
        }

        UnsubscribeActiveChanged();

        _party                = new Party(members);
        PlayerRuntime.Party   = _party;
        PlayerRuntime.Active  = _party.Active;

        _party.OnActiveChanged += OnActiveChanged;

        // 각 멤버 기절 감지 구독
        foreach (var m in _party.Members)
            m.OnChanged += OnMemberChanged;

        ApplyActiveTint();
        Debug.Log($"[PartyController] 파티 구성 완료: {members.Count}명, 첫 활성={_party.Active?.DisplayName}");
    }

    void UnsubscribeActiveChanged()
    {
        if (_party == null) return;
        _party.OnActiveChanged -= OnActiveChanged;
        foreach (var m in _party.Members)
            m.OnChanged -= OnMemberChanged;
    }

    // ── 입력 ──────────────────────────────────────────────────────────────

    void Update()
    {
        if (_party == null) return;
        if (UIManager.IsAnyPanelOpen) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) TrySwitchTo(0);
        else if (kb.digit2Key.wasPressedThisFrame) TrySwitchTo(1);
        else if (kb.digit3Key.wasPressedThisFrame) TrySwitchTo(2);
    }

    void TrySwitchTo(int i)
    {
        if (_party == null || i >= _party.Members.Count) return;
        _party.SwitchTo(i); // 성공 시 OnActiveChanged가 발생
    }

    // ── 기절 감지 ─────────────────────────────────────────────────────────

    void OnMemberChanged(string reason)
    {
        if (_party == null) return;
        if (!PlayerRuntime.Active.IsDowned) return; // 활성 캐릭터만 확인

        // 전멸 여부 먼저 확인 → PlayerDeath에 위임 (아무것도 안 함)
        if (_party.AllDowned) return;

        // 살아있는 다음 멤버로 자동 교체
        _party.AutoSwitchToNext();
    }

    // ── 활성 변경 ─────────────────────────────────────────────────────────

    void OnActiveChanged()
    {
        if (_party == null) return;
        PlayerRuntime.Active = _party.Active;
        ApplyActiveTint();
    }

    void ApplyActiveTint()
    {
        if (_party?.Active == null) return;
        _builder?.SetBodyTint(_party.Active.TintColor);
    }
}

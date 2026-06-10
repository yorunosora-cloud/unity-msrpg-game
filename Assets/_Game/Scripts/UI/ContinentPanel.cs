using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 패널이 활성화될 때 현재 대륙 테마 색을 Image에 적용한다.
/// 파티 활성 캐릭터가 바뀌면 즉시 갱신.
/// </summary>
[RequireComponent(typeof(Image))]
public class ContinentPanel : MonoBehaviour
{
    Image _img;
    Party _party;

    void Awake() => _img = GetComponent<Image>();

    void OnEnable()
    {
        ConnectParty();
        ApplyTheme();
    }

    void OnDisable()
    {
        if (_party != null) { _party.OnActiveChanged -= ApplyTheme; _party = null; }
    }

    void ConnectParty()
    {
        var p = PlayerRuntime.Party;
        if (p == null || p == _party) return;
        if (_party != null) _party.OnActiveChanged -= ApplyTheme;
        _party = p;
        _party.OnActiveChanged += ApplyTheme;
    }

    void ApplyTheme()
    {
        if (_img == null) _img = GetComponent<Image>();
        _img.color = UIThemeRuntime.ActivePanelBg;
    }
}

using UnityEngine;
using TMPro;

/// <summary>
/// 상단 재화 HUD — Wallet.OnChanged 구독해 자동 갱신.
/// MetaUISetup 에디터 메뉴가 자동으로 배치합니다.
/// </summary>
public class CurrencyHud : MonoBehaviour
{
    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text paperText;
    [SerializeField] TMP_Text focusText;
    [SerializeField] TMP_Text fragmentText;

    void Start()
    {
        if (!MetaState.IsInitialized) return;
        Refresh();
        MetaState.Wallet.OnChanged += Refresh;
    }

    void OnDestroy()
    {
        if (MetaState.IsInitialized)
            MetaState.Wallet.OnChanged -= Refresh;
    }

    void Refresh()
    {
        var w = MetaState.Wallet;
        if (goldText)     goldText.text     = $"골드 {w.Gold:N0}";
        if (paperText)    paperText.text    = $"논문 {w.Paper}";
        if (focusText)    focusText.text    = $"집중 {w.Focus}";
        if (fragmentText) fragmentText.text = $"조각 {w.Fragment}";
    }
}

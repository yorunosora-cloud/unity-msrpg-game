using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도서관 — 지식 탐구 패널 (가챠 정식 UI).
/// <para>
/// <see cref="MetaPanelController.OpenLibrary"/>가 SetActive(true) 하면 <see cref="OnEnable"/>이 발동.
/// 닫기 버튼 → SetActive(false) → <see cref="OnDisable"/> → <see cref="UIManager.Close"/> 호출.
/// </para>
/// </summary>
public class GachaPanel : MonoBehaviour
{
    [Header("정보 라벨")]
    [SerializeField] TMP_Text paperText;      // 논문 잔액 (실시간)
    [SerializeField] TMP_Text pityText;       // 천장 카운터

    [Header("버튼")]
    [SerializeField] Button singleButton;
    [SerializeField] Button tenButton;
    [SerializeField] Button closeButton;

    [Header("비용 표시")]
    [SerializeField] TMP_Text singleCostText;
    [SerializeField] TMP_Text tenCostText;

    [Header("결과")]
    [SerializeField] GameObject resultArea;
    [SerializeField] TMP_Text   resultText;
    [SerializeField] TMP_Text   statusText;

    GachaService _svc;

    void OnEnable()
    {
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (db != null && MetaState.IsInitialized)
        {
            _svc = new GachaService(db, MetaState.Wallet, MetaState.Roster,
                                    MetaState.GachaState, MetaState.Crystals);
            MetaState.Wallet.OnChanged += RefreshPaper;
        }

        if (singleCostText) singleCostText.text = $"논문 {GachaConfig.CostSingle}";
        if (tenCostText)    tenCostText.text    = $"논문 {GachaConfig.CostTen}";
        if (resultArea)     resultArea.SetActive(false);
        RefreshPaper();
        RefreshPity();
        ClearStatus();
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Wallet.OnChanged -= RefreshPaper;
        UIManager.Close();
    }

    // ── 버튼 이벤트 ───────────────────────────────────────────────────────

    public void OnSingleClicked()
    {
        if (_svc == null) return;
        var result = _svc.RollOne();
        if (result == null) { ShowStatus("논문이 부족합니다.", Color.red); return; }
        ShowResults(new[] { result.Value });
    }

    public void OnTenClicked()
    {
        if (_svc == null) return;
        var results = _svc.RollTen();
        if (results == null) { ShowStatus("논문이 부족합니다.", Color.red); return; }
        ShowResults(results);
    }

    public void OnCloseClicked() => gameObject.SetActive(false);

    // ── 내부 ──────────────────────────────────────────────────────────────

    void ShowResults(GachaResult[] results)
    {
        if (resultArea) resultArea.SetActive(true);

        var sb = new System.Text.StringBuilder();
        foreach (var r in results)
        {
            string name   = r.def != null ? r.def.nameKo : "???";
            string tag    = r.isNew ? "★ 신규" : "중복";
            string rLabel = RarityLabel(r.rarity);
            sb.Append($"[{rLabel}]  {name}  — {tag}");

            if (!r.isNew && r.crystal.HasValue && r.crystalAmount > 0)
                sb.Append($"  → {CrystalCatalog.DisplayName(r.crystal.Value)} ×{r.crystalAmount}");

            sb.AppendLine();
        }
        if (resultText) resultText.text = sb.ToString().TrimEnd();

        RefreshPaper();
        RefreshPity();
        ClearStatus();
    }

    void RefreshPaper()
    {
        if (paperText && MetaState.IsInitialized)
            paperText.text = $"논문: {MetaState.Wallet.Paper}";
    }

    void RefreshPity()
    {
        if (pityText && MetaState.IsInitialized)
            pityText.text = $"천장 {MetaState.GachaState.PityCounter} / {GachaConfig.PityCap}";
    }

    static string RarityLabel(Rarity r) => r switch
    {
        Rarity.UR  => "UR ✦",
        Rarity.SSR => "SSR",
        Rarity.SR  => "SR",
        Rarity.R   => "R",
        _          => "N",
    };

    void ShowStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    void ClearStatus()
    {
        if (statusText) statusText.text = "";
    }
}

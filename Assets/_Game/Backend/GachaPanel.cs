using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>가챠 뽑기 패널. MetaPanelController가 활성화 여부를 제어합니다.</summary>
public class GachaPanel : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] Button singleButton;
    [SerializeField] Button tenButton;
    [SerializeField] Button closeButton;

    [Header("비용 표시")]
    [SerializeField] TMP_Text singleCostText;
    [SerializeField] TMP_Text tenCostText;
    [SerializeField] TMP_Text pityText;

    [Header("결과")]
    [SerializeField] GameObject resultArea;
    [SerializeField] TMP_Text   resultText;
    [SerializeField] TMP_Text   statusText;

    GachaService _svc;

    void OnEnable()
    {
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (db != null && MetaState.IsInitialized)
            _svc = new GachaService(db, MetaState.Wallet, MetaState.Roster,
                                    MetaState.GachaState, MetaState.Crystals);

        if (singleCostText) singleCostText.text = $"논문 {GachaConfig.CostSingle}";
        if (tenCostText)    tenCostText.text    = $"논문 {GachaConfig.CostTen}";
        if (resultArea)     resultArea.SetActive(false);
        RefreshPity();
        ClearStatus();
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
            string name = r.def != null ? r.def.nameKo : "???";
            string tag  = r.isNew ? "[신규]" : "[중복]";
            sb.Append($"[{r.rarity}] {name}  {tag}");

            // 중복 결정 지급 표기
            if (!r.isNew && r.crystal.HasValue && r.crystalAmount > 0)
                sb.Append($"  → {CrystalCatalog.DisplayName(r.crystal.Value)} x{r.crystalAmount}");

            sb.AppendLine();
        }
        if (resultText) resultText.text = sb.ToString().TrimEnd();

        RefreshPity();
        ClearStatus();
    }

    void RefreshPity()
    {
        if (pityText && MetaState.IsInitialized)
            pityText.text = $"천장 {MetaState.GachaState.PityCounter} / {GachaConfig.PityCap}";
    }

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

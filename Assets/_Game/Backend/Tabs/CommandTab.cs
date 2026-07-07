using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자 패널 A탭 — 커맨드 (재화·레벨·퀘스트·가챠·계정).
/// 본인 계정 로컬 처리. CloudScript 불필요.
/// </summary>
public class CommandTab : MonoBehaviour
{
    [Header("재화 지급")]
    [SerializeField] TMP_Dropdown   kindDropdown;
    [SerializeField] TMP_InputField amountInput;
    [SerializeField] Button         giveCurrencyButton;

    [Header("레벨 조작")]
    [SerializeField] TMP_Dropdown   charDropdown;
    [SerializeField] TMP_InputField levelInput;
    [SerializeField] Button         applyLevelButton;

    [Header("퀘스트")]
    [SerializeField] TMP_InputField questIdInput;
    [SerializeField] Button         forceQuestButton;

    [Header("가챠 디버그")]
    [SerializeField] Button rollOneButton;
    [SerializeField] Button rollTenButton;

    [Header("계정")]
    [SerializeField] Button resetButton;
    [SerializeField] Button saveButton;
    [SerializeField] Button loadButton;

    [Header("셸 참조")]
    [SerializeField] AdminPanel owner;

    static readonly CurrencyKind[] _currencyMap =
    {
        CurrencyKind.Gold,
        CurrencyKind.Paper,
        CurrencyKind.Focus,
        CurrencyKind.Fragment,
    };

    static readonly CrystalKind[] _crystalMap =
        (CrystalKind[])Enum.GetValues(typeof(CrystalKind));

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Start()
    {
        if (giveCurrencyButton != null) giveCurrencyButton.onClick.AddListener(OnGiveCurrency);
        if (applyLevelButton   != null) applyLevelButton.onClick.AddListener(OnApplyLevel);
        if (forceQuestButton   != null) forceQuestButton.onClick.AddListener(OnForceQuest);
        if (rollOneButton      != null) rollOneButton.onClick.AddListener(OnRollOne);
        if (rollTenButton      != null) rollTenButton.onClick.AddListener(OnRollTen);
        if (resetButton        != null) resetButton.onClick.AddListener(OnReset);
        if (saveButton         != null) saveButton.onClick.AddListener(OnForceSave);
        if (loadButton         != null) loadButton.onClick.AddListener(OnForceLoad);
    }

    void OnEnable()
    {
        RefreshKindDropdown();
        RefreshCharDropdown();
    }

    // ── 드롭다운 초기화 ───────────────────────────────────────────────────

    void RefreshKindDropdown()
    {
        if (kindDropdown == null) return;
        var opts = new List<string> { "골드", "논문", "집중력", "조각" };
        foreach (CrystalKind ck in _crystalMap)
            opts.Add("결정 — " + ck);
        kindDropdown.ClearOptions();
        kindDropdown.AddOptions(opts);
    }

    void RefreshCharDropdown()
    {
        if (charDropdown == null || !MetaState.IsInitialized) return;
        var db   = Resources.Load<CharacterDatabase>("CharacterDatabase");
        var opts = new List<string>();
        foreach (var owned in MetaState.Roster.Owned)
        {
            var def = db != null ? db.ById(owned.id) : null;
            opts.Add(def != null ? def.nameKo : owned.id);
        }
        charDropdown.ClearOptions();
        charDropdown.AddOptions(opts);
    }

    // ── 재화 지급 ─────────────────────────────────────────────────────────

    void OnGiveCurrency()
    {
        if (!MetaState.IsInitialized) return;
        if (!int.TryParse(amountInput != null ? amountInput.text : "1000", out int amount) || amount <= 0)
            amount = 1000;

        int idx = kindDropdown != null ? kindDropdown.value : 0;
        if (idx < _currencyMap.Length)
        {
            MetaState.Wallet.Add(_currencyMap[idx], amount);
            SaveWithStatus($"+{_currencyMap[idx]} {amount:N0}");
        }
        else
        {
            int ci = idx - _currencyMap.Length;
            if (ci >= 0 && ci < _crystalMap.Length)
            {
                MetaState.Crystals.Add(_crystalMap[ci], amount);
                SaveWithStatus($"+결정 {_crystalMap[ci]} {amount}");
            }
        }
    }

    // ── 레벨 조작 ─────────────────────────────────────────────────────────

    void OnApplyLevel()
    {
        if (!MetaState.IsInitialized || charDropdown == null) return;
        int charIdx = charDropdown.value;
        var owned   = MetaState.Roster.Owned;
        if (charIdx < 0 || charIdx >= owned.Count) return;

        if (!int.TryParse(levelInput != null ? levelInput.text : "1", out int lvl) || lvl < 1)
            lvl = 1;

        owned[charIdx].level = lvl;
        owned[charIdx].exp   = 0;
        MetaState.Roster.NotifyChanged();
        SaveWithStatus($"{owned[charIdx].id} Lv.{lvl} 적용");
    }

    // ── 퀘스트 강제완료 (미구현) ──────────────────────────────────────────

    void OnForceQuest()
    {
        owner?.ShowStatus("퀘스트 시스템 미구현", Color.yellow);
    }

    // ── 가챠 디버그 ───────────────────────────────────────────────────────

    void OnRollOne()
    {
        var svc = MakeGachaService();
        if (svc == null) return;
        MetaState.Wallet.Add(CurrencyKind.Paper, GachaConfig.CostSingle);
        var r = svc.RollOne();
        if (r.HasValue)
            owner?.ShowStatus(
                $"[{r.Value.rarity}] {r.Value.def?.nameKo ?? "?"} {(r.Value.isNew ? "[신규]" : "[중복]")}",
                Color.white);
        else
            owner?.ShowStatus("뽑기 실패", Color.red);
    }

    void OnRollTen()
    {
        var svc = MakeGachaService();
        if (svc == null) return;
        MetaState.Wallet.Add(CurrencyKind.Paper, GachaConfig.CostTen);
        var results = svc.RollTen();
        if (results == null) { owner?.ShowStatus("뽑기 실패", Color.red); return; }

        var sb = new System.Text.StringBuilder();
        foreach (var r in results)
            sb.AppendLine($"[{r.rarity}] {r.def?.nameKo ?? "?"} {(r.isNew ? "[신규]" : "")}");
        owner?.ShowStatus(sb.ToString().TrimEnd(), Color.white);
    }

    // ── 계정 ──────────────────────────────────────────────────────────────

    void OnReset()
    {
        MetaState.Init();
        owner?.ShowStatus("계정 초기화 완료 (저장 안 됨)", Color.red);
    }

    void OnForceSave()
    {
        MetaSaveService.Save(
            ()  => owner?.ShowStatus("저장 완료", Color.green),
            err => owner?.ShowStatus($"저장 실패: {err}", Color.red));
    }

    void OnForceLoad()
    {
        MetaSaveService.Load(
            ()  => { owner?.ShowStatus("불러오기 완료", Color.green); RefreshCharDropdown(); },
            err => owner?.ShowStatus($"불러오기 실패: {err}", Color.red));
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

    GachaService MakeGachaService()
    {
        if (!MetaState.IsInitialized) return null;
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (db == null) return null;
        return new GachaService(db, MetaState.Wallet, MetaState.Roster,
                                MetaState.GachaState, MetaState.Crystals);
    }

    void SaveWithStatus(string msg)
    {
        owner?.ShowStatus($"{msg} 저장 중...", Color.white);
        MetaSaveService.Save(
            ()  => owner?.ShowStatus($"{msg} (저장 완료)", Color.green),
            err => owner?.ShowStatus($"{msg} (저장 실패: {err})", Color.yellow));
    }
}

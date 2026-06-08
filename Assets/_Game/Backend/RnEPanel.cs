using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// K키 전용 R&amp;E (Research &amp; Education) 패널.
/// 캐릭터 그리드(스킬 보유 캐릭터만, 3열 전체폭) → 카드 클릭 시 우측에 상세 패널 슬라이드.
/// 상세 패널: 과목 연구 자원 소모 레벨업 + 스킬 해금(문제 풀이).
/// </summary>
public class RnEPanel : MonoBehaviour
{
    // ── 카드 크기 상수 ────────────────────────────────────────────────────
    const float CARD_H      = 200f;
    const float CARD_W_3COL = 316f;   // 3열: (980 - 16pad - 16spacing) / 3
    const float CARD_W_1COL = 374f;   // 1열: 390 - 16pad

    // ── 참조 ─────────────────────────────────────────────────────────────
    [Header("그리드")]
    [SerializeField] RectTransform   charScrollViewRt;
    [SerializeField] RectTransform   charGridContent;
    [SerializeField] GridLayoutGroup gridLayout;

    [Header("상세 패널 (카드 선택 시 우측 슬라이드)")]
    [SerializeField] GameObject  detailPanel;
    [SerializeField] Image       detailPortrait;
    [SerializeField] TMP_Text    detailNameText;
    [SerializeField] TMP_Text    detailContinentText;
    [SerializeField] TMP_Text    detailLevelText;
    [SerializeField] TMP_Text    detailMaterialText;
    [SerializeField] Button      levelUpButton;
    [SerializeField] TMP_Text    levelUpBtnLabel;

    [Header("스킬 목록")]
    [SerializeField] RectTransform skillListContent;

    [Header("문제 오버레이 (스킬 해금)")]
    [SerializeField] GameObject     problemOverlay;
    [SerializeField] TMP_Text       promptText;
    [SerializeField] GameObject     multipleChoiceArea;
    [SerializeField] Button[]       choiceButtons;
    [SerializeField] TMP_Text[]     choiceLabels;
    [SerializeField] GameObject     freeInputArea;
    [SerializeField] TMP_InputField answerInput;
    [SerializeField] Button         submitButton;
    [SerializeField] TMP_Text       feedbackText;
    [SerializeField] TMP_Text       explanationText;
    [SerializeField] Button         closeProblemButton;

    [Header("닫기")]
    [SerializeField] Button closeButton;

    // ── 상태 ─────────────────────────────────────────────────────────────
    CharacterDatabase _charDb;
    ProblemDatabase   _problemDb;
    OwnedCharacter    _selectedOc;
    CharacterDef      _selectedDef;
    int               _selectedSkillIndex = -1;
    ProblemDef        _currentProblem;

    readonly List<GameObject> _charCards = new List<GameObject>();
    readonly List<GameObject> _skillBtns = new List<GameObject>();

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void OnEnable()
    {
        _charDb    = Resources.Load<CharacterDatabase>("CharacterDatabase");
        _problemDb = Resources.Load<ProblemDatabase>("ProblemDatabase");

        _selectedOc         = null;
        _selectedDef        = null;
        _selectedSkillIndex = -1;
        _currentProblem     = null;

        CloseDetail();
        BuildCharGrid();

        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged += OnRosterChanged;

        if (levelUpButton      != null) levelUpButton.onClick.AddListener(OnLevelUpClicked);
        if (closeProblemButton != null) closeProblemButton.onClick.AddListener(CloseProblemOverlay);
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged -= OnRosterChanged;

        if (levelUpButton      != null) levelUpButton.onClick.RemoveAllListeners();
        if (closeProblemButton != null) closeProblemButton.onClick.RemoveAllListeners();

        UIManager.Close();
    }

    // ── 그리드 ────────────────────────────────────────────────────────────

    void BuildCharGrid()
    {
        foreach (var go in _charCards) if (go) Destroy(go);
        _charCards.Clear();

        if (!MetaState.IsInitialized || charGridContent == null) return;

        var font = GetFont();

        foreach (var oc in MetaState.Roster.Owned)
        {
            var def = _charDb?.ById(oc.id);
            if (def == null || def.skills == null || def.skills.Length == 0) continue;

            var card = CreateCharCard(charGridContent, def.nameKo, oc.level, def.portraitColor, font);
            _charCards.Add(card);

            var capturedOc  = oc;
            var capturedDef = def;
            card.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(capturedOc, capturedDef));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(charGridContent);
    }

    void OnRosterChanged() => BuildCharGrid();

    // ── 카드 선택 / 레이아웃 ─────────────────────────────────────────────

    void SelectCharacter(OwnedCharacter oc, CharacterDef def)
    {
        _selectedOc         = oc;
        _selectedDef        = def;
        _selectedSkillIndex = -1;
        _currentProblem     = null;

        if (problemOverlay != null) problemOverlay.SetActive(false);

        RefreshDetailPanel();
        BuildSkillList();
        ApplyDetailLayout(show: true);
    }

    // 3열 전체폭 ↔ 1열 좌측 전환
    void ApplyDetailLayout(bool show)
    {
        if (detailPanel != null) detailPanel.SetActive(show);

        if (gridLayout != null)
        {
            gridLayout.constraintCount = show ? 1 : 3;
            gridLayout.cellSize = show
                ? new Vector2(CARD_W_1COL, CARD_H)
                : new Vector2(CARD_W_3COL, CARD_H);
        }

        if (charScrollViewRt != null)
        {
            if (show)
            {
                // 좌측 40% 영역
                charScrollViewRt.anchorMin = new Vector2(0f, 0f);
                charScrollViewRt.anchorMax = new Vector2(0f, 1f);
                charScrollViewRt.offsetMin = new Vector2(10f,  65f);
                charScrollViewRt.offsetMax = new Vector2(400f, -70f);
            }
            else
            {
                // 전체 폭
                charScrollViewRt.anchorMin = new Vector2(0f, 0f);
                charScrollViewRt.anchorMax = new Vector2(1f, 1f);
                charScrollViewRt.offsetMin = new Vector2(10f,  65f);
                charScrollViewRt.offsetMax = new Vector2(-10f, -70f);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(charGridContent);
    }

    void CloseDetail()
    {
        _selectedOc  = null;
        _selectedDef = null;
        ApplyDetailLayout(show: false);
    }

    // ── 상세 패널 갱신 ────────────────────────────────────────────────────

    void RefreshDetailPanel()
    {
        if (_selectedDef == null || _selectedOc == null) return;

        if (detailPortrait     != null) detailPortrait.color      = _selectedDef.portraitColor;
        if (detailNameText     != null) detailNameText.text        = _selectedDef.nameKo;
        if (detailContinentText != null) detailContinentText.text = ContinentKo(_selectedDef.continent);

        int cap    = StatGrowth.LevelCap(_selectedDef.rarity);
        bool atCap = _selectedOc.level >= cap;

        if (detailLevelText != null)
            detailLevelText.text = atCap
                ? $"Lv.{_selectedOc.level}  최대 레벨 (상한 {cap})"
                : $"Lv.{_selectedOc.level}  (상한 Lv.{cap})";

        int cost = StudyMaterialWallet.LevelUpCost(_selectedOc.level);
        int have = MetaState.IsInitialized ? MetaState.StudyMaterials.Get(_selectedDef.continent) : 0;

        if (detailMaterialText != null)
            detailMaterialText.text = atCap
                ? "—"
                : $"{ContinentKo(_selectedDef.continent)} 자원: {have}개  /  필요: {cost}개";

        if (levelUpButton != null)
            levelUpButton.interactable = !atCap && have >= cost;

        if (levelUpBtnLabel != null)
            levelUpBtnLabel.text = atCap ? "최대 레벨" : $"레벨업 (자원 {cost}개)";
    }

    // ── 레벨업 ────────────────────────────────────────────────────────────

    void OnLevelUpClicked()
    {
        if (_selectedOc == null || _selectedDef == null) return;
        if (!MetaState.IsInitialized) return;

        int cost = StudyMaterialWallet.LevelUpCost(_selectedOc.level);
        if (!MetaState.StudyMaterials.TrySpend(_selectedDef.continent, cost)) return;

        bool leveled;
        var active = PlayerRuntime.Active;
        if (active != null && active.Id == _selectedDef.id)
        {
            leveled = active.DirectLevelUp();
            if (leveled)
            {
                var playerGO = UnityEngine.GameObject.FindWithTag("Player");
                if (playerGO != null)
                    DamageNumber.SpawnLevelUp(playerGO.transform.position, _selectedOc.level);
            }
        }
        else
        {
            leveled = new CombatCharacter(_selectedDef, _selectedOc).DirectLevelUp();
        }

        if (leveled)
        {
            MetaSaveService.Save();
            MetaState.Roster.NotifyChanged();
            RefreshDetailPanel();
            BuildCharGrid(); // 카드 레벨 텍스트 갱신
        }
        else
        {
            // 실패 시 자원 환불
            MetaState.StudyMaterials.Add(_selectedDef.continent, cost);
        }
    }

    // ── 스킬 목록 ─────────────────────────────────────────────────────────

    void BuildSkillList()
    {
        foreach (var go in _skillBtns) if (go) Destroy(go);
        _skillBtns.Clear();

        if (_selectedDef == null || _selectedDef.skills == null || skillListContent == null) return;

        var font     = GetFont();
        var unlocked = _selectedOc?.unlockedSkillIds;

        for (int i = 0; i < _selectedDef.skills.Length; i++)
        {
            var skill = _selectedDef.skills[i];
            if (skill == null) continue;

            bool isUnlocked = (unlocked == null || unlocked.Count == 0)
                ? i == 0
                : unlocked.Contains(skill.id);

            string lbl = isUnlocked ? $"[해금] {skill.nameKo}" : $"[잠금] {skill.nameKo}";
            var btn    = CreateListButton(skillListContent, lbl, font);
            _skillBtns.Add(btn);

            var b = btn.GetComponent<Button>();
            if (!isUnlocked)
            {
                int idx = i;
                b.onClick.AddListener(() => SelectSkillForResearch(idx));
            }
            else
            {
                b.interactable = false;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(skillListContent);
    }

    void SelectSkillForResearch(int skillIndex)
    {
        if (_selectedDef == null || skillIndex < 0 || skillIndex >= _selectedDef.skills.Length) return;
        var skill = _selectedDef.skills[skillIndex];
        if (skill == null) return;

        _selectedSkillIndex = skillIndex;
        _currentProblem     = _problemDb?.BySkillId(skill.id);

        if (_currentProblem == null)
        {
            // 문제 없음 — 오버레이는 열지 않음
            return;
        }

        ShowProblemOverlay(_currentProblem);
    }

    // ── 문제 오버레이 ─────────────────────────────────────────────────────

    void ShowProblemOverlay(ProblemDef prob)
    {
        if (problemOverlay  != null) problemOverlay.SetActive(true);
        if (feedbackText    != null) feedbackText.gameObject.SetActive(false);
        if (explanationText != null) explanationText.gameObject.SetActive(false);
        if (promptText      != null) promptText.text = prob.prompt;

        bool isMulti = prob.type == ProblemType.MultipleChoice;
        if (multipleChoiceArea != null) multipleChoiceArea.SetActive(isMulti);
        if (freeInputArea      != null) freeInputArea.SetActive(!isMulti);

        if (isMulti && choiceButtons != null)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null) continue;
                choiceButtons[i].onClick.RemoveAllListeners();
                bool hasChoice = prob.choices != null && i < prob.choices.Length;
                choiceButtons[i].gameObject.SetActive(hasChoice);
                if (!hasChoice) continue;
                if (choiceLabels != null && i < choiceLabels.Length && choiceLabels[i] != null)
                    choiceLabels[i].text = prob.choices[i];
                int ci = i;
                choiceButtons[i].onClick.AddListener(() => SubmitSkillAnswer("", ci));
            }
        }
        else
        {
            if (answerInput  != null) answerInput.text = "";
            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(
                    () => SubmitSkillAnswer(answerInput != null ? answerInput.text : "", -1));
            }
        }
    }

    void SubmitSkillAnswer(string answer, int selectedIndex)
    {
        if (_currentProblem == null) return;
        bool correct = ProblemChecker.Check(_currentProblem, answer, selectedIndex);

        if (correct)
        {
            UnlockCurrentSkill();
            ShowFeedback("정답입니다! 스킬이 해금되었습니다.", true);
            ShowExplanation(_currentProblem.explanation);
            if (multipleChoiceArea != null) multipleChoiceArea.SetActive(false);
            if (freeInputArea      != null) freeInputArea.SetActive(false);
        }
        else
        {
            ShowFeedback("오답입니다. 다시 도전해보세요!", false);
        }
    }

    void UnlockCurrentSkill()
    {
        if (_selectedOc == null || _selectedDef == null || _selectedSkillIndex < 0) return;
        if (_selectedSkillIndex >= _selectedDef.skills.Length) return;
        var skill = _selectedDef.skills[_selectedSkillIndex];
        if (skill == null) return;

        if (_selectedOc.unlockedSkillIds == null)
            _selectedOc.unlockedSkillIds = new List<string>();
        if (!_selectedOc.unlockedSkillIds.Contains(skill.id))
            _selectedOc.unlockedSkillIds.Add(skill.id);

        var active = PlayerRuntime.Active;
        if (active != null && active.Id == _selectedDef.id)
            active.Unlock(_selectedSkillIndex);

        MetaSaveService.Save();
        MetaState.Roster.NotifyChanged();
        BuildSkillList();
    }

    void CloseProblemOverlay()
    {
        if (problemOverlay != null) problemOverlay.SetActive(false);
        _currentProblem     = null;
        _selectedSkillIndex = -1;
    }

    void ShowFeedback(string msg, bool isSuccess)
    {
        if (feedbackText == null) return;
        feedbackText.gameObject.SetActive(true);
        feedbackText.text  = msg;
        feedbackText.color = isSuccess ? Color.green : Color.red;
    }

    void ShowExplanation(string text)
    {
        if (explanationText == null || string.IsNullOrEmpty(text)) return;
        explanationText.gameObject.SetActive(true);
        explanationText.text = text;
    }

    public void OnCloseClicked() => gameObject.SetActive(false);

    // ── 내부 유틸 ─────────────────────────────────────────────────────────

    TMP_FontAsset GetFont()
    {
        if (detailNameText  != null && detailNameText.font  != null) return detailNameText.font;
        if (detailLevelText != null && detailLevelText.font != null) return detailLevelText.font;
        return null;
    }

    static string ContinentKo(Continent c) => c switch
    {
        Continent.Physics   => "물리",
        Continent.Chemistry => "화학",
        Continent.Biology   => "생명",
        Continent.EarthSci  => "지구과학",
        Continent.Math      => "수학",
        Continent.Info      => "정보",
        Continent.Mesoria   => "메소리아",
        _                    => c.ToString(),
    };

    static GameObject CreateCharCard(RectTransform parent, string charName, int level,
                                     Color portraitColor, TMP_FontAsset font)
    {
        var card = new GameObject("Card_" + charName);
        card.transform.SetParent(parent, false);
        card.AddComponent<RectTransform>();
        card.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.30f, 0.95f);
        card.AddComponent<Button>();

        // 이름 (상단 13%)
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(card.transform, false);
        var nameRt = nameGO.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0.87f);
        nameRt.anchorMax = new Vector2(1f, 1.00f);
        nameRt.offsetMin = new Vector2(4f, 0f);
        nameRt.offsetMax = new Vector2(-4f, -2f);
        var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
        nameTxt.text = charName; nameTxt.fontSize = 16; nameTxt.color = Color.white;
        nameTxt.alignment = TextAlignmentOptions.Center;
        if (font != null) nameTxt.font = font;

        // 레벨 (12%)
        var lvGO = new GameObject("Level");
        lvGO.transform.SetParent(card.transform, false);
        var lvRt = lvGO.AddComponent<RectTransform>();
        lvRt.anchorMin = new Vector2(0f, 0.75f);
        lvRt.anchorMax = new Vector2(1f, 0.86f);
        lvRt.offsetMin = new Vector2(4f, 0f);
        lvRt.offsetMax = new Vector2(-4f, 0f);
        var lvTxt = lvGO.AddComponent<TextMeshProUGUI>();
        lvTxt.text = $"Lv.{level}"; lvTxt.fontSize = 14;
        lvTxt.color = new Color(0.8f, 0.9f, 1f);
        lvTxt.alignment = TextAlignmentOptions.Center;
        if (font != null) lvTxt.font = font;

        // 초상화 (하단 73%)
        var portGO = new GameObject("Portrait");
        portGO.transform.SetParent(card.transform, false);
        var portRt = portGO.AddComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0.04f, 0.03f);
        portRt.anchorMax = new Vector2(0.96f, 0.73f);
        portRt.offsetMin = Vector2.zero;
        portRt.offsetMax = Vector2.zero;
        portGO.AddComponent<Image>().color = portraitColor;

        return card;
    }

    static GameObject CreateListButton(RectTransform parent, string label, TMP_FontAsset font)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 52f);
        go.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.30f, 0.9f);
        go.AddComponent<Button>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 0f); trt.offsetMax = new Vector2(-8f, 0f);
        var t = textGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 17; t.color = Color.white;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) t.font = font;

        return go;
    }
}

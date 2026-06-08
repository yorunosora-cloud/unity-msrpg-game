using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// K키 전용 R&amp;E (Research &amp; Education) 패널.
/// 상단 탭:
///   [레벨업] — 난이도(하/중/상) 선택 → 문제 풀이 → EXP 지급 (하 80 / 중 200 / 상 500)
///   [스킬 연구] — 기존 스킬 해금 흐름 (잠긴 스킬 클릭 → 문제 풀이 → 정답 시 해금)
/// </summary>
public class RnEPanel : MonoBehaviour
{
    // ── 참조 (MetaUISetup 이 주입) ──────────────────────────────────────────

    [Header("캐릭터 목록 영역")]
    [SerializeField] RectTransform charListContent;

    [Header("탭 버튼")]
    [SerializeField] Button  levelUpTabBtn;
    [SerializeField] Button  skillTabBtn;
    [SerializeField] Image   levelUpTabBg;
    [SerializeField] Image   skillTabBg;

    [Header("레벨업 탭 영역")]
    [SerializeField] GameObject  levelUpArea;
    [SerializeField] TMP_Text    levelInfoText;   // "Lv.N  EXP NNN / NNN  (상한 Lv.NN)"
    [SerializeField] Button      diffLowBtn;
    [SerializeField] Button      diffMidBtn;
    [SerializeField] Button      diffHighBtn;

    [Header("스킬 연구 탭 영역")]
    [SerializeField] GameObject    skillArea;
    [SerializeField] RectTransform skillListContent;

    [Header("공유 문제 영역")]
    [SerializeField] GameObject     problemArea;
    [SerializeField] TMP_Text       promptText;
    [SerializeField] GameObject     multipleChoiceArea;
    [SerializeField] Button[]       choiceButtons;   // 4개
    [SerializeField] TMP_Text[]     choiceLabels;
    [SerializeField] GameObject     freeInputArea;
    [SerializeField] TMP_InputField answerInput;
    [SerializeField] Button         submitButton;

    [Header("피드백")]
    [SerializeField] TMP_Text feedbackText;
    [SerializeField] TMP_Text explanationText;

    [Header("닫기")]
    [SerializeField] Button closeButton;

    // ── EXP 보상 상수 ────────────────────────────────────────────────────────
    const int EXP_LOW  = 80;
    const int EXP_MID  = 200;
    const int EXP_HIGH = 500;

    // ── 탭 색상 ──────────────────────────────────────────────────────────────
    static readonly Color TAB_ACTIVE   = new Color(0.25f, 0.50f, 1.00f, 1f);
    static readonly Color TAB_INACTIVE = new Color(0.15f, 0.18f, 0.28f, 1f);

    // ── 상태 ────────────────────────────────────────────────────────────────

    CharacterDatabase _charDb;
    ProblemDatabase   _problemDb;

    OwnedCharacter _selectedOc;
    CharacterDef   _selectedDef;

    ProblemDifficulty _pendingDifficulty;
    ProblemDef        _levelUpProblem;

    int        _selectedSkillIndex = -1;
    ProblemDef _skillProblem;

    bool _isLevelUpTab = true;

    readonly List<GameObject> _charBtns  = new List<GameObject>();
    readonly List<GameObject> _skillBtns = new List<GameObject>();

    // ── 생명주기 ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _charDb    = Resources.Load<CharacterDatabase>("CharacterDatabase");
        _problemDb = Resources.Load<ProblemDatabase>("ProblemDatabase");

        _selectedOc         = null;
        _selectedDef        = null;
        _selectedSkillIndex = -1;
        _levelUpProblem     = null;
        _skillProblem       = null;

        HideProblem();
        BuildCharList();
        ShowTab(levelUpTab: true);

        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged += OnRosterChanged;

        // closeButton은 MetaUISetup이 AddVoidPersistentListener로 등록하므로 런타임 중복 등록 생략
        if (levelUpTabBtn != null) levelUpTabBtn.onClick.AddListener(() => ShowTab(true));
        if (skillTabBtn   != null) skillTabBtn.onClick.AddListener(() => ShowTab(false));
        if (diffLowBtn    != null) diffLowBtn.onClick.AddListener(() => SelectDifficulty(ProblemDifficulty.Low));
        if (diffMidBtn    != null) diffMidBtn.onClick.AddListener(() => SelectDifficulty(ProblemDifficulty.Mid));
        if (diffHighBtn   != null) diffHighBtn.onClick.AddListener(() => SelectDifficulty(ProblemDifficulty.High));
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged -= OnRosterChanged;

        if (levelUpTabBtn != null) levelUpTabBtn.onClick.RemoveAllListeners();
        if (skillTabBtn   != null) skillTabBtn.onClick.RemoveAllListeners();
        if (diffLowBtn    != null) diffLowBtn.onClick.RemoveAllListeners();
        if (diffMidBtn    != null) diffMidBtn.onClick.RemoveAllListeners();
        if (diffHighBtn   != null) diffHighBtn.onClick.RemoveAllListeners();

        UIManager.Close();
    }

    // ── 탭 전환 ──────────────────────────────────────────────────────────────

    void ShowTab(bool levelUpTab)
    {
        _isLevelUpTab = levelUpTab;

        if (levelUpArea != null) levelUpArea.SetActive(levelUpTab);
        if (skillArea   != null) skillArea.SetActive(!levelUpTab);

        HideProblem();

        if (levelUpTabBg != null) levelUpTabBg.color = levelUpTab ? TAB_ACTIVE : TAB_INACTIVE;
        if (skillTabBg   != null) skillTabBg.color   = levelUpTab ? TAB_INACTIVE : TAB_ACTIVE;

        if (levelUpTab)
            RefreshLevelUpInfo();
        else
            BuildSkillList();
    }

    // ── 캐릭터 목록 ──────────────────────────────────────────────────────────

    void BuildCharList()
    {
        foreach (var go in _charBtns) if (go) Destroy(go);
        _charBtns.Clear();

        if (!MetaState.IsInitialized || charListContent == null) return;

        foreach (var oc in MetaState.Roster.Owned)
        {
            var def    = _charDb?.ById(oc.id);
            string lbl = def != null ? $"[{def.rarity}] {def.nameKo}" : oc.id;
            var btn    = CreateListButton(charListContent, lbl);
            _charBtns.Add(btn);

            var capturedOc  = oc;
            var capturedDef = def;
            btn.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(capturedOc, capturedDef));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(charListContent);
    }

    void OnRosterChanged() => BuildCharList();

    void SelectCharacter(OwnedCharacter oc, CharacterDef def)
    {
        _selectedOc         = oc;
        _selectedDef        = def;
        _selectedSkillIndex = -1;
        _levelUpProblem     = null;
        _skillProblem       = null;
        HideProblem();

        if (_isLevelUpTab)
            RefreshLevelUpInfo();
        else
            BuildSkillList();
    }

    // ── 레벨업 탭 ────────────────────────────────────────────────────────────

    void RefreshLevelUpInfo()
    {
        if (levelInfoText == null) return;

        if (_selectedOc == null || _selectedDef == null)
        {
            levelInfoText.text = "캐릭터를 선택하세요.";
            SetDiffButtons(false);
            return;
        }

        int  cap   = StatGrowth.LevelCap(_selectedDef.rarity);
        bool atCap = _selectedOc.level >= cap;

        if (atCap)
        {
            levelInfoText.text = $"Lv.{_selectedOc.level}  최대 레벨 도달! (등급 상한 {cap})";
            SetDiffButtons(false);
        }
        else
        {
            int nextExp        = ComputeNextExp(_selectedOc.level);
            levelInfoText.text = $"Lv.{_selectedOc.level}  EXP {_selectedOc.exp} / {nextExp}  (상한 Lv.{cap})";
            SetDiffButtons(true);
        }
    }

    void SetDiffButtons(bool interactable)
    {
        if (diffLowBtn  != null) diffLowBtn.interactable  = interactable;
        if (diffMidBtn  != null) diffMidBtn.interactable  = interactable;
        if (diffHighBtn != null) diffHighBtn.interactable = interactable;
    }

    void SelectDifficulty(ProblemDifficulty diff)
    {
        if (_selectedOc == null) return;

        _pendingDifficulty = diff;
        _levelUpProblem    = _problemDb?.RandomByDifficulty(diff);

        if (_levelUpProblem == null)
        {
            ShowFeedback($"[{DiffLabel(diff)}] 난이도 문제가 아직 없습니다.", false);
            HideProblemInput();
            return;
        }

        ShowProblem(_levelUpProblem, isLevelUp: true);
    }

    void SubmitLevelUpMultipleChoice(int selectedIndex)
    {
        if (_levelUpProblem == null) return;
        HandleLevelUpResult(ProblemChecker.Check(_levelUpProblem, "", selectedIndex));
    }

    void SubmitLevelUpFreeInput()
    {
        if (_levelUpProblem == null) return;
        string answer = answerInput != null ? answerInput.text : "";
        HandleLevelUpResult(ProblemChecker.Check(_levelUpProblem, answer, -1));
    }

    void HandleLevelUpResult(bool correct)
    {
        if (correct)
        {
            int expAmount = _pendingDifficulty switch
            {
                ProblemDifficulty.Low  => EXP_LOW,
                ProblemDifficulty.Mid  => EXP_MID,
                ProblemDifficulty.High => EXP_HIGH,
                _                      => EXP_LOW,
            };

            GiveExp(expAmount);
            ShowFeedback($"정답! +{expAmount} EXP", true);
            ShowExplanation(_levelUpProblem?.explanation);
            HideProblemInput();
            RefreshLevelUpInfo();
        }
        else
        {
            ShowFeedback("오답입니다. 다시 도전해보세요!", false);
        }
    }

    void GiveExp(int amount)
    {
        if (_selectedOc == null || _selectedDef == null) return;

        var active = PlayerRuntime.Active;
        if (active != null && active.Id == _selectedDef.id)
        {
            active.GainExp(amount);
        }
        else
        {
            // CombatCharacter는 _owned 참조를 공유하므로 GainExp가 owned.level/exp를 직접 수정한다.
            // HP/MP 풀회복(레벨업 부산물)은 비활성 캐릭터에겐 무관하므로 허용.
            new CombatCharacter(_selectedDef, _selectedOc).GainExp(amount);
        }

        MetaSaveService.Save();
        MetaState.Roster.NotifyChanged();
    }

    // ── 스킬 연구 탭 ─────────────────────────────────────────────────────────

    void ClearSkillList()
    {
        foreach (var go in _skillBtns) if (go) Destroy(go);
        _skillBtns.Clear();
    }

    void BuildSkillList()
    {
        ClearSkillList();
        if (_selectedDef == null || _selectedDef.skills == null || skillListContent == null) return;

        var unlocked = _selectedOc?.unlockedSkillIds;

        for (int i = 0; i < _selectedDef.skills.Length; i++)
        {
            var skill = _selectedDef.skills[i];
            if (skill == null) continue;

            bool isUnlocked = (unlocked == null || unlocked.Count == 0)
                ? i == 0
                : unlocked.Contains(skill.id);

            string lbl = isUnlocked ? $"[해금] {skill.nameKo}" : $"[잠금] {skill.nameKo}";
            var btn    = CreateListButton(skillListContent, lbl);
            _skillBtns.Add(btn);

            var b = btn.GetComponent<Button>();
            if (!isUnlocked)
            {
                int capturedIndex = i;
                b.onClick.AddListener(() => SelectSkill(capturedIndex));
            }
            else
            {
                b.interactable = false;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(skillListContent);
    }

    void SelectSkill(int skillIndex)
    {
        if (_selectedDef == null || skillIndex < 0 || skillIndex >= _selectedDef.skills.Length) return;
        var skill = _selectedDef.skills[skillIndex];
        if (skill == null) return;

        _selectedSkillIndex = skillIndex;
        _skillProblem       = _problemDb?.BySkillId(skill.id);

        if (_skillProblem == null)
        {
            ShowFeedback("이 스킬에 연결된 문제가 아직 없습니다.", false);
            HideProblemInput();
            return;
        }

        ShowProblem(_skillProblem, isLevelUp: false);
    }

    void SubmitSkillMultipleChoice(int selectedIndex)
    {
        if (_skillProblem == null) return;
        HandleSkillResult(ProblemChecker.Check(_skillProblem, "", selectedIndex));
    }

    void SubmitSkillFreeInput()
    {
        if (_skillProblem == null) return;
        string answer = answerInput != null ? answerInput.text : "";
        HandleSkillResult(ProblemChecker.Check(_skillProblem, answer, -1));
    }

    void HandleSkillResult(bool correct)
    {
        if (correct)
        {
            UnlockCurrentSkill();
            ShowFeedback("정답입니다! 스킬이 해금되었습니다.", true);
            ShowExplanation(_skillProblem?.explanation);
            HideProblemInput();
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

    // ── 문제 표시 (두 탭 공유) ───────────────────────────────────────────────

    void HideProblem()
    {
        if (problemArea     != null) problemArea.SetActive(false);
        if (feedbackText    != null) feedbackText.gameObject.SetActive(false);
        if (explanationText != null) explanationText.gameObject.SetActive(false);
    }

    void HideProblemInput()
    {
        if (multipleChoiceArea != null) multipleChoiceArea.SetActive(false);
        if (freeInputArea      != null) freeInputArea.SetActive(false);
    }

    void ShowProblem(ProblemDef prob, bool isLevelUp)
    {
        if (problemArea     != null) problemArea.SetActive(true);
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
                if (isLevelUp)
                    choiceButtons[i].onClick.AddListener(() => SubmitLevelUpMultipleChoice(ci));
                else
                    choiceButtons[i].onClick.AddListener(() => SubmitSkillMultipleChoice(ci));
            }
        }
        else
        {
            if (answerInput  != null) answerInput.text = "";
            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(isLevelUp ? (UnityEngine.Events.UnityAction)SubmitLevelUpFreeInput : SubmitSkillFreeInput);
            }
        }
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

    // ── 내부 유틸 ────────────────────────────────────────────────────────────

    static string DiffLabel(ProblemDifficulty d) => d switch
    {
        ProblemDifficulty.Low  => "하",
        ProblemDifficulty.Mid  => "중",
        ProblemDifficulty.High => "상",
        _                       => "?",
    };

    static int ComputeNextExp(int level) => StatGrowth.NextExp(level);

    static GameObject CreateListButton(RectTransform parent, string label)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 60f);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.22f, 0.30f, 0.9f);
        go.AddComponent<Button>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 0f);
        trt.offsetMax = new Vector2(-8f, 0f);
        var t = textGO.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = 22;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }
}

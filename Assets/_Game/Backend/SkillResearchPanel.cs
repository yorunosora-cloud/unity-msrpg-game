using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// K 키 전용 스킬 연구 패널. 흐름:
///   캐릭터 목록 → 스킬 목록(해금/잠금) → 잠긴 스킬 클릭 → 문제 풀이 → 정답 시 해금.
/// 버튼을 코드로 직접 생성하므로 프리팹 의존성 없음.
/// </summary>
public class SkillResearchPanel : MonoBehaviour
{
    // ── 참조 (MetaUISetup 이 주입) ──────────────────────────────────────────

    [Header("캐릭터 목록 영역")]
    [SerializeField] RectTransform  charListContent;  // Scroll → Viewport → Content

    [Header("스킬 목록 영역")]
    [SerializeField] RectTransform  skillListContent;

    [Header("문제 영역")]
    [SerializeField] GameObject     problemArea;
    [SerializeField] TMP_Text       promptText;
    [SerializeField] GameObject     multipleChoiceArea;
    [SerializeField] Button[]       choiceButtons;   // 4개
    [SerializeField] TMP_Text[]     choiceLabels;
    [SerializeField] GameObject     freeInputArea;
    [SerializeField] TMP_InputField answerInput;
    [SerializeField] Button         submitButton;

    [Header("피드백")]
    [SerializeField] TMP_Text       feedbackText;
    [SerializeField] TMP_Text       explanationText;

    [Header("닫기")]
    [SerializeField] Button         closeButton;

    // ── 상태 ────────────────────────────────────────────────────────────────

    CharacterDatabase _charDb;
    ProblemDatabase   _problemDb;

    OwnedCharacter _selectedOc;
    CharacterDef   _selectedDef;
    int            _selectedSkillIndex = -1;
    ProblemDef     _currentProblem;

    readonly List<GameObject> _charBtns  = new List<GameObject>();
    readonly List<GameObject> _skillBtns = new List<GameObject>();

    // ── 생명주기 ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _charDb    = Resources.Load<CharacterDatabase>("CharacterDatabase");
        _problemDb = Resources.Load<ProblemDatabase>("ProblemDatabase");

        ClearSkillList();
        HideProblem();
        BuildCharList();

        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged += OnRosterChanged;

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged -= OnRosterChanged;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);

        UIManager.Close();
    }

    // ── 캐릭터 목록 ──────────────────────────────────────────────────────────

    void BuildCharList()
    {
        foreach (var go in _charBtns) if (go) Destroy(go);
        _charBtns.Clear();

        if (!MetaState.IsInitialized || charListContent == null) return;

        foreach (var oc in MetaState.Roster.Owned)
        {
            var def   = _charDb?.ById(oc.id);
            string lbl = def != null ? $"[{def.rarity}] {def.nameKo}" : oc.id;
            var btn = CreateListButton(charListContent, lbl);
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
        _selectedOc  = oc;
        _selectedDef = def;
        _selectedSkillIndex = -1;
        HideProblem();
        BuildSkillList();
    }

    // ── 스킬 목록 ────────────────────────────────────────────────────────────

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
            var btn = CreateListButton(skillListContent, lbl);
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
        _currentProblem = _problemDb?.BySkillId(skill.id);

        if (_currentProblem == null)
        {
            ShowFeedback("이 스킬에 연결된 문제가 아직 없습니다.", false);
            HideProblemInput();
            return;
        }

        ShowProblem(_currentProblem);
    }

    // ── 문제 표시 ────────────────────────────────────────────────────────────

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

    void ShowProblem(ProblemDef prob)
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
                choiceButtons[i].onClick.AddListener(() => SubmitMultipleChoice(ci));
            }
        }
        else
        {
            if (answerInput  != null) answerInput.text = "";
            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitFreeInput);
            }
        }
    }

    // ── 정답 처리 ────────────────────────────────────────────────────────────

    void SubmitMultipleChoice(int selectedIndex)
    {
        if (_currentProblem == null) return;
        HandleResult(ProblemChecker.Check(_currentProblem, "", selectedIndex));
    }

    void SubmitFreeInput()
    {
        if (_currentProblem == null) return;
        string answer = answerInput != null ? answerInput.text : "";
        HandleResult(ProblemChecker.Check(_currentProblem, answer, -1));
    }

    void HandleResult(bool correct)
    {
        if (correct)
        {
            UnlockCurrentSkill();
            ShowFeedback("정답입니다! 스킬이 해금되었습니다.", true);
            ShowExplanation(_currentProblem?.explanation);
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

        // 활성 파티 멤버라면 CombatCharacter 에도 즉시 반영
        var active = PlayerRuntime.Active;
        if (active != null && active.Id == _selectedDef.id)
            active.Unlock(_selectedSkillIndex);

        MetaSaveService.Save();
        MetaState.Roster.NotifyChanged();
        BuildSkillList();
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

    // ── 내부 UI 생성 헬퍼 ────────────────────────────────────────────────────

    /// <summary>VerticalLayoutGroup Content 에 버튼 1개 생성.</summary>
    static GameObject CreateListButton(RectTransform parent, string label)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 60f); // 너비는 LayoutGroup이 결정
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

        // 폰트는 DamageNumber 패턴처럼 런타임에 씬 TMP에서 자동 캐시되도록 비워둠
        return go;
    }
}

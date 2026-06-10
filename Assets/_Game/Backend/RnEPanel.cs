using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// K키 전용 R&amp;E (Research &amp; Education) 패널.
///
/// 흐름:
///   캐릭터 그리드(3열) → 카드 클릭 → 개인 창(허브)
///     [레벨업] 버튼 → 난이도 선택(하/중/상) → 자원 소모
///                   → 문제 풀이(같은 문제 최대 3시도)
///                   → 정답 시 GainExp → 임계 도달 시 레벨업
///                   → 3회 오답 시 자원 소실
///     [스킬 연구] 버튼 → 스킬 목록(전체 폭) → 잠금 클릭 → 문제 → 해금
/// </summary>
public class RnEPanel : MonoBehaviour
{
    // ── 카드 크기 상수 ────────────────────────────────────────────────────
    const float CARD_H      = 200f;
    const float CARD_W_3COL = 316f;
    const float CARD_W_1COL = 374f;

    // ── 레벨업 결정 비용 (하/중/상) — 나중에 조정 예정 ────────────────────
    static readonly int[] CrystalCosts = { 10, 25, 50 };

    // ── 그리드 ────────────────────────────────────────────────────────────
    [Header("그리드")]
    [SerializeField] RectTransform   charScrollViewRt;
    [SerializeField] RectTransform   charGridContent;
    [SerializeField] GridLayoutGroup gridLayout;

    // ── 개인 창(허브) ─────────────────────────────────────────────────────
    [Header("개인 창(허브)")]
    [SerializeField] GameObject  detailPanel;
    [SerializeField] Image       detailPortrait;
    [SerializeField] TMP_Text    detailNameText;
    [SerializeField] TMP_Text    detailContinentText;
    [SerializeField] TMP_Text    detailLevelText;
    [SerializeField] TMP_Text    detailExpText;       // "EXP: 120 / 145"
    [SerializeField] TMP_Text    detailMaterialText;  // "과목 자원: 30개"
    [SerializeField] Button      levelUpModeButton;   // [레벨업] 버튼
    [SerializeField] Button      skillModeButton;     // [스킬 연구] 버튼

    // ── 난이도 선택 패널 ──────────────────────────────────────────────────
    [Header("난이도 선택 패널")]
    [SerializeField] GameObject  difficultyPanel;
    [SerializeField] Button[]    difficultyButtons  = new Button[3];   // 하/중/상
    [SerializeField] TMP_Text[]  difficultyLabels   = new TMP_Text[3]; // 버튼 라벨

    // ── 스킬 목록 패널 ────────────────────────────────────────────────────
    [Header("스킬 목록 패널")]
    [SerializeField] GameObject    skillListPanel;
    [SerializeField] RectTransform skillListContent;

    // ── 스킬 정보 패널 ────────────────────────────────────────────────────
    [Header("스킬 정보 패널")]
    [SerializeField] GameObject  skillInfoPanel;
    [SerializeField] TMP_Text    skillInfoNameText;
    [SerializeField] TMP_Text    skillInfoStatsText;
    [SerializeField] Button      skillResearchStartButton;
    [SerializeField] Button      skillInfoBackButton;

    // ── 문제 오버레이 ─────────────────────────────────────────────────────
    [Header("문제 오버레이")]
    [SerializeField] GameObject     problemOverlay;
    [SerializeField] TMP_Text       promptText;
    [SerializeField] TMP_Text       attemptText;       // "시도: 3 / 3"
    [SerializeField] GameObject     multipleChoiceArea;
    [SerializeField] Button[]       choiceButtons  = new Button[4];
    [SerializeField] TMP_Text[]     choiceLabels   = new TMP_Text[4];
    [SerializeField] GameObject     freeInputArea;
    [SerializeField] TMP_InputField answerInput;
    [SerializeField] Button         submitButton;
    [SerializeField] TMP_Text       feedbackText;
    [SerializeField] TMP_Text       explanationText;
    [SerializeField] Button         closeProblemButton;

    // ── 닫기 ─────────────────────────────────────────────────────────────
    [Header("닫기")]
    [SerializeField] Button closeButton;

    // ── 오버레이 모드 ─────────────────────────────────────────────────────
    enum OverlayMode { SkillUnlock, LevelUp }

    // ── 상태 ─────────────────────────────────────────────────────────────
    CharacterDatabase _charDb;
    ProblemDatabase   _problemDb;
    OwnedCharacter    _selectedOc;
    CharacterDef      _selectedDef;
    int               _selectedSkillIndex = -1;
    ProblemDef        _currentProblem;
    OverlayMode       _overlayMode;
    int               _attemptsLeft;
    int               _pendingExpReward;

    readonly List<GameObject> _charCards = new List<GameObject>();
    readonly List<GameObject> _skillBtns = new List<GameObject>();

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void OnEnable()
    {
        _charDb    = Resources.Load<CharacterDatabase>("CharacterDatabase");
        _problemDb = Resources.Load<ProblemDatabase>("ProblemDatabase");

        ResetState();
        CloseDetail();
        BuildCharGrid();

        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged += OnRosterChanged;

        RegisterHubButtons();
        RegisterDifficultyButtons();
        RegisterSkillInfoButtons();
        if (closeProblemButton != null) closeProblemButton.onClick.AddListener(CloseProblemOverlay);
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged -= OnRosterChanged;

        UnregisterHubButtons();
        UnregisterDifficultyButtons();
        UnregisterSkillInfoButtons();
        if (closeProblemButton != null) closeProblemButton.onClick.RemoveAllListeners();

        UIManager.Close();
    }

    void ResetState()
    {
        _selectedOc         = null;
        _selectedDef        = null;
        _selectedSkillIndex = -1;
        _currentProblem     = null;
        _attemptsLeft       = 0;
        _pendingExpReward   = 0;
    }

    // ── 허브 버튼 등록 ────────────────────────────────────────────────────

    void RegisterHubButtons()
    {
        if (levelUpModeButton != null) levelUpModeButton.onClick.AddListener(OnLevelUpModeClicked);
        if (skillModeButton   != null) skillModeButton.onClick.AddListener(OnSkillModeClicked);
    }

    void UnregisterHubButtons()
    {
        if (levelUpModeButton != null) levelUpModeButton.onClick.RemoveAllListeners();
        if (skillModeButton   != null) skillModeButton.onClick.RemoveAllListeners();
    }

    // ── 난이도 버튼 등록 ──────────────────────────────────────────────────

    void RegisterDifficultyButtons()
    {
        if (difficultyButtons == null) return;
        var difficulties = new[] { ProblemDifficulty.Low, ProblemDifficulty.Mid, ProblemDifficulty.High };
        for (int i = 0; i < difficultyButtons.Length && i < difficulties.Length; i++)
        {
            if (difficultyButtons[i] == null) continue;
            var d = difficulties[i];
            difficultyButtons[i].onClick.RemoveAllListeners();
            difficultyButtons[i].onClick.AddListener(() => OnDifficultySelected(d));
        }
    }

    void UnregisterDifficultyButtons()
    {
        if (difficultyButtons == null) return;
        foreach (var b in difficultyButtons)
            if (b != null) b.onClick.RemoveAllListeners();
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
            if (def == null) continue;

            var card = CreateCharCard(charGridContent, def.nameKo, oc.level, def.portraitColor, font);
            _charCards.Add(card);

            var capturedOc  = oc;
            var capturedDef = def;
            card.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(capturedOc, capturedDef));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(charGridContent);
    }

    void OnRosterChanged()
    {
        BuildCharGrid();
        if (_selectedOc != null && _selectedDef != null)
            RefreshDetailInfo();
    }

    // ── 카드 선택 ─────────────────────────────────────────────────────────

    void SelectCharacter(OwnedCharacter oc, CharacterDef def)
    {
        _selectedOc         = oc;
        _selectedDef        = def;
        _selectedSkillIndex = -1;
        _currentProblem     = null;

        if (problemOverlay != null) problemOverlay.SetActive(false);

        // 콘텐츠 패널 모두 닫기 (허브 기본 상태)
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (skillListPanel  != null) skillListPanel.SetActive(false);
        if (skillInfoPanel  != null) skillInfoPanel.SetActive(false);

        RefreshDetailInfo();
        ApplyDetailLayout(show: true);
    }

    // ── 허브 버튼 핸들러 ──────────────────────────────────────────────────

    void OnLevelUpModeClicked()
    {
        if (_selectedDef == null || _selectedOc == null) return;

        if (skillListPanel  != null) skillListPanel.SetActive(false);
        if (difficultyPanel != null) difficultyPanel.SetActive(true);

        RefreshDifficultyPanel();
    }

    void OnSkillModeClicked()
    {
        if (_selectedDef == null || _selectedOc == null)
        {
            UnityEngine.Debug.LogWarning("[RnE] OnSkillModeClicked: selectedDef or selectedOc is null");
            return;
        }

        UnityEngine.Debug.Log($"[RnE] OnSkillModeClicked: def={_selectedDef.id}  skills={((_selectedDef.skills == null) ? "NULL" : _selectedDef.skills.Length.ToString())}  skillListPanel={(skillListPanel == null ? "NULL" : "OK")}");

        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (skillInfoPanel  != null) skillInfoPanel.SetActive(false);
        if (skillListPanel  != null) skillListPanel.SetActive(true);

        BuildSkillList();
    }

    // ── 난이도 패널 갱신 ──────────────────────────────────────────────────

    void RefreshDifficultyPanel()
    {
        if (_selectedDef == null || _selectedOc == null) return;

        int cap    = StatGrowth.LevelCap(_selectedDef.rarity);
        bool atCap = _selectedOc.level >= cap;
        var  ckind = CrystalFor(_selectedDef);
        int  have  = MetaState.IsInitialized ? MetaState.Crystals.Get(ckind) : 0;

        var difficulties = new[] { ProblemDifficulty.Low, ProblemDifficulty.Mid, ProblemDifficulty.High };

        for (int i = 0; i < difficultyButtons.Length && i < difficulties.Length; i++)
        {
            if (difficultyButtons[i] == null) continue;
            int cost = CrystalCosts[i];
            int exp  = ProblemDifficultyInfo.ExpReward(difficulties[i]);

            if (difficultyLabels != null && i < difficultyLabels.Length && difficultyLabels[i] != null)
                difficultyLabels[i].text = $"{ProblemDifficultyInfo.Label(difficulties[i])}  결정 {cost}개  EXP +{exp}";

            difficultyButtons[i].interactable = !atCap && have >= cost;
        }
    }

    // ── 난이도 선택 → 레벨업 문제 세션 시작 ─────────────────────────────

    void OnDifficultySelected(ProblemDifficulty d)
    {
        if (_selectedDef == null || _selectedOc == null) return;
        if (!MetaState.IsInitialized) return;

        int cost  = CrystalCosts[(int)d];
        var ckind = CrystalFor(_selectedDef);
        if (!MetaState.Crystals.TrySpend(ckind, cost)) return;

        var prob = _problemDb?.RandomByDifficulty(d);
        if (prob == null)
        {
            // 문제가 없으면 결정 환불 후 종료
            MetaState.Crystals.Add(ckind, cost);
            return;
        }

        _currentProblem   = prob;
        _overlayMode      = OverlayMode.LevelUp;
        _attemptsLeft     = 3;
        _pendingExpReward = ProblemDifficultyInfo.ExpReward(d);

        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        ShowProblemOverlay(prob);
    }

    // ── 개인 창 정보 갱신 ────────────────────────────────────────────────

    void RefreshDetailInfo()
    {
        if (_selectedDef == null || _selectedOc == null) return;

        if (detailPortrait      != null) detailPortrait.color        = _selectedDef.portraitColor;
        if (detailNameText      != null) detailNameText.text          = _selectedDef.nameKo;
        if (detailContinentText != null) detailContinentText.text     = ContinentKo(_selectedDef.continent);

        int cap   = StatGrowth.LevelCap(_selectedDef.rarity);
        bool atCap = _selectedOc.level >= cap;

        if (detailLevelText != null)
            detailLevelText.text = atCap
                ? $"Lv.{_selectedOc.level}  (최대 — 상한 {cap})"
                : $"Lv.{_selectedOc.level}  (상한 Lv.{cap})";

        if (detailExpText != null)
        {
            if (atCap)
                detailExpText.text = "EXP: MAX";
            else
            {
                int nextExp = StatGrowth.NextExp(_selectedOc.level);
                detailExpText.text = $"EXP: {_selectedOc.exp} / {nextExp}";
            }
        }

        var crystalKind = CrystalFor(_selectedDef);
        int have = MetaState.IsInitialized ? MetaState.Crystals.Get(crystalKind) : 0;
        if (detailMaterialText != null)
            detailMaterialText.text = $"결정 ({CrystalKindKo(crystalKind)}): {have}개";

        // [레벨업] 버튼 — 최대 레벨이면 비활성 (자원 체크는 난이도 버튼에서 함)
        if (levelUpModeButton != null)
            levelUpModeButton.interactable = !atCap;
    }

    // ── 레이아웃 전환 ─────────────────────────────────────────────────────

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
                charScrollViewRt.anchorMin = new Vector2(0f, 0f);
                charScrollViewRt.anchorMax = new Vector2(0f, 1f);
                charScrollViewRt.offsetMin = new Vector2(10f,  65f);
                charScrollViewRt.offsetMax = new Vector2(400f, -70f);
            }
            else
            {
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
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (skillListPanel  != null) skillListPanel.SetActive(false);
        if (skillInfoPanel  != null) skillInfoPanel.SetActive(false);
        ApplyDetailLayout(show: false);
    }

    // ── 스킬 목록 ─────────────────────────────────────────────────────────

    void BuildSkillList()
    {
        foreach (var go in _skillBtns) if (go) Destroy(go);
        _skillBtns.Clear();

        if (_selectedDef == null || _selectedDef.skills == null || skillListContent == null)
        {
            UnityEngine.Debug.LogWarning($"[RnE] BuildSkillList 조기 종료: def={_selectedDef?.id ?? "NULL"}  skills={(_selectedDef?.skills == null ? "NULL" : _selectedDef.skills.Length.ToString())}  skillListContent={(skillListContent == null ? "NULL" : "OK")}");
            return;
        }

        var font     = GetFont();
        var unlocked = _selectedOc?.unlockedSkillIds;

        int nullSkills = 0;
        for (int i = 0; i < _selectedDef.skills.Length; i++)
            if (_selectedDef.skills[i] == null) nullSkills++;
        UnityEngine.Debug.Log($"[RnE] BuildSkillList: total={_selectedDef.skills.Length}  null={nullSkills}  def={_selectedDef.id}");

        for (int i = 0; i < _selectedDef.skills.Length; i++)
        {
            var skill = _selectedDef.skills[i];
            if (skill == null) continue;

            bool isUnlocked = i == 0 || (unlocked != null && unlocked.Contains(skill.id));

            string lbl = isUnlocked
                ? $"[해금]  {skill.nameKo}"
                : $"[잠금]  {skill.nameKo}";
            var btn = CreateListButton(skillListContent, lbl, font);
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
        ShowSkillInfoPanel(skill);  // 바로 문제 시작 대신 스킬 정보 패널 먼저 표시
    }

    // ── 스킬 정보 패널 ─────────────────────────────────────────────────────

    void RegisterSkillInfoButtons()
    {
        if (skillResearchStartButton != null)
            skillResearchStartButton.onClick.AddListener(OnSkillResearchStartClicked);
        if (skillInfoBackButton != null)
            skillInfoBackButton.onClick.AddListener(OnSkillInfoBackClicked);
    }

    void UnregisterSkillInfoButtons()
    {
        if (skillResearchStartButton != null) skillResearchStartButton.onClick.RemoveAllListeners();
        if (skillInfoBackButton      != null) skillInfoBackButton.onClick.RemoveAllListeners();
    }

    void ShowSkillInfoPanel(SkillDef skill)
    {
        if (skillInfoPanel == null) return;

        if (skillInfoNameText  != null) skillInfoNameText.text  = skill.nameKo;
        if (skillInfoStatsText != null) skillInfoStatsText.text = BuildSkillStatsText(skill);

        // 목록은 뒤로 숨기고 정보 패널 표시
        if (skillListPanel != null) skillListPanel.SetActive(false);
        skillInfoPanel.SetActive(true);
    }

    string BuildSkillStatsText(SkillDef skill)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(skill.descKo))
            sb.AppendLine(skill.descKo).AppendLine();

        sb.AppendLine($"효과 종류: {EffectKindKo(skill.effectKind)}");
        sb.AppendLine($"MP 소모: {skill.mpCost}");
        sb.AppendLine($"쿨타임: {skill.cooldown:F1}초");
        sb.AppendLine($"사거리: {skill.range:F1}m");

        switch (skill.effectKind)
        {
            case SkillEffectKind.Strike:
            case SkillEffectKind.Aoe:
                sb.AppendLine($"데미지 배율: ×{skill.damageMultiplier:F2}");
                break;
            case SkillEffectKind.HealBuff:
                sb.AppendLine($"회복량: HP의 {skill.healPercent * 100f:F0}%");
                sb.AppendLine($"공격력 버프: ×{skill.buffAtkMultiplier:F2}  ({skill.buffDuration:F1}초)");
                break;
            case SkillEffectKind.Mark:
                sb.AppendLine("적에게 속성 표식 부여 (시너지 연계)");
                break;
        }
        return sb.ToString().TrimEnd();
    }

    static string EffectKindKo(SkillEffectKind k) => k switch
    {
        SkillEffectKind.Strike   => "단일 공격",
        SkillEffectKind.Aoe      => "광역 공격",
        SkillEffectKind.HealBuff => "회복·버프",
        SkillEffectKind.Mark     => "표식 부여",
        _                         => k.ToString(),
    };

    void OnSkillResearchStartClicked()
    {
        if (_selectedSkillIndex < 0 || _selectedDef == null) return;
        if (_selectedSkillIndex >= _selectedDef.skills.Length) return;
        var skill = _selectedDef.skills[_selectedSkillIndex];
        if (skill == null) return;

        _currentProblem = _problemDb?.BySkillId(skill.id);
        _overlayMode    = OverlayMode.SkillUnlock;
        _attemptsLeft   = 0;

        if (skillInfoPanel != null) skillInfoPanel.SetActive(false);
        if (_currentProblem == null) return;
        ShowProblemOverlay(_currentProblem);
    }

    void OnSkillInfoBackClicked()
    {
        if (skillInfoPanel != null) skillInfoPanel.SetActive(false);
        if (skillListPanel != null) skillListPanel.SetActive(true);
        _selectedSkillIndex = -1;
    }

    // ── 문제 오버레이 ─────────────────────────────────────────────────────

    void ShowProblemOverlay(ProblemDef prob)
    {
        if (problemOverlay  != null) problemOverlay.SetActive(true);
        if (feedbackText    != null) feedbackText.gameObject.SetActive(false);
        if (explanationText != null) explanationText.gameObject.SetActive(false);
        if (promptText      != null) promptText.text = prob.prompt;

        UpdateAttemptText();

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
                choiceButtons[i].onClick.AddListener(() => SubmitAnswer("", ci));
            }
        }
        else
        {
            if (answerInput  != null) answerInput.text = "";
            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(
                    () => SubmitAnswer(answerInput != null ? answerInput.text : "", -1));
            }
        }
    }

    void UpdateAttemptText()
    {
        if (attemptText == null) return;
        if (_overlayMode == OverlayMode.LevelUp)
            attemptText.text = $"시도: {_attemptsLeft} / 3";
        else
            attemptText.gameObject.SetActive(false);
    }

    // ── 정답 제출 (스킬 해금 / 레벨업 공용) ───────────────────────────────

    void SubmitAnswer(string answer, int selectedIndex)
    {
        if (_currentProblem == null) return;
        bool correct = ProblemChecker.Check(_currentProblem, answer, selectedIndex);

        if (correct)
        {
            if (_overlayMode == OverlayMode.SkillUnlock)
            {
                UnlockCurrentSkill();
                ShowFeedback("정답! 스킬이 해금되었습니다.", true);
            }
            else // LevelUp
            {
                GainExpForSelected(_pendingExpReward);
                ShowFeedback($"정답! EXP +{_pendingExpReward}", true);
            }

            ShowExplanation(_currentProblem.explanation);
            if (multipleChoiceArea != null) multipleChoiceArea.SetActive(false);
            if (freeInputArea      != null) freeInputArea.SetActive(false);
        }
        else
        {
            if (_overlayMode == OverlayMode.LevelUp)
            {
                _attemptsLeft--;
                UpdateAttemptText();

                if (_attemptsLeft <= 0)
                {
                    ShowFeedback("기회 소진 — 자원 소실", false);
                    if (multipleChoiceArea != null) multipleChoiceArea.SetActive(false);
                    if (freeInputArea      != null) freeInputArea.SetActive(false);
                    return;
                }
            }
            ShowFeedback("오답입니다. 다시 도전해보세요!", false);
        }
    }

    // ── EXP 획득 및 레벨업 처리 ─────────────────────────────────────────

    void GainExpForSelected(int expAmount)
    {
        if (_selectedOc == null || _selectedDef == null) return;

        bool leveled = false;
        int  prevLv  = _selectedOc.level;

        var active = PlayerRuntime.Active;
        if (active != null && active.Id == _selectedDef.id)
        {
            active.GainExp(expAmount);
            leveled = _selectedOc.level > prevLv;
            if (leveled)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null)
                    DamageNumber.SpawnLevelUp(playerGO.transform.position, _selectedOc.level);
            }
        }
        else
        {
            // 비활성 캐릭터: OwnedCharacter 참조 공유로 영속
            var cc = new CombatCharacter(_selectedDef, _selectedOc);
            cc.GainExp(expAmount);
            leveled = _selectedOc.level > prevLv;
        }

        MetaSaveService.Save();
        MetaState.Roster.NotifyChanged();
        RefreshDetailInfo();

        if (leveled && feedbackText != null)
            feedbackText.text = $"레벨업! Lv.{_selectedOc.level}";
    }

    // ── 스킬 해금 ─────────────────────────────────────────────────────────

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
        _attemptsLeft       = 0;
        _pendingExpReward   = 0;

        // 레벨업 모드로 돌아왔으면 난이도 패널 재표시
        if (_overlayMode == OverlayMode.LevelUp && _selectedOc != null)
        {
            if (difficultyPanel != null) difficultyPanel.SetActive(true);
            RefreshDifficultyPanel();
        }
        else if (_overlayMode == OverlayMode.SkillUnlock && _selectedOc != null)
        {
            if (skillListPanel != null) skillListPanel.SetActive(true);
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

    // ── 내부 유틸 ─────────────────────────────────────────────────────────

    TMP_FontAsset GetFont()
    {
        if (detailNameText  != null && detailNameText.font  != null) return detailNameText.font;
        if (detailLevelText != null && detailLevelText.font != null) return detailLevelText.font;
        return null;
    }

    static CrystalKind CrystalFor(CharacterDef def) => def.continent switch
    {
        Continent.Physics   => CrystalKind.PrimeForce,
        Continent.Chemistry => CrystalKind.ElementaCrystal,
        Continent.Biology   => CrystalKind.LifeCode,
        Continent.Math      => CrystalKind.Axioma,
        Continent.Info      => CrystalKind.PrimeData,
        Continent.EarthSci  => EarthSciCrystal(def.country),
        _                    => CrystalKind.PrimeForce,  // Mesoria 등 fallback
    };

    static CrystalKind EarthSciCrystal(string country)
    {
        if (string.IsNullOrEmpty(country)) return CrystalKind.MemoryOfStar;
        var c = country.ToLowerInvariant();
        if (c.Contains("geo")   || c.Contains("rock"))  return CrystalKind.BoneOfTheEarth;
        if (c.Contains("ocean") || c.Contains("sea"))   return CrystalKind.OceanPrime;
        if (c.Contains("atmo")  || c.Contains("wind"))  return CrystalKind.TornadoCore;
        return CrystalKind.MemoryOfStar;
    }

    static string CrystalKindKo(CrystalKind k) => k switch
    {
        CrystalKind.PrimeForce      => "프라임포스",
        CrystalKind.ElementaCrystal => "엘레멘타",
        CrystalKind.LifeCode        => "라이프코드",
        CrystalKind.MemoryOfStar    => "별의기억",
        CrystalKind.BoneOfTheEarth  => "대지의뼈",
        CrystalKind.OceanPrime      => "오션프라임",
        CrystalKind.TornadoCore     => "토네이도코어",
        CrystalKind.Axioma          => "악시오마",
        CrystalKind.PrimeData       => "프라임데이터",
        _                            => k.ToString(),
    };

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
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 52f;
        go.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.30f, 0.9f);
        go.AddComponent<Button>();

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 0f); trt.offsetMax = new Vector2(-12f, 0f);
        var t = textGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 17; t.color = Color.white;
        t.textWrappingMode = TextWrappingModes.NoWrap;  // 가로 길게 — 줄바꿈 방지
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) t.font = font;

        return go;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자 패널 문제탭의 추가/수정 폼. ProblemPanel의 자식으로 배치되며
/// ProblemTab이 열고 닫는다. 저장/삭제는 ProblemSyncService(CloudScript)를 거친다.
/// </summary>
public class ProblemFormPanel : MonoBehaviour
{
    [Header("용도/분류")]
    [SerializeField] TMP_Dropdown   purposeDropdown;   // 레벨업 / 스킬
    [SerializeField] TMP_Dropdown   subjectDropdown;   // 과목 (레벨업용)
    [SerializeField] TMP_InputField countryInput;      // 국가 (레벨업용)
    [SerializeField] TMP_Dropdown   skillDropdown;     // 캐릭터-스킬 (스킬용)
    [SerializeField] TMP_Dropdown   difficultyDropdown;

    [Header("문제 내용")]
    [SerializeField] TMP_Dropdown   typeDropdown;      // 객관식/주관식
    [SerializeField] TMP_InputField promptInput;
    [SerializeField] TMP_InputField choice1Input;
    [SerializeField] TMP_InputField choice2Input;
    [SerializeField] TMP_InputField choice3Input;
    [SerializeField] TMP_InputField choice4Input;
    [SerializeField] TMP_Dropdown   correctIndexDropdown;
    [SerializeField] TMP_InputField acceptedAnswersInput; // 콤마 구분
    [SerializeField] TMP_InputField explanationInput;

    [Header("상태·버튼")]
    [SerializeField] TMP_Text idLabel;
    [SerializeField] TMP_Text statusText;
    [SerializeField] Button   saveButton;
    [SerializeField] Button   deleteButton;
    [SerializeField] Button   cancelButton;

    /// <summary>저장·삭제 성공 시 호출 — ProblemTab이 트리를 다시 그리는 데 사용.</summary>
    public Action onChanged;

    static readonly Continent[] _subjects =
    {
        Continent.Physics, Continent.Chemistry, Continent.Biology,
        Continent.EarthSci, Continent.Math, Continent.Info,
    };

    string   _editingId; // null이면 신규
    string[] _skillIds;  // skillDropdown 인덱스 → skillId

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Start()
    {
        if (saveButton   != null) saveButton.onClick.AddListener(OnSaveClicked);
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);

        if (purposeDropdown != null)
        {
            purposeDropdown.ClearOptions();
            purposeDropdown.AddOptions(new List<string> { "레벨업", "스킬" });
            purposeDropdown.onValueChanged.AddListener(_ => RefreshInteractable());
        }
        if (subjectDropdown != null)
        {
            subjectDropdown.ClearOptions();
            subjectDropdown.AddOptions(_subjects.Select(ProblemTab.ContinentLabel).ToList());
        }
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string> { "하", "중", "상" });
        }
        if (typeDropdown != null)
        {
            typeDropdown.ClearOptions();
            typeDropdown.AddOptions(new List<string> { "객관식", "주관식" });
            typeDropdown.onValueChanged.AddListener(_ => RefreshInteractable());
        }
        if (correctIndexDropdown != null)
        {
            correctIndexDropdown.ClearOptions();
            correctIndexDropdown.AddOptions(new List<string> { "보기1", "보기2", "보기3", "보기4" });
        }
    }

    // ── 열기 ──────────────────────────────────────────────────────────────

    public void OpenNew()
    {
        _editingId = null;
        RefreshSkillDropdown();

        if (idLabel != null) idLabel.text = "새 문제";
        SetField(purposeDropdown, 0);
        SetField(subjectDropdown, 0);
        if (countryInput != null) countryInput.text = "";
        SetField(skillDropdown, 0);
        SetField(difficultyDropdown, 0);
        SetField(typeDropdown, 0);
        if (promptInput  != null) promptInput.text  = "";
        if (choice1Input != null) choice1Input.text = "";
        if (choice2Input != null) choice2Input.text = "";
        if (choice3Input != null) choice3Input.text = "";
        if (choice4Input != null) choice4Input.text = "";
        SetField(correctIndexDropdown, 0);
        if (acceptedAnswersInput != null) acceptedAnswersInput.text = "";
        if (explanationInput     != null) explanationInput.text     = "";
        if (deleteButton != null) deleteButton.interactable = false;

        SetStatus("", Color.white);
        RefreshInteractable();
        gameObject.SetActive(true);
    }

    public void OpenEdit(ProblemDef def)
    {
        if (def == null) { OpenNew(); return; }

        _editingId = def.id;
        RefreshSkillDropdown();

        if (idLabel != null) idLabel.text = def.id;

        bool isSkill = !string.IsNullOrEmpty(def.skillId);
        SetField(purposeDropdown, isSkill ? 1 : 0);

        int subjectIdx = Array.IndexOf(_subjects, def.subject);
        SetField(subjectDropdown, subjectIdx >= 0 ? subjectIdx : 0);
        if (countryInput != null) countryInput.text = def.country ?? "";

        if (_skillIds != null)
        {
            int skillIdx = Array.IndexOf(_skillIds, def.skillId);
            SetField(skillDropdown, skillIdx >= 0 ? skillIdx : 0);
        }

        SetField(difficultyDropdown, (int)def.difficulty);
        SetField(typeDropdown, (int)def.type);
        if (promptInput != null) promptInput.text = def.prompt ?? "";

        var c = def.choices ?? new string[4];
        if (choice1Input != null) choice1Input.text = c.Length > 0 ? c[0] : "";
        if (choice2Input != null) choice2Input.text = c.Length > 1 ? c[1] : "";
        if (choice3Input != null) choice3Input.text = c.Length > 2 ? c[2] : "";
        if (choice4Input != null) choice4Input.text = c.Length > 3 ? c[3] : "";
        SetField(correctIndexDropdown, Mathf.Clamp(def.correctIndex, 0, 3));

        if (acceptedAnswersInput != null)
            acceptedAnswersInput.text = def.acceptedAnswers != null ? string.Join(", ", def.acceptedAnswers) : "";
        if (explanationInput != null) explanationInput.text = def.explanation ?? "";

        if (deleteButton != null) deleteButton.interactable = true;

        SetStatus("", Color.white);
        RefreshInteractable();
        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    static void SetField(TMP_Dropdown dd, int value) { if (dd != null) dd.value = value; }

    // ── 필드 상호배제 ─────────────────────────────────────────────────────

    void RefreshInteractable()
    {
        bool isSkill = purposeDropdown != null && purposeDropdown.value == 1;
        if (subjectDropdown != null) subjectDropdown.interactable = !isSkill;
        if (countryInput    != null) countryInput.interactable    = !isSkill;
        if (skillDropdown   != null) skillDropdown.interactable   = isSkill;

        bool isMulti = typeDropdown != null && typeDropdown.value == 0;
        if (choice1Input != null) choice1Input.interactable = isMulti;
        if (choice2Input != null) choice2Input.interactable = isMulti;
        if (choice3Input != null) choice3Input.interactable = isMulti;
        if (choice4Input != null) choice4Input.interactable = isMulti;
        if (correctIndexDropdown != null) correctIndexDropdown.interactable = isMulti;
        if (acceptedAnswersInput != null) acceptedAnswersInput.interactable = !isMulti;
    }

    void RefreshSkillDropdown()
    {
        var db     = Resources.Load<CharacterDatabase>("CharacterDatabase");
        var labels = new List<string>();
        var ids    = new List<string>();
        if (db != null)
        {
            foreach (var def in db.All)
            {
                if (def == null || def.skills == null) continue;
                foreach (var skill in def.skills)
                {
                    if (skill == null) continue;
                    labels.Add($"{def.nameKo} - {skill.nameKo}");
                    ids.Add(skill.id);
                }
            }
        }
        _skillIds = ids.ToArray();

        if (skillDropdown == null) return;
        skillDropdown.ClearOptions();
        skillDropdown.AddOptions(labels.Count > 0 ? labels : new List<string> { "(스킬 없음)" });
    }

    // ── 저장/삭제 ─────────────────────────────────────────────────────────

    void OnSaveClicked()
    {
        var def = ScriptableObject.CreateInstance<ProblemDef>();
        def.id = _editingId ?? ("usr_" + Guid.NewGuid().ToString("N").Substring(0, 10));

        bool isSkill = purposeDropdown != null && purposeDropdown.value == 1;
        if (isSkill)
        {
            def.skillId = (skillDropdown != null && _skillIds != null && skillDropdown.value < _skillIds.Length)
                ? _skillIds[skillDropdown.value] : "";
            def.subject = default;
            def.country = "";
        }
        else
        {
            def.skillId = "";
            def.subject = _subjects[Mathf.Clamp(subjectDropdown != null ? subjectDropdown.value : 0, 0, _subjects.Length - 1)];
            def.country = countryInput != null ? countryInput.text.Trim() : "";
        }

        def.difficulty = (ProblemDifficulty)(difficultyDropdown != null ? difficultyDropdown.value : 0);
        def.type       = (ProblemType)(typeDropdown != null ? typeDropdown.value : 0);
        def.prompt     = promptInput != null ? promptInput.text : "";

        def.choices = new[]
        {
            choice1Input != null ? choice1Input.text : "",
            choice2Input != null ? choice2Input.text : "",
            choice3Input != null ? choice3Input.text : "",
            choice4Input != null ? choice4Input.text : "",
        };
        def.correctIndex = correctIndexDropdown != null ? correctIndexDropdown.value : 0;

        def.acceptedAnswers = acceptedAnswersInput != null && !string.IsNullOrWhiteSpace(acceptedAnswersInput.text)
            ? acceptedAnswersInput.text.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray()
            : new string[0];

        def.explanation = explanationInput != null ? explanationInput.text : "";

        if (isSkill && string.IsNullOrEmpty(def.skillId))
        {
            SetStatus("스킬을 선택하세요.", Color.yellow);
            return;
        }
        if (string.IsNullOrWhiteSpace(def.prompt))
        {
            SetStatus("문제 지문을 입력하세요.", Color.yellow);
            return;
        }

        SetStatus("저장 중...", Color.white);
        ProblemSyncService.Upsert(def,
            () => { SetStatus("저장 완료", Color.green); onChanged?.Invoke(); Close(); },
            err => SetStatus($"저장 실패: {err}", Color.red));
    }

    void OnDeleteClicked()
    {
        if (string.IsNullOrEmpty(_editingId)) return;

        SetStatus("삭제 중...", Color.white);
        ProblemSyncService.Delete(_editingId,
            () => { SetStatus("삭제 완료", Color.green); onChanged?.Invoke(); Close(); },
            err => SetStatus($"삭제 실패: {err}", Color.red));
    }

    void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }
}

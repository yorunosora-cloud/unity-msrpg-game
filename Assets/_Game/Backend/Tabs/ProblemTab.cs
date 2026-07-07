using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자 패널 D탭 — 문제 분류 트리(읽기 전용).
/// 용도(레벨업/스킬/미니게임) → 과목 → 국가 → (스킬 경로는 캐릭터→스킬 추가) → 난이도 → 개별 문제.
/// CRUD·TitleData 연동은 다음 라운드. 이번엔 기존 ProblemDatabase를 브라우징만 한다.
/// </summary>
public class ProblemTab : MonoBehaviour
{
    [SerializeField] RectTransform contentRoot;
    [SerializeField] AdminPanel    owner;
    [SerializeField] Button          addButton;
    [SerializeField] ProblemFormPanel formPanel;

    const float ROW_H  = 44f;
    const float INDENT = 22f;

    /// <summary>트리 노드 1개. 런타임 GameObject(행)와 1:1 대응.</summary>
    class TreeNode
    {
        public string        label;
        public int           depth;
        public bool          expanded;
        public bool          isLeafCount; // true면 label에 이미 "(N개)" 포함 — 접기/펼치기 화살표 없이 카운트만
        public TreeNode       parent;
        public List<TreeNode> children = new List<TreeNode>();
        public GameObject     rowGO;
        public TMP_Text       labelText;
        public ProblemDef     def; // 문제 미리보기 리프에만 세팅 — 클릭 시 편집 폼 오픈

        public TreeNode AddChild(TreeNode child)
        {
            child.parent = this;
            children.Add(child);
            return child;
        }
    }

    ProblemDatabase   _problemDb;
    CharacterDatabase _characterDb;
    readonly List<TreeNode> _flatRows = new List<TreeNode>(); // 생성 순서 = 표시 순서(부모 다음에 자식들)
    TreeNode _root; // 가상 루트(화면에 안 그림), children = 용도 3개

    static readonly Continent[] _subjects =
    {
        Continent.Physics, Continent.Chemistry, Continent.Biology,
        Continent.EarthSci, Continent.Math, Continent.Info,
    };

    static readonly ProblemDifficulty[] _difficulties =
    {
        ProblemDifficulty.Low, ProblemDifficulty.Mid, ProblemDifficulty.High,
    };

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (_problemDb == null)   _problemDb   = Resources.Load<ProblemDatabase>("ProblemDatabase");
        if (_characterDb == null) _characterDb = Resources.Load<CharacterDatabase>("CharacterDatabase");

        if (addButton != null)
        {
            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(() => formPanel?.OpenNew());
        }
        if (formPanel != null) formPanel.onChanged = Rebuild;

        Rebuild();
    }

    /// <summary>트리를 다시 빌드·렌더링한다. 폼 저장/삭제 성공 후에도 호출된다.</summary>
    void Rebuild()
    {
        BuildTree();
        RenderAll();
        Relayout();
    }

    // ── 트리 구성 ─────────────────────────────────────────────────────────

    void BuildTree()
    {
        _root = new TreeNode { depth = -1, expanded = true };

        var levelUpNode = _root.AddChild(new TreeNode { label = "레벨업", depth = 0 });
        BuildLevelUpBranch(levelUpNode);

        var skillNode = _root.AddChild(new TreeNode { label = "스킬 (해금 + 레벨업 공용 풀)", depth = 0 });
        BuildSkillBranch(skillNode);

        var miniGameNode = _root.AddChild(new TreeNode { label = "미니게임", depth = 0 });
        miniGameNode.AddChild(new TreeNode
        {
            label = "추후 구현 예정 (시스템 미구현)", depth = 1, isLeafCount = true,
        });
    }

    void BuildLevelUpBranch(TreeNode subjectParent)
    {
        if (_problemDb == null) return;

        foreach (var subject in _subjects)
        {
            var subjectNode = subjectParent.AddChild(new TreeNode { label = ContinentLabel(subject), depth = 1 });

            // 이 과목의 레벨업 문제(skillId 빈 것)들 중 country 유니크 목록
            var countries = new List<string>();
            foreach (var p in _problemDb.All)
            {
                if (p == null || !string.IsNullOrEmpty(p.skillId)) continue;
                if (p.subject != subject) continue;
                string c = p.country ?? "";
                if (!countries.Contains(c)) countries.Add(c);
            }
            if (countries.Count == 0) countries.Add(""); // 데이터 없어도 국가 슬롯 하나는 보여줌

            foreach (var country in countries)
            {
                var countryNode = subjectNode.AddChild(new TreeNode
                {
                    label = string.IsNullOrEmpty(country) ? "(미분류)" : country,
                    depth = 2,
                });

                foreach (var diff in _difficulties)
                {
                    var matches = new List<ProblemDef>();
                    foreach (var p in _problemDb.All)
                    {
                        if (p == null || !string.IsNullOrEmpty(p.skillId)) continue;
                        if (p.subject != subject) continue;
                        if ((p.country ?? "") != country) continue;
                        if (p.difficulty != diff) continue;
                        matches.Add(p);
                    }

                    var diffNode = countryNode.AddChild(new TreeNode
                    {
                        label = $"{ProblemDifficultyInfo.Label(diff)}  ({matches.Count}개)",
                        depth = 3,
                    });
                    foreach (var p in matches)
                        diffNode.AddChild(MakeProblemPreviewNode(p, 4));
                }
            }
        }
    }

    void BuildSkillBranch(TreeNode subjectParent)
    {
        if (_characterDb == null) return;

        foreach (var subject in _subjects)
        {
            var subjectNode = new TreeNode { label = ContinentLabel(subject), depth = 1 };

            // 이 과목 캐릭터들의 country 유니크 목록
            var countries = new List<string>();
            foreach (var def in _characterDb.All)
            {
                if (def == null || def.continent != subject) continue;
                string c = def.country ?? "";
                if (!countries.Contains(c)) countries.Add(c);
            }

            foreach (var country in countries)
            {
                var countryNode = new TreeNode
                {
                    label = string.IsNullOrEmpty(country) ? "(미분류)" : country,
                    depth = 2,
                };

                foreach (var def in _characterDb.All)
                {
                    if (def == null || def.continent != subject || (def.country ?? "") != country) continue;
                    if (def.skills == null || def.skills.Length == 0) continue;

                    var charNode = countryNode.AddChild(new TreeNode { label = def.nameKo, depth = 3 });

                    foreach (var skill in def.skills)
                    {
                        if (skill == null) continue;
                        var skillNode = charNode.AddChild(new TreeNode { label = skill.nameKo, depth = 4 });

                        var allForSkill = _problemDb != null ? _problemDb.AllBySkill(skill.id) : new ProblemDef[0];
                        foreach (var diff in _difficulties)
                        {
                            var matches = new List<ProblemDef>();
                            foreach (var p in allForSkill)
                                if (p != null && p.difficulty == diff) matches.Add(p);

                            var diffNode = skillNode.AddChild(new TreeNode
                            {
                                label = $"{ProblemDifficultyInfo.Label(diff)}  ({matches.Count}개)",
                                depth = 5,
                            });
                            foreach (var p in matches)
                                diffNode.AddChild(MakeProblemPreviewNode(p, 6));
                        }
                    }
                }
                if (countryNode.children.Count > 0)
                    subjectNode.AddChild(countryNode);
            }
            subjectParent.AddChild(subjectNode);
        }
    }

    static TreeNode MakeProblemPreviewNode(ProblemDef p, int depth)
    {
        string preview = string.IsNullOrEmpty(p.prompt) ? "(문제 텍스트 없음)" : p.prompt;
        if (preview.Length > 30) preview = preview.Substring(0, 30) + "...";
        return new TreeNode { label = $"· {preview}", depth = depth, isLeafCount = true, def = p };
    }

    public static string ContinentLabel(Continent c) => c switch
    {
        Continent.Physics   => "물리",
        Continent.Chemistry => "화학",
        Continent.Biology   => "생명과학",
        Continent.EarthSci  => "지구과학",
        Continent.Math      => "수학",
        Continent.Info      => "정보",
        _                   => c.ToString(),
    };

    // ── 렌더링 (전부 생성, SetActive 토글) ──────────────────────────────

    void RenderAll()
    {
        if (contentRoot == null) return;
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);
        _flatRows.Clear();

        foreach (var top in _root.children)
            CreateRowRecursive(top);
    }

    void CreateRowRecursive(TreeNode node)
    {
        node.rowGO = new GameObject("Row");
        node.rowGO.transform.SetParent(contentRoot, false);
        var rt = node.rowGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, ROW_H - 2f);

        var bg = node.rowGO.AddComponent<Image>();
        bg.color = node.depth % 2 == 0 ? UITheme.PanelBgMid : UITheme.PanelBgDark;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(node.rowGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8f + node.depth * INDENT, 0f);
        textRT.offsetMax = new Vector2(-8f, 0f);
        var txt = textGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = UITheme.FontBody + 1;
        txt.color     = UITheme.TextPrimary;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        node.labelText = txt;

        bool expandable = node.children.Count > 0 && !node.isLeafCount;
        UpdateLabel(node, expandable);

        if (expandable)
        {
            var btn = node.rowGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => { node.expanded = !node.expanded; UpdateLabel(node, true); Relayout(); });
        }
        else if (node.def != null)
        {
            var btn = node.rowGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => formPanel?.OpenEdit(node.def));
        }

        _flatRows.Add(node);

        foreach (var child in node.children)
            CreateRowRecursive(child);
    }

    void UpdateLabel(TreeNode node, bool expandable)
    {
        if (node.labelText == null) return;
        string prefix = expandable ? (node.expanded ? "▼ " : "▶ ") : "";
        node.labelText.text = prefix + node.label;
    }

    /// <summary>펼쳐진 조상만 보이도록 SetActive + Y좌표 재배치.</summary>
    void Relayout()
    {
        if (contentRoot == null) return;
        float y = 0f;
        int visibleCount = 0;

        foreach (var node in _flatRows)
        {
            bool visible = IsVisible(node);
            node.rowGO.SetActive(visible);
            if (!visible) continue;

            var rt = node.rowGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, -y);
            y += ROW_H;
            visibleCount++;
        }

        contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, visibleCount * ROW_H);
    }

    /// <summary>자기 자신을 제외한 모든 조상이 expanded=true 여야 보인다(가상 루트 _root는 항상 expanded).</summary>
    bool IsVisible(TreeNode node)
    {
        var p = node.parent;
        while (p != null && p != _root)
        {
            if (!p.expanded) return false;
            p = p.parent;
        }
        return true;
    }
}

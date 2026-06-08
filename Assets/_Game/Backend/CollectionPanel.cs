using UnityEngine;
using TMPro;

/// <summary>보유 캐릭터 컬렉션(도감) 패널.</summary>
public class CollectionPanel : MonoBehaviour
{
    [SerializeField] TMP_Text listText;

    CharacterDatabase _db;

    void OnEnable()
    {
        _db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        Refresh();
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged += Refresh;
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged -= Refresh;
        UIManager.Close();
    }

    void Refresh()
    {
        if (listText == null || !MetaState.IsInitialized) return;

        var owned = MetaState.Roster.Owned;
        if (owned.Count == 0)
        {
            listText.text = "보유 캐릭터가 없습니다.\n가챠를 뽑아 캐릭터를 획득해보세요!";
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"보유 캐릭터: {owned.Count}종");
        sb.AppendLine("─────────────────");

        foreach (var oc in owned)
        {
            var def  = _db?.ById(oc.id);
            string name = def != null ? def.nameKo : oc.id;
            string rar  = def != null ? $"[{def.rarity}]" : "[?]";
            string dup  = oc.dupes > 0 ? $"  +{oc.dupes}중복" : "";
            string skill = SkillProgress(oc, def);
            sb.AppendLine($"{rar} {name}  Lv.{oc.level}{dup}{skill}");
        }

        listText.text = sb.ToString().TrimEnd();
    }

    public void OnCloseClicked() => gameObject.SetActive(false);

    static string SkillProgress(OwnedCharacter oc, CharacterDef def)
    {
        if (def == null || def.skills == null || def.skills.Length == 0) return "";
        int total    = def.skills.Length;
        int unlocked = (oc.unlockedSkillIds == null || oc.unlockedSkillIds.Count == 0)
            ? 1  // 기본 스킬 폴백
            : oc.unlockedSkillIds.Count;
        unlocked = System.Math.Min(unlocked, total);
        return $"  스킬 {unlocked}/{total}";
    }
}

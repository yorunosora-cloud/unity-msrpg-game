using UnityEngine;
using TMPro;

/// <summary>결정(進化 재화) 인벤토리 패널. I 키로 열고 닫습니다.</summary>
public class InventoryPanel : MonoBehaviour
{
    [SerializeField] TMP_Text listText;

    void OnEnable()
    {
        Refresh();
        if (MetaState.IsInitialized)
            MetaState.Crystals.OnChanged += Refresh;
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Crystals.OnChanged -= Refresh;
        UIManager.Close();
    }

    void Refresh()
    {
        if (listText == null || !MetaState.IsInitialized) return;

        var sb    = new System.Text.StringBuilder();
        var kinds = (CrystalKind[])System.Enum.GetValues(typeof(CrystalKind));

        bool hasAny = false;
        foreach (var kind in kinds)
        {
            int amt = MetaState.Crystals.Get(kind);
            if (amt <= 0) continue;
            sb.AppendLine($"{CrystalCatalog.ContinentLabel(kind)}  {CrystalCatalog.DisplayName(kind)}  x{amt}");
            hasAny = true;
        }

        listText.text = hasAny ? sb.ToString().TrimEnd() : "보유 결정이 없습니다.";
    }

    public void OnCloseClicked() => gameObject.SetActive(false);
}

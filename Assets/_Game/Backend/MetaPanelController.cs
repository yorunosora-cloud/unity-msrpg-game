using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 메타 레이어 패널 토글 컨트롤러.
///   C   — 도감 (CharacterCollection)
///   Tab — 인벤토리
///   F1  — 관리자 패널 (권한 있을 때만)
///   E   — 건물 상호작용 (OpenLab/OpenLibrary 메서드, Interactable.onInteract에서 호출됨)
/// 하나의 패널이 열려 있으면 다른 패널은 열 수 없습니다 (UIManager).
/// </summary>
public class MetaPanelController : MonoBehaviour
{
    [Header("패널 (기본 비활성)")]
    [SerializeField] GameObject collectionPanel;
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] GameObject rnePanel;
    [SerializeField] GameObject adminPanel;
    [SerializeField] GameObject gachaPanel;   // 도서관 — E키 상호작용으로만 열림

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool inputFocused = IsAnyInputFieldFocused();
        if (kb.cKey.wasPressedThisFrame   && !inputFocused)  Toggle(collectionPanel);
        if (kb.tabKey.wasPressedThisFrame  && !inputFocused)  Toggle(inventoryPanel);
        // rnePanel·gachaPanel 은 건물 E키 상호작용 (OpenLab/OpenLibrary) 으로만 열림
        if (kb.f1Key.wasPressedThisFrame   && !inputFocused && AdminPanel.ShouldAllow()) Toggle(adminPanel);
    }

    static bool IsAnyInputFieldFocused()
    {
        var go = EventSystem.current?.currentSelectedGameObject;
        return go != null && go.GetComponentInParent<TMP_InputField>() != null;
    }

    void Toggle(GameObject panel)
    {
        if (panel == null) return;
        if (panel.activeSelf)
            panel.SetActive(false); // OnDisable → UIManager.Close()
        else if (UIManager.TryOpen())
            panel.SetActive(true);
    }

    /// <summary>
    /// 연구소(Lab) → R&amp;E 패널 오픈.
    /// <see cref="Interactable.onInteract"/> 에서 호출됨.
    /// </summary>
    public void OpenLab()
    {
        if (rnePanel == null) return;
        if (!UIManager.TryOpen()) return;
        rnePanel.SetActive(true);
    }

    /// <summary>
    /// 도서관(Library) → 가챠 패널 오픈.
    /// <see cref="Interactable.onInteract"/> 에서 호출됨.
    /// </summary>
    public void OpenLibrary()
    {
        if (gachaPanel == null) return;
        if (!UIManager.TryOpen()) return;
        gachaPanel.SetActive(true);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 메타 레이어 패널 토글 컨트롤러.
///   C   — 도감 (CharacterCollection)
///   I   — 인벤토리 (결정)
///   F1  — 관리자 패널 (권한 있을 때만)
/// 하나의 패널이 열려 있으면 다른 패널은 열 수 없습니다 (UIManager).
/// </summary>
public class MetaPanelController : MonoBehaviour
{
    [Header("패널 (기본 비활성)")]
    [SerializeField] GameObject collectionPanel;
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] GameObject adminPanel;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.cKey.wasPressedThisFrame) Toggle(collectionPanel);
        if (kb.iKey.wasPressedThisFrame) Toggle(inventoryPanel);
        if (kb.f1Key.wasPressedThisFrame && AdminPanel.ShouldAllow()) Toggle(adminPanel);
    }

    void Toggle(GameObject panel)
    {
        if (panel == null) return;
        if (panel.activeSelf)
            panel.SetActive(false); // OnDisable → UIManager.Close()
        else if (UIManager.TryOpen())
            panel.SetActive(true);
    }
}

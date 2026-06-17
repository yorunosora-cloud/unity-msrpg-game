using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player에 부착. 근처 Interactable을 감지하고 E키로 발동한다.
/// <para>
/// - <see cref="Interactable.All"/> 레지스트리를 매 프레임 폴링해 가장 가까운 대상 선택.<br/>
/// - 대상이 있으면 <see cref="promptLabel"/> 텍스트를 표시, 없으면 숨김.<br/>
/// - E키 + 패널 닫힘 + 대상 존재 → <see cref="Interactable.Activate()"/> 호출.
/// </para>
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] TMP_Text promptLabel;

    Interactable _current;

    void Update()
    {
        _current = Interactable.FindNearest(transform.position, Interactable.All);

        // 프롬프트 라벨 표시 제어
        if (promptLabel != null)
        {
            bool hasTarget = _current != null;
            if (promptLabel.gameObject.activeSelf != hasTarget)
                promptLabel.gameObject.SetActive(hasTarget);
            if (hasTarget)
                promptLabel.text = _current.promptText;
        }

        // E키 + 패널 닫힘 → 상호작용 발동
        var kb = Keyboard.current;
        if (kb == null || _current == null) return;
        if (UIManager.IsAnyPanelOpen)     return;
        if (kb.eKey.wasPressedThisFrame)
            _current.Activate();
    }
}

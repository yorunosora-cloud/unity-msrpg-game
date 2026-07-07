using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자/개발자 패널 셸 (탭바 + 탭 전환).
/// 에디터·dev 빌드·admin 계정(MetaState.IsAdmin)에서만 접근 가능.
/// F1로 토글하거나 AccountPanel의 관리자 로그인 흐름으로 열 수 있습니다.
/// </summary>
public class AdminPanel : MonoBehaviour
{
    [SerializeField] Button[]     tabButtons;   // [0]커맨드 [1]캐릭터 [2]플레이어 [3]문제
    [SerializeField] GameObject[] tabPanels;    // 탭별 컨테이너 GO
    [SerializeField] TMP_Text     statusText;
    [SerializeField] Button       closeButton;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (!ShouldAllow())
            enabled = false;
    }

    void OnEnable()
    {
        SetTab(0);
        ShowStatus("관리자 패널", Color.yellow);
    }

    // ── 탭 전환 ───────────────────────────────────────────────────────────

    public void SetTab(int idx)
    {
        if (tabPanels == null) return;
        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null)
                tabPanels[i].SetActive(i == idx);
        }
        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == idx) ? UITheme.BtnPrimary : UITheme.BtnNeutral;
            }
        }
    }

    // 탭 버튼용 무인자 래퍼 (UnityEventTools 영구 리스너용)
    public void OnTab0() => SetTab(0);
    public void OnTab1() => SetTab(1);
    public void OnTab2() => SetTab(2);
    public void OnTab3() => SetTab(3);

    // ── 닫기 ──────────────────────────────────────────────────────────────

    public void OnCloseClicked()
    {
        gameObject.SetActive(false); // OnDisable → UIManager.Close()
    }

    void OnDisable() => UIManager.Close();

    // ── 상태 표시 (탭에서 공유 호출) ─────────────────────────────────────

    public void ShowStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    /// <summary>admin 패널 접근 허용 조건.</summary>
    public static bool ShouldAllow() =>
        Application.isEditor || Debug.isDebugBuild || MetaState.IsAdmin;
}

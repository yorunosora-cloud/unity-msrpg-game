using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 머리 위 월드 스페이스 Canvas HP바.
/// Enemy.BuildVisual()에서 WorldHealthBar.Create()로 생성된다.
/// LateUpdate에서 카메라를 향해 빌보드 회전.
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    Image _fill;
    Canvas _canvas;

    /// <summary>
    /// 월드 스페이스 HP바를 생성하고 parent 아래에 붙입니다.
    /// </summary>
    public static WorldHealthBar Create(Transform parent, Vector3 offset)
    {
        // Canvas (월드 스페이스)
        var canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(parent, false);
        canvasGO.transform.localPosition = offset;
        canvasGO.transform.localScale    = Vector3.one * 0.01f; // 월드 단위 조정

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 12f);

        // 배경 (어두운 회색)
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // 채움 (초록)
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        var fillRt = fillGO.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.sizeDelta = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.15f, 0.85f, 0.3f);
        fillImg.type  = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        var bar = canvasGO.AddComponent<WorldHealthBar>();
        bar._fill   = fillImg;
        bar._canvas = canvas;
        return bar;
    }

    /// <summary>0(비어있음) ~ 1(가득)로 HP 비율을 업데이트합니다.</summary>
    public void SetFraction(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        if (_fill != null) _fill.fillAmount = fraction;

        // 색상: 잔량에 따라 초록→노랑→빨강
        if (_fill != null)
            _fill.color = Color.Lerp(Color.red, new Color(0.15f, 0.85f, 0.3f), fraction);

        // 가득 차면 숨김
        if (_canvas != null)
            _canvas.enabled = fraction < 0.999f;
    }

    void LateUpdate()
    {
        // 카메라 빌보드
        var cam = Camera.main;
        if (cam == null) return;
        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                         cam.transform.rotation * Vector3.up);
    }
}

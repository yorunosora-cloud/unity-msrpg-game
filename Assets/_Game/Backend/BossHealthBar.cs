using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    TMP_Text _nameLabel;
    Slider   _hpSlider;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(string bossName, int phaseCount)
    {
        gameObject.SetActive(true);
        if (_nameLabel) _nameLabel.text = bossName;
        if (_hpSlider)  _hpSlider.value = 1f;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetHp(float fraction)
    {
        if (_hpSlider) _hpSlider.value = Mathf.Clamp01(fraction);
    }

    public static BossHealthBar BuildAndAttach(Transform parent)
    {
        var go = new GameObject("BossHealthBar");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 0.88f);
        rect.anchorMax = new Vector2(0.8f, 0.97f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = new GameObject("Bg");
        bg.transform.SetParent(go.transform, false);
        var bgImg  = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        var nameGO  = new GameObject("NameLabel");
        nameGO.transform.SetParent(go.transform, false);
        var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text      = "";
        nameTmp.fontSize  = 18;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color     = Color.white;
        var nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.55f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = nameRect.offsetMax = Vector2.zero;

        var sliderGO = new GameObject("HpSlider");
        sliderGO.transform.SetParent(go.transform, false);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 1f;
        slider.interactable = false;

        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = fillAreaRect.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg  = fill.AddComponent<Image>();
        fillImg.color = new Color(0.85f, 0.1f, 0.1f);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;

        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.offsetMin = new Vector2(8f, 4f);
        sliderRect.offsetMax = new Vector2(-8f, -4f);

        var bar = go.AddComponent<BossHealthBar>();
        bar._nameLabel = nameTmp;
        bar._hpSlider  = slider;

        go.SetActive(false);
        return bar;
    }
}

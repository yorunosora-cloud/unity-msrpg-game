using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UR 등급 UI 요소에 부착해 hue-shift 무지개 색 애니메이션을 구현한다.
/// Image 컴포넌트가 같은 GameObject에 있어야 한다.
/// </summary>
[RequireComponent(typeof(Image))]
public class RarityColorAnimator : MonoBehaviour
{
    [SerializeField] float hueSpeed   = 0.25f;
    [SerializeField] float saturation = 0.85f;
    [SerializeField] float brightness = 1.00f;

    Image _image;

    void Awake() => _image = GetComponent<Image>();

    void Update()
    {
        float hue = Mathf.Repeat(Time.time * hueSpeed, 1f);
        _image.color = Color.HSVToRGB(hue, saturation, brightness);
    }
}

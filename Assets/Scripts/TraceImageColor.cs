using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraceImageColor : MonoBehaviour
{
    TMP_Text colorText;
    Image image;

    public bool useHSV;
    public bool includeValue;
    public bool includeHue;

    private void Awake()
    {
        image = GetComponentInChildren<Image>();
        colorText = GetComponentInChildren<TMP_Text>();
    }

    private void Update()
    {
        if (useHSV)
        {
            Color.RGBToHSV(image.color, out float h, out float s, out float v);
            colorText.text = "Saturation: " + s.ToString("F3");
            if (includeValue)
            {
                colorText.text += "\n Value: " + v.ToString("F3");
            }
            if (includeHue)
            {
                colorText.text = "Hue: " + h.ToString("F3") + "\n" + colorText.text;
            }
        }
        else colorText.text = "(" + image.color.r.ToString("F2") + ", " + image.color.g.ToString("F2") + ", " + image.color.b.ToString("F2") + "" + ")";
    }
}

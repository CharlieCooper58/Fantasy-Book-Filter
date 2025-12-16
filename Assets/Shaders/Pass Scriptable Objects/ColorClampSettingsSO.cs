using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Render Pass", menuName = "Render Passes/Color Clamp")]

public class ColorClampSettingsSO : PassInfoSO
{
    public ColorClampSettingsSO()
    {
        m_passType = PassType.colorClamp;
    }
    [Range(1, 200)] public int rClamp = 16;
    [Range(1, 200)] public int gClamp;
    [Range(1, 200)] public int bClamp;

    [Range(1, 200)] public int hueClamp;
    [Range(1, 200)] public int satClamp;
    [Range(1, 200)] public int valueClamp;

    [Range(0, 1)] public float hueShift;

    public bool useSameClampForAllChannels;
    public bool useHSV;
    public bool dither;
    [Range(0, 1)] public float ditherSpread;

    public bool preserveLowerValues;

}

using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Render Pass", menuName = "Render Passes/Downscale")]

public class DownscaleSettingsSO : PassInfoSO
{
    public DownscaleSettingsSO()
    {
        m_passType = PassType.downscale;
    }
    [Range(0, 4)] public int numDownscales;
    [Range(1, 3)] public float downscaleAmount = 2f;
}
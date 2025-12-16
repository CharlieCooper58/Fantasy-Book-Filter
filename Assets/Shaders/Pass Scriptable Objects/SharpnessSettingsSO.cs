using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Render Pass", menuName = "Render Passes/Sharpness")]
public class SharpnessSettingsSO : PassInfoSO
{
    public SharpnessSettingsSO()
    {
        m_passType = PassType.sharpness;
    }
    [SerializeField] public float sharpnessAmount;
}

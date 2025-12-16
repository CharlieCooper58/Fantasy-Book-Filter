using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Render Pass", menuName = "Render Passes/Blank Pass")]
public class PassInfoSO : ScriptableObject
{
    public enum PassType
    {
        kuwahara,
        colorClamp,
        downscale,
        sharpness,
        dither
    }
    public PassType m_passType;
    public Material passMaterial;

}

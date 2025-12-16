using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public float horizontalLookSensitivity = 1f;
    public float verticalLookSensitivity = 1f;
    void Awake()
    {
        instance = this;
    }
}

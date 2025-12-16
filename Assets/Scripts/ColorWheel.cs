using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class ColorWheel : MonoBehaviour
{
    public int rows = 32;
    public int cols = 32;

    private Texture2D tex;
    private Material mat;

    public float value;

    private void Start()
    {
        
        mat = GetComponent<MeshRenderer>().material;


        // Create the texture
        tex = new Texture2D(cols, rows, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // preserves color blocks
        tex.wrapMode = TextureWrapMode.Clamp;

        // Fill texture with color wheel
        for (int y = 0; y < rows; y++)
        {
            float radius = y / (float)(rows - 1);
            for (int x = 0; x < cols; x++)
            {
                float theta = x / (float)cols;
                Color col = ColorFromHSV(theta, radius, value);
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();

        // Assign texture to RawImage
        mat.SetTexture("_MainTex", tex);
    }

    private void Update()
    {
        // Fill texture with color wheel
        for (int y = 0; y < rows; y++)
        {
            float radius = y / (float)(rows - 1);
            for (int x = 0; x < cols; x++)
            {
                float theta = x / (float)cols;
                Color col = ColorFromHSV(theta, radius, value);
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();

        // Assign texture to RawImage
        mat.SetTexture("_MainTex", tex);
    }

    private Color ColorFromHSV(float h, float s, float v)
    {

        float3 K = new float3(1.0f, 2.0f / 3.0f, 1.0f / 3.0f);
        float3 p = new float3(h, h, h) + K;
        p -= new float3(Mathf.Floor(p.x), Mathf.Floor(p.y), Mathf.Floor(p.z));
        p *= 6f;
        p -= new float3(3.0f, 3.0f, 3.0f);
        float3 clampedHue = new float3(
            Mathf.Clamp01(Mathf.Abs(p.x) - K.x), 
            Mathf.Clamp01(Mathf.Abs(p.y) - K.x), 
            Mathf.Clamp01(Mathf.Abs(p.z) - K.x)
            );

        float3 lerped = v * Vector3.Lerp(K.xxx, clampedHue, s);
        return new Color(lerped.x, lerped.y, lerped.z);


    }
     /*
    private float3 HSV2RGB(float r, float g, float b)
    {
        float3 rgb2hsv(float3 c)
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
            float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;
            return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }

        float4 K = float4(0f, -1f / 3, 2f / 3, -1f);
        float4 p =

    } */
}

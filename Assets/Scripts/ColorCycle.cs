using UnityEngine;

public class ColorCycle : MonoBehaviour
{
    public float speed = 0.2f; // One full rainbow cycle every 5 seconds.
    [Range(0f, 1f)] public float saturation = 0.8f;
    [Range(0f, 1f)] public float brightness = 1f;

    private Material mat;

    private void Awake()
    {
        Renderer targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
            mat = targetRenderer.material; // Use this renderer's material copy.
    }

    private void Update()
    {
        if (mat == null)
            return;

        // Cycle through red, yellow, green, cyan, blue, and magenta.
        float hue = Mathf.Repeat(Time.time * speed, 1f);
        mat.SetColor("_BaseColor", Color.HSVToRGB(hue, saturation, brightness));
    }
}
using UnityEngine;

public class DataPoint : MonoBehaviour
{
    // -1 means no label has been assigned.
    public int trueLabel = -1;
    public int cluster = -1;

    // The larger ball used in KNN mode.
    public bool isQuery = false;

    private Material bodyMat;
    private Material haloMat;
    private Color target = Color.white;
    private Color current = Color.white;
    private float baseScale;
    private float pulse;
    private float highlight = 1f;
    private float highlightTarget = 1f;

    private void Awake()
    {
        Renderer bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null)
            bodyMat = bodyRenderer.material;

        Transform halo = transform.Find("Halo");
        if (halo != null)
        {
            Renderer haloRenderer = halo.GetComponent<Renderer>();
            if (haloRenderer != null)
                haloMat = haloRenderer.material;
        }

        baseScale = transform.localScale.x;
    }

    public void SetBaseScale(float scale)
    {
        baseScale = scale;
    }

    public void SetColor(Color color, bool instant = false)
    {
        target = color;

        if (instant)
        {
            current = color;
            Apply();
        }
    }

    // Grow and shrink once.
    public void Pulse()
    {
        pulse = 1f;
    }

    public void SetHighlight(bool on)
    {
        highlightTarget = on ? 1.8f : 1f;
    }

    private void Update()
    {
        current = Color.Lerp(current, target, 8f * Time.deltaTime);
        Apply();

        pulse = Mathf.MoveTowards(pulse, 0f, 2f * Time.deltaTime);
        highlight = Mathf.Lerp(
            highlight,
            highlightTarget,
            10f * Time.deltaTime);

        float scale = baseScale * highlight *
                      (1f + 0.6f * Mathf.Sin(pulse * Mathf.PI));

        transform.localScale = Vector3.one * scale;
    }

    private void Apply()
    {
        if (bodyMat != null)
            bodyMat.SetColor("_BaseColor", current);

        if (haloMat != null)
        {
            haloMat.SetColor(
                "_BaseColor",
                new Color(current.r, current.g, current.b, 0.15f));
        }
    }

    private void OnDestroy()
    {
        if (bodyMat != null)
            Destroy(bodyMat);

        if (haloMat != null)
            Destroy(haloMat);
    }
}
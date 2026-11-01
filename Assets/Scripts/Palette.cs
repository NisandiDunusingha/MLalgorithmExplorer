using UnityEngine;

public static class Palette
{
    public static readonly Color[] Cluster =
    {
        new Color(0.00f, 0.90f, 1.00f), // Cyan
        new Color(1.00f, 0.25f, 0.70f), // Magenta
        new Color(1.00f, 0.85f, 0.10f), // Yellow
        new Color(0.45f, 1.00f, 0.25f), // Lime
        new Color(1.00f, 0.50f, 0.10f)  // Orange
    };

    public static readonly Color Unlabelled =
        new Color(0.85f, 0.85f, 0.95f);

    public static readonly Color Error =
        new Color(1.00f, 0.30f, 0.30f);

    // Returns a color for any integer, wrapping around the palette.
    public static Color Get(int i)
    {
        int n = Cluster.Length;
        return Cluster[((i % n) + n) % n];
    }
}

// Makes a thin two-point line.
public static class LineUtil
{
    public static LineRenderer Make(
        Transform parent,
        Material mat,
        float width,
        string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = mat;
        lr.positionCount = 2;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        return lr;
    }

    public static void SetColor(LineRenderer lr, Color c)
    {
        if (lr == null)
            return;

        lr.startColor = c;
        lr.endColor = c;
    }
}
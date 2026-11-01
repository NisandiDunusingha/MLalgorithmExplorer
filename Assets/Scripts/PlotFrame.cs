using UnityEngine;

public class PlotFrame : MonoBehaviour
{
    public Material lineMaterial; // Assign VertexColorMat.
    public float size = 0.3f;
    public int gridDivisions = 6;
    public Color frameColor = new Color(0.45f, 0.8f, 1f, 0.7f);
    public Color gridColor = new Color(0.45f, 0.6f, 1f, 0.25f);

    private void Start()
    {
        float halfSize = size / 2f;

        // Floor grid.
        if (gridDivisions > 0)
        {
            for (int i = 0; i <= gridDivisions; i++)
            {
                float t = -halfSize + size * i / gridDivisions;

                MakeLine(
                    new Vector3(-halfSize, 0f, t),
                    new Vector3(halfSize, 0f, t),
                    gridColor,
                    0.0015f);

                MakeLine(
                    new Vector3(t, 0f, -halfSize),
                    new Vector3(t, 0f, halfSize),
                    gridColor,
                    0.0015f);
            }
        }

        // Four vertical edges and the top square.
        Vector3[] corners =
        {
            new Vector3(-halfSize, 0f, -halfSize),
            new Vector3(halfSize, 0f, -halfSize),
            new Vector3(halfSize, 0f, halfSize),
            new Vector3(-halfSize, 0f, halfSize)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 top = corners[i] + Vector3.up * size;
            Vector3 nextTop =
                corners[(i + 1) % corners.Length] + Vector3.up * size;

            MakeLine(corners[i], top, frameColor, 0.003f);
            MakeLine(top, nextTop, frameColor, 0.003f);
        }

        // Axes meet at one corner: X red, Y lime, Z blue.
        Vector3 origin = new Vector3(-halfSize, 0f, -halfSize);

        MakeLine(origin, origin + Vector3.right * size, Palette.Error, 0.006f);
        MakeLine(origin, origin + Vector3.up * size, Palette.Get(3), 0.006f);
        MakeLine(
            origin,
            origin + Vector3.forward * size,
            new Color(0.3f, 0.6f, 1f),
            0.006f);
    }

    private void MakeLine(Vector3 a, Vector3 b, Color color, float width)
    {
        LineRenderer line = LineUtil.Make(
            transform,
            lineMaterial,
            width,
            "Line");

        line.SetPosition(0, a);
        line.SetPosition(1, b);
        LineUtil.SetColor(line, color);
    }
}
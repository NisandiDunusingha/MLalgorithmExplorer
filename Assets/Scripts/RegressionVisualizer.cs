using System.Collections.Generic;
using UnityEngine;

public class RegressionVisualizer : AlgorithmVisualizer
{
    [Header("Parts of the scene")]
    public Material lineMaterial;
    public Transform bowl;
    public Transform ball;
    public Transform minMarker;

    [Header("Learning")]
    public float learningRate = 0.2f;
    public float stepInterval = 0.12f;

    // The data uses unit coordinates: 1 unit = 13 cm.
    // y = 0 sits 15 cm above the floor.
    private const float S = 0.13f;
    private const float Y0 = 0.15f;

    // The bowl shows m from -2 to 2 and b from -1.5 to 1.5.
    private const float M_RANGE = 2f;
    private const float B_RANGE = 1.5f;
    private const float BOWL_W = 0.28f;
    private const float BOWL_H = 0.18f;
    private const float LOSS_MAX = 3f;
    private const int GRID = 40;

    // The fitted line is y = m*x + b.
    private float m;
    private float b;
    private int steps;

    private bool autoPlay;
    private bool converged;
    private bool diverged;
    private float timer;

    private float[] xs = new float[0];
    private float[] ys = new float[0];

    private LineRenderer fitLine;
    private readonly List<LineRenderer> errorSticks =
        new List<LineRenderer>();

    private Mesh bowlMesh;
    private bool trianglesBuilt;
    private Vector3[] bowlVerts;
    private Color[] bowlCols;
    private Vector3 ballTarget;

    public override string Title => "Linear Regression + Gradient Descent";

    public override string Explanation =>
        "Goal: find the straight line y = m*x + b that fits the points best.\n\n" +
        "- The red sticks are the errors. The loss is their average squared length.\n" +
        "- The bowl shows the loss for every possible line (m, b). The best line is at the bottom.\n" +
        "- The ball is our current line. Gradient descent rolls it downhill, step by step.\n" +
        "- The spinning lime diamond marks the exact best answer.\n\n" +
        "Try a very high learning rate: the ball bounces and flies away!";

    public override string Hint =>
        "Press Play and watch the ball roll down the bowl. Try a high learning rate!";

    public override bool IsPlaying => autoPlay;

    public override void MakeData(LabController lab)
    {
        // Create points around a random line, with some noise.
        float slope = Random.Range(0.4f, 1.0f) *
                      (Random.value < 0.5f ? -1f : 1f);

        for (int i = 0; i < 25; i++)
        {
            float ux = Random.Range(-1f, 1f);
            float uy = slope * ux + Random.Range(-0.25f, 0.25f);
            Vector3 position = new Vector3(ux * S, Y0 + uy * S, 0f);

            lab.SpawnPoint(lab.ClampToBox(position), Palette.Get(0));
        }
    }

    public override void Begin()
    {
        autoPlay = false;
        converged = false;
        diverged = false;
        steps = 0;
        timer = 0f;

        if (fitLine == null)
            fitLine = LineUtil.Make(transform, lineMaterial, 0.008f, "FitLine");

        // Start with a randomly chosen, relatively poor line.
        m = Random.Range(1.2f, 1.8f) *
            (Random.value < 0.5f ? -1f : 1f);

        b = Random.Range(0.8f, 1.2f) *
            (Random.value < 0.5f ? -1f : 1f);

        BuildErrorSticks();
        OnDataChanged();

        // Move the ball to the starting position and clear its previous trail.
        if (ball != null)
        {
            ball.localPosition = ballTarget;

            TrailRenderer trail = ball.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
                trail.startColor = Palette.Get(1);
                trail.endColor = new Color(1f, 0.25f, 0.7f, 0f);
            }
        }
    }

    // Called when the data changes, including when a point is dragged.
    public override void OnDataChanged()
    {
        if (LabController.Instance == null)
            return;

        List<DataPoint> points = LabController.Instance.points;

        // Flatten the points onto the x-y plane and read them in unit coordinates.
        if (xs.Length != points.Count)
        {
            xs = new float[points.Count];
            ys = new float[points.Count];
        }

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 position = points[i].transform.localPosition;
            position.z = 0f;
            points[i].transform.localPosition = position;

            xs[i] = position.x / S;
            ys[i] = (position.y - Y0) / S;
        }

        if (errorSticks.Count != points.Count)
            BuildErrorSticks();

        converged = false;
        diverged = false;

        RebuildBowl();
        UpdateMinMarker();
        UpdateVisuals();
    }

    public override void StepOnce()
    {
        if (diverged || converged || xs.Length == 0)
            return;

        // Calculate the gradient for m and b.
        int count = xs.Length;
        float gradM = 0f;
        float gradB = 0f;

        for (int i = 0; i < count; i++)
        {
            float error = m * xs[i] + b - ys[i];
            gradM += error * xs[i];
            gradB += error;
        }

        gradM = 2f * gradM / count;
        gradB = 2f * gradB / count;

        // Take a step downhill.
        m -= learningRate * gradM;
        b -= learningRate * gradB;
        steps++;

        if (Mathf.Abs(m) > 8f || Mathf.Abs(b) > 8f)
        {
            diverged = true;
            autoPlay = false;
            timer = 0f;
        }
        else if (gradM * gradM + gradB * gradB < 1e-6f)
        {
            converged = true;
            autoPlay = false;
            timer = 0f;
            LabController.Instance.Celebrate();
        }

        UpdateVisuals();
    }

    public override void TogglePlay()
    {
        if (converged || diverged)
        {
            Begin();
            autoPlay = true;
            timer = 0f;
            return;
        }

        autoPlay = !autoPlay;
        timer = 0f;
    }

    public override ParamSetup GetParamSetup()
    {
        ParamSetup setup = new ParamSetup
        {
            label = "Learning rate",
            min = 0.02f,
            max = 1.1f,
            value = learningRate,
            whole = false
        };

        return setup;
    }

    public override void SetParameter(float value)
    {
        learningRate = value;
    }

    // Mean squared error for the line y = mm*x + bb.
    private float Loss(float mm, float bb)
    {
        float sum = 0f;

        for (int i = 0; i < xs.Length; i++)
        {
            float error = mm * xs[i] + bb - ys[i];
            sum += error * error;
        }

        return sum / Mathf.Max(1, xs.Length);
    }

    // Convert unit coordinates to positions inside the plot box.
    private Vector3 ToPlot(float ux, float uy)
    {
        return new Vector3(ux * S, Y0 + uy * S, 0f);
    }

    private void UpdateVisuals()
    {
        float loss = Loss(m, b);

        // Draw the fitted line from x = -1 to x = +1.
        if (fitLine != null)
        {
            fitLine.SetPosition(0, ToPlot(-1f, -m + b));
            fitLine.SetPosition(1, ToPlot(1f, m + b));

            LineUtil.SetColor(
                fitLine,
                Color.Lerp(Palette.Get(3), Palette.Error, Mathf.Clamp01(loss))
            );
        }

        // Draw an error stick from each point to the fitted line.
        if (LabController.Instance != null)
        {
            List<DataPoint> points = LabController.Instance.points;

            for (int i = 0; i < points.Count && i < errorSticks.Count; i++)
            {
                if (errorSticks[i] == null)
                    continue;

                float lineY = m * xs[i] + b;
                float errorSize = Mathf.Abs(lineY - ys[i]);

                errorSticks[i].SetPosition(
                    0,
                    points[i].transform.localPosition
                );
                errorSticks[i].SetPosition(1, ToPlot(xs[i], lineY));

                LineUtil.SetColor(
                    errorSticks[i],
                    Color.Lerp(
                        Palette.Get(2),
                        Palette.Error,
                        Mathf.Clamp01(errorSize / 1.2f)
                    )
                );
            }
        }

        ballTarget = BowlPoint(m, b);

        string state = string.Empty;

        if (diverged)
            state = "   <color=#FF4D4D>Diverged! Learning rate too high</color>";
        else if (converged)
            state = "   <color=#73FF40>Converged!</color>";

        Status =
            "Step " + steps +
            "   Loss " + loss.ToString("0.000") +
            "\nm = " + m.ToString("0.00") +
            "   b = " + b.ToString("0.00") +
            state;
    }

    private void BuildErrorSticks()
    {
        foreach (LineRenderer line in errorSticks)
        {
            if (line != null)
                Destroy(line.gameObject);
        }

        errorSticks.Clear();

        if (LabController.Instance == null)
            return;

        int count = LabController.Instance.points.Count;

        for (int i = 0; i < count; i++)
        {
            errorSticks.Add(
                LineUtil.Make(transform, lineMaterial, 0.004f, "ErrorStick")
            );
        }
    }

    // Find the position on the bowl surface for parameters (mm, bb).
    private Vector3 BowlPoint(float mm, float bb)
    {
        mm = Mathf.Clamp(mm, -M_RANGE, M_RANGE);
        bb = Mathf.Clamp(bb, -B_RANGE, B_RANGE);

        float x = mm / M_RANGE * BOWL_W / 2f;
        float z = bb / B_RANGE * BOWL_W / 2f;
        float y = Mathf.Clamp01(Loss(mm, bb) / LOSS_MAX) * BOWL_H;

        return new Vector3(x, y + 0.012f, z);
    }

    // Low loss is cyan, followed by lime, yellow, and magenta at the top.
    private Color HeightColor(float height)
    {
        if (height < 0.33f)
        {
            return Color.Lerp(
                Palette.Get(0),
                Palette.Get(3),
                height / 0.33f
            );
        }

        if (height < 0.66f)
        {
            return Color.Lerp(
                Palette.Get(3),
                Palette.Get(2),
                (height - 0.33f) / 0.33f
            );
        }

        return Color.Lerp(
            Palette.Get(2),
            Palette.Get(1),
            (height - 0.66f) / 0.34f
        );
    }

    private void RebuildBowl()
    {
        if (bowl == null)
            return;

        int vertexCount = (GRID + 1) * (GRID + 1);

        if (bowlMesh == null)
        {
            bowlMesh = new Mesh
            {
                name = "LossBowl"
            };

            bowlVerts = new Vector3[vertexCount];
            bowlCols = new Color[vertexCount];

            MeshFilter meshFilter = bowl.GetComponent<MeshFilter>();
            if (meshFilter == null)
                return;

            meshFilter.sharedMesh = bowlMesh;
        }

        // Set a vertex for every (m, b) point on the grid.
        for (int row = 0; row <= GRID; row++)
        {
            for (int column = 0; column <= GRID; column++)
            {
                float u = column / (float)GRID;
                float v = row / (float)GRID;

                float loss = Loss(
                    Mathf.Lerp(-M_RANGE, M_RANGE, u),
                    Mathf.Lerp(-B_RANGE, B_RANGE, v)
                );

                float height = Mathf.Clamp01(loss / LOSS_MAX);
                int index = row * (GRID + 1) + column;

                bowlVerts[index] = new Vector3(
                    (u - 0.5f) * BOWL_W,
                    height * BOWL_H,
                    (v - 0.5f) * BOWL_W
                );

                bowlCols[index] = HeightColor(height);
            }
        }

        bowlMesh.vertices = bowlVerts;

        // The grid triangles do not change, so build them once.
        if (!trianglesBuilt)
        {
            int[] triangles = new int[GRID * GRID * 6];
            int triangleIndex = 0;

            for (int row = 0; row < GRID; row++)
            {
                for (int column = 0; column < GRID; column++)
                {
                    int i0 = row * (GRID + 1) + column;
                    int i1 = i0 + 1;
                    int i2 = i0 + GRID + 1;
                    int i3 = i2 + 1;

                    triangles[triangleIndex++] = i0;
                    triangles[triangleIndex++] = i2;
                    triangles[triangleIndex++] = i1;

                    triangles[triangleIndex++] = i1;
                    triangles[triangleIndex++] = i2;
                    triangles[triangleIndex++] = i3;
                }
            }

            bowlMesh.triangles = triangles;
            trianglesBuilt = true;
        }

        bowlMesh.colors = bowlCols;
        bowlMesh.RecalculateBounds();
        bowlMesh.RecalculateNormals();
    }

    // Place the marker at the exact best-fit line.
    private void UpdateMinMarker()
    {
        int count = xs.Length;

        if (count == 0 || minMarker == null)
            return;

        float meanX = 0f;
        float meanY = 0f;

        for (int i = 0; i < count; i++)
        {
            meanX += xs[i];
            meanY += ys[i];
        }

        meanX /= count;
        meanY /= count;

        float covariance = 0f;
        float varianceX = 0f;

        for (int i = 0; i < count; i++)
        {
            covariance += (xs[i] - meanX) * (ys[i] - meanY);
            varianceX += (xs[i] - meanX) * (xs[i] - meanX);
        }

        if (varianceX < 1e-6f)
            return;

        float bestM = covariance / varianceX;
        float bestB = meanY - bestM * meanX;

        minMarker.localPosition = BowlPoint(bestM, bestB);
    }

    private void Update()
    {
        // Play mode: perform one gradient descent step every stepInterval seconds.
        if (autoPlay)
        {
            timer += Time.deltaTime;
            float interval = Mathf.Max(0.01f, stepInterval);

            if (timer >= interval)
            {
                timer = 0f;
                StepOnce();
            }
        }

        // Roll the ball smoothly toward the current line's position on the bowl.
        if (ball != null)
        {
            ball.localPosition = Vector3.Lerp(
                ball.localPosition,
                ballTarget,
                10f * Time.deltaTime
            );
        }
    }

    private void OnDestroy()
    {
        if (bowlMesh != null)
            Destroy(bowlMesh);
    }
}
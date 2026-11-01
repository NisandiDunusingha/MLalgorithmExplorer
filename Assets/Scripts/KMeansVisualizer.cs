using System.Collections.Generic;
using UnityEngine;

public class KMeansVisualizer : AlgorithmVisualizer
{
    public GameObject centroidPrefab;
    public Material lineMaterial;
    public int k = 3;
    public float stepInterval = 1.0f;

    private enum Phase
    {
        Assign,
        Update
    }

    private Phase phase = Phase.Assign;
    private readonly List<Transform> centroids = new List<Transform>();
    private readonly List<Vector3> targets = new List<Vector3>();
    private readonly List<LineRenderer> links = new List<LineRenderer>();

    private int iteration;
    private bool autoPlay;
    private bool converged;
    private float timer;

    public override string Title => "K-Means Clustering";

    public override string Explanation =>
        "Goal: split unlabelled data into k groups.\n\n" +
        "1. Place k centroids (the glowing diamonds).\n" +
        "2. ASSIGN: every point joins its nearest centroid and takes its colour.\n" +
        "3. UPDATE: every centroid glides to the average position of its points.\n" +
        "4. Repeat until nothing changes.\n\n" +
        "Try it: change k, press Reset for new random start centroids, or drag a point.";

    public override string Hint =>
        "Press Step (ASSIGN, then UPDATE) or Play. Drag any point!";

    public override bool IsPlaying => autoPlay;

    public override void MakeData(LabController lab)
    {
        lab.MakeBlobs(60, 3, false);
    }

    public override void Begin()
    {
        LabController lab = LabController.Instance;

        // Remove old centroids and link lines.
        foreach (Transform centroid in centroids)
        {
            if (centroid != null)
                Destroy(centroid.gameObject);
        }

        foreach (LineRenderer link in links)
        {
            if (link != null)
                Destroy(link.gameObject);
        }

        centroids.Clear();
        targets.Clear();
        links.Clear();

        // Reset points and create one link line for each point.
        foreach (DataPoint point in lab.points)
        {
            point.cluster = -1;
            point.SetColor(Palette.Unlabelled);
            links.Add(LineUtil.Make(transform, lineMaterial, 0.0025f, "Link"));
        }

        // Start with k centroids placed on random data points.
        List<DataPoint> pool = new List<DataPoint>(lab.points);

        for (int i = 0; i < k && pool.Count > 0; i++)
        {
            int pick = Random.Range(0, pool.Count);
            Vector3 position = pool[pick].transform.localPosition;
            pool.RemoveAt(pick);

            GameObject centroidObject = Instantiate(centroidPrefab, transform);
            centroidObject.transform.localPosition = position;

            Color color = Palette.Get(i);
            Renderer centroidRenderer = centroidObject.GetComponent<Renderer>();

            if (centroidRenderer != null)
                centroidRenderer.material.SetColor("_BaseColor", color);

            TrailRenderer trail = centroidObject.GetComponent<TrailRenderer>();

            if (trail != null)
            {
                trail.Clear();
                trail.startColor = color;
                trail.endColor = new Color(color.r, color.g, color.b, 0f);
            }

            centroids.Add(centroidObject.transform);
            targets.Add(position);
        }

        phase = Phase.Assign;
        iteration = 0;
        converged = false;
        autoPlay = false;
        timer = 0f;

        UpdateStatus();
    }

    public override void StepOnce()
    {
        if (converged || centroids.Count == 0)
            return;

        if (phase == Phase.Assign)
        {
            int changed = AssignPoints();

            // Nobody changed groups, so the algorithm has converged.
            if (changed == 0)
            {
                converged = true;
                autoPlay = false;
                timer = 0f;
                LabController.Instance.Celebrate();
            }
            else
            {
                phase = Phase.Update;
            }
        }
        else
        {
            MoveCentroids();
            phase = Phase.Assign;
            iteration++;
        }

        UpdateStatus();
    }

    public override void TogglePlay()
    {
        if (converged)
        {
            Begin();
            autoPlay = true;
            timer = 0f;
            return;
        }

        autoPlay = !autoPlay;
        timer = 0f;
    }

    public override void OnDataChanged()
    {
        converged = false;
        phase = Phase.Assign;
        UpdateStatus();
    }

    public override ParamSetup GetParamSetup()
    {
        ParamSetup setup = new ParamSetup
        {
            label = "Clusters (k)",
            min = 2,
            max = 5,
            value = k,
            whole = true
        };

        return setup;
    }

    public override void SetParameter(float value)
    {
        int newK = Mathf.RoundToInt(value);

        if (newK == k)
            return;

        k = newK;
        Begin();
    }

    // ASSIGN: each point joins its nearest centroid.
    // Returns the number of points whose group changed.
    private int AssignPoints()
    {
        int changed = 0;

        foreach (DataPoint point in LabController.Instance.points)
        {
            int best = 0;
            float bestDistance = float.MaxValue;

            for (int c = 0; c < targets.Count; c++)
            {
                float distance =
                    (point.transform.localPosition - targets[c]).sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = c;
                }
            }

            if (point.cluster != best)
            {
                changed++;
                point.cluster = best;
                point.SetColor(Palette.Get(best));
                point.Pulse();
            }
        }

        return changed;
    }

    // UPDATE: move each centroid to the average position of its points.
    private void MoveCentroids()
    {
        for (int c = 0; c < targets.Count; c++)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (DataPoint point in LabController.Instance.points)
            {
                if (point.cluster == c)
                {
                    sum += point.transform.localPosition;
                    count++;
                }
            }

            if (count > 0)
                targets[c] = sum / count;
        }
    }

    // Total squared distance from every point to its assigned centroid.
    private float Inertia()
    {
        float total = 0f;

        foreach (DataPoint point in LabController.Instance.points)
        {
            if (point.cluster >= 0 && point.cluster < targets.Count)
            {
                total +=
                    (point.transform.localPosition - targets[point.cluster]).sqrMagnitude;
            }
        }

        return total;
    }

    private void UpdateStatus()
    {
        string next;

        if (converged)
        {
            next = "<color=#73FF40>Converged! Nothing moves any more.</color>";
        }
        else if (phase == Phase.Assign)
        {
            next = "Next: ASSIGN each point to its nearest centroid";
        }
        else
        {
            next = "Next: UPDATE (move each centroid to its group's average)";
        }

        Status =
            "Iteration " + iteration +
            "   Total error " + (Inertia() * 1000f).ToString("0.0") +
            "\n" + next;
    }

    private void Update()
    {
        // Play mode: perform one algorithm step every stepInterval seconds.
        if (autoPlay)
        {
            timer += Time.deltaTime;

            if (timer >= Mathf.Max(0.01f, stepInterval))
            {
                timer = 0f;
                StepOnce();
            }
        }

        // Glide centroids smoothly to their targets.
        for (int c = 0; c < centroids.Count; c++)
        {
            if (centroids[c] != null)
            {
                centroids[c].localPosition = Vector3.Lerp(
                    centroids[c].localPosition,
                    targets[c],
                    5f * Time.deltaTime
                );
            }
        }

        // Draw a line from each point to its assigned centroid.
        List<DataPoint> points = LabController.Instance.points;

        for (int i = 0; i < points.Count && i < links.Count; i++)
        {
            int cluster = points[i].cluster;

            if (cluster < 0 || cluster >= centroids.Count)
            {
                links[i].enabled = false;
                continue;
            }

            links[i].enabled = true;
            links[i].SetPosition(0, points[i].transform.localPosition);
            links[i].SetPosition(1, centroids[cluster].localPosition);

            Color color = Palette.Get(cluster);
            color.a = 0.4f;
            LineUtil.SetColor(links[i], color);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

public class LabController : MonoBehaviour
{
    public static LabController Instance;

    [Header("Data")]
    public GameObject pointPrefab;
    public Transform pointsParent;
    public float boxSize = 0.3f; // The data box is 30 cm wide.

    [Header("Algorithms: 0 = K-Means, 1 = Regression, 2 = KNN")]
    public AlgorithmVisualizer[] algorithms;

    [Header("Effects (optional)")]
    public ParticleSystem celebrateEffect;
    public AudioSource audioSource;
    public AudioClip newDataSound;
    public AudioClip doneSound;

    [HideInInspector]
    public List<DataPoint> points = new List<DataPoint>();

    public int modeIndex { get; private set; } = -1;

    // The active algorithm, or null if none is available.
    public AlgorithmVisualizer Current
    {
        get
        {
            if (algorithms == null ||
                modeIndex < 0 ||
                modeIndex >= algorithms.Length)
            {
                return null;
            }

            return algorithms[modeIndex];
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (algorithms != null && algorithms.Length > 0)
            SetMode(0);
        else
            NewData();
    }

    public void SetMode(int index)
    {
        if (algorithms == null ||
            index < 0 ||
            index >= algorithms.Length ||
            algorithms[index] == null)
        {
            return;
        }

        modeIndex = index;

        for (int i = 0; i < algorithms.Length; i++)
        {
            if (algorithms[i] != null)
                algorithms[i].gameObject.SetActive(i == index);
        }

        NewData();
    }

    public void NewData()
    {
        ClearPoints();

        AlgorithmVisualizer algo = Current;
        if (algo != null)
        {
            algo.MakeData(this);
            algo.Begin();
        }
        else
        {
            MakeBlobs(60, 3, true);
        }

        PlaySound(newDataSound);
    }

    public void ClearPoints()
    {
        foreach (DataPoint point in points)
        {
            if (point != null)
                Destroy(point.gameObject);
        }

        points.Clear();
    }

    public DataPoint SpawnPoint(
        Vector3 localPosition,
        Color colour,
        int trueLabel = -1)
    {
        GameObject go = Instantiate(pointPrefab, pointsParent);
        go.transform.localPosition = localPosition;

        DataPoint dataPoint = go.GetComponent<DataPoint>();
        dataPoint.trueLabel = trueLabel;
        dataPoint.SetColor(colour, true);
        dataPoint.Pulse();

        points.Add(dataPoint);
        return dataPoint;
    }

    // Creates fuzzy groups of points. If coloured is true, each point
    // uses its group's color.
    public void MakeBlobs(int count, int groups, bool coloured)
    {
        if (groups <= 0)
            return;

        Vector3[] centres = new Vector3[groups];

        for (int group = 0; group < groups; group++)
        {
            float angle =
                (group * 360f / groups + Random.Range(-20f, 20f))
                * Mathf.Deg2Rad;

            centres[group] = new Vector3(
                Mathf.Cos(angle) * 0.085f,
                Random.Range(0.08f, 0.22f),
                Mathf.Sin(angle) * 0.085f);
        }

        for (int i = 0; i < count; i++)
        {
            int group = i % groups;
            Vector3 position = ClampToBox(
                centres[group] + Random.insideUnitSphere * 0.05f);

            SpawnPoint(
                position,
                coloured ? Palette.Get(group) : Palette.Unlabelled,
                coloured ? group : -1);
        }
    }

    // Keeps a position inside the data box.
    public Vector3 ClampToBox(Vector3 position)
    {
        float halfSize = boxSize / 2f;

        return new Vector3(
            Mathf.Clamp(position.x, -halfSize, halfSize),
            Mathf.Clamp(position.y, 0.01f, boxSize),
            Mathf.Clamp(position.z, -halfSize, halfSize));
    }

    public void OnDataChanged()
    {
        AlgorithmVisualizer algo = Current;
        if (algo != null)
            algo.OnDataChanged();
    }

    public void Celebrate()
    {
        if (celebrateEffect != null)
            celebrateEffect.Play();

        PlaySound(doneSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;

public class MLUIManager : MonoBehaviour
{
    [Header("AR")]
    public ARPlaneManager planeManager; // Assign the XR Origin here.
    public PlaceOnPlane placeOnPlane;   // Assign the XR Origin here.

    [Header("Texts")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playButtonText;
    public TextMeshProUGUI sliderLabel;

    [Header("Controls")]
    public GameObject controlsPanel;
    public GameObject sideButtons;
    public Slider paramSlider;
    public Button stepButton;
    public Button playButton;
    public Button[] modeButtons; // K-Means, Regression, KNN, in this order.

    [Header("Info card")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoBody;

    private bool planesVisible = true;
    private int shownMode = -2;
    private ParamSetup setup;

    private void Start()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void Update()
    {
        LabController lab = LabController.Instance;
        bool ready = lab != null;

        if (controlsPanel != null)
            controlsPanel.SetActive(ready);
        if (sideButtons != null)
            sideButtons.SetActive(ready);

        if (!ready)
        {
            shownMode = -2;
            if (statusText != null)
                statusText.text = string.Empty;

            if (hintText != null && planeManager != null)
            {
                hintText.text = planeManager.trackables.count == 0
                    ? "Move your phone slowly to scan the floor or a table..."
                    : "Surface found! Tap it to place the ML Lab.";
            }
            return;
        }

        // Refresh the slider, buttons, and info card when the selected mode changes.
        if (lab.modeIndex != shownMode)
        {
            shownMode = lab.modeIndex;
            RefreshForMode(lab);
        }

        AlgorithmVisualizer algo = lab.Current;
        if (hintText != null)
        {
            hintText.text = placeOnPlane != null && placeOnPlane.IsMoving
                ? "Tap a surface to move the lab there."
                : algo != null ? algo.Hint : "Your data points are ready!";
        }

        if (statusText != null)
            statusText.text = algo != null ? algo.Status : string.Empty;
        if (playButtonText != null)
            playButtonText.text = algo != null && algo.IsPlaying ? "Pause" : "Play";
    }

    private void RefreshForMode(LabController lab)
    {
        if (modeButtons != null)
        {
            for (int i = 0; i < modeButtons.Length; i++)
            {
                if (modeButtons[i] != null)
                    modeButtons[i].transform.localScale =
                        Vector3.one * (i == lab.modeIndex ? 1.1f : 0.92f);
            }
        }

        AlgorithmVisualizer algo = lab.Current;
        if (algo == null)
            return;

        setup = algo.GetParamSetup();
        if (paramSlider != null)
        {
            paramSlider.wholeNumbers = setup.whole;
            paramSlider.minValue = setup.min;
            paramSlider.maxValue = setup.max;
            paramSlider.SetValueWithoutNotify(setup.value);
        }

        UpdateSliderLabel(setup.value);

        if (stepButton != null)
            stepButton.interactable = algo.HasSteps;
        if (playButton != null)
            playButton.interactable = algo.HasSteps;

        ShowInfo(algo);
    }

    private AlgorithmVisualizer Algo()
    {
        LabController lab = LabController.Instance;
        return lab != null ? lab.Current : null;
    }

    private void UpdateSliderLabel(float value)
    {
        if (sliderLabel == null || setup == null)
            return;

        string formattedValue = setup.whole
            ? value.ToString("0")
            : value.ToString("0.00");

        sliderLabel.text = setup.label + ": " + formattedValue;
    }

    private void ShowInfo(AlgorithmVisualizer algo)
    {
        if (algo == null)
            return;

        if (infoTitle != null)
            infoTitle.text = algo.Title;
        if (infoBody != null)
            infoBody.text = algo.Explanation;
        if (infoPanel != null)
            infoPanel.SetActive(true);
    }

    // Called by the UI buttons.
    public void SetMode(int index)
    {
        if (LabController.Instance != null)
            LabController.Instance.SetMode(index);
    }

    public void OnStep()
    {
        AlgorithmVisualizer algo = Algo();
        if (algo != null)
            algo.StepOnce();
    }

    public void OnPlay()
    {
        AlgorithmVisualizer algo = Algo();
        if (algo != null)
            algo.TogglePlay();
    }

    public void OnReset()
    {
        AlgorithmVisualizer algo = Algo();
        if (algo != null)
            algo.Begin();
    }

    public void OnNewData()
    {
        if (LabController.Instance != null)
            LabController.Instance.NewData();
    }

    public void OnSlider(float value)
    {
        AlgorithmVisualizer algo = Algo();
        if (algo != null)
            algo.SetParameter(value);

        UpdateSliderLabel(value);
    }

    public void OnMove()
    {
        if (placeOnPlane != null)
            placeOnPlane.StartMove();
    }

    public void OnInfo()
    {
        ShowInfo(Algo());
    }

    public void CloseInfo()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    // Show or hide the surfaces detected by AR Foundation.
    public void TogglePlanes()
    {
        if (planeManager == null)
            return;

        planesVisible = !planesVisible;
        planeManager.enabled = planesVisible;

        foreach (ARPlane plane in planeManager.trackables)
        {
            if (plane != null)
                plane.gameObject.SetActive(planesVisible);
        }
    }
}
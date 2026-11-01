using UnityEngine;

// How a slider should look for an algorithm (k, learning rate ...)
public class ParamSetup
{
    public string label;
    public float min, max, value;
    public bool whole;     // true = whole numbers only
}

public abstract class AlgorithmVisualizer : MonoBehaviour
{
    public abstract string Title { get; }
    public abstract string Explanation { get; }
    public abstract string Hint { get; }

    public string Status { get; protected set; } = "";
    public virtual bool IsPlaying => false;
    public virtual bool HasSteps => true;

    public abstract void MakeData(LabController lab);   // create the data this algorithm needs
    public abstract void Begin();                       // (re)start on the current data
    public virtual void StepOnce() { }
    public virtual void TogglePlay() { }
    public virtual void OnDataChanged() { }             // a point was dragged
    public abstract ParamSetup GetParamSetup();
    public abstract void SetParameter(float value);
}
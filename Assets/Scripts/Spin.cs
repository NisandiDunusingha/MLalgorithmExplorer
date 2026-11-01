using UnityEngine;

public class Spin : MonoBehaviour
{
    // Degrees per second around each axis.
    public Vector3 degreesPerSecond = new Vector3(0f, 45f, 0f);

    private void Update()
    {
        transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
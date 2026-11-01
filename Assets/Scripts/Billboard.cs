using UnityEngine;

public class Billboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - mainCamera.transform.position);
    }
}
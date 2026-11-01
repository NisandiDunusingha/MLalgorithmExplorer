using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceOnPlane : MonoBehaviour
{
    [Header("Placement")]
    public ARRaycastManager raycastManager;
    public GameObject objectToPlace;

    [Header("Sound (optional)")]
    public AudioSource audioSource;
    public AudioClip placeSound;

    private GameObject placedObject;
    private bool moveNext;
    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    public bool HasPlacedObject => placedObject != null;
    public bool IsMoving => moveNext;

    private void Update()
    {
        // After placement, ignore taps unless Move mode is active.
        if (HasPlacedObject && !moveNext)
            return;

        if (!TryGetTap(out Vector2 tapPosition) || IsTapOnUI())
            return;

        if (raycastManager == null || objectToPlace == null)
            return;

        if (raycastManager.Raycast(
                tapPosition,
                hits,
                TrackableType.PlaneWithinPolygon))
        {
            Vector3 position = hits[0].pose.position;

            if (placedObject == null)
            {
                placedObject = Instantiate(
                    objectToPlace,
                    position,
                    Quaternion.identity);
            }
            else
            {
                placedObject.transform.position = position;
                moveNext = false;
            }

            PlaySound(placeSound);
        }
    }

    // Connect this method to the Move button's OnClick event.
    public void StartMove()
    {
        if (HasPlacedObject)
            moveNext = true;
    }

    private bool TryGetTap(out Vector2 position)
    {
        position = Vector2.zero;

        if (Input.touchCount == 1 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        // Mouse click support for XR Simulation in the Editor.
        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }

        return false;
    }

    private bool IsTapOnUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(
                Input.GetTouch(0).fingerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
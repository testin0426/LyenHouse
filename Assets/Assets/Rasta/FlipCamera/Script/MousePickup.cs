using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal replacement for VRChat's VRC_Pickup component, driven by the mouse instead of a VR
/// controller. Attach it to any GameObject that has a Collider.
///
/// - Left-click the object to grab it (hold the button to carry it in front of the camera).
/// - Release the left mouse button to drop it.
/// - Right-click while holding to trigger "Use" (OnPickupUseDown/OnPickupUseUp).
///
/// Pickup events are forwarded to an <see cref="EventDispatcher"/> on the same GameObject, which
/// mirrors the original VRChat pickup -> Udon event flow.
/// </summary>
public class MousePickup : MonoBehaviour
{
    [Tooltip("Distance (meters) the object floats in front of the camera while held. 0 or less captures the distance automatically at grab time.")]
    [SerializeField] private float holdDistance = 2f;

    [Tooltip("How quickly the held object follows the mouse cursor.")]
    [SerializeField] private float dragSpeed = 20f;

    private EventDispatcher dispatcher;
    private Camera cam;
    private bool isHeld = false;
    private float currentHoldDistance;

    public bool IsHeld => isHeld;

    private void Awake()
    {
        dispatcher = GetComponent<EventDispatcher>();
    }

    private void Update()
    {
        if (cam == null)
            cam = LocalPlayerProxy.Camera;
        if (cam == null)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (!isHeld)
        {
            if (mouse.leftButton.wasPressedThisFrame && RaycastThisObject(out RaycastHit hit))
            {
                isHeld = true;
                currentHoldDistance = holdDistance > 0f
                    ? holdDistance
                    : Mathf.Max(0.5f, Vector3.Distance(cam.transform.position, transform.position));
                dispatcher?.OnPickup();
            }
        }
        else
        {
            Drag();

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                Drop();
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                dispatcher?.OnPickupUseDown();
            }
            else if (mouse.rightButton.wasReleasedThisFrame)
            {
                dispatcher?.OnPickupUseUp();
            }
        }
    }

    private bool RaycastThisObject(out RaycastHit hit)
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out hit, 100f))
        {
            Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
            return hitTransform != null && (hitTransform == transform || hitTransform.IsChildOf(transform));
        }
        return false;
    }

    private void Drag()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 target = ray.GetPoint(currentHoldDistance);
        transform.position = Vector3.Lerp(transform.position, target, dragSpeed * Time.deltaTime);
    }

    /// <summary>Drops the object if it is currently held.</summary>
    public void Drop()
    {
        if (!isHeld)
            return;
        isHeld = false;
        dispatcher?.OnDrop();
    }
}

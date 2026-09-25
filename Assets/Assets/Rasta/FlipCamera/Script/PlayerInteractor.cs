using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// First-person interaction controller. Attach to the player camera (e.g. the MFPP camera).
///
/// - Raycasts from the camera's center (the crosshair) every frame.
/// - Highlights the hovered IInteractable via its HoverOutline component.
/// - Left mouse button  = Interact()  (e.g. take a photo).
/// - Right mouse button = Drop()      (e.g. put the object down).
///
/// A minimal screen-space crosshair is created at runtime, so no UI prefab is required.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("Maximum interaction distance in meters.")]
    [SerializeField] private float interactDistance = 6f;

    [Tooltip("Layer mask for raycast targets. Empty = everything.")]
    [SerializeField] private LayerMask interactMask = ~0;

    private Camera cam;
    private IInteractable currentHover;
    private Image crosshairImage;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            LocalPlayerProxy.SetPlayerCamera(cam);
        BuildCrosshair();
    }

    private void Update()
    {
        if (cam == null)
            cam = LocalPlayerProxy.Camera;
        if (cam == null)
            return;

        // Raycast from the camera center (the crosshair), like a standard FPS.
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        IInteractable nextHover = null;
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
        {
            // The interactable can live on the hit object itself, a parent (e.g. the camera
            // body) or a child (e.g. the Photo "Controller" child of a photo prefab).
            nextHover = hit.collider.GetComponentInParent<IInteractable>();
            if (nextHover == null)
                nextHover = hit.collider.GetComponentInChildren<IInteractable>();
        }

        if (nextHover != currentHover)
        {
            if (currentHover != null)
                currentHover.OnHoverExit();
            currentHover = (nextHover != null && nextHover.CanInteract) ? nextHover : null;
            if (currentHover != null)
                currentHover.OnHoverEnter();
        }

        if (crosshairImage != null)
            crosshairImage.color = currentHover != null ? Color.green : Color.white;

        Mouse mouse = Mouse.current;
        if (mouse == null || currentHover == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
            currentHover.Interact();
        else if (mouse.rightButton.wasPressedThisFrame)
            currentHover.Drop();
    }

    private void BuildCrosshair()
    {
        var canvasGO = new GameObject("InteractionCrosshair");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("Dot");
        imgGO.transform.SetParent(canvasGO.transform, false);
        crosshairImage = imgGO.AddComponent<Image>();
        crosshairImage.color = Color.white;
        crosshairImage.raycastTarget = false;

        RectTransform rect = crosshairImage.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(4f, 4f);
    }
}

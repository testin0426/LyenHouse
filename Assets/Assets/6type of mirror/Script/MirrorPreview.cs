using UnityEngine;

/// <summary>
/// Clickable mirror that toggles its preview. Left-click toggles the mirror on/off.
/// Attach to the mirror object (or its clickable frame) and assign the mirror to toggle.
/// </summary>
public class MirrorPreview : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject mirrorObject;     // the mirror surface / preview to toggle
    [SerializeField] private GameObject previewDisplay;   // optional secondary display to sync

    private void Awake()
    {
        if (mirrorObject == null)
            mirrorObject = gameObject;
    }

    public bool CanInteract => true;

    public void OnHoverEnter()
    {
        GetComponent<HoverOutline>()?.SetHighlight(true);
    }

    public void OnHoverExit()
    {
        GetComponent<HoverOutline>()?.SetHighlight(false);
    }

    public void Interact()
    {
        if (mirrorObject != null)
        {
            bool show = !mirrorObject.activeSelf;
            mirrorObject.SetActive(show);
            if (previewDisplay != null)
                previewDisplay.SetActive(show);
        }
    }

    public void Drop()
    {
        // Right-click does nothing for a mirror.
    }
}

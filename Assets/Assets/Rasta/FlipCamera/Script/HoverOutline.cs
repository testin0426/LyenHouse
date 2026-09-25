using UnityEngine;

/// <summary>
/// Toggles a toon outline on a renderer to highlight a hovered object.
/// Works with SRUniversal (which has an _OUTLINE_ON keyword pass) or any shader that exposes
/// an "_OutlineColor" / "_OutlineWidth" property.
/// </summary>
public class HoverOutline : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.15f, 1f);

    private Color originalColor;
    private bool originalKeyword;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }

    public void SetHighlight(bool on)
    {
        if (targetRenderer == null)
            return;

        // "material" returns an instanced copy so we never modify the shared asset.
        Material mat = targetRenderer.material;
        if (mat == null || !mat.HasProperty("_OutlineColor"))
            return;

        if (on)
        {
            originalKeyword = mat.IsKeywordEnabled("_OUTLINE_ON");
            originalColor = mat.GetColor("_OutlineColor");
            mat.EnableKeyword("_OUTLINE_ON");
            mat.SetColor("_OutlineColor", highlightColor);
            if (mat.HasProperty("_OutlineWidth") && mat.GetFloat("_OutlineWidth") < 1f)
                mat.SetFloat("_OutlineWidth", 1f);
        }
        else
        {
            if (!originalKeyword)
                mat.DisableKeyword("_OUTLINE_ON");
            mat.SetColor("_OutlineColor", originalColor);
        }
    }
}

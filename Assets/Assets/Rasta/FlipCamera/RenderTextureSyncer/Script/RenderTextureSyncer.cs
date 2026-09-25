using UnityEngine;

namespace Rasta.RenderTextureSyncer
{
    /// <summary>
    /// Local-only replacement for the VRChat networked RenderTextureSyncer.
    ///
    /// The FlipCamera already renders the live view directly into the photo's RenderTexture, and
    /// switching the camera target away freezes that frame on the photo. Encode() snapshots the
    /// render texture back into itself so the printed photo is a static image that is fully
    /// decoupled from the live camera.
    /// </summary>
    public class RenderTextureSyncer : MonoBehaviour
    {
        public RenderTexture TargetRenderTexture;

        [Tooltip("Kept for reference compatibility; unused in the local-only port.")]
        public MonoBehaviour HandleTarget;

        public void Encode()
        {
            if (TargetRenderTexture == null)
                return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = TargetRenderTexture;

            Texture2D snapshot = new Texture2D(
                TargetRenderTexture.width,
                TargetRenderTexture.height,
                TextureFormat.RGBA32,
                false);

            snapshot.ReadPixels(new Rect(0, 0, TargetRenderTexture.width, TargetRenderTexture.height), 0, 0, false);
            snapshot.Apply();

            RenderTexture.active = previous;

            Graphics.Blit(snapshot, TargetRenderTexture);
            Destroy(snapshot);
        }

        public void DeleteEncodeData()
        {
            // Nothing to delete locally.
        }
    }
}

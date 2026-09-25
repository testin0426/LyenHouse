using UnityEngine;

namespace Rasta.RenderTextureSyncer
{
    /// <summary>Kept for prefab compatibility; no longer used in the local-only port.</summary>
    public class Initializer : MonoBehaviour
    {
        private void Start()
        {
            // The TempTexture/decoder setup is not needed without network sync.
        }
    }
}

using UnityEngine;

namespace Rasta.RenderTextureSyncer
{
    /// <summary>Kept for prefab compatibility; no longer used in the local-only port.</summary>
    public class StaticValues : MonoBehaviour
    {
        [HideInInspector] public bool encoderBusy = false;
        [HideInInspector] public bool decoderBusy = false;
        [HideInInspector] public bool syncBusy = false;
    }
}

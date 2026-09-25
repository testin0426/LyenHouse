using UnityEngine;

/// <summary>
/// Stand-in for VRCPlayerApi / Networking.LocalPlayer in a blank (single-player) Unity project.
///
/// Works with any first-person setup: it finds the player camera via the "MainCamera" tag first,
/// then falls back to any enabled camera in the scene (e.g. an FPS controller whose camera is not
/// tagged MainCamera). If your FPS controller has a dedicated player root, assign
/// <see cref="PlayerRoot"/> once (or tag it "Player") so interaction ranges are measured from the
/// player body instead of the camera.
/// </summary>
public static class LocalPlayerProxy
{
    private static Camera cachedCamera;
    private static Camera playerCamera;   // the actual FPS camera, registered by PlayerInteractor
    private static Transform playerRoot;

    /// <summary>
    /// Register the first-person camera. PlayerInteractor calls this from its own Awake so that
    /// interaction (and anything that follows the player's view) always uses the correct camera,
    /// even when the scene has multiple cameras or none is tagged "MainCamera".
    /// </summary>
    public static void SetPlayerCamera(Camera cam)
    {
        playerCamera = cam;
    }

    /// <summary>Optional player-root transform (e.g. the first-person controller).</summary>
    public static Transform PlayerRoot
    {
        get
        {
            if (playerRoot == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerRoot = player.transform;
            }
            return playerRoot;
        }
        set => playerRoot = value;
    }

    /// <summary>The player's camera (the one registered by PlayerInteractor, else main camera).</summary>
    public static Camera Camera
    {
        get
        {
            if (playerCamera != null && playerCamera.isActiveAndEnabled)
                return playerCamera;
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
                cachedCamera = FindCamera();
            return cachedCamera;
        }
    }

    /// <summary>The head transform (the player camera).</summary>
    public static Transform Head => Camera != null ? Camera.transform : null;

    /// <summary>World position of the player (player root if available, otherwise the head).</summary>
    public static Vector3 Position =>
        PlayerRoot != null ? PlayerRoot.position : (Head != null ? Head.position : Vector3.zero);

    public static bool IsValid => Camera != null;

    private static Camera FindCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        foreach (Camera cam in Camera.allCameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
                return cam;
        }
        return null;
    }
}

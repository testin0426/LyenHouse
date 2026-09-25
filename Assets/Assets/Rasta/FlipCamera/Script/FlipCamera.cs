using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rasta.FlipCamera
{
    using RenderTextureSyncer = Rasta.RenderTextureSyncer.RenderTextureSyncer;

    public class FlipCamera : MonoBehaviour, IPickupTarget, IInteractable
    {
        [SerializeField] private bool enableRenderTextureSyncer = true;
        [SerializeField] private bool enableFlip = true;
        [Space(20)]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RawImage cameraDisplay;
        [SerializeField] private GameObject photosParent;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Text countText;

        [Header("Hold (follows the player's view)")]
        [SerializeField] private Vector3 holdOffset = new Vector3(0.35f, -0.22f, 0.55f);
        [SerializeField] private Vector3 holdRotationEuler = Vector3.zero;

        private GameObject[] photos;
        private Photo[] photoControllers;
        private RenderTexture[] renderTextures;
        private RenderTextureSyncer[] renderTextureSyncers;

        private DateTime sleepTime = DateTime.MinValue;
        private bool isPickup = false;
        private int remainingPhotoCount = -1;

        // Snapshotted at pickup so the camera can be returned to its world spot on drop.
        private Transform holdParent;
        private Vector3 restorePosition;
        private Quaternion restoreRotation;
        private bool hasRestore = false;

        // Backing fields (formerly UdonSynced + FieldChangeCallback).
        private int _streamingRTIndex = int.MinValue;
        private bool _isFlip = false;
        private bool _isSleep = true;
        private bool _isPrintout = false;

        private void SetRemainingPhotoCount(int count)
        {
            remainingPhotoCount = count;
            if (countText != null)
                countText.text = count + "/" + photos.Length;
        }

        private void Start()
        {
            photos = new GameObject[photosParent.transform.childCount];
            photoControllers = new Photo[photos.Length];
            renderTextures = new RenderTexture[photos.Length + 1];
            renderTextureSyncers = new RenderTextureSyncer[photos.Length];

            for (int i = 0; i < photos.Length; i++)
            {
                photos[i] = photosParent.transform.GetChild(i).gameObject;

                Transform controller = photos[i].transform.Find("Controller");
                if (controller != null)
                    photoControllers[i] = controller.GetComponent<Photo>();

                renderTextures[i] = new RenderTexture(1600, 1200, 16, RenderTextureFormat.ARGB32);

                Transform photo = photos[i].transform.Find("Photo");
                if (photo != null)
                {
                    Renderer renderer = photo.GetComponent<Renderer>();
                    if (renderer != null && renderer.materials.Length > 0)
                        renderer.materials[0].mainTexture = renderTextures[i];
                }

                if (controller != null)
                {
                    Transform syncerTransform = controller.Find("RenderTextureSyncer");
                    renderTextureSyncers[i] = syncerTransform != null
                        ? syncerTransform.GetComponent<RenderTextureSyncer>()
                        : null;
                    if (renderTextureSyncers[i] != null)
                    {
                        renderTextureSyncers[i].TargetRenderTexture = renderTextures[i];
                        renderTextureSyncers[i].HandleTarget = photoControllers[i];
                    }
                }
            }

            renderTextures[renderTextures.Length - 1] = new RenderTexture(1600, 1200, 16, RenderTextureFormat.ARGB32);
            targetCamera.targetTexture = renderTextures[renderTextures.Length - 1];
            cameraDisplay.texture = renderTextures[renderTextures.Length - 1];

            StreamingRTIndex = 0;

            if (remainingPhotoCount == -1)
                SetRemainingPhotoCount(photos.Length);
        }

        private void Update()
        {
            if (!isPickup && !IsSleep && DateTime.Now > sleepTime)
            {
                IsSleep = true;
            }
        }

        public void HandlePickup()
        {
            isPickup = true;
            IsSleep = false;

            AttachToView();

            // Raising the camera to eye level flips the lens cover open.
            if (enableFlip)
                IsFlip = true;

            // First pickup: start streaming into the selected photo.
            if (StreamingRTIndex >= 0 && photoControllers[StreamingRTIndex] != null)
                photoControllers[StreamingRTIndex].PhotoState = 1;
        }

        public void HandlePickupUseDown()
        {
            // Can only print when no photo is currently printing and a photo is streaming.
            if (FindPhotoStateIndex(2) == -1 && StreamingRTIndex >= 0 && photoControllers[StreamingRTIndex] != null)
            {
                ShotSound();

                photoControllers[StreamingRTIndex].PhotoState = 2;
                IsPrintout = true;

                if (enableRenderTextureSyncer && renderTextureSyncers[StreamingRTIndex] != null)
                    renderTextureSyncers[StreamingRTIndex].Encode();

                // Point the camera at the next available photo.
                StreamingRTIndex = FindPhotoStateIndex(0);

                if (StreamingRTIndex >= 0 && photoControllers[StreamingRTIndex] != null)
                    photoControllers[StreamingRTIndex].PhotoState = 1;
            }
        }

        public void HandlePickupUseUp()
        {
        }

        public void HandleDrop()
        {
            isPickup = false;

            DetachFromView();

            // Dropping the camera flips the lens cover back down.
            if (enableFlip)
                IsFlip = false;

            SetSleepTime();
        }

        public void SetSleepTime()
        {
            sleepTime = DateTime.Now.AddSeconds(10);
        }

        private void AttachToView()
        {
            Transform head = LocalPlayerProxy.Head;
            if (head == null)
                return;

            holdParent = transform.parent;
            restorePosition = transform.position;
            restoreRotation = transform.rotation;
            hasRestore = true;

            transform.SetParent(head, false);
            transform.localPosition = holdOffset;
            transform.localRotation = Quaternion.Euler(holdRotationEuler);
        }

        private void DetachFromView()
        {
            if (!hasRestore)
                return;

            transform.SetParent(holdParent, true);
            transform.position = restorePosition;
            transform.rotation = restoreRotation;
            hasRestore = false;
        }

        public int StreamingRTIndex
        {
            get => _streamingRTIndex;
            set
            {
                if (_streamingRTIndex == value)
                    return;
                _streamingRTIndex = value;

                if (_streamingRTIndex >= 0)
                {
                    targetCamera.targetTexture = renderTextures[_streamingRTIndex];
                    cameraDisplay.texture = renderTextures[_streamingRTIndex];
                }
                else if (_streamingRTIndex == -1)
                {
                    // No photos left: show the spare render texture.
                    targetCamera.targetTexture = renderTextures[renderTextures.Length - 1];
                    cameraDisplay.texture = renderTextures[renderTextures.Length - 1];
                }
            }
        }

        public bool IsFlip
        {
            get => _isFlip;
            set
            {
                _isFlip = value;
                if (animator != null)
                    animator.SetBool("flip", value);
            }
        }

        public bool IsSleep
        {
            get => _isSleep;
            set
            {
                _isSleep = value;
                if (targetCamera != null)
                    targetCamera.enabled = !_isSleep;
                if (animator != null)
                    animator.SetBool("sleep", _isSleep);
                if (_isSleep && enableFlip)
                    IsFlip = false;
            }
        }

        public bool IsPrintout
        {
            get => _isPrintout;
            set
            {
                _isPrintout = value;
                if (animator != null)
                    animator.Play(_isPrintout ? "Printout" : "ResetPrintout");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            int index = Array.IndexOf(photos, other.gameObject);
            if (index == -1)
                return;

            // Return the photo to the camera when the holder drops it back inside.
            Photo controller = photoControllers[index];
            if (controller != null && controller.IsPickup)
            {
                controller.PhotoState = 0;

                MousePickup pickup = photos[index].GetComponent<MousePickup>();
                if (pickup != null)
                    pickup.Drop();

                if (enableRenderTextureSyncer && renderTextureSyncers[index] != null)
                    renderTextureSyncers[index].DeleteEncodeData();
            }
        }

        public void UpdateRemainingPhotoCount()
        {
            SetRemainingPhotoCount(CountPhotoState(0) + CountPhotoState(1));

            if (StreamingRTIndex == -1 && remainingPhotoCount > 0 && CountPhotoState(1) == 0)
            {
                StreamingRTIndex = FindPhotoStateIndex(0);
                if (StreamingRTIndex >= 0 && photoControllers[StreamingRTIndex] != null)
                    photoControllers[StreamingRTIndex].PhotoState = 1;
            }
        }

        private int CountPhotoState(int targetState)
        {
            int count = 0;
            for (int i = 0; i < photoControllers.Length; i++)
            {
                if (photoControllers[i] != null && photoControllers[i].PhotoState == targetState)
                    count++;
            }
            return count;
        }

        private int FindPhotoStateIndex(int targetState)
        {
            for (int i = 0; i < photoControllers.Length; i++)
            {
                if (photoControllers[i] != null && photoControllers[i].PhotoState == targetState)
                    return i;
            }
            return -1;
        }

        public void ShotSound()
        {
            if (audioSource != null && audioSource.clip != null)
                audioSource.PlayOneShot(audioSource.clip);
        }

        public void SetIsPrintoutFalse()
        {
            IsPrintout = false;
        }

        // --- IInteractable (raycast interaction) ---
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
            // First left click: pick the camera up and raise it to eye level.
            // Subsequent left clicks: take a photo.
            if (!isPickup)
                HandlePickup();
            else
                HandlePickupUseDown();
        }

        public void Drop()
        {
            // Right click: put the camera down.
            HandleDrop();
        }
    }
}

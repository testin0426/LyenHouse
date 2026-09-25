using UnityEngine;
using UnityEngine.Animations;

namespace Rasta.FlipCamera
{
    public class Photo : MonoBehaviour, IPickupTarget, IInteractable
    {
        [SerializeField] private FlipCamera cameraController;
        [SerializeField] private GameObject followTarget;
        [SerializeField] private GameObject photoObj;
        [SerializeField] private Animator photoAnimator;

        private int _photoState = 0;

        private Collider photoCollider;
        private ParentConstraint parentConstraint;

        private bool isFollow = false;
        private bool isActive = false;

        [HideInInspector] public bool IsPickup = false;

        private void Start()
        {
            GameObject photoParent = transform.parent != null ? transform.parent.gameObject : gameObject;
            photoCollider = photoParent.GetComponent<Collider>();
            parentConstraint = photoParent.GetComponent<ParentConstraint>();

            if (photoObj != null)
                photoObj.SetActive(false);
            if (photoCollider != null)
                photoCollider.enabled = false;
        }

        public void HandlePickup()
        {
            IsPickup = true;

            if (PhotoState == 2 && cameraController != null)
                cameraController.SetIsPrintoutFalse();
            PhotoState = 3;
        }

        public void HandleDrop()
        {
            IsPickup = false;
        }

        public void HandlePickupUseDown() { }
        public void HandlePickupUseUp() { }

        // Download-status callbacks are kept for API compatibility; they are unused locally.
        public void HandleWaitStartDownload() { if (photoAnimator != null) photoAnimator.SetInteger("state", 1); }
        public void HandleStartDownload() { if (photoAnimator != null) photoAnimator.SetInteger("state", 2); }
        public void HandleFinishDownload() { if (photoAnimator != null) photoAnimator.SetInteger("state", 0); }
        public void HandleDownloadError() { if (photoAnimator != null) photoAnimator.SetInteger("state", 3); }

        public int PhotoState
        {
            get => _photoState;
            set
            {
                bool isChanged = _photoState != value;
                _photoState = value;

                switch (_photoState)
                {
                    case 0:
                        isFollow = false;
                        isActive = false;
                        break;
                    case 1:
                        isFollow = true;
                        isActive = false;
                        break;
                    case 2:
                        isFollow = true;
                        isActive = true;
                        break;
                    case 3:
                        isFollow = false;
                        isActive = true;
                        break;
                }

                if (parentConstraint != null)
                    parentConstraint.constraintActive = isFollow;
                if (photoObj != null)
                    photoObj.SetActive(isActive);
                if (photoCollider != null)
                    photoCollider.enabled = isActive;

                if (!isActive && photoAnimator != null)
                    photoAnimator.SetInteger("state", 0);

                if (isChanged && cameraController != null)
                    cameraController.UpdateRemainingPhotoCount();
            }
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
            // Left click: pick up the photo.
            HandlePickup();
        }

        public void Drop()
        {
            // Right click: release the photo.
            HandleDrop();
        }
    }
}

using System;
using System.Linq;
using UnityEngine;

namespace MFPP.Modules
{
    [HelpURL("https://ashkoredracson.github.io/MFPP/#ladder-module")]
    public class LadderModule : PlayerModule
    {
        /// <summary>
        /// The layer mask of the ladder(s).
        /// </summary>
        [Space]
        [Tooltip("The layer mask of the ladder(s).")]
        public LayerMask LadderLayerMask;
        /// <summary>
        /// The climb speed of the <see cref="Player"/> on ladders.
        /// </summary>
        [Tooltip("The climb speed of the player on ladders.")]
        public float ClimbSpeed = 3f;
        /// <summary>
        /// The step interval of the <see cref="Player"/> on ladders.
        /// </summary>
        [Tooltip("The step interval of the player on ladders.")]
        public float ClimbStepTimer = 1f;
        /// <summary>
        /// The grab force multiplier when grabbing a ladder.
        /// </summary>
        [Space]
        [Range(0f, 10f)]
        [Tooltip("The grab force multiplier when grabbing a ladder.")]
        public float GrabForceMultiplier = 5f;
        /// <summary>
        /// The grab duration in seconds (until we grab a ladder).
        /// </summary>
        [Tooltip("The grab duration in seconds (until we grab a ladder).")]
        public float GrabDuration = 0.5f;

        /// <summary>
        /// True if the player is using a ladder.
        /// </summary>
        public bool OnLadder { get; protected set; }
        /// <summary>
        /// True if the player is on top of a ladder.
        /// </summary>
        public bool OnTopOfLadder { get; protected set; }
        /// <summary>
        /// Returns the current ladder we are colliding and using.
        /// </summary>
        public Collider CurrentLadderCollider { get; protected set; }

        private bool oldOnLadder;
        private bool oldOnTopOfLadder;
        private Vector3 grabVelocity;
        private float grabTime = 0f;
        private Vector3 impulseMovement;

        public override void Initialize()
        {
            StepTimer = 0f;
            Player.BeforeFinalMovement += Player_BeforeFinalMovement;
        }

        public override void AfterUpdate()
        {
            DoLadderSteps();
        }

        private void Player_BeforeFinalMovement(Vector3 finalMovement)
        {
            if (!Enabled)
                return;

            ControllerColliderHit hit = Player.ColliderHits.FirstOrDefault(c => c.collider != null && LadderLayerMask.Contains(c.gameObject.layer));
            OnLadder = (Player.CharacterController.collisionFlags & CollisionFlags.Sides) == CollisionFlags.Sides && hit != null && Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) <= 0.01f;
            OnTopOfLadder =
                (Player.CharacterController.collisionFlags & CollisionFlags.Below) == CollisionFlags.Below &&
                hit != null;

            if (oldOnLadder && !OnLadder)
                Player.AddImpulse(impulseMovement * Player.CharacterController.radius * 4f);

            if (OnLadder)
            {
                CurrentLadderCollider = hit.collider;
                Vector3 movement = Vector3.zero;

                grabTime = 0f;

                if (!Player.IsGrounded)
                    movement = -hit.normal * (ClimbSpeed * 0.75f); // Stick to the ladder

                // Calculate the movement again
                Vector3 desiredNormalizedMovement = new Vector3(GetCurrentAxis("Horizontal"), 0, GetCurrentAxis("Vertical"));
                if (desiredNormalizedMovement.magnitude > 1f)
                    desiredNormalizedMovement.Normalize();

                Vector3 desiredNormalizedWorldMovement = transform.TransformVector(desiredNormalizedMovement);
                impulseMovement = desiredNormalizedWorldMovement;

                float forwardDot = Vector3.Dot(desiredNormalizedWorldMovement, -hit.normal);
                float mult = 1 - Mathf.Abs(forwardDot);

                // Lerp it to the Y axis so that instead of going forward on the ladder, we go upwards and so on.
                desiredNormalizedWorldMovement = new Vector3(desiredNormalizedWorldMovement.x * mult, Mathf.LerpUnclamped(0, desiredNormalizedWorldMovement.magnitude, forwardDot), desiredNormalizedWorldMovement.z * mult).normalized;

                // Apply movement and speed
                Vector3 desiredWorldMovement = desiredNormalizedWorldMovement * ClimbSpeed;
                movement += desiredWorldMovement;

                // If we are jumping, jump off the ladder
                if (GetButtonDown(Player.Controls.JumpButton))
                {
                    movement = hit.normal * Player.Movement.Jump.Power;
                }

                Player.FinalMovement = movement; // Apply final movement
            }
            else if (!OnTopOfLadder && oldOnTopOfLadder && !Player.IsJumping && !Player.IsGrounded)
            {
                RaycastHit hitSphere;
                if (Physics.SphereCast(transform.position + Player.CharacterController.center, Player.RadiusWithSkinWidth, Vector3.down, out hitSphere, Mathf.Infinity, LadderLayerMask))
                {
                    grabVelocity = -new Vector3(hitSphere.normal.x, 0, hitSphere.normal.z).normalized * Player.Velocity.magnitude * GrabForceMultiplier;
                    grabTime = GrabDuration;
                }
            }

            if (grabTime > 0)
            {
                Player.FinalMovement += grabVelocity * Time.deltaTime;
                grabTime -= Time.deltaTime;
            }

            oldOnLadder = OnLadder;
            oldOnTopOfLadder = OnTopOfLadder;
        }

        /// <summary>
        /// The footstep timer interval left before emitting a ladder step.
        /// </summary>
        protected float StepTimer { get; set; }
        private void DoLadderSteps()
        {
            if (!OnLadder) // If not on ladder, don't bother with the rest.
                return;

            if (StepTimer <= 0) // If step timer is due, emit step ladder sound.
            {
                EmitLadderSound();
                StepTimer += ClimbStepTimer;
            }
            else // Decrease based on velocity and the climb step timer, the current step timer.
            {
                StepTimer -= Player.Velocity.magnitude / ClimbStepTimer * Time.deltaTime;
            }
        }

        /// <summary>
        /// Emits a ladder step sound.
        /// </summary>
        public void EmitLadderSound()
        {
            if (Player.Footstep.GlobalVolume <= 0 || Player.Footstep.Asset == null) // If global volume or footstep asset is null, don't bother with the rest.
                return;

            FootstepAsset.Data data;

            // Get the corresponding footstep data
            if (CurrentLadderCollider == null)
            {
                data = Player.CurrentFootstepData;
            }
            else
            {
                FootstepOverride footstepOverride = CurrentLadderCollider.GetComponent<FootstepOverride>();
                MeshRenderer mr = CurrentLadderCollider.GetComponent<MeshRenderer>();

                if (footstepOverride != null)
                {
                    data = Player.Footstep.Asset.GetData(footstepOverride.FootstepOverrideName);
                }
                else
                {
                    data = (mr != null ? Player.Footstep.Asset.GetData(mr.sharedMaterial) ?? Player.Footstep.Asset.GetData(mr.sharedMaterial.mainTexture) ?? Player.CurrentFootstepData : Player.CurrentFootstepData);
                }
            }

            if (data == null)
                throw new Exception("Footstep data was not found, are you sure you have any footsteps in your FootstepAsset or that you provided an existing footstep data name for the default footsteps?");

            AudioClip sound = data.GetRandomFootstep(); // Get sound
            float volume = Player.Footstep.Asset.WalkVolume;

            if (volume <= 0)
                return;

            float pitch = data.GetRandomPitch(); // Get random specified pitch

            // Play sound
            if (!Player.Footstep.Is3D)
            {
                Audio.Play(sound, volume * Player.Footstep.GlobalVolume, pitch);
            }
            else
            {
                if (Player.Footstep.Range <= 0f)
                    Audio.Play3D(sound, transform, volume * Player.Footstep.GlobalVolume, pitch);
                else
                    Audio.Play3D(sound, transform, Player.Footstep.GlobalVolume * volume, pitch, Player.Footstep.Range);
            }
        }
    }
}
using UnityEngine;

namespace MFPP.Modules
{
    [HelpURL("https://ashkoredracson.github.io/MFPP/#pick-up-module")]
    public class PickUpModule : PlayerModule
    {
        [Space]
        [Tooltip("The maximum pickup distance (how far you can reach to grab an object).")]
        public float MaxPickupDistance = 2f;

        [Tooltip("The maximum pickup mass.")]
        public float MaxPickupMass = 5f;

        [Space]
        [Tooltip("Pick up button.")]
        public string PickUpButton = "Pick Up";

        [Space]
        [Header("Carry")]
        [Tooltip("How far in front of the camera the object is held, in meters.")]
        public float HoldDistance = 1.5f;

        [Tooltip("Extra local offset applied to the hold position, relative to the camera.")]
        public Vector3 HoldOffset = Vector3.zero;

        [Tooltip("How strongly the object is pulled toward the hold point. Higher = stiffer/snappier, lower = floatier.")]
        public float FollowSpeed = 12f;

        [Tooltip("How strongly the object is rotated toward its target orientation. Higher = faster.")]
        public float RotationSpeed = 12f;

        [Tooltip("If true, the object keeps its upright world orientation and only rotates with the player's yaw. If false, it matches the camera's full rotation (pitch/roll included).")]
        public bool RotateWithYaw = true;

        private Rigidbody target;
        private Quaternion originalRotation;
        private Quaternion pickupCameraRotation;
        private float pickupYaw;

        public override void AfterUpdate()
        {
            if (Input.GetButtonDown(PickUpButton)) // If pick up button was pressed
            {
                if (target != null) // If we already have a target, drop/throw it.
                {
                    target = null;
                }
                else
                {
                    Ray ray = new Ray(Camera.transform.position, Camera.transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, MaxPickupDistance)) // Otherwise, shoot a ray where we are aiming.
                    {
                        Rigidbody body = hit.collider.attachedRigidbody;
                        if (body != null && !body.isKinematic && body.mass <= MaxPickupMass) // Retrieve the rigidbody and make sure it is not kinematic.
                        {
                            target = body; // Set the target.
                            originalRotation = target.rotation;
                            pickupCameraRotation = Camera.transform.rotation;
                            pickupYaw = Player.transform.eulerAngles.y;
                        }
                    }
                }
            }

            if (target != null) // If target is not null, carry it.
                Carry();
        }

        private void Carry()
        {
            // Hold point: in front of the camera, plus a local offset so height/side can be tuned.
            Vector3 holdPosition = Camera.transform.position
                + Camera.transform.rotation * (Vector3.forward * HoldDistance + HoldOffset);

            // Linear: exponentially damp the velocity toward the hold point. Stable first-order
            // response with no oscillation and no steady-state drift.
            Vector3 desiredVelocity = (holdPosition - target.position) / Time.deltaTime;
            target.linearVelocity = Vector3.Lerp(target.linearVelocity, desiredVelocity,
                1f - Mathf.Exp(-FollowSpeed * Time.deltaTime));

            // Angular: build the target orientation.
            Quaternion desiredRotation;
            if (RotateWithYaw)
            {
                // Keep the object's original world orientation, rotated only by how much the
                // player has turned since pickup. Ignores the camera's fixed local Y offset.
                float yawDelta = Player.transform.eulerAngles.y - pickupYaw;
                desiredRotation = Quaternion.Euler(0f, yawDelta, 0f) * originalRotation;
            }
            else
            {
                // Rigidly match the camera, keeping the exact relative orientation from pickup.
                desiredRotation = Camera.transform.rotation
                    * Quaternion.Inverse(pickupCameraRotation) * originalRotation;
            }

            Quaternion delta = desiredRotation * Quaternion.Inverse(target.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f)
                angle -= 360f;

            Vector3 desiredAngularVelocity = Vector3.zero;
            if (Mathf.Abs(angle) > 0.1f)
                desiredAngularVelocity = axis.normalized * (angle * Mathf.Deg2Rad / Time.deltaTime);

            target.angularVelocity = Vector3.Lerp(target.angularVelocity, desiredAngularVelocity,
                1f - Mathf.Exp(-RotationSpeed * Time.deltaTime));
        }
    }
}
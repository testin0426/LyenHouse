using UnityEngine;
using UnityEngine.InputSystem;

namespace SunnyItems
{
    public class Fork : MonoBehaviour
    {
        private Trigger triggerB;
        private Camera cam;

        private void Start()
        {
            Transform triggerTransform = transform.Find("Trigger");
            if (triggerTransform != null)
                triggerB = triggerTransform.GetComponent<Trigger>();
        }

        private void Update()
        {
            if (cam == null)
                cam = LocalPlayerProxy.Camera;
            if (cam == null)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            // A local click acts as "pickup use": dip/eat from the fondue.
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
                    if (hitTransform != null && (hitTransform == transform || hitTransform.IsChildOf(transform)) && triggerB != null)
                        triggerB.Eat();
                }
            }
        }
    }
}

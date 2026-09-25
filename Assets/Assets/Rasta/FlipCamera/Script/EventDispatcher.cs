using UnityEngine;

/// <summary>
/// Implemented by scripts that react to pickup/use/drop events. Mirrors the VRChat Udon
/// pickup event names (HandlePickup, HandlePickupUseDown, HandlePickupUseUp, HandleDrop).
/// </summary>
public interface IPickupTarget
{
    void HandlePickup();
    void HandlePickupUseDown();
    void HandlePickupUseUp();
    void HandleDrop();
}

/// <summary>
/// Replacement for the VRChat UdonBehaviour pickup-event relay. A <see cref="MousePickup"/> on the
/// same GameObject calls these methods, which forward the enabled events to the configured target.
/// </summary>
public class EventDispatcher : MonoBehaviour
{
    [SerializeField] private MonoBehaviour target;

    [SerializeField] private bool pickupHandler = false;
    [SerializeField] private bool pickupUseDownHandler = false;
    [SerializeField] private bool pickupUseUpHandler = false;
    [SerializeField] private bool dropHandler = false;

    public void OnPickup()
    {
        if (pickupHandler && target is IPickupTarget pickup)
            pickup.HandlePickup();
    }

    public void OnPickupUseDown()
    {
        if (pickupUseDownHandler && target is IPickupTarget pickup)
            pickup.HandlePickupUseDown();
    }

    public void OnPickupUseUp()
    {
        if (pickupUseUpHandler && target is IPickupTarget pickup)
            pickup.HandlePickupUseUp();
    }

    public void OnDrop()
    {
        if (dropHandler && target is IPickupTarget pickup)
            pickup.HandleDrop();
    }
}

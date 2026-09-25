using UnityEngine;

/// <summary>
/// Implemented by objects that react to first-person raycast interaction.
/// Left mouse button = Interact(), right mouse button = Drop().
/// </summary>
public interface IInteractable
{
    bool CanInteract { get; }
    void OnHoverEnter();
    void OnHoverExit();
    void Interact();
    void Drop();
}

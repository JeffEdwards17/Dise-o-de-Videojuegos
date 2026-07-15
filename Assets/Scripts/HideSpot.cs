using UnityEngine;

public class HideSpot : MonoBehaviour, IInteractable
{
    public Transform hidePoint;

    public string Prompt
    {
        get { return "E - Esconderse / Salir"; }
    }

    private void Awake()
    {
        if (hidePoint == null)
            hidePoint = transform;
    }

    public void Interact(PlayerController player)
    {
        player.SetHidden(!player.IsHidden, hidePoint);
    }
}
using UnityEngine;

public class HideSpot : MonoBehaviour, IInteractable
{
    public Transform hidePoint;

    [Tooltip("Lugar donde aparece el jugador al salir del escondite. Si no se asigna, vuelve a la posición previa.")]
    public Transform exitPoint;

    public float hideCameraY = 0.7f;

    private PlayerController cachedPlayer;

    public string Prompt
    {
        get
        {
            if (cachedPlayer == null)
                cachedPlayer = FindObjectOfType<PlayerController>();

            return cachedPlayer != null && cachedPlayer.IsHidden
                ? "E - Salir del escondite"
                : "E - Ocultarse";
        }
    }

    private void Awake()
    {
        if (hidePoint == null)
            hidePoint = transform;
    }

    public void Interact(PlayerController player)
    {
        if (player.IsHidden)
            player.ExitHide(exitPoint);
        else
            player.SetHidden(true, hidePoint, hideCameraY);
    }
}

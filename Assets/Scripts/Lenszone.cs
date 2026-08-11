using UnityEngine;

public class LensZone : MonoBehaviour, IInteractable
{
    [Header("Identidad de esta zona")]
    public string zoneId = "zona_1";

    [Header("Imágenes del recuerdo (arrastra 3-4 sprites, en orden)")]
    public Sprite[] visionImages;

    [Header("Grupo de sonidos de esta visión (susurros, gritos, estática...)")]
    [Tooltip("Cualquier cantidad. Se reproducen en orden durante la visión.")]
    public AudioClip[] visionSoundGroup;

    [Header("Prompt que ve el jugador")]
    public string promptMessage = "Usar el Lente del Tiempo";

    private bool used = false;

    public string Prompt => used ? "" : promptMessage;

    public void Interact(PlayerController player)
    {
        if (used)
            return;

        used = true;

        if (LensManager.Instance != null)
            LensManager.Instance.RegisterVision(zoneId);

        // Marca la tarea correspondiente en la lista de objetivos (TaskListUI),
        // usando el mismo zoneId como id de la tarea.
        if (TaskListUI.Instance != null)
            TaskListUI.Instance.CompleteTask(zoneId);

        if (VisionSequenceUI.Instance != null)
            VisionSequenceUI.Instance.Play(visionImages, visionSoundGroup);
        else
            Debug.LogWarning("[LensZone] No se encontró un VisionSequenceUI en la escena.");
    }
}

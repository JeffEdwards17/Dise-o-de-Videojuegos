using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public string requiredItemId = "";
    public float openAngle = 90f;
    public float openSpeed = 4f;

    [Header("Bloqueo por Lente del Tiempo (opcional)")]
    [Tooltip("Si está activado, la puerta no abre hasta completar las 3 visiones.")]
    public bool requireAllVisions = false;
    public string lockedVisionsMessage = "Aún debo entender qué pasó aquí...";

    [Header("Fin de la demo (opcional)")]
    [Tooltip("Si está activado, después de abrir esta puerta se regresa al Menú Principal.")]
    public bool returnToMainMenuOnOpen = false;
    public string mainMenuSceneName = "MainMenu";
    public float delayBeforeMainMenu = 3f;

    [Header("Messages")]
    public string lockedMessage = "La puerta está cerrada.";
    public string openMessage = "";
    public string objectiveAfterOpen = "";
    public string taskIdOnOpen = "";

    [Header("Audio")]
    public AudioClip openClip;
    public AudioClip closeClip;
    public AudioClip lockedClip;

    private bool isOpen;
    private bool isMoving;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string Prompt
    {
        get
        {
            if (isOpen)
                return "E - Cerrar";

            return "E - Abrir";
        }
    }

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    public void Interact(PlayerController player)
    {
        if (isMoving)
            return;

        if (requireAllVisions && !VisionsCompleted())
        {
            PlaySound(lockedClip);

            if (GameMessageUI.Instance != null)
                GameMessageUI.Instance.ShowMessage(lockedVisionsMessage);
            else
                Debug.LogWarning(lockedVisionsMessage + " (faltan visiones del Lente del Tiempo)");

            return;
        }

        if (!string.IsNullOrEmpty(requiredItemId))
        {
            SimpleInventory inventory = player.GetComponent<SimpleInventory>();

            if (inventory == null || !inventory.HasItem(requiredItemId))
            {
                PlaySound(lockedClip);

                if (GameMessageUI.Instance != null)
                    GameMessageUI.Instance.ShowMessage(lockedMessage);
                else
                    Debug.LogWarning(lockedMessage + " Necesitas: " + requiredItemId);

                return;
            }
        }

        StartCoroutine(ToggleDoor());
    }

    private bool VisionsCompleted()
    {
        if (LensManager.Instance == null)
        {
            Debug.LogWarning("[DoorInteractable] requireAllVisions está activado pero no hay LensManager en la escena.");
            return true; 
        }

        return LensManager.Instance.CurrentCount >= LensManager.Instance.TotalVisions;
    }

    private IEnumerator ToggleDoor()
    {
        isMoving = true;

        Quaternion start = transform.localRotation;
        Quaternion target = isOpen ? closedRotation : openRotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            transform.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        transform.localRotation = target;
        isOpen = !isOpen;
        isMoving = false;
        PlaySound(isOpen ? openClip : closeClip);

        if (isOpen)
        {
            if (GameMessageUI.Instance != null && !string.IsNullOrEmpty(openMessage))
                GameMessageUI.Instance.ShowMessage(openMessage);

            if (!string.IsNullOrEmpty(taskIdOnOpen))
            {
                if (TaskListUI.Instance != null)
                    TaskListUI.Instance.CompleteTask(taskIdOnOpen);
                else
                    Debug.LogWarning("Task completada (sin TaskListUI): " + taskIdOnOpen);
            }

            if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(objectiveAfterOpen))
                ObjectiveManager.Instance.SetObjective(objectiveAfterOpen);

            if (returnToMainMenuOnOpen)
                StartCoroutine(ReturnToMainMenuAfterDelay());
        }
    }

    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeMainMenu);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, transform.position, 0.65f);
    }
}

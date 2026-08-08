using System.Collections;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public string requiredItemId = "";
    public float openAngle = 90f;
    public float openSpeed = 4f;

    [Header("Messages")]
    public string lockedMessage = "La puerta está cerrada.";
    public string openMessage = "";
    public string objectiveAfterOpen = "";
    public string taskIdOnOpen = "";

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

        if (!string.IsNullOrEmpty(requiredItemId))
        {
            SimpleInventory inventory = player.GetComponent<SimpleInventory>();

            if (inventory == null || !inventory.HasItem(requiredItemId))
            {
                if (GameMessageUI.Instance != null)
                    GameMessageUI.Instance.ShowMessage(lockedMessage);
                else
                    Debug.LogWarning(lockedMessage + " Necesitas: " + requiredItemId);

                return;
            }
        }

        StartCoroutine(ToggleDoor());
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
        }
    }
}

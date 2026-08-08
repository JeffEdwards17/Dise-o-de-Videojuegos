using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    public string itemId = "key";
    public string itemName = "Llave";

    [TextArea(2, 4)]
    public string pickupMessage = "";

    [TextArea(2, 4)]
    public string objectiveAfterPickup = "";

    public string taskOnPickup = "";

    [Tooltip("Objetos que se desactivan al recoger (ej. barrera de energía).")]
    public GameObject[] deactivateOnPickup;

    public string Prompt
    {
        get { return "E - Recoger " + itemName; }
    }

    public void Interact(PlayerController player)
    {
        SimpleInventory inventory = player.GetComponent<SimpleInventory>();

        if (inventory != null)
            inventory.AddItem(itemId);

        if (GameMessageUI.Instance != null)
        {
            if (!string.IsNullOrEmpty(pickupMessage))
                GameMessageUI.Instance.ShowMessage(pickupMessage);
            else
                GameMessageUI.Instance.ShowMessage("Obtuviste: " + itemName);
        }

        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(objectiveAfterPickup))
            ObjectiveManager.Instance.SetObjective(objectiveAfterPickup);

        if (!string.IsNullOrEmpty(taskOnPickup))
        {
            if (TaskListUI.Instance != null)
                TaskListUI.Instance.CompleteTask(taskOnPickup);
            else
                Debug.LogWarning("Task completada (sin TaskListUI): " + taskOnPickup);
        }

        if (deactivateOnPickup != null)
        {
            foreach (GameObject go in deactivateOnPickup)
            {
                if (go != null)
                    go.SetActive(false);
            }
        }

        Destroy(gameObject);
    }
}

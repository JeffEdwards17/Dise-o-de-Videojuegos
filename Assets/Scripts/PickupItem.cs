using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    public string itemId = "key";
    public string itemName = "Llave";

    public string Prompt
    {
        get { return "E - Recoger " + itemName; }
    }

    public void Interact(PlayerController player)
    {
        SimpleInventory inventory = player.GetComponent<SimpleInventory>();

        if (inventory != null)
            inventory.AddItem(itemId);

        Destroy(gameObject);
    }
}
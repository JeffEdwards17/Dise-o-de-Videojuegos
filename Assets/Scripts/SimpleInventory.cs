using System.Collections.Generic;
using UnityEngine;

public class SimpleInventory : MonoBehaviour
{
    private HashSet<string> items = new HashSet<string>();

    public void AddItem(string itemId)
    {
        if (!items.Contains(itemId))
        {
            items.Add(itemId);
            Debug.Log("Item obtenido: " + itemId);
        }
    }

    public bool HasItem(string itemId)
    {
        return items.Contains(itemId);
    }
}
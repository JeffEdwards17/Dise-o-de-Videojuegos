using System.Collections.Generic;
using UnityEngine;

public class SimpleInventory : MonoBehaviour
{
    private static HashSet<string> globalItems = new HashSet<string>();

    public void AddItem(string itemId)
    {
        if (!globalItems.Contains(itemId))
        {
            globalItems.Add(itemId);
            Debug.Log("Item obtenido: " + itemId);
        }
    }

    public bool HasItem(string itemId)
    {
        return globalItems.Contains(itemId);
    }

    public static void ClearInventory()
    {
        globalItems.Clear();
    }
}

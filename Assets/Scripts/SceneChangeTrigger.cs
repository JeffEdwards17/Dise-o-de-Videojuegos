using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeTrigger : MonoBehaviour
{
    public string sceneName = "Forest_Level2";

    [Header("Requirements")]
    public string requiredItemId = "";
    public string blockedMessage = "Aún no puedes salir.";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!string.IsNullOrEmpty(requiredItemId))
        {
            SimpleInventory inventory = other.GetComponent<SimpleInventory>();

            if (inventory == null || !inventory.HasItem(requiredItemId))
            {
                if (GameMessageUI.Instance != null)
                    GameMessageUI.Instance.ShowMessage(blockedMessage);
                else
                    Debug.Log(blockedMessage);

                return;
            }
        }

        SceneManager.LoadScene(sceneName);
    }
}

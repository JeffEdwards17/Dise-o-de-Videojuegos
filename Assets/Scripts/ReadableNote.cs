using UnityEngine;

public class ReadableNote : MonoBehaviour, IInteractable
{
    public string noteTitle = "Nota";

    [TextArea(3, 8)]
    public string noteText = "Texto de la nota.";

    [TextArea(2, 4)]
    public string objectiveAfterRead = "";

    public string Prompt
    {
        get { return "E - Leer " + noteTitle; }
    }

    public void Interact(PlayerController player)
    {
        if (GameMessageUI.Instance != null)
            GameMessageUI.Instance.ShowMessage(noteText, 5f);

        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(objectiveAfterRead))
            ObjectiveManager.Instance.SetObjective(objectiveAfterRead);
    }
}

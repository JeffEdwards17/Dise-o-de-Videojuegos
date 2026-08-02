using TMPro;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    public TMP_Text objectiveText;
    public string initialObjective = "Escapa de la cabaña de Noc.";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetObjective(initialObjective);
    }

    public void SetObjective(string objective)
    {
        if (objectiveText != null)
            objectiveText.text = "Objetivo: " + objective;
    }
}

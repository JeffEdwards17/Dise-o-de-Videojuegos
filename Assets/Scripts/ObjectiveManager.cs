using TMPro;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("Legacy")]
    public bool useLegacyObjective;
    public TMP_Text objectiveText;
    public string initialObjective = "Escapa de la cabaña de Noc.";

    private void Awake()
    {
        // TaskListUI es el sistema principal; este texto se conserva por compatibilidad.
        if (!useLegacyObjective)
        {
            if (objectiveText != null)
                objectiveText.gameObject.SetActive(false);
            enabled = false;
            return;
        }

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

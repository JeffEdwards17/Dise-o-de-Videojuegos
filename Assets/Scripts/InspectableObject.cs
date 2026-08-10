using TMPro;
using UnityEngine;

/// <summary>
/// Objeto que se examina de cerca (HU: inspección).
/// Muestra un panel con el nombre y la descripción del objeto.
/// Si requiredItemId no está vacío, hace falta ese objeto del inventario
/// (por ejemplo la lupa) para poder examinarlo.
/// </summary>
public class InspectableObject : MonoBehaviour, IInteractable
{
    public static bool IsAnyOpen;
    private static int lastEscapeCloseFrame = -1;

    public static bool ConsumedEscapeThisFrame
    {
        get { return lastEscapeCloseFrame == Time.frameCount; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Asegura que una sesión de Play anterior (con panel abierto) no herede
        // estado bloqueado en la siguiente carga de escena.
        IsAnyOpen = false;
        lastEscapeCloseFrame = -1;
    }

    [Header("Contenido")]
    public string objectName = "Objeto";
    [TextArea(2, 8)] public string inspectText = "No se distingue nada.";
    public string requiredItemId = "";
    [TextArea(1, 3)] public string blockedMessage = "Necesitas un objeto para examinar esto mejor.";
    public string objectiveAfterInspect = "";
    public string taskOnInspect = "";

    [Header("UI (las conecta el builder)")]
    public GameObject inspectPanel;
    public TMP_Text titleText;
    public TMP_Text bodyText;

    private bool inspected;
    private PlayerController inspectingPlayer;
    private Interactor inspectingInteractor;
    private bool playerWasEnabled;
    private bool interactorWasEnabled;

    public string Prompt
    {
        get { return "E - Inspeccionar"; }
    }

    public void Interact(PlayerController player)
    {
        if (inspectPanel == null || IsAnyOpen)
            return;

        if (!string.IsNullOrEmpty(requiredItemId))
        {
            var inv = player != null ? player.GetComponent<SimpleInventory>() : null;
            if (inv == null || !inv.HasItem(requiredItemId))
            {
                if (GameMessageUI.Instance != null)
                    GameMessageUI.Instance.ShowMessage(blockedMessage);
                return;
            }
        }

        if (titleText != null)
            titleText.text = objectName;

        if (bodyText != null)
            bodyText.text = inspectText;

        inspectPanel.SetActive(true);
        IsAnyOpen = true;

        inspectingPlayer = player;
        inspectingInteractor = player != null ? player.GetComponent<Interactor>() : null;
        playerWasEnabled = inspectingPlayer != null && inspectingPlayer.enabled;
        interactorWasEnabled = inspectingInteractor != null && inspectingInteractor.enabled;

        if (inspectingPlayer != null)
            inspectingPlayer.enabled = false;
        if (inspectingInteractor != null)
            inspectingInteractor.enabled = false;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!inspected)
        {
            if (!string.IsNullOrEmpty(objectiveAfterInspect))
            {
                if (ObjectiveManager.Instance != null)
                    ObjectiveManager.Instance.SetObjective(objectiveAfterInspect);
            }

            if (!string.IsNullOrEmpty(taskOnInspect))
            {
                if (TaskListUI.Instance != null)
                    TaskListUI.Instance.CompleteTask(taskOnInspect);
                else
                    Debug.LogWarning("Task completada (sin TaskListUI): " + taskOnInspect);
            }

            inspected = true;
        }
    }

    private void Update()
    {
        if (!IsAnyOpen || inspectPanel == null || !inspectPanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            lastEscapeCloseFrame = Time.frameCount;
            Close();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            Close();
        }
    }

    public void Close()
    {
        if (inspectPanel != null)
            inspectPanel.SetActive(false);

        IsAnyOpen = false;

        Time.timeScale = 1f;

        if (inspectingPlayer != null)
            inspectingPlayer.enabled = playerWasEnabled;
        if (inspectingInteractor != null)
            inspectingInteractor.enabled = interactorWasEnabled;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

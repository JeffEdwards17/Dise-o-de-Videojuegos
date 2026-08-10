using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla la pausa del juego y sus botones principales.
/// El controlador permanece activo mientras el panel visual se oculta o muestra.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject pausePanel;
    public Button continueButton;
    public Button restartButton;
    public Button settingsButton;
    public Button menuButton;

    [Header("Configuración")]
    public string mainMenuScene = "MainMenu";
    public string currentScene = "Cabin_Level1";

    private bool isPaused;
    private PlayerController playerController;
    private Interactor interactor;
    private bool playerWasEnabled;
    private bool interactorWasEnabled;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(Resume);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);

        if (settingsButton != null)
            settingsButton.interactable = false;

        if (menuButton != null)
            menuButton.onClick.AddListener(GoToMainMenu);
    }

    private void Start()
    {
        // Evita que una escena nueva comience con el tiempo congelado.
        isPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Si hay un panel de inspección abierto, ESC lo cierra primero
            // en lugar de abrir la pausa (lo gestiona InspectableObject).
            if (InspectableObject.IsAnyOpen || InspectableObject.ConsumedEscapeThisFrame)
                return;

            if (isPaused) Resume();
            else Pause();
        }
    }

    private void Pause()
    {
        playerController = FindObjectOfType<PlayerController>();
        interactor = FindObjectOfType<Interactor>();
        playerWasEnabled = playerController != null && playerController.enabled;
        interactorWasEnabled = interactor != null && interactor.enabled;

        if (playerController != null)
            playerController.enabled = false;
        if (interactor != null)
            interactor.enabled = false;

        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (playerController != null)
            playerController.enabled = playerWasEnabled;
        if (interactor != null)
            interactor.enabled = interactorWasEnabled;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerPrefs.SetString("UltimaEscena", currentScene);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void RestartLevel()
    {
        // Restaura el tiempo antes de recargar la escena.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerPrefs.SetString("UltimaEscena", currentScene);
        SceneManager.LoadScene(mainMenuScene);
    }
}

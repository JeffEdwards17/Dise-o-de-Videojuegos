using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pausa del juego (HU: pausa con Esc).
/// Colocar en el GameObject del panel de pausa creado por el builder.
/// Deshabilitado al inicio de la escena; lo activa la intro al terminar.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias (las conecta el builder)")]
    public GameObject pausePanel;
    public Button continueButton;
    public Button menuButton;

    [Header("Configuración")]
    public string mainMenuScene = "MainMenu";
    public string currentScene = "Cabin_Level1";

    private bool isPaused;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(Resume);

        if (menuButton != null)
            menuButton.onClick.AddListener(GoToMainMenu);
    }

    private void Start()
    {
        // Estado seguro al entrar en la escena: jamás nacer pausado.
        // Si algo dejó el tiempo congelado, lo restauramos.
        isPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Si hay un panel de inspección abierto, ESC lo cierra primero
            // en lugar de abrir la pausa (lo gestiona InspectableObject).
            if (InspectableObject.IsAnyOpen)
                return;

            if (isPaused) Resume();
            else Pause();
        }
    }

    private void Pause()
    {
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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerPrefs.SetString("UltimaEscena", currentScene);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("UltimaEscena", currentScene);
        SceneManager.LoadScene(mainMenuScene);
    }
}

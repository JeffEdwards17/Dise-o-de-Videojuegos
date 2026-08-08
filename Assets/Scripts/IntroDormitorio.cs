using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Intro de despertar (HU: inicio de partida).
/// Desactiva los controles, muestra un texto sobre una pantalla negra
/// y espera un clic o ESPACIO para continuar.
/// Al terminar reanuda el tiempo y activa la pausa.
/// </summary>
public class IntroDormitorio : MonoBehaviour
{
    [Header("Referencias (las conecta el builder)")]
    public Image panel;
    public TMP_Text introText;
    public PauseMenu pauseMenu;

    [Header("Configuración")]
    public string textoIntro = "Despiertas en la celda de la cabana. Solo recuerdas el bosque...\ny algo que te miraba entre los arboles.";
    public float fadeInDuration = 1.5f;
    public float fadeOutDuration = 1.2f;
    public float timeoutAfterText = 12f;

    private void Awake()
    {
        // La intro NO congela el tiempo: solo bloquea el input del jugador.
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null) pc.enabled = false;

        var it = FindObjectOfType<Interactor>();
        if (it != null) it.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panel != null)
            panel.color = new Color(0f, 0f, 0f, 1f);

        if (introText != null)
        {
            introText.text = textoIntro;
            introText.color = new Color(introText.color.r, introText.color.g, introText.color.b, 0f);
        }
    }

    private void Start()
    {
        StartCoroutine(RutinaIntro());
    }

    private System.Collections.IEnumerator RutinaIntro()
    {
        float t = 0f;

        if (panel != null)
        {
            while (t < fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                panel.color = new Color(0f, 0f, 0f, 1f);
                yield return null;
            }
        }

        t = 0f;
        if (introText != null)
        {
            while (t < 1.2f)
            {
                t += Time.unscaledDeltaTime;
                Color c = introText.color;
                c.a = Mathf.Lerp(0f, 1f, t / 1.2f);
                introText.color = c;
                yield return null;
            }
        }

        float waiting = 0f;
        while (waiting < timeoutAfterText)
        {
            waiting += Time.unscaledDeltaTime;

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
                break;

            yield return null;
        }

        t = 0f;
        if (introText != null)
        {
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                Color c = introText.color;
                c.a = Mathf.Lerp(1f, 0f, t / 0.4f);
                introText.color = c;
                yield return null;
            }
        }

        t = 0f;
        if (panel != null)
        {
            while (t < fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                panel.color = new Color(0f, 0f, 0f, 1f - (t / fadeOutDuration));
                yield return null;
            }

            panel.gameObject.SetActive(false);
        }

        Terminar();
    }

    private void Terminar()
    {
        Time.timeScale = 1f;

        var pc = FindObjectOfType<PlayerController>();
        if (pc != null) pc.enabled = true;

        var it = FindObjectOfType<Interactor>();
        if (it != null) it.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pauseMenu != null)
            pauseMenu.enabled = true;

        gameObject.SetActive(false);
    }
}

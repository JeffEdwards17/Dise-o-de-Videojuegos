using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string primerNivelScene = "Cabin_Level1";

    [Header("Botones")]
    [SerializeField] private Button nuevaPartidaBtn;
    [SerializeField] private Button continuarBtn;
    [SerializeField] private Button ajustesBtn;
    [SerializeField] private Button salirBtn;

    [Header("Paneles")]
    [SerializeField] private GameObject panelAjustes;
    [SerializeField] private GameObject panelConfirmarSalida;
    [SerializeField] private CanvasGroup fadeCanvasGroup; 

    [Header("Transición")]
    [SerializeField] private float duracionFade = 1f;

    private void Awake()
    {
        nuevaPartidaBtn.onClick.AddListener(OnNuevaPartida);
        ajustesBtn.onClick.AddListener(AbrirAjustes);
        salirBtn.onClick.AddListener(PedirConfirmacionSalida);

        if (continuarBtn != null)
        {
            continuarBtn.onClick.AddListener(OnContinuar);
            continuarBtn.interactable = ExisteGuardado();
        }

        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelConfirmarSalida != null) panelConfirmarSalida.SetActive(false);
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    private bool ExisteGuardado()
    {
        // Continue permanece deshabilitado hasta que exista un guardado real
        return false;
    }

    private void OnNuevaPartida()
    {
        SimpleInventory.ClearInventory();
        CargarEscena(primerNivelScene);
    }

    private void OnContinuar()
    {
        string ultimaEscena = PlayerPrefs.GetString("UltimaEscena", primerNivelScene);
        CargarEscena(ultimaEscena);
    }

    private void CargarEscena(string nombreEscena)
    {
        StartCoroutine(FadeYCargar(nombreEscena));
    }

    private System.Collections.IEnumerator FadeYCargar(string nombreEscena)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            float t = 0f;
            while (t < duracionFade)
            {
                t += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(t / duracionFade);
                yield return null;
            }
        }
        SceneManager.LoadScene(nombreEscena);
    }

    private void AbrirAjustes()
    {
        panelAjustes.SetActive(true);
    }

    public void CerrarAjustes()
    {
        panelAjustes.SetActive(false);
    }

    private void PedirConfirmacionSalida()
    {
        panelConfirmarSalida.SetActive(true);
    }

    public void CancelarSalida()
    {
        panelConfirmarSalida.SetActive(false);
    }

    public void ConfirmarSalida()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

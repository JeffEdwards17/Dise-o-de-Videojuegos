using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumenGeneralSlider;
    [SerializeField] private Slider musicaSlider;
    [SerializeField] private Slider efectosSlider;

    [Header("Control")]
    [SerializeField] private Slider sensibilidadSlider;
    [SerializeField] private Slider brilloSlider;
    [SerializeField] private Button cerrarButton;

    [Header("Brillo")]
    [Tooltip("Panel semitransparente encima de la cámara para simular brillo")]
    [SerializeField] private CanvasGroup overlayBrillo;

    private const string K_VOL = "VolumenGeneral";
    private const string K_MUS = "Musica";
    private const string K_SFX = "Efectos";
    private const string K_SENS = "Sensibilidad";
    private const string K_BRI = "Brillo";

    private void OnEnable()
    {
        if (volumenGeneralSlider == null || musicaSlider == null || efectosSlider == null ||
            sensibilidadSlider == null || brilloSlider == null)
        {
            Debug.LogError("El panel de ajustes necesita sus cinco sliders asignados.", this);
            enabled = false;
            return;
        }

        CargarValoresGuardados();

        volumenGeneralSlider.onValueChanged.AddListener(SetVolumenGeneral);
        musicaSlider.onValueChanged.AddListener(SetMusica);
        efectosSlider.onValueChanged.AddListener(SetEfectos);
        sensibilidadSlider.onValueChanged.AddListener(SetSensibilidad);
        brilloSlider.onValueChanged.AddListener(SetBrillo);
        if (cerrarButton != null)
            cerrarButton.onClick.AddListener(CerrarPanel);
    }

    private void OnDisable()
    {
        volumenGeneralSlider.onValueChanged.RemoveListener(SetVolumenGeneral);
        musicaSlider.onValueChanged.RemoveListener(SetMusica);
        efectosSlider.onValueChanged.RemoveListener(SetEfectos);
        sensibilidadSlider.onValueChanged.RemoveListener(SetSensibilidad);
        brilloSlider.onValueChanged.RemoveListener(SetBrillo);
        if (cerrarButton != null)
            cerrarButton.onClick.RemoveListener(CerrarPanel);
    }

    private void CargarValoresGuardados()
    {
        volumenGeneralSlider.value = PlayerPrefs.GetFloat(K_VOL, 0.7f);
        musicaSlider.value = PlayerPrefs.GetFloat(K_MUS, 0.55f);
        efectosSlider.value = PlayerPrefs.GetFloat(K_SFX, 0.8f);
        sensibilidadSlider.value = PlayerPrefs.GetFloat(K_SENS, 0.5f);
        brilloSlider.value = PlayerPrefs.GetFloat(K_BRI, 0.5f);

        AplicarTodo();
    }

    private void AplicarTodo()
    {
        SetVolumenGeneral(volumenGeneralSlider.value);
        SetMusica(musicaSlider.value);
        SetEfectos(efectosSlider.value);
        SetSensibilidad(sensibilidadSlider.value);
        SetBrillo(brilloSlider.value);
    }

    private float LinealADecibeles(float valor01)
    {
        // Los sliders van de 0 a 1; el Audio Mixer trabaja en dB (-80 a 0)
        return Mathf.Log10(Mathf.Clamp(valor01, 0.0001f, 1f)) * 20f;
    }

    public void SetVolumenGeneral(float valor)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", LinealADecibeles(valor));
        PlayerPrefs.SetFloat(K_VOL, valor);
    }

    public void SetMusica(float valor)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", LinealADecibeles(valor));
        PlayerPrefs.SetFloat(K_MUS, valor);
    }

    public void SetEfectos(float valor)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", LinealADecibeles(valor));
        PlayerPrefs.SetFloat(K_SFX, valor);
    }

    public void SetSensibilidad(float valor)
    {
        // Rango sugerido: 0.5 a 5.0 grados por pixel de movimiento del mouse
        float sensibilidadFinal = Mathf.Lerp(0.5f, 5f, valor);
        PlayerPrefs.SetFloat(K_SENS, valor);
        PlayerPrefs.SetFloat("SensibilidadFinal", sensibilidadFinal);
        // El script de la cámara en primera persona debe leer "SensibilidadFinal"
    }

    public void SetBrillo(float valor)
    {
        PlayerPrefs.SetFloat(K_BRI, valor);
        if (overlayBrillo != null)
        {
            // A menor brillo, más oscuro el overlay (máx. 0.6 de opacidad)
            overlayBrillo.alpha = Mathf.Lerp(0.6f, 0f, valor);
        }
    }

    private void CerrarPanel()
    {
        gameObject.SetActive(false);
    }
}

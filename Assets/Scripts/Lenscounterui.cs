using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
public class LensCounterUI : MonoBehaviour
{
    [Header("Arrastra aquí un Text (TMP) ya creado en tu Canvas")]
    public TMP_Text counterText;
 
    [Header("Opcional: ícono del lente")]
    public Image lensIcon;
 
    private bool subscribed = false;
 
    private void Start()
    {
        // Se suscribe en Start (no en OnEnable) para garantizar que
        // LensManager.Awake() ya haya corrido y su Instance ya exista.
        if (LensManager.Instance != null && !subscribed)
        {
            LensManager.Instance.OnProgressChanged += UpdateCounter;
            subscribed = true;
            UpdateCounter(LensManager.Instance.CurrentCount, LensManager.Instance.TotalVisions);
        }
        else if (LensManager.Instance == null)
        {
            Debug.LogWarning("[LensCounterUI] No se encontró LensManager en la escena.");
        }
    }
 
    private void OnDisable()
    {
        if (LensManager.Instance != null && subscribed)
        {
            LensManager.Instance.OnProgressChanged -= UpdateCounter;
            subscribed = false;
        }
    }
 
    private void UpdateCounter(int current, int total)
    {
        if (counterText != null)
            counterText.text = current + " / " + total;
    }
}

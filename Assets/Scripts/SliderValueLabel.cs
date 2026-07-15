using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Slider))]
public class SliderValueLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string nombreControl = "Volumen general";
    [SerializeField] private bool mostrarComoPorcentaje = true;

    private Slider slider;
    private Color colorNormal = new Color(0.42f, 0.40f, 0.35f);
    private Color colorHover = new Color(0.79f, 0.76f, 0.67f);

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(Actualizar);
        Actualizar(slider.value);
        label.color = colorNormal;
    }

    private void Actualizar(float valor)
    {
        int mostrado = mostrarComoPorcentaje ? Mathf.RoundToInt(valor * 100) : Mathf.RoundToInt(valor);
        label.text = nombreControl + "   " + mostrado;
    }

    public void OnPointerEnter(PointerEventData eventData) { label.color = colorHover; }
    public void OnPointerExit(PointerEventData eventData) { label.color = colorNormal; }
}
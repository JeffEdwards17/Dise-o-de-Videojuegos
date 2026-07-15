using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text texto;
    [SerializeField] private Color colorNormal = new Color(0.42f, 0.40f, 0.35f);
    [SerializeField] private Color colorHover = new Color(0.79f, 0.76f, 0.67f);

    private void Awake()
    {
        if (texto == null) texto = GetComponentInChildren<TMP_Text>();
        texto.color = colorNormal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        texto.color = colorHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        texto.color = colorNormal;
    }
}
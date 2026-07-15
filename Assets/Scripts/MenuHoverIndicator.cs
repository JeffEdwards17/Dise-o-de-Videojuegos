using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuHoverIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text indicador;

    private void Awake()
    {
        if (indicador != null) SetVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        Color c = indicador.color;
        c.a = visible ? 1f : 0f;
        indicador.color = c;
    }
}

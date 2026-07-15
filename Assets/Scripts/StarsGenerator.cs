using UnityEngine;
using UnityEngine.UI;

public class StarsGenerator : MonoBehaviour
{
    [SerializeField] private int cantidadEstrellas = 50;
    [SerializeField] private RectTransform area;

    private void Start()
    {
        for (int i = 0; i < cantidadEstrellas; i++)
        {
            GameObject estrella = new GameObject("Star", typeof(Image));
            estrella.transform.SetParent(area, false);

            Image img = estrella.GetComponent<Image>();
            img.color = new Color(0.9f, 0.89f, 0.82f);

            RectTransform rt = estrella.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2, 2);
            rt.anchoredPosition = new Vector2(
                Random.Range(-area.rect.width / 2, area.rect.width / 2),
                Random.Range(0, area.rect.height / 2)
            );

            estrella.AddComponent<StarTwinkle>();
        }
    }
}

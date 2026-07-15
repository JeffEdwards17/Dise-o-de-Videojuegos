using UnityEngine;
using UnityEngine.UI;

public class StarTwinkle : MonoBehaviour
{
    private Image img;
    private float velocidad;
    private float offset;

    private void Awake()
    {
        img = GetComponent<Image>();
        velocidad = Random.Range(0.5f, 1.2f);
        offset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float alpha = Mathf.Lerp(0.15f, 0.85f, (Mathf.Sin((Time.time + offset) * velocidad) + 1f) / 2f);
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
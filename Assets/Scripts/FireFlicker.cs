using UnityEngine;
using UnityEngine.UI;

public class FireFlicker : MonoBehaviour
{
    private Image img;
    [SerializeField] private float velocidad = 7f;
    [SerializeField] private float alphaMin = 0.55f;
    [SerializeField] private float alphaMax = 1f;

    private float offset;

    private void Awake()
    {
        img = GetComponent<Image>();
        offset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float ruido = Mathf.PerlinNoise(Time.time * velocidad, offset);
        float alpha = Mathf.Lerp(alphaMin, alphaMax, ruido);
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}

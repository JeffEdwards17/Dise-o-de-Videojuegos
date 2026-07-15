using UnityEngine;
using TMPro;

public class TitleFlicker : MonoBehaviour
{
    [SerializeField] private float duracionCiclo = 6f;
    private TMP_Text texto;
    private float temporizador;

    private void Awake()
    {
        texto = GetComponent<TMP_Text>();
        temporizador = 0f;
    }

    private void Update()
    {
        temporizador += Time.deltaTime;
        float t = (temporizador % duracionCiclo) / duracionCiclo;

        float alpha = 1f;

        if (t >= 0.71f && t < 0.72f)
            alpha = Mathf.Lerp(1f, 0.7f, (t - 0.71f) / 0.01f);
        else if (t >= 0.72f && t < 0.73f)
            alpha = Mathf.Lerp(0.7f, 1f, (t - 0.72f) / 0.01f);
        else if (t >= 0.73f && t < 0.74f)
            alpha = Mathf.Lerp(1f, 0.85f, (t - 0.73f) / 0.01f);
        else if (t >= 0.74f && t < 0.75f)
            alpha = Mathf.Lerp(0.85f, 1f, (t - 0.74f) / 0.01f);

        Color c = texto.color;
        c.a = alpha;
        texto.color = c;
    }
}
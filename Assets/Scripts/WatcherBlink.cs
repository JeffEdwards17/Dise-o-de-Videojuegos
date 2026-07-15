using UnityEngine;
using UnityEngine.UI;

public class WatcherBlink : MonoBehaviour
{
    private Image img;
    [SerializeField] private float tiempoEntreParpadeos = 6.5f;
    private float temporizador;

    private void Awake()
    {
        img = GetComponent<Image>();
        SetAlpha(0);
        temporizador = Random.Range(2f, tiempoEntreParpadeos);
    }

    private void Update()
    {
        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            temporizador = tiempoEntreParpadeos + Random.Range(-1f, 1f);
            StopAllCoroutines();
            StartCoroutine(Parpadear());
        }
    }

    private System.Collections.IEnumerator Parpadear()
    {
        float t = 0f;
        while (t < 0.4f) { t += Time.deltaTime; SetAlpha(Mathf.Lerp(0, 1, t / 0.4f)); yield return null; }
        yield return new WaitForSeconds(0.6f);
        t = 0f;
        while (t < 0.5f) { t += Time.deltaTime; SetAlpha(Mathf.Lerp(1, 0, t / 0.5f)); yield return null; }
    }

    private void SetAlpha(float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}
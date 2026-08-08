using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public float velocidad = 8f;
    public float factorMin = 0.55f;
    public float factorMax = 1f;

    private Light luz;
    private float baseIntensity;
    private float offset;

    private void Awake()
    {
        luz = GetComponent<Light>();
        baseIntensity = luz != null ? luz.intensity : 1f;
        offset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (luz == null)
            return;

        float ruido = Mathf.PerlinNoise(Time.time * velocidad, offset);
        float factor = Mathf.Lerp(factorMin, factorMax, ruido);
        luz.intensity = baseIntensity * factor;
    }
}

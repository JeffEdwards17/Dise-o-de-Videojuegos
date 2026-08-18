using UnityEngine;
 
[RequireComponent(typeof(Light))]
public class WispLight : MonoBehaviour
{
    [Header("Movimiento flotante")]
    public float floatHeight = 0.3f;
    public float floatSpeed = 1f;
    public float driftRadius = 0.15f;
    public float driftSpeed = 0.6f;
 
    [Header("Parpadeo de luz")]
    public float minIntensity = 0.6f;
    public float maxIntensity = 1.4f;
    public float flickerSpeed = 3f;
 
    [Header("Color")]
    public bool overrideColor = true;
    public Color wispColor = new Color(0.4f, 0.85f, 0.7f);
 
    private Light lightSource;
    private Vector3 startPos;
    private float noiseSeedX;
    private float noiseSeedZ;
    private float noiseSeedFlicker;
 
    private void Awake()
    {
        lightSource = GetComponent<Light>();
        startPos = transform.localPosition;
 
        noiseSeedX = Random.Range(0f, 100f);
        noiseSeedZ = Random.Range(0f, 100f);
        noiseSeedFlicker = Random.Range(0f, 100f);
 
        if (overrideColor)
            lightSource.color = wispColor;
    }
 
    private void Update()
    {
        float t = Time.time;
 
        float y = Mathf.Sin(t * floatSpeed) * floatHeight;
 
        float x = (Mathf.PerlinNoise(noiseSeedX, t * driftSpeed) - 0.5f) * 2f * driftRadius;
        float z = (Mathf.PerlinNoise(noiseSeedZ, t * driftSpeed) - 0.5f) * 2f * driftRadius;
 
        transform.localPosition = startPos + new Vector3(x, y, z);
 
        float flicker = Mathf.PerlinNoise(noiseSeedFlicker, t * flickerSpeed);
        lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, flicker);
    }
}

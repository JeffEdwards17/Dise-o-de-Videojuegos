using System.Collections;
using UnityEngine;

public class AmbientHorrorEvent : MonoBehaviour
{
    [Header("Sonido")]
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.5f;
    public bool oneShot = true;

    [Header("Flicker de luz")]
    public Light flickerLight;
    public float flickerDuration = 2.5f;
    public float flickerMinIntensity = 0.15f;
    public float flickerMaxIntensity = 0.9f;

    private bool triggered;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volume;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (oneShot && triggered)
            return;

        triggered = true;

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);

        if (flickerLight != null)
            StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        float baseIntensity = flickerMaxIntensity;
        float endTime = Time.time + flickerDuration;

        while (Time.time < endTime)
        {
            flickerLight.intensity = Random.Range(flickerMinIntensity, flickerMaxIntensity);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
        }

        flickerLight.intensity = baseIntensity;
    }
}

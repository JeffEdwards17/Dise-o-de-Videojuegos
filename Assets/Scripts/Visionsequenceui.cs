using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VisionSequenceUI : MonoBehaviour
{
    public static VisionSequenceUI Instance;

    [Header("Arrastrar imagen")]
    public Image screenImage;

    [Header("Tiempos")]
    public float fadeInTime = 0.15f;
    public float holdTime = 0.6f;
    public float fadeOutTime = 0.15f;

    [Header("Jugador (se bloquea mientras dura la visión)")]
    public PlayerController player;
    public Interactor interactor;

    [Header("Música de suspenso durante la visión")]
    public AudioSource visionMusicSource;
    public AudioClip visionMusicClip;
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    public float musicFadeOutTime = 0.5f;

    [Header("Efectos de sonido por imagen")]
    public AudioSource sfxSource;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("Movimiento")]
    public bool enableCreepyMotion = true;
    public float shakeAmount = 6f;
    public float shakeSpeed = 4f;
    public float zoomAmount = 0.06f;

    private bool playing = false;
    private RectTransform screenRect;
    private Vector2 baseAnchoredPos;
    private Vector3 baseScale;

    private void Awake()
    {
        Instance = this;

        if (screenImage != null)
        {
            SetAlpha(0f);
            screenImage.gameObject.SetActive(false);

            screenRect = screenImage.rectTransform;
            baseAnchoredPos = screenRect.anchoredPosition;
            baseScale = screenRect.localScale;
        }
    }

    public void Play(Sprite[] images, AudioClip[] groupSounds = null, System.Action onComplete = null)
    {
        if (playing)
            return;

        StartCoroutine(PlaySequence(images, groupSounds, onComplete));
    }

    private IEnumerator PlaySequence(Sprite[] images, AudioClip[] groupSounds, System.Action onComplete)
    {
        playing = true;

        if (player != null) player.enabled = false;
        if (interactor != null) interactor.enabled = false;

        if (screenImage != null)
            screenImage.gameObject.SetActive(true);

        Coroutine motionRoutine = null;
        if (enableCreepyMotion && screenRect != null)
            motionRoutine = StartCoroutine(CreepyMotion());

        if (visionMusicSource != null && visionMusicClip != null)
        {
            visionMusicSource.clip = visionMusicClip;
            visionMusicSource.volume = musicVolume;
            visionMusicSource.loop = true;
            visionMusicSource.Play();
        }

        Coroutine soundGroupRoutine = null;
        if (sfxSource != null && groupSounds != null && groupSounds.Length > 0)
            soundGroupRoutine = StartCoroutine(PlayGroupSoundsInOrder(groupSounds));

        if (images != null)
        {
            for (int i = 0; i < images.Length; i++)
            {
                if (screenImage != null)
                    screenImage.sprite = images[i];

                yield return Fade(0f, 1f, fadeInTime);
                yield return new WaitForSeconds(holdTime);
                yield return Fade(1f, 0f, fadeOutTime);
            }
        }

        if (screenImage != null)
            screenImage.gameObject.SetActive(false);

        if (soundGroupRoutine != null)
            StopCoroutine(soundGroupRoutine);

        if (motionRoutine != null)
            StopCoroutine(motionRoutine);

        if (screenRect != null)
        {
            screenRect.anchoredPosition = baseAnchoredPos;
            screenRect.localScale = baseScale;
        }

        if (visionMusicSource != null)
            yield return FadeOutMusic();

        if (player != null) player.enabled = true;
        if (interactor != null) interactor.enabled = true;

        playing = false;
        onComplete?.Invoke();
    }

    private IEnumerator PlayGroupSoundsInOrder(AudioClip[] soundGroup)
    {
        foreach (AudioClip clip in soundGroup)
        {
            if (clip == null)
                continue;

            sfxSource.PlayOneShot(clip, sfxVolume);
            yield return new WaitForSeconds(clip.length);
        }
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (screenImage == null || duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }

        SetAlpha(to);
    }

    private IEnumerator CreepyMotion()
    {
        float seedX = Random.Range(0f, 100f);
        float seedY = Random.Range(0f, 100f);
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            float nx = (Mathf.PerlinNoise(seedX, Time.unscaledTime * shakeSpeed) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(seedY, Time.unscaledTime * shakeSpeed) - 0.5f) * 2f;

            screenRect.anchoredPosition = baseAnchoredPos + new Vector2(nx, ny) * shakeAmount;

            float zoom = 1f + Mathf.Clamp01(elapsed * 0.05f) * zoomAmount;
            screenRect.localScale = baseScale * zoom;

            yield return null;
        }
    }

    private IEnumerator FadeOutMusic()
    {
        float startVolume = visionMusicSource.volume;
        float t = 0f;

        while (t < musicFadeOutTime)
        {
            t += Time.deltaTime;
            visionMusicSource.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeOutTime);
            yield return null;
        }

        visionMusicSource.Stop();
        visionMusicSource.volume = musicVolume;
    }

    private void SetAlpha(float a)
    {
        if (screenImage == null)
            return;

        Color c = screenImage.color;
        c.a = a;
        screenImage.color = c;
    }
}

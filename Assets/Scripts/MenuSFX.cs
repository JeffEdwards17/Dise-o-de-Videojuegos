using UnityEngine;

public class MenuSFX : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoClic;
    [SerializeField] private AudioClip sonidoHover;

    public void ReproducirClic()
    {
        audioSource.PlayOneShot(sonidoClic);
    }

    public void ReproducirHover()
    {
        audioSource.PlayOneShot(sonidoHover);
    }
}
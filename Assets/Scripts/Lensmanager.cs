using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
 
public class LensManager : MonoBehaviour
{
    public static LensManager Instance;
 
    public int TotalVisions = 3;
    private readonly HashSet<string> collected = new HashSet<string>();
 
    public int CurrentCount => collected.Count;
 
    public delegate void ProgressChanged(int current, int total);
    public event ProgressChanged OnProgressChanged;
 
    public delegate void AllVisionsCollected();
    public event AllVisionsCollected OnAllCollected;
 
    [Header("Acciones al completar las 3 visiones (arrastra objetos aquí)")]
    public UnityEvent OnAllCollectedUnityEvent;
 
    private void Awake()
    {
        Instance = this;
    }
 
    public void RegisterVision(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId) || collected.Contains(zoneId))
            return;
 
        collected.Add(zoneId);
        OnProgressChanged?.Invoke(collected.Count, TotalVisions);
 
        if (collected.Count >= TotalVisions)
        {
            OnAllCollected?.Invoke();
            OnAllCollectedUnityEvent?.Invoke();
        }
    }
}

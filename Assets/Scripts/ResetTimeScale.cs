using UnityEngine;
 
public class ResetTimeScale : MonoBehaviour
{
    private void Awake()
    {
        Time.timeScale = 1f;
    }
}

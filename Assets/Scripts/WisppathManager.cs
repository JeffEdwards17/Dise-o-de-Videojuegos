using UnityEngine;
 
public class WispPathManager : MonoBehaviour
{
    [System.Serializable]
    public class WispGroup
    {
        public string label = "Tramo hacia zona X";
        public GameObject[] wisps;
    }
 
    [Header("Un grupo por tramo del camino, EN ORDEN")]
    [Tooltip("paths[0] = camino hacia la 1ra visión. paths[1] = camino hacia la 2da (aparece al completar la 1ra). Y así sucesivamente.")]
    public WispGroup[] paths;
 
    private void Start()
    {
        if (LensManager.Instance != null)
            LensManager.Instance.OnProgressChanged += HandleProgress;
 
        ShowOnly(0);
    }
 
    private void OnDestroy()
    {
        if (LensManager.Instance != null)
            LensManager.Instance.OnProgressChanged -= HandleProgress;
    }
 
    private void HandleProgress(int current, int total)
    {
        ShowOnly(current);
    }
 
    private void ShowOnly(int activeIndex)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            bool active = (i == activeIndex);
 
            if (paths[i].wisps == null)
                continue;
 
            foreach (GameObject wisp in paths[i].wisps)
            {
                if (wisp != null)
                    wisp.SetActive(active);
            }
        }
    }
}

using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    private bool yaActivado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (yaActivado) return;
        if (!other.CompareTag("Player")) return;

        yaActivado = true;
        SaveManager.GuardarProgreso();
    }
}
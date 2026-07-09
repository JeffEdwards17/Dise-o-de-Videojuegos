using System.Collections;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    public string requiredItemId = "";
    public float openAngle = 90f;
    public float openSpeed = 4f;

    private bool isOpen;
    private bool isMoving;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string Prompt
    {
        get
        {
            if (isOpen)
                return "E - Cerrar";

            return "E - Abrir";
        }
    }

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    public void Interact(PlayerController player)
    {
        if (isMoving)
            return;

        if (!string.IsNullOrEmpty(requiredItemId))
        {
            SimpleInventory inventory = player.GetComponent<SimpleInventory>();

            if (inventory == null || !inventory.HasItem(requiredItemId))
            {
                Debug.Log("La puerta está cerrada. Necesitas: " + requiredItemId);
                return;
            }
        }

        StartCoroutine(ToggleDoor());
    }

    private IEnumerator ToggleDoor()
    {
        isMoving = true;

        Quaternion start = transform.localRotation;
        Quaternion target = isOpen ? closedRotation : openRotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            transform.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        transform.localRotation = target;
        isOpen = !isOpen;
        isMoving = false;
    }
}
using TMPro;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    public Transform cameraTransform;
    public float interactDistance = 3f;
    public LayerMask interactMask = ~0;
    public TMP_Text promptText;

    private PlayerController player;
    private IInteractable currentInteractable;

    private void Awake()
    {
        player = GetComponent<PlayerController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        FindInteractable();

        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact(player);
        }
    }

    private void FindInteractable()
    {
        currentInteractable = null;

        if (cameraTransform == null)
            return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    currentInteractable = interactable;
                    break;
                }
            }
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(currentInteractable != null);

            if (currentInteractable != null)
                promptText.text = currentInteractable.Prompt;
        }
    }
}
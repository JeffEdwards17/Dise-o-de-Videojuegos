using UnityEngine;

public class TaskTrigger : MonoBehaviour
{
    public string taskId = "";

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        if (TaskListUI.Instance != null)
            TaskListUI.Instance.CompleteTask(taskId);

        Destroy(gameObject);
    }
}

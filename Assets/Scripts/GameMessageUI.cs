using System.Collections;
using TMPro;
using UnityEngine;

public class GameMessageUI : MonoBehaviour
{
    public static GameMessageUI Instance;

    public TMP_Text messageText;
    public float defaultDuration = 3f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;

        if (messageText != null)
            messageText.text = "";
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, defaultDuration);
    }

    public void ShowMessage(string message, float duration)
    {
        if (messageText == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowMessageRoutine(message, duration));
    }

    private IEnumerator ShowMessageRoutine(string message, float duration)
    {
        messageText.text = message;
        yield return new WaitForSeconds(duration);
        messageText.text = "";
    }
}

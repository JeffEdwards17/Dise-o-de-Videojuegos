using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveManager
{
    private const string SAVE_KEY = "NocturiaSaveExists";
    private const string SCENE_KEY = "UltimaEscena";

    public static void GuardarProgreso()
    {
        PlayerPrefs.SetInt(SAVE_KEY, 1);
        PlayerPrefs.SetString(SCENE_KEY, SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        Debug.Log("Progreso guardado en: " + SceneManager.GetActiveScene().name);
    }

    public static void GuardarProgreso(string nombreEscena)
    {
        PlayerPrefs.SetInt(SAVE_KEY, 1);
        PlayerPrefs.SetString(SCENE_KEY, nombreEscena);
        PlayerPrefs.Save();
        Debug.Log("Progreso guardado en: " + nombreEscena);
    }

    public static void BorrarProgreso()
    {
        PlayerPrefs.SetInt(SAVE_KEY, 0);
        PlayerPrefs.DeleteKey(SCENE_KEY);
        PlayerPrefs.Save();
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskListUI : MonoBehaviour
{
    public static TaskListUI Instance;

    [System.Serializable]
    public class TaskDefinition
    {
        public string id;
        public string text;
    }

    [Header("Tareas (las define el builder)")]
    public List<TaskDefinition> tasks = new List<TaskDefinition>();

    private readonly Dictionary<string, int> index = new Dictionary<string, int>();
    private readonly List<TMP_Text> rows = new List<TMP_Text>();
    private readonly List<bool> done = new List<bool>();
    private GameObject panel;
    private bool built;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Build();
    }

    private void Build()
    {
        if (built)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas();

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        panel = new GameObject("TaskList_Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 1f);
        prt.anchorMax = new Vector2(0f, 1f);
        prt.pivot = new Vector2(0f, 1f);
        prt.anchoredPosition = new Vector2(24f, -24f);
        prt.sizeDelta = new Vector2(400f, 44f + tasks.Count * 26f);

        TMP_Text title = MakeText(panel, "Task_Title", font, "OBJETIVOS", 20, new Color(1f, 0.8f, 0.3f));
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -14f);
        title.rectTransform.sizeDelta = new Vector2(0f, 26f);

        for (int i = 0; i < tasks.Count; i++)
        {
            index[tasks[i].id] = i;
            done.Add(false);

            TMP_Text row = MakeText(panel, "Task_" + tasks[i].id, font, "", 16, Color.white);
            RectTransform rt = row.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -42f - i * 26f);
            rt.sizeDelta = new Vector2(-16f, 22f);
            row.alignment = TextAlignmentOptions.TopLeft;
            row.enableWordWrapping = true;
            row.raycastTarget = false;
            rows.Add(row);
            Refresh(i);
        }

        built = true;
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("TaskListUI_Canvas", typeof(Canvas));
        go.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private TMP_Text MakeText(GameObject parent, string name, TMP_FontAsset font, string content, float size, Color color)
    {
        if (parent == null)
        {
            Debug.LogError("[TaskListUI] No se encontró el contenedor padre de la lista de tareas.");
            return null;
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);

        TMP_Text txt = go.GetComponent<TMP_Text>();
        if (txt == null)
        {
            Debug.LogError("[TaskListUI] No se pudo crear el texto '" + name + "'.");
            return null;
        }

        if (font == null)
            font = TMP_Settings.defaultFontAsset;
        if (font == null)
            Debug.LogError("[TaskListUI] No se encontró ninguna fuente TMP (TMP_Settings.defaultFontAsset y LiberationSans SDF). El texto usará la fuente por defecto de TMP.");

        txt.font = font;
        txt.fontSize = size;
        txt.color = color;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    public void CompleteTask(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        int i;
        if (!index.TryGetValue(id, out i) || done[i])
            return;

        done[i] = true;
        Refresh(i);
    }

    private void Refresh(int i)
    {
        if (rows.Count <= i)
            return;

        TMP_Text row = rows[i];
        string marker = done[i] ? "[X]" : "[ ]";
        row.text = (done[i] ? "<s>" : "") + marker + " " + tasks[i].text + (done[i] ? "</s>" : "");
        row.color = done[i] ? new Color(0.55f, 0.55f, 0.55f, 0.65f) : Color.white;
    }
}

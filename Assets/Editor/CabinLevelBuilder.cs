using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// NO EJECUTAR: generador historico de la escena Cabin_Level1.
/// Reconstruye la escena completa desde cero (suelo, paredes, techo, muebles,
/// puertas, llave, materiales de Assets/Materials/Generated, luces, arboles...)
/// y SOBRESCRIBIRIA los ajustes manuales actuales (posicion de la llave,
/// pasada visual, cableado de UI, entre otros).
/// Se conserva como referencia historica. Usar CabinLevelValidator para validar.
/// </summary>
public static class CabinLevelBuilder
{
    private const string ScenePath = "Assets/Scenes/Cabin_Level1.unity";
    private const string PrefabsDir = "Assets/Cabin/Prefabs/";
    private const string MatDir = "Assets/Cabin/Models/Materials/";
    private const string HorrorAudioDir = "Assets/Horror Elements/";
    private const string FreeHorrorAudioDir = "Assets/free horror ambience 2/";
    private const string GenMatDir = "Assets/Materials/Generated/";

    private static readonly string[] TreePaths =
    {
        "Assets/Forest Pack/Trees/small pine/5K poly/small_pine_05K_poly_1005960000.fbx",
        "Assets/Forest Pack/Trees/small pine/5K poly/small_pine_05K_poly_1006030309.fbx",
        "Assets/Forest Pack/Trees/small pine/5K poly/small_pine_05K_poly_1138873974.fbx",
        "Assets/Forest Pack/Trees/small pine/5K poly/small_pine_05K_poly_1005979599.fbx",
        "Assets/Forest Pack/Trees/small pine/5K poly/small_pine_05K_poly_1138338027.fbx",
        "Assets/Forest Pack/Trees/small pine/5K poly/small_pine_05K_poly_1006065537.fbx",
        "Assets/Forest Pack/Trees/small pine/5K poly/small_pine_05K_poly_1138137634.fbx"
    };

    private static readonly Vector3[] TreePositions =
    {
        new Vector3(-13f, 0f, -17.5f), new Vector3(-9f, 0f, -20.5f), new Vector3(-4.5f, 0f, -23.5f),
        new Vector3(3f, 0f, -24.5f), new Vector3(7.5f, 0f, -21.5f), new Vector3(11.5f, 0f, -18.5f),
        new Vector3(13.5f, 0f, -15.5f)
    };

    private static readonly Color WarmLight = new Color(1f, 0.72f, 0.5f);
    private static readonly Color CrateColor = new Color(0.28f, 0.2f, 0.13f);
    private static readonly Color NoteColor = new Color(0.8f, 0.75f, 0.62f);

    private static Transform levelRoot;
    private static Bounds footprint;
    private static GameObject barrier;

    private static GameObject inspectPanel;
    private static TMP_Text inspectTitleText;
    private static TMP_Text inspectBodyText;
    private static PauseMenu pauseMenu;

    [MenuItem("Nocturia/Cabin_Level1/Build & Repair Level (LEGACY - NO USAR)")]
    public static void BuildLevelMenu()
    {
        if (!EditorUtility.DisplayDialog("CabinLevelBuilder (LEGACY)",
                "Esta herramienta es LEGACY y reconstruye la escena desde cero, SOBRESCRIBIENDO los ajustes manuales actuales.\n\n¿Ejecutar de todos modos?", "Ejecutar", "Cancelar"))
            return;
        RunBuild();
    }

    [MenuItem("Nocturia/Cabin_Level1/Build & Repair Level (LEGACY - NO USAR)", true)]
    public static bool ValidateBuildLevelMenu()
    {
        return false;
    }

    [MenuItem("Nocturia/Cabin_Level1/Remove generated objects (NOC_*) - LEGACY")]
    public static void CleanupMenu()
    {
        if (!EditorUtility.DisplayDialog("CabinLevelBuilder (LEGACY)",
                "Elimina todos los objetos NOC_* y materiales generados de la escena.\n\nEsta acción es destructiva y NO se revierte con Ctrl+Z al reabrir.\n\n¿Continuar?", "Eliminar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var level = GameObject.Find("NOC_Level");
        if (level != null) Undo.DestroyObjectImmediate(level);

        DeleteGeneratedMat("Crate");
        DeleteGeneratedMat("Note");
        DeleteGeneratedMat("Barrier");
        DeleteGeneratedMat("Lens");
        DeleteGeneratedMat("Lantern");
        DeleteGeneratedMat("Lupa");

        var tree = GameObject.Find("Tree");
        if (tree != null && !tree.activeSelf)
        {
            Undo.RecordObject(tree, "Restaurar Tree");
            tree.SetActive(true);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[CabinBuilder] Objetos NOC_* eliminados. Re-ejecuta 'Build & Repair Level' para regenerarlos.");
    }

    [MenuItem("Nocturia/Cabin_Level1/Remove generated objects (NOC_*) - LEGACY", true)]
    public static bool ValidateCleanupMenu()
    {
        return false;
    }

    public static void RunBuild()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Cabin Level Build");

        var cabinRoot = GameObject.Find("Cabin");
        if (cabinRoot == null)
        {
            Debug.LogError("[CabinBuilder] No se encontró el root 'Cabin'. Se aborta.");
            return;
        }

        levelRoot = GetOrCreate("NOC_Level", null).transform;
        footprint = ComputeFootprint();

        BuildRoof();
        BuildExterior();
        DisableFakeTree();
        BuildTrees();
        BuildHud();
        BuildUi();
        WirePlayer();
        BuildDoors();
        BuildKeyNook();
        BuildFurnitureAndNotes();
        BuildHideSpots();
        BuildFootsteps();
        BuildLights();
        BuildCandlesAndLantern();
        BuildBarrier();
        BuildLens();
        BuildEvents();
        WireExitTrigger();
        UpdateObjectives();
        ApplyGlobalSettings();

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[CabinBuilder] Build completado y escena guardada.");
        CabinLevelValidator.RunValidate();
    }

    private static Bounds ComputeFootprint()
    {
        var walls = GameObject.Find("Cabin_Walls");
        var renderers = walls != null ? walls.GetComponentsInChildren<MeshRenderer>(true) : null;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool any = false;

        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r == null || r.bounds.size.sqrMagnitude < 0.01f) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        if (!any)
            b = new Bounds(new Vector3(0f, 1.6f, 0f), new Vector3(28.4f, 3.5f, 22.4f));

        return b;
    }

    private static void BuildRoof()
    {
        float wallTop = footprint.max.y;
        GameObject roof = GetOrCreate("NOC_Roof", levelRoot);
        SetupCube(roof);
        SetMat(roof, LoadMaterialByGuid("21ba00c050a978c479ce3d7ac9fc0174", MatDir + "Cabin.mat"));
        roof.transform.position = new Vector3(footprint.center.x, wallTop + 0.125f, footprint.center.z);
        roof.transform.rotation = Quaternion.identity;
        roof.transform.localScale = new Vector3(footprint.size.x + 0.6f, 0.25f, footprint.size.z + 0.6f);
    }

    private static void BuildExterior()
    {
        GameObject ground = GetOrCreate("NOC_ExteriorGround", levelRoot);
        SetupCube(ground);
        var groundMat = LoadMaterialByGuid("2d63dc0551dc54b468531f9cd3f2e096", null);
        if (groundMat == null) groundMat = GetMat("NOC_Ground", new Color(0.18f, 0.15f, 0.12f), Color.black, false);
        SetMat(ground, groundMat);
        ground.transform.position = new Vector3(footprint.center.x, -0.05f, footprint.min.z - 7f);
        ground.transform.rotation = Quaternion.identity;
        ground.transform.localScale = new Vector3(footprint.size.x + 12f, 0.2f, 24f);
    }

    private static void DisableFakeTree()
    {
        var tree = GameObject.Find("Tree");
        if (tree != null && tree.activeSelf)
        {
            Undo.RecordObject(tree, "Desactivar árbol falso");
            tree.SetActive(false);
            Debug.Log("[CabinBuilder] 'Tree' (árbol falso) desactivado.");
        }
    }

    private static void BuildTrees()
    {
        int created = 0;

        for (int i = 0; i < TreePaths.Length; i++)
        {
            string name = "NOC_Tree_" + (i + 1).ToString("00");
            if (Find(name) != null) continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePaths[i]);
            if (prefab == null)
            {
                Debug.LogWarning("[CabinBuilder] No se pudo cargar el árbol: " + TreePaths[i]);
                continue;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (go == null)
                go = (GameObject)Object.Instantiate(prefab);

            if (go == null)
            {
                Debug.LogError("[CabinBuilder] No se pudo instanciar el árbol: " + TreePaths[i]);
                continue;
            }

            Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
            go.name = name;
            go.transform.SetParent(levelRoot, true);
            go.transform.rotation = Quaternion.Euler(0f, i * 47.3f, 0f);

            Bounds b = GetBounds(go);
            float h = Mathf.Max(0.01f, b.size.y);
            go.transform.localScale = go.transform.localScale * (5f / h);

            b = GetBounds(go);
            go.transform.position = new Vector3(TreePositions[i].x, -b.min.y, TreePositions[i].z);
            created++;
        }

        if (created > 0)
            Debug.Log("[CabinBuilder] Árboles creados: " + created);
    }

    private static void BuildHud()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("[CabinBuilder] Canvas no encontrado, HUD no reparado.");
            return;
        }

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas);
        Undo.RecordObject(scaler, "CanvasScaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var rt = canvas.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "Canvas rect");
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        FixHudText("ObjectiveText", new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(700f, 50f), 22, false);
        FixHudText("MessageText", new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(700f, 40f), 20);
        FixHudText("PromptText", new Vector2(0.5f, 0.35f), new Vector2(0f, 0f), new Vector2(600f, 40f), 20);
        FixHudSlider("StaminaBar", new Vector2(0f, 0f), new Vector2(35f, 35f), new Vector2(36f, 160f));
        EnsureTaskListUI(canvas);
    }

    private static void EnsureTaskListUI(GameObject canvas)
    {
        var tlu = canvas.GetComponent<TaskListUI>();
        if (tlu == null) tlu = Undo.AddComponent<TaskListUI>(canvas);
        Undo.RecordObject(tlu, "TaskListUI tareas");
        if (tlu.tasks == null) tlu.tasks = new List<TaskListUI.TaskDefinition>();
        if (tlu.tasks.Count == 0)
        {
            AddTask(tlu, "escape_cell", "Escapa de la celda");
            AddTask(tlu, "explore_house", "Investiga la cabaña");
            AddTask(tlu, "find_clue", "Encuentra una pista sobre la salida");
            AddTask(tlu, "find_key", "Encuentra la llave oxidada");
            AddTask(tlu, "open_exit", "Abre la salida secundaria");
        }
    }

    private static void AddTask(TaskListUI tlu, string id, string text)
    {
        var t = new TaskListUI.TaskDefinition();
        t.id = id;
        t.text = text;
        tlu.tasks.Add(t);
    }

    private static void FixHudText(string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, bool warnIfMissing = true)
    {
        var go = FindIncludingInactive(name);
        if (go == null)
        {
            if (warnIfMissing)
                Debug.LogWarning("[CabinBuilder] HUD '" + name + "' no encontrado.");
            return;
        }

        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "HUD " + name);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        var tmp = go.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            Undo.RecordObject(tmp, "HUD " + name);
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        var legacy = go.GetComponent<Text>();
        if (legacy != null)
        {
            Undo.RecordObject(legacy, "HUD " + name);
            legacy.fontSize = fontSize;
            legacy.alignment = TextAnchor.MiddleCenter;
            legacy.raycastTarget = false;
        }
    }

    private static void FixHudSlider(string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogWarning("[CabinBuilder] HUD '" + name + "' no encontrado.");
            return;
        }

        var slider = go.GetComponent<Slider>();
        if (slider == null) slider = Undo.AddComponent<Slider>(go);

        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "HUD " + name);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }

    private static void BuildUi()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("[CabinBuilder] Canvas no encontrado, UI de pausa/inspección/intro omitida.");
            return;
        }

        var font = LoadFont();

        // --- Panel de inspección ---
        var insp = GetOrCreateUi("NOC_InspectPanel", canvas.transform);
        SetRect(insp.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateUiImage(insp, new Color(0f, 0f, 0f, 0.78f));

        var inspTitle = GetOrCreateUi("NOC_InspectTitle", insp.transform);
        SetRect(inspTitle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(900f, 60f));
        CreateUiText(inspTitle, "Inspección", 34, TextAlignmentOptions.Center, font);

        var inspBody = GetOrCreateUi("NOC_InspectBody", insp.transform);
        SetRect(inspBody.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1000f, 300f));
        CreateUiText(inspBody, "", 26, TextAlignmentOptions.Center, font);

        var inspHint = GetOrCreateUi("NOC_InspectHint", insp.transform);
        SetRect(inspHint.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -280f), new Vector2(600f, 40f));
        CreateUiText(inspHint, "Pulsa E para cerrar", 18, TextAlignmentOptions.Center, font);

        insp.SetActive(false);
        inspectPanel = insp;
        inspectTitleText = inspTitle.GetComponent<TMP_Text>();
        inspectBodyText = inspBody.GetComponent<TMP_Text>();

        // --- Pausa ---
        var pause = GetOrCreateUi("NOC_PausePanel", canvas.transform);
        SetRect(pause.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateUiImage(pause, new Color(0f, 0f, 0f, 0.55f));

        var pauseTitle = GetOrCreateUi("NOC_PauseTitle", pause.transform);
        SetRect(pauseTitle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(500f, 80f));
        CreateUiText(pauseTitle, "PAUSA", 48, TextAlignmentOptions.Center, font);

        var continueBtn = GetOrCreateUi("NOC_PauseContinueButton", pause.transform);
        SetRect(continueBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(360f, 64f));
        CreateUiButton(continueBtn, "Continuar", new Color(0.22f, 0.22f, 0.26f), font);

        var menuBtn = GetOrCreateUi("NOC_PauseMenuButton", pause.transform);
        SetRect(menuBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(360f, 64f));
        CreateUiButton(menuBtn, "Menú principal", new Color(0.22f, 0.22f, 0.26f), font);

        var pm = EnsureComponent<PauseMenu>(pause);
        Undo.RecordObject(pm, "PauseMenu config");
        pm.pausePanel = pause;
        pm.continueButton = continueBtn.GetComponent<Button>();
        pm.menuButton = menuBtn.GetComponent<Button>();
        pm.mainMenuScene = "MainMenu";
        pm.currentScene = "Cabin_Level1";
        pm.enabled = false;
        pauseMenu = pm;

        // --- Intro de despertar ---
        var intro = GetOrCreateUi("NOC_IntroOverlay", canvas.transform);
        SetRect(intro.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateUiImage(intro, new Color(0f, 0f, 0f, 1f));

        var introText = GetOrCreateUi("NOC_IntroText", intro.transform);
        SetRect(introText.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1100f, 220f));
        CreateUiText(introText, "", 30, TextAlignmentOptions.Center, font);

        var introComp = EnsureComponent<IntroDormitorio>(intro);
        Undo.RecordObject(introComp, "Intro config");
        introComp.panel = intro.GetComponent<Image>();
        introComp.introText = introText.GetComponent<TMP_Text>();
        introComp.pauseMenu = pm;
        introComp.textoIntro = "Despiertas en la celda de la cabaña.\nSolo recuerdas el bosque...\ny algo que te miraba entre los árboles.";
    }

    private static TMP_FontAsset LoadFont()
    {
        string path = AssetDatabase.GUIDToAssetPath("8f586378b4e144a9851e7b34d9b748ee");
        if (!string.IsNullOrEmpty(path))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f != null) return f;
        }
        return TMP_Settings.defaultFontAsset;
    }

    private static GameObject GetOrCreateUi(string name, Transform parent)
    {
        var existing = Find(name);
        if (existing != null)
        {
            if (existing.transform.parent != parent)
                Undo.SetTransformParent(existing.transform, parent, "Reparent " + name);
            return existing;
        }

        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        Undo.RecordObject(rt, "Rect " + rt.name);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static Image CreateUiImage(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(go);
        Undo.RecordObject(img, "Image " + go.name);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TMP_Text CreateUiText(GameObject go, string content, float fontSize, TextAlignmentOptions align, TMP_FontAsset font)
    {
        var tmp = go.GetComponent<TMP_Text>();
        if (tmp == null) tmp = Undo.AddComponent<TextMeshProUGUI>(go);
        Undo.RecordObject(tmp, "TMP " + go.name);
        if (font != null) tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.text = content;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static void CreateUiButton(GameObject go, string label, Color bgColor, TMP_FontAsset font)
    {
        var img = CreateUiImage(go, bgColor);
        var btn = go.GetComponent<Button>();
        if (btn == null) btn = Undo.AddComponent<Button>(go);
        Undo.RecordObject(btn, "Button " + go.name);
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock
        {
            normalColor = bgColor,
            highlightedColor = bgColor * 1.25f,
            pressedColor = bgColor * 0.8f,
            selectedColor = bgColor,
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        var textGo = GetOrCreateUi("Text", go.transform);
        SetRect(textGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var tmp = CreateUiText(textGo, label, 24, TextAlignmentOptions.Center, font);
        tmp.raycastTarget = false;
    }

    private static void WirePlayer()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[CabinBuilder] Player no encontrado.");
            return;
        }

        if (player.tag != "Player")
        {
            Undo.RecordObject(player, "Tag Player");
            player.tag = "Player";
        }

        EnsureComponent<CharacterController>(player);
        EnsureComponent<PlayerController>(player);
        EnsureComponent<Interactor>(player);
        EnsureComponent<SimpleInventory>(player);

        var camChild = player.transform.Find("Main Camera");
        var cam = camChild != null ? camChild.GetComponent<Camera>() : Camera.main;
        if (cam == null) return;

        var pc = player.GetComponent<PlayerController>();
        Undo.RecordObject(pc, "Player refs");
        pc.playerCamera = cam.transform;

        var bar = GameObject.Find("StaminaBar");
        if (bar != null)
        {
            var slider = bar.GetComponent<Slider>();
            if (slider == null) slider = Undo.AddComponent<Slider>(bar);
            pc.staminaSlider = slider;
        }

        var it = player.GetComponent<Interactor>();
        Undo.RecordObject(it, "Interactor refs");
        it.cameraTransform = cam.transform;

        var pt = GameObject.Find("PromptText");
        if (pt != null) it.promptText = pt.GetComponent<TMP_Text>();
    }

    private static void BuildDoors()
    {
        // La puerta de la celda ya no pide llave: el recorrido empieza explorando la cabaña.
        FixDoor("Door_Cell_Pivot", "Door_Cell_Model", "", "", 90f, "");

        FixDoor("Door_CabinExit_Pivot", "Door_CabinExit_Model", "cabin_key",
                "La puerta está cerrada con llave — usa la llave de la cabaña", 90f,
                "Camina hacia el bosque y huye de la cabaña de Noc");
    }

    private static void FixDoor(string pivotName, string modelName, string requiredId, string lockedMsg, float openAngle, string objectiveAfterOpen)
    {
        var pivot = GameObject.Find(pivotName);
        var model = FindTrimmed(modelName);
        if (pivot == null || model == null)
        {
            Debug.LogError("[CabinBuilder] Puerta incompleta: " + pivotName + " / " + modelName);
            return;
        }

        if (model.transform.parent != pivot.transform)
            Undo.SetTransformParent(model.transform, pivot.transform, "Reparent " + modelName);

        Undo.RecordObject(pivot.transform, "Door pivot " + pivotName);
        Undo.RecordObject(model.transform, "Door model " + modelName);
        pivot.transform.rotation = Quaternion.identity;
        pivot.transform.localScale = Vector3.one;

        Vector3 w = model.transform.position;
        float half = Mathf.Max(0.01f, model.transform.lossyScale.x) * 0.5f;
        pivot.transform.position = new Vector3(w.x - half, 1.5f, w.z);
        model.transform.localPosition = new Vector3(half, w.y - 1.5f, 0f);
        model.transform.localRotation = Quaternion.identity;

        var door = EnsureComponent<DoorInteractable>(pivot);
        Undo.RecordObject(door, "Door settings " + pivotName);
        door.requiredItemId = requiredId;
        door.lockedMessage = lockedMsg;
        door.openAngle = openAngle;
        door.objectiveAfterOpen = objectiveAfterOpen;
    }

    private static void BuildKeyNook()
    {
        GameObject cabinet = GetOrCreate("NOC_Cabinet_Key", levelRoot);
        SetupCube(cabinet);
        SetMat(cabinet, GetMat("Crate", CrateColor));
        cabinet.transform.position = new Vector3(6.3f, 0.525f, -8.3f);
        cabinet.transform.rotation = Quaternion.identity;
        cabinet.transform.localScale = new Vector3(0.7f, 1.05f, 0.45f);

        var crate = GameObject.Find("NOC_Crate_Key");
        if (crate != null)
        {
            Undo.RecordObject(crate, "Mesita de la celda");
            crate.name = "NOC_Table_Cell";
        }

        GameObject table = GetOrCreate("NOC_Table_Cell", levelRoot);
        SetupCube(table);
        SetMat(table, GetMat("Crate", CrateColor));
        table.transform.position = new Vector3(-12.8f, 0.325f, -10.1f);
        table.transform.rotation = Quaternion.identity;
        table.transform.localScale = new Vector3(0.9f, 0.65f, 0.6f);

        var key = GameObject.Find("Key_Cabin");
        if (key == null)
        {
            Debug.LogError("[CabinBuilder] Key_Cabin no encontrado.");
            return;
        }

        SetMat(key, GetMat("Key_Gold", new Color(0.9f, 0.72f, 0.32f), new Color(1f, 0.72f, 0.28f), false));

        var pi = EnsureComponent<PickupItem>(key);
        Undo.RecordObject(pi, "Key settings");
        pi.itemId = "cabin_key";
        pi.itemName = "Llave oxidada";
        pi.pickupMessage = "Has encontrado la Llave oxidada dentro del armario.";
        pi.objectiveAfterPickup = "Regresa a la salida secundaria (la puerta de la celda)";
        pi.deactivateOnPickup = null;

        var capsule = key.GetComponent<CapsuleCollider>();
        if (capsule != null) Undo.DestroyObjectImmediate(capsule);
        var box = key.GetComponent<BoxCollider>();
        if (box == null) box = key.gameObject.AddComponent<BoxCollider>();

        Undo.RecordObject(key.transform, "Key position");
        Bounds cb = GetBounds(cabinet);
        key.transform.position = new Vector3(6.3f, cb.max.y + 0.06f, -8.3f);
        key.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        key.transform.localScale = new Vector3(0.25f, 0.08f, 0.6f);

        var keyLight = key.transform.Find("NOC_KeyLight");
        if (keyLight == null)
        {
            var lg = new GameObject("NOC_KeyLight");
            Undo.RegisterCreatedObjectUndo(lg, "Key light");
            lg.transform.SetParent(key.transform);
            lg.transform.localPosition = Vector3.zero;
            keyLight = lg.transform;
        }
        var light = keyLight.GetComponent<Light>();
        if (light == null) light = keyLight.gameObject.AddComponent<Light>();
        Undo.RecordObject(light, "Key light");
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.6f);
        light.intensity = 0.5f;
        light.range = 2.5f;
        light.shadows = LightShadows.None;
    }

    private static void BuildFurnitureAndNotes()
    {
        var table = EnsurePrefabInstance("NOC_Table_Kitchen", PrefabsDir + "Table.prefab",
                                         new Vector3(9f, 0.1f, -7.6f), Quaternion.Euler(0f, 90f, 0f));
        if (table != null)
        {
            Bounds tb = GetBounds(table);
            var note = GetOrCreate("NOC_Note_Kitchen", levelRoot);
            SetupCube(note);
            SetMat(note, GetMat("Note", NoteColor));
            note.transform.position = new Vector3(tb.center.x, tb.max.y + 0.03f, tb.center.z);
            note.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            note.transform.localScale = new Vector3(0.34f, 0.42f, 0.02f);

            var rn = EnsureComponent<ReadableNote>(note);
            Undo.RecordObject(rn, "Note kitchen");
            rn.noteTitle = "Nota arrugada";
            rn.noteText = "Te lo dije... las llaves no cuelgan de las paredes.";
            rn.objectiveAfterRead = "";
        }

        EnsurePrefabInstance("NOC_Bed_Bedroom", PrefabsDir + "Bed.prefab",
                             new Vector3(11.6f, 0.1f, -2.9f), Quaternion.Euler(0f, 90f, 0f));

        var bedside = GetOrCreate("NOC_Crate_Bedside", levelRoot);
        SetupCube(bedside);
        SetMat(bedside, GetMat("Crate", CrateColor));
        bedside.transform.position = new Vector3(10.3f, 0.25f, -2.9f);
        bedside.transform.rotation = Quaternion.identity;
        bedside.transform.localScale = new Vector3(0.45f, 0.5f, 0.35f);

        Bounds bb = GetBounds(bedside);
        var note2 = GetOrCreate("NOC_Note_Bedroom", levelRoot);
        SetupCube(note2);
        SetMat(note2, GetMat("Note", NoteColor));
        note2.transform.position = new Vector3(bb.center.x, bb.max.y + 0.03f, bb.center.z);
        note2.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
        note2.transform.localScale = new Vector3(0.34f, 0.42f, 0.02f);

        var rn2 = EnsureComponent<ReadableNote>(note2);
        Undo.RecordObject(rn2, "Note bedroom");
        rn2.noteTitle = "Carta";
        rn2.noteText = "Espero que la oscuridad te abrace, hermano.";
        rn2.objectiveAfterRead = "";

        // Sillas de la cocina.
        EnsurePrefabInstance("NOC_Chair_Kitchen_A", PrefabsDir + "Stool.prefab",
                             new Vector3(8.2f, 0.1f, -7.6f), Quaternion.Euler(0f, 90f, 0f));
        EnsurePrefabInstance("NOC_Chair_Kitchen_B", PrefabsDir + "Stool.prefab",
                             new Vector3(9.8f, 0.1f, -7.6f), Quaternion.Euler(0f, 270f, 0f));

        // Ropero del dormitorio (escondite).
        var wardrobe = GetOrCreate("NOC_Wardrobe_Bedroom", levelRoot);
        SetupCube(wardrobe);
        SetMat(wardrobe, GetMat("Crate", new Color(0.2f, 0.15f, 0.11f)));
        wardrobe.transform.position = new Vector3(12.9f, 1f, -2.6f);
        wardrobe.transform.rotation = Quaternion.identity;
        wardrobe.transform.localScale = new Vector3(0.9f, 2f, 0.5f);

        // Lupa sobre la mesita de noche (necesaria para leer la pinza).
        var lupa = GetOrCreate("NOC_Item_Lupa", levelRoot);
        SetupCube(lupa);
        SetMat(lupa, GetMat("Lupa", new Color(0.85f, 0.95f, 1f), new Color(0.4f, 0.7f, 0.9f), false));
        Bounds lb = GetBounds(bedside);
        lupa.transform.position = new Vector3(lb.center.x + 0.22f, lb.max.y + 0.03f, lb.center.z - 0.1f);
        lupa.transform.rotation = Quaternion.identity;
        lupa.transform.localScale = new Vector3(0.35f, 0.05f, 0.12f);
        var lpi = EnsureComponent<PickupItem>(lupa);
        Undo.RecordObject(lpi, "Lupa settings");
        lpi.itemId = "lupa";
        lpi.itemName = "Lupa";
        lpi.pickupMessage = "Has encontrado la Lupa.";
        lpi.objectiveAfterPickup = "Examina la pinza de la celda para leer la inscripción";
        lpi.deactivateOnPickup = null;

        // Diario junto a la cama (pista de la llave).
        var diario = GetOrCreate("NOC_Item_Diario", levelRoot);
        SetupCube(diario);
        SetMat(diario, GetMat("Note", NoteColor));
        diario.transform.position = new Vector3(10.9f, 0.05f, -2.95f);
        diario.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
        diario.transform.localScale = new Vector3(0.5f, 0.05f, 0.7f);
        var dpi = EnsureComponent<PickupItem>(diario);
        Undo.RecordObject(dpi, "Diario settings");
        dpi.itemId = "diario";
        dpi.itemName = "Diario de la abuela";
        dpi.pickupMessage = "«Mamá decía que las llaves estaban donde nadie mira: en el armario, en la balda alta.»";
        dpi.objectiveAfterPickup = "La llave está dentro del armario de la cocina";
        dpi.deactivateOnPickup = null;

        // Pinza de ropa sobre la mesita de la celda (se examina con la lupa).
        var pinza = GetOrCreate("NOC_Item_Pinza", levelRoot);
        SetupCube(pinza);
        SetMat(pinza, GetMat("Crate", new Color(0.45f, 0.25f, 0.18f)));
        var tableCell = Find("NOC_Table_Cell");
        if (tableCell != null)
        {
            Bounds tcb = GetBounds(tableCell);
            pinza.transform.position = new Vector3(tcb.center.x, tcb.max.y + 0.03f, tcb.center.z);
        }
        pinza.transform.rotation = Quaternion.identity;
        pinza.transform.localScale = new Vector3(0.24f, 0.05f, 0.08f);
        var pio = EnsureComponent<InspectableObject>(pinza);
        Undo.RecordObject(pio, "Pinza settings");
        pio.objectName = "Pinza de ropa";
        pio.inspectText = "Con la lupa se lee la inscripción grabada en la madera:\n«La llave vive en el armario de la cocina.»";
        pio.requiredItemId = "lupa";
        pio.blockedMessage = "La inscripción es diminuta. Necesitas la lupa para leerla.";
        pio.objectiveAfterInspect = "La llave está dentro del armario de la cocina";
        pio.inspectPanel = inspectPanel;
        pio.titleText = inspectTitleText;
        pio.bodyText = inspectBodyText;
    }

    private static void BuildHideSpots()
    {
        SetupHideSpot("NOC_Bed_Bedroom", "HidePoint_Bed", new Vector3(0.9f, 0.05f, 0f),
                      "ExitPoint_Bed", new Vector3(10.1f, 0.1f, -2.3f));
        SetupHideSpot("NOC_Wardrobe_Bedroom", "HidePoint_Wardrobe", new Vector3(-0.6f, -0.7f, 0f),
                      "ExitPoint_Wardrobe", new Vector3(12.9f, 0.1f, -1.7f));
    }

    private static void SetupHideSpot(string ownerName, string hidePointName, Vector3 localOffset,
                                      string exitPointName, Vector3 exitWorldPos)
    {
        var owner = Find(ownerName);
        if (owner == null)
        {
            Debug.LogWarning("[CabinBuilder] Escondite sin dueño: " + ownerName);
            return;
        }

        var hs = EnsureComponent<HideSpot>(owner);

        var hp = owner.transform.Find(hidePointName);
        if (hp == null)
        {
            var go = new GameObject(hidePointName);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + hidePointName);
            go.transform.SetParent(owner.transform, false);
            go.transform.localPosition = localOffset;
            hp = go.transform;
        }

        var ep = Find(exitPointName);
        if (ep == null)
        {
            ep = new GameObject(exitPointName);
            Undo.RegisterCreatedObjectUndo(ep, "Crear " + exitPointName);
            ep.transform.SetParent(levelRoot, false);
        }
        Undo.RecordObject(ep.transform, "ExitPoint " + exitPointName);
        ep.transform.position = exitWorldPos;
        ep.transform.rotation = Quaternion.identity;

        Undo.RecordObject(hs, "HideSpot " + ownerName);
        hs.hidePoint = hp;
        hs.exitPoint = ep.transform;
    }

    private static void BuildFootsteps()
    {
        var player = GameObject.Find("Player");
        if (player == null) return;

        var src = player.GetComponent<AudioSource>();
        if (src == null) src = Undo.AddComponent<AudioSource>(player);

        var fs = EnsureComponent<PlayerFootsteps>(player);
        Undo.RecordObject(fs, "Footsteps");
        fs.player = player.GetComponent<PlayerController>();
        fs.source = src;
        fs.stepClip = LoadAudio(new[] { FreeHorrorAudioDir + "ha-crunchy_1.wav" });

        Undo.RecordObject(src, "Footsteps audio");
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.minDistance = 1f;
        src.maxDistance = 10f;
        src.volume = 0.5f;
    }

    private static void BuildLights()
    {
        RoomLight("NOC_Light_Cell", new Vector3(-13f, 2.65f, -10.2f), WarmLight, 0.9f, 8f, true, true, 5f);
        RoomLight("NOC_Light_Artifact", new Vector3(-8.5f, 2.65f, 0.2f), WarmLight, 0.8f, 9f, true, true, 6f);
        RoomLight("NOC_Light_Corridor", new Vector3(0.4f, 2.65f, -7.7f), new Color(1f, 0.85f, 0.7f), 0.75f, 6f, true, true, 6f);
        RoomLight("NOC_Light_Bedroom", new Vector3(9f, 2.65f, -2.8f), WarmLight, 0.8f, 8f, true, true, 6f);
        RoomLight("NOC_Light_Kitchen", new Vector3(9f, 2.65f, -8.6f), new Color(1f, 0.75f, 0.55f), 0.7f, 8f, true, true, 6f);
        RoomLight("NOC_Light_Exit", new Vector3(0.6f, 2.5f, -10f), new Color(1f, 0.8f, 0.6f), 0.9f, 6f, false, false, 0f);

        var sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            var sunLight = sun.GetComponent<Light>();
            if (sunLight != null)
            {
                Undo.RecordObject(sun.transform, "Sun");
                Undo.RecordObject(sunLight, "Sun");
                sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
                sunLight.color = new Color(0.5f, 0.62f, 1f);
                sunLight.intensity = 0.2f;
                sunLight.shadows = LightShadows.Soft;
            }
        }
    }

    private static void RoomLight(string name, Vector3 pos, Color color, float intensity, float range, bool shadows, bool flicker, float velocidadParpadeo)
    {
        var go = GetOrCreate(name, levelRoot);
        var light = go.GetComponent<Light>();
        if (light == null) light = go.AddComponent<Light>();

        Undo.RecordObject(go.transform, name);
        Undo.RecordObject(light, name);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;

        var flick = go.GetComponent<LightFlicker>();
        if (flicker)
        {
            if (flick == null) flick = go.AddComponent<LightFlicker>();
            flick.velocidad = velocidadParpadeo;
            flick.factorMin = 0.55f;
            flick.factorMax = 1f;
        }
        else if (flick != null)
        {
            Undo.DestroyObjectImmediate(flick);
        }
    }

    private static void BuildCandlesAndLantern()
    {
        var cellTable = GameObject.Find("Table");
        if (cellTable != null)
            CreateCandle("NOC_Candle_Cell", GetBounds(cellTable).max.y, new Vector3(-13f, 0f, -5.4f));

        var kitchenTable = Find("NOC_Table_Kitchen");
        if (kitchenTable != null)
        {
            Bounds tb = GetBounds(kitchenTable);
            CreateCandle("NOC_Candle_Kitchen", tb.max.y, tb.center + new Vector3(0.25f, 0f, -0.12f));
        }

        var bedside = Find("NOC_Crate_Bedside");
        if (bedside != null)
        {
            Bounds bb = GetBounds(bedside);
            CreateCandle("NOC_Candle_Bedside", bb.max.y, bb.center + new Vector3(0.22f, 0f, 0.08f));
        }

        // Segunda vela del dormitorio, en el suelo junto a la cama.
        CreateCandle("NOC_Candle_Bedroom", 0f, new Vector3(12.4f, 0f, -2.2f));

        var pedestal = GetOrCreate("NOC_Pedestal_Candle", levelRoot);
        SetupCube(pedestal);
        SetMat(pedestal, GetMat("Crate", CrateColor));
        pedestal.transform.position = new Vector3(-9f, 0.25f, 0.6f);
        pedestal.transform.rotation = Quaternion.identity;
        pedestal.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Bounds pb = GetBounds(pedestal);
        CreateCandle("NOC_Candle_Artifact", pb.max.y, pb.center);

        var shelf = GetOrCreate("NOC_Shelf_Lantern", levelRoot);
        SetupCube(shelf);
        SetMat(shelf, GetMat("Crate", CrateColor));
        shelf.transform.position = new Vector3(-6.6f, 0.25f, -1f);
        shelf.transform.rotation = Quaternion.identity;
        shelf.transform.localScale = new Vector3(0.7f, 0.5f, 0.4f);
        float shelfTop = GetBounds(shelf).max.y;

        var lantern = GetOrCreate("NOC_Lantern_Artifact", levelRoot);
        SetupCube(lantern);
        SetMat(lantern, GetMat("Lantern", new Color(0.85f, 0.7f, 0.35f)));
        lantern.transform.position = new Vector3(-6.6f, shelfTop + 0.2f, -1f);
        lantern.transform.rotation = Quaternion.identity;
        lantern.transform.localScale = new Vector3(0.22f, 0.4f, 0.22f);

        var light = lantern.GetComponent<Light>();
        if (light == null) light = lantern.AddComponent<Light>();
        Undo.RecordObject(light, "Lantern light");
        light.type = LightType.Point;
        light.color = new Color(1f, 0.7f, 0.4f);
        light.intensity = 1f;
        light.range = 5f;
        light.shadows = LightShadows.None;

        AddLightFlicker(lantern, 8f, 0.6f, 1.1f);
    }

    private static void CreateCandle(string name, float surfaceY, Vector3 groundPos)
    {
        var go = GetOrCreate(name, levelRoot);
        SetupCube(go);
        SetMat(go, GetMat("Crate", new Color(0.3f, 0.22f, 0.15f)));
        go.transform.position = new Vector3(groundPos.x, surfaceY + 0.14f, groundPos.z);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = new Vector3(0.1f, 0.28f, 0.1f);

        var light = go.GetComponent<Light>();
        if (light == null) light = go.AddComponent<Light>();
        Undo.RecordObject(light, "Candle light " + name);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.25f);
        light.intensity = 0.9f;
        light.range = 5f;
        light.shadows = LightShadows.None;

        AddLightFlicker(go, 12f, 0.5f, 1.15f);
    }

    private static void AddLightFlicker(GameObject go, float speed, float minFactor, float maxFactor)
    {
        var fl = EnsureComponent<LightFlicker>(go);
        fl.velocidad = speed;
        fl.factorMin = minFactor;
        fl.factorMax = maxFactor;
    }

    private static void BuildBarrier()
    {
        barrier = GetOrCreate("NOC_Energy_Barrier", levelRoot);
        SetupCube(barrier);
        SetMat(barrier, GetMat("Barrier", new Color(0.2f, 0.85f, 1f, 0.35f), new Color(0.15f, 0.6f, 0.9f), true));
        barrier.transform.position = new Vector3(3.92f, 1.3f, -2.7f);
        barrier.transform.rotation = Quaternion.identity;
        barrier.transform.localScale = new Vector3(0.2f, 2.6f, 3.5f);

        var bc = barrier.GetComponent<BoxCollider>();
        if (bc != null)
        {
            Undo.RecordObject(bc, "Barrier collider");
            bc.isTrigger = false;
        }

        var light = barrier.GetComponent<Light>();
        if (light == null) light = barrier.AddComponent<Light>();
        Undo.RecordObject(light, "Barrier light");
        light.type = LightType.Point;
        light.color = new Color(0.25f, 0.8f, 1f);
        light.intensity = 1f;
        light.range = 7f;
        light.shadows = LightShadows.None;

        AddLightFlicker(barrier, 6f, 0.75f, 1.05f);
    }

    private static void BuildLens()
    {
        var pedestal = GetOrCreate("NOC_Pedestal_Lens", levelRoot);
        SetupCube(pedestal);
        SetMat(pedestal, GetMat("Crate", CrateColor));
        pedestal.transform.position = new Vector3(-11.8f, 0.325f, -0.8f);
        pedestal.transform.rotation = Quaternion.identity;
        pedestal.transform.localScale = new Vector3(0.55f, 0.65f, 0.55f);

        var lens = GetOrCreate("NOC_Lens_Crystal", levelRoot);
        SetupCube(lens);
        SetMat(lens, GetMat("Lens", new Color(0.3f, 0.85f, 1f), new Color(0.2f, 0.7f, 1f), false));
        Bounds pb = GetBounds(pedestal);
        lens.transform.position = new Vector3(pb.center.x, pb.max.y + 0.04f, pb.center.z);
        lens.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        lens.transform.localScale = new Vector3(0.55f, 0.22f, 0.4f);

        var pi = EnsureComponent<PickupItem>(lens);
        Undo.RecordObject(pi, "Lens settings");
        pi.itemId = "lens_crystal";
        pi.itemName = "Lente de cristal";
        pi.pickupMessage = "Has recogido la Lente de cristal.";
        pi.objectiveAfterPickup = "El pasillo al norte está bloqueado por una barrera de energía — regresa al pasillo central";
        pi.deactivateOnPickup = barrier != null ? new GameObject[] { barrier } : new GameObject[0];

        var light = lens.GetComponent<Light>();
        if (light == null) light = lens.AddComponent<Light>();
        Undo.RecordObject(light, "Lens light");
        light.type = LightType.Point;
        light.color = new Color(0.35f, 0.7f, 1f);
        light.intensity = 1.2f;
        light.range = 4f;
        light.shadows = LightShadows.None;
    }

    private static void BuildEvents()
    {
        var cellLight = GameObject.Find("NOC_Light_Cell");
        var artifactLight = GameObject.Find("NOC_Light_Artifact");
        var corridorLight = GameObject.Find("NOC_Light_Corridor");
        var kitchenLight = GameObject.Find("NOC_Light_Kitchen");

        EventTrigger("NOC_Ev_CellDoor_Whisper", new Vector3(-8.35f, 1.5f, -5.9f), new Vector3(3.6f, 3f, 3.4f),
                     LoadAudio(new[] { HorrorAudioDir + "Misc/Misc_Whisper.wav", HorrorAudioDir + "Misc/Misc_Shh.wav" }),
                     0.4f, cellLight);

        EventTrigger("NOC_Ev_Artifact_Riser", new Vector3(-8.35f, 1.5f, -2.1f), new Vector3(3.6f, 3f, 3f),
                     LoadAudio(new[] { HorrorAudioDir + "Hits/Hit_Suspens.wav", HorrorAudioDir + "Ambient/Amb_scary.wav", HorrorAudioDir + "Ambient/Amb_Deep_impacts.wav" }),
                     0.3f, artifactLight);

        EventTrigger("NOC_Ev_Corridor_Bell", new Vector3(0.4f, 1.5f, -8.5f), new Vector3(4.6f, 3f, 4f),
                     LoadAudio(new[] { HorrorAudioDir + "Ambient/Amb_bell.wav" }),
                     0.35f, corridorLight);

        EventTrigger("NOC_Ev_Kitchen_Whisper", new Vector3(12.4f, 1.5f, -5.6f), new Vector3(3.6f, 3f, 2.6f),
                     LoadAudio(new[] { HorrorAudioDir + "Misc/Misc_Ghost.wav", HorrorAudioDir + "Misc/Misc_Whisper.wav" }),
                     0.45f, kitchenLight);

        var amb = GetOrCreate("NOC_Cabin_Ambience", levelRoot);
        var src = amb.GetComponent<AudioSource>();
        if (src == null) src = amb.AddComponent<AudioSource>();
        Undo.RecordObject(src, "Ambience");
        src.clip = LoadAudio(new[]
        {
            FreeHorrorAudioDir + "ha-undercurrent2.wav",
            FreeHorrorAudioDir + "ha-pressure-nofx.wav",
            FreeHorrorAudioDir + "ha-backrooms.wav"
        });
        src.loop = true;
        src.playOnAwake = true;
        src.spatialBlend = 0f;
        src.volume = 0.35f;
    }

    private static void EventTrigger(string name, Vector3 pos, Vector3 size, AudioClip clip, float volume, GameObject flickerLight)
    {
        var go = GetOrCreate(name, levelRoot);

        var col = go.GetComponent<BoxCollider>();
        if (col == null) col = go.AddComponent<BoxCollider>();
        Undo.RecordObject(col, name);
        col.isTrigger = true;

        Undo.RecordObject(go.transform, name);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = size;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null) Undo.DestroyObjectImmediate(renderer);

        var ev = EnsureComponent<AmbientHorrorEvent>(go);
        Undo.RecordObject(ev, name);
        ev.clip = clip;
        ev.volume = volume;
        ev.oneShot = true;
        ev.flickerLight = flickerLight != null ? flickerLight.GetComponent<Light>() : null;
        ev.flickerDuration = 2.5f;
        ev.flickerMinIntensity = 0.15f;
        ev.flickerMaxIntensity = 0.9f;
    }

    private static void WireExitTrigger()
    {
        var trig = GameObject.Find("Exit_To_Forest_Trigger");
        if (trig == null)
        {
            Debug.LogError("[CabinBuilder] Exit_To_Forest_Trigger no encontrado.");
            return;
        }

        var sc = EnsureComponent<SceneChangeTrigger>(trig);
        Undo.RecordObject(sc, "Exit trigger");
        sc.sceneName = "Forest_Level2";
        sc.requiredItemId = "cabin_key";
        sc.blockedMessage = "La puerta está cerrada con llave — usa la llave de la cabaña";

        foreach (var r in trig.GetComponentsInChildren<Renderer>(true))
        {
            if (r.enabled)
            {
                Undo.RecordObject(r, "Exit trigger renderer");
                r.enabled = false;
            }
        }
    }

    private static void UpdateObjectives()
    {
        var managers = GameObject.Find("Managers");
        if (managers == null)
        {
            Debug.LogWarning("[CabinBuilder] Managers no encontrado, objetivo inicial intacto.");
            return;
        }

        var om = managers.GetComponent<ObjectiveManager>();
        if (om == null)
        {
            Debug.LogWarning("[CabinBuilder] ObjectiveManager no encontrado en Managers.");
            return;
        }

        Undo.RecordObject(om, "Objective inicial");
        om.initialObjective = "Encuentra una forma de salir";
    }

    private static void ApplyGlobalSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.015f, 0.015f, 0.02f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.04f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.14f, 0.14f, 0.17f);
    }

    private static GameObject GetOrCreate(string name, Transform parent)
    {
        var existing = Find(name);
        if (existing != null)
        {
            if (existing.transform.parent != parent)
                Undo.SetTransformParent(existing.transform, parent, "Reparent " + name);
            return existing;
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
        if (parent != null)
        {
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }
        return go;
    }

    private static GameObject EnsurePrefabInstance(string name, string path, Vector3 pos, Quaternion rot)
    {
        var existing = Find(name);
        if (existing != null)
        {
            if (existing.transform.parent != levelRoot)
                Undo.SetTransformParent(existing.transform, levelRoot, "Reparent " + name);
            existing.transform.position = pos;
            existing.transform.rotation = rot;
            return existing;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("[CabinBuilder] Prefab no encontrado: " + path);
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (go == null) return null;

        Undo.RegisterCreatedObjectUndo(go, "Instanciar " + name);
        go.name = name;
        go.transform.SetParent(levelRoot, true);
        go.transform.position = pos;
        go.transform.rotation = rot;
        return go;
    }

    private static void SetupCube(GameObject go)
    {
        if (go.GetComponent<MeshFilter>() == null)
        {
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = AssetDatabase.GetBuiltinExtraResource<Mesh>("Cube.fbx");
        }
        if (go.GetComponent<MeshRenderer>() == null) go.AddComponent<MeshRenderer>();
        if (go.GetComponent<BoxCollider>() == null) go.AddComponent<BoxCollider>();
    }

    private static void SetMat(GameObject go, Material mat)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null || mat == null) return;
        Undo.RecordObject(mr, "Material " + go.name);
        mr.sharedMaterial = mat;
    }

    private static Material GetMat(string name, Color color)
    {
        return GetMat(name, color, Color.black, false);
    }

    private static Material GetMat(string name, Color color, Color emission, bool transparent)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(GenMatDir))
            AssetDatabase.CreateFolder("Assets/Materials", "Generated");

        string path = GenMatDir + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var mat = new Material(Shader.Find("Standard"));
        mat.name = name;

        if (transparent)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        mat.color = color;

        if (emission != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void DeleteGeneratedMat(string name)
    {
        string path = GenMatDir + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private static Material LoadMaterialByGuid(string guid, string fallbackPath)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (!string.IsNullOrEmpty(path))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
        }
        return AssetDatabase.LoadAssetAtPath<Material>(fallbackPath);
    }

    private static AudioClip LoadAudio(string[] candidates)
    {
        foreach (var p in candidates)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            if (c != null) return c;
        }
        return null;
    }

    private static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<MeshRenderer>(true);
        if (rs == null || rs.Length == 0)
            return new Bounds(go.transform.position + Vector3.up * 0.45f, new Vector3(0.9f, 0.9f, 0.9f));

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
        {
            if (rs[i] != null) b.Encapsulate(rs[i].bounds);
        }
        return b;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null) return c;
        return Undo.AddComponent<T>(go);
    }

    private static GameObject Find(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) return go;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var t = FindDeep(root.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private static GameObject FindTrimmed(string name)
    {
        var go = Find(name);
        if (go != null) return go;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var t = FindDeepTrimmed(root.transform, name);
            if (t != null)
            {
                Undo.RecordObject(t.gameObject, "Renombrar " + t.name);
                t.name = name;
                return t.gameObject;
            }
        }
        return null;
    }

    private static GameObject FindIncludingInactive(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) return go;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var t = FindDeepIncludingInactive(root.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private static Transform FindDeepIncludingInactive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var t = FindDeepIncludingInactive(child, name);
            if (t != null) return t;
        }
        return null;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var t = FindDeep(child, name);
            if (t != null) return t;
        }
        return null;
    }

    private static Transform FindDeepTrimmed(Transform parent, string name)
    {
        if (parent.name.Trim() == name) return parent;
        foreach (Transform child in parent)
        {
            var t = FindDeepTrimmed(child, name);
            if (t != null) return t;
        }
        return null;
    }
}

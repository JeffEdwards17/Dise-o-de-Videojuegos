using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CabinLevelValidator
{
    private static readonly List<string> Report = new List<string>();
    private static int fails;
    private static int passes;

    [MenuItem("Nocturia/Cabin_Level1/Validate Level (report)")]
    public static void ValidateMenu()
    {
        RunValidate();
    }

    public static void RunValidate()
    {
        fails = 0;
        passes = 0;
        Report.Clear();
        Report.Add("=== REPORTE DE VALIDACIÓN — Cabin_Level1 ===");
        Report.Add("Generado: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        Report.Add("");

        if (SceneManager.GetActiveScene().name != "Cabin_Level1")
        {
            Report.Add("  [FALLO] La escena activa no es Cabin_Level1. Ábrela primero y vuelve a validar.");
            Report.Add("");
            Report.Add("=== RESUMEN: 0 OK, 1 FALLOS ===");
            string early = string.Join("\n", Report);
            Debug.LogError("[CabinValidator]\n" + early);
            return;
        }

        // --- Build settings
        var scenes = EditorBuildSettings.scenes;
        Check("Build Settings: MainMenu en índice 0", scenes.Length > 0 && scenes[0].path.EndsWith("MainMenu.unity"));
        Check("Build Settings: Cabin_Level1 en índice 1", scenes.Length > 1 && scenes[1].path.EndsWith("Cabin_Level1.unity") && scenes[1].enabled);

        // --- Estructura base
        Check("Floor_Cabin existe", GameObject.Find("Floor_Cabin") != null);
        Check("Cabin_Walls existe", GameObject.Find("Cabin_Walls") != null);

        // --- Techo (validación estructural contra la huella de la cabaña)
        var roof = Find("NOC_Roof");
        Check("Techo NOC_Roof existe", roof != null);
        if (roof != null)
        {
            Bounds rb;
            bool hasBounds = TryGetBounds(roof, out rb);
            var structure = GetCabinStructureBounds();
            if (hasBounds)
            {
                Check("Techo cubre la huella de la cabaña",
                    rb.size.x >= structure.size.x - 0.5f && rb.size.z >= structure.size.z - 0.5f);
                Check("Techo centrado sobre la estructura",
                    Mathf.Abs(rb.center.x - structure.center.x) < 0.6f && Mathf.Abs(rb.center.z - structure.center.z) < 0.6f);

                Report.Add("  INFO: Huella del techo x[" + rb.min.x.ToString("0.0") + ", " + rb.max.x.ToString("0.0") +
                    "] z[" + rb.min.z.ToString("0.0") + ", " + rb.max.z.ToString("0.0") + "]");
                Report.Add("  INFO: Huella de la cabaña x[" + structure.min.x.ToString("0.0") + ", " + structure.max.x.ToString("0.0") +
                    "] z[" + structure.min.z.ToString("0.0") + ", " + structure.max.z.ToString("0.0") + "]");
            }
            else
            {
                Report.Add("  INFO: NOC_Roof sin renderers ni colliders; huella no calculable, se omite la validación de cobertura.");
            }

            float wallTop = GetWallTop();
            float roofBottom = hasBounds ? rb.min.y : roof.transform.position.y - roof.transform.localScale.y * 0.5f;
            Check("Techo asentado sobre las paredes (sin hueco)", Mathf.Abs(roofBottom - wallTop) < 0.5f);
        }

        // --- Exterior
        Check("Suelo exterior NOC_ExteriorGround existe", Find("NOC_ExteriorGround") != null);
        var fakeTree = GameObject.Find("Tree");
        Check("Árbol falso 'Tree' desactivado", fakeTree == null || !fakeTree.activeSelf);
        int trees = 0;
        for (int i = 1; i <= 7; i++)
            if (Find("NOC_Tree_" + i.ToString("00")) != null) trees++;
        Check("Árboles generados (" + trees + "/7)", trees >= 4);

        // --- Puertas
        var cellPivot = GameObject.Find("Door_Cell_Pivot");
        var cellDoor = cellPivot != null ? cellPivot.GetComponent<DoorInteractable>() : null;
        Check("Celda: puerta existe", cellPivot != null);
        Check("Celda: abierta sin llave (recorrido nuevo)", cellDoor != null && string.IsNullOrEmpty(cellDoor.requiredItemId));
        Check("Celda: pivote a 1.5m de altura", cellPivot != null && Mathf.Abs(cellPivot.transform.position.y - 1.5f) < 0.2f);

        var exitPivot = GameObject.Find("Door_CabinExit_Pivot");
        var exitModel = GameObject.Find("Door_CabinExit_Model");
        var exitDoor = exitPivot != null ? exitPivot.GetComponent<DoorInteractable>() : null;
        Check("Salida: pivote existe", exitPivot != null);
        Check("Salida: modelo existe", exitModel != null);
        Check("Salida: requiere cabin_key", exitDoor != null && exitDoor.requiredItemId == "cabin_key");
        Check("Salida: objetivo tras abrir definido", exitDoor != null && !string.IsNullOrEmpty(exitDoor.objectiveAfterOpen));
        if (exitPivot != null && exitModel != null)
        {
            float half = exitModel.transform.lossyScale.x * 0.5f;
            float hingeX = exitModel.transform.position.x - half;
            Check("Salida: bisagra en borde izquierdo", Mathf.Abs(exitPivot.transform.position.x - hingeX) < 0.05f);
            Check("Salida: en fachada sur (z ≈ -11.05)", Mathf.Abs(exitModel.transform.position.z - (-11.05f)) < 0.3f);
        }

        // --- Trigger de salida
        var trig = GameObject.Find("Exit_To_Forest_Trigger");
        var sc = trig != null ? trig.GetComponent<SceneChangeTrigger>() : null;
        Check("Trigger de salida existe", trig != null);
        Check("Trigger → Forest_Level2", sc != null && sc.sceneName == "Forest_Level2");
        Check("Trigger requiere cabin_key", sc != null && sc.requiredItemId == "cabin_key");
        if (trig != null)
        {
            bool invisible = true;
            foreach (Renderer r in trig.GetComponentsInChildren<Renderer>(true))
                if (r.enabled) invisible = false;
            Check("Trigger invisible (sin renderer)", invisible);
        }

        // --- Llave (sin coordenadas fijas: la posición la decide el diseñador)
        var key = GameObject.Find("Key_Cabin");
        var keyPickup = key != null ? key.GetComponent<PickupItem>() : null;
        Check("Llave existe", key != null);
        Check("Llave: PickupItem presente", keyPickup != null);
        Check("Llave: itemId cabin_key", keyPickup != null && keyPickup.itemId == "cabin_key");
        Check("Llave: nombre 'Llave oxidada'", keyPickup != null && keyPickup.itemName == "Llave oxidada");
        Check("Llave: objetivo tras recoger (regresa a la salida)", keyPickup != null && !string.IsNullOrEmpty(keyPickup.objectiveAfterPickup) && keyPickup.objectiveAfterPickup.StartsWith("Regresa a la salida"));
        if (key != null)
        {
            Check("Llave: sin CapsuleCollider gigante", key.GetComponent<CapsuleCollider>() == null);
            var bc = key.GetComponent<BoxCollider>();
            Check("Llave: con BoxCollider", bc != null);
            if (bc != null)
            {
                var bs = Vector3.Scale(bc.size, key.transform.lossyScale);
                Check("Llave: BoxCollider de tamaño razonable (< 0.75m)", bs.magnitude < 0.75f);
                Report.Add("  INFO: Llave BoxCollider isTrigger=" + bc.isTrigger + " (el pickup es por 'E', no por contacto)");
            }
            var keyLight = key.transform.Find("NOC_KeyLight");
            var kl = keyLight != null ? keyLight.GetComponent<Light>() : null;
            Check("Llave: luz dorada (NOC_KeyLight)", kl != null && kl.type == LightType.Point && kl.range < 4f);
            var mr = key.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
                Report.Add("  INFO: Material de la llave: " + mr.sharedMaterial.name + (mr.sharedMaterial.IsKeywordEnabled("_EMISSION") ? " (con emisión/glow)" : " (sin emisión)"));

            Report.Add("  INFO: Posición actual de la llave: " + key.transform.position.ToString("F2") + " (sin validar contra coordenadas)");
            Report.Add("  WARNING: Accesibilidad de la llave (fuera de paredes y alcanzable) no verificable estáticamente; compruébala en Play Mode.");
        }

        var cabinet = Find("NOC_Cabinet_Key");
        Check("Armario de la cocina existe", cabinet != null);
        if (cabinet != null)
            Check("Armario en la cocina (x>5, z≈-8.3)", cabinet.transform.position.x > 5f && Mathf.Abs(cabinet.transform.position.z - (-8.3f)) < 1f);
        Check("Caja de la celda reemplazada por la mesita", Find("NOC_Crate_Key") == null && Find("NOC_Table_Cell") != null);

        // --- Lente y barrera
        var lens = Find("NOC_Lens_Crystal");
        var lensPickup = lens != null ? lens.GetComponent<PickupItem>() : null;
        var barrier = Find("NOC_Energy_Barrier");
        Check("Lente existe en sala de artefactos", lens != null && lens.transform.position.x < -3f && lens.transform.position.x > -14.2f);
        Check("Lente: itemId lens_crystal", lensPickup != null && lensPickup.itemId == "lens_crystal");
        Check("Barrera de energía existe", barrier != null);
        if (barrier != null)
        {
            var bc = barrier.GetComponent<BoxCollider>();
            Check("Barrera: collider sólido (bloquea)", bc != null && !bc.isTrigger);
        }
        if (lensPickup != null && barrier != null)
        {
            bool wired = lensPickup.deactivateOnPickup != null && System.Array.IndexOf(lensPickup.deactivateOnPickup, barrier) >= 0;
            Check("Lente desactiva la barrera al recogerla", wired);
        }

        // --- Notas
        var notes = Object.FindObjectsByType<ReadableNote>(FindObjectsSortMode.None);
        Check("2 notas de historia", notes != null && notes.Length == 2);
        if (notes != null)
        {
            foreach (ReadableNote n in notes)
                Check("Nota '" + n.noteTitle + "' con texto", !string.IsNullOrEmpty(n.noteText));
        }

        // --- Velas / lámpara / luces
        int candleFlickers = 0;
        foreach (var f in Object.FindObjectsByType<LightFlicker>(FindObjectsSortMode.None))
            if (f != null && f.name.StartsWith("NOC_Candle_")) candleFlickers++;
        Check("5 velas con llama parpadeante (>=4)", candleFlickers >= 4);
        Check("Vela extra del dormitorio en el suelo", Find("NOC_Candle_Bedroom") != null);

        var lantern = Find("NOC_Lantern_Artifact");
        Check("Lámpara de aceite en sala de artefactos", lantern != null);
        var lanternLight = lantern != null ? lantern.GetComponent<Light>() : null;
        Check("Lámpara emite luz", lanternLight != null && lanternLight.type == LightType.Point);

        int points = 0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l != null && l.type == LightType.Point) points++;
        Check("Luces puntuales suficientes (>=6)", points >= 6);

        // --- HUD
        var canvas = GameObject.Find("Canvas");
        var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        Check("Canvas con CanvasScaler 1920x1080", scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize);
        var tlu = canvas != null ? canvas.GetComponent<TaskListUI>() : null;
        Check("TaskListUI presente en el Canvas", tlu != null);
        if (tlu != null)
        {
            Check("TaskListUI con tareas definidas (>=4)", tlu.tasks != null && tlu.tasks.Count >= 4);
            bool ids = tlu.tasks != null &&
                tlu.tasks.Exists(t => t.id == "escape_cell") &&
                tlu.tasks.Exists(t => t.id == "explore_house") &&
                tlu.tasks.Exists(t => t.id == "find_clue") &&
                tlu.tasks.Exists(t => t.id == "find_key") &&
                tlu.tasks.Exists(t => t.id == "open_exit");
            Check("TaskListUI con ids esperados", ids);
        }
        Check("MessageText visible bajo el objetivo", IsAnchoredAt("MessageText", new Vector2(0.5f, 1f)));
        Check("PromptText visible (centro-bajo)", IsAnchoredAt("PromptText", new Vector2(0.5f, 0.35f)));
        Check("StaminaBar visible abajo-izquierda", IsStaminaBottomLeft());
        var objText = Find("ObjectiveText");
        if (objText != null && objText.activeInHierarchy)
            Report.Add("  INFO: ObjectiveText legado sigue visible (opcional; TaskListUI es el HUD principal)");
        else
            Report.Add("  INFO: ObjectiveText legado inactivo o ausente (correcto: TaskListUI es el HUD principal)");

        // --- Player
        var player = GameObject.Find("Player");
        Check("Player existe", player != null);
        if (player != null)
        {
            Check("Player: tag 'Player'", player.tag == "Player");
            var pc = player.GetComponent<PlayerController>();
            Check("PlayerController con cámara", pc != null && pc.playerCamera != null);
            Check("PlayerController con staminaSlider", pc != null && pc.staminaSlider != null);
            var it = player.GetComponent<Interactor>();
            Check("Interactor con promptText", it != null && it.promptText != null);
            Check("CharacterController presente", player.GetComponent<CharacterController>() != null);
            Check("SimpleInventory presente", player.GetComponent<SimpleInventory>() != null);
            var fs = player.GetComponent<PlayerFootsteps>();
            Check("Pasos: PlayerFootsteps con clip", fs != null && fs.stepClip != null);
            Check("Pasos: AudioSource en el Player", player.GetComponent<AudioSource>() != null);
        }

        // --- Pausa
        var pausePanel = Find("NOC_PausePanel");
        var pm = pausePanel != null ? pausePanel.GetComponent<PauseMenu>() : null;
        Check("Pausa: panel NOC_PausePanel existe", pausePanel != null);
        Check("Pausa: PauseMenu con botones", pm != null && pm.continueButton != null && pm.menuButton != null);
        Check("Pausa: desactivado hasta que la intro termine", pm != null && !pm.enabled);

        // --- Intro
        var intro = Find("NOC_IntroOverlay");
        var introComp = intro != null ? intro.GetComponent<IntroDormitorio>() : null;
        Check("Intro: NOC_IntroOverlay existe", intro != null);
        Check("Intro: IntroDormitorio con panel y texto", introComp != null && introComp.panel != null && introComp.introText != null);

        // --- Inspección (lupa / pinza)
        Check("Inspección: panel NOC_InspectPanel existe", Find("NOC_InspectPanel") != null);

        var pinza = Find("NOC_Item_Pinza");
        var pinzaInsp = pinza != null ? pinza.GetComponent<InspectableObject>() : null;
        Check("Pinza existe en la celda", pinza != null);
        Check("Pinza: inspeccionable con lupa", pinzaInsp != null && pinzaInsp.requiredItemId == "lupa");
        Check("Pinza: panel de inspección conectado", pinzaInsp != null && pinzaInsp.inspectPanel != null);

        var lupa = Find("NOC_Item_Lupa");
        var lupaPickup = lupa != null ? lupa.GetComponent<PickupItem>() : null;
        Check("Lupa existe en el dormitorio", lupa != null);
        Check("Lupa: itemId lupa", lupaPickup != null && lupaPickup.itemId == "lupa");

        var diario = Find("NOC_Item_Diario");
        var diarioPickup = diario != null ? diario.GetComponent<PickupItem>() : null;
        Check("Diario existe en el dormitorio", diario != null);
        Check("Diario: itemId diario", diarioPickup != null && diarioPickup.itemId == "diario");

        // --- Escondites
        var hideSpots = Object.FindObjectsByType<HideSpot>(FindObjectsSortMode.None);
        Check("Escondites: 2 (cama del dormitorio y ropero)", hideSpots != null && hideSpots.Length == 2);
        if (hideSpots != null)
        {
            foreach (var h in hideSpots)
            {
                Check("Escondite '" + h.name + "' con punto de ocultamiento", h != null && h.hidePoint != null);
                Check("Escondite '" + h.name + "' con punto de salida", h != null && h.exitPoint != null);
            }
        }

        // --- Auditoría de colliders sólidos invisibles (causas típicas de choques fantasma)
        int invisibleSolids = 0;
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger) continue;

            // Exenciones legítimas: el Player es un CharacterController sin
            // renderer por diseño (no es un muro invisible accidental).
            if (HasLegitimateInvisibleReason(c))
                continue;

            bool visible = false;
            foreach (Renderer r in c.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && r.enabled) { visible = true; break; }
            }
            if (!visible)
            {
                invisibleSolids++;
                Report.Add("  INFO: Collider sólido sin renderer visible: " + c.gameObject.name);
            }
        }
        Check("Sin colliders sólidos invisibles", invisibleSolids == 0);

        // --- Muebles nuevos
        Check("Sillas de la cocina (2)", Find("NOC_Chair_Kitchen_A") != null && Find("NOC_Chair_Kitchen_B") != null);
        Check("Ropero del dormitorio existe", Find("NOC_Wardrobe_Bedroom") != null);

        // --- Managers
        var managers = GameObject.Find("Managers");
        Check("Managers existe", managers != null);
        if (managers != null)
        {
            var om = managers.GetComponent<ObjectiveManager>();
            Check("ObjectiveManager con objectiveText", om != null && om.objectiveText != null);
            Check("Objetivo inicial: encuentra una forma de salir", om != null && om.initialObjective.StartsWith("Encuentra una forma de salir"));
            var gui = managers.GetComponent<GameMessageUI>();
            Check("GameMessageUI con messageText", gui != null && gui.messageText != null);
        }

        // --- Ambiente y eventos
        var ambience = Find("NOC_Cabin_Ambience");
        var ambSrc = ambience != null ? ambience.GetComponent<AudioSource>() : null;
        Check("Ambiente sonoro con clip en loop", ambSrc != null && ambSrc.clip != null && ambSrc.loop);
        var events = Object.FindObjectsByType<AmbientHorrorEvent>(FindObjectsSortMode.None);
        Check("Eventos de ambiente (4)", events != null && events.Length >= 4);
        if (events != null)
        {
            foreach (var e in events)
            {
                if (e == null) continue;
                Report.Add("  INFO: " + e.name + " — " + (e.clip != null ? "clip OK" : "SIN CLIP"));
            }
        }

        // --- Render global
        Check("Niebla activa y oscura", RenderSettings.fog && RenderSettings.fogDensity > 0.01f);
        var sun = GameObject.Find("Directional Light");
        var sunLight = sun != null ? sun.GetComponent<Light>() : null;
        Check("Luz direccional tenue (< 0.5)", sunLight != null && sunLight.intensity < 0.5f);

        Report.Add("");
        Report.Add("=== RESUMEN: " + passes + " OK, " + fails + " FALLOS ===");
        string log = string.Join("\n", Report);
        Debug.Log("[CabinValidator]\n" + log);

        string outPath = Path.Combine(Application.dataPath, "..", "Library", "CabinLevel_Report.txt");
        try
        {
            File.WriteAllText(outPath, log);
            Debug.Log("[CabinValidator] Reporte guardado en: " + Path.GetFullPath(outPath));
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[CabinValidator] No se pudo escribir el reporte: " + ex.Message);
        }
    }

    private static void Check(string label, bool ok)
    {
        if (ok) { passes++; Report.Add("  [OK]    " + label); }
        else { fails++; Report.Add("  [FALLO] " + label); }
    }

    private static bool IsAnchoredAt(string name, Vector2 anchor)
    {
        var go = GameObject.Find(name);
        if (go == null) return false;
        var rt = go.GetComponent<RectTransform>();
        return rt != null && (rt.anchorMin - anchor).sqrMagnitude < 0.01f && (rt.anchorMax - anchor).sqrMagnitude < 0.01f;
    }

    private static Bounds GetCabinStructureBounds()
    {
        bool any = false;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        EncapsulateRenderers(GameObject.Find("Floor_Cabin"), ref b, ref any);
        EncapsulateRenderers(GameObject.Find("Cabin_Walls"), ref b, ref any);
        if (!any) b = new Bounds(new Vector3(0f, 1.75f, 0f), new Vector3(28.4f, 3.5f, 22.2f));
        return b;
    }

    private static void EncapsulateRenderers(GameObject go, ref Bounds b, ref bool any)
    {
        if (go == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || !r.enabled || r.bounds.size.sqrMagnitude < 0.01f) continue;
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
    }

    private static bool IsStaminaBottomLeft()
    {
        var go = GameObject.Find("StaminaBar");
        if (go == null) return false;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return false;
        bool corner = rt.anchorMin.x < 0.01f && rt.anchorMin.y < 0.01f
                   && rt.anchorMax.x < 0.01f && rt.anchorMax.y < 0.01f
                   && rt.pivot.x < 0.01f && rt.pivot.y < 0.01f;
        return corner
            && rt.anchoredPosition.x > 10f && rt.anchoredPosition.x < 80f
            && rt.anchoredPosition.y > 10f && rt.anchoredPosition.y < 80f;
    }

    private static float GetWallTop()
    {
        var walls = GameObject.Find("Cabin_Walls");
        var renderers = walls != null ? walls.GetComponentsInChildren<MeshRenderer>(true) : null;
        float top = 0f;
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null && r.bounds.max.y > top) top = r.bounds.max.y;
            }
        }
        return top;
    }

    private static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        bool any = false;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);

        // Huella real combinando TODOS los renderers del objeto y sus hijos
        // (MeshRenderer, SkinnedMeshRenderer, SpriteRenderer, ParticleSystem...).
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || !r.enabled) continue;
            if (r.bounds.size.sqrMagnitude < 0.01f) continue;
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }

        // Sin renderers: recurrir a colliders sólidos como fallback apropiado.
        if (!any)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || !c.enabled || c.isTrigger) continue;
                if (c.bounds.size.sqrMagnitude < 0.01f) continue;
                if (!any) { b = c.bounds; any = true; }
                else b.Encapsulate(c.bounds);
            }
        }

        bounds = any ? b : new Bounds(Vector3.zero, Vector3.zero);
        return any;
    }

    private static bool HasLegitimateInvisibleReason(Collider c)
    {
        // Solo exenciones estrechas y justificadas; todo lo demás se audita.
        // 1. CharacterController: es el collider de personaje, no un muro estático.
        if (c is CharacterController)
            return true;
        // 2. Objetos del jugador: por nombre (Player), por tag 'Player' o por capa Player.
        if (c.GetComponent<CharacterController>() != null)
            return true;
        if (c.gameObject.tag == "Player")
            return true;
        int playerLayer = LayerMask.NameToLayer("Player");
        return playerLayer >= 0 && c.gameObject.layer == playerLayer;
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
}

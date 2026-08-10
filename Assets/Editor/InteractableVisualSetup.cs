using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramienta EXCLUSIVAMENTE de Editor para dar cuerpos visuales low-poly
/// (primitivas de Unity) a los objetos interactivos de Cabin_Level1.
/// - Nunca toca el .unity como YAML: la serializacion la hace Unity.
/// - Idempotente: no crea duplicados (reutiliza IV_* si existen).
/// - Los hijos IV_* jamas tendran colliders (se eliminan al crearse).
/// - Key_Cabin nunca se mueve.
/// - Independiente de CabinLevelBuilder.
/// </summary>
public static class InteractableVisualSetup
{
    private const string MenuRoot = "Nocturia/Cabin_Level1/Visuals/";
    private const string MatDir = "Assets/Materials/Generated/";
    private const string KeyPrefKey = "IVSetup.KeyCabinPos";
    // Altura definitiva del contenedor IV_* (probada en Unity). Nada la sobrescribe.
    private const float VisualHeight = -2.46f;
    // Margen extra del collider del root alrededor de los visuales (metros).
    private const float ColliderPadding = 0.05f;
    // Limite de seguridad anti-collider-gigante (metros en mundo).
    private const float MaxColliderWorldSize = 5f;

    // ---------------------------------------------------------------- menu --

    [MenuItem(MenuRoot + "Apply Interactable Visuals")]
    private static void ApplyInteractableVisuals()
    {
        if (!IsCabinScene()) return;

        Undo.SetCurrentGroupName("Apply Interactable Visuals [IV]");
        int group = Undo.GetCurrentGroup();

        SaveKeyCabinSnapshot();
        var mats = ResolveMaterials(ensureCreate: true);

        bool anyIssue = false;
        foreach (var spec in ItemSpecs)
        {
            anyIssue |= ProcessItem(spec, mats);
        }

        if (mats.WoodCreated || mats.CoverCreated)
            AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(group);

        Debug.Log("[InteractableVisualSetup] Apply completado. " +
                  (anyIssue ? "Con pendientes (revisar log)." : "Todo OK."));
    }

    [MenuItem(MenuRoot + "Validate Interactable Visuals")]
    private static void ValidateInteractableVisuals()
    {
        if (!IsCabinScene()) return;

        int failures = 0;
        var lines = new List<string>();

        foreach (var spec in ItemSpecs)
        {
            lines.AddRange(ValidateItem(spec, ref failures));
        }
        lines.AddRange(ValidateKeyCabin(ref failures));

        foreach (var l in lines) Debug.Log("[IVValidate] " + l);

        if (failures == 0)
        {
            Debug.Log("[InteractableVisualSetup] Validacion OK. Visuales IV_* presentes, hijos sin colliders, altura -2.46, collider ROOT presente/habilitado y alineado, IInteractable en root.");
        }
        else
        {
            Debug.LogError("[InteractableVisualSetup] Validacion con " + failures + " problema(s). Ver lineas [IVValidate].");
            EditorUtility.DisplayDialog("IV Validate",
                "Validacion de visuales IV_: " + failures + " problema(s). Revisa la consola.",
                "OK");
        }
    }

    // ------------------------------------------------------------- specs --

    private enum Kind { Lupa, Pinza, Diario, Carta, Nota, Crystal }

    private class ChildSpec
    {
        public string Name;
        public PrimitiveType Prim;
        public Vector3 Pos;
        public Vector3 Rot;
        public Vector3 Scale;   // unidades de MUNDO (la compensacion hace los hijos 1:1)
        public Material Mat;
    }

    private class ItemSpec
    {
        public string RootName;
        public string VisualName;
        public Kind Kind;
        public Vector3 Tilt;             // inclinacion ligera del cuerpo visual
        public ChildSpec[] Children;
    }

    private static readonly ItemSpec[] ItemSpecs = BuildSpecs();

    private static ItemSpec[] BuildSpecs()
    {
        return new[]
        {
            new ItemSpec
            {
                RootName = "NOC_Item_Lupa",
                VisualName = "IV_LupaVisual",
                Kind = Kind.Lupa,
                Tilt = new Vector3(8f, 0f, 4f),
                Children = new[]
                {
                    // lente: cilindro muy plano, diametro 0.28, alto 0.035
                    C("IV_Lens", PrimitiveType.Cylinder, new Vector3(0f, 0.020f, 0f), Vector3.zero, new Vector3(0.28f, 0.0175f, 0.28f), null),
                    // aro: cilindro fino metalico, un poco mayor
                    C("IV_Rim", PrimitiveType.Cylinder, new Vector3(0f, 0.017f, 0f), Vector3.zero, new Vector3(0.31f, 0.010f, 0.31f), null),
                    // mango: cubo alargado inclinado hacia atras
                    C("IV_Handle", PrimitiveType.Cube, new Vector3(0f, 0.005f, -0.135f), new Vector3(-42f, 0f, 0f), new Vector3(0.045f, 0.24f, 0.045f), null),
                }
            },
            new ItemSpec
            {
                RootName = "NOC_Item_Pinza",
                VisualName = "IV_PinzaVisual",
                Kind = Kind.Pinza,
                Tilt = Vector3.zero,
                Children = new[]
                {
                    C("IV_JawA", PrimitiveType.Cube, new Vector3(0f, 0.135f, 0f), new Vector3(0f, 0f, 5f), new Vector3(0.05f, 0.27f, 0.025f), null),
                    C("IV_JawB", PrimitiveType.Cube, new Vector3(0f, 0.128f, 0f), new Vector3(0f, 0f, -5f), new Vector3(0.05f, 0.27f, 0.025f), null),
                    C("IV_Spring", PrimitiveType.Cylinder, new Vector3(0f, 0.105f, 0f), Vector3.zero, new Vector3(0.07f, 0.025f, 0.07f), null),
                }
            },
            new ItemSpec
            {
                RootName = "NOC_Item_Diario",
                VisualName = "IV_DiarioVisual",
                Kind = Kind.Diario,
                Tilt = Vector3.zero,
                Children = new[]
                {
                    C("IV_BackCover", PrimitiveType.Cube, new Vector3(0f, 0.014f, 0f), Vector3.zero, new Vector3(0.40f, 0.028f, 0.30f), null),
                    C("IV_Pages", PrimitiveType.Cube, new Vector3(0f, 0.030f, 0f), Vector3.zero, new Vector3(0.365f, 0.014f, 0.27f), null),
                    // tapa frontal ligeramente abierta (~15 grados)
                    C("IV_FrontCover", PrimitiveType.Cube, new Vector3(0f, 0.038f, 0f), new Vector3(-15f, 0f, 0f), new Vector3(0.40f, 0.022f, 0.30f), null),
                }
            },
            new ItemSpec
            {
                RootName = "NOC_Note_Bedroom",   // ReadableNote.noteTitle == "Carta"
                VisualName = "IV_CartaVisual",
                Kind = Kind.Carta,
                Tilt = Vector3.zero,
                Children = new[]
                {
                    C("IV_Paper", PrimitiveType.Cube, new Vector3(0f, 0.004f, 0f), Vector3.zero, new Vector3(0.35f, 0.008f, 0.25f), null),
                    C("IV_Fold", PrimitiveType.Cube, new Vector3(-0.10f, 0.010f, 0f), new Vector3(-9f, 28f, 0f), new Vector3(0.17f, 0.008f, 0.25f), null),
                }
            },
            new ItemSpec
            {
                RootName = "NOC_Note_Kitchen",  // ReadableNote.noteTitle == "Nota arrugada"
                VisualName = "IV_NotaVisual",
                Kind = Kind.Nota,
                Tilt = Vector3.zero,
                Children = new[]
                {
                    C("IV_PaperA", PrimitiveType.Cube, new Vector3(0f, 0.004f, 0f), Vector3.zero, new Vector3(0.30f, 0.007f, 0.28f), null),
                    C("IV_PaperB", PrimitiveType.Cube, new Vector3(-0.02f, 0.010f, 0.01f), new Vector3(6f, -18f, 0f), new Vector3(0.26f, 0.007f, 0.24f), null),
                    C("IV_PaperC", PrimitiveType.Cube, new Vector3(0.06f, 0.016f, -0.02f), new Vector3(-5f, 40f, 0f), new Vector3(0.20f, 0.007f, 0.20f), null),
                }
            },
            new ItemSpec
            {
                RootName = "NOC_Lens_Crystal",  // PickupItem.itemId == "lens_crystal"
                VisualName = "IV_CrystalVisual",
                Kind = Kind.Crystal,
                Tilt = Vector3.zero,
                Children = new[]
                {
                    C("IV_CrystalBase", PrimitiveType.Cube, new Vector3(0f, 0.07f, 0f), new Vector3(0f, 45f, 0f), new Vector3(0.42f, 0.14f, 0.42f), null),
                    C("IV_CrystalMid", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0f, 45f, 0f), new Vector3(0.30f, 0.16f, 0.30f), null),
                    C("IV_CrystalTop", PrimitiveType.Cube, new Vector3(0f, 0.39f, 0f), new Vector3(0f, 45f, 0f), new Vector3(0.20f, 0.18f, 0.20f), null),
                }
            },
        };
    }

    private static ChildSpec C(string name, PrimitiveType p, Vector3 pos, Vector3 rot, Vector3 scl, Material mat)
    {
        return new ChildSpec { Name = name, Prim = p, Pos = pos, Rot = rot, Scale = scl, Mat = mat };
    }

    // ----------------------------------------------------------- materials --

    private class MatSet
    {
        public Material Metal, Paper, Pages, WarmGold, LensMat, LupaMat;
        public Material Wood, Cover;
        public bool WoodCreated, CoverCreated;
    }

    private static MatSet ResolveMaterials(bool ensureCreate)
    {
        var set = new MatSet();
        set.Metal = LoadMat("Interactable_Metal.mat");
        set.Paper = LoadMat("Interactable_Paper.mat");
        set.Pages = LoadMat("Interactable_Pages.mat");
        set.WarmGold = LoadMat("Key_Gold.mat");
        set.LensMat = LoadMat("Lens.mat");
        set.LupaMat = LoadMat("Lupa.mat");

        set.Wood = LoadMat("Interactable_Wood.mat");
        set.Cover = LoadMat("Interactable_BookCover.mat");

        if (ensureCreate)
        {
            if (set.Wood == null)
            {
                set.Wood = CreateMat("Interactable_Wood.mat", new Color(0.27f, 0.18f, 0.11f), 0f);
                set.WoodCreated = set.Wood != null;
            }
            if (set.Cover == null)
            {
                set.Cover = CreateMat("Interactable_BookCover.mat", new Color(0.34f, 0.14f, 0.11f), 0f);
                set.CoverCreated = set.Cover != null;
            }
        }
        return set;
    }

    private static Material LoadMat(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(MatDir + fileName);
    }

    private static Material CreateMat(string fileName, Color color, float metallic)
    {
        Shader s = Shader.Find("Standard");
        if (s == null)
        {
            Debug.LogError("[InteractableVisualSetup] Shader 'Standard' no encontrado. No creo " + fileName);
            return null;
        }
        var mat = new Material(s) { name = System.IO.Path.GetFileNameWithoutExtension(fileName) };
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", 0.35f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);
        AssetDatabase.CreateAsset(mat, MatDir + fileName);
        Debug.Log("[InteractableVisualSetup] Material creado: " + MatDir + fileName);
        return mat;
    }

    // ------------------------------------------------------------ guards --

    private static bool IsCabinScene()
    {
        Scene s = EditorSceneManager.GetActiveScene();
        if (s.name != "Cabin_Level1")
        {
            Debug.LogError(
                "[InteractableVisualSetup] Abre Cabin_Level1 antes de ejecutar esta herramienta.");
            return false;
        }
        return true;
    }

    private static GameObject FindInScene(string name)
    {
        foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (g.name == name) return g;
            var t = g.transform.FindDeep(name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    // ---------------------------------------------------------- processing --

    private static bool ProcessItem(ItemSpec spec, MatSet mats)
    {
        GameObject root = FindInScene(spec.RootName);
        if (root == null)
        {
            Debug.LogWarning("[InteractableVisualSetup] NO EXISTE root '" + spec.RootName + "'. Se omite.");
            return false;
        }
        Transform rt = root.transform;

        // 1) Contenedor visual (idempotente: reutiliza el IV_* existente).
        Transform vr = EnsureVisualRoot(root, spec.VisualName);
        if (vr == null) return false;

        // 2) Compensacion de escala (hijos 1:1 en mundo) + inclinacion del cuerpo.
        Vector3 scl = rt.localScale;
        scl = new Vector3(Mathf.Abs(scl.x) < 0.0001f ? 1f : 1f / scl.x,
                          Mathf.Abs(scl.y) < 0.0001f ? 1f : 1f / scl.y,
                          Mathf.Abs(scl.z) < 0.0001f ? 1f : 1f / scl.z);
        Undo.RecordObject(vr, "IV visual root");
        vr.localScale = scl;
        vr.localRotation = Quaternion.Euler(spec.Tilt);

        // 3) Hijos primitivos (escala local = especificacion del hijo).
        for (int i = 0; i < spec.Children.Length; i++)
        {
            BuildOrRestyleChild(vr, spec, mats, i);
        }

        // 4) Ningun hijo IV_* puede tener collider.
        RemoveVisualColliders(vr);

        // 5) Altura FINAL definitiva: los contenedores se centran en X/Z sobre
        //    su root y localPosition.y queda FIJO en -2.46 (probado en Unity).
        //    Ninguna otra logica (surface/gap/raise) puede sobrescribirlo.
        vr.localPosition = new Vector3(0f, VisualHeight, 0f);

        // 6) Collider funcional del root: SIEMPRE cubriendo los visuales
        //    (donde estan, a -2.46), para que el raycast del Interactor
        //    encuentre el collider en el mismo sitio que la grafica.
        FitRootColliderToVisual(root, vr);

        Debug.Log("[InteractableVisualSetup] " + spec.RootName + " -> " + spec.VisualName +
                  " configurado con localPosition.y = " + VisualHeight.ToString("0.000") + ".");
        return true;
    }

    private static void BuildOrRestyleChild(Transform vr, ItemSpec spec, MatSet mats, int index)
    {
        ChildSpec cs = spec.Children[index];
        Material mat = ResolveChildMat(spec.Kind, cs.Name, mats);

        Transform ch = vr.Find(cs.Name);
        if (ch == null)
        {
            GameObject go = GameObject.CreatePrimitive(cs.Prim);
            Undo.RegisterCreatedObjectUndo(go, "IV child " + cs.Name);
            go.name = cs.Name;
            go.transform.SetParent(vr, false);
            ch = go.transform;
        }
        Undo.RecordObject(ch, "IV restyle " + cs.Name);
        ch.localPosition = cs.Pos;
        ch.localRotation = Quaternion.Euler(cs.Rot);
        ch.localScale = cs.Scale;

        var r = ch.GetComponent<Renderer>();
        if (r != null && mat != null)
        {
            Undo.RecordObject(r, "IV mat " + cs.Name);
            r.sharedMaterial = mat;
        }
        ch.gameObject.SetActive(true);
    }

    private static Material ResolveChildMat(Kind kind, string childName, MatSet m)
    {
        switch (kind)
        {
            case Kind.Lupa:
                if (childName == "IV_Lens") return m.LupaMat ?? m.Pages;
                if (childName == "IV_Rim") return m.Metal;
                return m.Wood;
            case Kind.Pinza:
                if (childName == "IV_Spring") return m.WarmGold ?? m.Metal;
                return m.Wood;
            case Kind.Diario:
                if (childName == "IV_Pages") return m.Pages;
                return m.Cover;
            case Kind.Carta:
            case Kind.Nota:
                return m.Paper;
            case Kind.Crystal:
                return m.LensMat;
        }
        return m.Paper;
    }

    private static void RemoveVisualColliders(Transform vr)
    {
        foreach (Collider col in vr.GetComponentsInChildren<Collider>(true))
        {
            Undo.DestroyObjectImmediate(col);
        }
    }

    /// <summary>
    /// Ajusta el BoxCollider del root para cubrir el AABB combinado de los
    /// renderers IV_* (estado VIVO en la escena, ya a VisualHeight=-2.46).
    /// Reutiliza el BoxCollider existente (preserva enabled/isTrigger) o lo
    /// crea si falta. No anade colliders a los hijos IV_*.
    /// </summary>
    private static void FitRootColliderToVisual(GameObject root, Transform vr)
    {
        Bounds vis = default;
        bool any = false;
        foreach (Renderer r in vr.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.bounds.size.sqrMagnitude <= 0f) continue;
            if (!any) { vis = r.bounds; any = true; }
            else vis.Encapsulate(r.bounds);
        }

        if (!any)
        {
            Debug.LogWarning("[InteractableVisualSetup] " + root.name +
                             ": sin renderers activos bajo " + vr.name + "; NO se ajusta el collider.");
            return;
        }

        // Anti-gigante: recorta en MUNDO cualquier eje desmesurado.
        Vector3 size = vis.size;
        bool clamped = false;
        for (int axis = 0; axis < 3; axis++)
        {
            if (size[axis] > MaxColliderWorldSize)
            {
                size[axis] = MaxColliderWorldSize;
                clamped = true;
            }
        }
        if (clamped)
        {
            vis.min = vis.center - size * 0.5f;
            vis.max = vis.center + size * 0.5f;
            Debug.LogWarning("[InteractableVisualSetup] " + root.name +
                             ": bounds visuales anormalmente grandes; collider recortado a " + size);
        }

        // 8 esquinas del AABB mundial -> espacio local del root.
        Vector3[] corners =
        {
            new Vector3(vis.min.x, vis.min.y, vis.min.z),
            new Vector3(vis.min.x, vis.min.y, vis.max.z),
            new Vector3(vis.min.x, vis.max.y, vis.min.z),
            new Vector3(vis.min.x, vis.max.y, vis.max.z),
            new Vector3(vis.max.x, vis.min.y, vis.min.z),
            new Vector3(vis.max.x, vis.min.y, vis.max.z),
            new Vector3(vis.max.x, vis.max.y, vis.min.z),
            new Vector3(vis.max.x, vis.max.y, vis.max.z),
        };
        Vector3 lmin = Vector3.positiveInfinity;
        Vector3 lmax = Vector3.negativeInfinity;
        foreach (Vector3 p in corners)
        {
            Vector3 lp = root.transform.InverseTransformPoint(p);
            lmin = Vector3.Min(lmin, lp);
            lmax = Vector3.Max(lmax, lp);
        }

        BoxCollider box = root.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = Undo.AddComponent<BoxCollider>(root);
            Debug.Log("[InteractableVisualSetup] " + root.name + ": sin BoxCollider, creado uno nuevo.");
        }

        bool wasEnabled = box.enabled;
        bool wasTrigger = box.isTrigger;
        Undo.RecordObject(box, "IV fit collider " + root.name);
        box.center = (lmin + lmax) * 0.5f;
        box.size = lmax - lmin + Vector3.one * (ColliderPadding * 2f);
        box.enabled = wasEnabled;
        box.isTrigger = wasTrigger;
        Debug.Log("[InteractableVisualSetup] " + root.name + ": collider ROOT ajustado a los visuales. " +
                  "center=" + box.center + " size=" + box.size + " (visual a localPosition.y=" +
                  vr.localPosition.y.ToString("0.000") + ").");
    }

    private static Transform EnsureVisualRoot(GameObject root, string visualName)
    {
        var existing = root.transform.Find(visualName);
        if (existing != null) return existing;
        GameObject go = new GameObject(visualName);
        Undo.RegisterCreatedObjectUndo(go, "IV root " + visualName);
        go.transform.SetParent(root.transform, false);
        return go.transform;
    }

    // -------------------------------------------------------------- validate --

    private static List<string> ValidateItem(ItemSpec spec, ref int failures)
    {
        var lines = new List<string>();
        GameObject root = FindInScene(spec.RootName);
        if (root == null)
        {
            failures++;
            lines.Add(spec.RootName + ": FAIL root no encontrado.");
            return lines;
        }
        Transform vr = root.transform.Find(spec.VisualName);
        if (vr == null)
        {
            failures++;
            lines.Add(spec.RootName + ": FAIL " + spec.VisualName + " no existe.");
        }
        else
        {
            bool active = vr.gameObject.activeInHierarchy;
            Renderer[] rs = vr.GetComponentsInChildren<Renderer>(true);
            int renderers = 0;
            int colliders = 0;
            foreach (var r in rs) if (r.enabled) renderers++;
            foreach (var c in vr.GetComponentsInChildren<Collider>(true)) colliders++;
            bool ok = active && renderers > 0 && colliders == 0;
            if (!ok) failures++;
            lines.Add(spec.RootName + ": " + (ok ? "PASS" : "FAIL") +
                      " visual activo=" + active + " renderers=" + renderers + " collidersIV=" + colliders + ".");

            bool heightOk = Mathf.Abs(vr.localPosition.y - VisualHeight) < 0.001f;
            if (!heightOk) failures++;
            lines.Add(spec.RootName + ": " + (heightOk ? "ALTURA OK" : "ALTURA INCORRECTA") +
                      " localPosition.y=" + vr.localPosition.y.ToString("0.000") + ".");

            // Alineacion: el centro del AABB visual debe quedar DENTRO del collider del root.
            Bounds vis = default;
            bool any = false;
            foreach (var r in rs)
            {
                if (!r.enabled) continue;
                if (!any) { vis = r.bounds; any = true; }
                else vis.Encapsulate(r.bounds);
            }
            var box = root.GetComponent<BoxCollider>();
            if (any && box != null)
            {
                Vector3 cbCenter = root.transform.TransformPoint(box.center);
                Vector3 cbSize = Vector3.Scale(box.size, root.transform.lossyScale);
                Bounds cb = new Bounds(cbCenter, cbSize);
                bool aligned = cb.Contains(vis.center);
                if (!aligned) failures++;
                lines.Add(spec.RootName + ": " + (aligned ? "ALINEADO" : "NO ALINEADO") +
                          " collider(" + cbCenter.y.ToString("0.000") + ") vs visual(" +
                          vis.center.y.ToString("0.000") + ") offsetY=" +
                          (vis.center.y - cbCenter.y).ToString("0.000") + ".");
            }
        }
        var rootCollider = root.GetComponent<Collider>();
        if (rootCollider == null)
        {
            failures++;
            lines.Add(spec.RootName + ": FAIL collider ROOT ausente.");
        }
        else if (!rootCollider.enabled)
        {
            failures++;
            lines.Add(spec.RootName + ": FAIL collider ROOT deshabilitado.");
        }
        bool hasInteractable = root.GetComponentInParent<IInteractable>() != null;
        if (!hasInteractable) failures++;
        lines.Add(spec.RootName + ": " + (hasInteractable ? "PASS" : "FAIL") +
                  " IInteractable en root o padres.");
        return lines;
    }

    private static List<string> ValidateKeyCabin(ref int failures)
    {
        var lines = new List<string>();
        GameObject key = FindInScene("Key_Cabin");
        if (key == null)
        {
            failures++;
            lines.Add("Key_Cabin: FAIL no encontrada.");
            return lines;
        }
        string snap = EditorPrefs.GetString(KeyPrefKey, "");
        bool posOk = true;
        if (!string.IsNullOrEmpty(snap))
        {
            string[] parts = snap.Split(';');
            if (parts.Length == 3)
            {
                Vector3 saved = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture));
                posOk = (key.transform.position - saved).sqrMagnitude < 0.00001f;
            }
        }
        var item = key.GetComponent<PickupItem>();
        bool hasItem = item != null && item.itemId == "cabin_key";
        bool hasCol = key.GetComponent<Collider>() != null;
        bool ok = posOk && hasItem && hasCol;
        if (!ok) failures++;
        lines.Add("Key_Cabin: " + (ok ? "PASS" : "FAIL") +
                  " posicion sin cambios=" + posOk + " PickupItem(cabin_key)=" + hasItem + " collider=" + hasCol + ".");
        return lines;
    }

    private static void SaveKeyCabinSnapshot()
    {
        GameObject key = FindInScene("Key_Cabin");
        if (key == null) return;
        Vector3 p = key.transform.position;
        EditorPrefs.SetString(KeyPrefKey, p.x.ToString("R", CultureInfo.InvariantCulture) + ";" + p.y.ToString("R", CultureInfo.InvariantCulture) + ";" + p.z.ToString("R", CultureInfo.InvariantCulture));
    }
}

public static class TransformSearchExtensions
{
    public static Transform FindDeep(this Transform t, string name)
    {
        foreach (Transform c in t)
        {
            if (c.name == name) return c;
            Transform r = c.FindDeep(name);
            if (r != null) return r;
        }
        return null;
    }
}
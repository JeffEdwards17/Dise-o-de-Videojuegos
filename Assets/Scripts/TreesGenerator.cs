using UnityEngine;
using UnityEngine.UI;

public class TreesGenerator : MonoBehaviour
{
     [SerializeField] private RectTransform area;
    [SerializeField] private int cantidadArboles = 22;
    [SerializeField] private float alturaMin = 160f;
    [SerializeField] private float alturaMax = 260f;
    [SerializeField] private float opacidadMin = 0.55f;
    [SerializeField] private float opacidadMax = 1f;

    private Sprite spriteTriangulo;

        private void Start()
    {
        spriteTriangulo = CrearSpritePino();
        GenerarArboles();
    }

    private Sprite CrearSpritePino()
{
    int size = 128;
    Texture2D tex = new Texture2D(size, size);
    Color transparente = new Color(0, 0, 0, 0);
    Color colorArbol = new Color(48f / 255f, 90f / 255f, 74f / 255f, 1f);

    float[] limitesTiers = { 0.00f, 0.35f, 0.62f, 0.85f, 1.00f };

    for (int y = 0; y < size; y++)
    {
        float alturaNormalizada = 1f - ((float)y / size);

        int tier = 0;
        for (int i = 0; i < limitesTiers.Length - 1; i++)
        {
            bool esUltimoTier = (i == limitesTiers.Length - 2);
            if (alturaNormalizada >= limitesTiers[i] &&
                (alturaNormalizada < limitesTiers[i + 1] || esUltimoTier))
            {
                tier = i;
            }
        }

        float inicioTier = limitesTiers[tier];
        float finTier = limitesTiers[tier + 1];
        float progresoEnTier = (alturaNormalizada - inicioTier) / (finTier - inicioTier);

        float anchoMaxTier = 0.34f + (tier * 0.22f);
        float mitadAncho = progresoEnTier * anchoMaxTier * (size / 2f);

        for (int x = 0; x < size; x++)
        {
            bool dentro = Mathf.Abs(x - size / 2f) <= mitadAncho;
            tex.SetPixel(x, y, dentro ? colorArbol : transparente);
        }
    }

    tex.Apply();
    tex.filterMode = FilterMode.Bilinear;
    return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f));
}

    private void GenerarArboles()
    {
        float anchoTotal = area.rect.width;
        float paso = anchoTotal / (cantidadArboles - 1);
        float xInicial = -anchoTotal / 2f;

        for (int i = 0; i < cantidadArboles; i++)
        {
            GameObject arbol = new GameObject("Tree", typeof(Image));
            arbol.transform.SetParent(area, false);

            Image img = arbol.GetComponent<Image>();
            img.sprite = spriteTriangulo;
            img.color = new Color(48f / 255f, 90f / 255f, 74f / 255f, Random.Range(opacidadMin, opacidadMax));

            RectTransform rt = arbol.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            float alturaBase = Random.Range(alturaMin, alturaMax);
            float anchoBase = Random.Range(90f, 150f);
            rt.sizeDelta = new Vector2(anchoBase, alturaBase);

            float posX = xInicial + (i * paso) + Random.Range(-20f, 20f);
            rt.anchoredPosition = new Vector2(posX, 0f);
        }
    }
}
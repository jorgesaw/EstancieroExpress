using UnityEngine;

public enum TileType
{
    Normal,
    Start,
    Property,
    Event,
    Robbery,
    Tax,
    Jail,
    GoToJail,
    Rest,
    Prize,
    Infrastructure,
    Construction,
    Maintenance
}

public class Tile : MonoBehaviour
{
    public int index;
    public TileType type = TileType.Normal;

    [Header("Renderers (se autocompletan si los dejás vacíos)")]
    public SpriteRenderer borderSR;  // marco (raíz)
    public SpriteRenderer fillSR;    // relleno (hijo "Fill")
    public SpriteRenderer ownerSR;   // marca de dueño (hijo "Owner")

    [Header("Outline")]
    public float specialOutlineScale = 0.90f; // tamaño del relleno cuando hay marco

    Color baseColor;
    bool cached;

    void Awake() { Cache(); }
    void OnEnable() { Cache(); }

    void Cache()
    {
        if (cached) return;

        // Marco en la raíz
        if (!borderSR) borderSR = GetComponent<SpriteRenderer>();

        // Relleno en el hijo "Fill"
        if (!fillSR)
        {
            var t = transform.Find("Fill");
            if (t) fillSR = t.GetComponent<SpriteRenderer>();
            if (!fillSR) fillSR = GetComponentInChildren<SpriteRenderer>(true);
        }

        // Marca de dueño en el hijo "Owner"
        if (!ownerSR)
        {
            var o = transform.Find("Owner");
            if (o) ownerSR = o.GetComponent<SpriteRenderer>();
        }

        // Si no hay border pero sí fill, usar fill como fallback
        if (!borderSR && fillSR) borderSR = fillSR;

        cached = true;
    }

    // ----- Llamado por BoardManager -----
    public void SetBaseColor(Color c)
    {
        Cache();
        baseColor = c;

        // Especiales = índices pares (0,2,4,...,38)
        bool special = (index % 2) == 0;

        // Relleno
        if (fillSR)
        {
            fillSR.color = c;
            fillSR.transform.localScale = special
                ? new Vector3(specialOutlineScale, specialOutlineScale, 1f)
                : Vector3.one;
        }

        // Marco solo en especiales
        if (borderSR)
        {
            borderSR.enabled = special;
            if (special) borderSR.color = Color.white;
        }
    }

    public void Highlight(Color c)
    {
        Cache();
        if (fillSR) fillSR.color = c;   // resalta SOLO el relleno
    }

    public void ClearHighlight()
    {
        Cache();
        if (fillSR) fillSR.color = baseColor;
    }

    Color Darken(Color x, float amount) => Color.Lerp(x, Color.black, Mathf.Clamp01(amount));

    // ----- Marca de dueño -----
    public void SetOwnerMark(Color c, bool visible = true)
    {
        if (ownerSR)
        {
            ownerSR.color = c;
            ownerSR.enabled = visible;
        }
    }

    public void ClearOwnerMark()
    {
        if (ownerSR) ownerSR.enabled = false;
    }
}

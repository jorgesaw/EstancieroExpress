using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject tilePrefab;

    [Header("Tablero")]
    [Min(3)] public int tilesPerSide = 6;
    public float spacing = 1.4f;

    [Header("Colores")]
    public bool useCheckerboard = true;
    public Color tileColorA = new Color(0.78f, 0.78f, 0.78f, 1f);
    public Color tileColorB = new Color(0.58f, 0.58f, 0.58f, 1f);
    public Color startColor = new Color(0.96f, 0.90f, 0.45f, 1f); // SALIDA
    [Header("Colores especiales")]
    public Color eventColor = new Color32(0x66, 0xCC, 0xFF, 0xFF); // Evento
    public Color restColor = new Color32(0x99, 0xF2, 0xE6, 0xFF); // Rest  (#99F2E6)
    public Color robberyColor = new Color32(0x1A, 0x1A, 0x1A, 0xFF); // Robbery (#1A1A1A)
    public Color taxColor = new Color32(0xFF, 0x9F, 0x54, 0xFF); // Tax (naranja suave)
    public Color jailColor = new Color32(0x33, 0x1F, 0x4D, 0xFF); // Jail (#331F4D)
    public Color prizeColor = new Color32(0xFF, 0xD5, 0x54, 0xFF); // 🎁 dorado
    public Color infraColor = new Color32(0x5E, 0xC2, 0x5E, 0xFF); // 🚉 verde
    public Color constructionColor = new Color32(0xB7, 0x8C, 0xFF, 0xFF); // 🛠 lila
    public Color maintenanceColor = new Color32(0xFF, 0xC0, 0x7A, 0xFF); // ⚠ ámbar
    public Color goToJailColor = new Color32(0xE0, 0x3C, 0x31, 0xFF); // 🚔 rojo (Ir a cárcel)
    [Header("Colores de propiedades (impares)")]
    public Color propAmarillo = new Color32(0xFF, 0xE0, 0x66, 0xFF); // 1,3
    public Color propCeleste = new Color32(0x6C, 0xC5, 0xFF, 0xFF); // 5,7
    public Color propNaranja = new Color32(0xFF, 0xA6, 0x3A, 0xFF); // 9,11
    public Color propRosa = new Color32(0xFF, 0x7A, 0xB3, 0xFF); // 13,15
    public Color propVerde = new Color32(0x5E, 0xC2, 0x5E, 0xFF); // 17,19
    public Color propAzulClaro = new Color32(0x8F, 0xC1, 0xFF, 0xFF); // 21,23
    public Color propMarron = new Color32(0xB4, 0x8A, 0x60, 0xFF); // 25,27
    public Color propPurpura = new Color32(0x9D, 0x6B, 0xFF, 0xFF); // 29,31
    public Color propRojo = new Color32(0xFF, 0x5C, 0x5C, 0xFF); // 33,35
    public Color propAzul = new Color32(0x4D, 0x7C, 0xFF, 0xFF); // 37,39

    private readonly List<Tile> tiles = new();
    public Vector3[] PathPositions { get; private set; }

    void Awake() => Build();

    [ContextMenu("Rebuild Board")]
    public void RebuildBoard() => Build();

#if UNITY_EDITOR
    // Se auto-regenera en modo edición cuando cambiás algo
    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (tilePrefab == null) return;
        if (Application.isPlaying) return;

        // Evita warnings de OnValidate/OnTransformChildrenChanged
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            BuildImmediate();
        };
    }
#endif

    // ---------------------- Build ----------------------

    void Build()
    {
        ClearChildren();
        GeneratePositions();
        InstantiateTiles(immediate: false);
    }

#if UNITY_EDITOR
    void BuildImmediate()
    {
        ClearChildrenImmediate();
        GeneratePositions();
        InstantiateTiles(immediate:true);
    }
#endif

    void GeneratePositions()
    {
        tiles.Clear();

        int side = Mathf.Max(3, tilesPerSide);
        var positions = new List<Vector3>();

        float halfW = (side - 1) * spacing * 0.5f;
        float halfH = (side - 1) * spacing * 0.5f;

        // Tile_00: esquina inferior izquierda
        positions.Add(new Vector3(-halfW, -halfH, 0));

        // 1) Lado IZQUIERDO: de abajo → arriba
        for (int y = 1; y < side; y++)
            positions.Add(new Vector3(-halfW, -halfH + y * spacing, 0));

        // 2) Lado SUPERIOR: de izquierda → derecha
        for (int x = 1; x < side; x++)
            positions.Add(new Vector3(-halfW + x * spacing, halfH, 0));

        // 3) Lado DERECHO: de arriba → abajo (incluye esquina inferior derecha)
        for (int y = side - 2; y >= 0; y--)
            positions.Add(new Vector3(halfW, -halfH + y * spacing, 0));

        // 4) Lado INFERIOR: de derecha → izquierda (sin repetir Tile_00)
        for (int x = side - 2; x >= 1; x--)
            positions.Add(new Vector3(-halfW + x * spacing, -halfH, 0));

        PathPositions = positions.ToArray();
    }


    void InstantiateTiles(bool immediate)
    {
        for (int i = 0; i < PathPositions.Length; i++)
        {
            GameObject go;

#if UNITY_EDITOR
            if (immediate)
                go = UnityEditor.PrefabUtility.InstantiatePrefab(tilePrefab, transform) as GameObject;
            else
                go = Instantiate(tilePrefab, transform);
#else
            go = Instantiate(tilePrefab, transform);
#endif
            go.transform.position = PathPositions[i];
            go.name = $"Tile_{i:00}";

            var t = go.GetComponent<Tile>();
            if (t != null)
            {
                t.index = i;

                // --------- Tipo por índice (pares especiales completos) ---------
                TileType kind = TileType.Normal;

                // Impares = propiedades
                if (i % 2 == 1) kind = TileType.Property;

                // Pares según tu tabla final
                switch (i)
                {
                    case 0: kind = TileType.Start; break;
                    case 2: kind = TileType.Event; break;
                    case 4: kind = TileType.Prize; break;
                    case 6: kind = TileType.Infrastructure; break;
                    case 8: kind = TileType.Tax; break;
                    case 10: kind = TileType.Jail; break;   // Visita
                    case 12: kind = TileType.Robbery; break;
                    case 14: kind = TileType.Infrastructure; break;
                    case 16: kind = TileType.Event; break;
                    case 18: kind = TileType.Prize; break;
                    case 20: kind = TileType.Rest; break;
                    case 22: kind = TileType.Robbery; break;
                    case 24: kind = TileType.Infrastructure; break;
                    case 26: kind = TileType.Event; break;
                    case 28: kind = TileType.Construction; break;
                    case 30: kind = TileType.GoToJail; break;   // Teleporta a 10 y pierde 1 turno (luego)
                    case 32: kind = TileType.Infrastructure; break;
                    case 34: kind = TileType.Event; break;
                    case 36: kind = TileType.Maintenance; break;
                    case 38: kind = TileType.Robbery; break;
                }
                t.type = kind;

                // --------- Color por tipo (placeholders) ---------
                // --- Color base por tipo ---
                Color c =
                    kind == TileType.Property ? GetPropertyColorForIndex(i) :
                    kind switch
                    {
                        TileType.Start => startColor,
                        TileType.Event => eventColor,
                        TileType.Prize => prizeColor,
                        TileType.Infrastructure => infraColor,
                        TileType.Tax => taxColor,
                        TileType.Jail => jailColor,
                        TileType.GoToJail => goToJailColor,
                        TileType.Rest => restColor,
                        TileType.Robbery => robberyColor,
                        TileType.Construction => constructionColor,
                        TileType.Maintenance => maintenanceColor,
                        _ => useCheckerboard ? ((i % 2 == 0) ? tileColorA : tileColorB) : tileColorA
                    };


                t.SetBaseColor(c);
                tiles.Add(t);

            }
        }
    }
    Color GetPropertyColorForIndex(int i)
    {
        switch (i)
        {
            case 1: case 3: return propAmarillo;
            case 5: case 7: return propCeleste;
            case 9: case 11: return propNaranja;
            case 13: case 15: return propRosa;
            case 17: case 19: return propVerde;
            case 21: case 23: return propAzulClaro;
            case 25: case 27: return propMarron;
            case 29: case 31: return propPurpura;
            case 33: case 35: return propRojo;
            case 37: case 39: return propAzul;
            default: return tileColorA; // fallback
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

#if UNITY_EDITOR
    void ClearChildrenImmediate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }
#endif

    // ---------------------- Helpers ----------------------

    public Tile GetTile(int index)
    {
        if (tiles.Count == 0) return null;
        index = ((index % tiles.Count) + tiles.Count) % tiles.Count;
        return tiles[index];
    }

    public int TileCount => tiles.Count;
}

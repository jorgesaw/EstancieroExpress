using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public BoardManager board;
    public PlayerToken player1Prefab;
    public PlayerToken player2Prefab;

    [Header("UI")]
    public Button btnTirar;
    public TMP_Text txtEstado;
    public TMP_Text txtDado;
    public ModalPrompt modal;   // ← referencia al modal
    public TMP_Text txtP1;   // ← NUEVO
    public TMP_Text txtP2;   // ← NUEVO

    [Header("Highlight")]
    public Color currentColor = new Color(0.40f, 0.74f, 1f, 1f);
    public Color previewColor = new Color(1f, 0.86f, 0.35f, 1f);

    [Header("Economía")]
    public int startMoney = 600;
    public int startBonus = 150;   // ← NUEVO
    int dinero1, dinero2;

    [Header("Propiedades (demo)")]
    public int demoPropertyPrice = 100;
    public int demoPropertyRent = 20; // lo usaremos después
    public Color ownerColorP1 = new Color(0.90f, 0.10f, 0.10f, 1f); // rojo
    public Color ownerColorP2 = new Color(0.20f, 0.40f, 1f, 1f);    // azul

    [Header("Especiales (demo)")]
    public int premioMonto = 100;
    public int impuestoMonto = 80;   // ← NUEVO
    public int restBonus = 50;   // ← NUEVO (Descanso)


    int[] ownerByTile; // -1 = sin dueño, 0 = J1, 1 = J2
                       // ----- Tabla de propiedades (índices impares) -----
    [System.Serializable]
    public class PropertyInfo
    {
        public int index;    // 1,3,5,...,39
        public string nombre; // p.ej. "Amarillo 1"
        public string grupo;  // p.ej. "Amarillo"
        public int precio;    // por ahora 100 (luego lo escalamos)
        public int renta;     // por ahora 20 (luego lo escalamos)
    }

    PropertyInfo[] props; // tabla en memoria (20 items)

    // Devuelve la propiedad por índice (o null si no hay)
    PropertyInfo GetProp(int idx)
    {
        if (props == null) return null;
        for (int i = 0; i < props.Length; i++)
            if (props[i].index == idx) return props[i];
        return null;
    }

    // Crea la tabla por defecto con nombres/grupos (precio/renta placeholder)
    void BuildDefaultProps()
    {
        // Pares de índices impares (1,3) (5,7) ... (37,39)
        int[] indices = { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39 };
        string[] grupos = {
        "Amarillo","Amarillo",
        "Celeste","Celeste",
        "Naranja","Naranja",
        "Rosa","Rosa",
        "Verde","Verde",
        "Azul claro","Azul claro",
        "Marrón","Marrón",
        "Púrpura","Púrpura",
        "Rojo","Rojo",
        "Azul","Azul"
    };

        props = new PropertyInfo[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            string g = grupos[i];
            // nombre sencillo: "<Grupo> 1/2" según par de la pareja
            int par = (i % 2) + 1; // 1 o 2
            props[i] = new PropertyInfo
            {
                index = indices[i],
                grupo = g,
                nombre = $"{g} {par}",
                precio = demoPropertyPrice,  // por ahora usamos los valores actuales
                renta = demoPropertyRent
            };
        }
    }


    PlayerToken p1, p2;
    int turno = 0; // 0 = jugador 1, 1 = jugador 2
    bool esperandoDecision = false; // para pausar el flujo hasta que elija en el modal

    Tile lastCurrent, lastPreview;

    void Start()
    {
        // Instanciar fichas en la salida (índice 0) con una leve separación
        Vector3 start = board.PathPositions[0];
        p1 = Instantiate(player1Prefab, start + new Vector3(-0.35f, 0, 0), Quaternion.identity);
        p2 = Instantiate(player2Prefab, start + new Vector3(0.35f, 0, 0), Quaternion.identity);
        p1.boardIndex = 0;
        p2.boardIndex = 0;

        if (btnTirar) btnTirar.onClick.AddListener(OnTirar);

        dinero1 = startMoney;          // ← NUEVO
        dinero2 = startMoney;          // ← NUEVO
        UpdateMoneyUI();               // ← NUEVO
        ownerByTile = new int[board.TileCount];
        for (int i = 0; i < ownerByTile.Length; i++) ownerByTile[i] = -1;
        BuildDefaultProps();  // ← crea la tabla de propiedades (nombres/grupos)


        UpdateTurnUI();
        HighlightCurrentOnly();

    }

    void OnDestroy()
    {
        if (btnTirar) btnTirar.onClick.RemoveListener(OnTirar);
    }

    void OnTirar()
    {
        if (IsMovingAny()) return;
        StartCoroutine(RollAndMove());
    }

    bool IsMovingAny()
    {
        return (p1 && p1.isMoving) || (p2 && p2.isMoving);
    }

    IEnumerator RollAndMove()
    {
        if (btnTirar) btnTirar.interactable = false;

        var player = (turno == 0) ? p1 : p2;

        int roll = Random.Range(1, 7); // 1..6
        if (txtDado) txtDado.text = roll.ToString();

        int idxAntes = player.boardIndex;   // ← NUEVO (guardar índice de salida)

        // Resalta casilla actual + preview de destino
        ShowHighlights(player, roll);
        yield return new WaitForSeconds(0.35f);

        // Mover (PlayerToken avanza horario sumando +1)
        yield return player.MoveSteps(board.PathPositions, roll);

        // --- Bono por pasar/caer en Start (Tile_00) ---  ← NUEVO
        int totalTiles = board.TileCount;
        bool pasoPorStart = (idxAntes + roll) >= totalTiles; // si “cruzó” el 0
        if (pasoPorStart)
        {
            AddMoney(turno, startBonus); // usa tu helper y refresca la UI
        }

        // Actualiza resaltado a la posición final
        ClearHighlights();
        HighlightTile(board.GetTile(player.boardIndex), currentColor, ref lastCurrent);
        // === Compra/propiedad (v1) ===
        var landedTile = board.GetTile(player.boardIndex);
        if (landedTile && (landedTile.index % 2 == 1)) // solo impares = propiedades
        {
            int idx = landedTile.index;
            int duenoActual = ownerByTile[idx];

            if (duenoActual == -1) // sin dueño → ofrecer compra
            {
                esperandoDecision = true;
                var info = GetProp(idx);
                int price = info?.precio ?? demoPropertyPrice;
                int rent = info?.renta ?? demoPropertyRent;

                string titulo = info != null ? $"¿Comprar {info.nombre}?" : "¿Comprar propiedad?";
                string cuerpo = $"Precio ${price} • Renta ${rent}";


                modal.Show(
                    titulo,
                    cuerpo,
                    "Comprar",
                    "Pasar",
                    onYes: () =>
                    {
                        // intenta pagar
                        if (SpendMoney(turno, price))
                        {
                            ownerByTile[idx] = turno;
                            var colorDueno = (turno == 0) ? ownerColorP1 : ownerColorP2;
                            landedTile.SetOwnerMark(colorDueno, true);
                            if (txtEstado) txtEstado.text = $"Jugador {turno + 1} compró {(info != null ? info.nombre : $"la casilla {idx}")} por ${price}.";
                        }

                        else
                        {
                            // sin saldo: no compra (más adelante mostramos aviso bonito)
                            Debug.Log("No alcanza el dinero.");
                        }
                        esperandoDecision = false;
                    },
                    onNo: () => { esperandoDecision = false; }
                );

                // Espera hasta que toque un botón del modal
                yield return new WaitUntil(() => esperandoDecision == false);
            }
            else
            {
                // Si el dueño es el rival, cobro renta v1
                if (duenoActual != turno)
                {
                    var info = GetProp(idx);
                    int baseRent = info?.renta ?? demoPropertyRent;
                    bool parCompleto = OwnsPair(duenoActual, idx);
                    int rent = parCompleto ? baseRent * 2 : baseRent;

                    PayPlayerToPlayer(turno, duenoActual, rent);
                    if (txtEstado) txtEstado.text =
                        $"Jugador {turno + 1} pagó renta ${rent} a Jugador {duenoActual + 1}" +
                        (info != null ? $" ({info.nombre})" : "") +
                        (parCompleto ? " (x2 por grupo)" : "") + ".";

                }

                // Si es tu propia propiedad, no pasa nada
            }

        }
        // --- Especiales pares simples (v1) ---
        if (landedTile && landedTile.index % 2 == 0 && landedTile.index != 0) // pares, excluye Start (0)
        {
            int idx = landedTile.index;

            switch (idx)
            {
                case 4:
                case 18:
                    {
                        // Cobrar
                        AddMoney(turno, premioMonto);
                        if (txtEstado) txtEstado.text =
                            $"Jugador {turno + 1} cobró premio ${premioMonto} (casilla {idx}).";

                        // Mostrar modal informativo y esperar que lo cierre
                        esperandoDecision = true;
                        modal.Show(
                            "Premio",
                            $"${premioMonto}",
                            "OK",
                            "Cerrar",
                            onYes: () => { esperandoDecision = false; },
                            onNo: () => { esperandoDecision = false; }
                        );
                        yield return new WaitUntil(() => esperandoDecision == false);

                        break;
                    }


                case 8:
                    {
                        // Impuesto: pagas al banco
                        PayToBank(turno, impuestoMonto);
                        if (txtEstado) txtEstado.text =
                            $"Jugador {turno + 1} pagó impuesto ${impuestoMonto} (casilla {idx}).";

                        // Modal informativo
                        esperandoDecision = true;
                        modal.Show(
                            "Impuesto",
                            $"${impuestoMonto}",
                            "OK",
                            "Cerrar",
                            onYes: () => { esperandoDecision = false; },
                            onNo: () => { esperandoDecision = false; }
                        );
                        yield return new WaitUntil(() => esperandoDecision == false);
                        break;
                    }
                case 20:
                    {
                        // Descanso: pequeño bonus
                        AddMoney(turno, restBonus);
                        if (txtEstado) txtEstado.text =
                            $"Jugador {turno + 1} descansó y cobró ${restBonus} (casilla {idx}).";

                        // Modal informativo
                        esperandoDecision = true;
                        modal.Show(
                            "Descanso",
                            $"+${restBonus}",
                            "OK",
                            "Cerrar",
                            onYes: () => { esperandoDecision = false; },
                            onNo: () => { esperandoDecision = false; }
                        );
                        yield return new WaitUntil(() => esperandoDecision == false);
                        break;
                    }

            }
        }

        turno = 1 - turno;
       
        UpdateTurnUI();
        if (btnTirar) btnTirar.interactable = true;
    }

    void UpdateTurnUI()
    {
        if (txtEstado) txtEstado.text = $"Turno: Jugador {turno + 1}";
        UpdateMoneyUI(); // ← añade esta línea
    }


    int Wrap(int index)
    {
        int n = board.TileCount;
        return ((index % n) + n) % n;
    }

    void ShowHighlights(PlayerToken pl, int steps)
    {
        ClearHighlights();

        // Actual
        HighlightTile(board.GetTile(pl.boardIndex), currentColor, ref lastCurrent);

        // Destino (horario = sumar pasos)
        int destIndex = Wrap(pl.boardIndex + steps);
        HighlightTile(board.GetTile(destIndex), previewColor, ref lastPreview);
    }


    void HighlightCurrentOnly()
    {
        ClearHighlights();
        var player = (turno == 0) ? p1 : p2;
        HighlightTile(board.GetTile(player.boardIndex), currentColor, ref lastCurrent);
    }

    void HighlightTile(Tile tile, Color color, ref Tile store)
    {
        if (!tile) return;
        tile.Highlight(color);
        store = tile;
    }

    void ClearHighlights()
    {
        if (lastCurrent) { lastCurrent.ClearHighlight(); lastCurrent = null; }
        if (lastPreview) { lastPreview.ClearHighlight(); lastPreview = null; }
    }
    void UpdateMoneyUI()
    {
        if (txtP1) txtP1.text = $"J1: ${dinero1}";
        if (txtP2) txtP2.text = $"J2: ${dinero2}";
    }

    // Helpers que usaremos después (compras, alquiler, etc.)
    public void AddMoney(int playerIndex, int amount)
    {
        if (playerIndex == 0) dinero1 += amount;
        else dinero2 += amount;
        UpdateMoneyUI();
    }

    public bool SpendMoney(int playerIndex, int amount)
    {
        int cur = (playerIndex == 0) ? dinero1 : dinero2;
        if (cur < amount) return false;
        if (playerIndex == 0) dinero1 -= amount;
        else dinero2 -= amount;
        UpdateMoneyUI();
        return true;
    }
    // Paga "amount" al banco. Si no alcanza, paga lo que tenga (queda en 0).
    void PayToBank(int player, int amount)
    {
        // Si alcanza, listo
        if (SpendMoney(player, amount)) return;

        // Si NO alcanza, paga todo lo que tenga (queda en 0)
        int cur = (player == 0) ? dinero1 : dinero2;
        if (cur > 0)
        {
            if (player == 0) dinero1 -= cur; else dinero2 -= cur;
            UpdateMoneyUI();
        }
        Debug.Log("No alcanzó para impuesto; se pagó parcial.");
    }

    // Si no alcanza el dinero, paga lo que tenga (v1 simple).
    void PayPlayerToPlayer(int from, int to, int amount)
    {
        if (from == to) return;

        // Intento pagar completo con el helper
        if (SpendMoney(from, amount))
        {
            AddMoney(to, amount);
            return;
        }

        // Si no alcanzó, paga lo que haya (parcial) y queda en 0
        int cur = (from == 0) ? dinero1 : dinero2;
        if (cur > 0)
        {
            if (from == 0) dinero1 -= cur;
            else dinero2 -= cur;

            AddMoney(to, cur); // AddMoney ya refresca la UI
        }

        Debug.Log("No alcanza para pagar renta (v1). Se pagó parcial.");
    }

    // ← PEGA AQUÍ (sigue dentro de GameManager)
    int MateOf(int idx)
    {
        switch (idx)
        {
            case 1: return 3;
            case 3: return 1;
            case 5: return 7;
            case 7: return 5;
            case 9: return 11;
            case 11: return 9;
            case 13: return 15;
            case 15: return 13;
            case 17: return 19;
            case 19: return 17;
            case 21: return 23;
            case 23: return 21;
            case 25: return 27;
            case 27: return 25;
            case 29: return 31;
            case 31: return 29;
            case 33: return 35;
            case 35: return 33;
            case 37: return 39;
            case 39: return 37;
            default: return -1;
        }
    }

    bool OwnsPair(int owner, int idx)
    {
        int mate = MateOf(idx);
        if (mate == -1) return false;
        return ownerByTile[idx] == owner && ownerByTile[mate] == owner;
    }

}

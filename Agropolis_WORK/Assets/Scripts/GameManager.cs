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

    [Header("Cárcel")]
    public int bailCost = 120;     // fianza
    int[] skipTurns = new int[2];  // turnos a saltar por jugador (J1=0, J2=1)

    [Header("Eventos (config v1) — 40% catástrofes / 60% estándar")]
    // Pool estándar reescalado a 60% total:
    // (Premio 12, Multa 9, Mover±3 9, Intercambio 6, Descuento 9, Buff 7, Debuff 8)
    public int evPremio = 12;
    public int evMulta = 9;
    public int evMover = 9;
    public int evIntercambio = 6;
    public int evDescuento = 9;
    public int evBuff = 7;
    public int evDebuff = 8;

    // Anti-tilt:
    // - Cooldown global: no dos catástrofes seguidas globalmente
    // - Cooldown por jugador: un jugador no puede recibir catástrofe en turnos consecutivos
    bool globalCatCooldown = false;   // si true, la próxima "Carta" forzará estándar
    int[] playerCatCooldown = new int[2]; // 1 = bloquea catástrofe para ese jugador en su próxima carta

    enum StdEventType { Premio, Multa, Mover, Intercambio, Descuento, Buff, Debuff }

    [Header("Infraestructura (especial comprable) — opción B")]
    public int[] infraIndices = { 6, 14, 24, 32 };
    public string[] infraNombres = { "Planta Reciclaje", "Parque Eólico", "Planta Solar", "Planta Hidroeléctrica" };
    public int[] infraPrecios = { 140, 160, 180, 200 };
    public int[] infraRentasBase = { 28, 32, 36, 40 };

    int InfraSlotOf(int idx)
    {
        for (int i = 0; i < infraIndices.Length; i++)
            if (infraIndices[i] == idx) return i;
        return -1;
    }
    int CountInfraOwned(int owner)
    {
        int c = 0;
        for (int i = 0; i < infraIndices.Length; i++)
            if (ownerByTile.Length > infraIndices[i] && ownerByTile[infraIndices[i]] == owner) c++;
        return c;
    }

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
        // Valores finales del README, alineados con el array 'indices' (1,3,5,...,39)
        int[] precios = {
    100, 120, 140, 160, 180, 200, 220, 240, 260, 280,
    300, 320, 350, 400, 420, 440, 460, 480, 500, 520
};
        int[] rentas = {
    20, 25, 28, 32, 36, 40, 44, 48, 52, 56,
    60, 64, 70, 80, 84, 88, 92, 96, 100, 104
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
                precio = precios[i],
                renta = rentas[i]

            };
        }
    }

    // ===== Nombres visibles por casilla (índice 0..39) =====
    static readonly string[] AGRO_NOMBRES_V1 = new string[40]
    {
    // 0..9
    "Start/Meta",     // 0  (Start)
    "Av. Central",    // 1  (Propiedad)
    "Evento",         // 2  (Evento)
    "Calle Mercado",  // 3  (Propiedad)
    "Premio",         // 4  (Premio)
    "Calle Río",      // 5  (Propiedad)
    "Planta Reciclaje", // 6  (Infra)
    "Calle Flores",   // 7  (Propiedad)
    "Impuesto",       // 8  (Tax)
    "Calle Sol",      // 9  (Propiedad)

    // 10..19
    "Cárcel",         // 10 (Visita / regla especial)
    "Calle Parque",   // 11 (Propiedad)
    "Robo",           // 12 (Robbery)
    "Calle Puerto",   // 13 (Propiedad)
    "Planta Eólica",  // 14 (Infra)
    "Calle Bosque",   // 15 (Propiedad)
    "Evento",         // 16 (Evento)
    "Av. Mercado",    // 17 (Propiedad)
    "Premio",         // 18 (Premio)
    "Calle Mayor",    // 19 (Propiedad)

    // 20..29
    "Descanso",       // 20 (Rest)
    "Calle Lago",     // 21 (Propiedad)
    "Robo",           // 22 (Robbery)
    "Av. Montaña",    // 23 (Propiedad)
    "Planta Solar",   // 24 (Infra)
    "Calle Real",     // 25 (Propiedad)
    "Evento",         // 26 (Evento)
    "Calle Corona",   // 27 (Propiedad)
    "Construcción",   // 28 (Construction)
    "Av. Castillo",   // 29 (Propiedad)

    // 30..39
"Ir a Cárcel",           // 30 (GoToJail)
"Calle Reina",           // 31 (Propiedad)
"Planta Hidroeléctrica", // 32 (Infra)
"Calle Rubí",            // 33 (Propiedad)
"Evento",                // 34 (Evento)
"Av. Granada",           // 35 (Propiedad)
"Mantenimiento",         // 36 (Maintenance)
"Calle Marina",          // 37 (Propiedad)
"Robo",                  // 38 (Robbery)
"Av. Emperador"          // 39 (Propiedad)

    };


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
        ApplyTileLabels();    // ← pinta los nombres en cada casilla

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

        // ¿Debe saltar el turno por cárcel?
        if (skipTurns[turno] > 0)
        {
            skipTurns[turno]--;
            if (txtEstado) txtEstado.text = $"Jugador {turno + 1} pierde el turno (cárcel).";
            turno = 1 - turno;
            UpdateTurnUI();
            return;
        }

        StartCoroutine(RollAndMove());
    }


    bool IsMovingAny()
    {
        return (p1 && p1.isMoving) || (p2 && p2.isMoving);
    }
    // Sortea un evento estándar según los pesos configurados (suman 60)
    StdEventType PickStandardEvent()
    {
        int total = evPremio + evMulta + evMover + evIntercambio + evDescuento + evBuff + evDebuff;
        int r = Random.Range(0, total);
        int acc = 0;

        acc += evPremio; if (r < acc) return StdEventType.Premio;
        acc += evMulta; if (r < acc) return StdEventType.Multa;
        acc += evMover; if (r < acc) return StdEventType.Mover;
        acc += evIntercambio; if (r < acc) return StdEventType.Intercambio;
        acc += evDescuento; if (r < acc) return StdEventType.Descuento;
        acc += evBuff; if (r < acc) return StdEventType.Buff;
        // lo que quede
        return StdEventType.Debuff;
    }

    // Resolver el evento ESTÁNDAR (por ahora implementamos Premio y Multa)
    IEnumerator ResolveStandardEvent(int jugador, StdEventType t)
    {
        switch (t)
        {
            case StdEventType.Premio:
                {
                    AddMoney(jugador, premioMonto);
                    if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} recibe Premio +${premioMonto}.";
                    bool done = false;
                    modal.Show("Premio", $"+${premioMonto}", "OK", "Cerrar", onYes: () => done = true, onNo: () => done = true);
                    yield return new WaitUntil(() => done);
                    break;
                }
            case StdEventType.Multa:
                {
                    PayToBank(jugador, premioMonto);
                    if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} paga Multa ${premioMonto}.";
                    bool done = false;
                    modal.Show("Multa", $"-${premioMonto}", "OK", "Cerrar", onYes: () => done = true, onNo: () => done = true);
                    yield return new WaitUntil(() => done);
                    break;
                }
            case StdEventType.Mover:
                {
                    // 50/50: +3 o -3
                    int delta = (Random.Range(0, 2) == 0) ? 3 : -3;
                    var tok = (jugador == 0) ? p1 : p2;

                    int idxAntes = tok.boardIndex;
                    int destino = Wrap(tok.boardIndex + delta);
                    string txt = (delta > 0) ? $"+{delta}" : delta.ToString();

                    // Bono por Start solo si avanza (igual que al tirar dado)
                    if (delta > 0)
                    {
                        int total = board.TileCount;
                        if (idxAntes + delta >= total) AddMoney(jugador, startBonus);
                    }

                    // Mover (v1: teleport + resolver destino)
                    tok.TeleportTo(board.PathPositions[destino], destino);
                    HighlightCurrentOnly();

                    if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} se mueve {txt} (Evento).";

                    // Pequeño modal informativo (opcional)
                    bool ack = false;
                    modal.Show("Mover", $"Te moviste {txt}.", "OK", "Seguir",
                        onYes: () => ack = true, onNo: () => ack = true);
                    yield return new WaitUntil(() => ack);

                    // ===== Resolver la casilla de destino =====
                    var tile = board.GetTile(destino);
                    if (!tile) break;

                    // --- Propiedad (comprar / pagar renta con monopolio +50%) ---
                    if (tile.type == TileType.Property)
                    {
                        int idx = tile.index;
                        int dueno = ownerByTile[idx];

                        if (dueno == -1)
                        {
                            // Ofrecer compra
                            esperandoDecision = true;
                            var info = GetProp(idx);
                            int price = info?.precio ?? demoPropertyPrice;
                            int rent = info?.renta ?? demoPropertyRent;

                            string titulo = info != null ? $"¿Comprar {info.nombre}?" : "¿Comprar propiedad?";
                            string cuerpo = $"Precio ${price} • Renta ${rent}";

                            modal.Show(
                                titulo, cuerpo, "Comprar", "Pasar",
                                onYes: () =>
                                {
                                    if (SpendMoney(jugador, price))
                                    {
                                        ownerByTile[idx] = jugador;
                                        var color = (jugador == 0) ? ownerColorP1 : ownerColorP2;
                                        tile.SetOwnerMark(color, true);
                                        if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} compró {(info != null ? info.nombre : $"la casilla {idx}")} por ${price}.";
                                    }
                                    else
                                    {
                                        Debug.Log("No alcanza el dinero.");
                                    }
                                    esperandoDecision = false;
                                },
                                onNo: () => { esperandoDecision = false; }
                            );
                            yield return new WaitUntil(() => esperandoDecision == false);
                        }
                        else if (dueno != jugador)
                        {
                            var info = GetProp(idx);
                            int baseRent = info?.renta ?? demoPropertyRent;
                            bool parCompleto = OwnsPair(dueno, idx);
                            int rent = parCompleto ? Mathf.RoundToInt(baseRent * 1.5f) : baseRent;

                            PayPlayerToPlayer(jugador, dueno, rent);
                            if (txtEstado) txtEstado.text =
                                $"Jugador {jugador + 1} pagó renta ${rent} a Jugador {dueno + 1}" +
                                (info != null ? $" ({info.nombre})" : "") +
                                (parCompleto ? " (+50% por monopolio)" : "") + ".";
                        }
                        break;
                    }

                    // --- Infraestructura (comprable; renta = base × cantidad poseída por el dueño) ---
                    if (tile.type == TileType.Infrastructure)
                    {
                        int idx = tile.index;
                        int slot = InfraSlotOf(idx);
                        if (slot >= 0)
                        {
                            int dueno = ownerByTile[idx];

                            if (dueno == -1)
                            {
                                esperandoDecision = true;
                                string nom = infraNombres[slot];
                                int price = infraPrecios[slot];
                                int baseRent = infraRentasBase[slot];

                                modal.Show(
                                    $"¿Comprar {nom}?",
                                    $"Precio ${price} • Renta base ${baseRent}\n(La renta escala por cantidad total de infra que posea el dueño)",
                                    "Comprar", "Pasar",
                                    onYes: () =>
                                    {
                                        if (SpendMoney(jugador, price))
                                        {
                                            ownerByTile[idx] = jugador;
                                            var color = (jugador == 0) ? ownerColorP1 : ownerColorP2;
                                            tile.SetOwnerMark(color, true);
                                            if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} compró {nom} por ${price}.";
                                        }
                                        else
                                        {
                                            Debug.Log("No alcanza el dinero para infraestructura.");
                                        }
                                        esperandoDecision = false;
                                    },
                                    onNo: () => { esperandoDecision = false; }
                                );
                                yield return new WaitUntil(() => esperandoDecision == false);
                            }
                            else if (dueno != jugador)
                            {
                                int owned = CountInfraOwned(dueno);
                                int rent = infraRentasBase[slot] * Mathf.Max(1, owned);
                                PayPlayerToPlayer(jugador, dueno, rent);

                                if (txtEstado) txtEstado.text =
                                    $"Jugador {jugador + 1} pagó renta ${rent} de infraestructura a Jugador {dueno + 1} ({infraNombres[slot]}, posee {owned}).";
                            }
                        }
                        break;
                    }

                    // --- Evento (permite encadenar, con nuestros cooldowns) ---
                    if (tile.type == TileType.Event)
                    {
                        bool forceStandard = globalCatCooldown || (playerCatCooldown[jugador] > 0);
                        bool isCat = false;
                        if (!forceStandard)
                        {
                            int rollCat = Random.Range(0, 100);
                            isCat = (rollCat < 40);
                        }

                        if (isCat)
                        {
                            globalCatCooldown = true;
                            playerCatCooldown[jugador] = 1;
                            yield return ResolveCatastrophe(jugador);
                        }
                        else
                        {
                            if (globalCatCooldown) globalCatCooldown = false;
                            if (playerCatCooldown[jugador] > 0) playerCatCooldown[jugador] = 0;

                            var ev2 = PickStandardEvent();
                            yield return ResolveStandardEvent(jugador, ev2);
                        }
                        break;
                    }

                    // --- Premios/Impuestos/Descanso (pares) ---
                    if (tile.type == TileType.Prize)
                    {
                        AddMoney(jugador, premioMonto);
                        bool done = false;
                        modal.Show("Premio", $"+${premioMonto}", "OK", "Cerrar",
                            onYes: () => done = true, onNo: () => done = true);
                        yield return new WaitUntil(() => done);
                        if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} cobró premio ${premioMonto} (casilla {tile.index}).";
                        break;
                    }
                    if (tile.type == TileType.Tax)
                    {
                        PayToBank(jugador, impuestoMonto);
                        bool done = false;
                        modal.Show("Impuesto", $"${impuestoMonto}", "OK", "Cerrar",
                            onYes: () => done = true, onNo: () => done = true);
                        yield return new WaitUntil(() => done);
                        if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} pagó impuesto ${impuestoMonto} (casilla {tile.index}).";
                        break;
                    }
                    if (tile.type == TileType.Rest)
                    {
                        AddMoney(jugador, restBonus);
                        bool done = false;
                        modal.Show("Descanso", $"+${restBonus}", "OK", "Cerrar",
                            onYes: () => done = true, onNo: () => done = true);
                        yield return new WaitUntil(() => done);
                        if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} descansó y cobró ${restBonus} (casilla {tile.index}).";
                        break;
                    }

                    // --- Cárcel / Ir a Cárcel ---
                    if (tile.type == TileType.GoToJail)
                    {
                        tok.TeleportTo(board.PathPositions[10], 10);
                        skipTurns[jugador] = 1;
                        if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} va a Cárcel y perderá 1 turno.";
                        break;
                    }
                    if (tile.type == TileType.Jail)
                    {
                        bool done = false;
                        modal.Show(
                            "Cárcel",
                            $"Pagar fianza ${bailCost} o perder 1 turno",
                            "Pagar",
                            "Perder turno",
                            onYes: () =>
                            {
                                if (SpendMoney(jugador, bailCost))
                                {
                                    if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} pagó fianza y sigue.";
                                }
                                else
                                {
                                    skipTurns[jugador] = 1;
                                    if (txtEstado) txtEstado.text = $"No alcanzó para la fianza. Jugador {jugador + 1} pierde 1 turno.";
                                }
                                done = true;
                            },
                            onNo: () => { skipTurns[jugador] = 1; done = true; }
                        );
                        yield return new WaitUntil(() => done);
                        break;
                    }

                    // (Robo / Construcción / Mantenimiento los añadimos en pasos siguientes)
                    break;
                }

            default:
                {
                    // Placeholder: iremos implementando estos en micro-pasos
                    bool done = false;
                    modal.Show("Evento", "Efecto en preparación (próximo paso).", "OK", "Cerrar",
                        onYes: () => done = true, onNo: () => done = true);
                    yield return new WaitUntil(() => done);
                    break;
                }
        }
        yield break;
    }

    // Resolver Catástrofe (placeholder por ahora, con cooldowns listos)
    IEnumerator ResolveCatastrophe(int jugador)
    {
        // Aquí luego agregamos: severidad (L/M/S) + tipo (Inundación/Sequía/Tormenta/Granizo) y efectos
        if (txtEstado) txtEstado.text = $"Jugador {jugador + 1} — Catástrofe (placeholder).";
        bool done = false;
        modal.Show("Catástrofe", "Se aplicará en el siguiente paso (implementación).", "OK", "Cerrar",
            onYes: () => done = true, onNo: () => done = true);
        yield return new WaitUntil(() => done);
        yield break;
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
        // Ir a Cárcel (30): ir a 10 y perder 1 turno (sin opción de fianza aquí)
        if (landedTile && landedTile.type == TileType.GoToJail)
        {
            player.TeleportTo(board.PathPositions[10], 10);
            skipTurns[turno] = 1;
            if (txtEstado) txtEstado.text = $"Jugador {turno + 1} va a Cárcel y perderá 1 turno.";

            // Termina el turno inmediatamente
            turno = 1 - turno;
            UpdateTurnUI();
            if (btnTirar) btnTirar.interactable = true;
            yield break;
        }

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
                    int rent = parCompleto ? Mathf.RoundToInt(baseRent * 1.5f) : baseRent;

                    PayPlayerToPlayer(turno, duenoActual, rent);
                    if (txtEstado) txtEstado.text =
                        $"Jugador {turno + 1} pagó renta ${rent} a Jugador {duenoActual + 1}" +
                        (info != null ? $" ({info.nombre})" : "") +
                        (parCompleto ? " (+50% por monopolio)" : "") + ".";
                }

                // Si es tu propia propiedad, no pasa nada
            }

        }
        // --- Infraestructura (pares especiales comprables) ---
        if (landedTile && landedTile.type == TileType.Infrastructure)
        {
            int idx = landedTile.index;
            int slot = InfraSlotOf(idx);
            if (slot >= 0)
            {
                int duenoActual = ownerByTile[idx];

                if (duenoActual == -1)
                {
                    // Ofrecer compra
                    esperandoDecision = true;
                    string nom = infraNombres[slot];
                    int price = infraPrecios[slot];
                    int baseRent = infraRentasBase[slot];

                    modal.Show(
                        $"¿Comprar {nom}?",
                        $"Precio ${price} • Renta base ${baseRent}\n(La renta escala por cantidad total de infra que posea el dueño)",
                        "Comprar",
                        "Pasar",
                        onYes: () =>
                        {
                            if (SpendMoney(turno, price))
                            {
                                ownerByTile[idx] = turno;
                                var colorDueno = (turno == 0) ? ownerColorP1 : ownerColorP2;
                                landedTile.SetOwnerMark(colorDueno, true);
                                if (txtEstado) txtEstado.text = $"Jugador {turno + 1} compró {nom} por ${price}.";
                            }
                            else
                            {
                                Debug.Log("No alcanza el dinero para infraestructura.");
                            }
                            esperandoDecision = false;
                        },
                        onNo: () => { esperandoDecision = false; }
                    );
                    yield return new WaitUntil(() => esperandoDecision == false);
                }
                else if (duenoActual != turno)
                {
                    // Opción B: renta = rentaBaseDeEsaCasilla × (infra poseídas por el dueño)
                    int owned = CountInfraOwned(duenoActual);
                    int rent = infraRentasBase[slot] * Mathf.Max(1, owned);
                    PayPlayerToPlayer(turno, duenoActual, rent);

                    if (txtEstado) txtEstado.text =
                        $"Jugador {turno + 1} pagó renta ${rent} de infraestructura a Jugador {duenoActual + 1} ({infraNombres[slot]}, posee {owned}).";
                }
                // Si es tuya, no pasa nada
            }
        }
        // === Evento (2,16,26,34): 40% Catástrofe / 60% Estándar con anti-tilt ===
        if (landedTile && landedTile.type == TileType.Event)
        {
            int jugador = turno;

            // ¿Catástrofe o estándar?
            bool forceStandard = globalCatCooldown || (playerCatCooldown[jugador] > 0);
            bool isCat = false;

            if (!forceStandard)
            {
                // 40% catástrofe, 60% estándar
                int rollCat = Random.Range(0, 100);
                isCat = (rollCat < 40);
            }

            if (isCat)
            {
                // Catástrofe → activar cooldowns
                globalCatCooldown = true;           // la próxima carta (de quien sea) será estándar
                playerCatCooldown[jugador] = 1;     // este jugador no puede recibir catástrofe en su próxima carta
                yield return ResolveCatastrophe(jugador);
            }
            else
            {
                // Estándar → limpiar cooldowns si estaban activos
                if (globalCatCooldown) globalCatCooldown = false;
                if (playerCatCooldown[jugador] > 0) playerCatCooldown[jugador] = 0;

                var ev = PickStandardEvent();
                yield return ResolveStandardEvent(jugador, ev);
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

                        // Modal informativo y esperar que lo cierre
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

                case 10:
                    {
                        // Cárcel: elegir fianza o perder 1 turno
                        esperandoDecision = true;
                        modal.Show(
                            "Cárcel",
                            $"Pagar fianza ${bailCost} o perder 1 turno",
                            "Pagar",
                            "Perder turno",
                            onYes: () =>
                            {
                                if (SpendMoney(turno, bailCost))
                                {
                                    if (txtEstado) txtEstado.text = $"Jugador {turno + 1} pagó fianza y sigue jugando.";
                                }
                                else
                                {
                                    skipTurns[turno] = 1;
                                    if (txtEstado) txtEstado.text = $"No alcanzó para la fianza. Jugador {turno + 1} pierde 1 turno.";
                                }
                                esperandoDecision = false;
                            },
                            onNo: () =>
                            {
                                skipTurns[turno] = 1;
                                esperandoDecision = false;
                            }
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

                    // Otros pares (22, 26, 28, 30, 32, 34, 36, 38) se manejan en otros flujos o aún no implementados aquí.
            }
        }


        int prevJugador = turno;  // el que acaba de jugar
        turno = 1 - turno;

        // Consumir cooldown por jugador (si tenía bloqueo de catástrofe, se limpia ahora)
        if (playerCatCooldown[prevJugador] > 0)
            playerCatCooldown[prevJugador] = 0;

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

    // --- NUEVO: aplica los nombres a cada casilla (fuera de Start) ---
    void ApplyTileLabels()
    {
        int n = board.TileCount;
        for (int i = 0; i < n && i < AGRO_NOMBRES_V1.Length; i++)
        {
            var t = board.GetTile(i);
            if (t) t.SetLabel(AGRO_NOMBRES_V1[i]);
        }
    }

    // Muestra "00..39" en el hijo "Label" de cada casilla (útil para debug, NO llamar en Start)
    void ShowTileIndices()
    {
        for (int i = 0; i < board.TileCount; i++)
        {
            var tile = board.GetTile(i);
            if (!tile) continue;

            var label = tile.transform.Find("Label")?.GetComponent<TMPro.TextMeshPro>();
            if (label)
                label.text = i.ToString("00"); // 00, 01, 02...
        }
    }
}

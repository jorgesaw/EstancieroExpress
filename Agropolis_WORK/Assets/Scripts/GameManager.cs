using System.Collections;
using System.Collections.Generic; // ← NUEVO
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
    public ToastUI toast;   // ← arrastra aquí el objeto "Toast" de la escena
    public RobberyUI robberyUI;

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

    [Header("Construcción")]
    public int constructionCost = 150;     // cuánto cuesta mejorar
    public float constructionMult = 1.75f; // renta x1.75

    // Propiedades mejoradas por tile index (true = mejorada)
    // (Guardamos por casilla, así la mejora “viaja” con la propiedad si se intercambia)
    bool[] upgraded = new bool[40];

    // Helpers
    bool IsUpgraded(int idx) => (idx >= 0 && idx < upgraded.Length) && upgraded[idx];
    float GetUpgradeMultForTile(int idx) => IsUpgraded(idx) ? constructionMult : 1f;

    [Header("Especiales (demo)")]
    public int premioMonto = 100;
    public int multaMonto = 100;     // ← NUEVO: monto de la Multa (evento estándar)
    public int impuestoMonto = 80;   // ← NUEVO
    public int restBonus = 50;   // ← NUEVO (Descanso)

    [Header("Mantenimiento")]
    public int maintenanceCostPerProperty = 10; // costo por propiedad poseída

    [Header("Robo")]
    public int robberyWin = 80;   // lo que ganas si te sale la carta “Robar”
    public int robberyLose = 60;  // lo que pierdes si te sale “Te roban”

    [Header("Cárcel")]
    public int bailCost = 120;     // fianza
    int[] skipTurns = new int[2];  // turnos a saltar por jugador (J1=0, J2=1)

    [Header("Power-ups")]
    public int discountPercent = 30;       // -30% a la PRÓXIMA compra
    bool[] hasDiscount = new bool[2];      // slot por jugador (J1=0, J2=1)

    // Precio efectivo si hay descuento
    int EffectivePrice(int jugador, int basePrice)
    {
        if (hasDiscount[jugador])
            return Mathf.RoundToInt(basePrice * (100 - discountPercent) / 100f);
        return basePrice;
    }

    // Consumir descuento tras usarlo
    void ConsumeDiscount(int jugador) { hasDiscount[jugador] = false; }

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

    [Header("Catástrofes (config v1) — 40% del total")]
    public int catDesastre = 30;      // quita 1 mejora aleatoria
    public int catReparaciones = 30;  // paga 2× mantenimiento por propiedad
    public int catEmbargo = 25;       // paga 20% del dinero actual
    public int catParo = 15;          // limpia buff/debuff de renta

    // ----- Buff/Debuff de renta (propiedades) -----
    [Header("Buff/Debuff Renta")]
    public int rentModDurationLaps = 1;    // dura 1 vuelta

    enum RentMod { None, Buff, Debuff }
    RentMod[] rentMod = new RentMod[2];    // por jugador (J1=0, J2=1)
    int[] rentModLapsLeft = new int[2];
  
    // --- Seguimiento por casilla de origen del efecto ---
    int[] rentModExpireTile = new int[2]; // casilla Evento donde se activó (−1 = ninguno)
    bool[] rentModArmed = new bool[2]; // se “arma” al salir de esa casilla

    // Activadores
    void GiveRentBuff(int player)
    {
        rentMod[player] = RentMod.Buff;
        rentModLapsLeft[player] = rentModDurationLaps;

        // === NUEVO: casilla donde se activó y “armado” pendiente ===
        rentModExpireTile[player] = (player == 0 ? p1 : p2).boardIndex;
        rentModArmed[player] = false;

        ShowToast($"Jugador {player + 1}: +25% de renta por {rentModDurationLaps} vuelta(s)", 2.5f);
    }


    void GiveRentDebuff(int player)
    {
        rentMod[player] = RentMod.Debuff;
        rentModLapsLeft[player] = rentModDurationLaps;

        // === NUEVO: casilla donde se activó y “armado” pendiente ===
        rentModExpireTile[player] = (player == 0 ? p1 : p2).boardIndex;
        rentModArmed[player] = false;

        ShowToast($"Jugador {player + 1}: -25% de renta por {rentModDurationLaps} vuelta(s)", 2.5f);
    }


    // Multiplicador según dueño que cobra
    float GetRentModMultiplierForOwner(int owner)
    {
        return rentMod[owner] == RentMod.Buff ? 1.25f :
               rentMod[owner] == RentMod.Debuff ? 0.75f : 1f;
    }

    // Tick de duración: se descuenta cuando el JUGADOR pasa por Start
    void AdvanceRentModLapIfPassedStart(int player, bool pasoPorStart)
    {
        if (!pasoPorStart) return;
        if (rentModLapsLeft[player] > 0)
        {
            rentModLapsLeft[player]--;
            if (rentModLapsLeft[player] == 0) rentMod[player] = RentMod.None;
        }
    }

    // Etiqueta breve para el mensaje de estado
    // Etiqueta breve para el mensaje de estado (con vueltas restantes)
    string RentModTag(int owner)
    {
        var type = rentMod[owner];
        if (type == RentMod.None) return "";

        int laps = Mathf.Max(0, rentModLapsLeft[owner]);
        string lapsTxt = laps > 0 ? (laps == 1 ? ", 1 vuelta" : $", {laps} vueltas") : "";

        return type == RentMod.Buff
            ? $" (+25% buff{lapsTxt})"
            : $" (-25% debuff{lapsTxt})";
    }

    // “Arma” el efecto cuando el jugador abandona la casilla de origen (inicio del siguiente turno).
    void ArmRentModIfNeededAtTurnStart(int player)
    {
        if (rentModExpireTile[player] != -1 && !rentModArmed[player])
            rentModArmed[player] = true;
    }

    // Si el jugador aterriza en la casilla de origen y el efecto estaba armado, consume una “vuelta”
    void TryExpireRentModOnLanding(int player, int tileIndex)
    {
        if (rentModExpireTile[player] == -1) return;
        if (!rentModArmed[player]) return;
        if (tileIndex != rentModExpireTile[player]) return;

        if (rentModLapsLeft[player] > 0)
        {
            rentModLapsLeft[player]--;
            if (rentModLapsLeft[player] == 0)
            {
                rentMod[player] = RentMod.None;
                rentModExpireTile[player] = -1;
                rentModArmed[player] = false;
                ShowToast($"Fin del efecto de renta para Jugador {player + 1}.", 2.0f);
            }
        }
    }
    [Header("Infraestructura (especial comprable) — opción B")]
    public int[] infraIndices = { 6, 14, 24, 32 };
    public string[] infraNombres = { "Planta Reciclaje", "Parque Eólico", "Planta Solar", "Planta Hidroeléctrica" };
    public int[] infraPrecios = { 140, 160, 180, 200 };
    public int[] infraRentasBase = { 28, 32, 36, 40 };

    int CountOwnedProps(int owner)
    {
        int c = 0;
        // Solo impares = propiedades
        for (int i = 1; i < ownerByTile.Length; i += 2)
            if (ownerByTile[i] == owner) c++;
        return c;
    }
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
        for (int i = 0; i < 2; i++)
        {
            rentModExpireTile[i] = -1;
            rentModArmed[i] = false;
        }

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
                    ToastStatus($"Jugador {jugador + 1} recibe Premio +${premioMonto}.", 3f);

                    bool done = false;
                    modal.Show("Premio", $"+${premioMonto}", "OK", "Cerrar",
                        onYes: () => done = true, onNo: () => done = true);
                    yield return new WaitUntil(() => done);
                    break;
                }

            case StdEventType.Multa:
                {
                    PayToBank(jugador, multaMonto);
                    ToastStatus($"Jugador {jugador + 1} paga Multa ${multaMonto}.", 3f);

                    bool done = false;
                    modal.Show("Multa", $"-${multaMonto}", "OK", "Cerrar",
                        onYes: () => done = true, onNo: () => done = true);
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
                        if (idxAntes + delta >= total)
                        {
                            AddMoney(jugador, startBonus);
                            ToastStatus($"+${startBonus} por pasar Start", 2f);
                        }
                    }

                    // Mover (v1: teleport + resolver destino)
                    tok.TeleportTo(board.PathPositions[destino], destino);
                    HighlightCurrentOnly();

                    ToastStatus($"Jugador {jugador + 1} se mueve {txt} (Evento).", 2.5f);

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
                            var info = GetProp(idx);
                            int price = info?.precio ?? demoPropertyPrice;
                            int rent = info?.renta ?? demoPropertyRent;

                            int finalPrice = EffectivePrice(jugador, price);
                            bool tieneDesc = hasDiscount[jugador];

                            string titulo = info != null ? $"¿Comprar {info.nombre}?" : "¿Comprar propiedad?";
                            string cuerpo = tieneDesc
                                ? $"Precio ${price} → con descuento −{discountPercent}%: ${finalPrice}\nRenta ${rent}"
                                : $"Precio ${price} • Renta ${rent}";

                            esperandoDecision = true;
                            modal.Show(
                                titulo, cuerpo, "Comprar", "Pasar",
                                onYes: () =>
                                {
                                    if (SpendMoney(jugador, finalPrice))
                                    {
                                        if (tieneDesc) ConsumeDiscount(jugador);
                                        ownerByTile[idx] = jugador;
                                        var color = (jugador == 0) ? ownerColorP1 : ownerColorP2;
                                        tile.SetOwnerMark(color, true);
                                        ToastStatus($"Jugador {jugador + 1} compró {(info != null ? info.nombre : $"la casilla {idx}")} por ${finalPrice}.", 3f);
                                    }
                                    else
                                    {
                                        ToastStatus("No alcanza el dinero para comprar.", 2.5f);
                                    }
                                    esperandoDecision = false;
                                },
                                onNo: () => { esperandoDecision = false; }
                            );
                            yield return new WaitUntil(() => esperandoDecision == false);
                        }
                        // (PROPIEDADES) si el dueño es el rival, cobro renta v1
                        else if (dueno != jugador)
                        {
                            var info = GetProp(idx);
                            int baseRent = info?.renta ?? demoPropertyRent;
                            bool parCompleto = OwnsPair(dueno, idx);

                            float r = parCompleto ? baseRent * 1.5f : baseRent;

                            // NUEVO: multiplicador por mejora de construcción
                            float up = GetUpgradeMultForTile(idx);
                            r *= up;

                            // Buff/Debuff del dueño que cobra
                            float mod = GetRentModMultiplierForOwner(dueno);
                            r *= mod;

                            int rent = Mathf.RoundToInt(r);

                            // Toast con desglose
                            string monoTag = parCompleto ? " ×1.5 (monopolio)" : "";
                            string upTag = up > 1f ? $" ×{up:0.##} (mejora)" : "";
                            string modTag = mod > 1f ? " ×1.25 (buff)" : (mod < 1f ? " ×0.75 (debuff)" : "");
                            ShowToast($"Alquiler base ${baseRent}{monoTag}{upTag}{modTag} → Total ${rent}", 3f);

                            PayPlayerToPlayer(jugador, dueno, rent);
                            ToastStatus(
                                $"Jugador {jugador + 1} pagó renta ${rent} a Jugador {dueno + 1}"
                                + (info != null ? $" ({info.nombre})" : ""),
                                3f
                            );
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
                                string nom = infraNombres[slot];
                                int price = infraPrecios[slot];
                                int baseRent = infraRentasBase[slot];

                                int finalPrice = EffectivePrice(jugador, price);
                                bool tieneDesc = hasDiscount[jugador];

                                esperandoDecision = true;
                                modal.Show(
                                    $"¿Comprar {nom}?",
                                    (tieneDesc
                                        ? $"Precio ${price} → con descuento −{discountPercent}%: ${finalPrice}\nRenta base ${baseRent}\n(La renta escala por cantidad total de infra del dueño)"
                                        : $"Precio ${price} • Renta base ${baseRent}\n(La renta escala por cantidad total de infra del dueño)"),
                                    "Comprar", "Pasar",
                                    onYes: () =>
                                    {
                                        if (SpendMoney(jugador, finalPrice))
                                        {
                                            if (tieneDesc) ConsumeDiscount(jugador);
                                            ownerByTile[idx] = jugador;
                                            var color = (jugador == 0) ? ownerColorP1 : ownerColorP2;
                                            tile.SetOwnerMark(color, true);
                                            ToastStatus($"Jugador {jugador + 1} compró {nom} por ${finalPrice}.", 3f);
                                        }
                                        else
                                        {
                                            ToastStatus("No alcanza el dinero para infraestructura.", 2.5f);
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
                                int baseRent = infraRentasBase[slot];
                                int rent = baseRent * Mathf.Max(1, owned);

                                // Toast con el desglose de INFRA
                                ToastStatus($"Infra: base ${baseRent} × posee {owned} = ${rent}", 3f);

                                PayPlayerToPlayer(jugador, dueno, rent);
                                ToastStatus($"Jugador {jugador + 1} pagó renta ${rent} de infraestructura a Jugador {dueno + 1} ({infraNombres[slot]}, posee {owned}).", 3f);
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
                        ToastStatus($"Jugador {jugador + 1} cobró premio +${premioMonto} (casilla {tile.index}).", 3f);

                        bool done = false;
                        modal.Show("Premio", $"+${premioMonto}", "OK", "Cerrar",
                            onYes: () => done = true, onNo: () => done = true);
                        yield return new WaitUntil(() => done);
                        break;
                    }
                    if (tile.type == TileType.Tax)
                    {
                        PayToBank(jugador, impuestoMonto);
                        ToastStatus($"Jugador {jugador + 1} pagó impuesto ${impuestoMonto} (casilla {tile.index}).", 3f);

                        bool done = false;
                        modal.Show("Impuesto", $"${impuestoMonto}", "OK", "Cerrar",
                            onYes: () => done = true, onNo: () => done = true);
                        yield return new WaitUntil(() => done);
                        break;
                    }
                    if (tile.type == TileType.Rest)
                    {
                        AddMoney(jugador, restBonus);
                        ToastStatus($"Jugador {jugador + 1} descansó y cobró ${restBonus} (casilla {tile.index}).", 3f);

                        bool done = false;
                        modal.Show("Descanso", $"+${restBonus}", "OK", "Cerrar",
                            onYes: () => done = true, onNo: () => done = true);
                        yield return new WaitUntil(() => done);
                        break;
                    }
                    // --- Robo ---
                    if (tile.type == TileType.Robbery)
                    {
                        yield return ResolveRobbery(jugador);
                        break;
                    }
                    // --- Cárcel / Ir a Cárcel ---
                    if (tile.type == TileType.GoToJail)
                    {
                        tok.TeleportTo(board.PathPositions[10], 10);
                        skipTurns[jugador] = 1;
                        ToastStatus($"Jugador {jugador + 1} va a Cárcel y perderá 1 turno.", 3f);
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
                                    ToastStatus($"Jugador {jugador + 1} pagó fianza ${bailCost} y sigue jugando.", 3f);
                                }
                                else
                                {
                                    skipTurns[jugador] = 1;
                                    ToastStatus($"No alcanzó para la fianza. Jugador {jugador + 1} pierde 1 turno.", 3f);
                                }
                                done = true;
                            },
                            onNo: () =>
                            {
                                skipTurns[jugador] = 1;
                                ToastStatus($"Jugador {jugador + 1} perdió 1 turno (cárcel).", 3f);
                                done = true;
                            }
                        );
                        yield return new WaitUntil(() => done);
                        break;
                    }

                    // (Robo / Construcción / Mantenimiento los añadimos en pasos siguientes)
                    break;
                }

            case StdEventType.Intercambio:
                {
                    // Rival: elegimos aleatoriamente ENTRE los jugadores que tengan al menos 1 propiedad.
                    // Esto funciona con 2 jugadores HOY y escalará a 3–4 mañana sin tocar esta lógica.

                    // 1) Mis propiedades (solo casillas impares = propiedades)
                    List<int> misProps = new List<int>();
                    for (int i = 1; i < ownerByTile.Length; i += 2)
                        if (ownerByTile[i] == jugador) misProps.Add(i);

                    // 2) Propiedades por oponente (clave = ownerId)
                    Dictionary<int, List<int>> propsPorOponente = new Dictionary<int, List<int>>();
                    for (int i = 1; i < ownerByTile.Length; i += 2)
                    {
                        int own = ownerByTile[i];
                        if (own >= 0 && own != jugador)
                        {
                            if (!propsPorOponente.TryGetValue(own, out var lista))
                            {
                                lista = new List<int>();
                                propsPorOponente[own] = lista;
                            }
                            lista.Add(i);
                        }
                    }

                    // 3) Fallback: si yo NO tengo o NADIE rival tiene propiedades → Premio +$80
                    if (misProps.Count == 0 || propsPorOponente.Count == 0)
                    {
                        int premioFallback = 80;
                        AddMoney(jugador, premioFallback);
                        ToastStatus($"Intercambio no posible. Jugador {jugador + 1} recibe premio +${premioFallback}.", 3f);

                        bool ok = false;
                        modal.Show("Premio", $"+${premioFallback}", "OK", "Cerrar",
                            onYes: () => ok = true, onNo: () => ok = true);
                        yield return new WaitUntil(() => ok);
                        break;
                    }

                    // 4) Elegir rival al azar ENTRE quienes tengan propiedades
                    var rivalesConProps = new List<int>(propsPorOponente.Keys);
                    int rival = rivalesConProps[Random.Range(0, rivalesConProps.Count)];

                    // 5) Elegir 1 propiedad mía y 1 del rival al azar
                    int idxA = misProps[Random.Range(0, misProps.Count)];
                    var susProps = propsPorOponente[rival];
                    int idxB = susProps[Random.Range(0, susProps.Count)];

                    var propA = GetProp(idxA);
                    var propB = GetProp(idxB);
                    string nombreA = propA != null ? propA.nombre : $"Prop {idxA}";
                    string nombreB = propB != null ? propB.nombre : $"Prop {idxB}";

                    // 6) Intercambiar dueños
                    ownerByTile[idxA] = rival;
                    ownerByTile[idxB] = jugador;

                    // 7) Actualizar marquitas visuales (nota: con >2 jugadores habrá que mapear color por ownerId)
                    var tA = board.GetTile(idxA);
                    if (tA) tA.SetOwnerMark(rival == 0 ? ownerColorP1 : ownerColorP2, true);
                    var tB = board.GetTile(idxB);
                    if (tB) tB.SetOwnerMark(jugador == 0 ? ownerColorP1 : ownerColorP2, true);

                    ToastStatus($"Intercambio: Jugador {jugador + 1} da {nombreA} ↔ recibe {nombreB} de Jugador {rival + 1}.", 3f);

                    bool done = false;
                    modal.Show("Intercambio", $"Entregaste: {nombreA}\nRecibiste: {nombreB}", "OK", "Cerrar",
                        onYes: () => done = true, onNo: () => done = true);
                    yield return new WaitUntil(() => done);
                    break;
                }

            case StdEventType.Descuento:
                {
                    if (!hasDiscount[jugador])
                    {
                        hasDiscount[jugador] = true;
                        ToastStatus($"Jugador {jugador + 1} obtuvo Descuento −{discountPercent}% en la próxima compra.", 3f);

                        bool ok = false;
                        modal.Show("Power-up: Descuento",
                            $"Se aplicará automáticamente a tu próxima compra.\nValor: −{discountPercent}%",
                            "OK", "Cerrar",
                            onYes: () => ok = true, onNo: () => ok = true);
                        yield return new WaitUntil(() => ok);
                    }
                    else
                    {
                        ToastStatus("Descuento ya activo: no se acumula ni se renueva.", 3f);

                        bool ok = false;
                        modal.Show("Descuento ya activo",
                            "Ya tienes un Descuento guardado.\n(No se acumula ni se renueva)", "OK", "Cerrar",
                            onYes: () => ok = true, onNo: () => ok = true);
                        yield return new WaitUntil(() => ok);
                    }
                    break;
                }

            case StdEventType.Buff:
                {
                    GiveRentBuff(jugador); // ya toastea adentro
                    bool ok = false;
                    modal.Show("Buff de renta",
                        $"+25% en tus propiedades por {rentModDurationLaps} vuelta.",
                        "OK", "Cerrar",
                        onYes: () => ok = true, onNo: () => ok = true);
                    yield return new WaitUntil(() => ok);
                    break;
                }

            case StdEventType.Debuff:
                {
                    GiveRentDebuff(jugador); // ya toastea adentro
                    bool ok = false;
                    modal.Show("Debuff de renta",
                        $"-25% en tus propiedades por {rentModDurationLaps} vuelta.",
                        "OK", "Cerrar",
                        onYes: () => ok = true, onNo: () => ok = true);
                    yield return new WaitUntil(() => ok);
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

    // --- Robo: UI de 3 cartas + lógica simple ---
    IEnumerator ResolveRobbery(int jugador)
    {
        int rival = (jugador == 0) ? 1 : 0;
        bool done = false;

        // Muestra la UI con 3 cartas. El callback nos da 0/1/2 según la carta elegida.
        robberyUI.Show(
            "Robo",
            "Elige una carta:",
            "Robar +" + robberyWin,   // Carta A
            "Nada",                   // Carta B
            "Te roban -" + robberyLose, // Carta C
            (pick) =>
            {
                switch (pick)
                {
                    case 0: // Robar al rival
                        PayPlayerToPlayer(rival, jugador, robberyWin);
                        ToastStatus($"Jugador {jugador + 1} robó ${robberyWin} a Jugador {rival + 1}.", 3f);
                        break;

                    case 1: // Nada
                        ToastStatus("Nada pasó.", 2f);
                        break;

                    case 2: // Te roban
                        PayPlayerToPlayer(jugador, rival, robberyLose);
                        ToastStatus($"A Jugador {jugador + 1} le robaron ${robberyLose}.", 3f);
                        break;

                    default: // Cerrar sin elegir
                        ToastStatus("Robo cancelado.", 2f);
                        break;
                }

                done = true;
            }
        );

        yield return new WaitUntil(() => done);
    }

         // Resolver Catástrofe (ya se llama sólo cuando "isCat" es true)
    IEnumerator ResolveCatastrophe(int jugador)
    {
        // Elegimos tipo según pesos configurados
        int total = catDesastre + catReparaciones + catEmbargo + catParo;
        int r = Random.Range(0, Mathf.Max(1, total));
        string titulo = "Catástrofe";
        string cuerpo = "";
        System.Action apply = null;

        int acc = 0;

        // 1) DesastreNatural: quita 1 MEJORA aleatoria; si no hay, paga Multa
        acc += catDesastre;
        if (r < acc)
        {
            int idx = GetRandomUpgradedProp(jugador);
            if (idx >= 0)
            {
                upgraded[idx] = false;
                var info = GetProp(idx);
                string nombre = info != null ? info.nombre : $"Prop {idx}";
                cuerpo = $"Desastre natural en {nombre}.\nPerdiste la mejora.";
                apply = () =>
                {
                    ShowToast($"Desastre: se perdió mejora en {nombre}.", 3f);
                };
            }
            else
            {
                // fallback: Multa estándar
                cuerpo = $"Desastre natural.\nNo tenías mejoras: pagas multa ${multaMonto}.";
                apply = () =>
                {
                    PayToBank(jugador, multaMonto);
                    ShowToast($"Desastre: multa ${multaMonto}.", 3f);
                };
            }
        }
        else
        {
            // 2) ReparacionesMayores: 2× mantenimiento por propiedad
            acc += catReparaciones;
            if (r < acc)
            {
                int props = CountOwnedProps(jugador);
                int totalPago = 2 * maintenanceCostPerProperty * props;
                cuerpo = $"Reparaciones mayores.\nPropiedades: {props}\nPagas ${maintenanceCostPerProperty}×2 por cada una → ${totalPago}.";
                apply = () =>
                {
                    PayToBank(jugador, totalPago);
                    ShowToast($"Reparaciones: −${totalPago} ({props} props).", 3f);
                };
            }
            else
            {
                // 3) Embargo: paga 20% del dinero actual
                acc += catEmbargo;
                if (r < acc)
                {
                    int money = GetMoney(jugador);
                    int cargo = Mathf.RoundToInt(money * 0.20f);
                    cuerpo = $"Embargo fiscal.\nPagas el 20% de tu dinero actual (${money}) → ${cargo}.";
                    apply = () =>
                    {
                        PayToBank(jugador, cargo);
                        ShowToast($"Embargo: −${cargo} (20%).", 3f);
                    };
                }
                else
                {
                    // 4) ParoGeneral: limpia buff/debuff de renta
                    cuerpo = $"Paro general.\nSe limpian tus modificadores de renta (buff/debuff).";
                    apply = () =>
                    {
                        rentMod[jugador] = RentMod.None;
                        rentModLapsLeft[jugador] = 0;
                        ShowToast("Paro: se limpió tu buff/debuff de renta.", 3f);
                    };
                }
            }
        }

        bool done = false;
        modal.Show(titulo, cuerpo, "OK", "Cerrar",
            onYes: () => { apply?.Invoke(); done = true; },
            onNo: () => { apply?.Invoke(); done = true; });

        yield return new WaitUntil(() => done);
        yield break;
    }


    IEnumerator RollAndMove()
    {
        if (btnTirar) btnTirar.interactable = false;

        var player = (turno == 0) ? p1 : p2;

        ArmRentModIfNeededAtTurnStart(turno);   // ← NUEVO

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
            AddMoney(turno, startBonus);
            ShowToast($"+${startBonus} por pasar Start", 2f);
        }

        TryExpireRentModOnLanding(turno, player.boardIndex);   // ← NUEVO

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
            ToastStatus($"Jugador {turno + 1} fue enviado a Cárcel. Pierde 1 turno.", 3f);

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
                int finalPrice = EffectivePrice(turno, price);
                bool tieneDesc = hasDiscount[turno];

                string titulo = info != null ? $"¿Comprar {info.nombre}?" : "¿Comprar propiedad?";
                string cuerpo = tieneDesc
    ? $"Precio ${price} → con descuento −{discountPercent}%: ${finalPrice}\nRenta ${rent}"
    : $"Precio ${price} • Renta ${rent}";



                modal.Show(
                    titulo,
                    cuerpo,
                    "Comprar",
                    "Pasar",
                    onYes: () =>
                    {
                        // intenta pagar
                        if (SpendMoney(turno, finalPrice))
                        {
                            if (tieneDesc) ConsumeDiscount(turno);
                            // (lo demás queda igual)
                            ownerByTile[idx] = turno;
                            var colorDueno = (turno == 0) ? ownerColorP1 : ownerColorP2;
                            landedTile.SetOwnerMark(colorDueno, true);
                            ToastStatus($"Jugador {turno + 1} compró {(info != null ? info.nombre : $"casilla {idx}")} por ${finalPrice}.", 3f);
                        }

                        else
                        {
                            // sin saldo: no compra (más adelante mostramos aviso bonito)
                            ToastStatus("No alcanza el dinero para comprar.", 2.5f);
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

                    float r = parCompleto ? baseRent * 1.5f : baseRent;
                    float up = GetUpgradeMultForTile(idx);
                    r *= up;

                    r *= GetRentModMultiplierForOwner(duenoActual);   // +25% o -25% según el dueño
                    int rent = Mathf.RoundToInt(r);

                    // Toast con el desglose del alquiler
                    string monoTag = parCompleto ? " ×1.5 (monopolio)" : "";
                    string upTag = up > 1f ? $" ×{up:0.##} (mejora)" : "";   // ← NUEVO
                    float mod = GetRentModMultiplierForOwner(duenoActual);
                    string modTag = mod > 1f ? " ×1.25 (buff)" : (mod < 1f ? " ×0.75 (debuff)" : "");

                    ShowToast($"Alquiler base ${baseRent}{monoTag}{upTag}{modTag} → Total ${rent}", 3f);


                    PayPlayerToPlayer(turno, duenoActual, rent);
                    if (txtEstado) txtEstado.text =
                        $"Jugador {turno + 1} pagó renta ${rent} a Jugador {duenoActual + 1}" +
                        (info != null ? $" ({info.nombre})" : "") +
                        (parCompleto ? " (+50% por monopolio)" : "") + RentModTag(duenoActual) + ".";
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
                    int finalPrice = EffectivePrice(turno, price);
                    bool tieneDesc = hasDiscount[turno];

                    modal.Show(
    $"¿Comprar {nom}?",
    tieneDesc
        ? $"Precio ${price} -> con descuento -{discountPercent}%: ${finalPrice}\nRenta base ${baseRent}\n(La renta escala por cantidad total de infra del dueno)"
        : $"Precio ${price} • Renta base ${baseRent}\n(La renta escala por cantidad total de infra del dueno)",
    "Comprar",
    "Pasar",
    onYes: () =>
    {
        if (SpendMoney(turno, finalPrice))
        {
            if (tieneDesc) ConsumeDiscount(turno);
            ownerByTile[idx] = turno;
            var colorDueno = (turno == 0) ? ownerColorP1 : ownerColorP2;
            landedTile.SetOwnerMark(colorDueno, true);
            ToastStatus($"Jugador {turno + 1} compró {nom} por ${finalPrice}.", 3f);
        }
        else
        {
            ToastStatus("No alcanza el dinero para infraestructura.", 2.5f);
        }
        esperandoDecision = false;
    },
    onNo: () => { esperandoDecision = false; }
);

                    yield return new WaitUntil(() => esperandoDecision == false);
                }
                else if (duenoActual != turno)
                {
                    int owned = CountInfraOwned(duenoActual);
                    int baseRent = infraRentasBase[slot];

                    // aplicar buff/debuff del dueño que COBRA
                    float mod = GetRentModMultiplierForOwner(duenoActual);
                    int rent = Mathf.RoundToInt(baseRent * Mathf.Max(1, owned) * mod);

                    // Toast con desglose (incluye buff/debuff si aplica)
                    string modTag = mod > 1f ? " ×1.25 (buff)" : (mod < 1f ? " ×0.75 (debuff)" : "");
                    ShowToast($"Infra: base ${baseRent} × posee {owned}{modTag} = ${rent}", 3f);

                    PayPlayerToPlayer(turno, duenoActual, rent);
                    if (txtEstado) txtEstado.text =
                    $"Jugador {turno + 1} pagó renta ${rent} de infraestructura a Jugador {duenoActual + 1} ({infraNombres[slot]}, posee {owned})"
                    + RentModTag(duenoActual) + ".";
                }

                // Si es tuya, no pasa nada
            }
        }
        // --- ROBO (12, 22, 38) ---
        if (landedTile && landedTile.type == TileType.Robbery)
        {
            // Abre la UI de Robo y aplica el resultado
            yield return ResolveRobbery(turno);
        }

        // --- Construcción (28) ---
        if (landedTile && landedTile.type == TileType.Construction)
        {
            // reunir propiedades propias NO mejoradas (impares)
            var upgradables = new System.Collections.Generic.List<int>();
            for (int i = 1; i < board.TileCount; i += 2)
                if (ownerByTile[i] == turno && !IsUpgraded(i)) upgradables.Add(i);

            if (upgradables.Count == 0)
            {
                bool ok = false;
                modal.Show("Construcción",
                    "No tienes propiedades para mejorar (o ya están mejoradas).",
                    "OK", "Cerrar",
                    onYes: () => ok = true, onNo: () => ok = true);
                yield return new WaitUntil(() => ok);
            }
            else
            {
                int idxMej = upgradables[0]; // v1: tomamos la primera
                var infoMej = GetProp(idxMej);
                string nombre = infoMej != null ? infoMej.nombre : $"Prop {idxMej}";
                int baseRent = infoMej?.renta ?? demoPropertyRent;
                int rentMejorada = Mathf.RoundToInt(baseRent * constructionMult);

                bool ok = false;
                modal.Show(
                    "Construcción",
                    $"¿Mejorar {nombre} por ${constructionCost}?\n" +
                    $"Renta base: ${baseRent} → ${rentMejorada}\n" +
                    $"(La mejora se acumula con monopolio y buff/debuff)",
                    "Mejorar", "Cancelar",
                    onYes: () =>
                    {
                        if (SpendMoney(turno, constructionCost))
                        {
                            upgraded[idxMej] = true;
                            ShowToast($"Mejoraste {nombre}: renta ×{constructionMult:0.##} (−${constructionCost}).", 3f);
                        }
                        else
                        {
                            ShowToast("No alcanza el dinero para construir.", 2.5f);
                        }
                        ok = true;
                    },
                    onNo: () => ok = true
                );
                yield return new WaitUntil(() => ok);
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
        // --- Mantenimiento (36) ---
        else if (landedTile && landedTile.type == TileType.Maintenance)
        {
            int props = CountOwnedProps(turno);
            int total = props * maintenanceCostPerProperty;

            if (props <= 0)
            {
                // Sin propiedades: no cobres nada ni muestres modal
                ToastStatus($"Jugador {turno + 1} no tiene propiedades. Mantenimiento $0.", 2.5f);
            }
            else
            {
                // Con propiedades: cobrar y mostrar el modal con el desglose
                PayToBank(turno, total);

                bool done = false;
                modal.Show(
                    "Mantenimiento",
                    $"−${total}\n${maintenanceCostPerProperty} × {props} propiedades",
                    "OK", "Cerrar",
                    onYes: () => done = true,
                    onNo: () => done = true
                );
                yield return new WaitUntil(() => done);

                ToastStatus($"Jugador {turno + 1} pagó mantenimiento ${total} ({props} propiedades).", 3f);
            }
        } // ← cierre correcto del bloque de Mantenimiento

        // --- Especiales pares simples (v1) ---
        else if (landedTile && (
                 landedTile.type == TileType.Prize ||
                 landedTile.type == TileType.Tax ||
                 landedTile.type == TileType.Rest ||
                 landedTile.type == TileType.Jail))
        {
            int idx = landedTile.index;

            switch (idx)
            {
                case 4:
                case 18:
                    {
                        // Cobrar premio
                        AddMoney(turno, premioMonto);
                        ToastStatus($"Jugador {turno + 1} cobró premio ${premioMonto} (casilla {idx}).", 3f);

                        bool ok = false;
                        modal.Show(
                            "Premio", $"${premioMonto}", "OK", "Cerrar",
                            onYes: () => ok = true,
                            onNo: () => ok = true
                        );
                        yield return new WaitUntil(() => ok);
                        break;
                    }

                case 8:
                    {
                        // Impuesto
                        PayToBank(turno, impuestoMonto);
                        ToastStatus($"Jugador {turno + 1} pagó impuesto ${impuestoMonto} (casilla {idx}).", 3f);

                        bool ok = false;
                        modal.Show(
                            "Impuesto", $"${impuestoMonto}", "OK", "Cerrar",
                            onYes: () => ok = true,
                            onNo: () => ok = true
                        );
                        yield return new WaitUntil(() => ok);
                        break;
                    }

                case 10:
                    {
                        // Cárcel: elegir fianza o perder 1 turno
                        bool done = false;
                        modal.Show(
                            "Cárcel",
                            $"Pagar fianza ${bailCost} o perder 1 turno",
                            "Pagar", "Perder turno",
                            onYes: () =>
                            {
                                if (SpendMoney(turno, bailCost))
                                {
                                    ToastStatus($"Jugador {turno + 1} pagó fianza ${bailCost} y sigue jugando.", 3f);
                                }
                                else
                                {
                                    skipTurns[turno] = 1;
                                    ToastStatus($"No alcanzó para la fianza. Jugador {turno + 1} pierde 1 turno.", 3f);
                                }
                                done = true;
                            },
                            onNo: () =>
                            {
                                skipTurns[turno] = 1;
                                ToastStatus($"Jugador {turno + 1} perdió 1 turno (cárcel).", 3f);
                                done = true;
                            }
                        );
                        yield return new WaitUntil(() => done);
                        break;
                    }

                case 20:
                    {
                        // Descanso: pequeño bonus
                        AddMoney(turno, restBonus);
                        ToastStatus($"Jugador {turno + 1} descansó y cobró ${restBonus} (casilla {idx}).", 3f);

                        bool ok = false;
                        modal.Show(
                            "Descanso", $"+${restBonus}", "OK", "Cerrar",
                            onYes: () => ok = true,
                            onNo: () => ok = true
                        );
                        yield return new WaitUntil(() => ok);
                        break;
                    }
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
    // --- Toast helper ---
    void ShowToast(string text, float time = 3f)
    {
        if (toast) toast.Show(text, time);
    }
    // Manda un mensaje al pop-up (toast) durante 'time' segundos
    void ToastStatus(string msg, float time = 3f)
    {
        ShowToast(msg, time);
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

    // Dinero actual del jugador
    int GetMoney(int player) => (player == 0) ? dinero1 : dinero2;

    // Lista de propiedades (impares) del owner
    List<int> GetOwnedProps(int owner)
    {
        var list = new List<int>();
        for (int i = 1; i < ownerByTile.Length; i += 2)
            if (ownerByTile[i] == owner) list.Add(i);
        return list;
    }

    // Devuelve una propiedad MEJORADA del owner al azar (o -1 si no hay)
    int GetRandomUpgradedProp(int owner)
    {
        var ups = new List<int>();
        for (int i = 1; i < ownerByTile.Length; i += 2)
            if (ownerByTile[i] == owner && IsUpgraded(i)) ups.Add(i);
        if (ups.Count == 0) return -1;
        return ups[Random.Range(0, ups.Count)];
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

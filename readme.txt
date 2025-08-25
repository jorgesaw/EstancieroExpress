1) Visión y metas

Sesiones cortas: 10–15 min, 2–4 jugadores (local + IA; online a futuro).

Énfasis en sorpresa: eventos, minijuego de robo, premios y penalizaciones.

Claridad visual: iconos por tipo de casilla, colores fuertes, animaciones cortas.

Mobile-first (vertical): tablero visible + barra de acciones rápida.

Condición de victoria (v1)

Por eliminación (quiebra) o límite de turnos (gana quien tenga más patrimonio).

2) Núcleo jugable (core loop)

Tirar dado → mover ficha en sentido horario.

Resolver la casilla de destino:

Propiedad: comprar / pagar alquiler / mejorar (más adelante).

Estación: renta fija (mejora por cantidad).

Evento: premio, multa, chance, mantenimiento, construcción, etc.

Robo: se abre minijuego de cartas.

Cárcel: perder 1 turno (o salir pagando/carta).

Cobrar/ajustar dinero → mostrar feedback visual/sonoro → siguiente jugador.

3) Tablero y numeración

40 casillas alrededor del perímetro (índices internos 0..39).

Inicio/Salida = Tile_00 (interno) = Nº 1 en documento.

Recorre horario: 0→1→2→…→39→(wrap)0.

4) Tipos de casilla e iconos

🏠 Propiedad (con grupos de color para combos/monopolios, +mejoras).

🚂 Estación/Terminal (renta fija, escala por cantidad poseída).

🎁 Premio (ganancia instantánea / bonus).

📦 Evento misterioso / Chance (positivo/negativo, movimiento, buffs/debuffs).

🛠 Construcción (mejorar una propiedad pagando; sube el alquiler).

⚠ Mantenimiento / Impuesto (costo fijo o por propiedad).

🃏 Robo al oponente (minijuego).

🚔 Control Judicial (Cárcel) 

🏁 Meta Final (bonus grande).

Icono + color de fondo fijo por tipo; texto corto debajo (legible en móvil).

6) Minijuego “Robo al oponente” (diseño final)

Setup (al caer en 🃏)

8 cartas boca abajo:

4 rivales (Azul, Verde, Amarillo, Rojo).

Al revelar: robar dinero (10/15/20/25%) o una propiedad aleatoria del rival.

Si sale tu propio color → vas a Cárcel (tile 9) y pierdes 1 turno.

2 defensa (escudo plata): robo anulado; el ladrón pierde 5–10% de su dinero.

2 especiales:

Robo Doble: roba 10% a dos rivales aleatorios.

Intercambio Forzoso: intercambia 1 propiedad con el rival que salga.

Flow

Mostrar rejilla 2×4 de cartas → el jugador elige 1.

Revelar con animación + resolver efecto.

Si “Robo Doble”: selección automática de rivales (o diálogo si hay 3+).

8) Arte y UX

Estilo: caricaturesco, vibrante, sombras suaves, contornos limpios.

Iconografía fija por tipo de casilla; marquita de dueño con color del jugador.

Animaciones:

Dado rodando, highlight de casilla, flash del color del dueño al pagar.

Cartas emergentes (slide + elastic), cofres/ trofeos para premios.

Panel superior: dinero por jugador, 👑 líder (destello al cambiar).

Zona inferior: botón “Tirar dado”, estado, modal compacto.

9) Arquitectura técnica (Unity)

Prefabs principales

TilePrefab (SpriteRenderer + Collider + Rigidbody2D + Tile, opcional PropertyTile, StationTile, etc.).

PlayerTokenPrefab_[Color] (ficha con animación).

ModalPrompt, PopupCard, PopupPrize, PopupPenalty, PopupRobbery, etc.

Scripts actuales (ya en proyecto)

BoardManager — genera tablero, colorea, índices.

Tile — tipo base, color, highlight, FlashOnce.

PropertyTile — datos (nombre, precio, renta, owner, OwnerMark).

GameManager — turnos, dado, movimiento, compra/alquiler, bonus salida.

PlayerToken — movimiento y estado del jugador.

ModalPrompt — cartel SÍ/NO.

Extensiones previstas (v2)

StationTile (renta por cantidad).

EventTile (chance/premio/multa/construcción/mantenimiento).

JailTile / GoToJailTile.

RobberyMiniGame (controlador UI).

LeaderPanel (👑).

Datos configurables (recomendado con ScriptableObjects)

TileDefinition (tipo, nombre, valores, icono).

CardDefinition para robo y eventos.

EconomyConfig (bonos, multas, porcentajes).

10) Roadmap (pasos y criterios de aceptación)
Fase 1 — Base sólida (✅ parte hecha)

✅ Tablero 40, movimiento horario, bonus salida.

✅ Propiedades: compra, dueño, cobro de alquiler, flash al pagar.

✅ Marcador visual de dueño (OwnerMark).

Estaciones: renta fija + multiplicadores por cantidad.

Fase 2 — Eventos y minijuegos
Premios/Multas/Chance: popups bonitos, efecto aplicado, feedback.
Minijuego de Robo: 8 cartas, flujo y efectos

Fase 3 — UX y pulido visual
Tooltips/Popups contextuales (compra, mejora, pago, cárcel).
Panel 👑 líder con destello al cambiar.
SFX básicos (dado, comprar, cobrar, carta, cárcel).

Fase 4 — IA y monetización (opcional en v1)
Bot simple (prioriza compra barata, evita riesgo alto).
Rewarded Ads / IAP “remove ads” / skins básicas.

11) Tareas inmediatas (orden sugerido de implementación)

Tipos de casilla (motor)

Añadir enum BoardTileType { Start, Property, Station, Prize, Event, Robbery, Tax, GoToJail, Jail, Rest, Finish }.

BoardManager: asignar tipo según lista final (separar de PropertyIndices).

Tile muestra icono según tipo (sprite simple, placeholder OK).

Cárcel / Ir a Cárcel / Perder turno

En GameManager.HandleLanding: si es GoToJail → Teleport, skipTurns[player]=1.

Si es Jail y el jugador llega por efecto de robo o “Ir a Cárcel”, marcar skipTurns.

UI: pequeño contador sobre la ficha (texto “1”).

Estaciones

StationTile con baseRent=30.

Cálculo: rent = baseRent * (1 + estacionesDelOwner) (o x2/x3/x4, configurable).

Eventos simples (Premio/Impuesto/Rest)

EventTile: tipos Prize, Tax, Rest.

Mostrar PopupPrize/PopupPenalty con icono y texto → aplicar efecto.

Minijuego Robo (MVP)

UI de cartas (8 botones).

Lógica de efectos (porcentajes y propiedad aleatoria).

En todos los casos que aplican, enviar a Cárcel y perder 1 turno.

Cada paso lo hacemos como venimos: micro-paso, captura, “listo”, siguiente.

12) Activos/art direction (mínimos para avanzar)

Sprites (placeholders válidos para arrancar):

Iconos: 🏠 🚂 🎁 📦 🛠 ⚠ 🃏 🚔 🔄 🏁 (podemos usar figuras simples/colores hasta reemplazar).

UI: 1 panel modal genérico + 3 popups (Premio, Penalización, Robo).

SFX: dado, chime premio, “cash”, carta, sirena suave.

13) Notas técnicas importantes

Motor ya configurado en horario y Tile_00 = Inicio.

BoardManager tiene herramientas de rebuild y force recolor (evita “tablero blanco”).

Prefab TilePrefab incluye OwnerMark para mostrar dueño.

Mantener todos los valores en config (ScriptableObject) para balance veloz.

Regla base

Propiedades: 20 casillas → todos los índices impares 1,3,5,...,39 (se agrupan en parejas por color).

Especiales: 20 casillas → todos los índices pares 0,2,4,...,38.

Recorrido horario.

Start = índice 0.

Economía inicial (para partidas tensas de 10–15 min)
Dinero

Saldo inicial: $600 por jugador.

Suficiente para 1–2 compras tempranas o ahorrar para combos; obliga a elegir.

Bono por vuelta (Start, índice 0): +$150.

Crece ritmo de compra sin romper el equilibrio.

Descanso (índice 20): +$50 (pequeño alivio opcional).

Monopolio (tener las 2 del color): +50% a la renta de ese par.

Ej.: propiedad de $36 → $54 con monopolio.

Construcción (índice 28): puedes mejorar 1 de tus propiedades pagando $150 → renta de esa propiedad x1.75 (una sola mejora por v1).

Por qué esta distribución funciona

20 propiedades dan margen para competir por monopolios sin que todos compren todo.

Poca liquidez al inicio (+ bonos moderados) = decisiones reales: ¿compro ya o ahorro?

Robos y mantenimiento castigan al líder y generan vueltas dramáticas.

Estaciones dan ingresos alternativos y simples.

Construcción introduce una única mejora clara (evita complejidad de casas/hoteles).

Eventos mueven el tablero (dinero y posición) sin ser 100% punitivos ni 100% generosos.

A) Regla de Robo (actualizada)

Éxito (robaste % o propiedad):

Te mandan a Control Judicial (cárcel) por “control”, pero NO pierdes turno.

Sabor: “te detienen, firmas y sales”.

Tu propio color (fallo crítico):

Vas a cárcel y pierdes 1 turno.

Carta de Defensa (robo anulado):

No vas a cárcel; pierdes 5–10% de tu dinero.

Robo doble / intercambio forzoso: se aplica el efecto y se mantiene la regla de arriba según sea éxito o no

B) Las 4 “estaciones” ahora son infraestructura variada (con valores crecientes suaves)

Opción C (eco/energía)
Índice	Infraestructura	Compra	Renta
6	Planta de reciclaje	$140	$28
14	Parque eólico	$160	$32
24	Planta solar	$180	$36
32	Hidroeléctrica	$200	$40

Todas mantienen la escala suave (compra +$20, renta +$4). Con tu saldo inicial $600 y bono por vuelta $150, van a sentirse accesibles pero obligan a decidir.

Tendencia de mercado (buff rotativo por color)

Qué es: cada 4 turnos globales, un grupo de color (p.ej. Amarillo, Celeste, etc.) recibe +25% renta por 1 vuelta del tablero.

Visual: chip “📈 +25%” sobre cualquier tile de ese color y etiqueta en la barra superior.

Stacking: no se acumula; si sale el mismo color antes de que termine, se renueva el contador.

Interacción: aplica a propiedades, no a infraestructuras. Se suma al bonus de monopolio (multiplicativo: rentaBase * 1.5 * 1.25).

Selección: aleatoria entre colores presentes; si nadie posee ese color, igual hay buff (incentiva comprarlo).

Parámetros:
marketTrendEnabled=true, trendIntervalTurns=4, trendBonus=+25%, trendDurationLaps=1.

Power-ups de un uso (vía eventos)

Slots por jugador: 1 (si tomas uno nuevo con el slot lleno, debes reemplazar o descartar).

Tipos (v1):

Seguro 🛡: anula un cobro (alquiler o robo). Auto-consume.

Re-roll 🎲: relanza el dado una vez en tu turno (elige entre el viejo y el nuevo). Costo al usar: $30.

Descuento 💸: −30% en la próxima compra (propiedad o infraestructura) dentro de 1 vuelta.

Obtención: por Evento (pool con 30–40% de probabilidad) o raremente por Premio grande.

UI: pequeño icono bajo el panel del jugador; tap para usar (si aplica).

Parámetros:
powerupsEnabled=true, rerollCost=30, discountPercent=30, discountDurationLaps=1, eventPowerupDrop=0.35.

Hot Tiles 🔥 (renta x2 temporal)

Cuándo: al pasar por Start, se marcan 2 propiedades aleatorias como Hot por 1 vuelta.

Elegibilidad: preferir propiedades sin mismo dueño; si no es posible, permitir repetición. No marca infraestructuras.

Efecto: x2 renta de esa propiedad (se multiplica con monopolio y tendencia: renta * 2 * 1.5 * 1.25…).

Visual: borde brillante + flama pequeña; tooltip “x2 por 1 vuelta”.

Stacking: un tile ya Hot que salga sorteado renueva su duración; no pasa a x4.

Parámetros:
hotTilesEnabled=true, hotTilesPerLap=2, hotTilesDurationLaps=1.

Balance con tu economía actual

Saldo inicial $600 + Start $150 mantiene presión.

Tendencia de mercado y Hot Tiles premian moverse y poseer, sin regalar dinero.

Power-ups dan decisiones puntuales y contrajuego al robo/alquiler.

Edge cases y reglas de convivencia

Robo exitoso: actualizamos regla: va a cárcel pero no pierde turno.
– Si tiene Seguro, puede anular el robo (el ladrón no roba).

Descuento + Subasta: el Descuento no aplica en subasta (solo compra directa).

Tendencia + Hot: multiplican, no suman.

Mantenimiento (36): se calcula post-bonos (no afecta).

IA (futuro): puja solo si liquidez ≥ 1.2× precio efectivo que pretende.

UI mínima (móvil vertical)

Subasta: modal con +10 / −10, cronómetro, “Puja” grande, lista de pujadores con color.

Tendencia: chip “📈 Color X +25%” en la barra superior.

Power-up: iconito bajo el marcador del jugador; mostrar confirmación al usar.

Hot Tiles: brillo breve al marcar; icono 🔥 fijo mientras dure.

Subasta por Liquidez (Banco)
¿Cuándo se activa?

Si al final de tu turno quedas con saldo < 0 → entras en estado En Rojo.

Tienes 1 turno de gracia para salir de Rojo (con premios, Start, cobros, vender voluntario, etc.).

Al inicio de tu siguiente turno, si sigues < 0 → se activa la Subasta por Liquidez.

Toggle: liquidityAuctionEnabled = true
Parámetros: redGraceTurns = 1

Respiro del Banco (crédito puente)

El banco te acredita lo mínimo para dejarte ≥ $0 + un colchón de $50, con un tope de $150.

Fórmula: respiro = clamp( abs(saldo) + 50, 0, 150 ).

Este respiro es crédito temporal que se cubre con el resultado de la subasta.

Si tras subastar aún falta dinero, el remanente se convierte en deuda con el banco (ver abajo).

Parámetros: respiroBuffer = 50, respiroCap = 150

Subasta por Liquidez — selección de propiedad (actualizada)

Candidatas: propiedades y infraestructuras del jugador en Rojo.

Prioridades (en orden):

Evitar Hot Tiles si existe otra opción.

Preferir NO romper monopolios, pero si no hay alternativa viable, se puede romper.

Entre las posibles, elegir la de menor valoración: score = price + 2*rent. (Elige la más “barata” en impacto).

Si todas rompen monopolio, elegir la que rompa el monopolio de menor valor total (suma de rentas de ese par/grupo).

Toggles/params:

allowBreakMonopolyIfNeeded = true (✅ activado)

avoidHotTiles = true (se intenta evitar, pero si no hay otra, también se puede subastar)

valuationScore = price + 2*rent (criterio de desempate)

La subasta (entre los otros jugadores)

Participan solo los demás jugadores (el deudor no puja).

Precio inicial: 60% del precio base (redondeado a múltiplos de $10).

Incrementos: pasos fijos de $10.

Reloj: 10 s (o termina 3 s después de la última puja).

Ganador paga al banco y recibe la propiedad (dueño+marquita).

Parámetros: auctionStartFactor = 0.6, auctionIncrement = 10, auctionWindow = 10s

Si nadie puja:

El banco toma la propiedad y queda “del banco” (sin alquiler para nadie) por 1 vuelta; luego se re-lista a precio completo cuando alguien caiga allí, o se vuelve a subastar en la próxima activación.

¿A dónde va el dinero de la subasta?

Primero cubre el Respiro recién otorgado.

Si sobra, se abona al jugador para que siga la partida con algo de aire.

Si no alcanzó para cubrir el respiro, el faltante pasa a deuda con el banco.

Deuda con el banco (simple, sin freno de ritmo)

Se acumula en bankDebt[player].

Interés: 10% al pasar por Start (una sola vez por vuelta).

Cobro automático: cada vez que pasas por Start, se descuenta hasta $150 de tu bono para pagar deuda (si hay).

Puedes pagar manual desde un botón (si tienes efectivo).

Parámetros: debtInterestOnStart = 0.10, debtAutoPayPerStart = 150

Anti-frustración:

Máximo 1 subasta por liquidez por jugador cada 2 turnos.

No bloquea tu turno: después de la subasta, sigues jugando normalmente (tirás el dado).

Parámetro: liquidityAuctionCooldownTurns = 2

UI/UX (rápida y clara)

Badge “En Rojo” en el panel del jugador + contador de turnos de gracia.

Pop-up: “Respiro del Banco +$XX” (subtítulo: “Se cubrirá con la subasta”).

Modal de subasta: foto de la propiedad (nombre, renta), precio actual, botones +10 / Pujar, reloj, lista de pujadores.

Toast final: “Se subastó Mercado Viejo por $190. Respiro cubierto. Tu saldo: $40.”
(o “Deuda pendiente con el banco: $60”)

Números de ejemplo (con economía actual)

Jugador cae y paga alquiler, queda en –$45.

Turno de gracia: no logra salir de Rojo.

Inicio de su turno: Respiro = min(45 + 50, 150) = $95.

Se elige la propiedad menos valiosa  → precio base $150 → subasta arranca en $90.

Termina en $140.

$95 cubre el Respiro, sobran $45 → saldo del jugador +45.

Sigue su turno con oxígeno pero sin regalarle la partida.

Eventos — Pool v1 (para índices 2, 16, 26, 34)

Al caer en una casilla de Evento se sortea 1 de estos resultados.

Efecto	Qué pasa	Valores	Duración	Prob.
Premio	Ganas dinero	+$100	Instantáneo	20%
Multa	Pierdes dinero	–$100	Instantáneo	15%
Mover ±3	Avanza +3 o retrocede –3 (50/50)	3 casillas	Instantáneo	15%
Intercambio	Intercambias 1 propiedad al azar con un rival aleatorio	—	Instantáneo	10%
Descuento	Próxima compra con –30%	Aplica a 1 compra	1 vuelta	15%
Buff de renta	Tus propiedades cobran +25%	+25%	1 vuelta	12%
Debuff de renta	Tus propiedades cobran –25%	–25%	1 vuelta	13%

Catástrofes climáticas (detalle del 40%)

Severidad al salir catástrofe: Leve 60% / Media 30% / Severa 10%.
Enfriamiento: un jugador no puede recibir catástrofe dos turnos seguidos.

Catástrofe	Efecto	Reparar / Mitigar	Notas
🌊 Inundación	1 propiedad −50% renta (Severa: 2) hasta reparar	$40 / $60 / $80 (L/M/S)	Hidroeléctrica: −$10 al costo
☀️ Sequía	−25% renta global (tus propiedades) 1 vuelta (Severa: 2 vueltas)	Mitigar ahora: $30 / $45 / $60 (L/M/S)	Planta solar: −$10 al costo
⚡ Tormenta eléctrica	Si tenés Parque eólico → renta 0 hasta reparar; si no, 1 propiedad −50% 1 vuelta	Reparar eólico: $40 / $60 / $80	Severa: −$20 inmediato
🧊 Granizo	1 propiedad Clausurada (renta 0) hasta reparar (Severa: 2 o 1 infraestructura)	$50 / $70 / $90 (L/M/S)	Reciclaje: −$10 al costo

Reglas rápidas

Descuento (power-up) sí reduce el costo de reparar/mitigar (lo tratamos como “servicio”).

Buff/Debuff y catástrofes no se acumulan: si vuelven a salir, renuevan duración.

Mover ±3: si caes en casilla con acción (compra, impuesto, etc.), se resuelve normal.

Intercambio: si vos o el rival no tienen propiedades, se reintenta; si nadie tiene, se convierte en Premio +$80.

Cárcel (10): el jugador elige entre pagar fianza $120 o perder 1 turno.

Ir a Cárcel (30): va a 10 y pierde 1 turno.

Premio $100, Impuesto –$80, Descanso +$50, Bono por vuelta (0): +$150.

Tablero 0–39 (nombres cortos con prefijo Av./Calle y “Planta …”)
Índice	Tipo	Nombre	Compra	Renta	Efecto
0	🏁 Especial	Salida / Meta	—	—	Cobras +$150 al pasar/caer
1	🏠 Amarillo	Av. Central	100	20	—
2	📦 Evento	Evento	—	—	Carta (buena/mala)
3	🏠 Amarillo	Av. Mercado	120	25	—
4	🎁 Premio	Premio	—	—	Cobras +$100
5	🏠 Celeste	Calle Río	140	28	—
6	⚡ Estación	Planta Reciclaje	140	28	Paga renta si no es tuya
7	🏠 Celeste	Calle Flores	160	32	—
8	🏦 Impuesto	Impuesto	—	—	Pagas –$80
9	🏠 Naranja	Calle Sol	180	36	—
10	🚓 Cárcel	Cárcel	—	—	Elegir: fianza $120 o perder 1 turno
11	🏠 Naranja	Calle Parque	200	40	—
12	🕵️ Robo	Robo	—	—	Minijuego
13	🏠 Rosa	Calle Puerto	220	44	—
14	⚡ Estación	Planta Eólica	160	32	Paga renta si no es tuya
15	🏠 Rosa	Calle Bosque	240	48	—
16	📦 Evento	Evento	—	—	Carta (buena/mala)
17	🏠 Verde	Av. Mercado	260	52	—
18	🎁 Premio	Premio	—	—	Cobras +$100
19	🏠 Verde	Calle Mayor	280	56	—
20	☕ Descanso	Descanso	—	—	Bonus +$50
21	🏠 Azul claro	Calle Lago	300	60	—
22	🕵️ Robo	Robo	—	—	Minijuego
23	🏠 Azul claro	Av. Montaña	320	64	—
24	⚡ Estación	Planta Solar	180	36	Paga renta si no es tuya
25	🏠 Marrón	Calle Real	350	70	—
26	📦 Evento	Evento	—	—	Carta (buena/mala)
27	🏠 Marrón	Calle Corona	400	80	—
28	🛠️ Construcción	Construcción	—	—	Mejora 1 propiedad pagando
29	🏠 Púrpura	Av. Castillo	420	84	—
30	🚓 Ir a Cárcel	Ir a Cárcel	—	—	Va a 10 y pierde 1 turno
31	🏠 Púrpura	Calle Reina	440	88	—
32	⚡ Estación	Planta Hidroeléctrica	200	40	Paga renta si no es tuya
33	🏠 Rojo	Calle Rubí	460	92	—
34	📦 Evento	Evento	—	—	Carta (buena/mala)
35	🏠 Rojo	Av. Granada	480	96	—
36	🧰 Mantenimiento	Mantenimiento	—	—	Pago por propiedad
37	🏠 Azul	Calle Marina	500	100	—
38	🕵️ Robo	Robo	—	—	Minijuego
39	🏠 Azul	Av. Emperador	520	104	—


# 🌾 Agrópolis Work

**Agrópolis Work** es un juego de mesa digital para celulares inspirado en clásicos como *El Estanciero* o *Monopoly*, con un enfoque **mobile-first**, partidas rápidas y mecánicas novedosas (eventos, robos, subastas, tendencias de mercado).  

📱 Diseñado para sesiones cortas (10–15 min), de **2–4 jugadores** (local + IA; online en futuras versiones).  

---

## 📑 Índice

- [🎯 Visión y Metas](#-visión-y-metas)
- [♻️ Núcleo Jugable (Core Loop)](#️-núcleo-jugable-core-loop)
- [🎲 Tablero](#-tablero)
- [🗂 Tipos de Casilla e Iconos](#-tipos-de-casilla-e-iconos)
- [🃏 Minijuego: Robo al Oponente](#-minijuego-robo-al-oponente)
- [🎨 Arte y UX](#-arte-y-ux)
- [🛠 Arquitectura Técnica (Unity)](#-arquitectura-técnica-unity)
- [🛤 Roadmap](#-roadmap)
- [💰 Economía Inicial](#-economía-inicial)
- [🔥 Mecánicas Avanzadas](#-mecánicas-avanzadas)
- [📑 Tablero (v1)](#-tablero-v1)
- [🔮 Futuras Expansiones](#-futuras-expansiones)
- [📌 Notas Técnicas](#-notas-técnicas)
- [🚀 Estado Actual](#-estado-actual)

---

## 🎯 Visión y Metas

- **Sesiones cortas**: partidas dinámicas de 10–15 minutos.  
- **Multijugador local + IA** (modo online previsto en roadmap).  
- **Énfasis en sorpresa**: eventos, minijuego de robo, premios y penalizaciones.  
- **Claridad visual**: iconos y colores fuertes, animaciones breves.  
- **Mobile-first (vertical)**: tablero visible + barra de acciones rápida.  

**Condición de victoria (v1):**  
- Eliminación (quiebra).  
- Límite de turnos (gana quien tenga más patrimonio).  

---

## ♻️ Núcleo Jugable (Core Loop)

1. Tirar dado → mover ficha en sentido horario.  
2. Resolver casilla:  
   - **Propiedad**: comprar, pagar alquiler o mejorar.  
   - **Estación**: renta fija, escala por cantidad poseída.  
   - **Evento**: premio, multa, chance, construcción, mantenimiento.  
   - **Robo**: minijuego de cartas.  
   - **Cárcel**: perder turno (o salir pagando/carta).  
3. Ajustar dinero + feedback visual/sonoro.  
4. Turno del siguiente jugador.  

---

## 🎲 Tablero

- **40 casillas** alrededor del perímetro (índices `0..39`).  
- Recorrido **horario**: 0 → 1 → 2 … → 39 → (vuelve a 0).  
- **Inicio/Salida**: índice `0` (+$150 al pasar).  

---

## 🗂 Tipos de Casilla e Iconos

| Tipo            | Icono | Descripción |
|-----------------|-------|-------------|
| Propiedad       | 🏠    | Comprar, pagar o mejorar (monopolios por color). |
| Estación        | 🚂    | Renta fija con multiplicador por cantidad. |
| Premio          | 🎁    | Ganancia instantánea. |
| Evento          | 📦    | Chance positiva/negativa. |
| Construcción    | 🛠    | Mejora de propiedad (↑ renta). |
| Impuesto        | ⚠    | Pago fijo o proporcional. |
| Robo            | 🃏    | Minijuego de robo. |
| Cárcel          | 🚔    | Pierdes 1 turno (o fianza). |
| Ir a Cárcel     | 🚓    | Envía directo a la cárcel. |
| Descanso        | ☕    | Bonus +$50. |
| Meta Final      | 🏁    | Bonus grande. |

---

## 🃏 Minijuego: Robo al Oponente

- **Setup:** 8 cartas boca abajo → jugador elige 1.  
- **Cartas posibles:**  
  - Robar % de dinero o propiedad de rival.  
  - Carta propia → vas a Cárcel.  
  - Defensa → robo anulado, ladrón pierde %.  
  - Robo Doble → quita a 2 rivales.  
  - Intercambio Forzoso → cambia 1 propiedad.  
- **Flow:**  
  - Animación → resolver efecto → feedback visual.  

---

## 🎨 Arte y UX

- Estilo **caricaturesco y vibrante**.  
- Iconografía clara, colores fijos por tipo.  
- Animaciones clave: dado rodando, highlight de casilla, cartas emergentes.  
- UI vertical:  
  - Panel superior → dinero + 👑 líder.  
  - Barra inferior → botón “🎲 Tirar dado” + estado.  

---

## 🛠 Arquitectura Técnica (Unity)

### Prefabs principales
- `TilePrefab` (SpriteRenderer + Collider + Tile).  
- `PlayerTokenPrefab_[Color]` (ficha animada).  
- `ModalPrompt`, `PopupCard`, `PopupPrize`, `PopupPenalty`, `PopupRobbery`.  

### Scripts base
- `BoardManager` — genera tablero, índices, colores.  
- `GameManager` — turnos, dado, compras/alquileres.  
- `Tile`, `PropertyTile`, `StationTile`, `EventTile`, `JailTile`.  
- `PlayerToken` — estado y movimiento.  
- `RobberyMiniGame` — controlador UI.  

### Configuración con **ScriptableObjects**
- `TileDefinition` (tipo, valores, icono).  
- `CardDefinition` (eventos, robo).  
- `EconomyConfig` (bonos, porcentajes, costos).  

---

## 🛤 Roadmap

### ✅ Fase 1 — Base
- Tablero 40 casillas.  
- Movimiento y bonus salida.  
- Propiedades con compra, dueño y cobro.  

### 🚧 Fase 2 — Eventos y Minijuegos
- Eventos con popups.  
- Minijuego de robo completo.  

### 🚧 Fase 3 — UX y Visual
- Tooltips, panel líder, animaciones y SFX básicos.  

### 🚧 Fase 4 — IA y Monetización
- Bot simple.  
- Ads recompensadas, skins e IAP.  

---

## 💰 Economía Inicial

- **Saldo inicial:** $600 por jugador.  
- **Bono por vuelta:** +$150.  
- **Descanso (índice 20):** +$50.  
- **Monopolio:** renta +50%.  
- **Construcción:** mejora única → renta ×1.75.  

---

## 🔥 Mecánicas Avanzadas

- **Tendencia de Mercado (📈):** cada 4 turnos un color gana +25% renta temporal.  
- **Hot Tiles (🔥):** al pasar por Start, 2 propiedades al azar x2 renta por 1 vuelta.  
- **Power-ups:**  
  - Seguro 🛡 — anula un cobro.  
  - Re-roll 🎲 — volver a tirar dado.  
  - Descuento 💸 — –30% en próxima compra.  

- **Subasta por Liquidez:**  
  - Si un jugador queda en negativo, se subasta una de sus propiedades.  
  - Banco da “respiro” de $50–150.  
  - Otros jugadores pujan desde 60% del valor.  
  - Deuda con banco si no alcanza.  

---

## 📑 Tablero (v1)

| Nº | Tipo | Nombre | Compra | Renta | Efecto |
|----|------|--------|--------|-------|--------|
| 0  | 🏁 Salida | Meta | — | — | +$150 al pasar |
| 1  | 🏠 Amarillo | Av. Central | 100 | 20 | — |
| 2  | 📦 Evento | Evento | — | — | Carta random |
| 3  | 🏠 Amarillo | Av. Mercado | 120 | 25 | — |
| 4  | 🎁 Premio | Premio | — | — | +$100 |
| …  | … | … | … | … | … |
| 39 | 🏠 Azul | Av. Emperador | 520 | 104 | — |

*(ver documento completo para todos los índices 0–39)*  

---

## 🔮 Futuras Expansiones

- **IA avanzada** con distintas personalidades.  
- **Modo online** competitivo y cooperativo.  
- **Editor de tableros** (custom maps y reglas).  
- **Eventos dinámicos** según temporada.  
- **Skins y coleccionables** de tokens y tableros.  

---

## 📌 Notas Técnicas

- Motor configurado con recorrido horario (0–39).  
- Balance económico diseñado para decisiones tácticas rápidas.  
- Prefabs listos para placeholders y reemplazo por arte final.  
- Todos los valores centralizados en `ScriptableObjects` para tuning rápido.  

---

## 🚀 Estado Actual

- Base funcional ✅  
- Propiedades y compras ✅  
- Estaciones y multiplicadores 🚧  
- Eventos, Robos y Subastas 🚧  
- IA y online ❌ (planificado)  

---

👨‍💻 **Equipo:** Proyecto independiente en desarrollo.  
📅 **Última actualización:** Agosto 2025.  
📌 **Licencia:** pendiente de definir.  

---

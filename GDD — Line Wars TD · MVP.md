# GDD — Block & Blood · MVP
*Tower Defense Roguelite — Unity 6000.3.11f1*

> **Este archivo es la fuente de verdad editable del diseño.**
> El archivo `GDD — Line Wars TD · MVP.pdf` es un snapshot histórico y ya no se actualiza.
> Última actualización: v1.11 — Torres Fuego y Agua eliminadas; efectos elementales ahora vía cartas.

---

## Índice

1. [Concepto](#1-concepto)
2. [Loop de juego](#2-loop-de-juego)
3. [Mapa y grilla](#3-mapa-y-grilla)
4. [Torres](#4-torres)
5. [Monstruos](#5-monstruos)
6. [Héroe](#6-héroe)
7. [Sistema de XP y cartas](#7-sistema-de-xp-y-cartas)
8. [Estructura de la run](#8-estructura-de-la-run)
9. [Boss final — Troll Anciano](#9-boss-final--troll-anciano)
10. [UI y controles](#10-ui-y-controles)
11. [Assets y arte](#11-assets-y-arte)
12. [Arquitectura técnica](#12-arquitectura-técnica)
13. [Criterios de éxito del MVP](#13-criterios-de-éxito-del-mvp)

---

## 1. Concepto

**Block & Blood** es un tower defense roguelite en el que el jugador construye un laberinto de torres en una grilla 14×18, elige cartas de mejora entre niveles de XP y defiende contra oleadas continuas de monstruos durante ~15 minutos hasta enfrentarse a un boss final.

**Plataforma:** PC (Windows / Mac)
**Modelo de negocio:** Demo gratuita en itch.io → Early Access en Steam a $2.99

---

## 2. Loop de juego

```
Preparación (30s, 50 oro)
    ↓
Stream continuo de monstruos
    ↓ (cada ~150/300/500 XP)
Pausa → elegir 1 de 3 cartas
    ↓ (minuto ~14:00)
Boss final — Troll Anciano
    ↓
Victoria (boss muerto) / Derrota (0 vidas, de 5 iniciales)
```

- Sin meta-progresión — cada run empieza igual
- La variedad viene de las cartas elegidas en cada run

---

## 3. Mapa y grilla

### Especificaciones

| Parámetro | Valor |
|-----------|-------|
| Dimensiones | 14 columnas × 18 filas (252 celdas) |
| Tamaño de celda | 48×48 px → `CellSize = 0.48f` unidades mundo |
| Coordenadas | X: 0–13 (columnas) · Y: 0–17 (filas) |
| Spawn (entrada enemigos) | Fila 17 — borde visual superior |
| Meta (salida enemigos) | Fila 0 — borde visual inferior |
| Celdas restringidas | Fila 17 completa (14 celdas) — no buildable permanentemente |

### Estados de celda

| Estado | Descripción | Sprite |
|--------|-------------|--------|
| `Libre` | Transitable, disponible para construir | `grass_base` / `path_base` / `path_edge_*` según columna |
| `EnConstrucción` | Reservada 5s, no bloquea pathfinding hasta completarse | tile085 — gris + llave |
| `Ocupada` | Torre activa, bloquea pathfinding | tile040 — pasto + X |
| `Restringida` | Fila GoalRow — permanentemente no buildable | tile116 — borde tierra/pasto |

### Pathfinding

- Algoritmo: **A\* Pathfinding Project** (plugin instalado, no implementar desde cero)
- Recalcula cuando una celda cambia a `Ocupada` o vuelve a `Libre`
- **Regla de validación:** `CanPlaceTower()` verifica que exista al menos un camino spawn→meta excluyendo la celda objetivo. Si no existe, rechaza con flash rojo en la celda
- Cada monstruo recalcula su ruta individualmente cuando el grafo cambia
- El héroe ignora la grilla completamente (vuela en línea recta)

### Estrategias válidas (no forzar ninguna)

- **Serpenteo** — laberinto largo maximiza exposición al daño
- **Embudo** — concentrar tráfico en un chokepoint para torres AoE
- **Chokepoint** — estrechez para torres Melee que ralentizan

---

## 4. Torres

### Principios generales

- Clic en celda libre con torre seleccionada en HUD → inicia construcción (5 segundos)
- La construcción es **remota** — el héroe puede estar en cualquier posición
- Vender devuelve **60% del costo total** (construcción + mejoras)
- Mejora con oro en cualquier momento: clic en torre → botón Mejorar

### Tabla de torres

| Torre | Costo | Daño | Rango | Efecto especial |
|-------|-------|------|-------|-----------------|
| Melee Lv1 | 12 oro | 15 dps (físico, AoE) | Celda propia + 8 adyacentes | Slow −15% |
| Torre Sierra (Melee Lv2) | +18 oro | 28 dps | = Lv1 | Slow −15% |
| Rango Lv1 | 10 oro | 20/proyectil (físico) | 3 celdas radio | — |

> Los efectos elementales (Burn, Slow proyectil, ArmorReduction) se aplican exclusivamente a través de **cartas**, no como torres dedicadas.

### Detalles por torre

**Torre Melee Lv1 (12 oro)**
- Daño en área, sin proyectil — no puede fallar
- Rol: bloquear camino + ralentizar pasivamente. Ideal en chokepoints

**Torre Sierra — Melee Lv2 (+18 oro, total 30 oro)**
- +87% daño respecto a Lv1
- Rol: mayor DPS en chokepoints establecidos

**Torre de Rango Lv1 (10 oro)**
- 1 disparo/segundo
- Target: enemigo con mayor progreso hacia la meta
- Rol: DPS a distancia, cubre más área que Melee

### Efectividad por monstruo

| Torre | Caminante | Rápido | Blindado |
|-------|-----------|--------|----------|
| Melee | ★★★ | ★★ | ★★ |
| Rango | ★★★ | ★★ | ★ |

---

## 5. Monstruos

### Tabla comparativa

| Monstruo | HP | Velocidad | Armadura | Oro | XP |
|----------|----|-----------|----------|-----|-----|
| Caminante | 60 | 2 c/s | 0% | 2 | 5 |
| Rápido | 40 | 4 c/s | 0% | 3 | 8 |
| Blindado | 200 | 1.5 c/s | 50% físico | 5 | 15 |

### Comportamiento de spawn

- **Caminante:** aparece en grupos. Enemigo tutorial.
- **Rápido:** spawna siempre solo, nunca en grupo.
- **Blindado:** siempre precedido por 3 Caminantes (señal de aviso).

### Mecánicas especiales

- La armadura del Blindado **no afecta el DoT de Burn** (aplicado vía cartas)
- Al llegar a la meta: resta **1 vida** al jugador
- Al morir: da **oro + XP** al jugador

### EffectSystem

Componente de `EnemyBehaviour` que gestiona efectos activos:

| Efecto | Valor | Comportamiento |
|--------|-------|---------------|
| Burn (DoT) | 4 dmg/s | Ignora armadura. Nuevo impacto refresca duración, no acumula stacks. Aplicado vía cartas |
| Slow | −40% por carta | Múltiples fuentes acumulan hasta cap **−70%**. Aplicado vía cartas |

---

## 6. Héroe

### Control y movimiento

- **WASD + flechas** — 8 direcciones, velocidad constante (`MoveSpeed = 4 u/s`)
- **Vuelo:** ignora pathfinding y grilla. Se mueve en línea recta sobre torres y monstruos
- Sin colisión con torres ni monstruos
- **Confinado a los límites de pantalla** — no puede salir del viewport de la cámara

### Stats base

| Stat | Valor |
|------|-------|
| Velocidad de movimiento | 4 u/s |
| Daño por ataque | 25 (físico) |
| Rango de ataque | 1.5 celdas de radio (1.44 u) |
| Velocidad de ataque | 1.5 ataques/segundo |
| HP | No tiene — no puede ser dañado en el MVP |

### Combate

- Ataque completamente automático al enemigo **más cercano** dentro de rango
- Con carta *Instinto cazador*: prioriza el enemigo con **mayor HP** en lugar del más cercano
- Sin habilidades activas — el jugador solo controla el posicionamiento

### Construcción (mecánica clave)

- Seleccionar torre en HUD + clic en celda válida → el héroe **se desplaza automáticamente** al tile adyacente más cercano a la celda objetivo
- Al llegar: inicia el timer de construcción de **5 segundos** (manejado por `TowerBehaviour`)
- El héroe puede moverse y atacar libremente durante la construcción
- **Múltiples construcciones en cola** — se procesan en orden; los timers de 5s corren concurrentemente
- WASD durante el desplazamiento automático cancela el auto-movimiento (el jugador retoma control)
- Celda inválida: flash rojo, no se encola

---

## 7. Sistema de XP y cartas

### Barra de XP

| Nivel | XP requerida | Momento estimado |
|-------|-------------|-----------------|
| 1 | 150 XP | ~4 min |
| 2 | 300 XP | ~9 min |
| 3 | 500 XP | ~13 min |

### Flujo de pausa de carta

1. Barra llena → stream **se pausa instantáneamente**
2. Popup centrado con **3 cartas aleatorias** según rareza del nivel
3. Jugador elige una carta → efecto aplica inmediata y permanentemente
4. Stream **se reanuda**
5. Sin tiempo límite de elección

### Rareza por nivel

| Nivel | Distribución |
|-------|-------------|
| 1 | 3 Comunes |
| 2 | 2 Comunes + 1 Rara |
| 3 | 1 Rara + 1 Épica + 1 aleatoria |

### Catálogo de cartas (15)

#### Comunes (8)

| Carta | Efecto |
|-------|--------|
| Filo afilado | Torres Melee +20% daño |
| Pólvora | Torres de Rango +20% daño |
| Rescoldo | Torres con Burn: quemadura 5s (antes 3s) |
| Corriente fría | Torres con Slow: slow −55% (antes −40%) |
| Construcción rápida | Tiempo de construcción 3s (antes 5s) |
| Buen ojo | Héroe +50% rango de ataque |
| Golpe certero | Héroe +30% daño |
| Avaricia | Cada monstruo da +1 oro al morir |

#### Raras (5)

| Carta | Efecto |
|-------|--------|
| Corriente amplificada | Enemigo ralentizado por Slow recibe +25% daño de todas las fuentes |
| Brasas | Al expirar quemadura, AoE de 10 dmg en 1 celda alrededor |
| Economía de guerra | Vender torre devuelve 80% (antes 60%) — una vez por pausa de XP |
| Sierra en cadena | Torre Melee mata enemigo → siguiente en misma celda recibe 50% del daño del kill |
| Instinto cazador | Héroe prioriza enemigo con mayor HP en lugar del más cercano |

#### Épicas (2)

| Carta | Requisito | Efecto |
|-------|-----------|--------|
| Tormenta de fuego | ≥1 torre con Burn aplicado | Torres con Burn también aplican Slow (−25% vel, 1.5s) |
| El laberinto vivo | — | Monstruo que dobla en esquina recibe 15 de daño |

### Sinergias destacadas

| Combinación | Resultado |
|-------------|-----------|
| Corriente fría + Corriente amplificada | Slow máximo + daño amplificado |
| Rescoldo + Corriente amplificada | Quemadura más larga + amplificación = máximo DoT |
| Brasas + Sierra en cadena | AoE de Brasas puede activar daño en cadena de Sierra |
| El laberinto vivo + construcción activa | Incentiva rediseñar el laberinto en mid-run |

---

## 8. Estructura de la run

### Duración objetivo: ~15 minutos

### Fase de preparación (0:00 — 0:30)

- 30 segundos + **50 oro inicial** para construir torres
- Sin monstruos activos
- Countdown visible centrado en pantalla
- No se puede extender

**Opciones típicas con 50 oro:**
- 5 Torres de Rango (50 oro)
- 4 Rango + 1 Melee (52 oro → sobra algo)
- 3 Rango + 2 Melee (54 oro → requiere ahorrar)

### Stream de monstruos (0:30 → fin)

| Período | Composición | Intervalo | Notas |
|---------|-------------|-----------|-------|
| 0:30 — 3:00 | 100% Caminantes | 1 cada 3s | Tutorial implícito del laberinto |
| 3:00 — 6:00 | 75% Caminantes · 25% Rápidos | 1 cada 2.5s | Rápidos siempre solos |
| 6:00 — 10:00 | 60% Caminantes · 20% Rápidos · 20% Blindados | 1 cada 2s | Blindados precedidos por 3 Caminantes |
| 10:00 — 14:00 | 50% Caminantes · 25% Rápidos · 25% Blindados | 1 cada 1.5s | Presión máxima antes del boss |
| 14:00+ | Stream reducido (mixto) | 1 cada 4s | Boss activo, stream de soporte |

### Victoria y derrota

- **Victoria:** boss muere → pantalla de resultados
- **Derrota:** jugador pierde las **5 vidas** → game over con opción de reintentar
- Sin penalización por derrota — cada run empieza igual

---

## 9. Boss final — Troll Anciano

### Aparición

- Entra desde el spawn al minuto **~14:00**
- El stream normal continúa a baja intensidad (1 cada 4s, mixto)

### Stats base

| Stat | Valor |
|------|-------|
| HP | 1500 |
| Velocidad | 0.8 c/s |
| Armadura | 25% reducción física |
| Recompensa | 30 oro / 50 XP |

### Fase 1 (100% → 50% HP)

- Movimiento lento y predecible
- A los **30 segundos** de entrar, invoca **8 Caminantes** desde el spawn

### Fase 2 (50% → 0% HP)

- Velocidad **+60%** → 1.3 c/s
- **Rugido** al activarse: +20% velocidad a todos los monstruos en pantalla durante 6 segundos
- Invoca horda mixta **(5 Caminantes + 3 Rápidos)** cada 40 segundos
- **Regenera 2 HP/segundo** → requiere DPS > 2 HP/s para no estancarse

### Mecánica de diseño

El boss valida el build del jugador:
- **Cartas de Burn** → manejan el HP alto (DoT ignora armadura)
- **Cartas de Slow** → contienen la velocidad de fase 2
- **Sin ninguno** → el jugador pierde vidas inevitablemente

La regeneración en fase 2 castiga builds defensivos con DPS < 2 HP/s.

---

## 10. UI y controles

### Controles

| Acción | Control |
|--------|---------|
| Mover héroe | WASD (funciona siempre, independiente de la selección) |
| Seleccionar héroe | Clic izquierdo en celda vacía / Escape / Clic derecho |
| Seleccionar torre | Clic izquierdo en celda con torre activa |
| Seleccionar tipo de torre para construir | Clic en botón del HUD inferior (solo visible con héroe seleccionado) |
| Colocar torre | Clic izquierdo en celda del mapa (durante modo build) |
| Cancelar modo build | Clic derecho / Escape |
| Vender torre | Seleccionar torre → botón Vender |
| Mejorar torre | Seleccionar torre → botón Mejorar |
| Elegir carta de XP | Clic en la carta deseada |
| Pausa | Escape / P |

### Sistema de selección

- Solo una unidad puede estar seleccionada a la vez: **héroe** o **torre**
- Al iniciar la partida, el héroe está seleccionado por defecto
- **Indicador visual:** elipse verde (solo contorno, centro transparente) debajo de la unidad seleccionada
- La selección determina qué controles del HUD son visibles:
  - **Héroe seleccionado** → botones de construcción visibles
  - **Torre seleccionada** → panel de torre (mejorar/vender) visible, botones de construcción ocultos
- El movimiento WASD y el ataque automático del héroe funcionan **siempre**, sin importar qué unidad está seleccionada
- La detección de clicks en torres usa la **celda de la grilla**, no el collider de física (los colliders de torre son pequeños por el escalado del sprite)

### HUD superior

- **Vidas restantes** — número + icono de corazón (`Heart.png`), máximo **5**
- **Oro actual** — número + icono de moneda (`GoldCoin.png`)
- **Barra de XP** con número de nivel actual

### Panel lateral derecho de información de unidad (siempre visible)

Panel vertical procedural de **200px de ancho**, altura completa, anclado al borde derecho de la pantalla (`Screen Space - Overlay`). El panel **se superpone al juego** — la cámara usa viewport completo (pantalla entera), no se recorta para excluir el panel. Secciones apiladas con `VerticalLayoutGroup` (`childForceExpandHeight = false`); el fondo `#161C16` queda visible debajo del último elemento.

**1. Portrait (70px)**
- Sprite 44×44 de la unidad seleccionada, nombre y subtipo/nivel
- Fondo `#111711`

**2. Stats (124px)**
- 4 filas: label + barra de progreso coloreada + valor numérico
- **Héroe:** Daño, Rango, Vel. ataque, Velocidad de movimiento
- **Torre:** Daño, Rango, Vel. ataque, Efecto especial (texto sin barra)
- Colores de barra: daño `#e24b4a`, rango `#7f77dd`, velocidad `#f0c040`, efecto `#1d9e75`

**3. Cartas (solo héroe: 68px / solo torre: 120px)**

*Con héroe seleccionado:*
- Inventario de cartas del jugador como miniaturas (hasta 6 slots, slots vacíos con borde)

*Con torre seleccionada:*
- 6 slots de efectos aplicados a la torre. Slots ocupados muestran ícono + indicador de rareza. Slots vacíos con signo `+`
- Inventario del jugador como miniaturas clicables. Click → aplica carta permanentemente a la torre (sin deshacer). Cartas incompatibles con el tipo de torre a opacity 35%, no clicables

**4. Construir torre (altura dinámica, solo héroe)**
- Grilla 2×2 (`GridLayoutGroup`, cell 84×48, spacing 4×4) de botones de construcción
- Cada botón: ícono de torre (24×24) + nombre + costo en dorado, apilados verticalmente
- Torres: Melee (12g), Rango (10g)
- Botones para `TowerData` no asignados se omiten automáticamente
- Botón desactivado visualmente (alpha 0.35) cuando el oro del jugador es insuficiente

**5. Acciones (160px, solo torre)**
- Botón(es) **Mejorar** (1 o 2 según upgrade paths disponibles) con nombre y costo
- Botón **Vender** mostrando oro devuelto (60% del total invertido)
- Advertencia: "Cartas aplicadas se pierden al vender" (solo visible si la torre tiene cartas)
- Al vender → héroe reseleccionado automáticamente

### Contador de preparación

- Solo visible durante los 30 segundos iniciales
- Número grande centrado en pantalla, desaparece al comenzar el stream

### Popup de carta de XP

- Fondo semitransparente que oscurece el mapa
- 3 cartas en fila horizontal
- Cada carta: nombre, rareza (color de borde), descripción del efecto
- Hover: descripción extendida

| Rareza | Color de borde |
|--------|---------------|
| Común | Gris / Blanco |
| Rara | Azul |
| Épica | Púrpura |

---

## 11. Assets y arte

### Filosofía MVP

**Legibilidad sobre calidad visual.** El arte final llega después de validar que el juego es divertido.

### Plataforma objetivo: PC (cambio desde diseño original mobile)

El juego comenzó como diseño mobile y ahora apunta a **PC (Windows/Mac)**. Esto implica:

- Tiles más grandes: **96×96 o 128×128 px** (vs. 60×60 px mobile) — más detalle visible por tile
- Sin restricciones de memoria/batería — assets más ricos son viables
- Input: mouse + teclado en lugar de touch

### Estilo visual: 3/4 View (2.5D) — referencia Ball x Pit

**Referencia:** Ball x Pit — que usa perspectiva **3D con cámara 3/4 view**. Block & Blood replica esa sensación visual en **Unity 2D estándar** con sprites top-down y técnicas de pseudo-profundidad. La diferencia clave con isométrico:

| Perspectiva | Cámara | Suelo | Ejemplo |
|-------------|--------|-------|---------|
| **Isométrico** | 45° rotada | Grilla en diamante | Diablo 1, Age of Empires |
| **3/4 view** (elegida) | Inclinada desde arriba y atrás | Grilla rectangular normal | Zelda: ALttP, Pokémon, Enter the Gungeon |

**Qué implica técnicamente:**
- **Unity 2D estándar** — sin Tilemap isométrico, sin configuración especial de grilla
- Grilla rectangular normal (14×18) — **nada cambia** en GridManager ni pathfinding
- Los personajes se ven "desde atrás" al moverse hacia arriba (sprites direccionales)
- La profundidad se logra con sorting por Y, sombras y Sorting Layers (mismas técnicas de TASK-12)

**Cámara perspectiva 2.5D:**
- Tipo: **perspectiva** (`cam.orthographic = false`), `fieldOfView = 60°`
- Tilt X: `CameraTilt = 15°` — rotación en X para efecto 3/4 view
- `CenterCamera()` en GridManager calcula distancia Z para llenar el viewport, con `offsetY = 2.44f` para compensar el desplazamiento del tilt
- Sorting: `TransparencySortMode.CustomAxis`, `sortAxis = Vector3.up`

**Lo que da la sensación de 3/4 view:**

1. **Cámara perspectiva con tilt de 15°** — genera escorzo natural (objetos lejanos más pequeños) + leve inclinación top-down
2. **Sprites direccionales** — héroe y monstruos con al menos 2 vistas (frente/espalda). El personaje se ve "desde atrás" subiendo y "de frente" bajando. Pack **Roguelike Characters** de Kenney tiene exactamente esto
3. **Sombra proyectada bajo entidades dinámicas** — `GameObject` hijo con sprite elipse negra semitransparente (~30% opacidad) bajo cada monstruo, torre y héroe
4. **Sprite Sort Point = "Pivot"** — entidades más abajo en pantalla se dibujan encima, creando profundidad correcta
5. **Sorting Layers por componente de torre** — base y cañón/sierra en layers distintos dan sensación de volumen

### Packs de Kenney recomendados

| Pack | Uso |
|------|-----|
| **Tower Defense Top-Down** (ya incluido) | Torres, tiles de grilla — sprites top-down que funcionan perfecto en 3/4 view |
| **Tower Defense** (original) | Más variantes de torres y monstruos |
| **Roguelike Characters** | Personajes con sprites en 4 direcciones — héroe y monstruos vistos desde atrás/frente |
| **Roguelike/RPG Pack** | Iconos para las cartas roguelike |
| **UI Pack** | HUD, botones, barras, popup de cartas |

### Fuente de assets

**Kenney Tower Defense Top-Down** — gratuito, dominio público, estilo visual consistente.
- Fuente original: `kenney_tower-defense-top-down/` (directorio raíz del proyecto, no modificar)
- Tiles importados a `Assets/Resources/Grid/` renombrados con prefijo `Tile_`
- **Kenney Roguelike Characters** — sprites direccionales para héroe y monstruos

### Tiles de grilla implementados

| Sprite | Nombre en proyecto | Uso |
|--------|-------------------|-----|
| tile116 | `Tile_Restricted` | Fila superior — no buildable |
| grass_base | `Resources/Decorations/grass_base` | Celda libre — pasto (columnas fuera del camino) |
| path_base | `Resources/Decorations/path_base` | Celda libre — camino central (cols 2–4) |
| path_edge_left | `Resources/Decorations/path_edge_left` | Celda libre — borde izquierdo del camino (col 1) |
| path_edge_right | `Resources/Decorations/path_edge_right` | Celda libre — borde derecho del camino (col 5) |
| tile040 | `Tile_Ocupada` | Torre colocada |
| tile085 | `Tile_Building` | En construcción |
| tile086 | `Tile_Invalid` | Flash de rechazo |
| — | `Tile_Black` | Fondo negro escalado detrás de la grilla |

### Decoraciones visuales

Sprites decorativos del sprite sheet `GRASS+.png` (troncos rotos, plantas) colocados en celdas libres para dar variedad visual al suelo. Se eliminan automáticamente al construir una torre en su celda (`TowerPlacementManager` llama `GridVisualizer.RemoveDecoration(cell)`).

### Assets pendientes por TASK

- **[TASK-12]** Configuración visual 3/4 view: Sprite Sort Point, Sorting Layers, sombras, sprites direccionales
- Torres: base + cañón rotatorio (Rango), base + sierra (Melee)
- Monstruos: 3 variantes con sprites direccionales (frente/espalda) — Roguelike Characters
- Proyectiles básicos
- Efectos de estado: partícula de fuego (Burn, vía cartas), partícula de agua/hielo (Slow, vía cartas)
- Barras de HP sobre monstruos
- Hero sprite: `roguelikeChar_magenta_0_transparent.png` asignado (Kenney Roguelike Characters, fondo removido). Escala 4.5 (~75% de una celda). Pendiente: sprites direccionales (frente/espalda) para completar 3/4 view
- Boss sprite más grande e imponente

### Feedback visual requerido

| Situación | Feedback visual |
|-----------|----------------|
| Celda inválida para construir | Flash rojo (tile086) |
| Torre en construcción | Tile llave + barra de progreso |
| Enemigo quemado | Partícula de fuego |
| Enemigo ralentizado | Partícula de agua/hielo |
| HP del monstruo | Barra sobre el sprite |
| Boss fase 2 activada | Cambio visual (color, escala o efecto) |

### Audio mínimo (no bloqueante)

- Sonido de disparo por tipo de torre
- Sonido de muerte de monstruo
- Sonido de colocación de torre
- Música de fondo en loop (Kenney o CC0)

---

## 12. Arquitectura técnica

### Estructura de carpetas

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Managers/
│   │   │   ├── GameManager.cs
│   │   │   ├── GridManager.cs
│   │   │   ├── GridVisualizer.cs       ← IMPLEMENTADO (TASK-01)
│   │   │   ├── SelectionManager.cs    ← IMPLEMENTADO
│   │   │   ├── TowerPlacementManager.cs ← IMPLEMENTADO
│   │   │   ├── WaveManager.cs
│   │   │   ├── EconomyManager.cs
│   │   │   ├── LivesManager.cs          ← IMPLEMENTADO
│   │   │   └── XPManager.cs
│   │   ├── Towers/
│   │   │   ├── TowerBehaviour.cs
│   │   │   ├── ProjectileBehaviour.cs
│   │   │   └── EffectSystem.cs
│   │   ├── Enemies/
│   │   │   └── EnemyBehaviour.cs
│   │   ├── Hero/
│   │   │   └── HeroBehaviour.cs        ← IMPLEMENTADO (TASK-04)
│   │   ├── Shared/
│   │   │   ├── ISelectable.cs           ← IMPLEMENTADO
│   │   │   ├── EntityShadow.cs          ← IMPLEMENTADO
│   │   │   └── TowerType.cs             ← IMPLEMENTADO
│   │   ├── Cards/
│   │   │   ├── CardData.cs              ← IMPLEMENTADO (SO)
│   │   │   ├── PlayerInventory.cs       ← IMPLEMENTADO
│   │   │   ├── CardSystem.cs
│   │   │   └── CardEffect.cs
│   │   └── UI/
│   │       ├── HUDController.cs
│   │       └── CardPopupController.cs
│   ├── ScriptableObjects/
│   │   ├── Towers/         TowerData SO × 3
│   │   ├── Enemies/        EnemyData SO × 3
│   │   └── Cards/          CardData SO × 15
│   ├── Prefabs/
│   ├── Scenes/
│   └── Art/
│       ├── Sprites/
│       │   └── Hero/           roguelikeChar_magenta_0_transparent.png
│       └── UI/
├── Resources/
│   ├── Grid/               ← IMPLEMENTADO (TASK-01) — sprites de tiles en runtime
│   └── Decorations/        GRASS+.png (sprite sheet), grass_base.png, path_base.png, path_edge_left.png, path_edge_right.png
├── AstarPathfindingProject/ Plugin A* (no modificar)
└── Kenney/                 Assets externos (no modificar)
```

### Sistemas y responsabilidades

| Sistema | Responsabilidad |
|---------|----------------|
| GameManager | Estado global de la run (referencias a managers) |
| LivesManager | Vidas del jugador (5 iniciales), escucha OnEnemyReachedGoal, emite OnLivesChanged/OnGameOver |
| GridManager | Estado de celdas + validación pathfinding + configuración A* |
| GridVisualizer | Tiles visuales (grass/path sprites por columna) + decoraciones + feedback de colocación |
| WaveManager | Stream de monstruos + pausas por XP |
| XPManager | Acumula XP, emite `OnLevelUp` |
| EconomyManager | Oro: ingresos y gastos |
| CardSystem | Pool de cartas + distribución por rareza + aplicación de efectos |
| SelectionManager | Selección de unidad (héroe/torre), indicador visual, drive HUD visibility |
| TowerPlacementManager | Orquesta selección de torre para construir, validación, instanciación |
| PlayerInventory | Inventario de cartas del jugador (máx 6). AddCard/SpendCard, emite OnInventoryChanged |
| EffectSystem | Burn y Slow por enemigo + acumulación hasta caps |

### ScriptableObjects

**TowerData**
```csharp
string towerName
TowerType type          // Melee / Range
int cost
float damageBase
float attackSpeed
float range
DamageType damageType   // Physical / Fire / Water
GameObject prefab
TowerData[] upgradePaths
```

**EnemyData**
```csharp
string enemyName
float hp
float speed
float armor             // 0.0–1.0 (porcentaje de reducción)
int goldReward
int xpReward
GameObject prefab
```

**CardData**
```csharp
string cardName
CardData.Rarity cardRarity  // Common / Rare / Epic
string description
Sprite icon
TowerType[] compatibleTowerTypes  // vacío = compatible con todos
```

**WavePhase**
```csharp
float startTime
float endTime
float spawnInterval
EnemySpawnWeight[] composition   // tipo + peso probabilístico
```

### Input

El proyecto usa el **New Input System** (`UnityEngine.InputSystem`). No usar `UnityEngine.Input` (legacy).

- Movimiento: `Keyboard.current.[key].isPressed`
- Clicks: `Mouse.current.leftButton/rightButton.wasPressedThisFrame`
- Posición de mouse: `Mouse.current.position.ReadValue()`

### Patrones obligatorios

- **Object Pooling** para monstruos (~30 activos), proyectiles (~50 activos) y efectos visuales — usar `UnityEngine.Pool.ObjectPool<T>`
- **ScriptableObjects** para todos los datos de configuración
- **Eventos C#** para comunicación entre managers (no referencias directas)
- **A\* Pathfinding Project** — no implementar A\* desde cero

### Convenciones de código

- Clases, métodos, propiedades públicas: `PascalCase`
- Variables privadas: `_camelCase` con prefijo `_`
- ScriptableObjects: sufijo `Data` (`TowerData`, `EnemyData`, `CardData`)
- Eventos: prefijo `On` (`OnLevelUp`, `OnEnemyDeath`)

### Lo que NO tocar sin revisión cuidadosa

- `GridManager.CanPlaceTower` — un bug aquí rompe el juego
- Números de balance — están en ScriptableObjects, no hardcodeados
- Lógica de aplicación de cartas épicas (`Tormenta de fuego`, `El laberinto vivo`)

---

## 13. Criterios de éxito del MVP

### Técnicos — Mes 4

- [ ] Run completa de ~15 minutos sin crashes
- [ ] Pathfinding recalcula en tiempo real sin stutters visibles
- [ ] Las 15 cartas funcionan correctamente
- [ ] El boss completa sus 2 fases
- [ ] 60 fps estables en hardware de gama media

### Diseño — Mes 5

**Al menos 3 de 5 testers externos, sin explicación previa, quieren hacer otra run inmediatamente.**

Este es el único criterio que importa para decidir si continuar hacia la versión completa.

### Métricas secundarias de playtesting

| Pregunta | Objetivo |
|----------|---------|
| ¿El jugador entiende el pathfinding solo? | Sí |
| ¿El jugador cambia estrategia después de perder vidas con Blindados? | Sí |
| ¿El jugador recuerda qué cartas eligió al terminar? | Sí |
| ¿El jugador comenta sinergias entre cartas? | Sí |

### Contexto post-MVP

Este GDD cubre exclusivamente el MVP. El diseño completo (Mundos 2–5, Torres Lv3, desbloqueo permanente) existe como documento separado y se retoma después de validar el MVP.

---

## Historial de cambios

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0 | 2026-03-17 | Creación del GDD.md a partir del PDF original + TASK files |
| 1.1 | 2026-03-17 | TASK-01 completada: grilla visual con tiles Kenney, celdas restringidas (fila 8), A* configurado en runtime, GridVisualizer implementado |
| 1.2 | 2026-03-17 | TASK-04 completada: HeroBehaviour implementado (WASD + flechas, vuelo libre, confinamiento a pantalla, ataque automático, cola de construcción con auto-movimiento al tile adyacente, stats modificables por cartas); sprite towerDefense_tile250 asignado; proyecto usa New Input System |
| 1.3 | 2026-03-17 | §11 actualizado: plataforma PC confirmada (antes mobile), estilo visual Ball x Pit (pseudo-profundidad top-down), tres técnicas de profundidad (sombras, Sprite Sort Point Pivot, Sorting Layers), packs Kenney recomendados, TASK-12 creada |
| 1.4 | 2026-03-18 | Corrección visual: Ball x Pit es 3D con 3/4 view (no isométrico ni top-down puro). Se adopta **3/4 view (2.5D)** en Unity 2D estándar. §3 restaura grilla rectangular. §11 reescrito: estilo 3/4 view, packs Kenney actualizados (añade Roguelike Characters para sprites direccionales, Tower Defense original), sprites direccionales para personajes (frente/espalda). TASK-12 reescrita. TASK-09 actualizada |
| 1.5 | 2026-03-18 | Grilla ampliada de 5×9 a **7×9** (63 celdas). `RestrictedCells` actualizadas (7 celdas en fila 8). `_gridOrigin` ajustado a x=-0.96 para mantener centrado. `GridVisualizer` con `[ExecuteAlways]` (tiles visibles en Edit mode). Héroe: sprite `roguelikeChar_magenta_0_transparent.png` con fondo transparente, escala 4.5 (~75% de una celda) |
| 1.6 | 2026-03-21 | Sistema de vidas implementado: **5 vidas** (antes 20). `LivesManager` creado (singleton, escucha `OnEnemyReachedGoal`, emite `OnLivesChanged`/`OnGameOver`). HUD actualizado: vidas con icono de corazón (`Heart.png`), oro con icono de moneda (`GoldCoin.png`) sin sufijo "g". Nuevos assets en `Art/UI/` |
| 1.7 | 2026-03-22 | Visual del suelo reemplazado: tiles Kenney → sprite pixel art `GRASS+_58` (16×16, tiling sin costuras). Sprite sheet `GRASS+.png` sliceado (350 sprites) e importado a `Resources/Decorations/`. Fondo `Tile_Black` escalado. Sistema de decoraciones: 4 sprites decorativos (troncos, plantas) en celdas libres, eliminados automáticamente al construir torre. `GrassSpriteSheetSlicer` editor script añadido |
| 1.8 | 2026-03-23 | Sistema de selección implementado: `SelectionManager` (singleton en GameManager), interfaz `ISelectable` (implementada por `HeroBehaviour` y `TowerBehaviour`). Elipse verde (ring con centro transparente) indica unidad seleccionada. Héroe seleccionado por defecto; click en celda con torre → selecciona torre; click vacío/ESC/right-click → reselecciona héroe. HUD condicionado a selección: botones de construcción solo visibles con héroe seleccionado, panel torre solo con torre seleccionada. Detección de clicks basada en celda de grilla (no colliders). WASD y ataque automático siempre activos |
| 1.9 | 2026-03-23 | Panel inferior de información de unidad: HUDController reescrito completamente. Panel 148px fijo inferior con 4 secciones (Portrait 100px, Stats flex, Cards 210px, Actions 160px). Construido proceduralmente en código. Nuevos sistemas: `TowerType` enum, `CardData` SO (nombre, ícono, rareza, compatibilidad por tipo de torre), `PlayerInventory` singleton (máx 6 cartas, AddCard/SpendCard, evento OnInventoryChanged). `TowerBehaviour` extendido con `AppliedEffects` (máx 6 cartas permanentes), `ApplyCard()`, evento `OnEffectApplied`. 4 botones de construcción (Melee/Rango/Fuego/Agua). Cartas aplicables a torres desde el inventario con filtro de compatibilidad. Botones vender/mejorar en sección Actions. Eliminados: botones flotantes anteriores y panel lateral derecho |
| 1.10 | 2026-03-24 | HUD migrado de panel inferior a **panel lateral derecho** (200px ancho, altura completa). Viewport de cámara ajustado para excluir franja del panel. Secciones en `VerticalLayoutGroup` vertical. Botones de construcción reestructurados: grilla 2×2 (`GridLayoutGroup`, cell 84×48) con layout vertical por botón (ícono 24×24 + nombre + costo). Botones para `TowerData` null se omiten; altura de sección dinámica. `_fireTowerData` y `_waterTowerData` asignados en Inspector (`TowerFire_Lv2`, `TowerWater_Lv2`). Botón desactivado a alpha 0.35 |
| 1.11 | 2026-03-24 | **Torres Fuego y Agua eliminadas.** Los efectos elementales (Burn, Slow, ArmorReduction) ahora se aplican exclusivamente vía cartas. `TowerType` enum reducido a Melee/Range. Eliminados: `TowerFire_Lv2` y `TowerWater_Lv2` (SOs + prefabs), upgrade paths de torre Rango vaciados, `_fireTowerData`/`_waterTowerData` del HUD. Botones de construcción reducidos a 2 (Melee/Rango). Cartas actualizadas para referenciar efectos en vez de torres. Boss: estrategia ahora requiere cartas de Burn/Slow en vez de torres dedicadas |
| 1.12 | 2026-03-24 | **Tiles de suelo diferenciados por columna.** `GRASS+_58` uniforme reemplazado por 4 sprites artesanales: `grass_base` (pasto), `path_base` (camino central, cols 2–4), `path_edge_left` (col 1), `path_edge_right` (col 5). `GridVisualizer`: constantes `PathColMin=2`/`PathColMax=4`, método `GetTileSprite(col,row)` para asignar sprite según columna. Nuevos assets en `Resources/Decorations/` |
| 2.1 | 2026-03-25 | **HUD superpuesto al juego.** El panel lateral derecho ya no reduce el viewport de la cámara — `Camera.main.rect = Rect(0,0,1,1)` (pantalla completa). El panel `Screen Space - Overlay` se superpone sobre el área jugable en lugar de estar al lado de ella |
| 2.0 | 2026-03-25 | **Grilla ampliada de 7×9 a 14×18** (252 celdas). `CellSize` reducido de 0.96 a **0.48**. Spawn movido a fila 17 (arriba), meta a fila 0 (abajo). `_gridOrigin` centrado en origen mundo. **Cámara perspectiva 2.5D:** `fieldOfView=60°`, `CameraTilt=15°`, `CenterCamera()` calcula distZ para llenar viewport con `offsetY=2.44f` para compensar tilt. A* graph reconfigurado (14×18, nodeSize=0.48) |

# Block & Blood — CLAUDE.md

## Skills activos para este proyecto
Aplicar siempre los principios de estos sub-skills al trabajar en tareas de desarrollo:
- `game-development/pc-games` — plataforma objetivo: PC (Steam/Desktop)
- `game-development/2d-games` — dimensión: 2D sprites, tilemaps, física 2D

---

Tower Defense roguelite en Unity 6000.3.11f1. El jugador construye un laberinto en una grilla 14×18, elige cartas de bono entre niveles de XP y defiende contra oleadas continuas de monstruos.

**GDD completo:** `GDD — Line Wars TD · MVP.md` (raíz del proyecto) — fuente de verdad editable
**GDD snapshot histórico:** `GDD — Line Wars TD · MVP.pdf` (ya no se actualiza)
**Tareas detalladas:** `Docs/Tasks/TASK-01` a `TASK-12`

---

## Estructura del proyecto

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Managers/       GameManager, GridManager, GridVisualizer, WaveManager, EconomyManager, XPManager, LivesManager, SelectionManager, TowerPlacementManager
│   │   ├── Towers/         TowerBehaviour, ProjectileBehaviour, EffectSystem
│   │   ├── Enemies/        EnemyBehaviour
│   │   ├── Hero/           HeroBehaviour
│   │   ├── Cards/          CardData, PlayerInventory, CardSystem, CardEffect
│   │   ├── Shared/          ISelectable, EntityShadow, TowerType
│   │   └── UI/             HUDController, CardPopupController, CursorManager
│   ├── ScriptableObjects/
│   │   ├── Towers/         TowerData SO × 3 (Melee Lv1, Melee Lv2, Rango Lv1)
│   │   ├── Enemies/        EnemyData SO × 3 (Caminante, Rápido, Blindado)
│   │   └── Cards/          CardData SO × 15
│   ├── Prefabs/
│   ├── Scenes/
│   └── Art/
│       ├── Sprites/        GRASS+.png (sprite sheet pixel art 400×224, sliceado 16×16, 350 sprites)
│       └── UI/             Heart.png (icono vidas), GoldCoin.png (icono oro)
├── Resources/
│   ├── Grid/               Sprites de tiles cargados en runtime (Tile_Restricted, Tile_Black, etc.)
│   └── Decorations/        GRASS+.png (sprite sheet), grass_base.png, path_base.png, path_edge_left.png, path_edge_right.png
├── AstarPathfindingProject/ Plugin A* Pathfinding Project (no modificar)
└── Kenney/                 Assets externos — no modificar
```

---

## Sistemas principales

### GridManager
- Grilla de **14 columnas × 18 filas** (252 celdas, `CellSize = 0.48f` unidades mundo)
- Spawn: fila 17 (visual superior) · Meta: fila 0 (visual inferior)
- Estados de celda: `Libre` / `EnConstrucción` / `Ocupada`
- **Celdas restringidas:** fila 17 completa (14 celdas) — permanentemente no buildable
- Expone `bool CanPlaceTower(Vector2Int cell)` — valida bounds + estado + `IsRestricted` + pathfinding **antes** de confirmar construcción
- Expone `bool IsRestricted(Vector2Int cell)` — consulta `RestrictedCells[]`
- Configura el `GridGraph` de A* en `Awake()` (width=14, depth=18, nodeSize=0.48, is2D=true)
- Notifica al PathfindingSystem via `AstarPath.active.UpdateGraphs(bounds)` cuando una celda cambia
- `_gridOrigin` se calcula para centrar el grid en el origen mundo: `(-(gridWidth/2), -(gridHeight/2), 0)`
- Pathfinding: **A\* Pathfinding Project** instalado en `Assets/AstarPathfindingProject/`

### Cámara (perspectiva 2.5D)
- **Tipo:** perspectiva (`cam.orthographic = false`) — no ortográfica
- **FOV:** `CameraFOV = 60°` · **Tilt X:** `CameraTilt = 15°` (efecto 3/4 view)
- **`CenterCamera()`** en GridManager calcula la distancia Z para que el grid llene el viewport:
  - `distFromH` y `distFromW` → se toma el mayor, multiplicado por `0.85f` para acercar la cámara
  - `offsetY = 2.44f` — compensa el desplazamiento visual del tilt (la cámara baja en Y para centrar el grid)
  - Posición final: `(GridCenter.x, GridCenter.y - offsetY, -distZ)`
  - Rotación: `Euler(-CameraTilt, 0, 0)`
- **Sorting:** `TransparencySortMode.CustomAxis`, `sortAxis = Vector3.up`
- **Se llama dos veces:** en `GridManager.Awake()` y en `HUDController.ApplyCameraViewport()` (viewport siempre `Rect(0,0,1,1)` — pantalla completa)
- **NO modificar** FOV ni tilt sin verificar que el grid sigue llenando el viewport

### GridVisualizer
- `[ExecuteAlways]` — los tiles se crean también en Edit mode (visibles en Scene view sin Play)
- **Suelo diferenciado por columna:** 4 sprites artesanales (16×16, PPU 16) cargados desde `Resources/Decorations/`:
  - `grass_base` — pasto (columnas fuera del camino: 0, 6)
  - `path_base` — camino central (cols `PathColMin=2` a `PathColMax=4`)
  - `path_edge_left` — borde izquierdo del camino (col 1)
  - `path_edge_right` — borde derecho del camino (col 5)
- `LoadTileSprites()` carga los 4 sprites en `Awake()` · `GetTileSprite(col, row)` selecciona sprite según columna
- **Fondo:** sprite `Tile_Black` escalado ×30 detrás de toda la grilla (cubre bordes exteriores)
- **Celdas restringidas:** `Tile_Restricted` (tile116) — las que no están ocultas por `_hideRestrictedVisual`
- **Decoraciones:** sprites decorativos (`GRASS+_310`, `GRASS+_311`, `GRASS+_291`, `GRASS+_317`) colocados en celdas libres para variedad visual. `RemoveDecoration(cell)` los elimina al construir una torre
- `FlashTile(cell, valid)` — feedback visual: verde (válido) / rojo + X (inválido), 3 parpadeos
- `RefreshTile(cell)` / `RefreshAll()` — actualiza sprites según estado actual de la celda
- **Editor script:** `Assets/Editor/GrassSpriteSheetSlicer.cs` — sliceo automático de `GRASS+.png` en 350 sprites (25×14 grid de 16×16px) vía `Tools > Slice GRASS+ Sprite Sheet`

### WaveManager
- Lee `WavePhase` ScriptableObjects (composición + spawn rate por tramo de tiempo)
- Spawna desde Object Pool
- Escucha `XPManager.OnLevelUp` para pausar/reanudar el stream

### XPManager
- Acumula XP de `EnemyBehaviour.OnDeath`
- Emite `OnLevelUp` al completar cada barra
- 3 niveles por run: 150 / 300 / 500 XP

### LivesManager
- Vidas iniciales: **5**
- Cada monstruo que llega a la meta resta **1 vida**
- Escucha `EnemyBehaviour.OnEnemyReachedGoal`
- Eventos: `OnLivesChanged(int)`, `OnGameOver`
- Singleton en el mismo GameObject que `GameManager`

### EconomyManager
- Oro inicial: **50**
- Ingresos: monstruos muertos (Caminante 2, Rápido 3, Blindado 5, Boss 30)
- Gastos: construcción y mejora de torres

### CardData (ScriptableObject)
- `CardName`, `Description`, `Icon` (sprite), `CardRarity` (Common/Rare/Epic)
- `TowerType[] CompatibleTowerTypes` — vacío = compatible con todos los tipos de torre
- `IsCompatibleWith(TowerType)` — consulta si la carta es aplicable a un tipo de torre específico
- Creación: `Create > Block&Blood > CardData`

### CardSystem
- Pool de 15 cartas (8 Comunes, 5 Raras, 2 Épicas)
- Distribución por nivel: Nivel 1 → 3C · Nivel 2 → 2C+1R · Nivel 3 → 1R+1E+1 aleatoria
- Al elegir: `CardEffect.Apply(GameState state)` modifica stats en tiempo real

### SelectionManager
- Singleton en el GameObject `GameManager` — trackea la unidad seleccionada (`ISelectable Current`)
- Al iniciar, el **héroe** está seleccionado por defecto
- **Interfaz `ISelectable`** (`Scripts/Shared/`): implementada por `HeroBehaviour` y `TowerBehaviour`
  - `Transform SelectionTransform` — posición para la elipse de selección
  - `bool IsSelectable` — héroe: siempre true; torre: solo en estado `Active`
- **Indicador visual:** elipse verde procedural (ring, centro transparente), sortingOrder −45
  - Escala: `0.7×0.35` · Color: `(0, 1, 0, 0.7)` · Offset Y: −0.55
  - Se posiciona en `LateUpdate()` sobre el `SelectionTransform` del `ISelectable` activo
- **Detección de clicks:** basada en **celda de la grilla** (no en colliders de física)
  - Click en celda `Ocupada` → busca torre via `Physics2D.OverlapCircle` en el centro de la celda
  - Click en celda vacía / ESC / right-click → reselecciona héroe
  - Clicks durante build mode o sobre UI son ignorados
- Evento: `OnSelectionChanged(ISelectable previous, ISelectable current)`
- **HUDController** escucha `OnSelectionChanged` → reconstruye el panel inferior completo
  - Héroe seleccionado → portrait héroe, stats héroe, inventario + 4 build buttons, mensaje neutro en acciones
  - Torre seleccionada → portrait torre, stats torre, 6 slots de efectos aplicados + inventario clickable, botones mejorar/vender
- El movimiento WASD y ataque automático del héroe funcionan **siempre**, independiente de la selección

### PlayerInventory
- Singleton (componente en `GameManager`)
- Gestiona hasta **6 cartas** en el inventario del jugador
- `AddCard(CardData)` — agrega carta (retorna false si lleno)
- `SpendCard(CardData, TowerBehaviour)` — remueve carta del inventario y la aplica a la torre
- Evento: `OnInventoryChanged` → HUDController refresca sección de cartas
- Las cartas se obtienen del popup de XP level-up (CardPopupController, pendiente)

### HUDController (panel lateral derecho)
- **Panel procedural** de 200px ancho, altura completa, anclado al borde derecho, `Screen Space - Overlay`
- El panel **se superpone al juego** — la cámara usa viewport completo `Rect(0,0,1,1)`, no se reduce para excluir el panel
- Top HUD (oro, vidas) anclado a top-left del canvas
- **Secciones en `VerticalLayoutGroup`** (stacking vertical, `childForceExpandHeight = false`):
  - **Portrait** (70px): sprite 44×44 + nombre + subtipo, fondo `#111711`
  - **Stats** (124px): 4 barras de progreso coloreadas (daño `#e24b4a`, rango `#7f77dd`, velocidad `#f0c040`, efecto `#1d9e75`)
  - **HeroCards** (68px, solo héroe): inventario (6 slots)
  - **TowerCards** (120px, solo torre): 6 slots efectos aplicados + inventario clickable con filtro de compatibilidad por `TowerType`
  - **Build** (altura dinámica, solo héroe): grilla 2×2 de botones de construcción (`GridLayoutGroup`, `CellSize 84×48`, `Spacing 4×4`). Cada botón: ícono 24×24 + nombre + costo en dorado. Botones para `TowerData` null se omiten. Desactivado visual: `alpha 0.35` cuando oro insuficiente
  - **Actions** (160px, solo torre): botones mejorar (1 o 2 según upgrade paths) + vender + advertencia de cartas
- Constantes de barras: `MaxDamage=50`, `MaxRange=4`, `MaxAttackSpeed=5`, `MaxMoveSpeed=8`
- Escucha: `OnSelectionChanged`, `OnGoldChanged`, `OnInventoryChanged`, `OnEffectApplied`, `OnTowerSold`, `OnTowerUpgraded`
- **SerializeFields necesarios en Inspector:** `_goldText`, `_goldIcon`, `_livesText`, `_heartIcon`, `_meleeTowerData`, `_rangeTowerData`

### EffectSystem (componente en EnemyBehaviour)
- Gestiona `Burn` (DoT 4 dmg/s, ignora armadura) y `Slow` (−40%, acumula hasta −70%)
- Burn: nuevo impacto refresca duración (no acumula stacks)
- Slow: múltiples fuentes acumulan hasta cap −70%

---

## Torres

| Torre | Costo | Daño | Rango | Efecto |
|-------|-------|------|-------|--------|
| Melee Lv1 | 12 oro | 15 dps (físico) | Celda + 8 adyacentes | Slow −15% |
| Melee Lv2 (Sierra) | +18 oro | 28 dps | = Lv1 | = Lv1 |
| Rango Lv1 | 10 oro | 20/proyectil (físico) | 3 celdas radio | — |

- Construcción: **5 segundos** — el héroe puede moverse y atacar libremente durante ese tiempo
- Venta: devuelve **60%** del costo total (construcción + mejoras)
- **TowerType** (`enum`): cada `TowerData` tiene un campo `Type` (Melee/Range) — usado por `CardData.IsCompatibleWith()` para filtrar cartas aplicables
- Los efectos elementales (Burn, Slow, ArmorReduction) se aplican exclusivamente a través de **cartas**, no como torres dedicadas
- **Cartas aplicadas:** cada torre tiene `AppliedEffects` (máx 6 `CardData`). `ApplyCard(CardData)` agrega permanentemente. Evento `OnEffectApplied`. Las cartas se pierden al vender la torre
- **TotalGoldInvested:** trackea oro invertido (base + mejoras). `SellValue = 60% * TotalGoldInvested`

---

## Monstruos

| Monstruo | HP | Velocidad | Armadura | Oro | XP |
|----------|----|-----------|----------|-----|-----|
| Caminante | 60 | 2 c/s | 0% | 2 | 5 |
| Rápido | 40 | 4 c/s | 0% | 3 | 8 |
| Blindado | 200 | 1.5 c/s | 50% físico | 5 | 15 |

- La armadura del Blindado **no afecta el DoT de Burn** (aplicado vía cartas)
- Rápido: siempre spawna solo, nunca en grupo
- Blindado: siempre precedido por 3 Caminantes

---

## Héroe
- Movimiento WASD, 8 direcciones, **vuela sobre torres** (ignora pathfinding)
- Sin colisión con torres ni monstruos
- Ataque automático al **enemigo más cercano** en rango (1.5 celdas, 25 dmg, 1.5 ataques/s)
- Sin HP — no puede ser dañado en el MVP
- La construcción es remota: seleccionar torre en HUD + clic en celda → 5s de build desde cualquier posición
- Múltiples construcciones simultáneas con timers independientes
- **Sprite:** `Assets/_Project/Art/Sprites/Hero/roguelikeChar_magenta_0_transparent.png` (Kenney Roguelike Characters, fondo magenta removido por script) · **Escala:** 4.5 → 0.72 unidades mundo (~75% de una celda)

---

## Boss — Troll Anciano (~14:00)
- HP: 1500 · Velocidad: 0.8 c/s · Armadura: 25% física · Recompensa: 30 oro / 50 XP
- **Fase 1** (100%→50%): lento; a los 30s invoca 8 Caminantes
- **Fase 2** (50%→0%): velocidad +60% → 1.3 c/s; rugido +20% velocidad a todos 6s; horda mixta cada 40s; regenera **2 HP/s**
- La regeneración de fase 2 requiere DPS > 2 HP/s para no estancarse

---

## Convenciones de código

### Nombrado
- Clases, métodos, propiedades públicas: `PascalCase`
- Variables privadas: `_camelCase` con prefijo `_`
- ScriptableObjects: sufijo `Data` (ej. `TowerData`, `EnemyData`, `CardData`)
- Eventos: prefijo `On` (ej. `OnLevelUp`, `OnEnemyDeath`)

### Patrones obligatorios
- **Object Pooling** para monstruos, proyectiles y efectos visuales — usar `UnityEngine.Pool.ObjectPool<T>`
- **ScriptableObjects** para todos los datos de configuración (torres, monstruos, cartas)
- **Eventos C#** para comunicación entre managers (no referencias directas)
- No implementar A\* desde cero — usar **A\* Pathfinding Project**

### Lo que NO tocar sin revisión cuidadosa
- Lógica de validación del pathfinding en `GridManager.CanPlaceTower` — un bug aquí rompe el juego
- Números de balance (daño, HP, XP, oro) — están en ScriptableObjects, no hardcodeados
- Lógica de aplicación de cartas épicas (`Tormenta de fuego`, `El laberinto vivo`)

---

## Criterios de éxito

### Técnicos (mes 4)
- Run completa de ~15 minutos sin crashes
- Pathfinding recalcula en tiempo real sin stutters
- Las 15 cartas funcionan correctamente
- El boss completa sus 2 fases
- 60 fps estables en hardware de gama media

### Diseño (mes 5)
- **3 de 5 testers quieren hacer otra run inmediatamente** — único criterio que importa para continuar

---

## Assets externos y estilo visual
- **Plataforma:** PC (Windows/Mac) — el diseño original era mobile, ya no aplica
- **Estilo visual:** 3/4 view (2.5D) — inspirado en Ball x Pit (que es 3D). Unity 2D estándar, grilla rectangular normal, sin Tilemap isométrico
- **Kenney Tower Defense Top-Down** — fuente original: `kenney_tower-defense-top-down/` (raíz del proyecto, no modificar). Sprites top-down compatibles con 3/4 view
- **Kenney Roguelike Characters** — sprites direccionales (frente/espalda/izq/der) para héroe y monstruos. Clave para el estilo 3/4 view
- **Kenney Tower Defense (original)** — más variantes de torres y monstruos
- **Kenney Roguelike/RPG Pack** — iconos para cartas
- **Kenney UI Pack** — HUD, botones, barras
- Tiles importados a `Assets/Resources/Grid/` según necesidad — renombrados con prefijo `Tile_`
- Arte final se implementa post-validación del MVP
- Audio no es bloqueante — el juego debe ser jugable en silencio

### Configuración visual 3/4 view (TASK-12)
- Héroe y monstruos con **sprites direccionales** (mínimo frente/espalda) — cambian según dirección de movimiento
- `Camera`: `Transparency Sort Mode = Custom Axis`, `Y = 1`
- Todos los `SpriteRenderer` de entidades: `Sprite Sort Point = Pivot`, pivot en la base del sprite
- Sorting Layers (orden inferior a superior): `Ground` → `Shadows` → `TowerBase` → `Characters` → `TowerTop` → `Effects` → `UI`
- Cada monstruo/héroe/torre tiene un hijo `Shadow` (sprite elipse, alpha 0.3, escala `0.7×0.35`, layer `Shadows`)

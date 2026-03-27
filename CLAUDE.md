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
│   │   ├── Enemies/        EnemyBehaviour, EnemyPool, PriestBehaviour, BruteBehaviour
│   │   ├── Hero/           HeroBehaviour
│   │   ├── Cards/          CardData, PlayerInventory, CardSystem, CardEffect
│   │   ├── Data/           CardRarity (enum standalone)
│   │   ├── Shared/          ISelectable, EntityShadow, TowerType
│   │   ├── UI/             HUDController, CardPopupController, CursorManager, GameOverScreen
│   │   └── Waves/          WavePhase (SO), WaveManager
│   ├── ScriptableObjects/
│   │   ├── Towers/         TowerData SO × 3 (Melee Lv1, Melee Lv2, Rango Lv1)
│   │   ├── Enemies/        EnemyData SO × 5 (Caminante, Rápido, Blindado, Sacerdote, Bruto)
│   │   ├── Cards/          CardData SO × 15
│   │   └── WavePhases/     WavePhase_01–42 (assets de oleadas, 20 s cada una, 840 s total)
│   ├── Prefabs/
│   │   └── Enemies/        Enemy_Caminante, Enemy_Rapido, Enemy_Blindado, Enemy_Sacerdote, Enemy_Bruto
│   ├── Scenes/
│   └── Art/
│       ├── Sprites/        GRASS+.png (sprite sheet pixel art 400×224, sliceado 16×16, 350 sprites)
│       └── UI/             Heart.png (icono vidas), GoldCoin.png (icono oro)
├── Resources/
│   ├── Grid/               Sprites de tiles cargados en runtime (Tile_Restricted, Tile_Black, etc.)
│   └── Decorations/        GRASS+.png (sprite sheet), grass_base.png, path_base.png, path_edge_left.png, path_edge_right.png, rockPath_1.png, rockPath_2.png, rockPath_3.png, rockPath_4.png
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
- **Columnas jugables:** solo cols `GridVisualizer.PathColMin` (2) a `GridVisualizer.PathColMax` (11); cols 0, 1, 12, 13 son no buildables y no transitables
- Expone `bool CanPlaceTower(Vector2Int cell)` — valida bounds + estado + `IsRestricted` + columna jugable + pathfinding **antes** de confirmar construcción
- Expone `bool IsRestricted(Vector2Int cell)` — consulta `RestrictedCells[]`
- Configura el `GridGraph` de A* en `Awake()` (width=14, depth=18, nodeSize=0.48, is2D=true); tras el scan marca como `Walkable=false` todos los nodos en cols < `PathColMin` o > `PathColMax`
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
- **`PathColMin = 2` y `PathColMax = 11` son `public const int`** — fuente de verdad para los límites del área jugable; leídos por GridManager, HeroBehaviour y WaveManager. No hardcodear esos números en otros scripts.
- **Suelo diferenciado por columna:** sprites artesanales (16×16, PPU 16) cargados desde `Resources/Decorations/`:
  - `grass_base` — pasto (cols 0 y 13, fuera del camino y no jugables)
  - `path_edge_left` — borde izquierdo del camino (col 1, no jugable)
  - `path_edge_right` — borde derecho del camino (col 12, no jugable)
  - `rockPath_1` … `rockPath_4` — **variantes del camino central** (cols `PathColMin=2` a `PathColMax=11`); elegidas al azar por celda con seed determinístico `col * 1000 + row` → resultado fijo entre regeneraciones
- `LoadTileSprites()` carga todos los sprites en `Awake()`, incluyendo `_pathVariants[4]` · `GetTileSprite(col, row)` selecciona sprite según columna; en cols de camino elige variante con `Random.InitState(col*1000+row)` y restaura aleatoriedad global con `TickCount`
- **Fondo:** sprite `Tile_Black` escalado ×30 detrás de toda la grilla (cubre bordes exteriores)
- **Celdas restringidas:** `Tile_Restricted` (tile116) — las que no están ocultas por `_hideRestrictedVisual`
- **Decoraciones:** sprites decorativos (`GRASS+_310`, `GRASS+_311`, `GRASS+_291`, `GRASS+_317`) colocados en celdas libres para variedad visual. `RemoveDecoration(cell)` los elimina al construir una torre
- `FlashTile(cell, valid)` — feedback visual: verde (válido) / rojo + X (inválido), 3 parpadeos
- `RefreshTile(cell)` / `RefreshAll()` — actualiza sprites según estado actual de la celda
- **Editor script:** `Assets/Editor/GrassSpriteSheetSlicer.cs` — sliceo automático de `GRASS+.png` en 350 sprites (25×14 grid de 16×16px) vía `Tools > Slice GRASS+ Sprite Sheet`

### GameManager (máquina de estados)
- `GameState` enum: `Preparation` / `Playing` / `Paused` / `Victory` / `Defeat`
- `static event Action<GameState> OnGameStateChanged` — todos los sistemas escuchan este evento
- `CurrentState` propiedad pública; `TransitionTo(GameState)` con guard de mismo estado
- **Preparation:** `_prepDuration = 5s`; `EconomyManager.Add(50)` en `Start()`; build countdown procedural (TMP, `sortingOrder=100`)
- **Playing:** al expirar countdown o al elegir una carta (`CardSystem.OnCardChosen`)
- **Paused:** al dispararse `XPManager.OnLevelUp`
- **Victory:** al dispararse `BossBehaviour.OnBossDefeated`
- **Defeat:** al dispararse `LivesManager.OnGameOver`
- **Singleton:** componente en el GameObject `GameManager` junto con `LivesManager`, `EconomyManager`, `XPManager`, `SelectionManager`, `PlayerInventory`, `EnemyPool`

### WaveManager
- Lee `WavePhase[]` ScriptableObjects — composición + intervalo por tramo de tiempo de juego
- **Tiempo de juego:** `PlayingElapsed = Time.time - _playingStartTime - _totalPausedTime` donde `_playingStartTime` se fija cuando Playing comienza por primera vez (no desde el inicio del juego)
- `SpawnLoop` coroutine: drena `_spawnQueue` primero (precursores del Blindado), luego selecciona enemigo por fase activa
- **Reglas especiales:** Rápido nunca dos seguidos (re-roll; si insiste, fuerza otro tipo). Blindado siempre enqueue [Caminante×3, Blindado]
- `SpawnNext`: columna aleatoria entre `GridVisualizer.PathColMin` y `GridVisualizer.PathColMax` inclusive (cols 2–11), fila spawn=17, meta=0; llama `EnemyPool.Instance.Spawn()`
- Escucha `GameManager.OnGameStateChanged`: Playing → `StartSpawning`, Paused → `StopSpawning`, Defeat/Victory → detiene y vacía cola
- **Inspector obligatorio:** `_phases` (array de 5 WavePhase SOs), `_caminanteData`, `_rapidoData`, `_blindadoData` — si alguno está vacío/null, no se spawnea nada sin error visible

### WavePhase (ScriptableObject)
- `[CreateAssetMenu(menuName = "Block&Blood/WavePhase")]`
- Campos: `float StartTime`, `float EndTime`, `float SpawnInterval`, `EnemySpawnWeight[] Composition`
- `EnemySpawnWeight`: struct con `EnemyData Data` + `float Weight` (peso probabilístico)
- **Los tiempos son relativos al inicio del estado Playing** (no al inicio del juego). WavePhase_01.StartTime=0 significa "spawnear inmediatamente al comenzar Playing"
- Assets en `ScriptableObjects/WavePhases/`:

42 fases de 20 s cada una (0–840 s). Al llegar a 840 s, WaveManager dispara `OnBossPhaseStart` y detiene el stream.
Brute y Priest aparecen desde fase 04/05. Armored (Blindado) se introduce en fase 25.

| Fases | Intervalo | Composición (pesos) |
|-------|-----------|---------------------|
| 01 (0–20) | 1.40s | Walker 100 |
| 02 (20–40) | 1.40s | W80 R20 |
| 03 (40–60) | 1.35s | W70 R30 |
| 04 (60–80) | 1.30s | W65 R30 Br5 |
| 05 (80–100) | 1.30s | W55 R30 Br10 P5 |
| 06 (100–120) | 1.25s | W50 R30 Br10 P10 |
| 07 (120–140) | 1.25s | W45 R35 Br12 P8 |
| 08 (140–160) | 1.20s | W42 R35 Br13 P10 |
| 09 (160–180) | 1.20s | W40 R35 Br15 P10 |
| 10 (180–200) | 1.15s | W38 R35 Br15 P12 |
| 11 (200–220) | 1.15s | W35 R35 Br17 P13 |
| 12 (220–240) | 1.10s | W33 R35 Br17 P15 |
| 13 (240–260) | 1.10s | W30 R35 Br20 P15 |
| 14 (260–280) | 1.05s | W28 R35 Br20 P17 |
| 15 (280–300) | 1.05s | W27 R33 Br22 P18 |
| 16 (300–320) | 1.00s | W25 R33 Br22 P20 |
| 17 (320–340) | 1.00s | W25 R30 Br25 P20 |
| 18 (340–360) | 0.95s | W23 R30 Br27 P20 |
| 19 (360–380) | 0.95s | W22 R28 Br28 P22 |
| 20 (380–400) | 0.90s | W20 R28 Br30 P22 |
| 21 (400–420) | 0.90s | W20 R27 Br30 P23 |
| 22 (420–440) | 0.87s | W18 R27 Br32 P23 |
| 23 (440–460) | 0.87s | W18 R25 Br32 P25 |
| 24 (460–480) | 0.85s | W17 R25 Br33 P25 |
| 25 (480–500) | 0.85s | W20 R22 Br28 P20 A10 |
| 26 (500–520) | 0.82s | W18 R22 Br27 P18 A15 |
| 27 (520–540) | 0.82s | W17 R20 Br27 P18 A18 |
| 28 (540–560) | 0.80s | W15 R20 Br27 P18 A20 |
| 29 (560–580) | 0.80s | W15 R18 Br27 P17 A23 |
| 30 (580–600) | 0.78s | W13 R18 Br27 P17 A25 |
| 31 (600–620) | 0.75s | W12 R18 Br28 P17 A25 |
| 32 (620–640) | 0.73s | W12 R17 Br28 P18 A25 |
| 33 (640–660) | 0.71s | W10 R17 Br28 P20 A25 |
| 34 (660–680) | 0.69s | W10 R15 Br30 P20 A25 |
| 35 (680–700) | 0.67s | W10 R15 Br30 P20 A25 |
| 36 (700–720) | 0.65s | W8 R15 Br30 P22 A25 |
| 37 (720–740) | 0.63s | W8 R13 Br32 P22 A25 |
| 38 (740–760) | 0.62s | W7 R13 Br32 P23 A25 |
| 39 (760–780) | 0.61s | W7 R12 Br33 P23 A25 |
| 40 (780–800) | 0.60s | W5 R12 Br33 P25 A25 |
| 41 (800–820) | 0.60s | W5 R10 Br35 P25 A25 |
| 42 (820–840) | 0.60s | W5 R10 Br35 P25 A25 |

Leyenda: W=Walker R=Runner Br=Brute P=Priest A=Armored

### EnemyPool
- Singleton en el GameObject `GameManager`
- Un `ObjectPool<EnemyBehaviour>` por `EnemyData` (creado lazy en primer uso), capacidad 30/máx 60
- `Spawn(EnemyData, Vector3 spawnPos, Vector3 goalPos)` — activa desde pool o instancia el `EnemyData.Prefab`
- `Despawn(EnemyBehaviour)` — identifica el pool correcto via `_activeMap<EnemyBehaviour, pool>` y libera
- `EnemyBehaviour.ReturnToPool()` llama `EnemyPool.Instance?.Despawn(this)`, fallback `_pool.Release()`, fallback `Destroy()`
- `EffectSystem.ClearEffects()` se llama en `EnemyBehaviour.Initialize()` al salir del pool

### EnemyBehaviour — API pública relevante
- `public float MaxHp` — HP máximo del tipo (delegado a `EnemyData.MaxHp`)
- `public void Heal(float amount)` — restaura HP hasta MaxHp; llamado por `PriestBehaviour`
- `public const float BruteAuraArmorBonus = 0.3f` — bono de armadura del aura de Bruto
- `public bool IsUnderBruteAura` — true cuando `_bruteAuraCount > 0`
- `public void AddBruteAura()` / `RemoveBruteAura()` — llamados por `BruteBehaviour` al entrar/salir del radio
- `Initialize()` resetea `_bruteAuraCount = 0` — garantiza estado limpio al salir del pool

### XPManager
- Acumula XP de `EnemyBehaviour.OnEnemyDeath`
- **100 XP por nivel, máximo nivel 15** (antes: umbrales fijos 150/300/500 con 3 niveles)
- Ritmo esperado: jugador eficiente sube ~1 nivel/minuto; dejar pasar enemigos penaliza implícitamente
- `CurrentXp`, `CurrentLevel` (0 = sin level-up aún)
- `static event Action<int> OnXpChanged` — fired con XP total al ganar XP
- `static event Action OnLevelUp` — fired al cruzar cada umbral; `GameManager` transiciona a `Paused`
- `static CardRarity GetRarityForCurrentLevel()`:
  - Niveles 1–5: 80% Común, 20% Rara
  - Niveles 6–10: 50% Común, 40% Rara, 10% Épica
  - Niveles 11–15: 20% Común, 50% Rara, 30% Épica

### LivesManager
- Vidas iniciales: **5**
- Cada monstruo que llega a la meta resta **1 vida**
- Escucha `EnemyBehaviour.OnEnemyReachedGoal`
- Eventos: `OnLivesChanged(int)`, `OnGameOver`
- Singleton en el mismo GameObject que `GameManager`

### EconomyManager
- Oro inicial: **50**
- Ingresos: monstruos muertos (Caminante 2, Rápido 3, Blindado 5, Sacerdote 4, Bruto 6, Boss 30)
- Gastos: construcción y mejora de torres

### CardData (ScriptableObject)
- `CardName`, `Description`, `Icon` (sprite), `CardRarity` (Common/Rare/Epic)
- `TowerType[] CompatibleTowerTypes` — vacío = compatible con todos los tipos de torre
- `IsCompatibleWith(TowerType)` — consulta si la carta es aplicable a un tipo de torre específico
- Creación: `Create > Block&Blood > CardData`

### CardSystem
- **MonoBehaviour singleton** en el GameObject `GameManager`
- Escucha `XPManager.OnLevelUp` → genera 3 cartas placeholder (`ScriptableObject.CreateInstance<CardData>()`) según rareza del nivel via `XPManager.GetRarityForCurrentLevel()`
- Muestra picker procedural (Canvas `sortingOrder=200`): overlay oscuro + panel centrado 580×260px con título + fila de 3 cartas clickeables (160×140px cada una)
- Colores de rareza: Común blanco `(0.9,0.9,0.9)`, Rara amarillo `(1,0.85,0.2)`, Épica violeta `(0.75,0.4,1)`
- Al elegir: `PlayerInventory.Instance?.AddCard(chosen)` + destruye canvas + `OnCardChosen?.Invoke()`
- `static event Action OnCardChosen` — escuchado por `GameManager` para transicionar de `Paused` a `Playing`
- **Enum dual:** `CardRarity` standalone (`Scripts/Data/CardRarity.cs`) para XPManager/CardSystem; `CardData.Rarity` (nested) para los SO de cartas; mapeados via switch expression
- Distribución de rareza delegada a `XPManager.GetRarityForCurrentLevel()` (pesos por rango de nivel)

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
- Las cartas se obtienen al elegir en el picker de `CardSystem` (el popup se dispara automáticamente en cada `OnLevelUp`)

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
- Escucha: `OnSelectionChanged`, `OnGoldChanged`, `OnInventoryChanged`, `OnEffectApplied`, `OnTowerSold`, `OnTowerUpgraded`, `XPManager.OnXpChanged`, `XPManager.OnLevelUp`, `EnemyBehaviour.OnEnemyDeath`
- **Iconos oro y corazón:** cada par icono→número está en un `HorizontalLayoutGroup` (spacing 4px, `childForceExpandWidth=false`). El icono tiene `LayoutElement` con tamaño fijo igual a `fontSize` del TMP adyacente. El panel usa `ContentSizeFitter` horizontal para crecer con el número (sin ancho fijo). El TMP tiene `enableWordWrapping=false` y `overflowMode=Overflow`
- **Barra de XP:** `BuildXpHUD()` crea el GameObject `XPBar` anclado al borde inferior del canvas (anchorMin=`(0.05,0)`, anchorMax=`(0.95,0)`, margen 20px, alto 10px). Fondo semitransparente `(0.06,0.06,0.06, 120/255)`. Hijo `XPFill`: `Image.Type.Simple`, color `#C8A840`; el fill se controla moviendo `rectTransform.anchorMax.x` entre 0 y 1 (NO usar `fillAmount` — Unity no lo respeta sin sprite en modo Filled). Hijo `XpLabel`: TMP centrado, 11pt, blanco. `RefreshXp(int)` calcula `xpRelativa = xp - nivel*100`, `anchorMax.x = xpRelativa/100f`. Al nivel 15: anchorMax.x=1, texto "MAX"
- **SerializeFields necesarios en Inspector:** `_goldText`, `_goldIcon`, `_livesText`, `_heartIcon`, `_meleeTowerData`, `_rangeTowerData`

### TowerPlacementManager
- Singleton en el GameObject `GameManager`
- `SelectTower(TowerData)` — activa el modo placement; dispara `OnTowerSelected` (escuchado por `CursorManager`)
- `CancelSelection()` — desactiva el modo; dispara `OnPlacementCancelled`. **Solo se llama en estos casos:**
  - Clic derecho o ESC (`HeroBehaviour.HandleCancelInput`)
  - Click en botón de otra torre del HUD (llama `SelectTower` con el nuevo tipo)
  - Click en celda con torre ya construida (SelectionManager la selecciona)
  - Oro insuficiente al intentar construir (`RequestPlacement` lo detecta y llama `CancelSelection` + flash rojo)
- `RequestPlacement(cell, data)` — llamado por `HeroBehaviour` al llegar al tile adyacente; valida con `CanPlaceTower`, gasta oro e instancia la torre. **Tras una construcción exitosa NO cancela el modo** — el preview del cursor permanece activo con el mismo `TowerData` para permitir construir múltiples torres sin volver a pulsar el botón
- Eventos: `OnTowerSelected(TowerData)`, `OnPlacementCancelled`, `OnTowerPlaced(TowerBehaviour)`

### CursorManager
- Singleton en escena; escucha `TowerPlacementManager.OnTowerSelected` / `OnPlacementCancelled`
- **Modo normal:** cursor de hardware personalizado (`_cursorTexture` / `_cursorDownTexture`)
- **Modo placement:** cursor oculto; sprite del prefab snapeado a la celda de la grilla, tintado verde (`_validColor`) o rojo (`_invalidColor`) según `CanPlaceQuick()`
- **`CanPlaceQuick(cell)`** — check ligero por frame (sin A*): bounds + `CellState.Libre` + no SpawnCell/GoalCell + `IsRestricted` + **columna en `[PathColMin, PathColMax]`**. La validación completa con pathfinding corre en click (`CanPlaceTower`)
- El sprite del preview toma `TowerData.Icon`; si es null, coge el `SpriteRenderer` del prefab. Escala igual al prefab
- Hijo `CursorIcon` (0.3 u) pegado a la esquina inferior-derecha del preview

### GameOverScreen
- **MonoBehaviour** en el GameObject `GameManager`
- Escucha `GameManager.OnGameStateChanged`
- **Defeat** → muestra "DERROTA" + "Llegaron demasiados enemigos a la base"
- **Victory** → muestra "VICTORIA" + "¡Derrotaste al Troll Anciano!"
- Canvas procedural `sortingOrder=300` (encima del CardSystem a 200). Panel centrado 480×290px con título (56pt bold) + subtítulo (15pt) + botón "Reintentar" → `SceneManager.LoadScene(GetActiveScene().name)`
- Se destruye y recrea en cada estado relevante; `_canvas.SetActive(false)` para otros estados

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
- **Modo placement persistente:** tras construir una torre exitosamente, el modo placement **no se cancela** — el cursor mantiene el preview activo para construir otra del mismo tipo de inmediato. Se cancela solo con clic derecho, ESC, cambio de tipo de torre u oro insuficiente
- Venta: devuelve **60%** del costo total (construcción + mejoras)
- **TowerType** (`enum`): cada `TowerData` tiene un campo `Type` (Melee/Range) — usado por `CardData.IsCompatibleWith()` para filtrar cartas aplicables
- Los efectos elementales (Burn, Slow, ArmorReduction) se aplican exclusivamente a través de **cartas**, no como torres dedicadas
- **Cartas aplicadas:** cada torre tiene `AppliedEffects` (máx 6 `CardData`). `ApplyCard(CardData)` agrega permanentemente. Evento `OnEffectApplied`. Las cartas se pierden al vender la torre
- **TotalGoldInvested:** trackea oro invertido (base + mejoras). `SellValue = 60% * TotalGoldInvested`

---

## Monstruos

| Monstruo | HP | Velocidad | Armadura | Oro | XP |
|----------|----|-----------|----------|-----|-----|
| Caminante | 150 | 1.2 c/s | 0% | 2 | 5 |
| Rápido | 40 | 4 c/s | 0% | 3 | 8 |
| Blindado | 200 | 1.5 c/s | 50% físico | 5 | 15 |
| Sacerdote | 200 | 2.0 c/s | 0% | 4 | 12 |
| Bruto | 650 | 1.2 c/s | 20% físico | 6 | 18 |

- La armadura del Blindado **no afecta el DoT de Burn** (aplicado vía cartas)
- Rápido: siempre spawna solo, nunca en grupo
- Blindado: siempre precedido por 3 Caminantes
- **Sacerdote (`PriestBehaviour`):** cada 2 s cura el 15% del HP máximo a todos los enemigos en radio 1.92 u, incluyéndose a sí mismo. No cura a otros Sacerdotes. La curación no supera el HP máximo del objetivo. El timer de curación se reinicia al salir y volver del pool.
- **Bruto (`BruteBehaviour`):** aura pasiva en radio 1.92 u — otorga +30% armadura física a todos los enemigos en rango (incluido él mismo). Múltiples Brutos no acumulan: `EnemyBehaviour` lleva un contador `_bruteAuraCount`; `IsUnderBruteAura` es true cuando el contador > 0. El aura se recalcula cada frame. La constante del bono es `EnemyBehaviour.BruteAuraArmorBonus = 0.3f`.

---

## Héroe
- Movimiento WASD, 8 direcciones, **vuela sobre torres** (ignora pathfinding)
- Sin colisión con torres ni monstruos
- **Confinado horizontalmente** a las columnas jugables (`GridVisualizer.PathColMin`–`GridVisualizer.PathColMax`, cols 2–11) — `ClampToScreen()` calcula los límites en X a partir del origen de la grilla. Movimiento en Y libre (filas 0–17)
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

### Errores conocidos y sus causas (post-mortem)

**EnemyData.Prefab con fileID incorrecto → `InvalidCastException` en Instantiate**
- Al crear o reasignar prefabs de enemigos via YAML/script, el campo `Prefab` en el SO queda con `fileID: 100100000` (ID legacy). Eso hace que `Instantiate(prefab)` lance `InvalidCastException` porque no apunta al root GameObject.
- El fileID correcto es el anchor `&` del objeto `!u!1` (GameObject) en el archivo `.prefab`. En los prefabs actuales de enemigos ese valor es `5651935703564863863`.
- **Siempre asignar prefabs desde el Inspector de Unity**, nunca editando el YAML a mano con fileID arbitrarios.

**WaveManager duplicado en la escena → eventos recibidos dos veces**
- El GameObject `GameManager` tuvo en un momento dos componentes `WaveManager`. Ambos se suscriben a `OnGameStateChanged` y ambos inician su propio `SpawnLoop`.
- Verificar con el Inspector que `GameManager` tiene exactamente **un** componente `WaveManager`.

**WaveManager con `_phases` vacío → silencio total, sin spawn ni error**
- Si el array `_phases` o los campos `_caminanteData/_rapidoData/_blindadoData` están en null en el Inspector, `GetActivePhase()` siempre devuelve null y el SpawnLoop no hace nada. No hay error en consola.
- Después de añadir o recrear el WaveManager en la escena, verificar siempre que estos campos están asignados.

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

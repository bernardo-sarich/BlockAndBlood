# Block & Blood — CLAUDE.md

## Skills activos
- `game-development/pc-games` · `game-development/2d-games`

Tower Defense roguelite, Unity 6000.3.11f1. Grilla 14×18, cartas de bono por XP, oleadas continuas de monstruos.
**GDD:** `GDD — Line Wars TD · MVP.md` (fuente de verdad) · **Tareas:** `Docs/Tasks/TASK-01` a `TASK-12`

---

## Estructura del proyecto

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Managers/   GameManager, GridManager, GridVisualizer, WaveManager, EconomyManager, XPManager, LivesManager, SelectionManager, TowerPlacementManager
│   │   ├── Towers/     TowerBehaviour, ProjectileBehaviour, EffectSystem
│   │   ├── Enemies/    EnemyBehaviour, EnemyAnimator, EnemyPool, HealOrb, PriestBehaviour, BruteBehaviour
│   │   ├── Hero/       HeroBehaviour
│   │   ├── Cards/      CardData, PlayerInventory, CardSystem, CardEffect
│   │   ├── Data/       CardRarity (enum standalone)
│   │   ├── Shared/     ISelectable, EntityShadow, TowerType
│   │   ├── UI/         HUDController, CardPopupController, CursorManager, GameOverScreen
│   │   └── Waves/      WavePhase (SO), WaveManager
│   ├── ScriptableObjects/
│   │   ├── Towers/     TowerData SO × 3 (Melee Lv1, Melee Lv2, Rango Lv1)
│   │   ├── Enemies/    EnemyData SO × 5 (Caminante, Rápido, Blindado, Sacerdote, Bruto)
│   │   ├── Cards/      CardData SO × 15
│   │   └── WavePhases/ WavePhase_01–42 (20 s c/u, 840 s total)
│   ├── Prefabs/Enemies/ Enemy_Caminante, Enemy_Rapido, Enemy_Blindado, Enemy_Sacerdote, Enemy_Bruto, HealOrb
│   └── Art/
│       ├── Sprites/    GRASS+.png (400×224, 16×16, 350 sprites)
│       ├── Enemies/    spider_sheet.png 4f 32×32 PPU32 guid:47b54cba9bbc9b643b35a004a3646539 (Rápido)
│       │               zombie_32x32-sheet.png 4f 32×32 PPU48 guid:cb37bb16db387ef4aa3bc29d8ff6ed69 (Caminante)
│       │               priest/priest_walk.png 4f 32×32 PPU48 guid:8f6ec1208a22dc9449294e50e52a1990
│       │               priest/priest_cast.png 3f 32×32 PPU48 guid:df1832685dff3274fa5a40f7dc5032c4
│       │               priest/heal_orb_DRAFT.png 4f 16×16 PPU16 guid:1492210df7157444aa7747234dbcb27b
│       └── UI/         Heart.png, GoldCoin.png
├── Resources/
│   ├── Grid/           Tile_Restricted, Tile_Black, etc. (cargados en runtime)
│   └── Decorations/    GRASS+.png, grass_base.png, path_base.png, path_edge_left.png, path_edge_right.png, rockPath_1–4.png
├── AstarPathfindingProject/  (NO modificar)
└── Kenney/                   (NO modificar)
```

---

## Sistemas principales

### GridManager
- Grilla **14×18** · `CellSize = 1.0f` · Spawn: fila 17 · Meta: fila 0
- Estados celda: `Libre` / `EnConstrucción` / `Ocupada`
- Celdas restringidas: fila 17 completa (permanentemente no buildable)
- **Columnas jugables:** `PathColMin=2` a `PathColMax=11` (cols 0,1,12,13 no buildables ni transitables)
- `CanPlaceTower(Vector2Int)` — valida bounds + estado + `IsRestricted` + columna + pathfinding
- `GridGraph` A* en `Awake()`: width=14, depth=18, nodeSize=1.0, is2D=true; nodos cols < PathColMin o > PathColMax → `Walkable=false`
- `AstarPath.active.UpdateGraphs(bounds)` al cambiar celda
- `_gridOrigin = (-(gridWidth/2), -(gridHeight/2), 0)`

### Cámara (perspectiva 2.5D)
- Perspectiva, `FOV=60°`, `Tilt=15°`
- `CenterCamera()`: toma el mayor de `distFromH`/`distFromW` × 0.85f; `offsetY = gridHeight * 0.2824f`
- Pos final: `(GridCenter.x, GridCenter.y - offsetY, -distZ)` · Rot: `Euler(-15, 0, 0)`
- `TransparencySortMode.CustomAxis`, `sortAxis = Vector3.up`
- Se llama en `GridManager.Awake()` y `HUDController.ApplyCameraViewport()` · viewport `Rect(0,0,1,1)` siempre
- **NO modificar FOV/tilt** sin verificar que el grid sigue llenando el viewport

### GridVisualizer
- `[ExecuteAlways]` — tiles visibles en Edit mode
- **`PathColMin = 2` y `PathColMax = 11` son `public const int`** — fuente de verdad; leídos por GridManager, HeroBehaviour, WaveManager
- Suelo por columna (sprites 16×16 PPU16 desde `Resources/Decorations/`):
  - Cols 0,13 → `grass_base` · Col 1 → `path_edge_left` · Col 12 → `path_edge_right`
  - Cols 2–11 → `rockPath_1…4`, seed determinístico `col*1000+row`
- `Tile_Black` ×30 como fondo · `Tile_Restricted` (tile116) en celdas restringidas
- Decoraciones: `GRASS+_310/311/291/317` · `RemoveDecoration(cell)` al construir torre
- `FlashTile(cell, valid)` — verde válido / rojo+X inválido, 3 parpadeos
- `RefreshTile(cell)` / `RefreshAll()`
- Editor: `Assets/Editor/GrassSpriteSheetSlicer.cs` → `Tools > Slice GRASS+ Sprite Sheet`

### GameManager (máquina de estados)
- `GameState`: `Preparation` / `Playing` / `Paused` / `Victory` / `Defeat`
- `static event Action<GameState> OnGameStateChanged`
- `TransitionTo(GameState)` con guard de mismo estado
- Preparation: `_prepDuration=5s`, `EconomyManager.Add(50)`, countdown TMP `sortingOrder=100`
- Playing: al expirar countdown o `CardSystem.OnCardChosen` → `timeScale=1`
- Paused: `XPManager.OnLevelUp` → `timeScale=0`
- Victory: `BossBehaviour.OnBossDefeated` · Defeat: `LivesManager.OnGameOver`
- **Singleton GO `GameManager`** contiene: `LivesManager`, `EconomyManager`, `XPManager`, `SelectionManager`, `PlayerInventory`, `EnemyPool`, `WaveManager`, `CardSystem`, `GameOverScreen`

### WaveManager
- Lee `WavePhase[]` SOs · `PlayingElapsed = Time.time - _playingStartTime - _totalPausedTime`
- `SpawnLoop`: drena `_spawnQueue` (precursores Blindado) → selecciona por fase activa
- Rápido: nunca 2 seguidos (re-roll/fuerza). Blindado: enqueue [Caminante×3, Blindado]
- `SpawnNext`: col aleatoria PathColMin–PathColMax, fila 17→0, llama `EnemyPool.Instance.Spawn()`
- Playing→`StartSpawning` · Paused→`StopSpawning` · Defeat/Victory→detiene y vacía cola
- **Inspector obligatorio:** `_phases[]`, `_caminanteData`, `_rapidoData`, `_blindadoData`

### WavePhase (ScriptableObject)
- `[CreateAssetMenu("Block&Blood/WavePhase")]`
- Campos: `float StartTime`, `EndTime`, `SpawnInterval` · `EnemySpawnWeight[] Composition` (Data + Weight)
- Tiempos relativos al inicio de Playing. 42 fases × 20s = 840s. A 840s dispara `OnBossPhaseStart`.
- Brute/Priest desde fase 04/05. Armored desde fase 25.

| Fases | Intervalo | Composición |
|-------|-----------|-------------|
| 01 (0–20) | 1.40s | W100 |
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

W=Walker R=Runner Br=Brute P=Priest A=Armored

### EnemyAnimator
- En raíz del prefab junto al `SpriteRenderer`
- `walkSprites[]`: orden `[dir0_f0, dir0_f1…, dir3_fN]`
- `_walkCols`: **9** para LPC 4×9; **4** para sheets de 4 frames horizontales
- Dirección desde `AIPath.velocity`: Up=0, Left=1, Down=2, Right=3. Frame 0=idle
- `public bool IsLocked` — cuando true, Update() no ejecuta (PriestBehaviour lo usa)

### EnemyPool
- Singleton en GO `GameManager` · `ObjectPool<EnemyBehaviour>` por EnemyData (lazy, cap 30/máx 60)
- `Spawn(EnemyData, spawnPos, goalPos)` · `Despawn(EnemyBehaviour)` via `_activeMap`
- `ReturnToPool()`: `EnemyPool.Instance?.Despawn` → fallback `_pool.Release()` → fallback `Destroy()`
- `EffectSystem.ClearEffects()` llamado en `EnemyBehaviour.Initialize()`

### EnemyBehaviour — API relevante
- `public float MaxHp` · `public void Heal(float amount)`
- `public const float BruteAuraArmorBonus = 0.3f`
- `public bool IsUnderBruteAura` (true cuando `_bruteAuraCount > 0`)
- `AddBruteAura()` / `RemoveBruteAura()` · `Initialize()` resetea `_bruteAuraCount = 0`

### XPManager
- 100 XP/nivel, máximo nivel 15
- `CurrentXp`, `CurrentLevel` · `static event Action<int> OnXpChanged` · `static event Action OnLevelUp`
- `GetRarityForCurrentLevel()`: Niv 1–5: 80%C/20%R · Niv 6–10: 50%C/40%R/10%E · Niv 11–15: 20%C/50%R/30%E

### LivesManager
- 5 vidas iniciales · −1 por enemigo en meta
- Escucha `EnemyBehaviour.OnEnemyReachedGoal`
- Eventos: `OnLivesChanged(int)`, `OnGameOver`

### EconomyManager
- 50 oro inicial · Ingresos: Caminante 2, Rápido 3, Blindado 5, Sacerdote 4, Bruto 6, Boss 30

### CardData (ScriptableObject)
- `CardName`, `Description`, `CardRarity` · `Icon`, `IconFrames[]` · `DisplayIcon` = `Icon ?? IconFrames?[0]`
- `BonusDamage` — como `DamageType.Fire` (ignora armadura) · `OnHitEffects[]` · `TowerType[] CompatibleTowerTypes`
- `IsCompatibleWith(TowerType)` · Create menu: `Block&Blood > CardData`
- **Ubicación obligatoria:** `Assets/Resources/Cards/`
- **`mainObjectFileID: 11400000`** en el `.meta` — sin esto `Resources.LoadAll` devuelve vacío

### CardSystem
- MonoBehaviour singleton en GO `GameManager`
- `OnLevelUp` → `Resources.LoadAll<CardData>("Cards")`, filtra por rareza, elige 3 sin repetir (`HashSet<int>`)
- Picker Canvas `sortingOrder=200`: overlay + panel 580×260 + 3 cartas 160×140
  - Animación: `WaitForSecondsRealtime(1f/8f)` (crítico para `timeScale=0`) · Sprite: `card.DisplayIcon`
  - Colores: Común `(0.9,0.9,0.9)` · Rara `(1,0.85,0.2)` · Épica `(0.75,0.4,1)`
- `ShowPicker()`: `IsPickerActive=true`, cursor visible, `CursorLockMode.Confined`
- `ChooseCard()`: `IsPickerActive=false`, `CursorLockMode.None`, `PlayerInventory.Instance?.AddCard(chosen)`, destruye canvas, `OnCardChosen?.Invoke()`
- `public static bool IsPickerActive` — HeroBehaviour y CursorManager lo consultan
- `static event Action OnCardChosen` — GameManager transiciona Paused→Playing
- Enum dual: `CardRarity` standalone (XPManager/CardSystem) vs `CardData.Rarity` (nested SO); mapeados via switch

### SelectionManager
- Singleton GO `GameManager` · `ISelectable Current` · Héroe seleccionado por defecto
- `ISelectable`: `Transform SelectionTransform`, `bool IsSelectable`
- Elipse verde: `sortingOrder=−45`, escala `0.7×0.35`, color `(0,1,0,0.7)`, offsetY −0.55, en `LateUpdate()`
- Click basado en celda: Ocupada → `Physics2D.OverlapCircle` → torre; vacía/ESC/right-click → héroe
- Paused + torre seleccionada → `SelectHero()` automáticamente
- Evento: `OnSelectionChanged(ISelectable prev, ISelectable curr)` → HUDController reconstruye panel

### PlayerInventory
- Singleton GO `GameManager` · hasta 6 cartas
- `AddCard(CardData)` (false si lleno) · `SpendCard(CardData, TowerBehaviour)`
- Evento: `OnInventoryChanged`

### HUDController
- Panel 200px derecha, `Screen Space - Overlay`, viewport completo `Rect(0,0,1,1)`
- `VerticalLayoutGroup`, `childForceExpandHeight=false`. Secciones:
  - **Portrait** 70px: sprite 44×44 + nombre + subtipo, bg `#111711`
  - **Stats** 124px: 4 barras (dmg `#e24b4a`, rango `#7f77dd`, vel `#f0c040`, efecto `#1d9e75`). Máximos: Dmg=50, Range=4, AtkSpd=5, MoveSpd=8
  - **HeroCards** 68px: 6 slots inventario
  - **TowerCards** 120px: 6 slots efectos + inventario clickable filtrado por TowerType
  - **Build** (héroe): `GridLayoutGroup`, CellSize 84×48, Spacing 4×4, ícono 24×24 + nombre + costo. Alpha 0.35 si oro insuficiente
  - **Actions** 160px (torre): mejorar + vender + advertencia cartas
- Barra XP: GO `XPBar` anchorMin`(0.05,0)`, anchorMax`(0.95,0)`, 20px margen, 10px alto. Fill `XPFill` color `#C8A840`; controlar con `rectTransform.anchorMax.x` (NO `fillAmount`). Nivel 15 → anchorMax.x=1, texto "MAX"
- Escucha: `OnSelectionChanged`, `OnGoldChanged`, `OnInventoryChanged`, `OnEffectApplied`, `OnTowerSold`, `OnTowerUpgraded`, `XPManager.OnXpChanged`, `XPManager.OnLevelUp`, `EnemyBehaviour.OnEnemyDeath`
- **SerializeFields:** `_goldText`, `_goldIcon`, `_livesText`, `_heartIcon`, `_meleeTowerData`, `_rangeTowerData`

### TowerPlacementManager
- `SelectTower(TowerData)` → `OnTowerSelected` · `CancelSelection()` → `OnPlacementCancelled`
- `CancelSelection()` solo en: clic derecho/ESC, cambio de tipo, click en torre existente, oro insuficiente
- `RequestPlacement(cell, data)` — valida, gasta oro, instancia. **Construcción exitosa NO cancela el modo** (permite construir múltiples)
- Eventos: `OnTowerSelected(TowerData)`, `OnPlacementCancelled`, `OnTowerPlaced(TowerBehaviour)`

### CursorManager
- Modo normal: cursor hardware (`_cursorTexture` / `_cursorDownTexture`)
- Modo placement: cursor oculto; sprite snapeado a celda, verde/rojo según `CanPlaceQuick()`
- `CanPlaceQuick(cell)`: bounds + Libre + no Spawn/GoalCell + IsRestricted + col∈[PathColMin,PathColMax]. Sin A*
- Preview: `TowerData.Icon` o SpriteRenderer del prefab, misma escala. Hijo `CursorIcon` (0.3u) esquina inferior-derecha

### GameOverScreen
- Escucha `OnGameStateChanged`. Canvas `sortingOrder=300`, panel 480×290
- Defeat → "DERROTA" + "Llegaron demasiados enemigos" · Victory → "VICTORIA" + "¡Derrotaste al Troll Anciano!"
- Botón "Reintentar" → `SceneManager.LoadScene(GetActiveScene().name)`

### EffectSystem
- `Burn`: DoT 4 dmg/s ignora armadura, nuevo impacto refresca duración
- `Slow`: −40%, acumula hasta −70%

---

## Torres

| Torre | Costo | Daño | Rango | Efecto |
|-------|-------|------|-------|--------|
| Melee Lv1 | 12 | 15 dps (físico) | 3 celdas | Slow −15% |
| Melee Lv2 | +18 | 28 dps | 3 celdas | Slow −15% |
| Rango Lv1 | 10 | 20/proyectil (físico) | 6 celdas | — |

- Construcción: 5 s · Venta: 60% del TotalGoldInvested
- Efectos elementales solo vía cartas
- `AppliedEffects` máx 6 · `ApplyCard()` → `RebuildEffectiveEffects()`
- `RebuildEffectiveEffects()`: `_effectiveDamageBase` = DamageBase (físico) · `_effectiveBonusDamage` = Σ BonusDamage cartas (Fire, ignora armadura) · `_effectiveOnHitEffects` = unión OnHitEffects
- `ProjectileBehaviour.Launch(target, damage, damageType, bonusDamage=0f)`
- `TowerType` enum (Melee/Range) — usado por `CardData.IsCompatibleWith()`

---

## Monstruos

| Monstruo | HP | Vel (c/s) | Armadura | Oro | XP |
|----------|----|-----------|----------|-----|-----|
| Caminante | 150 | 1.2 | 0% | 2 | 5 |
| Rápido | 40 | 4.0 | 0% | 3 | 8 |
| Blindado | 200 | 1.5 | 50% físico | 5 | 15 |
| Sacerdote | 200 | 1.2 | 0% | 4 | 12 |
| Bruto | 650 | 1.2 | 20% físico | 6 | 18 |

- Blindado: armadura NO afecta Burn · siempre precedido por [Caminante×3]
- Rápido: nunca en grupo
- **Sacerdote:** cada 2s cura 15% MaxHp a enemigos en radio 4 celdas (no a otros Sacerdotes). Se detiene (`canMove=false`), cast 3f a 8fps, frame 2 → `HealOrb` visual (4u/s). `EnemyAnimator.IsLocked=true` durante cast. Al reanudar: `SearchPath()`.
- **Bruto:** aura radio 4 celdas → +30% armadura física a todos en rango. Contador `_bruteAuraCount`; no acumulan múltiples Brutos.

---

## Héroe
- WASD, 8 dir, vuela sobre torres (ignora pathfinding), sin colisiones, sin HP
- Confinado a cols 2–11 (`ClampToScreen()`), Y libre (filas 0–17)
- Ataque automático: enemigo más cercano, rango 1.5 celdas, 25 dmg, 1.5/s
- Construcción remota: HUD + click → 5s. Múltiples construcciones simultáneas
- Sprite: `Art/Sprites/Hero/roguelikeChar_magenta_0_transparent.png`, escala 0.72u

---

## Boss — Troll Anciano (~14:00)
- HP 1500 · 0.8 c/s · 25% armadura física · 30 oro / 50 XP
- Fase 1 (100→50%): a los 30s invoca 8 Caminantes
- Fase 2 (50→0%): vel 1.3 c/s; rugido +20% vel a todos cada 6s; horda mixta cada 40s; regen 2 HP/s (requiere DPS > 2)

---

## Convenciones de código

- `PascalCase` clases/métodos/props · `_camelCase` privados · sufijo `Data` para SOs · prefijo `On` para eventos
- Object Pooling: `UnityEngine.Pool.ObjectPool<T>` · SOs para config · Eventos C# entre managers · No reimplementar A*
- **Unidades en celdas** siempre; el código multiplica por `GridManager.CellSize` (1.0f)
- Sprites 64×64 PPU=64 → 1 world unit/celda → `localScale=(1,1,1)`

### NO tocar sin revisión
- `GridManager.CanPlaceTower` — un bug rompe el juego
- Números de balance (están en SOs)
- Lógica de cartas épicas (`Tormenta de fuego`, `El laberinto vivo`)

---

## Errores conocidos (post-mortem)

**EnemyData.Prefab fileID incorrecto → `InvalidCastException`**
- `fileID: 100100000` (legacy) en el SO al asignar via YAML. fileID correcto = anchor `&` del `!u!1` (GameObject) del prefab = `5651935703564863863`.
- Siempre asignar prefabs desde el Inspector, nunca editando YAML a mano.

**WaveManager duplicado → eventos 2×**
- Verificar que GO `GameManager` tiene exactamente **un** componente `WaveManager`.

**WaveManager `_phases` vacío → sin spawn, sin error**
- `GetActivePhase()` devuelve null silenciosamente. Siempre asignar `_phases`, `_caminanteData`, `_rapidoData`, `_blindadoData` en Inspector.

**Sacerdote/Bruto atraviesan torres → `constrainInsideGraph:0` + `orientation:0`**
- `orientation:0` (ZAxisForward) = modo 3D ignora graph 2D.
- **Obligatorio en prefabs 2D:** `orientation:1` (YAxisForward) · `enableRotation:0` · `constrainInsideGraph:1`
- Al restaurar `canMove=true` en PriestBehaviour llamar `SearchPath()`.

**WASD interrumpe auto-move → cola de construcción trabada**
- Fix: al detectar WASD con `_isAutoMoving==true`, limpiar `_currentBuild=default` y vaciar `_buildQueue`.

**Tile verde residual tras construcción sin oro**
- Fix en `GridVisualizer.FlashRoutine()`: resetear `sr.color=Color.white` al inicio, antes de capturar `origColor`.

**Enemigos oscilan ante gap del laberinto**
- `pickNextWaypointDist` excesivo saltaba waypoints. **Valor correcto: `1.04`** (≈ CellSize + margen).
- `radius` correcto: `0.1` (no 0.5). No subir `pickNextWaypointDist` por encima de 1.0+margen.
- `ForceAllEnemiesRepath`: usar `ai.SearchPath()`, no `ABPath` manual (elude el Seeker → condiciones de carrera).

**`mainObjectFileID:0` en `.meta` de carta → `Resources.LoadAll` vacío**
- Fix: cambiar `mainObjectFileID:0` → `mainObjectFileID:11400000` en el `.meta`, luego reimportar.

**`PlayerInventory` ausente en escena → cartas elegidas descartadas silenciosamente**
- `CardSystem.ChooseCard()` usa `?.AddCard` que no lanza error. Carta se pierde, `OnInventoryChanged` no dispara, HUD no actualiza.
- Fix: agregar `PlayerInventory` como componente del GO `GameManager`.

---

## Assets externos y estilo visual
- PC (Windows/Mac) · 3/4 view (2.5D), Unity 2D estándar, grilla rectangular
- Kenney Tower Defense Top-Down, Roguelike Characters, Roguelike/RPG Pack, UI Pack (no modificar)
- Sorting Layers: `Ground` → `Shadows` → `TowerBase` → `Characters` → `TowerTop` → `Effects` → `UI`
- Cada entidad: hijo `Shadow` (elipse alpha 0.3, escala 0.7×0.35, layer Shadows)
- `SpriteRenderer`: `Sprite Sort Point = Pivot`, pivot en la base del sprite
- Audio no bloqueante — jugable en silencio

## Criterios de éxito
- Técnico (mes 4): run ~15 min sin crashes, pathfinding sin stutters, 15 cartas funcionales, boss 2 fases, 60 fps
- Diseño (mes 5): **3/5 testers quieren otra run inmediatamente**

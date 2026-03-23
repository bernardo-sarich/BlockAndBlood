# TASK-01 — Grid y Pathfinding

**Sección GDD:** 3. Mapa y grilla · 12.3 GridManager

---

## Descripción
Implementar la grilla de juego 5×9 y el sistema de pathfinding A* que permite a los monstruos navegar del spawn a la meta, recalculando en tiempo real cuando se colocan o venden torres.

## Especificaciones

### Grilla
| Parámetro | Valor |
|-----------|-------|
| Dimensiones | 5 columnas × 9 filas (45 celdas) |
| Tamaño de celda | 96×96 px → `CellSize = 0.96f` unidades mundo |
| Spawn | Fila 0 (visual inferior, `SpawnRow = 0`) |
| Meta | Fila 8 (visual superior, `GoalRow = 8`) |
| Celdas restringidas | Fila 8 completa (5 celdas) — no buildable |

### Estados de celda
- **Libre** — transitable, disponible para construir
- **EnConstrucción** — reservada 5s, no transitable, no bloquea pathfinding hasta completarse
- **Ocupada** — torre activa, bloquea pathfinding
- **Restringida** — permanentemente no buildable (fila GoalRow); `IsRestricted()` devuelve `true`

### Visual (Kenney Tower Defense tiles)
| Estado | Tile | Archivo |
|--------|------|---------|
| Restringida | tile116 — borde tierra/pasto | `Resources/Grid/Tile_Restricted` |
| Libre | tile124 — pasto verde sólido | `Resources/Grid/Tile_Libre` |
| Ocupada | tile040 — pasto verde + X | `Resources/Grid/Tile_Ocupada` |
| EnConstrucción | tile085 — gris + llave inglesa | `Resources/Grid/Tile_Building` |
| Flash inválido | tile086 — gris + X | `Resources/Grid/Tile_Invalid` |

Sprites cargados en runtime con `Resources.Load<Sprite>()`. Escala calculada automáticamente para llenar exactamente una celda.

### Pathfinding
- Algoritmo: **A\***
- Recalcula cada vez que una celda cambia de `EnConstrucción` → `Ocupada`, o se vende una torre
- **Regla de validación:** antes de iniciar construcción, verificar que exista al menos un camino spawn→meta excluyendo la celda objetivo. Si no existe, rechazar con feedback visual (celda parpadea en rojo)
- Cada monstruo recalcula su ruta individualmente cuando el pathfinding cambia

### Estrategias válidas (no forzar ninguna)
- **Serpenteo** — laberinto largo
- **Embudo** — tráfico concentrado
- **Chokepoint** — estrechez para torres AoE/CC

## Implementación técnica

### GridManager.cs
```csharp
bool CanPlaceTower(Vector2Int cell)              // bounds + estado + IsRestricted + pathfinding
void SetCellState(Vector2Int cell, CellState state)
void NotifyPathfindingSystem(Vector2Int cell)    // AstarPath.active.UpdateGraphs(bounds)
bool IsRestricted(Vector2Int cell)               // true para celdas en RestrictedCells[]
Vector3 CellToWorld(Vector2Int cell)
Vector2Int WorldToCell(Vector3 worldPos)

// Configura GridGraph de A* en Awake() — width=5, depth=9, nodeSize=0.96, is2D=true
private void ConfigureAstarGraph()
```

- Usa **A\* Pathfinding Project** (instalado en `Assets/AstarPathfindingProject/`)
- Validación de camino: bloquea el nodo temporalmente → `ABPath` síncrono → restaura
- El héroe ignora la grilla completamente (vuela en línea recta)

### GridVisualizer.cs
- Crea 45 `SpriteRenderer` hijos en `Start()` (runtime, no se guardan en escena)
- `RefreshTile(cell)` — actualiza sprite según estado actual
- `FlashTile(cell, bool valid)` — 3 parpadeos, verde (válido) o rojo + X (inválido)
- `RefreshAll()` — refresca todos los tiles

## Archivos involucrados
- `Assets/_Project/Scripts/Managers/GridManager.cs`
- `Assets/_Project/Scripts/Managers/GridVisualizer.cs`
- `Assets/_Project/Scripts/Managers/GameManager.cs`
- `Assets/Resources/Grid/` — 5 sprites PNG (Tile_Libre, Tile_Ocupada, Tile_Building, Tile_Restricted, Tile_Invalid)

## Criterio de aceptación
- [x] La grilla 5×9 se visualiza correctamente en escena (tiles Kenney)
- [x] Fila superior (GoalRow) usa tile116 y no es buildable
- [x] Las celdas libres usan tile124 (verde) y las ocupadas tile040 (verde + X)
- [x] Las celdas cambian de estado correctamente
- [x] El pathfinding recalcula en tiempo real sin stutters (A* scan ~20ms)
- [x] Construcción se rechaza (flash rojo) cuando bloquearía todos los caminos
- [ ] Los monstruos recalculan ruta individualmente al cambiar el pathfinding *(TASK-03)*

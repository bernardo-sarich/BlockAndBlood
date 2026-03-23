# TASK-04 — Héroe

**Sección GDD:** 6. Héroe
**Estado:** ✅ Completada

---

## Descripción
Implementar al héroe jugable: movimiento WASD con vuelo libre sobre la grilla, ataque automático y mecánica de construcción con desplazamiento al tile adyacente.

## Control y movimiento
- **WASD + flechas** — 8 direcciones, velocidad constante (`MoveSpeed = 4 u/s`)
- **Vuelo:** ignora completamente el pathfinding y la grilla. Se mueve en línea recta
- Sin colisión con torres ni monstruos
- **Confinado a los límites de pantalla** — clampea contra los bordes ortográficos de la cámara

## Stats base
| Stat | Valor |
|------|-------|
| Velocidad de movimiento | 4 u/s |
| Daño por ataque | 25 (físico) |
| Rango de ataque | 1.5 celdas de radio (1.44 u) |
| Velocidad de ataque | 1.5 ataques/segundo |
| HP | **No tiene** — el héroe no puede ser dañado en el MVP |

## Combate
- Ataque **completamente automático** — ataca al enemigo **más cercano** dentro de su rango
- Con carta *Instinto cazador* (`PrioritizeHighestHp = true`): ataca al de **mayor HP**
- Sin habilidades activas
- El jugador solo controla el posicionamiento del héroe

## Construcción (mecánica clave)
- El héroe es el **único** que puede construir torres
- Seleccionar torre en HUD + clic en celda válida → el héroe **se desplaza automáticamente** al tile adyacente más cercano a la celda objetivo
- Al llegar: inicia el timer de **5 segundos** (manejado por `TowerBehaviour.Initialize`)
- El héroe puede moverse y atacar libremente durante la construcción
- **Cola de construcciones** — múltiples builds se encolan y procesan en orden; los timers de 5s corren concurrentemente
- WASD durante el auto-movimiento cancela el desplazamiento automático (el jugador retoma control)
- Celda inválida: flash rojo, no se encola la orden

## Input system
El proyecto usa el **New Input System** (`UnityEngine.InputSystem`). No usar `UnityEngine.Input` (legacy).
- Movimiento: `Keyboard.current.[key].isPressed`
- Click izquierdo: `Mouse.current.leftButton.wasPressedThisFrame`
- Click derecho / Escape: `Mouse.current.rightButton.wasPressedThisFrame` / `Keyboard.current.escapeKey.wasPressedThisFrame`

## Modificación por cartas
El héroe no tiene stats fijos más allá de los base. Las cartas de tipo Héroe pueden modificar:
- Daño, rango, velocidad de ataque, velocidad de movimiento
- Comportamientos nuevos: prioridad de targeting, efectos al matar

### Cartas que afectan al héroe
| Carta | Rareza | Efecto | Propiedad en HeroBehaviour |
|-------|--------|--------|---------------------------|
| Buen ojo | Común | +50% rango de ataque | `AttackRange` |
| Golpe certero | Común | +30% daño | `Damage` |
| Instinto cazador | Rara | Prioriza enemigo con mayor HP | `PrioritizeHighestHp = true` |

## Archivos involucrados
- `Assets/_Project/Scripts/Hero/HeroBehaviour.cs` — lógica principal del héroe
- `Assets/_Project/Scripts/Enemies/IHasHp.cs` — interfaz para targeting por HP (usada por *Instinto cazador*)
- `Assets/_Project/Art/Sprites/Hero/towerDefense_tile250.png` — sprite del héroe
- `Assets/_Project/Scripts/Managers/TowerPlacementManager.cs` — eliminado bloque Update temporal de TASK-02

## Posición inicial en escena
- GameObject `Hero` en `(2.4, 0.48, 0)` — centro de fila 0 (borde inferior de la grilla)
- Sorting Order: 10 (sobre tiles de grilla)

## Criterio de aceptación
- [x] Movimiento WASD + flechas en 8 direcciones sobre las torres sin colisión
- [x] El héroe no puede salir de los límites de pantalla
- [x] Ataque automático al enemigo más cercano en rango
- [x] Cola de construcción: héroe se mueve al tile adyacente más cercano e inicia build
- [x] Múltiples construcciones simultáneas con timers independientes
- [x] Los stats se pueden modificar por efectos de cartas en tiempo real
- [x] Sprite towerDefense_tile250 asignado

# TASK-03 — Monstruos

**Sección GDD:** 5. Monstruos · 12.4 Object Pooling

---

## Descripción
Implementar los 3 tipos de monstruos del MVP con su comportamiento de movimiento por pathfinding, sistema de efectos (Burn/Slow), recompensas y object pooling.

## Principios generales
- Spawnean desde fila 0 (borde superior)
- Calculan ruta con A\* al spawnear, recalculan cuando el pathfinding cambia
- Al llegar a meta (fila 8): restan **1 vida** al jugador y desaparecen
- Al morir: dan **Oro + XP** al jugador

## Los 3 monstruos

### Caminante
| Stat | Valor |
|------|-------|
| HP | 60 |
| Velocidad | 2 celdas/segundo |
| Armadura | 0% |
| Recompensa | 2 oro / 5 XP |

- Sin mecánica especial
- Aparece en **grupos**
- Enemigo tutorial — enseña que el laberinto largo es la estrategia básica

---

### Rápido
| Stat | Valor |
|------|-------|
| HP | 40 |
| Velocidad | 4 celdas/segundo (doble del Caminante) |
| Armadura | 0% |
| Recompensa | 3 oro / 8 XP |

- Sin mecánica especial más allá de la velocidad
- Spawna **siempre solo** — nunca en grupo
- Se intercala con Caminantes de forma impredecible
- Counter natural: Torre de Agua. Segunda línea: el héroe

---

### Blindado
| Stat | Valor |
|------|-------|
| HP | 200 |
| Velocidad | 1.5 celdas/segundo |
| Armadura | **50% reducción de daño físico** |
| Recompensa | 5 oro / 15 XP |

- La armadura reduce daño de Torre Melee y Torre de Rango en 50%
- El DoT de Torre de Fuego **ignora** esta reducción completamente
- Siempre precedido por 3 Caminantes (señal de aviso)
- Introduce la necesidad de la Torre de Fuego

---

## Tabla comparativa
| Monstruo | HP | Velocidad | Armadura | Oro | XP |
|----------|----|-----------|----------|-----|-----|
| Caminante | 60 | 2 c/s | 0% | 2 | 5 |
| Rápido | 40 | 4 c/s | 0% | 3 | 8 |
| Blindado | 200 | 1.5 c/s | 50% físico | 5 | 15 |

## EffectSystem (componente en EnemyBehaviour)
- Gestiona efectos activos: **Burn** (DoT), **Slow** (reducción de velocidad)
- Cada efecto tiene: duración, valor, fuente
- Múltiples fuentes de Slow se acumulan hasta cap **−70%**
- Burn: 4 dmg/s, ignora armadura, nuevo impacto refresca duración

## Object Pooling — OBLIGATORIO desde día 1
Usar `UnityEngine.Pool.ObjectPool<T>` (nativo Unity 2021+):
- **Monstruos:** máximo ~30 activos simultáneamente
- **Proyectiles:** máximo ~50 activos simultáneamente
- **Efectos visuales** de impacto y muerte

## ScriptableObject — EnemyData
```csharp
string enemyName
float hp
float speed
float armor        // 0.0 a 1.0 (porcentaje de reducción)
int goldReward
int xpReward
GameObject prefab
```

## Archivos involucrados
- `Assets/_Project/Scripts/Enemies/EnemyBehaviour.cs`
- `Assets/_Project/Scripts/Towers/EffectSystem.cs`
- `Assets/_Project/ScriptableObjects/Enemies/` (1 SO por monstruo)

## Criterio de aceptación
- [ ] Los 3 tipos de monstruo se mueven correctamente por el laberinto
- [ ] Recalculan ruta individualmente al cambiar pathfinding
- [ ] Al llegar a meta restan 1 vida
- [ ] Al morir dan oro y XP correctos
- [ ] Armadura del Blindado reduce 50% daño físico pero no el DoT de Fuego
- [ ] EffectSystem acumula Slow hasta −70%
- [ ] Object Pool activo para monstruos, proyectiles y efectos

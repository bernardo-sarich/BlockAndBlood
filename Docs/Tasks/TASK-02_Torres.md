# TASK-02 — Torres

**Sección GDD:** 4. Torres · 12.2 TowerData SO

---

## Descripción
Implementar las 4 torres del MVP con sus stats, efectos, mecánica de construcción (5s), mejoras y venta.

## Principios generales
- Clic en celda libre con torre seleccionada en HUD → inicia construcción (5 segundos)
- Sin límite de torres — limitado solo por oro
- Vender devuelve **60% del costo total** (construcción + mejoras). Pathfinding recalcula inmediatamente
- Mejora con oro en cualquier momento: clic en torre → botón Mejorar

## Torres del MVP

### Torre Melee (Nivel 1) — 12 oro
| Stat | Valor |
|------|-------|
| Daño/segundo | 15 (físico) |
| Rango | Celda propia + 8 adyacentes |
| Efecto | Ralentiza −15% velocidad |
| Tipo | Daño en área, sin proyectil — no puede fallar |

**Mejora → Torre Sierra (Nivel 2) — +18 oro**
- Daño/segundo: 28 (+87%)
- Velocidad de giro visual aumentada
- Sin cambio en rango ni efecto

**Rol:** bloquear camino + ralentizar pasivamente. Ideal en chokepoints.

---

### Torre de Rango (Nivel 1) — 10 oro
| Stat | Valor |
|------|-------|
| Daño/proyectil | 20 (físico) |
| Rango | 3 celdas de radio |
| Velocidad | 1 disparo/segundo |
| Target | Enemigo con mayor progreso hacia la meta |

**Rol:** DPS a distancia. Cubre más área que Melee.

---

### Torre de Fuego (Nivel 2 de Rango) — +15 oro · Total: 25 oro
| Stat | Valor |
|------|-------|
| Daño/proyectil | 20 (igual Rango Lv1) |
| Efecto al impacto | Quemadura |
| Duración quemadura | 3 segundos |
| DoT quemadura | 4 daño/segundo (ignora armadura física) |
| ¿Acumula? | No — nuevo impacto refresca duración |

**Rol:** DPS sostenido vs alto HP. Counter natural de Blindados. Sinergia con Torre de Agua.

---

### Torre de Agua (Nivel 2 de Rango) — +15 oro · Total: 25 oro
| Stat | Valor |
|------|-------|
| Daño/proyectil | 16 (−20% vs Rango Lv1) |
| Efecto al impacto | Ralentización |
| Slow | −40% velocidad durante 2s |
| ¿Acumula? | Sí — hasta máximo −70% |
| Bonus | −15% armadura durante 2s |

**Rol:** control sistémico. Una torre al inicio del laberinto amplifica todas las demás. Esencial vs Rápidos.

---

## Efectividad por monstruo
| Torre | Caminante | Rápido | Blindado |
|-------|-----------|--------|----------|
| Melee | ★★★ | ★★ | ★★ |
| Rango | ★★★ | ★★ | ★ |
| Fuego | ★★★ | ★★ | ★★★ |
| Agua | ★★ | ★★★ | ★★★ |

## ScriptableObject — TowerData
```csharp
string towerName
int cost
int upgradeCount
float damageBase
float attackSpeed
float range
DamageType damageType   // Physical / Fire / Water
GameObject prefab
TowerData[] upgradePaths   // referencias a SOs de mejora
```

## Archivos involucrados
- `Assets/_Project/Scripts/Towers/TowerBehaviour.cs`
- `Assets/_Project/Scripts/Towers/ProjectileBehaviour.cs`
- `Assets/_Project/Scripts/Towers/EffectSystem.cs`
- `Assets/_Project/ScriptableObjects/Towers/` (1 SO por torre y nivel)

## Criterio de aceptación
- [ ] Las 4 torres se pueden construir y colocan en 5 segundos
- [ ] Melee daña en área sin proyectil
- [ ] Fuego aplica DoT que ignora armadura
- [ ] Agua acumula slow hasta −70%
- [ ] Mejoras funcionan correctamente
- [ ] Venta devuelve 60% del costo total

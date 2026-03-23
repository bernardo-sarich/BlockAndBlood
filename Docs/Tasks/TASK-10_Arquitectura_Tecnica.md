# TASK-10 — Arquitectura Técnica

**Sección GDD:** 12. Arquitectura técnica

---

## Descripción
Establecer la estructura de carpetas del proyecto, los ScriptableObjects base y las convenciones técnicas antes de comenzar el desarrollo de sistemas.

## Estructura de carpetas Unity
```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Managers/
│   │   │   ├── GameManager.cs
│   │   │   ├── GridManager.cs
│   │   │   ├── WaveManager.cs
│   │   │   ├── EconomyManager.cs
│   │   │   └── XPManager.cs
│   │   ├── Towers/
│   │   │   ├── TowerBehaviour.cs
│   │   │   ├── ProjectileBehaviour.cs
│   │   │   └── EffectSystem.cs
│   │   ├── Enemies/
│   │   │   └── EnemyBehaviour.cs
│   │   ├── Hero/
│   │   │   └── HeroBehaviour.cs
│   │   ├── Cards/
│   │   │   ├── CardSystem.cs
│   │   │   └── CardEffect.cs
│   │   └── UI/
│   │       ├── HUDController.cs
│   │       └── CardPopupController.cs
│   ├── ScriptableObjects/
│   │   ├── Towers/     ← TowerData SO por cada torre y nivel
│   │   ├── Enemies/    ← EnemyData SO por cada monstruo
│   │   └── Cards/      ← CardData SO por cada carta (15 total)
│   ├── Prefabs/
│   ├── Scenes/
│   └── Art/
│       ├── Sprites/
│       └── UI/
└── Kenney/             ← Assets externos sin modificar
```

---

## ScriptableObjects a crear

### TowerData
```csharp
string towerName
int cost
int upgradeCount
float damageBase
float attackSpeed
float range
DamageType damageType   // Physical / Fire / Water
GameObject prefab
TowerData[] upgradePaths
```
**Instancias:** TowerMelee_Lv1, TowerMelee_Lv2, TowerRange_Lv1, TowerFire_Lv2, TowerWater_Lv2

### EnemyData
```csharp
string enemyName
float hp
float speed
float armor             // 0.0 a 1.0 (porcentaje de reducción)
int goldReward
int xpReward
GameObject prefab
```
**Instancias:** Caminante, Rápido, Blindado

### CardData
```csharp
string cardName
CardRarity rarity       // Common / Rare / Epic
string description
Sprite cardArt
CardEffectType effectType
float effectValue
```
**Instancias:** 15 cartas (8 Comunes, 5 Raras, 2 Épicas)

---

## Sistemas críticos — resumen de responsabilidades

| Sistema | Responsabilidad principal |
|---------|--------------------------|
| GameManager | Estado global de la run (vidas, oro, fase) |
| GridManager | Estado de celdas + validación de pathfinding |
| WaveManager | Stream de monstruos + pausas por XP |
| XPManager | Acumula XP, emite `OnLevelUp` |
| EconomyManager | Oro: ingresos (monstruos) y gastos (torres) |
| CardSystem | Pool de cartas + distribución por rareza + aplicación de efectos |
| EffectSystem | Burn y Slow por enemigo + acumulación hasta caps |

---

## Object Pooling — obligatorio desde día 1
```csharp
// Usar UnityEngine.Pool.ObjectPool<T> (nativo Unity 2021+)
// Pools necesarios:
//   - Monstruos (~30 activos simultáneamente)
//   - Proyectiles (~50 activos simultáneamente)
//   - Efectos visuales de impacto y muerte
```

## Lo que NO se delega sin revisión cuidadosa
- Lógica de validación del pathfinding **(bug aquí = juego roto)**
- Números de balance (daño, HP, XP, oro)
- Lógica de aplicación de efectos de cartas épicas

## Uso de A* Pathfinding Project
- No implementar A\* desde cero
- Instalar A\* Pathfinding Project (versión free es suficiente para el MVP)
- GridManager wrappea las llamadas al plugin

## Criterio de aceptación
- [ ] Estructura de carpetas creada según el esquema
- [ ] Todos los ScriptableObjects base definidos con sus campos
- [ ] Object pools activos para monstruos, proyectiles y efectos
- [ ] A\* Pathfinding Project instalado y configurado
- [ ] GameManager gestiona el estado de la run correctamente
- [ ] EconomyManager controla ingresos y gastos de oro

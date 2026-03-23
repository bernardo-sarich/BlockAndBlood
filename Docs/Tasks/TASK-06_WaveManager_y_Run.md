# TASK-06 — WaveManager y Estructura de la Run

**Sección GDD:** 8. Estructura de la run · 12.3 WaveManager

---

## Descripción
Implementar el ciclo completo de una run: fase de preparación, stream continuo de monstruos con composición variable, pausas de XP y condiciones de victoria/derrota.

## Duración objetivo de una run: ~15 minutos

---

## Fase de preparación (0:00 — 0:30)
- Jugador tiene **30 segundos** y **50 oro inicial** para construir torres
- El stream NO ha comenzado — sin monstruos
- Contador visible centrado en pantalla con countdown
- Al llegar a 0, el stream comienza automáticamente
- El jugador **no puede extender** este tiempo

### Opciones de construcción con 50 oro
- 4 Torres de Rango (40 oro) + sobran 10
- 3 Rango + 1 Melee (42 oro) + sobran 8
- 2 Rango + 2 Melee (44 oro) + sobran 6
- 1 Torre de Fuego completa (25) + 2 Rango (20) + sobran 5

---

## Stream de monstruos (0:30 → fin)
El stream es **continuo**. Composición cambia con el tiempo:

| Período | Composición | Spawn rate | Notas |
|---------|-------------|------------|-------|
| 0:30 — 3:00 | 100% Caminantes | 1 cada 3s | Tutorial implícito del laberinto |
| 3:00 — 6:00 | 75% Caminantes · 25% Rápidos | 1 cada 2.5s | Rápidos siempre solos |
| 6:00 — 10:00 | 60% Caminantes · 20% Rápidos · 20% Blindados | 1 cada 2s | Blindados precedidos por 3 Caminantes |
| 10:00 — 14:00 | 50% Caminantes · 25% Rápidos · 25% Blindados | 1 cada 1.5s | Presión máxima antes del boss |
| 14:00+ | Stream reducido (1 cada 4s, mixto) | — | Boss activo, stream de soporte |

## Pausas de XP
- 3 pausas interrumpen el stream en ~4, ~9 y ~13 minutos
- El momento exacto depende de cuántos monstruos mató el jugador
- Si el jugador mató más, las pausas llegan antes

## Victoria y derrota
- **Victoria:** el boss muere → pantalla de resultados (monstruos matados, torres construidas, cartas elegidas, oro gastado)
- **Derrota:** jugador pierde las **20 vidas** → pantalla de game over con opción de reintentar inmediatamente
- Sin penalización por derrota — cada run empieza igual (mismo oro, mismo mapa, sin stats permanentes)

---

## Implementación técnica — WaveManager

```csharp
// Lee lista de WavePhase ScriptableObjects:
// - composición (% por tipo de monstruo)
// - spawn rate variable por tramo de tiempo

// Spawna enemigos desde Object Pool

// Escucha evento OnLevelUp de XPManager:
//   → pausa el stream
//   → reanuda cuando CardSystem notifica que el jugador eligió carta
```

### WavePhase ScriptableObject
```csharp
float startTime
float endTime
float spawnInterval
EnemySpawnWeight[] composition   // tipo + peso probabilístico
```

## Archivos involucrados
- `Assets/_Project/Scripts/Managers/WaveManager.cs`
- `Assets/_Project/Scripts/Managers/GameManager.cs`
- `Assets/_Project/Scripts/Managers/EconomyManager.cs`
- `Assets/_Project/ScriptableObjects/` (WavePhase SOs)

## Criterio de aceptación
- [ ] Fase de preparación de 30s con countdown visible y 50 oro inicial
- [ ] Stream comienza automáticamente al terminar la preparación
- [ ] Composición del stream cambia correctamente según el tiempo de juego
- [ ] Rápidos siempre spawnean solos
- [ ] Blindados siempre precedidos por 3 Caminantes
- [ ] Stream se pausa/reanuda correctamente en pausas de XP
- [ ] Victoria e derrota muestran sus pantallas correspondientes
- [ ] Las 20 vidas se descuentan correctamente cuando un monstruo llega a la meta

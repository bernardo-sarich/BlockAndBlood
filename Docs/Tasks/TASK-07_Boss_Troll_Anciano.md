# TASK-07 — Boss Final: Troll Anciano

**Sección GDD:** 9. Boss final

---

## Descripción
Implementar el boss final del MVP con sus 2 fases, mecánicas de invocación y regeneración.

## Aparición
- El Troll Anciano entra desde el spawn al minuto **~14:00**
- El stream de monstruos normales **continúa a baja intensidad** mientras el boss está vivo (1 cada 4s, mixto)

## Stats base
| Stat | Valor |
|------|-------|
| HP | 1500 |
| Velocidad | 0.8 celdas/segundo |
| Armadura | 25% reducción física |
| Recompensa | 30 oro / 50 XP |

---

## Fase 1 (100% → 50% HP)
- Movimiento lento y predecible
- A los **30 segundos** de entrar al mapa, invoca una horda de **8 Caminantes** desde el spawn

## Fase 2 (50% → 0%)
Se activa al llegar al 50% de HP:
- Velocidad aumenta **+60%** (0.8 → 1.3 celdas/segundo)
- Emite un **rugido** que aumenta la velocidad de todos los monstruos en pantalla **+20%** durante 6 segundos
- Invoca horda mixta **(5 Caminantes + 3 Rápidos)** cada 40 segundos
- **Regenera 2 HP/segundo** — requiere DPS sostenido para no estancarse

---

## Mecánica de diseño
El boss valida el build del jugador:
- **Torres de Fuego** → pueden manejar el HP alto (DoT ignora armadura)
- **Torres de Agua** → pueden contener la velocidad de fase 2
- **Sin ninguno** → el jugador perderá vidas en el intento

La **regeneración en fase 2** castiga al jugador que gastó todo el oro en torres bloqueadoras sin suficiente DPS — el laberinto largo no alcanza si el DPS es menor que 2 HP/s.

---

## Implementación técnica
El boss es un `EnemyBehaviour` especializado con:

```csharp
// Estado de fase (Phase1 / Phase2)
// Trigger de fase 2 al 50% HP:
//   - modificar velocidad
//   - iniciar corrutina de rugido (buff global a monstruos en pantalla)
//   - iniciar corrutina de invocación cada 40s
//   - iniciar corrutina de regeneración (2 HP/s)
// Invocación de horda a los 30s en fase 1
// Invocación de hordas en fase 2
```

## Archivos involucrados
- `Assets/_Project/Scripts/Enemies/EnemyBehaviour.cs` (clase base + override para boss)
- `Assets/_Project/Scripts/Managers/WaveManager.cs` (recibe notificación de boss activo para reducir stream)

## Criterio de aceptación
- [ ] El boss aparece al minuto ~14:00 desde el spawn
- [ ] El stream de soporte continúa a baja intensidad durante el boss
- [ ] Fase 1: invoca 8 Caminantes a los 30s de entrar
- [ ] Transición a fase 2 al 50% HP (efecto visual + rugido)
- [ ] Fase 2: velocidad +60%, buff global +20% por 6s al rugir
- [ ] Fase 2: horda mixta cada 40s
- [ ] Fase 2: regeneración de 2 HP/s activa
- [ ] Al morir: victoria, 30 oro + 50 XP al jugador

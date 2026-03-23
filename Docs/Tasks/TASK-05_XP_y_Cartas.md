# TASK-05 — Sistema de XP y Cartas

**Sección GDD:** 7. Sistema de XP y cartas · 12.2 CardData SO · 12.3 CardSystem / XPManager

---

## Descripción
Implementar la barra de XP, las 3 pausas de nivel con popup de selección de carta y los efectos de las 15 cartas del MVP.

## Barra de XP

- Visible en HUD en todo momento
- Se llena matando monstruos (XP por tipo: Caminante 5, Rápido 8, Blindado 15, Boss 50)
- **3 niveles de XP por run**

| Nivel | XP requerida | Momento estimado |
|-------|-------------|-----------------|
| 1 | 150 XP | ~4 min |
| 2 | 300 XP | ~9 min |
| 3 | 500 XP | ~13 min |

## Pausa de carta (flujo)
1. Barra de XP llena → stream de monstruos **se pausa instantáneamente**
2. Popup centrado con **3 cartas aleatorias** según rareza del nivel
3. Jugador hace clic en una carta → efecto se aplica inmediata y permanentemente
4. Stream **se reanuda**
5. Sin tiempo límite — la pausa dura hasta que el jugador elige

## Rareza por nivel
| Nivel de XP | Distribución |
|-------------|-------------|
| 1 | 3 Comunes |
| 2 | 2 Comunes + 1 Rara |
| 3 | 1 Rara + 1 Épica + 1 aleatoria |

---

## Catálogo — 15 cartas

### Comunes (8)
| Carta | Efecto |
|-------|--------|
| Filo afilado | Torres Melee +20% daño |
| Pólvora | Torres de Rango +20% daño |
| Rescoldo | Torres de Fuego: quemadura 5s (antes 3s) |
| Corriente fría | Torres de Agua: slow −55% (antes −40%) |
| Construcción rápida | Tiempo de construcción 3s (antes 5s) |
| Buen ojo | Héroe +50% rango de ataque |
| Golpe certero | Héroe +30% daño |
| Avaricia | Cada monstruo da +1 oro al morir |

### Raras (5)
| Carta | Efecto |
|-------|--------|
| Corriente amplificada | Enemigo ralentizado por Agua recibe +25% daño de todas las fuentes |
| Brasas | Al expirar quemadura, AoE de 10 dmg en 1 celda alrededor |
| Economía de guerra | Vender torre devuelve 80% (antes 60%) — una vez por pausa de XP |
| Sierra en cadena | Torre Melee mata enemigo → siguiente en misma celda recibe 50% del daño del kill |
| Instinto cazador | Héroe prioriza enemigo con mayor HP en lugar del más cercano |

### Épicas (2)
| Carta | Requisito | Efecto |
|-------|-----------|--------|
| Tormenta de fuego | ≥1 Torre de Fuego construida | Torres de Fuego también aplican slow de Agua (−25% vel, 1.5s) |
| El laberinto vivo | — | Monstruo que dobla en esquina recibe 15 de daño |

## Interacciones clave entre cartas
| Combinación | Efecto |
|-------------|--------|
| Corriente fría + Corriente amplificada | Slow máximo + daño amplificado |
| Rescoldo + Corriente amplificada | Quemadura más larga + amplificación = máximo DoT |
| Brasas + Sierra en cadena | AoE de Brasas puede activar daño en cadena de Sierra |
| El laberinto vivo + construcción activa | Incentiva rediseñar el laberinto para crear más esquinas |

## ScriptableObject — CardData
```csharp
string cardName
CardRarity rarity     // Common / Rare / Epic
string description
Sprite cardArt
CardEffectType effectType
float effectValue
```

## XPManager
- Recibe XP de `EnemyBehaviour` al morir cada enemigo
- Al llenar la barra emite evento `OnLevelUp`
- `WaveManager` escucha `OnLevelUp` para pausar el stream
- `CardSystem` escucha `OnLevelUp` para mostrar el popup

## CardSystem
- Mantiene el pool de 15 cartas
- Al nivel de XP, selecciona 3 cartas respetando la distribución de rareza
- Al elegir, llama `CardEffect.Apply(GameState state)` que modifica stats correspondientes

## Archivos involucrados
- `Assets/_Project/Scripts/Managers/XPManager.cs`
- `Assets/_Project/Scripts/Cards/CardSystem.cs`
- `Assets/_Project/Scripts/Cards/CardEffect.cs`
- `Assets/_Project/Scripts/UI/CardPopupController.cs`
- `Assets/_Project/ScriptableObjects/Cards/` (1 SO por carta × 15)

## Criterio de aceptación
- [ ] Barra de XP se llena correctamente según XP por monstruo
- [ ] Al completar nivel, stream se pausa instantáneamente
- [ ] Popup muestra 3 cartas con distribución de rareza correcta por nivel
- [ ] Carta elegida aplica su efecto permanentemente en la run
- [ ] Las 15 cartas tienen efectos que funcionan correctamente
- [ ] Cartas épicas verifican sus requisitos antes de aparecer

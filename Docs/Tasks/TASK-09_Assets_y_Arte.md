# TASK-09 — Assets y Arte

**Sección GDD:** 11. Assets y arte

---

## Descripción
Integrar los assets visuales y de audio del MVP. La filosofía es **legibilidad sobre calidad visual** — el arte final llega después de validar que el juego es divertido.

## Filosofía MVP
- Assets de **Kenney.nl** — gratuitos, dominio público, estilo visual consistente
- Objetivo: el jugador entiende de un vistazo qué es cada torre, qué es cada monstruo y cuánto HP le queda
- El arte final se implementa post-validación del MVP

---

## Assets — Packs de Kenney

### Torres y tiles — Tower Defense Top-Down + Tower Defense (original)
- Sprites de torres (top-down, compatibles con 3/4 view)
  - Base + cañón rotatorio → Torre de Rango
  - Base + sierra → Torre Melee
  - Variantes elementales → Fuego / Agua (recolorear o sustituir)
- Tiles de terreno para la grilla (ya implementados)
- Proyectiles básicos
- El pack Tower Defense (original) añade más variantes de torres y monstruos

### Personajes — Roguelike Characters
- Sprites con **4 direcciones** (frente, espalda, izquierda, derecha) — clave para el estilo 3/4 view
- **Héroe:** sprite direccional (reemplaza placeholder towerDefense_tile250)
- **Monstruos:** 3 variantes distinguibles por **color o forma**, con sprites direccionales
  - Caminante: sprite base
  - Rápido: más pequeño / forma ágil
  - Blindado: más grande / color metálico
- **Boss:** sprite más grande que los monstruos normales, visualmente imponente

### Cartas — Roguelike/RPG Pack
- 1700 iconos para cartas roguelike

### UI — UI Pack
- Botones, barras, paneles para HUD y popup de cartas

## Assets adicionales mínimos (generables con AI o formas simples)
- **Indicador de torre en construcción** — outline parpadeante o barra de progreso sobre la celda
- **Efectos de estado:**
  - Partícula de fuego sobre enemigo quemado (Burn)
  - Partícula de agua/hielo sobre enemigo ralentizado (Slow)
- **Barra de HP** sobre cada monstruo
- **Sombras** — sprite elipse semitransparente bajo cada entidad (ver TASK-12)

## Feedback visual requerido por el gameplay
| Situación | Feedback visual |
|-----------|----------------|
| Celda inválida para construir | Celda parpadea en rojo |
| Torre en construcción | Outline parpadeante o barra de progreso |
| Enemigo quemado | Partícula de fuego |
| Enemigo ralentizado | Partícula de agua/hielo |
| HP del monstruo | Barra sobre el sprite |
| Boss fase 2 | Cambio visual (color, escala o efecto) al activarse |

---

## Audio mínimo (no bloqueante — el juego es jugable en silencio)
Implementar **si hay tiempo en el mes 4**:
- Sonido de disparo de torre (1 variante por tipo de torre)
- Sonido de muerte de monstruo
- Sonido de colocación de torre
- Música de fondo simple en loop (Kenney o freesound.org con licencia CC0)

## Estructura de carpetas de arte
```
Assets/
├── _Project/
│   └── Art/
│       ├── Sprites/
│       └── UI/
└── Kenney/       ← Assets externos sin modificar
```

## Criterio de aceptación
- [ ] Las 4 torres son visualmente distinguibles entre sí
- [ ] Los 3 tipos de monstruo son distinguibles de un vistazo
- [ ] Las barras de HP son visibles sobre cada monstruo
- [ ] El héroe es visualmente distinguible de todos los demás elementos
- [ ] Las celdas inválidas parpadean en rojo al intentar construir
- [ ] Los efectos de Burn y Slow son visibles sobre los monstruos
- [ ] La torre en construcción tiene indicador visual de progreso

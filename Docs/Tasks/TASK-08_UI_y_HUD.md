# TASK-08 — UI y HUD

**Sección GDD:** 10. UI y controles · 12.3 HUDController / CardPopupController

---

## Descripción
Implementar todos los elementos de UI del juego: HUD principal, panel de torre seleccionada, popup de carta de XP, contador de preparación y controles del jugador.

## Controles
| Acción | Control |
|--------|---------|
| Mover héroe | WASD |
| Seleccionar tipo de torre | Clic en botón del HUD inferior |
| Colocar torre | Clic izquierdo en celda del mapa |
| Cancelar selección de torre | Clic derecho / Escape |
| Vender torre | Clic en torre → botón Vender |
| Mejorar torre | Clic en torre → botón Mejorar |
| Elegir carta de XP | Clic en la carta deseada |
| Pausa | Escape / P |

---

## HUD — Barra superior
- **Vidas restantes** — número + icono de corazón (`Heart.png`), máximo **5** ← IMPLEMENTADO
- **Oro actual** — número + icono de moneda (`GoldCoin.png`) ← IMPLEMENTADO
- **Barra de XP** con número de nivel actual

## HUD — Panel inferior
- Botones de torre: **Melee (12 oro) · Rango (10 oro) · Fuego (25 oro) · Agua (25 oro)**
- Cada botón muestra el costo
- El botón se **desactiva visualmente** si el jugador no tiene suficiente oro

## Panel flotante (al hacer clic en una torre construida)
- Stats actuales de la torre
- **Botón Mejorar** (con costo) — solo disponible para Melee y Rango (no para Fuego/Agua que ya son Lv2)
- **Botón Vender** (muestra el oro que devuelve: 60% del total)

## Contador de preparación
- Solo visible durante los 30 segundos iniciales
- Número grande centrado en pantalla con countdown
- Desaparece al comenzar el stream

## Popup de carta de XP
- Aparece centrado en pantalla
- Fondo semitransparente que oscurece el mapa
- **3 cartas en fila horizontal**
- Cada carta muestra: nombre, rareza (color de borde), descripción del efecto
- Hover muestra descripción extendida
- Clic en carta → se selecciona y cierra el popup

### Colores de rareza
| Rareza | Color de borde |
|--------|---------------|
| Común | Gris / Blanco |
| Rara | Azul |
| Épica | Púrpura |

---

## Archivos involucrados
- `Assets/_Project/Scripts/UI/HUDController.cs`
- `Assets/_Project/Scripts/UI/CardPopupController.cs`
- `Assets/_Project/Scripts/Managers/LivesManager.cs` ← NUEVO
- `Assets/_Project/Art/UI/Heart.png` ← NUEVO (icono vidas)
- `Assets/_Project/Art/UI/GoldCoin.png` ← NUEVO (icono oro)

## Criterio de aceptación
- [x] HUD superior muestra vidas (5, icono corazón) y oro (icono moneda) correctamente
- [ ] HUD superior muestra barra de XP correctamente
- [ ] Botones de torre se desactivan cuando no hay oro suficiente
- [ ] Clic en torre muestra panel flotante con stats, Mejorar y Vender
- [ ] Botón Vender muestra la cantidad correcta de oro devuelto
- [ ] Contador de 30s visible solo en fase de preparación
- [ ] Popup de carta aparece centrado con fondo oscuro
- [ ] Las 3 cartas muestran nombre, rareza y descripción
- [ ] Hover en carta muestra descripción extendida
- [ ] Clic en carta cierra el popup y aplica el efecto
- [ ] Pausa con Escape / P funciona correctamente

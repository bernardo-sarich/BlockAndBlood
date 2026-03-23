# TASK-12 — Estilo Visual 3/4 View (2.5D) para PC

**Sección GDD:** 11. Assets y arte — Plataforma PC / Estilo 3/4 view

---

## Contexto

El juego apunta a **PC (Windows/Mac)** con estilo visual **3/4 view (2.5D)**, inspirado en Ball x Pit (que es 3D con cámara 3/4). Block & Blood replica esa sensación en **Unity 2D estándar** — grilla rectangular normal, sin Tilemap isométrico.

**Referencia de 3/4 view:** Zelda: A Link to the Past, Pokémon, Enter the Gungeon — cámara inclinada desde arriba y atrás, personajes vistos "desde atrás" al moverse hacia arriba.

**Regla clave:** ninguno de estos cambios toca la lógica de grilla, pathfinding ni gameplay. Son 100% artísticos/configuración.

---

## Objetivo

Lograr la sensación de profundidad 3/4 view con cuatro técnicas de bajo costo de implementación.

---

## Técnicas a implementar

### 1. Sprites direccionales para personajes (héroe + monstruos)

**Qué hace:** los personajes muestran vista trasera al moverse hacia arriba y vista frontal al moverse hacia abajo, dando la sensación de perspectiva 3/4. Es la técnica que más diferencia el estilo de un top-down plano.

**Pack:** Kenney **Roguelike Characters** — sprites con 4 direcciones (frente, espalda, izquierda, derecha).

**Implementación:**
- Héroe: sprite cambia según dirección de movimiento (WASD). Mínimo 2 sprites (frente/espalda), ideal 4
- Monstruos: sprite cambia según dirección de su ruta A*. Mínimo 2 sprites (frente/espalda)
- La lógica de cambio de sprite va en `HeroBehaviour` y `EnemyBehaviour` respectivamente
- Sprite idle: último sprite direccional usado (no vuelve a "frente" al detenerse)

**Script de ejemplo (componente reutilizable):**
```csharp
// DirectionalSprite.cs — componente genérico
// Recibe la dirección de movimiento y selecciona el sprite correspondiente
// Sprites asignados via Inspector: Front, Back, Left, Right
// Para el MVP: con Front/Back es suficiente
```

### 2. Sprite Sort Point = "Pivot" en todas las entidades dinámicas

**Qué hace:** entidades más abajo en pantalla se dibujan encima de las que están más arriba, creando la ilusión de profundidad correcta.

**Dónde aplicar:** todos los `SpriteRenderer` de monstruos, héroe y torres.

**Cómo:**
- En el Inspector del SpriteRenderer: `Sprite Sort Point` → cambiar de `Center` a `Pivot`
- Asegurarse de que el pivot de cada sprite esté en la base del sprite (no en el centro)
- Configurar en los prefabs para que se aplique automáticamente a todos los instanciados

**Dependencia:** requiere que `Camera` use `Transparency Sort Mode = Custom Axis` con `Y = 1` para que Unity ordene por posición Y.

```
Camera Inspector:
  Transparency Sort Mode: Custom Axis
  Transparency Sort Axis: X=0, Y=1, Z=0
```

### 3. Sorting Layers por componente de torre

**Qué hace:** base y cañón/sierra en layers distintos dan sensación de volumen sin sprites 3D.

**Layers a crear (orden de dibujado, de menor a mayor):**
```
Ground        ← tiles de grilla
Shadows       ← sombras proyectadas
TowerBase     ← base de todas las torres
Characters    ← monstruos y héroe
TowerTop      ← cañón rotatorio / sierra (encima de monstruos que pasan)
Effects       ← partículas de Burn/Slow, barras de HP
UI            ← HUD
```

**Cómo aplicar a prefabs de torres:**
- El GameObject raíz de la torre (base): `Sorting Layer = TowerBase`
- El hijo con el cañón/sierra: `Sorting Layer = TowerTop`

### 4. Sombra proyectada bajo entidades dinámicas

**Qué hace:** un óvalo semitransparente debajo del héroe, monstruos y torres los "despega" visualmente del suelo. Efecto visual grande por costo mínimo.

**Implementación:**
- Crear prefab `Shadow` — `SpriteRenderer` con sprite elipse, color negro, alpha = 0.3
- Agregar como `GameObject` hijo a los prefabs de monstruo, héroe y torres
- Posición local: `(0, -0.2f, 0)` (ligeramente por debajo del centro)
- Escala: `(0.7f, 0.35f, 1f)` — más ancho que alto para parecer proyectado
- Sorting Layer: `Shadows` (debajo de todo lo demás)

---

## Assets de Kenney a usar

### Sprites direccionales — Roguelike Characters
- Pack con sprites de personajes en 4 direcciones (frente, espalda, izquierda, derecha)
- Usar para héroe (reemplaza towerDefense_tile250 placeholder) y los 3 tipos de monstruo
- Los 3 monstruos deben ser visualmente distinguibles: Caminante (base), Rápido (más pequeño/ágil), Blindado (más grande/metálico)

### Torres — Tower Defense Top-Down + Tower Defense original
- Los sprites top-down del Tower Defense Kit funcionan perfectamente en 3/4 view
- El pack Tower Defense (original) añade más variantes de torres
- Los tiles de suelo actuales se mantienen sin cambios

### Tamaño de tiles para PC
- **Actual:** 96×96 px (`CellSize = 0.96f`) — válido para MVP
- **Recomendado para polish (mes 4):** 128×128 px si los sprites de torres lo permiten
- No cambiar `CellSize` hasta que haya sprites de resolución mayor

---

## Criterios de aceptación

- [ ] Héroe tiene sprites direccionales (mínimo frente/espalda) que cambian según movimiento WASD
- [ ] Monstruos tienen sprites direccionales (mínimo frente/espalda) que cambian según dirección de ruta
- [ ] La cámara tiene `Transparency Sort Mode = Custom Axis` con Y=1
- [ ] Todos los `SpriteRenderer` de entidades dinámicas tienen `Sprite Sort Point = Pivot`
- [ ] Los sprites tienen el pivot configurado en la base (no en el centro)
- [ ] Monstruo que está más abajo en pantalla se dibuja encima de uno que está más arriba
- [ ] Las torres tienen base y cañón/sierra en Sorting Layers distintos
- [ ] Cada monstruo, héroe y torre tiene sombra oval semitransparente en layer `Shadows`
- [ ] El juego se ve con profundidad notablemente mayor que una grilla plana

## Lo que NO está en scope de esta tarea

- Cambiar sprites de torres (eso es TASK-09)
- Cambiar el tamaño de `CellSize` (rompería la grilla y el pathfinding)
- Animaciones de caminar (post-MVP — el cambio de sprite entre direcciones es suficiente)
- Sprites custom dibujados (post-validación MVP)

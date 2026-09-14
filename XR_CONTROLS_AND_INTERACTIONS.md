# 🎮 Guía Maestra de Controles e Interacciones VR (PC Simulator & Oculus Quest)

Este documento sirve como **memoria técnica y guía de controles** para el proyecto VR Sports (Tiro Deportivo y Minijuegos), optimizado tanto para desarrollo en PC con el **XR Device Simulator** como para despliegue final en **Meta Quest 2 / 3 / Pro**.

---

## 1. Mapeo de Controles en PC (XR Device Simulator)

| Acción | Tecla / Control PC | Descripción |
| :--- | :--- | :--- |
| **Agarrar / Soltar Rifle (Toggle)** | `G` o `E` | **1 Clic:** Agarra y bloquea el rifle en la mano. <br>**2do Clic:** Lo suelta en la mesa. |
| **Disparar** | `Clic Izquierdo` o `T` | Disparo semiautomático con retroceso procedural, sonido, fogonazo y expulsión de casquillo. |
| **Apuntar con Miras (ADS / Bodycam)** | `F`, `Z`, `Espacio` o `Clic Derecho` | Alinea suavemente las miras de hierro (Front/Rear Sight) al ojo de la cámara. |
| **Recargar Munición** | `R` | Recarga el cargador de 10 balas (.22 LR) con animación y sonido. |
| **Mover Controlador en Espacio 3D** | `W`, `A`, `S`, `D` | Mueve la mano activa adelante / atrás / izquierda / derecha. |
| **Elevar / Bajar Controlador** | `Q` / `E` | Sube o baja la altura de la mano activa. |
| **Rotar Controlador / Cabeza** | `Clic Derecho + Mover Ratón` | Orienta la mano para apuntar a las dianas de 10m, 20m y 35m. |
| **Cambiar Mano Activa** | `Shift` (Mano Izq) / `Tab` | Alterna entre el controlador derecho e izquierdo. |

---

## 2. Mapeo de Controles en Meta Quest (Touch Controllers)

| Acción | Botón en Mando Quest | Comportamiento en VR |
| :--- | :--- | :--- |
| **Agarrar Arma** | Botón **Grip** (Gatillo lateral) | Agarre ergonómico en empuñadura. En modo Toggle no requiere sostenerlo cansando la mano. |
| **Apretar Gatillo / Disparo** | Botón **Trigger** (Gatillo índice) | Disparo con respuesta háptica (vibración HD en el mando) y balística de 250m. |
| **Modo Apuntado Táctico** | Botón **A / X** o acercar al visor | Estabilización de miras de hierro con la línea de visión del jugador. |
| **Soltar Arma** | Botón **Grip** (2do clic) | Suelta el arma con físicas naturales (`isKinematic = false`, gravedad activa). |
| **Giro y Movimiento** | **Thumbstick** (Palanca analógica) | Snap turn y movimiento suave/teletransporte dentro de la cabina de tiro. |

---

## 3. Arquitectura Técnica de Agarre y Miras (XRI 3.5.1)

### Jerarquía de Objetos del Rifle
```text
Rifle_Ruger_1022LR (Root con Rigidbody, BoxCollider, XRGrabInteractable, VRRifle)
└── Model_Root (Transform contenedor con rotación de calibración)
    ├── Rifle_Mesh_Model (Malla 3D del rifle)
    ├── AttachPoint_Grip (Punto de anclaje de la mano, hijo directo de Model_Root)
    ├── FrontSight (Mira de hierro delantera)
    ├── RearSight (Mira de hierro trasera)
    ├── MuzzlePoint (Boca del cañón / origen del trazador y fogonazo)
    └── Shell_Ejection_Point (Ventana expulsora de casquillos dorados)
```

### Regla de Oro de Anclaje (Attach Transform)
- `AttachPoint_Grip` debe ser hijo directo de `Model_Root`.
- Al estar emparentado con la malla, cualquier rotación de calibración (`modelRotationOffset`) rota automáticamente el punto de agarre.
- Cuando el interactor de VR (mando) toma el `AttachPoint_Grip`, el cañón del rifle **siempre apuntará exactamente en la dirección frontal del mando (`controller.forward`)**, eliminando cualquier desfase angular de 90° o 180°.

### Transición Cinemática de Físicas
- **Al reposar sobre la mesa:** `rb.isKinematic = false`, `rb.useGravity = true`. El arma descansa sólida sin atravesar la mesa.
- **Al ser agarrado (Grabbed):** `rb.isKinematic = true`, `rb.useGravity = false`. Evita que el motor PhysX colisione contra la mesa y cause flotación o vibraciones.
- **Al soltarse (Released):** `rb.isKinematic = false`, `rb.useGravity = true`. Vuelve a responder a la gravedad.

---

## 4. Herramientas de Editor Disponibles

- **`VR Sports > Calibración y Orientación de Rifle VR`**: Ventana visual con botones de 1 clic para rotar el rifle a 0°, 90°, 180°, -90° y alternar el modo Toggle Grab.
- **`VR Sports > Construir Campo de Tiro Deportivo (3 Carriles + 9 Dianas)`**: Generador completo de la escena de tiro con distancias oficiales (10m, 20m, 35m).

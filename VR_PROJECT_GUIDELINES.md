# GUÍA Y REGLAS DEL PROYECTO VR (OCULUS QUEST & UNITY)

> **Materia:** Realidad Virtual y Aumentada  
> **Modalidad:** Trabajo en parejas  
> **Duración:** 3 Semanas  
> **Herramientas Obligatorias:** Unity, Oculus Quest, XR Interaction Toolkit, XR Device Simulator, Git / GitHub, Trello, Miro.

---

## 1. REGLA DE ORO
> **"NO intenten hacer un juego enorme. Es preferible tener 1 mecánica excelente que 10 mecánicas a medio terminar."**  
> El objetivo es un **prototipo VR funcional** y técnicamente sólido, no un juego masivo.

---

## 2. FLUJO DE TRABAJO OBLIGATORIO

```
MIRO ──► TRELLO ──► GIT ──► UNITY ──► TEST ──► TRELLO ──► GIT
```

### A. Miro (Ideación y Diseño)
Antes de programar, definir en el tablero:
1. **Idea inicial:** Concepto, qué hará el jugador, por qué es interesante.
2. **Público objetivo:** A quién va dirigida la experiencia.
3. **Referencias:** Visuales y de mecánicas.
4. **Core Loop:** Ejemplo: `Explorar -> Lanzar/Interactuar -> Feedback/Puntaje -> Recompensa -> Continuar`.
5. **Mecánicas:** Principal, secundarias e interacciones VR.
6. **Wireframes / Bocetos:** HUD, Menús, Escena, Interacciones.
7. **Gray Box:** Distribución básica del escenario previa a modelos finales.

### B. Trello (Gestión de Proyecto)
- **Columnas obligatorias:**
  1. `BACKLOG`: Ideas y tareas pendientes de priorizar.
  2. `TO DO`: Tareas seleccionadas para el sprint/semana.
  3. `DOING`: En desarrollo actual.
  4. `TO TEST`: Terminadas por el dev, listas para testeo por el compañero.
  5. `TESTING`: Siendo probadas activamente.
  6. `DONE`: Terminadas y verificadas.
- **Regla estricta:**  
  $$\text{DOING} \longrightarrow \text{TO TEST} \longrightarrow \text{TESTING} \longrightarrow \text{DONE}$$  
  *(Una tarea NUNCA pasa directo de Doing a Done).*
- **Estructura de cada tarjeta:**
  - Título y Descripción.
  - Responsable y Prioridad.
  - Fecha límite (si aplica).
  - Checklist con pasos específicos.

### C. Git & GitHub (Control de Versiones)
- **Ramas:**
  - `main` / `master`: Siempre estable y funcional.
  - Ramas por funcionalidad: `feature/nombre-mecanica`, `feature/sistema-guardado`, `feature/menu`, etc.
- **Commits:**
  - Pequeños, frecuentes y descriptivos usando Conventional Commits:
    - `feat: agrega interacción con balón`
    - `fix: corrige offset del collider del aro`
  - Evitar commits genéricos (`final`, `cosas`, `cambios`).
  - Prohibido hacer *force push* a `main` y no trabajar ambos directo en la misma rama.

---

## 3. REQUISITOS TÉCNICOS OBLIGATORIOS EN UNITY

### 3.1 Experiencia VR Funcional
- Interacción nativa con controladores de Oculus Quest (XR Interaction Toolkit).
- Movimiento / Posicionamiento e interacción dentro del entorno.
- Feedback claro al usuario (háptico, auditivo y visual/HUD).
- Probado primero con **XR Device Simulator** y validado en **dispositivo Oculus Quest**.

### 3.2 Scriptable Objects (Requisito Justificado)
- **No se permite usar Scriptable Objects solo por rellenar.** Deben aportar una ventaja arquitectónica real.
- **Usos válidos:**
  - Configuración de modos de juego / dificultades / estadísticas.
  - Datos de objetos interactivos / tipos de bolas / palos / dianas.
  - Eventos de puntuación y multiplicadores (`ScoreEventData`).
  - Configuración de audio y haptics reutilizables.
- **Defensa en presentación:** Responder con claridad: *¿Por qué se utilizó Scriptable Objects y qué problema solucionaron?*

### 3.3 Sistema de Guardado y Carga
- Almacenamiento persistente en disco (PlayerPrefs, JSON, etc.):
  $$\text{Jugar} \longrightarrow \text{Guardar} \longrightarrow \text{Cerrar / Reiniciar} \longrightarrow \text{Cargar} \longrightarrow \text{Recuperar progreso}$$
- No basta con mantener datos en memoria durante la ejecución.
- Datos a persistir: High Score, configuración, nivel desbloqueado, récords o estado de partida.

---

## 4. CRONOGRAMA DE 3 SEMANAS

| Semana | Objetivo Principal | Entregables Clave |
|---|---|---|
| **Semana 1** | **Ideación y Prototipo** | Miro completo, Trello armado, Git configurado con commits iniciales, Gray Box en Unity, XR Simulator funcionando, primera interacción VR. Meta: *"Ya podemos entrar a la experiencia y hacer algo"*. |
| **Semana 2** | **Desarrollo de la Mecánica** | Núcleo de la mecánica jugable, Scriptable Objects integrados, UI/HUD con feedback, primera versión de Save/Load, pruebas en Oculus Quest. Meta: *"Se puede jugar de principio a fin"*. |
| **Semana 3** | **Integración, Polish y Pruebas** | Save System definitivo, pulido de haptics/audio/vfx, optimización para Quest (framerate estable), corrección de bugs, merge final en Git. Meta: *"Experiencia funcional, estable y presentable"*. |

---

## 5. CRITERIOS DE EVALUACIÓN (PRESENTACIÓN 10 MIN)
1. Idea y diseño (Miro).
2. Gestión del proyecto y flujo de trabajo (Trello).
3. Uso correcto de ramas y commits (Git).
4. Implementación de interacciones VR y mecánica central.
5. Uso y justificación técnica de Scriptable Objects.
6. Demostración funcional del Sistema de Guardado (Guardar $\rightarrow$ Reiniciar $\rightarrow$ Cargar).
7. Rendimiento, comodidad y ausencia de bugs en Oculus Quest.

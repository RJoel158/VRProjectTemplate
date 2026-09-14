# 📋 Memoria y Directrices Obligatorias del Proyecto VR

Este documento resume y sintetiza cada uno de los **20 puntos del Proyecto de Autoaprendizaje de Realidad Virtual (Unity + Oculus Quest)**. Sirve como memoria técnica y marco de referencia estricto para la toma de decisiones, arquitectura de código, diseño de mecánicas y flujo de trabajo.

---

## 1. Descripción del Proyecto
- **Objetivo central**: Desarrollar un prototipo de Realidad Virtual funcional e interactivo para **Oculus Quest** en un plazo de **3 semanas** en parejas.
- **Enfoque**: No un juego masivo, sino una experiencia sólida, pulida y con una **mecánica central claramente definida** que demuestre la aplicación correcta de los conceptos de la materia.
- **Herramientas obligatorias**: Unity, Oculus Quest, XR Interaction Toolkit, XR Device Simulator, Git/GitHub, Trello, Miro.

---

## 2. Objetivos Principales
1. Interacción natural mediante controles VR (Oculus Quest / XRI).
2. Movimiento e interacción física dentro del entorno virtual.
3. Una mecánica principal protagonista.
4. Interfaz de usuario (UI en espacio mundial / Canvas interactivo) y feedback claro (háptico, sonoro, visual).
5. **Uso justificado y correcto de Scriptable Objects**.
6. **Sistema de guardado y carga persistente**.
7. Pruebas iterativas con **XR Device Simulator** y en hardware real **Oculus Quest**.
8. Control de versiones con ramas y commits descriptivos en Git.
9. Gestión del proyecto en Trello y diseño/ideación previa en Miro.

---

## 3. Características Mínimas del Proyecto
- **Experiencia VR Funcional**:
  - El usuario puede entrar, observar su entorno en 360°, usar mandos VR, ejecutar al menos una acción interactiva significativa y recibir feedback inmediato.
  - *No se aceptan escenas estáticas o meros paseos visuales sin mecánicas interactivas.*
- **Mecánica Principal**:
  - Claramente identificable (ej. Tiro deportivo / Campo de tiro con dianas, puntería, gestión de proyectiles y puntuación).
  - **Regla clave**: *Es preferible tener 1 mecánica excelente y robusta que 5 mecánicas a medio terminar.*

---

## 4. Scriptable Objects (Requisito Obligatorio y Justificado)
- **Regla fundamental**: No crear ScriptableObjects solo por crearlos; deben resolver un problema de arquitectura real.
- **Justificación en nuestro proyecto**:
  - `ScoreEventData`: Desacopla la lógica de puntuación de los objetos físicos (dianas, aros, bolos, etc.) permitiendo reutilizar el mismo `ScoreManager` entre distintos deportes sin tocar código.
  - `MinigameData`: Configura tiempos, reglas, nombres y escenas de cada deporte/minijuego desde assets del inspector sin hardcodear lógica.
- **Pregunta clave para la presentación**:
  > *¿Por qué utilizamos Scriptable Objects y qué problema solucionan?*
  > *Respuesta:* Permiten arquitectura desacoplada basada en datos, facilitando crear nuevos tipos de dianas, niveles y reglas de juego sin modificar los scripts ni recompilar.

---

## 5. Sistema de Guardado y Carga (Requisito Obligatorio)
- **Obligatoriedad**: Debe existir un sistema de guardado persistente (guardado en disco, e.g. JSON / PlayerPrefs / Binary).
- **Datos mínimos a persistir**:
  - Puntuaciones más altas (High Scores) por modo o carril.
  - Progreso del jugador / récords obtenidos.
  - Configuraciones relevantes (audio, preferencias de control/mano).
- **Flujo de prueba de la presentación**:
  $$\text{Jugar} \longrightarrow \text{Guardar} \longrightarrow \text{Cerrar / Reiniciar Juego} \longrightarrow \text{Cargar} \longrightarrow \text{Recuperar Progreso}$$

---

## 6. XR Device Simulator
- Usado para desarrollo diario ágil sin depender del casco físico en cada compilación:
  - Validar agarre de armas/objetos (`XR Grab Interactable`).
  - Probar rayos interactivos, teletransporte o locomoción.
  - Probar activadores (`Activate`, `Select`, `Hover`) y feedback.

---

## 7. Pruebas en Oculus Quest (Hardware Real)
- Validar siempre en dispositivo autónomo:
  - Ergonomía y escala del entorno (altura de mostradores, mesas a 0.85m, distancia de dianas legibles).
  - Rendimiento (frames estables a 72/90/120 Hz, evitar shaders o texturas pesadas innecesarias).
  - Confort en locomoción y ángulo de disparo.

---

## 8. Flujo de Trabajo Obligatorio
$$\text{MIRO} \longrightarrow \text{TRELLO} \longrightarrow \text{GIT} \longrightarrow \text{UNITY} \longrightarrow \text{TEST} \longrightarrow \text{TRELLO} \longrightarrow \text{GIT}$$

---

## 9. Miro – Ideación y Diseño
- Contenido obligatorio del tablero:
  1. **Idea inicial**: Concepto, qué hace el jugador, qué lo hace divertido.
  2. **Público objetivo**: A quién va dirigido.
  3. **Referencias**: Visuales, de mecánicas y videojuegos afines.
  4. **Core Loop**: Ciclo principal (ej. *Elegir arma/carril $\rightarrow$ Apuntar y disparar a dianas $\rightarrow$ Acumular puntos $\rightarrow$ Finalizar ronda $\rightarrow$ Guardar récord*).
  5. **Mecánicas**: Principal y secundarias.
  6. **Wireframes**: Bocetos de UI espacial, marcadores y distribución.
  7. **Gray Box**: Distribución preliminar del escenario en 3D.

---

## 10 & 11. Trello – Gestión del Proyecto y Regla de Flujo
- **Columnas obligatorias**:
  1. `BACKLOG` (Ideas pendientes de priorizar)
  2. `TO DO` (Tareas seleccionadas para la semana)
  3. `DOING` (En desarrollo activo)
  4. `TO TEST` (Terminado por el programador, listo para ser testeado por el compañero)
  5. `TESTING` (En proceso de prueba)
  6. `DONE` (Verificado y cerrado)
- **Regla estricta**: *Una tarea NUNCA pasa directo de `DOING` a `DONE`.* Debe pasar por `TO TEST` $\rightarrow$ `TESTING` $\rightarrow$ `DONE`.
- **Estructura de tarjetas**: Nombre claro, Descripción, Responsable, Prioridad, Checklist de pasos de prueba.

---

## 12 & 13. Git – Control de Versiones e Integración
- **Ramas por funcionalidad**:
  - `main` / `master` siempre estable y funcional.
  - Ramas descriptivas: `feature/campo-tiro`, `feature/sistema-guardado`, `feature/mecanica-disparo`, `feature/ui-puntuacion`.
- **Commits descriptivos y atómicos**:
  - `feat: agrega dianas de 3 distancias con soporte de balanceo`
  - `fix: corrige punto de colision en el aro/diana`
  - `feat: implementa guardado de high score en JSON`
  - *(Evitar commits vagos como "cambios", "final", "asdf")*.
- **Integración limpia**:
  - `Desarrollar` $\rightarrow$ `Commit` $\rightarrow$ `Push` $\rightarrow$ `Revisar` $\rightarrow$ `Integrar / Merge` $\rightarrow$ `Probar`.
  - Mantener [.gitignore](file:///c:/Users/wiget/VRProjectTemplate/.gitignore) limpio para evitar subir temporales de Unity (`Library/`, `.vs/`, builds).

---

## 14. Organización del Equipo
- Trabajo en pareja con responsabilidades claras divididas entre ambos integrantes (Sistemas, VR Interaction, Escenario/UI, Guardado, QA).

---

## 15. Cronograma de 3 Semanas
- **Semana 1 (Ideación y Prototipo)**: Miro, Trello inicial, Repo Git, Gray Box, primera interacción VR funcional.
- **Semana 2 (Desarrollo de la Mecánica)**: Núcleo jugable de principio a fin, Scriptable Objects configurados, UI y feedback, primera versión del sistema de guardado.
- **Semana 3 (Polish e Integración)**: Sistema de guardado definitivo, audio/efectos, optimización Oculus Quest, corrección de errores, demo final lista.

---

## 16. Presentación Final (10 Minutos)
Estructura de la defensa:
1. **Concepto**: Breve pitch de la experiencia.
2. **Miro**: Evolución desde la idea al diseño.
3. **Trello**: Flujo y división de tareas.
4. **Git**: Historial de ramas, commits y colaboración.
5. **Demostración VR**: Gameplay en Oculus Quest.
6. **Scriptable Objects**: Demostración técnica de qué son, cómo funcionan y por qué se usaron.
7. **Sistema de Guardado**: Prueba en vivo de guardado y recuperación de datos tras reinicio.

---

## 17, 18 & 19. Evaluación, Criterio Principal y Regla de Oro
- **Criterio rector**: $\text{Idea} \rightarrow \text{Diseño} \rightarrow \text{Implementación} \rightarrow \text{Organización} \rightarrow \text{Pruebas} \rightarrow \text{Resultado}$.
- **Regla de Oro**: Se premia la solidez técnica, arquitectura limpia, ausencia de bugs y experiencia pulida sobre cantidad de gráficos o exceso de mecánicas incompletas.

---

## 20. Lista de Entregables Finales
- [ ] Proyecto Unity funcional y optimizado.
- [ ] Repositorio Git con historial limpio.
- [ ] Tablero Trello completo y organizado.
- [ ] Tablero Miro documentado.
- [ ] Build APK funcional para Oculus Quest.
- [ ] Sistema de guardado/carga operativo.
- [ ] Implementación justificada de Scriptable Objects.
- [ ] Pruebas documentadas en XR Device Simulator y Oculus Quest.
- [ ] Presentación final de 10 minutos preparada.

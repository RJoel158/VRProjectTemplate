# Reglas y Directrices Obligatorias del Proyecto VR

Siempre que trabajes en este proyecto, debes ceñirte a las siguientes directrices extraídas del documento oficial de requerimientos:

1. **Mecánica Central y Calidad:**
   - Priorizar 1 mecánica sólida, libre de bugs y con excelente feedback antes que múltiples mecánicas a medio terminar.
   - Toda interacción VR debe proveer feedback claro (visual, sonoro, háptico/físico).

2. **Uso de Scriptable Objects (Obligatorio y Justificado):**
   - Usar ScriptableObjects (`ScoreEventData`, `MinigameData`, configuraciones de armas/dianas) para desacoplar lógica y datos.
   - Cada ScriptableObject debe tener una justificación arquitectónica clara para la defensa del proyecto.

3. **Sistema de Guardado y Carga (Obligatorio):**
   - Obligatorio implementar persistencia que cumpla el ciclo: `Guardar -> Cerrar/Reiniciar -> Cargar -> Recuperar progreso/High Scores`.

4. **Compatibilidad VR dual:**
   - El proyecto debe funcionar y ser testeable tanto en **XR Device Simulator** (en editor) como en **Oculus Quest** (standalone Android).
   - Mantener escala ergonómica (mostradores a ~0.85m de altura, botones y textos legibles en VR).

5. **Git y Control de Versiones:**
   - Ramas descriptivas por funcionalidad (`feature/...`, `fix/...`).
   - Commits pequeños, atómicos y descriptivos con convención (`feat: ...`, `fix: ...`, `chore: ...`).
   - Mantener el `.gitignore` limpio sin archivos temporales de Unity.

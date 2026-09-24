using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Tiro.Data;

namespace Tiro.Targets
{
    /// <summary>
    /// Representa una diana concentrica olimpica interactiva.
    /// Calcula con precision milimetrica la distancia del impacto al centro (Bullseye),
    /// coloca marcas de orificio de bala, anima el retroceso de la diana, genera popups de puntuacion
    /// y gestiona el abatimiento/descenso automatico al completar los puntos requeridos.
    /// </summary>
    public class TargetBoard : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TargetConfigSO config;
        [Tooltip("Punto central exacto de la diana (eje del Bullseye 'X').")]
        [SerializeField] private Transform targetCenter;
        [Tooltip("Pivote superior para animar el balanceo al recibir un impacto.")]
        [SerializeField] private Transform swingPivot;

        [Header("Distance & Identifier")]
        [SerializeField] private float distanceMeters = 10f;
        [SerializeField] private string laneName = "Carril 1 (10m)";

        [Header("Knockdown Settings")]
        [Tooltip("Puntos acumulados necesarios en esta diana para abatirse/descender.")]
        [SerializeField] private int pointsToKnockdown = 30;
        [Tooltip("Distancia en metros que desciende el objetivo al abatirse.")]
        [SerializeField] private float dropDistance = 3.5f;
        [Tooltip("Angulo de inclinacion hacia atras al abatirse (en grados).")]
        [SerializeField] private float dropAngle = 0f;
        [Tooltip("Duracion en segundos de la animacion de caida y subida.")]
        [SerializeField] private float dropDuration = 0.45f;

        [Header("Decals & Popups")]
        [SerializeField] private GameObject bulletHolePrefab;
        [SerializeField] private GameObject scorePopupPrefab;
        [SerializeField] private AudioSource audioSource;

        private List<GameObject> activeDecals = new List<GameObject>();
        private Coroutine swingCoroutine;
        private Quaternion initialSwingRot;

        private int currentPointsAccumulated = 0;
        private bool isKnockedDown = false;
        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private bool hasSavedInitialTransform = false;
        private Coroutine dropCoroutine;

        public float DistanceMeters => distanceMeters;
        public string LaneName => laneName;
        public TargetConfigSO Config => config;
        public bool IsKnockedDown => isKnockedDown;
        public int CurrentPointsAccumulated => currentPointsAccumulated;
        public int PointsToKnockdown => pointsToKnockdown;

        public event Action<TargetBoard, int, bool, Vector3> OnHitScored; // (target, score, isBullseye, hitPoint)
        public event Action<TargetBoard> OnTargetKnockedDown;

        private void Awake()
        {
            if (targetCenter == null) targetCenter = transform;
            if (swingPivot == null) swingPivot = transform;
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            initialSwingRot = swingPivot.localRotation;
            SaveInitialTransform();
        }

        private void SaveInitialTransform()
        {
            if (!hasSavedInitialTransform)
            {
                initialLocalPos = transform.localPosition;
                initialLocalRot = transform.localRotation;
                hasSavedInitialTransform = true;
            }
        }

        public void RegisterBulletHit(Vector3 worldHitPoint, Vector3 hitNormal)
        {
            if (isKnockedDown) return;

            // 1. Proyectar el punto de impacto en el plano local de la diana si existe targetCenter
            bool isBullseye = false;
            int score = 0;

            if (targetCenter != null && config != null)
            {
                Vector3 localHit = targetCenter.InverseTransformPoint(worldHitPoint);
                float distanceFromCenter = new Vector2(localHit.x, localHit.y).magnitude;
                score = config.CalculateScore(distanceFromCenter, out isBullseye);
            }

            // Cada impacto registrado en el MeshCollider puntua directamente (10 pts)
            if (score <= 0)
            {
                score = 10;
                isBullseye = true;
            }

            // 2. Crear orificio de bala en el punto exacto de la colision
            SpawnBulletHole(worldHitPoint, hitNormal);

            // 3. Feedback sonoro
            PlayHitSound(isBullseye);

            // 4. Animacion de balanceo
            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            swingCoroutine = StartCoroutine(AnimateHitSwing());

            // 5. Texto flotante de puntuacion
            SpawnScorePopup(worldHitPoint, score, isBullseye);

            currentPointsAccumulated += score;
            OnHitScored?.Invoke(this, score, isBullseye, worldHitPoint);

            // 6. Si alcanza la cuota de puntos de la diana, se abate/desciende
            if (pointsToKnockdown > 0 && currentPointsAccumulated >= pointsToKnockdown)
            {
                KnockDownTarget();
            }
        }

        public void KnockDownTarget()
        {
            if (isKnockedDown) return;
            isKnockedDown = true;
            SaveInitialTransform();

            // Desactivar colliders inmediatamente para impedir seguir farmeando impactos
            SetCollidersActive(false);

            // Sonido de diana derribada / abatida
            Tiro.Audio.ShootingAudioManager.PlayTargetKnockdown();

            // Texto informativo flotante
            SpawnKnockdownPopup();

            // Animacion suave de descenso e inclinacion
            if (dropCoroutine != null) StopCoroutine(dropCoroutine);
            dropCoroutine = StartCoroutine(AnimateDropRoutine(true));

            OnTargetKnockedDown?.Invoke(this);
        }

        private void SpawnBulletHole(Vector3 point, Vector3 normal)
        {
            GameObject decal;
            if (bulletHolePrefab != null)
            {
                decal = Instantiate(bulletHolePrefab, point + normal * 0.001f, Quaternion.LookRotation(normal), transform);
            }
            else
            {
                // Decal procedural si no hay prefab
                decal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                decal.name = "BulletHole";
                decal.transform.position = point + normal * 0.001f;
                decal.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = new Vector3(0.012f, 0.0005f, 0.012f);
                decal.transform.SetParent(transform, true);

                Destroy(decal.GetComponent<Collider>());
                Renderer r = decal.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    r.material.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Negro impacto
                }
            }

            activeDecals.Add(decal);
        }

        private void SpawnScorePopup(Vector3 point, int score, bool isBullseye)
        {
            GameObject popupObj = new GameObject("ScorePopup");
            popupObj.transform.position = point + Vector3.up * 0.05f + Vector3.back * 0.02f;
            popupObj.transform.rotation = Quaternion.identity;

            TextMeshPro tmp = popupObj.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 2.4f;

            if (isBullseye || score >= 10)
            {
                tmp.text = "+10";
                tmp.color = new Color(1f, 0.85f, 0.1f, 1f); // Oro reluciente
            }
            else if (score >= 9)
            {
                tmp.text = $"+{score}";
                tmp.color = new Color(0.2f, 0.9f, 0.3f, 1f); // Verde acierto alto
            }
            else if (score > 0)
            {
                tmp.text = $"+{score}";
                tmp.color = Color.white;
            }
            else
            {
                tmp.text = "FALLO";
                tmp.color = new Color(0.85f, 0.2f, 0.2f, 1f); // Rojo fallo
            }

            StartCoroutine(AnimateScorePopup(popupObj));
        }

        private void SpawnKnockdownPopup()
        {
            GameObject popupObj = new GameObject("KnockdownPopup");
            popupObj.transform.position = transform.position + Vector3.up * 0.35f;
            popupObj.transform.rotation = Quaternion.identity;

            TextMeshPro tmp = popupObj.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 3f;
            tmp.text = "DIANA ABATIDA";
            tmp.color = new Color(1f, 0.75f, 0.1f, 1f);

            StartCoroutine(AnimateScorePopup(popupObj));
        }

        private IEnumerator AnimateScorePopup(GameObject popup)
        {
            float elapsed = 0f;
            float duration = 0.9f;
            Vector3 startPos = popup.transform.position;
            Vector3 endPos = startPos + Vector3.up * 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (popup != null)
                {
                    popup.transform.position = Vector3.Lerp(startPos, endPos, t);
                    // Orientar hacia el visor del jugador
                    if (Camera.main != null)
                    {
                        popup.transform.rotation = Quaternion.LookRotation(popup.transform.position - Camera.main.transform.position);
                    }
                }
                yield return null;
            }

            if (popup != null) Destroy(popup);
        }

        private void PlayHitSound(bool isBullseye)
        {
            Tiro.Audio.ShootingAudioManager.PlayTargetHit(isBullseye);
        }

        private IEnumerator AnimateHitSwing()
        {
            if (swingPivot == null) yield break;

            // Inclinacion hacia atras por impacto
            Quaternion kickedRot = initialSwingRot * Quaternion.Euler(-6f, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < 0.05f)
            {
                elapsed += Time.deltaTime;
                swingPivot.localRotation = Quaternion.Slerp(initialSwingRot, kickedRot, elapsed / 0.05f);
                yield return null;
            }

            // Oscilacion de amortiguacion
            elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.25f;
                float damp = Mathf.Cos(t * Mathf.PI * 3f) * (1f - t);
                swingPivot.localRotation = Quaternion.Slerp(initialSwingRot, kickedRot, damp);
                yield return null;
            }

            swingPivot.localRotation = initialSwingRot;
            swingCoroutine = null;
        }

        private IEnumerator AnimateDropRoutine(bool dropDown)
        {
            if (!dropDown)
            {
                // Al iniciar la subida, asegurar que los renderers sean visibles
                SetRenderersActive(true);
            }

            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;

            Vector3 targetPos = dropDown ? (initialLocalPos + Vector3.down * dropDistance) : initialLocalPos;
            Quaternion targetRot = dropDown ? (initialLocalRot * Quaternion.Euler(dropAngle, 0f, 0f)) : initialLocalRot;

            float elapsed = 0f;
            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dropDuration);
                float smoothT = t * t * (3f - 2f * t);

                transform.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                yield return null;
            }

            transform.localPosition = targetPos;
            transform.localRotation = targetRot;

            if (dropDown)
            {
                // Al estar totalmente hundido, desactivar renderers para no interferir visualmente
                SetRenderersActive(false);
            }
            else
            {
                // Al terminar de subir, volver a activar los colliders
                SetCollidersActive(true);
            }

            dropCoroutine = null;
        }

        private void SetCollidersActive(bool active)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = active;
                }
            }
        }

        private void SetRenderersActive(bool active)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = active;
                }
            }
        }

        /// <summary>
        /// Limpia los impactos previos, restaura la diana y la levanta para la siguiente serie.
        /// </summary>
        public void ResetTarget()
        {
            SaveInitialTransform();
            currentPointsAccumulated = 0;
            isKnockedDown = false;
            SetRenderersActive(true);

            for (int i = 0; i < activeDecals.Count; i++)
            {
                if (activeDecals[i] != null)
                {
                    Destroy(activeDecals[i]);
                }
            }
            activeDecals.Clear();

            if (swingPivot != null)
            {
                swingPivot.localRotation = initialSwingRot;
            }

            if (dropCoroutine != null) StopCoroutine(dropCoroutine);
            dropCoroutine = StartCoroutine(AnimateDropRoutine(false));
        }
    }
}

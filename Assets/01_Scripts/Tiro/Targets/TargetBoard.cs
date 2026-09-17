using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Tiro.Data;

namespace Tiro.Targets
{
    /// <summary>
    /// Representa una diana concéntrica olímpica interactiva.
    /// Calcula con precisión milimétrica la distancia del impacto al centro (Bullseye),
    /// coloca marcas de orificio de bala, anima el retroceso de la diana y genera popups de puntuación.
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

        [Header("Decals & Popups")]
        [SerializeField] private GameObject bulletHolePrefab;
        [SerializeField] private GameObject scorePopupPrefab;
        [SerializeField] private AudioSource audioSource;

        private List<GameObject> activeDecals = new List<GameObject>();
        private Coroutine swingCoroutine;
        private Quaternion initialSwingRot;

        public float DistanceMeters => distanceMeters;
        public string LaneName => laneName;
        public TargetConfigSO Config => config;

        public event Action<TargetBoard, int, bool, Vector3> OnHitScored; // (target, score, isBullseye, hitPoint)

        private void Awake()
        {
            if (targetCenter == null) targetCenter = transform;
            if (swingPivot == null) swingPivot = transform;
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            initialSwingRot = swingPivot.localRotation;
        }

        public void RegisterBulletHit(Vector3 worldHitPoint, Vector3 hitNormal)
        {
            // 1. Proyectar el punto de impacto en el plano local de la diana
            Vector3 localHit = targetCenter.InverseTransformPoint(worldHitPoint);
            float distanceFromCenter = new Vector2(localHit.x, localHit.y).magnitude;

            // 2. Calcular puntuación concéntrica olímpica (1 a 10)
            bool isBullseye = false;
            int score = 0;

            if (config != null)
            {
                score = config.CalculateScore(distanceFromCenter, out isBullseye);
            }
            else
            {
                // Fallback estándar
                if (distanceFromCenter < 0.03f) { score = 10; isBullseye = true; }
                else if (distanceFromCenter < 0.07f) score = 9;
                else if (distanceFromCenter < 0.12f) score = 8;
                else if (distanceFromCenter < 0.18f) score = 7;
                else if (distanceFromCenter < 0.25f) score = 5;
                else score = 0;
            }

            // 3. Crear orificio de bala
            SpawnBulletHole(worldHitPoint, hitNormal);

            // 4. Feedback sonoro
            PlayHitSound(isBullseye);

            // 5. Animación de balanceo
            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            swingCoroutine = StartCoroutine(AnimateHitSwing());

            // 6. Texto flotante de puntuación
            SpawnScorePopup(worldHitPoint, score, isBullseye);

            OnHitScored?.Invoke(this, score, isBullseye, worldHitPoint);
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

            if (isBullseye)
            {
                tmp.text = "★ 10 X ★";
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

            // Inclinación hacia atrás por impacto
            Quaternion kickedRot = initialSwingRot * Quaternion.Euler(-6f, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < 0.05f)
            {
                elapsed += Time.deltaTime;
                swingPivot.localRotation = Quaternion.Slerp(initialSwingRot, kickedRot, elapsed / 0.05f);
                yield return null;
            }

            // Oscilación de amortiguación
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

        /// <summary>
        /// Limpia los impactos previos y deja la diana nueva para la siguiente serie.
        /// </summary>
        public void ResetTarget()
        {
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
        }
    }
}

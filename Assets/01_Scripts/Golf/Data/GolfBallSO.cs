using System;
using UnityEngine;

namespace Golf.Data
{
    /// <summary>
    /// Define la configuracion fisica y visual de la bola de golf.
    /// Utiliza ScriptableObject para permitir cambiar pelotas sin modificar codigo.
    /// </summary>
    [CreateAssetMenu(fileName = "GolfBallConfig", menuName = "VR Sports/Golf/Ball Config")]
    public class GolfBallSO : ScriptableObject
    {
        [Header("Physics Properties")]
        [Tooltip("Masa de la bola en kilogramos (estandar ~0.0459 kg).")]
        [SerializeField] private float mass = 0.046f;
        [Tooltip("Friccion dinamica contra el cesped sintetico.")]
        [SerializeField] private float grassDynamicFriction = 0.35f;
        [Tooltip("Friccion estatica para detener la bola en pendientes suaves.")]
        [SerializeField] private float grassStaticFriction = 0.45f;
        [Tooltip("Elasticidad o rebote contra las maderas laterales (0 a 1).")]
        [Range(0f, 1f)]
        [SerializeField] private float bounciness = 0.65f;
        [Tooltip("Resistencia al rodamiento (fuerza de frenado angular).")]
        [SerializeField] private float angularDrag = 1.2f;
        [Tooltip("Velocidad lineal minima para considerar la bola en reposo total.")]
        [SerializeField] private float sleepVelocityThreshold = 0.04f;

        [Header("Visuals & Trails")]
        [SerializeField] private Color ballColor = Color.white;
        [SerializeField] private bool enableTrail = true;
        [SerializeField] private Color trailColor = new Color(0.2f, 0.8f, 1f, 0.5f);
        [SerializeField] private float trailTime = 0.6f;

        public float Mass => mass;
        public float GrassDynamicFriction => grassDynamicFriction;
        public float GrassStaticFriction => grassStaticFriction;
        public float Bounciness => bounciness;
        public float AngularDrag => angularDrag;
        public float SleepVelocityThreshold => sleepVelocityThreshold;

        public Color BallColor => ballColor;
        public bool EnableTrail => enableTrail;
        public Color TrailColor => trailColor;
        public float TrailTime => trailTime;
    }
}

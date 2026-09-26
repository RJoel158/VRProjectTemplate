using System.Collections.Generic;
using UnityEngine;

namespace Golf.Environment
{
    /// <summary>
    /// Sistema procedural de bandada de aves costeras (gaviotas estilizadas) que sobrevuelan
    /// la laguna y el campo de minigolf con dinamica de aleteo y planeo natural.
    /// No depende de modelos externos y esta altamente optimizado para VR.
    /// </summary>
    public class GolfBirdFlock : MonoBehaviour
    {
        [Header("Flock Settings")]
        [SerializeField] private int birdCount = 6;
        [SerializeField] private Vector3 orbitCenter = new Vector3(8f, 13.5f, 6f);
        [SerializeField] private float minRadius = 10f;
        [SerializeField] private float maxRadius = 24f;
        [SerializeField] private float minHeight = 11f;
        [SerializeField] private float maxHeight = 17f;

        private class BirdInstance
        {
            public GameObject root;
            public Transform leftWing;
            public Transform rightWing;
            public float orbitRadius;
            public float orbitSpeed;
            public float currentAngle;
            public float baseHeight;
            public float heightFrequency;
            public float flapFrequency;
            public float flapTimer;
            public bool isGliding;
            public float glideCooldown;
            public float currentRoll;
        }

        private List<BirdInstance> birds = new List<BirdInstance>();
        private Material birdBodyMat;
        private Material birdWingMat;
        private Material birdBeakMat;

        private void Start()
        {
            CreateMaterials();
            SpawnFlock();
        }

        private void CreateMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            birdBodyMat = new Material(litShader)
            {
                name = "M_Bird_Body",
                color = new Color(0.96f, 0.97f, 1.0f)
            };

            birdWingMat = new Material(litShader)
            {
                name = "M_Bird_Wings",
                color = new Color(0.82f, 0.85f, 0.90f)
            };

            birdBeakMat = new Material(litShader)
            {
                name = "M_Bird_Beak",
                color = new Color(1.0f, 0.72f, 0.1f)
            };
        }

        private void SpawnFlock()
        {
            for (int i = 0; i < birdCount; i++)
            {
                var bird = CreateBird(i);
                birds.Add(bird);
            }
        }

        private BirdInstance CreateBird(int index)
        {
            GameObject birdRoot = new GameObject($"Seagull_{index + 1}");
            birdRoot.transform.SetParent(transform, false);

            // 1. Cuerpo / Fuselaje del ave
            GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bodyObj.name = "Body";
            bodyObj.transform.SetParent(birdRoot.transform, false);
            bodyObj.transform.localScale = new Vector3(0.14f, 0.11f, 0.44f);
            Destroy(bodyObj.GetComponent<Collider>());
            bodyObj.GetComponent<Renderer>().sharedMaterial = birdBodyMat;

            // 2. Cabeza y Pico
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.name = "Head";
            headObj.transform.SetParent(birdRoot.transform, false);
            headObj.transform.localPosition = new Vector3(0f, 0.04f, 0.22f);
            headObj.transform.localScale = new Vector3(0.10f, 0.10f, 0.13f);
            Destroy(headObj.GetComponent<Collider>());
            headObj.GetComponent<Renderer>().sharedMaterial = birdBodyMat;

            GameObject beakObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beakObj.name = "Beak";
            beakObj.transform.SetParent(headObj.transform, false);
            beakObj.transform.localPosition = new Vector3(0f, -0.01f, 0.08f);
            beakObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            beakObj.transform.localScale = new Vector3(0.025f, 0.06f, 0.025f);
            Destroy(beakObj.GetComponent<Collider>());
            beakObj.GetComponent<Renderer>().sharedMaterial = birdBeakMat;

            // 3. Cola
            GameObject tailObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tailObj.name = "Tail";
            tailObj.transform.SetParent(birdRoot.transform, false);
            tailObj.transform.localPosition = new Vector3(0f, 0.03f, -0.26f);
            tailObj.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            tailObj.transform.localScale = new Vector3(0.12f, 0.015f, 0.18f);
            Destroy(tailObj.GetComponent<Collider>());
            tailObj.GetComponent<Renderer>().sharedMaterial = birdWingMat;

            // 4. Ala Izquierda (Pivote en el cuerpo)
            GameObject leftPivot = new GameObject("LeftWingPivot");
            leftPivot.transform.SetParent(birdRoot.transform, false);
            leftPivot.transform.localPosition = new Vector3(-0.06f, 0.03f, 0.04f);

            GameObject leftWingMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWingMesh.name = "WingMesh_L";
            leftWingMesh.transform.SetParent(leftPivot.transform, false);
            leftWingMesh.transform.localPosition = new Vector3(-0.24f, 0f, 0f);
            leftWingMesh.transform.localRotation = Quaternion.Euler(0f, -12f, 2f);
            leftWingMesh.transform.localScale = new Vector3(0.48f, 0.015f, 0.14f);
            Destroy(leftWingMesh.GetComponent<Collider>());
            leftWingMesh.GetComponent<Renderer>().sharedMaterial = birdWingMat;

            // 5. Ala Derecha (Pivote en el cuerpo)
            GameObject rightPivot = new GameObject("RightWingPivot");
            rightPivot.transform.SetParent(birdRoot.transform, false);
            rightPivot.transform.localPosition = new Vector3(0.06f, 0.03f, 0.04f);

            GameObject rightWingMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWingMesh.name = "WingMesh_R";
            rightWingMesh.transform.SetParent(rightPivot.transform, false);
            rightWingMesh.transform.localPosition = new Vector3(0.24f, 0f, 0f);
            rightWingMesh.transform.localRotation = Quaternion.Euler(0f, 12f, -2f);
            rightWingMesh.transform.localScale = new Vector3(0.48f, 0.015f, 0.14f);
            Destroy(rightWingMesh.GetComponent<Collider>());
            rightWingMesh.GetComponent<Renderer>().sharedMaterial = birdWingMat;

            // Escala general del ave (~1 metro de envergadura)
            birdRoot.transform.localScale = Vector3.one * 1.1f;

            float radius = Random.Range(minRadius, maxRadius);
            float speed = Random.Range(0.22f, 0.38f);
            float startAngle = (float)index / birdCount * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float baseH = Random.Range(minHeight, maxHeight);

            return new BirdInstance
            {
                root = birdRoot,
                leftWing = leftPivot.transform,
                rightWing = rightPivot.transform,
                orbitRadius = radius,
                orbitSpeed = speed,
                currentAngle = startAngle,
                baseHeight = baseH,
                heightFrequency = Random.Range(0.3f, 0.7f),
                flapFrequency = Random.Range(7.5f, 9.5f),
                flapTimer = Random.Range(0f, 10f),
                isGliding = false,
                glideCooldown = Random.Range(2f, 5f),
                currentRoll = 0f
            };
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < birds.Count; i++)
            {
                var b = birds[i];
                if (b.root == null) continue;

                // 1. Orbita circular / eliptica
                b.currentAngle += b.orbitSpeed * dt;
                float x = orbitCenter.x + Mathf.Cos(b.currentAngle) * b.orbitRadius;
                float z = orbitCenter.z + Mathf.Sin(b.currentAngle) * b.orbitRadius;
                float y = b.baseHeight + Mathf.Sin(Time.time * b.heightFrequency + i) * 0.8f;

                Vector3 targetPos = new Vector3(x, y, z);
                Vector3 moveDir = (targetPos - b.root.transform.position);

                b.root.transform.position = targetPos;

                // 2. Orientacion tangencial mirando hacia adelante en la orbita
                Vector3 tangent = new Vector3(-Mathf.Sin(b.currentAngle), 0f, Mathf.Cos(b.currentAngle)).normalized;
                
                // 3. Inclinacion/Bancaje (Banking) realista al girar
                float targetRoll = -22f; // Inclinacion hacia el centro de la orbita
                b.currentRoll = Mathf.Lerp(b.currentRoll, targetRoll, dt * 2.5f);

                Quaternion lookRot = Quaternion.LookRotation(tangent, Vector3.up);
                b.root.transform.rotation = lookRot * Quaternion.Euler(0f, 0f, b.currentRoll);

                // 4. Animacion de aleteo y planeo
                b.glideCooldown -= dt;
                if (b.glideCooldown <= 0f)
                {
                    b.isGliding = !b.isGliding;
                    b.glideCooldown = b.isGliding ? Random.Range(2.5f, 5.0f) : Random.Range(1.8f, 3.5f);
                }

                float wingAngle = 0f;
                if (!b.isGliding)
                {
                    b.flapTimer += dt * b.flapFrequency;
                    wingAngle = Mathf.Sin(b.flapTimer) * 28f;
                }
                else
                {
                    // En planeo las alas se mantienen ligeramente elevadas en V (dihedral)
                    wingAngle = Mathf.Sin(Time.time * 1.5f + i) * 3f + 4f;
                }

                if (b.leftWing != null) b.leftWing.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
                if (b.rightWing != null) b.rightWing.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
            }
        }
    }
}

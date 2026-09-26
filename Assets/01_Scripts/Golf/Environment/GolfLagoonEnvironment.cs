using System.Collections.Generic;
using UnityEngine;
using Golf.Gameplay;

namespace Golf.Environment
{
    /// <summary>
    /// Gestiona la atmosfera visual de la laguna / resort del minigolf.
    /// - Elimina cualquier bloque de prueba o suelo solido residual que opaque el agua.
    /// - Embellece la superficie del agua con acabado turquesa de laguna y reflejos suaves.
    /// - Genera islotes y rocas estilizadas en el perimetro lejano del agua.
    /// - Genera boyas flotantes con movimiento senoidal sobre el agua.
    /// - Integra la bandada de aves costeras (GolfBirdFlock).
    /// </summary>
    public class GolfLagoonEnvironment : MonoBehaviour
    {
        [Header("Lagoon Configuration")]
        [SerializeField] private Vector3 lagoonCenter = new Vector3(8f, -0.6f, 6f);
        [SerializeField] private Color waterDeepColor = new Color(0.06f, 0.42f, 0.62f, 0.92f);
        [SerializeField] private Color waterShallowColor = new Color(0.15f, 0.68f, 0.82f, 0.85f);

        private GameObject waterPlane;
        private List<Transform> bobbingObjects = new List<Transform>();
        private List<float> bobbingOffsets = new List<float>();

        private void Awake()
        {
            // 1. Limpieza de cualquier bloque solido residual que opaque el lago
            CleanupObsoleteFloorBlocks();
        }

        private void Start()
        {
            SetupWaterPlane();
            SetupBackgroundIslands();
            SetupFloatingBuoys();
            SetupBirdFlock();
        }

        private void CleanupObsoleteFloorBlocks()
        {
            string[] obsoleteNames = { "Resort_Patio_Floor", "Hole2_Bridge_Catwalk", "Hole2_Catwalk_Ramp" };
            foreach (string objName in obsoleteNames)
            {
                var obj = GameObject.Find(objName);
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        private void SetupWaterPlane()
        {
            waterPlane = GameObject.Find("Resort_Water_Plane");
            if (waterPlane == null)
            {
                waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                waterPlane.name = "Resort_Water_Plane";
                waterPlane.transform.position = lagoonCenter;
                waterPlane.transform.localScale = new Vector3(10f, 1f, 10f); // 100m x 100m
            }

            var rend = waterPlane.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Standard");

                Material waterMat = new Material(litShader)
                {
                    name = "M_Lagoon_TurquoiseWater",
                    color = waterDeepColor
                };
                waterMat.SetFloat("_Smoothness", 0.92f);
                waterMat.SetFloat("_Metallic", 0.05f);
                rend.sharedMaterial = waterMat;
            }

            // Eliminar MeshCollider concavo si existe (los MeshColliders concavos no admiten isTrigger)
            var meshCol = waterPlane.GetComponent<MeshCollider>();
            if (meshCol != null)
            {
                Destroy(meshCol);
            }

            // Usar BoxCollider volumetrico como trigger de fuera de pista bajo el agua
            var boxCol = waterPlane.GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = waterPlane.AddComponent<BoxCollider>();
            }
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(120f, 2.5f, 120f);
            boxCol.center = new Vector3(0f, -0.6f, 0f);

            if (waterPlane.GetComponent<OutOfBoundsTrigger>() == null)
            {
                waterPlane.AddComponent<OutOfBoundsTrigger>();
            }
        }

        private void SetupBackgroundIslands()
        {
            GameObject islandGroup = new GameObject("Lagoon_Islands_Decorative");
            islandGroup.transform.SetParent(transform, false);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material stoneMat = new Material(litShader) { name = "M_Lagoon_Stone", color = new Color(0.42f, 0.44f, 0.46f) };
            Material sandMat = new Material(litShader) { name = "M_Lagoon_Sand", color = new Color(0.85f, 0.78f, 0.62f) };
            Material mossMat = new Material(litShader) { name = "M_Lagoon_Moss", color = new Color(0.22f, 0.48f, 0.20f) };

            // Posiciones perimetrales en el lago donde enmarcan la vista sin estorbar el juego
            Vector3[] islandPositions = new Vector3[]
            {
                new Vector3(-12f, -0.7f, 18f),
                new Vector3(-8f, -0.7f, -8f),
                new Vector3(22f, -0.7f, -6f),
                new Vector3(24f, -0.7f, 16f),
                new Vector3(6f, -0.7f, 22f),
                new Vector3(16f, -0.7f, 24f),
                new Vector3(-14f, -0.7f, 4f)
            };

            for (int i = 0; i < islandPositions.Length; i++)
            {
                Vector3 pos = islandPositions[i];
                GameObject island = new GameObject($"Islet_{i + 1}");
                island.transform.SetParent(islandGroup.transform, false);
                island.transform.position = pos;

                // Base rocosa
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rock.name = "RockBase";
                rock.transform.SetParent(island.transform, false);
                rock.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                float radius = Random.Range(2.5f, 4.0f);
                rock.transform.localScale = new Vector3(radius, 0.7f, radius * Random.Range(0.8f, 1.3f));
                rock.transform.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                Destroy(rock.GetComponent<Collider>());
                rock.GetComponent<Renderer>().sharedMaterial = stoneMat;

                // Cima verde / vegetacion
                GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                top.name = "LushTop";
                top.transform.SetParent(island.transform, false);
                top.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                top.transform.localScale = new Vector3(radius * 0.85f, 0.55f, radius * 0.85f);
                Destroy(top.GetComponent<Collider>());
                top.GetComponent<Renderer>().sharedMaterial = mossMat;

                // Pequenas rocas accesorias en la orilla
                for (int r = 0; r < 2; r++)
                {
                    GameObject subRock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    subRock.transform.SetParent(island.transform, false);
                    float subAngle = r * 2.2f + i;
                    subRock.transform.localPosition = new Vector3(Mathf.Cos(subAngle) * radius * 0.5f, 0.25f, Mathf.Sin(subAngle) * radius * 0.5f);
                    subRock.transform.localScale = Vector3.one * Random.Range(0.6f, 1.1f);
                    Destroy(subRock.GetComponent<Collider>());
                    subRock.GetComponent<Renderer>().sharedMaterial = stoneMat;
                }
            }
        }

        private void SetupFloatingBuoys()
        {
            GameObject buoyGroup = new GameObject("Lagoon_Buoys_Decorative");
            buoyGroup.transform.SetParent(transform, false);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material buoyRedMat = new Material(litShader) { name = "M_Buoy_Red", color = new Color(0.88f, 0.20f, 0.15f) };
            Material buoyWhiteMat = new Material(litShader) { name = "M_Buoy_White", color = new Color(0.95f, 0.95f, 0.95f) };

            // Posiciones de boyas flotando en el agua alrededor del circuito
            Vector3[] buoyPositions = new Vector3[]
            {
                new Vector3(-2.8f, -0.55f, 2.5f),
                new Vector3(-2.5f, -0.55f, 8.5f),
                new Vector3(4.5f, -0.55f, -2.5f),
                new Vector3(11.5f, -0.55f, -2.0f),
                new Vector3(18.5f, -0.55f, 3.5f),
                new Vector3(18.0f, -0.55f, 11.5f),
                new Vector3(9.5f, -0.55f, 14.5f),
                new Vector3(2.5f, -0.55f, 13.5f)
            };

            for (int i = 0; i < buoyPositions.Length; i++)
            {
                Vector3 pos = buoyPositions[i];
                GameObject buoy = new GameObject($"Buoy_{i + 1}");
                buoy.transform.SetParent(buoyGroup.transform, false);
                buoy.transform.position = pos;

                // Esfera flotador
                GameObject floatObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                floatObj.name = "Float";
                floatObj.transform.SetParent(buoy.transform, false);
                floatObj.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                floatObj.transform.localScale = new Vector3(0.42f, 0.45f, 0.42f);
                Destroy(floatObj.GetComponent<Collider>());
                floatObj.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? buoyRedMat : buoyWhiteMat;

                // Anillo de contraste
                GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ringObj.name = "Ring";
                ringObj.transform.SetParent(buoy.transform, false);
                ringObj.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                ringObj.transform.localScale = new Vector3(0.44f, 0.08f, 0.44f);
                Destroy(ringObj.GetComponent<Collider>());
                ringObj.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? buoyWhiteMat : buoyRedMat;

                // Mastil con banderita
                GameObject mastObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mastObj.name = "Mast";
                mastObj.transform.SetParent(buoy.transform, false);
                mastObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                mastObj.transform.localScale = new Vector3(0.03f, 0.25f, 0.03f);
                Destroy(mastObj.GetComponent<Collider>());
                mastObj.GetComponent<Renderer>().sharedMaterial = buoyWhiteMat;

                bobbingObjects.Add(buoy.transform);
                bobbingOffsets.Add(i * 0.85f);
            }
        }

        private void SetupBirdFlock()
        {
            if (GetComponent<GolfBirdFlock>() == null)
            {
                gameObject.AddComponent<GolfBirdFlock>();
            }
        }

        private void Update()
        {
            // Animacion de vaiven senoidal suave de las boyas en el agua
            float time = Time.time;
            for (int i = 0; i < bobbingObjects.Count; i++)
            {
                var b = bobbingObjects[i];
                if (b == null) continue;

                float offset = bobbingOffsets[i];
                float yOffset = Mathf.Sin(time * 1.6f + offset) * 0.035f;
                float tiltAngle = Mathf.Sin(time * 1.2f + offset) * 4.5f;

                Vector3 currentPos = b.position;
                b.position = new Vector3(currentPos.x, -0.55f + yOffset, currentPos.z);
                b.rotation = Quaternion.Euler(tiltAngle, offset * 45f, Mathf.Cos(time * 1.4f + offset) * 3f);
            }
        }
    }
}

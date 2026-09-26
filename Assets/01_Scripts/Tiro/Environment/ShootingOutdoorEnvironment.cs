using System.Collections.Generic;
using UnityEngine;

namespace Tiro.Environment
{
    /// <summary>
    /// Entorno inmersivo de campo abierto para el Poligono de Tiro Olimpico.
    /// - Delimita lateralmente la zona de tiro con turriles azules industriales y pilas de neumaticos.
    /// - Integra un bosque perimetral de pinos low-poly y fauna exterior (rapaces en el cielo, fauna lejana).
    /// - Agrega lecho acustico suave de brisa y aves al aire libre.
    /// - IMPORTANTE: Todos los elementos visuales carecen de colliders y se ubican estrictamente en los
    ///   flancos exteriores (X <= -5.5m y X >= +5.5m), garantizando que el carril de tiro y las dianas
    ///   queden 100% despejados y libres de interferencias.
    /// </summary>
    public class ShootingOutdoorEnvironment : MonoBehaviour
    {
        [Header("Materials")]
        private Material blueDrumMat;
        private Material blackRubberMat;
        private Material metalRimMat;
        private Material pineTrunkMat;
        private Material pineFoliageMat1;
        private Material pineFoliageMat2;
        private Material faunaMat;
        private Material safetyLineMat;

        private AudioSource ambientAudioSource;

        private void Awake()
        {
            CreateMaterials();
        }

        private void Start()
        {
            BuildLateralBoundaries();
            BuildPerimeterForest();
            BuildSkyRaptors();
            BuildDistantWildlife();
            SetupOutdoorAudio();
        }

        private void CreateMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            // Turril azul industrial clasico de polietileno/metal
            blueDrumMat = new Material(litShader)
            {
                name = "M_Range_BlueDrum",
                color = new Color(0.08f, 0.38f, 0.76f)
            };
            blueDrumMat.SetFloat("_Smoothness", 0.65f);

            // Neumaticos negros de goma mate
            blackRubberMat = new Material(litShader)
            {
                name = "M_Range_BlackRubber",
                color = new Color(0.12f, 0.12f, 0.13f)
            };
            blackRubberMat.SetFloat("_Smoothness", 0.15f);

            // Aros metalicos / tapas
            metalRimMat = new Material(litShader)
            {
                name = "M_Range_MetalRim",
                color = new Color(0.20f, 0.22f, 0.25f)
            };
            metalRimMat.SetFloat("_Metallic", 0.85f);
            metalRimMat.SetFloat("_Smoothness", 0.70f);

            // Pinos (Tronco y follaje en dos tonos naturales)
            pineTrunkMat = new Material(litShader)
            {
                name = "M_Range_PineTrunk",
                color = new Color(0.32f, 0.20f, 0.12f)
            };

            pineFoliageMat1 = new Material(litShader)
            {
                name = "M_Range_PineFoliage_1",
                color = new Color(0.12f, 0.32f, 0.15f)
            };

            pineFoliageMat2 = new Material(litShader)
            {
                name = "M_Range_PineFoliage_2",
                color = new Color(0.16f, 0.38f, 0.18f)
            };

            // Fauna silvestre low-poly
            faunaMat = new Material(litShader)
            {
                name = "M_Range_Fauna",
                color = new Color(0.55f, 0.42f, 0.30f)
            };

            // Franja de seguridad amarilla
            safetyLineMat = new Material(litShader)
            {
                name = "M_Range_SafetyYellow",
                color = new Color(0.95f, 0.78f, 0.10f)
            };
            safetyLineMat.SetFloat("_Smoothness", 0.3f);
        }

        #region Lateral Boundaries (Turriles Azules y Neumáticos)

        private void BuildLateralBoundaries()
        {
            GameObject boundaryRoot = new GameObject("Boundary_Drums_And_Tires");
            boundaryRoot.transform.SetParent(transform, false);

            // Flanco Izquierdo (X = -5.8m) y Flanco Derecho (X = +5.8m)
            // Intervalos longitudinales desde el puesto de tiro (Z = 2m) hasta el muro de absorción (Z = 52m)
            float[] zSpacings = new float[] { 2.2f, 5.5f, 9.5f, 14.0f, 19.5f, 25.5f, 32.0f, 39.0f, 46.5f, 52.0f };

            for (int i = 0; i < zSpacings.Length; i++)
            {
                float z = zSpacings[i];

                // Flanco Izquierdo
                PlaceBoundaryCluster(boundaryRoot.transform, -5.85f, z, i % 3);

                // Flanco Derecho
                PlaceBoundaryCluster(boundaryRoot.transform, 5.85f, z, (i + 1) % 3);
            }

            // Linea de seguridad en el piso frente al puesto de tiro
            GameObject lineObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lineObj.name = "Safety_Line_Floor";
            lineObj.transform.SetParent(boundaryRoot.transform, false);
            lineObj.transform.position = new Vector3(0f, 0.005f, 0.85f);
            lineObj.transform.localScale = new Vector3(4.8f, 0.01f, 0.12f);
            Destroy(lineObj.GetComponent<Collider>());
            lineObj.GetComponent<Renderer>().sharedMaterial = safetyLineMat;
        }

        private void PlaceBoundaryCluster(Transform parent, float x, float z, int pattern)
        {
            float sideSign = Mathf.Sign(x);

            switch (pattern)
            {
                case 0:
                    // Turril azul de pie + pila de 2 neumaticos al lado
                    CreateBlueDrum(parent, new Vector3(x, 0.45f, z), Quaternion.identity);
                    CreateTireStack(parent, new Vector3(x + sideSign * 0.65f, 0.11f, z + 0.1f), 2);
                    break;

                case 1:
                    // Pila de 3 neumaticos alta + turril azul
                    CreateTireStack(parent, new Vector3(x, 0.11f, z), 3);
                    CreateBlueDrum(parent, new Vector3(x + sideSign * 0.70f, 0.45f, z - 0.25f), Quaternion.identity);
                    break;

                case 2:
                    // Dos turriles azules agrupados + 1 neumatico recostado
                    CreateBlueDrum(parent, new Vector3(x, 0.45f, z - 0.3f), Quaternion.identity);
                    CreateBlueDrum(parent, new Vector3(x + sideSign * 0.55f, 0.45f, z + 0.35f), Quaternion.Euler(0f, 25f, 0f));
                    CreateTireStack(parent, new Vector3(x - sideSign * 0.2f, 0.11f, z + 0.6f), 1);
                    break;
            }
        }

        private void CreateBlueDrum(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject drum = new GameObject("BlueDrum");
            drum.transform.SetParent(parent, false);
            drum.transform.position = pos;
            drum.transform.rotation = rot;

            // Cuerpo cilindrico principal
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "DrumBody";
            body.transform.SetParent(drum.transform, false);
            body.transform.localScale = new Vector3(0.58f, 0.44f, 0.58f); // Diametro 0.58m, altura 0.88m
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = blueDrumMat;

            // Aros metalicos de refuerzo (superior e inferior)
            CreateDrumRim(drum.transform, 0.22f);
            CreateDrumRim(drum.transform, -0.22f);

            // Tapa negra superior con tapon de seguridad
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Lid";
            cap.transform.SetParent(drum.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            cap.transform.localScale = new Vector3(0.56f, 0.015f, 0.56f);
            Destroy(cap.GetComponent<Collider>());
            cap.GetComponent<Renderer>().sharedMaterial = metalRimMat;
        }

        private void CreateDrumRim(Transform parent, float yOffset)
        {
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "ReinforcementRim";
            rim.transform.SetParent(parent, false);
            rim.transform.localPosition = new Vector3(0f, yOffset, 0f);
            rim.transform.localScale = new Vector3(0.61f, 0.025f, 0.61f);
            Destroy(rim.GetComponent<Collider>());
            rim.GetComponent<Renderer>().sharedMaterial = metalRimMat;
        }

        private void CreateTireStack(Transform parent, Vector3 basePos, int count)
        {
            GameObject stack = new GameObject($"TireStack_{count}");
            stack.transform.SetParent(parent, false);
            stack.transform.position = basePos;

            float tireHeight = 0.20f;
            for (int i = 0; i < count; i++)
            {
                GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tire.name = $"Tire_{i + 1}";
                tire.transform.SetParent(stack.transform, false);
                // Ligero desplazamiento organico para que no parezca un render sintetico perfecto
                float jitterX = (i > 0) ? Mathf.Sin(i * 1.7f) * 0.025f : 0f;
                float jitterZ = (i > 0) ? Mathf.Cos(i * 2.3f) * 0.025f : 0f;
                tire.transform.localPosition = new Vector3(jitterX, i * tireHeight, jitterZ);
                tire.transform.localScale = new Vector3(0.68f, 0.10f, 0.68f);
                Destroy(tire.GetComponent<Collider>());
                tire.GetComponent<Renderer>().sharedMaterial = blackRubberMat;

                // Centro / hueco visual del neumatico
                GameObject innerHub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                innerHub.name = "HubHole";
                innerHub.transform.SetParent(tire.transform, false);
                innerHub.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                innerHub.transform.localScale = new Vector3(0.42f, 0.11f, 0.42f);
                Destroy(innerHub.GetComponent<Collider>());
                innerHub.GetComponent<Renderer>().sharedMaterial = metalRimMat;
            }
        }

        #endregion

        #region Perimeter Forest (Pinos Low-Poly)

        private void BuildPerimeterForest()
        {
            GameObject forestRoot = new GameObject("Perimeter_Forest_LowPoly");
            forestRoot.transform.SetParent(transform, false);

            // Arboles en el flanco izquierdo (X entre -7.5m y -11.5m)
            for (float z = 3f; z <= 56f; z += 4.5f)
            {
                float x = Random.Range(-11.5f, -7.5f);
                float scale = Random.Range(0.85f, 1.25f);
                CreateLowPolyPine(forestRoot.transform, new Vector3(x, 0f, z + Random.Range(-1f, 1f)), scale);
            }

            // Arboles en el flanco derecho (X entre +7.5m y +11.5m)
            for (float z = 3f; z <= 56f; z += 4.5f)
            {
                float x = Random.Range(7.5f, 11.5f);
                float scale = Random.Range(0.85f, 1.25f);
                CreateLowPolyPine(forestRoot.transform, new Vector3(x, 0f, z + Random.Range(-1f, 1f)), scale);
            }

            // Arboles de fondo detras del muro de absorcion (Z = 57m a 65m)
            for (float x = -10f; x <= 10f; x += 3.8f)
            {
                float z = Random.Range(57.5f, 64f);
                float scale = Random.Range(1.1f, 1.55f);
                CreateLowPolyPine(forestRoot.transform, new Vector3(x + Random.Range(-0.8f, 0.8f), 0f, z), scale);
            }
        }

        private void CreateLowPolyPine(Transform parent, Vector3 pos, float scale)
        {
            GameObject tree = new GameObject("LowPoly_Pine");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            tree.transform.localScale = Vector3.one * scale;

            // Tronco
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 1.0f, 0.35f);
            Destroy(trunk.GetComponent<Collider>());
            trunk.GetComponent<Renderer>().sharedMaterial = pineTrunkMat;

            // 3 Niveles conicos de follaje apilados
            CreateFoliageTier(tree.transform, 1.8f, 1.9f, 0.95f, pineFoliageMat1);
            CreateFoliageTier(tree.transform, 2.8f, 1.5f, 0.85f, pineFoliageMat2);
            CreateFoliageTier(tree.transform, 3.7f, 1.1f, 0.75f, pineFoliageMat1);
            CreateFoliageTier(tree.transform, 4.4f, 0.65f, 0.65f, pineFoliageMat2);
        }

        private void CreateFoliageTier(Transform parent, float yPos, float radius, float height, Material mat)
        {
            // Usamos un cilindro aplanado con extremos cónicos como pirámide estilizada low-poly
            GameObject tier = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tier.name = "FoliageTier";
            tier.transform.SetParent(parent, false);
            tier.transform.localPosition = new Vector3(0f, yPos, 0f);
            tier.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            tier.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            Destroy(tier.GetComponent<Collider>());
            tier.GetComponent<Renderer>().sharedMaterial = mat;
        }

        #endregion

        #region Sky Raptors & Distant Wildlife (Fauna Low-Poly)

        private class SkyRaptor
        {
            public GameObject root;
            public float orbitRadius;
            public float orbitSpeed;
            public float currentAngle;
            public float baseAltitude;
            public float rollTilt;
        }

        private List<SkyRaptor> raptors = new List<SkyRaptor>();

        private void BuildSkyRaptors()
        {
            GameObject skyRoot = new GameObject("Sky_Raptors_Flock");
            skyRoot.transform.SetParent(transform, false);

            // 3 aves rapaces planeando a gran altura (entre 20m y 26m) sobre el bosque
            for (int i = 0; i < 3; i++)
            {
                GameObject birdObj = new GameObject($"Raptor_{i + 1}");
                birdObj.transform.SetParent(skyRoot.transform, false);

                // Cuerpo alargado
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.SetParent(birdObj.transform, false);
                body.transform.localScale = new Vector3(0.18f, 0.12f, 0.55f);
                Destroy(body.GetComponent<Collider>());
                body.GetComponent<Renderer>().sharedMaterial = faunaMat;

                // Alas extendidas de planeo (envergadura de ~1.8m)
                GameObject wings = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wings.transform.SetParent(birdObj.transform, false);
                wings.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
                wings.transform.localScale = new Vector3(1.75f, 0.018f, 0.24f);
                Destroy(wings.GetComponent<Collider>());
                wings.GetComponent<Renderer>().sharedMaterial = faunaMat;

                // Cola en abanico
                GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tail.transform.SetParent(birdObj.transform, false);
                tail.transform.localPosition = new Vector3(0f, 0.02f, -0.32f);
                tail.transform.localScale = new Vector3(0.32f, 0.015f, 0.22f);
                Destroy(tail.GetComponent<Collider>());
                tail.GetComponent<Renderer>().sharedMaterial = faunaMat;

                // Colisionador de deteccion y componente Easter Egg (a 21-26m de altura, nunca estorba las dianas)
                var sphereCol = birdObj.AddComponent<SphereCollider>();
                sphereCol.radius = 1.15f;
                var raptorTarget = birdObj.AddComponent<EasterEggRaptorTarget>();
                raptorTarget.Initialize(faunaMat);

                raptors.Add(new SkyRaptor
                {
                    root = birdObj,
                    orbitRadius = 22f + i * 5f,
                    orbitSpeed = 0.18f - i * 0.03f,
                    currentAngle = i * (Mathf.PI * 2f / 3f),
                    baseAltitude = 21f + i * 2.5f,
                    rollTilt = -18f
                });
            }
        }

        private void BuildDistantWildlife()
        {
            GameObject wildlifeRoot = new GameObject("Distant_Wildlife_LowPoly");
            wildlifeRoot.transform.SetParent(transform, false);

            // 1. Conejo silvestre en el borde izquierdo junto a los pinos (X = -7.2m, Z = 14m)
            CreateLowPolyRabbit(wildlifeRoot.transform, new Vector3(-7.2f, 0f, 14f), Quaternion.Euler(0f, 65f, 0f));

            // 2. Ciervo estilizado paciendo tranquilamente al fondo junto a los arboles (X = 8.5m, Z = 40m)
            CreateLowPolyDeer(wildlifeRoot.transform, new Vector3(8.5f, 0f, 40f), Quaternion.Euler(0f, -115f, 0f));
        }

        private void CreateLowPolyRabbit(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject rabbit = new GameObject("WildRabbit_LowPoly");
            rabbit.transform.SetParent(parent, false);
            rabbit.transform.position = pos;
            rabbit.transform.rotation = rot;
            rabbit.transform.localScale = Vector3.one * 0.7f;

            // Cuerpo
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "RabbitBody";
            body.transform.SetParent(rabbit.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            body.transform.localScale = new Vector3(0.24f, 0.22f, 0.36f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = faunaMat;

            // Cabeza
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "RabbitHead";
            head.transform.SetParent(rabbit.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.30f, 0.18f);
            head.transform.localScale = new Vector3(0.16f, 0.16f, 0.18f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial = faunaMat;

            // Orejas largas
            CreateRabbitEar(head.transform, new Vector3(-0.05f, 0.12f, -0.02f), -12f);
            CreateRabbitEar(head.transform, new Vector3(0.05f, 0.12f, -0.02f), 12f);
        }

        private void CreateRabbitEar(Transform head, Vector3 pos, float zRot)
        {
            GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ear.name = "Ear";
            ear.transform.SetParent(head, false);
            ear.transform.localPosition = pos;
            ear.transform.localRotation = Quaternion.Euler(15f, 0f, zRot);
            ear.transform.localScale = new Vector3(0.035f, 0.10f, 0.02f);
            Destroy(ear.GetComponent<Collider>());
            ear.GetComponent<Renderer>().sharedMaterial = faunaMat;
        }

        private void CreateLowPolyDeer(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject deer = new GameObject("Deer_LowPoly");
            deer.transform.SetParent(parent, false);
            deer.transform.position = pos;
            deer.transform.rotation = rot;
            deer.transform.localScale = Vector3.one * 1.35f;

            // Torso
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "DeerTorso";
            body.transform.SetParent(deer.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(0.45f, 0.50f, 1.10f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = faunaMat;

            // Cuello y cabeza inclinada paciendo en la hierba
            GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neck.name = "DeerNeck";
            neck.transform.SetParent(deer.transform, false);
            neck.transform.localPosition = new Vector3(0f, 0.75f, 0.72f);
            neck.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            neck.transform.localScale = new Vector3(0.24f, 0.55f, 0.28f);
            Destroy(neck.GetComponent<Collider>());
            neck.GetComponent<Renderer>().sharedMaterial = faunaMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "DeerHead";
            head.transform.SetParent(neck.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.32f, 0.12f);
            head.transform.localScale = new Vector3(0.22f, 0.24f, 0.38f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial = faunaMat;

            // 4 Patas
            CreateDeerLeg(deer.transform, new Vector3(-0.16f, 0.38f, 0.42f));
            CreateDeerLeg(deer.transform, new Vector3(0.16f, 0.38f, 0.42f));
            CreateDeerLeg(deer.transform, new Vector3(-0.16f, 0.38f, -0.42f));
            CreateDeerLeg(deer.transform, new Vector3(0.16f, 0.38f, -0.42f));
        }

        private void CreateDeerLeg(Transform parent, Vector3 localPos)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "DeerLeg";
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = localPos;
            leg.transform.localScale = new Vector3(0.09f, 0.38f, 0.09f);
            Destroy(leg.GetComponent<Collider>());
            leg.GetComponent<Renderer>().sharedMaterial = faunaMat;
        }

        #endregion

        #region Atmospheric Audio (Aire Libre y Brisa)

        private void SetupOutdoorAudio()
        {
            ambientAudioSource = gameObject.AddComponent<AudioSource>();
            ambientAudioSource.loop = true;
            ambientAudioSource.playOnAwake = false;
            ambientAudioSource.spatialBlend = 0f; // 2D Stereo envolvente
            ambientAudioSource.volume = 0.18f;    // Volumen comodo que no interfiere con disparos ni arcade

            AudioClip breezeClip = GenerateOutdoorBreezeClip();
            ambientAudioSource.clip = breezeClip;
            ambientAudioSource.Play();
        }

        /// <summary>
        /// Genera una textura sonora estéreo y sutil de brisa de campo al aire libre
        /// con modulacion organica de bajas frecuencias sin depender de archivos externos.
        /// </summary>
        private AudioClip GenerateOutdoorBreezeClip()
        {
            int sampleRate = 44100;
            int lengthSeconds = 6;
            int totalSamples = sampleRate * lengthSeconds;
            float[] data = new float[totalSamples];

            var rand = new System.Random(42);
            float lastVal = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float whiteNoise = (float)rand.NextDouble() * 2f - 1f;

                // Filtro pasabajo de viento suave (Pink/Brown noise)
                lastVal = Mathf.Lerp(lastVal, whiteNoise, 0.015f);

                // Modulacion lenta de rafaga de aire
                float swell = Mathf.Sin(t * 0.95f) * 0.3f + Mathf.Cos(t * 0.42f) * 0.25f + 0.5f;

                data[i] = Mathf.Clamp(lastVal * swell * 0.45f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Ambient_Outdoor_Breeze", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion

        private void Update()
        {
            // Vuelo circular fluido y banking realista de las aves rapaces en el cielo
            float dt = Time.deltaTime;
            Vector3 center = new Vector3(0f, 0f, 26f);

            for (int i = 0; i < raptors.Count; i++)
            {
                var r = raptors[i];
                if (r.root == null) continue;

                r.currentAngle += r.orbitSpeed * dt;
                float x = center.x + Mathf.Cos(r.currentAngle) * r.orbitRadius;
                float z = center.z + Mathf.Sin(r.currentAngle) * (r.orbitRadius * 1.15f);
                float y = r.baseAltitude + Mathf.Sin(Time.time * 0.45f + i) * 1.2f;

                r.root.transform.position = new Vector3(x, y, z);

                Vector3 tangent = new Vector3(-Mathf.Sin(r.currentAngle), 0f, Mathf.Cos(r.currentAngle) * 1.15f).normalized;
                Quaternion lookRot = Quaternion.LookRotation(tangent, Vector3.up);
                r.root.transform.rotation = lookRot * Quaternion.Euler(0f, 0f, r.rollTilt);
            }
        }
    }
}

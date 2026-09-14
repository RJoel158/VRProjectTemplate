using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum RifleType
{
    SemiAutomatic,  // Ruger 10/22 LR
    BoltAction      // Rifle de Cerrojo manual
}

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class VRRifle : MonoBehaviour
{
    [Header("Tipo de Rifle")]
    public RifleType rifleType = RifleType.SemiAutomatic;

    [Header("Componentes de Miras y Cañón")]
    public Transform muzzlePoint;
    public Transform frontSight;
    public Transform rearSight;
    public Transform attachPoint;
    public Transform boltTransform;          // Cerrojo que se mueve hacia atrás al disparar
    public Transform shellEjectionPoint;     // Ventana de expulsión de casquillos
    public Transform modelRoot;              // Nodo del modelo para retroceso procedural

    [Header("Balística y Disparo")]
    public float maxRange = 300f;
    public float fireRate = 0.18f;
    public float bulletImpactForce = 60f;
    public LayerMask hitLayers = ~0;

    [Header("Sistema de Munición y Recarga")]
    public int magazineCapacity = 10;
    public int currentAmmo = 10;
    public bool infiniteAmmo = false;
    public float reloadDuration = 1.5f;
    public bool isReloading = false;

    [Header("Retroceso Procedural (Estilo Bodycam)")]
    public float recoilKickBack = 0.04f;      // Desplazamiento hacia atrás
    public float recoilKickUp = 4.5f;         // Elevación del cañón (grados)
    public float recoilKickSide = 0.8f;       // Desviación lateral aleatoria
    public float recoilReturnSpeed = 16f;     // Velocidad de recuperación

    [Header("Animación de Cerrojo")]
    public float boltCycleDistance = 0.045f;
    public float boltCycleDuration = 0.08f;

    [Header("Feedback Háptico y Audio")]
    [Range(0f, 1f)] public float fireHapticIntensity = 0.85f;
    public float fireHapticDuration = 0.12f;
    public AudioClip fireAudioClip;
    public AudioClip reloadAudioClip;
    public AudioClip emptyAudioClip;
    public AudioClip boltCycleAudioClip;
    public ParticleSystem muzzleFlash;

    [Header("Efectos Visuales")]
    public LineRenderer tracerLineRenderer;
    public float tracerDuration = 0.04f;
    public GameObject hitImpactPrefab;

    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private Rigidbody rb;
    private float nextFireTime = 0f;
    private Vector3 initialModelLocalPos;
    private Quaternion initialModelLocalRot;
    private Vector3 currentRecoilPos;
    private Vector3 currentRecoilRot;
    private Vector3 initialBoltLocalPos;
    private bool boltChambered = true;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (modelRoot != null)
        {
            initialModelLocalPos = modelRoot.localPosition;
            initialModelLocalRot = modelRoot.localRotation;
        }

        if (boltTransform != null)
        {
            initialBoltLocalPos = boltTransform.localPosition;
        }

        if (attachPoint != null)
        {
            grabInteractable.attachTransform = attachPoint;
        }

        currentAmmo = magazineCapacity;
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.AddListener(OnTriggerPulled);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.RemoveListener(OnTriggerPulled);
        }
    }

    private void Update()
    {
        // Recuperación suave de retroceso procedural (Estilo Bodycam)
        if (modelRoot != null)
        {
            currentRecoilPos = Vector3.Lerp(currentRecoilPos, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
            currentRecoilRot = Vector3.Lerp(currentRecoilRot, Vector3.zero, Time.deltaTime * recoilReturnSpeed);

            modelRoot.localPosition = initialModelLocalPos + currentRecoilPos;
            modelRoot.localRotation = initialModelLocalRot * Quaternion.Euler(currentRecoilRot);
        }

        // Detección de botón de recarga (Botón secundario / 'B' o 'Y' en mandos VR o tecla R en teclado)
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                TryReload();
            }
        }
    }

    private void OnTriggerPulled(ActivateEventArgs args)
    {
        TryFire();
    }

    public void TryFire()
    {
        if (isReloading) return;
        if (Time.time < nextFireTime) return;

        if (!infiniteAmmo && currentAmmo <= 0)
        {
            PlaySound(emptyAudioClip, 0.4f);
            nextFireTime = Time.time + 0.3f;
            return;
        }

        if (rifleType == RifleType.BoltAction && !boltChambered)
        {
            PlaySound(emptyAudioClip, 0.4f);
            return;
        }

        ExecuteFire();
        nextFireTime = Time.time + fireRate;
    }

    private void ExecuteFire()
    {
        if (!infiniteAmmo) currentAmmo--;

        // 1. Sonido de disparo (Procedural o AudioClip)
        PlayGunshotAudio();

        // 2. Muzzle Flash
        if (muzzleFlash != null) muzzleFlash.Play();

        // 3. Feedback Háptico en el mando VR
        SendHaptics(fireHapticIntensity, fireHapticDuration);

        // 4. Retroceso Procedural Inmersivo (Bodycam recoil)
        ApplyProceduralRecoil();

        // 5. Animación de retroceso del cerrojo
        if (boltTransform != null)
        {
            StartCoroutine(CycleBoltRoutine());
        }

        // 6. Expulsión de casquillo vacío
        EjectShellCasing();

        // 7. Raycast de balística de alta precisión
        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
        Vector3 direction = muzzlePoint != null ? muzzlePoint.forward : transform.forward;

        // Si existen miras de hierro, apuntar exactamente a través de ellas
        if (rearSight != null && frontSight != null)
        {
            direction = (frontSight.position - rearSight.position).normalized;
        }

        Vector3 hitPoint = origin + (direction * maxRange);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;

            // Detectar impacto en Diana
            ShootingTarget target = hit.collider.GetComponentInParent<ShootingTarget>();
            if (target != null)
            {
                float distToCenter = Vector3.Distance(hit.point, target.transform.position);
                bool isBullseye = distToCenter < 0.25f;
                target.Hit(hit.point, isBullseye);
            }

            // Física de impacto
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
            {
                hit.rigidbody.AddForceAtPosition(direction * bulletImpactForce, hit.point, ForceMode.Impulse);
            }

            // Efecto de impacto
            if (hitImpactPrefab != null)
            {
                Instantiate(hitImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        // 8. Trazador de bala
        if (tracerLineRenderer != null)
        {
            StartCoroutine(ShowTracerRoutine(origin, hitPoint));
        }

        if (rifleType == RifleType.BoltAction)
        {
            boltChambered = false;
        }
    }

    private void ApplyProceduralRecoil()
    {
        // Impulso hacia atrás y arriba
        currentRecoilPos += new Vector3(0, 0, -recoilKickBack);
        float sideJitter = Random.Range(-recoilKickSide, recoilKickSide);
        currentRecoilRot += new Vector3(-recoilKickUp, sideJitter, -sideJitter * 0.5f);
    }

    private IEnumerator CycleBoltRoutine()
    {
        float elapsed = 0f;
        Vector3 backPos = initialBoltLocalPos + new Vector3(0, 0, -boltCycleDistance);

        // Retroceso del cerrojo
        while (elapsed < boltCycleDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            boltTransform.localPosition = Vector3.Lerp(initialBoltLocalPos, backPos, elapsed / (boltCycleDuration * 0.4f));
            yield return null;
        }

        // Avance del cerrojo
        elapsed = 0f;
        while (elapsed < boltCycleDuration * 0.6f)
        {
            elapsed += Time.deltaTime;
            boltTransform.localPosition = Vector3.Lerp(backPos, initialBoltLocalPos, elapsed / (boltCycleDuration * 0.6f));
            yield return null;
        }

        boltTransform.localPosition = initialBoltLocalPos;
    }

    private void EjectShellCasing()
    {
        if (shellEjectionPoint == null) return;

        // Crear casquillo procedural simple
        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shell.name = "Spent_Shell_22LR";
        shell.transform.position = shellEjectionPoint.position;
        shell.transform.rotation = Random.rotation;
        shell.transform.localScale = new Vector3(0.008f, 0.012f, 0.008f);

        Material matGold = shell.GetComponent<MeshRenderer>().material;
        matGold.color = new Color(0.9f, 0.75f, 0.2f);

        Rigidbody shellRb = shell.AddComponent<Rigidbody>();
        shellRb.mass = 0.01f;

        Vector3 ejectVelocity = shellEjectionPoint.right * Random.Range(1.8f, 2.5f) + shellEjectionPoint.up * Random.Range(1.0f, 1.8f);
        shellRb.linearVelocity = ejectVelocity;
        shellRb.angularVelocity = Random.insideUnitSphere * 20f;

        Destroy(shell, 3.5f);
    }

    public void TryReload()
    {
        if (isReloading || currentAmmo >= magazineCapacity) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        PlaySound(reloadAudioClip, 0.8f);
        SendHaptics(0.4f, 0.2f);

        yield return new WaitForSeconds(reloadDuration);

        currentAmmo = magazineCapacity;
        boltChambered = true;
        PlaySound(boltCycleAudioClip, 0.7f);
        SendHaptics(0.6f, 0.1f);
        isReloading = false;
    }

    private void SendHaptics(float intensity, float duration)
    {
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            foreach (var interactor in grabInteractable.interactorsSelecting)
            {
                if (interactor is XRBaseInputInteractor inputInteractor)
                {
                    inputInteractor.SendHapticImpulse(intensity, duration);
                }
            }
        }
    }

    private void PlayGunshotAudio()
    {
        if (fireAudioClip != null)
        {
            audioSource.PlayOneShot(fireAudioClip, 1.0f);
        }
        else
        {
            // Sonido sintetizado procedural para disparo de rifle .22 LR
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(GetOrCreateProceduralShotClip(), 0.9f);
        }
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private static AudioClip proceduralShotClip;
    private static AudioClip GetOrCreateProceduralShotClip()
    {
        if (proceduralShotClip != null) return proceduralShotClip;

        int sampleRate = 44100;
        int length = (int)(sampleRate * 0.22f);
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 18f);
            float thump = Mathf.Sin(t * 180f) * Mathf.Exp(-t * 22f);
            samples[i] = (noise * 0.7f + thump * 0.6f);
        }

        proceduralShotClip = AudioClip.Create("Procedural_Rifle_Shot", length, 1, sampleRate, false);
        proceduralShotClip.SetData(samples, 0);
        return proceduralShotClip;
    }

    private IEnumerator ShowTracerRoutine(Vector3 start, Vector3 end)
    {
        tracerLineRenderer.enabled = true;
        tracerLineRenderer.SetPosition(0, start);
        tracerLineRenderer.SetPosition(1, end);
        yield return new WaitForSeconds(tracerDuration);
        tracerLineRenderer.enabled = false;
    }
}

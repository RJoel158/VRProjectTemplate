using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum RifleType
{
    SemiAutomatic,  // Ruger 10/22 LR
    BoltAction      // Rifle de Cerrojo
}

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class VRRifle : MonoBehaviour
{
    [Header("Alineación y Orientación")]
    public Vector3 modelRotationOffset = new Vector3(0, -90f, 0);
    public Vector3 gripOffsetPosition = Vector3.zero;
    public Vector3 gripOffsetRotation = Vector3.zero;
    public bool toggleGrabMode = true;

    [Header("Tipo de Rifle")]
    public RifleType rifleType = RifleType.SemiAutomatic;

    [Header("Empties de Referencia")]
    public Transform muzzlePoint;
    public Transform frontSight;
    public Transform rearSight;
    public Transform attachPoint;
    public Transform shellEjectionPoint;
    public Transform modelRoot;

    [Header("Modo Bodycam / ADS")]
    public bool enableBodycamAim = true;
    public bool isAimingDownSights = false;
    public float bodycamEyeDistance = 0.28f;
    public float eyeOffsetRight = 0.035f;
    public float aimLerpSpeed = 16f;

    [Header("Balística y Disparo")]
    public float maxRange = 300f;
    public float fireRate = 0.18f;
    public float bulletImpactForce = 60f;
    public LayerMask hitLayers = ~0;

    [Header("Munición")]
    public int magazineCapacity = 10;
    public int currentAmmo = 10;
    public bool infiniteAmmo = true;
    public float reloadDuration = 1.5f;
    public bool isReloading = false;

    [Header("Retroceso Procedural (Bodycam)")]
    public float recoilKickBack = 0.035f;
    public float recoilKickUp = 3.5f;
    public float recoilKickSide = 0.6f;
    public float recoilReturnSpeed = 18f;

    [Header("Materiales y Efectos")]
    public Material tracerMaterial;
    public Material shellMaterial;
    public ParticleSystem muzzleFlash;
    public LineRenderer tracerLineRenderer;
    public float tracerDuration = 0.04f;

    [Header("Feedback")]
    [Range(0f, 1f)] public float fireHapticIntensity = 0.85f;
    public float fireHapticDuration = 0.12f;
    public AudioClip fireAudioClip;
    public AudioClip reloadAudioClip;
    public AudioClip emptyAudioClip;

    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private Rigidbody rb;
    private Camera playerCam;
    private float nextFireTime = 0f;

    private Vector3 initialModelLocalPos;
    private Vector3 currentRecoilPos;
    private Vector3 currentRecoilRot;

    private XRBaseInteractor currentHoldingInteractor;
    private bool isHeld = false;
    private bool explicitDropRequested = false;

    private void OnValidate()
    {
        AutoLocateReferences();
        if (modelRoot != null && !Application.isPlaying)
        {
            modelRoot.localEulerAngles = modelRotationOffset;
        }
    }

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        playerCam = Camera.main;

        AutoLocateReferences();

        // Configuración óptima para agarre instantáneo y sólido en VR
        if (grabInteractable != null)
        {
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.useDynamicAttach = false;
            grabInteractable.matchAttachPosition = true;
            grabInteractable.matchAttachRotation = true;
            grabInteractable.throwOnDetach = true;
            if (attachPoint != null) grabInteractable.attachTransform = attachPoint;
        }

        if (modelRoot != null)
        {
            initialModelLocalPos = modelRoot.localPosition;
            modelRoot.localEulerAngles = modelRotationOffset;
        }

        if (tracerLineRenderer == null) tracerLineRenderer = GetComponent<LineRenderer>();
        if (tracerLineRenderer != null && tracerMaterial != null)
        {
            tracerLineRenderer.sharedMaterial = tracerMaterial;
        }

        currentAmmo = magazineCapacity;
    }

    public void AutoLocateReferences()
    {
        if (modelRoot == null)
        {
            Transform found = transform.Find("Model_Root") ?? transform.Find("Rifle_Mesh_Model");
            if (found != null) modelRoot = found;
            else
            {
                MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
                if (mr != null) modelRoot = mr.transform;
            }
        }

        Transform searchRoot = modelRoot != null ? modelRoot : transform;

        if (muzzlePoint == null)
            muzzlePoint = searchRoot.Find("MuzzlePoint") ?? transform.Find("MuzzlePoint");

        if (shellEjectionPoint == null)
            shellEjectionPoint = searchRoot.Find("Shell_Ejection_Point") ?? transform.Find("Shell_Ejection_Point");

        if (frontSight == null)
            frontSight = searchRoot.Find("FrontSight") ?? transform.Find("FrontSight");

        if (rearSight == null)
            rearSight = searchRoot.Find("RearSight") ?? transform.Find("RearSight");

        if (attachPoint == null)
            attachPoint = transform.Find("AttachPoint_Grip") ?? searchRoot.Find("AttachPoint_Grip");

#if UNITY_EDITOR
        if (tracerMaterial == null)
            tracerMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Mat_Bullet_Tracer.mat");
        if (shellMaterial == null)
            shellMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Mat_Bullet_Shell.mat");
#endif
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.AddListener(OnTriggerPulled);
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.RemoveListener(OnTriggerPulled);
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
        explicitDropRequested = false;
        if (args.interactorObject is XRBaseInteractor baseInteractor)
        {
            currentHoldingInteractor = baseInteractor;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!toggleGrabMode || explicitDropRequested)
        {
            isHeld = false;
            currentHoldingInteractor = null;
            isAimingDownSights = false;

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    private void Update()
    {
        if (playerCam == null) playerCam = Camera.main;

        // Recuperación suave de retroceso procedural
        if (modelRoot != null)
        {
            currentRecoilPos = Vector3.Lerp(currentRecoilPos, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
            currentRecoilRot = Vector3.Lerp(currentRecoilRot, Vector3.zero, Time.deltaTime * recoilReturnSpeed);

            modelRoot.localPosition = initialModelLocalPos + currentRecoilPos;
            modelRoot.localRotation = Quaternion.Euler(modelRotationOffset) * Quaternion.Euler(currentRecoilRot);
        }

        // Comprobaciones cuando el rifle está sostenido
        if (isHeld || (grabInteractable != null && grabInteractable.isSelected))
        {
            HandleInputs();

            // Si está en modo Toggle y el interactor se soltó en el simulador, mantener posición fija en la mano
            if (isHeld && currentHoldingInteractor != null && (grabInteractable != null && !grabInteractable.isSelected))
            {
                Transform handTransform = currentHoldingInteractor.GetAttachTransform(grabInteractable);
                if (handTransform == null) handTransform = currentHoldingInteractor.transform;

                if (!isAimingDownSights)
                {
                    if (attachPoint != null)
                    {
                        Quaternion rotDiff = handTransform.rotation * Quaternion.Inverse(attachPoint.rotation);
                        transform.rotation = rotDiff * transform.rotation;
                        transform.position += (handTransform.position - attachPoint.position);
                    }
                    else
                    {
                        transform.position = handTransform.position;
                        transform.rotation = handTransform.rotation;
                    }
                }
            }

            // Modo Bodycam ADS
            if (enableBodycamAim && isAimingDownSights && playerCam != null)
            {
                ApplyBodycamAimAlignment();
            }
        }
    }

    public void DropWeapon()
    {
        explicitDropRequested = true;
        isHeld = false;
        currentHoldingInteractor = null;
        isAimingDownSights = false;

        if (grabInteractable != null && grabInteractable.isSelected)
        {
            var interactor = grabInteractable.firstInteractorSelecting;
            if (interactor != null && grabInteractable.interactionManager != null)
            {
                grabInteractable.interactionManager.SelectExit(interactor, grabInteractable);
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    private void HandleInputs()
    {
        // Tecla 'G' o 'E' para soltar en modo Toggle
        if (Keyboard.current != null && (Keyboard.current.gKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
        {
            DropWeapon();
            return;
        }

        // Tecla 'R' para recargar
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryReload();
        }

        // Teclas 'F', 'Z', 'Space' o Clic Derecho para alternar modo Bodycam ADS
        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame || 
                Keyboard.current.zKey.wasPressedThisFrame || 
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isAimingDownSights = !isAimingDownSights;
            }
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            isAimingDownSights = !isAimingDownSights;
        }

        // Clic izquierdo o tecla 'T' dispara si está sostenido
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryFire();
        }
        else if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            TryFire();
        }
    }

    private void ApplyBodycamAimAlignment()
    {
        if (frontSight == null || rearSight == null || playerCam == null) return;

        Vector3 eyePos = playerCam.transform.position + (playerCam.transform.right * eyeOffsetRight) + (playerCam.transform.forward * bodycamEyeDistance);
        Vector3 aimDirection = playerCam.transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(aimDirection, playerCam.transform.up);
        Vector3 sightOffsetInRifle = rearSight.position - transform.position;
        Vector3 targetPosition = eyePos - sightOffsetInRifle;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * aimLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * aimLerpSpeed);
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

        ExecuteFire();
        nextFireTime = Time.time + fireRate;
    }

    private void ExecuteFire()
    {
        if (!infiniteAmmo) currentAmmo--;

        PlayGunshotAudio();

        if (muzzleFlash != null) muzzleFlash.Play();

        SendHaptics(fireHapticIntensity, fireHapticDuration);
        ApplyProceduralRecoil();
        EjectShellCasing();

        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
        Vector3 direction = muzzlePoint != null ? muzzlePoint.forward : transform.forward;

        if (rearSight != null && frontSight != null)
        {
            direction = (frontSight.position - rearSight.position).normalized;
        }

        Vector3 hitPoint = origin + (direction * maxRange);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;

            ShootingTarget target = hit.collider.GetComponentInParent<ShootingTarget>();
            if (target != null)
            {
                float distToCenter = Vector3.Distance(hit.point, target.transform.position);
                bool isBullseye = distToCenter < 0.25f;
                target.Hit(hit.point, isBullseye);
            }

            if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
            {
                hit.rigidbody.AddForceAtPosition(direction * bulletImpactForce, hit.point, ForceMode.Impulse);
            }
        }

        if (tracerLineRenderer != null)
        {
            StartCoroutine(ShowTracerRoutine(origin, hitPoint));
        }
    }

    private void ApplyProceduralRecoil()
    {
        currentRecoilPos += new Vector3(0, 0, -recoilKickBack);
        float sideJitter = Random.Range(-recoilKickSide, recoilKickSide);
        currentRecoilRot += new Vector3(-recoilKickUp, sideJitter, -sideJitter * 0.5f);
    }

    private void EjectShellCasing()
    {
        if (shellEjectionPoint == null) return;

        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shell.name = "Spent_Shell";
        shell.transform.position = shellEjectionPoint.position;
        shell.transform.rotation = Random.rotation;
        shell.transform.localScale = new Vector3(0.008f, 0.012f, 0.008f);

        MeshRenderer shellMr = shell.GetComponent<MeshRenderer>();
        if (shellMaterial != null)
        {
            shellMr.sharedMaterial = shellMaterial;
        }
        else if (shellMr != null && shellMr.material != null)
        {
            shellMr.material.color = new Color(0.95f, 0.78f, 0.32f);
        }

        Rigidbody shellRb = shell.AddComponent<Rigidbody>();
        shellRb.mass = 0.01f;

        Vector3 ejectVelocity = shellEjectionPoint.right * Random.Range(1.8f, 2.5f) + shellEjectionPoint.up * Random.Range(1.0f, 1.8f);
        shellRb.linearVelocity = ejectVelocity;
        shellRb.angularVelocity = Random.insideUnitSphere * 20f;

        Destroy(shell, 3.0f);
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
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(GetOrCreateProceduralShotClip(), 0.9f);
        }
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null) audioSource.PlayOneShot(clip, volume);
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
        if (tracerMaterial != null && tracerLineRenderer.sharedMaterial != tracerMaterial)
        {
            tracerLineRenderer.sharedMaterial = tracerMaterial;
        }

        tracerLineRenderer.enabled = true;
        tracerLineRenderer.SetPosition(0, start);
        tracerLineRenderer.SetPosition(1, end);
        yield return new WaitForSeconds(tracerDuration);
        tracerLineRenderer.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        // 🔴 Boca del cañón (Muzzle)
        if (muzzlePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(muzzlePoint.position, 0.02f);
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * 0.5f);
        }

        // 🟡 Ventana de expulsión (Ejection)
        if (shellEjectionPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(shellEjectionPoint.position, 0.015f);
            Gizmos.DrawRay(shellEjectionPoint.position, shellEjectionPoint.right * 0.2f);
        }

        // 🟢 Miras de hierro (Sights)
        if (frontSight != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(frontSight.position, 0.01f);
        }
        if (rearSight != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(rearSight.position, 0.01f);
        }
        if (frontSight != null && rearSight != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rearSight.position, frontSight.position);
        }

        // 🔵 Punto de agarre (Grip)
        if (attachPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attachPoint.position, 0.025f);
        }
    }
}

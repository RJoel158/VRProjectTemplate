using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;

/// <summary>
/// Control del rifle:
/// - En reposo: descansando en la mesa/stand.
/// - Con un click: se monta frente a la cámara con las miras alineadas listo para apuntar y disparar.
/// - Estando apuntando: cada click dispara.
/// - Click secundario (Click Derecho / Tecla G / Grip VR): lo devuelve a la mesa.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VRRifle : MonoBehaviour
{
    [Header("Modo de Apuntado (ADS)")]
    [Tooltip("Indica si el rifle está montado frente a la cámara apuntando")]
    public bool isAiming = false;

    [Tooltip("Posición local relativa a la cámara al apuntar")]
    public Vector3 aimLocalPosition = new Vector3(0f, -0.045f, 0.32f);

    [Tooltip("Rotación local relativa a la cámara al apuntar")]
    public Vector3 aimLocalRotation = new Vector3(180f, 90f, 180f);

    [Header("Referencias de Miras y Cañón")]
    public Transform muzzlePoint;
    public Transform frontSight;
    public Transform rearSight;

    [Header("Parámetros de Disparo")]
    public float maxRange          = 300f;
    public float fireRate          = 0.18f;
    public float bulletImpactForce = 60f;
    public LayerMask hitLayers     = ~0;

    [Header("Munición")]
    public int   magazineCapacity = 10;
    public int   currentAmmo      = 10;
    public bool  infiniteAmmo     = true;
    public float reloadDuration   = 1.5f;
    public bool  isReloading      = false;

    [Header("Animación de Avatar")]
    [Tooltip("Animator del avatar para disparar la animación de retroceso/disparo")]
    public Animator avatarAnimator;

    [Header("Efectos Visuales")]
    public ParticleSystem muzzleFlash;
    public LineRenderer   tracerLineRenderer;
    public Material       tracerMaterial;
    public float          tracerDuration = 0.04f;

    [Header("Audio")]
    public AudioClip fireAudioClip;
    public AudioClip reloadAudioClip;
    public AudioClip emptyAudioClip;

    [Header("Háptica VR")]
    [Range(0f, 1f)] public float fireHapticIntensity = 0.85f;
    public float fireHapticDuration = 0.12f;

    // ─────────────────────────────────────────────────────────────────────────
    // CAMPOS PRIVADOS
    // ─────────────────────────────────────────────────────────────────────────

    private AudioSource audioSource;
    private XRGrabInteractable grabInteractable;
    private Camera playerCam;

    private Vector3 restLocalPosition;
    private Quaternion restLocalRotation;
    private Transform restParent;

    private float nextFireTime = 0f;
    private bool wasVRTriggerDownLastFrame = false;
    private bool wasVRGripDownLastFrame = false;

    // ─────────────────────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                bc.center = mf.sharedMesh.bounds.center;
                bc.size   = mf.sharedMesh.bounds.size;
            }
            else
            {
                bc.size = new Vector3(0.012f, 0.02f, 0.088f);
            }
            col = bc;
        }

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.throwOnDetach = false;
            if (grabInteractable.colliders.Count == 0 && col != null)
                grabInteractable.colliders.Add(col);

            // Al interactuar con rayo o mano en VR, montar el rifle para apuntar
            grabInteractable.selectEntered.AddListener((args) => SetAimPosition());
            grabInteractable.activated.AddListener((args) => OnActionTriggered());
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.8f;

        if (muzzlePoint == null)
        {
            Transform found = transform.Find("MuzzlePoint");
            if (found != null) muzzlePoint = found;
        }

        if (tracerLineRenderer == null) tracerLineRenderer = GetComponent<LineRenderer>();
        if (tracerLineRenderer != null && tracerMaterial != null)
            tracerLineRenderer.sharedMaterial = tracerMaterial;

        currentAmmo = magazineCapacity;
    }

    private void Start()
    {
        GetPlayerCamera();

        // Si ya estaba puesto como hijo de la cámara, guardar esos valores como la posición de apuntado
        if (transform.parent != null && transform.parent.name.Contains("Camera"))
        {
            isAiming = true;
            aimLocalPosition = transform.localPosition;
            aimLocalRotation = transform.localEulerAngles;
        }
        else
        {
            // Guardar posición de reposo en la mesa
            restParent        = transform.parent;
            restLocalPosition = transform.localPosition;
            restLocalRotation = transform.localRotation;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LOOP DE ENTRADAS
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        bool vrTrigger  = ReadVRTriggerDown();
        bool mouseShoot = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        bool keyShoot   = (Keyboard.current != null && (Keyboard.current.tKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame));

        // Click principal: si no está apuntando -> montarlo con miras alineadas; si ya está apuntando -> disparar
        if (vrTrigger || mouseShoot || keyShoot)
        {
            OnActionTriggered();
        }

        // Click secundario (Click Derecho / Tecla G / Grip VR): devolver a la mesa o alternar
        bool vrGrip      = ReadVRGripDown();
        bool mouseCancel = (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
        bool keyCancel   = (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame);

        if (vrGrip || mouseCancel || keyCancel)
        {
            if (isAiming)
                ReturnToRestPosition();
            else
                SetAimPosition();
        }

        // Recarga con tecla R
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryReload();
        }
    }

    private void OnActionTriggered()
    {
        if (!isAiming)
        {
            SetAimPosition();
        }
        else
        {
            TryFire();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SETEAR MIRA ALINEADA FRENTE A LA CÁMARA
    // ─────────────────────────────────────────────────────────────────────────

    public void SetAimPosition()
    {
        Camera cam = GetPlayerCamera();
        if (cam == null) return;

        isAiming = true;

        // Anclar como hijo de la cámara con las miras perfectamente alineadas
        transform.SetParent(cam.transform, false);
        transform.localPosition = aimLocalPosition;
        transform.localRotation = Quaternion.Euler(aimLocalRotation);

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        PlaySound(reloadAudioClip, 0.4f);
        Debug.Log("[VRRifle] ✅ Arma seteada con las miras alineadas lista para apuntar/disparar.");
    }

    public void ReturnToRestPosition()
    {
        if (!isAiming) return;
        isAiming = false;

        transform.SetParent(restParent, false);
        transform.localPosition = restLocalPosition;
        transform.localRotation = restLocalRotation;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Debug.Log("[VRRifle] ↩ Arma devuelta a su posición de reposo en el stand.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DETECCIÓN DE CONTROLES VR
    // ─────────────────────────────────────────────────────────────────────────

    private bool ReadVRTriggerDown()
    {
        bool isDownNow = false;

        var rightDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);
        foreach (var dev in rightDevices)
        {
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool btn) && btn) isDownNow = true;
            else if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float val) && val > 0.5f) isDownNow = true;
        }

        if (!isDownNow)
        {
            var leftDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftDevices);
            foreach (var dev in leftDevices)
            {
                if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool btn) && btn) isDownNow = true;
                else if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float val) && val > 0.5f) isDownNow = true;
            }
        }

        bool wasPressedThisFrame = isDownNow && !wasVRTriggerDownLastFrame;
        wasVRTriggerDownLastFrame = isDownNow;
        return wasPressedThisFrame;
    }

    private bool ReadVRGripDown()
    {
        bool isDownNow = false;

        var rightDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);
        foreach (var dev in rightDevices)
        {
            if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool btn) && btn) isDownNow = true;
            else if (dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float val) && val > 0.5f) isDownNow = true;
        }

        bool wasPressedThisFrame = isDownNow && !wasVRGripDownLastFrame;
        wasVRGripDownLastFrame = isDownNow;
        return wasPressedThisFrame;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LÓGICA DE DISPARO
    // ─────────────────────────────────────────────────────────────────────────

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

        if (ShootingRangeManager.Instance != null)
            ShootingRangeManager.Instance.RegisterShot();
    }

    private void ExecuteFire()
    {
        if (!infiniteAmmo) currentAmmo--;

        PlayGunshotAudio();
        if (muzzleFlash != null) muzzleFlash.Play();
        if (avatarAnimator != null) avatarAnimator.SetTrigger("Shoot");
        SendHaptics(fireHapticIntensity, fireHapticDuration);

        // Al disparar apuntando, la bala va exactamente hacia donde miras
        Camera cam = GetPlayerCamera();
        Vector3 origin    = (muzzlePoint != null) ? muzzlePoint.position : transform.position;
        Vector3 direction = (isAiming && cam != null) ? cam.transform.forward : ((muzzlePoint != null) ? muzzlePoint.forward : transform.forward);
        Vector3 hitPoint  = origin + direction * maxRange;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;

            ShootingTarget target = hit.collider.GetComponentInParent<ShootingTarget>();
            if (target != null)
            {
                float dist = Vector3.Distance(hit.point, target.transform.position);
                target.Hit(hit.point, dist < 0.25f);
            }

            if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                hit.rigidbody.AddForceAtPosition(direction * bulletImpactForce, hit.point, ForceMode.Impulse);
        }

        if (tracerLineRenderer != null)
            StartCoroutine(ShowTracerRoutine(origin, hitPoint));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RECARGA
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // AUDIO Y EFECTOS
    // ─────────────────────────────────────────────────────────────────────────

    private void PlayGunshotAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (fireAudioClip != null)
        {
            audioSource.PlayOneShot(fireAudioClip, 1f);
            return;
        }

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(GetOrCreateProceduralShotClip(), 0.9f);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.PlayOneShot(clip, volume);
    }

    private static AudioClip proceduralShotClip;
    private static AudioClip GetOrCreateProceduralShotClip()
    {
        if (proceduralShotClip != null) return proceduralShotClip;

        int     sampleRate = 44100;
        int     length     = (int)(sampleRate * 0.22f);
        float[] samples    = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            samples[i] = (Random.value * 2f - 1f) * Mathf.Exp(-t * 18f) * 0.7f
                       + Mathf.Sin(t * 180f) * Mathf.Exp(-t * 22f) * 0.6f;
        }

        proceduralShotClip = AudioClip.Create("Procedural_Rifle_Shot", length, 1, sampleRate, false);
        proceduralShotClip.SetData(samples, 0);
        return proceduralShotClip;
    }

    private IEnumerator ShowTracerRoutine(Vector3 start, Vector3 end)
    {
        if (tracerLineRenderer == null) yield break;
        if (tracerMaterial != null && tracerLineRenderer.sharedMaterial != tracerMaterial)
            tracerLineRenderer.sharedMaterial = tracerMaterial;
        tracerLineRenderer.enabled = true;
        tracerLineRenderer.SetPosition(0, start);
        tracerLineRenderer.SetPosition(1, end);
        yield return new WaitForSeconds(tracerDuration);
        tracerLineRenderer.enabled = false;
    }

    private void SendHaptics(float intensity, float duration)
    {
        if (grabInteractable == null || !grabInteractable.isSelected) return;
        foreach (var interactor in grabInteractable.interactorsSelecting)
        {
            if (interactor is XRBaseInputInteractor ii)
                ii.SendHapticImpulse(intensity, duration);
        }
    }

    public Camera GetPlayerCamera()
    {
        if (playerCam != null && playerCam.gameObject.activeInHierarchy)
            return playerCam;

        playerCam = Camera.main;
        if (playerCam != null) return playerCam;

        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            playerCam = xrOrigin.Camera;
            return playerCam;
        }

        playerCam = FindAnyObjectByType<Camera>();
        return playerCam;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowlingPinManager : MonoBehaviour
{
    public static BowlingPinManager Instance { get; private set; }

    [Header("Pins")]
    public List<PinFallDetector> pins = new List<PinFallDetector>();

    [Header("Bowling Ball")]
    public Transform bowlingBall;

    [Header("Score Events")]
    public ScoreEventData pinKnockedEvent;
    public ScoreEventData bonusEvent;
    public int strikeExtraPoints = 5;

    [Header("Settings")]
    public float settleCheckDelay = 2.5f;

    [Header("UI")]
    public TMP_Text throwText;

    [Header("Ball Settle Detection (for misses/gutter balls)")]
    [Tooltip("How slow the ball must be moving to count as 'settled'.")]
    public float ballSettleVelocityThreshold = 0.1f;
    [Tooltip("How many seconds the ball must stay settled before we resolve the throw, even if no pins fell.")]
    public float ballSettleTimeBeforeResolve = 2f;
    [Tooltip("How far the ball must move from its spawn point before we start watching it (avoids resolving instantly while it's just sitting at the rack).")]
    public float ballLeftSpawnDistance = 0.3f;

    [Header("Pin Sounds")]
    [Tooltip("AudioSource used to play pin sounds. If left empty, one will be added automatically.")]
    public AudioSource audioSource;
    [Tooltip("Played when exactly ONE pin falls within a short burst window.")]
    public AudioClip singlePinSound;
    [Tooltip("Played when MORE THAN ONE pin falls within the same short burst window (falling together).")]
    public AudioClip multiplePinsSound;
    [Tooltip("Seconds used to group pins that fall almost simultaneously into one 'burst' for sound purposes. Pins falling further apart than this each get their own sound.")]
    public float pinSoundBurstWindow = 0.4f;

    [Header("Floating Icons (Strike/Spare)")]
    [Tooltip("Prefab with SpriteRenderer + FloatingIconEffect shown when the player gets a Strike.")]
    public GameObject strikeIconPrefab;
    [Tooltip("Prefab with SpriteRenderer + FloatingIconEffect shown when the player gets a Spare.")]
    public GameObject spareIconPrefab;
    [Tooltip("Optional: an empty Transform placed at the center of the pin triangle. If left empty, the center is calculated automatically from the pins' positions.")]
    public Transform pinsCenterPoint;

    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();

    private List<PinFallDetector> pinsFallenThisThrow = new List<PinFallDetector>();

    private Vector3 originalBallPosition;
    private Quaternion originalBallRotation;

    private int throwNumberInFrame = 1;

    private bool waitingForSettle = false;
    private bool resolvingThrow = false;

    private Rigidbody ballRigidbody;
    private XRGrabInteractable ballGrabInteractable;
    private bool hasBeenPickedUpThisRound = false;
    private bool ballInPlay = false;
    private float ballSettledTimer = 0f;

    private List<PinFallDetector> pinsFallenInBurst = new List<PinFallDetector>();
    private bool burstTimerActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        foreach (var pin in pins)
        {
            originalPositions.Add(pin.transform.position);
            originalRotations.Add(pin.transform.rotation);
        }

        originalBallPosition = bowlingBall.position;
        originalBallRotation = bowlingBall.rotation;

        ballRigidbody = bowlingBall.GetComponent<Rigidbody>();
        ballGrabInteractable = bowlingBall.GetComponent<XRGrabInteractable>();

        if (ballGrabInteractable != null)
        {
            ballGrabInteractable.selectEntered.AddListener(OnBallPickedUp);
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ResetBall();

        UpdateThrowText();
    }

    private void Update()
    {
        CheckBallSettled();
    }

    public void NotifyPinFell(PinFallDetector pin)
    {
        // --- Sound burst tracking (independent of scoring/settle logic) ---
        // Groups pins that fall almost at the same instant into one "burst",
        // so a true simultaneous knockdown sounds different from pins that
        // topple one by one with a noticeable pause in between.
        if (!pinsFallenInBurst.Contains(pin))
        {
            pinsFallenInBurst.Add(pin);
        }
        if (!burstTimerActive)
        {
            burstTimerActive = true;
            Invoke(nameof(ResolvePinSoundBurst), pinSoundBurstWindow);
        }

        // --- Scoring / throw resolution logic (unchanged) ---
        if (resolvingThrow)
        {
            return;
        }

        if (!pinsFallenThisThrow.Contains(pin))
        {
            pinsFallenThisThrow.Add(pin);
        }

        if (waitingForSettle)
        {
            return;
        }

        waitingForSettle = true;

        Invoke(nameof(ResolveThrow), settleCheckDelay);
    }

    // Runs a short moment after the first pin in a "burst" falls, giving any
    // other pins that topple almost simultaneously time to join the same
    // burst before we decide which sound to play.
    private void ResolvePinSoundBurst()
    {
        burstTimerActive = false;
        int count = pinsFallenInBurst.Count;
        PlayPinSound(count);
        pinsFallenInBurst.Clear();
    }

    private void ResolveThrow()
    {
        if (resolvingThrow)
        {
            return;
        }

        resolvingThrow = true;
        waitingForSettle = false;

        int fallenThisThrow = pinsFallenThisThrow.Count;

        for (int i = 0; i < fallenThisThrow; i++)
        {
            ScoreManager.Instance.RegisterScoreEvent(pinKnockedEvent);
        }

        int totalStandingPins = CountStandingPins();

        if (throwNumberInFrame == 1)
        {
            if (totalStandingPins == 0)
            {
                ScoreManager.Instance.RegisterScoreEvent(bonusEvent);
                ScoreManager.Instance.AddPoints(strikeExtraPoints);

                SpawnCenterIcon(strikeIconPrefab);

                StartNewFrame();
            }
            else
            {
                foreach (var pin in pinsFallenThisThrow)
                {
                    pin.HidePin();
                }

                ResetBall();

                throwNumberInFrame = 2;

                UpdateThrowText();

                pinsFallenThisThrow.Clear();
            }
        }
        else
        {
            if (totalStandingPins == 0)
            {
                ScoreManager.Instance.RegisterScoreEvent(bonusEvent);

                SpawnCenterIcon(spareIconPrefab);
            }

            StartNewFrame();
        }

        resolvingThrow = false;
    }

    private void StartNewFrame()
    {
        CancelInvoke(nameof(ResolveThrow));

        waitingForSettle = false;
        resolvingThrow = false;

        throwNumberInFrame = 1;

        pinsFallenThisThrow.Clear();

        ResetAllPins();
        ResetBall();

        UpdateThrowText();
    }

    private void ResetAllPins()
    {
        for (int i = 0; i < pins.Count; i++)
        {
            pins[i].ResetPin(
                originalPositions[i],
                originalRotations[i]
            );
        }
    }

    private void ResetBall()
    {
        if (bowlingBall == null)
        {
            return;
        }

        Rigidbody rb = bowlingBall.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        bowlingBall.position = originalBallPosition;
        bowlingBall.rotation = originalBallRotation;

        ballInPlay = false;
        ballSettledTimer = 0f;
        hasBeenPickedUpThisRound = false;
    }

    private void PlayPinSound(int pinsFallen)
    {
        if (audioSource == null || pinsFallen == 0) return;

        if (pinsFallen == 1 && singlePinSound != null)
        {
            audioSource.PlayOneShot(singlePinSound);
        }
        else if (pinsFallen > 1 && multiplePinsSound != null)
        {
            audioSource.PlayOneShot(multiplePinsSound);
        }
    }

    private void OnBallPickedUp(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        hasBeenPickedUpThisRound = true;
    }

    private void CheckBallSettled()
    {
        if (bowlingBall == null || ballRigidbody == null) return;

        // Ignore any physics jitter (e.g. the ball settling into the rack)
        // until the player has actually grabbed it at least once this round.
        if (!hasBeenPickedUpThisRound) return;

        // Detect when the ball has actually left its spawn point (been thrown),
        // so we don't instantly "resolve" while it's just sitting at the rack.
        if (!ballInPlay)
        {
            float distanceFromSpawn = Vector3.Distance(bowlingBall.position, originalBallPosition);
            if (distanceFromSpawn >= ballLeftSpawnDistance)
            {
                ballInPlay = true;
                ballSettledTimer = 0f;
            }
            return;
        }

        // If a pin already started resolving the throw, let that flow handle it
        // instead of double-triggering here.
        if (waitingForSettle || resolvingThrow) return;

        bool isSettled = ballRigidbody.linearVelocity.magnitude < ballSettleVelocityThreshold &&
                          ballRigidbody.angularVelocity.magnitude < ballSettleVelocityThreshold;

        if (isSettled)
        {
            ballSettledTimer += Time.deltaTime;

            if (ballSettledTimer >= ballSettleTimeBeforeResolve)
            {
                ballInPlay = false;
                ballSettledTimer = 0f;
                ResolveThrow();
            }
        }
        else
        {
            ballSettledTimer = 0f;
        }
    }

    private int CountStandingPins()
    {
        int standing = 0;

        foreach (var pin in pins)
        {
            if (!pin.HasFallen)
            {
                standing++;
            }
        }

        return standing;
    }

    private void UpdateThrowText()
    {
        if (throwText != null)
        {
            throwText.text = "Tirada: " + throwNumberInFrame + "/2";
        }
    }

    private void SpawnCenterIcon(GameObject iconPrefab)
    {
        if (iconPrefab == null) return;
        Instantiate(iconPrefab, GetPinsCenter(), Quaternion.identity);
    }

    // Uses the manually assigned pinsCenterPoint if available, otherwise
    // calculates the average position of all 10 pins automatically.
    private Vector3 GetPinsCenter()
    {
        if (pinsCenterPoint != null)
        {
            return pinsCenterPoint.position;
        }

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < originalPositions.Count; i++)
        {
            sum += originalPositions[i];
        }
        return originalPositions.Count > 0 ? sum / originalPositions.Count : transform.position;
    }
}
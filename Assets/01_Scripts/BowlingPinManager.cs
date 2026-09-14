using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();

    private List<PinFallDetector> pinsFallenThisThrow = new List<PinFallDetector>();

    private Vector3 originalBallPosition;
    private Quaternion originalBallRotation;

    private int throwNumberInFrame = 1;

    private bool waitingForSettle = false;
    private bool resolvingThrow = false;

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

        ResetBall();

        UpdateThrowText();
    }

    public void NotifyPinFell(PinFallDetector pin)
    {
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
}
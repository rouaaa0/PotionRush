using System;
using UnityEngine;
using UnityEngine.InputSystem;

// The single place that reads tilt, shake and tap.
// Keyboard and mouse work as a fallback so the game can be tested on a computer:
// WASD / arrow keys = tilt, Space = shake, mouse click = tap.
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    // x: left/right, y: up/forward. Values go from -1 to 1.
    public Vector2 Move { get; private set; }

    public event Action Shaken;
    public event Action<Vector2> Tapped; // screen position of the tap

    [Header("Tilt")]
    [SerializeField] float tiltDeadZone = 0.1f;
    [SerializeField] float tiltRange = 0.5f;    // how far to tilt (in g) for full speed
    [SerializeField] bool invertTilt = false;   // flip if the phone moves the wrong way
    [SerializeField] bool invertForwardBack = false; // flip only forward / backward

    [Header("Shake")]
    [SerializeField] float shakeThreshold = 2.2f; // in g
    [SerializeField] float shakeCooldown = 0.25f;

    float lastShakeTime = -10f;
    bool calibrated;
    float neutralPitch;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // the game is made for landscape: allow both landscape sides, never portrait
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    void OnEnable()
    {
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    void Update()
    {
        Move = ReadMove();
        CheckShake();
        CheckTap();
    }

    Vector2 ReadMove()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            Vector2 v = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            if (v != Vector2.zero) return v.normalized;
        }

        if (Accelerometer.current != null)
        {
            Vector3 a = Accelerometer.current.acceleration.ReadValue();

            // The player may hold the phone upright or flat, so forward/back is the pitch angle
            // compared with the pose held when the stage started (that pose = standing still).
            // x is the left/right tilt. It is flipped because on the Galaxy A30 it came out reversed.
            if (!calibrated && a.sqrMagnitude > 0.25f)
            {
                neutralPitch = Pitch(a);
                calibrated = true;
            }
            float forward = calibrated ? Mathf.Sin(Pitch(a) - neutralPitch) : 0f;
            Vector2 tilt = new Vector2(-a.x, forward);
            if (invertTilt) tilt = -tilt;
            if (invertForwardBack) tilt.y = -tilt.y;
            tilt /= tiltRange;
            if (tilt.magnitude < tiltDeadZone) return Vector2.zero;
            return Vector2.ClampMagnitude(tilt, 1f);
        }

        return Vector2.zero;
    }

    // 0 when the phone stands upright, 90 degrees (in radians) when it lies flat, screen up.
    // Tilting the top edge away from the player makes the angle bigger.
    static float Pitch(Vector3 a)
    {
        return Mathf.Atan2(-a.z, -a.y);
    }

    void CheckShake()
    {
        bool shaken = false;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) shaken = true;

        if (Accelerometer.current != null &&
            Accelerometer.current.acceleration.ReadValue().magnitude > shakeThreshold)
            shaken = true;

        if (shaken && Time.time - lastShakeTime > shakeCooldown)
        {
            lastShakeTime = Time.time;
            Shaken?.Invoke();
        }
    }

    void CheckTap()
    {
        Pointer pointer = Pointer.current; // mouse on a computer, finger on a phone
        if (pointer != null && pointer.press.wasPressedThisFrame)
            Tapped?.Invoke(pointer.position.ReadValue());
    }
}

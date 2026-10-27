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

    [Header("Shake")]
    [SerializeField] float shakeThreshold = 2.2f; // in g
    [SerializeField] float shakeCooldown = 0.25f;

    float lastShakeTime = -10f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
            // The game is played in landscape, so the device axes are swapped.
            Vector2 tilt = new Vector2(-a.y, a.x);
            if (invertTilt) tilt = -tilt;
            tilt /= tiltRange;
            if (tilt.magnitude < tiltDeadZone) return Vector2.zero;
            return Vector2.ClampMagnitude(tilt, 1f);
        }

        return Vector2.zero;
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

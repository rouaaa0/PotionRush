using UnityEngine;

// Goes on the camera in the Dragon's Lair. It shakes the view a little when something big happens.
// It runs after CameraFollow, so the shared camera script stays untouched.
[DefaultExecutionOrder(100)]
public class ScreenRumble : MonoBehaviour
{
    static float strength;

    public static void Shake(float amount)
    {
        strength = Mathf.Max(strength, amount);
    }

    void OnEnable()
    {
        strength = 0f;
    }

    void LateUpdate()
    {
        if (strength <= 0f) return;

        transform.position += Random.insideUnitSphere * strength;
        strength = Mathf.MoveTowards(strength, 0f, Time.deltaTime * 0.9f);
    }
}

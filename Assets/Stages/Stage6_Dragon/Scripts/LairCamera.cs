using UnityEngine;

// The camera of the Dragon's Lair. The shared CameraFollow stays close to the wizard, but in a boss
// fight the player must always see the dragon, so this one shows the whole room and only drifts a
// little towards the wizard.
public class LairCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 position = new Vector3(0f, 16.5f, 13.5f);
    public Vector3 lookAt = new Vector3(0f, 0.5f, -0.8f);
    public float drift = 0.22f;      // 0 = the camera never moves, 1 = it follows the wizard completely
    public float smoothing = 3f;

    Vector3 current;

    void Start()
    {
        current = Wanted();
    }

    Vector3 Wanted()
    {
        if (target == null) return Vector3.zero;
        Vector3 t = target.position;
        return new Vector3(t.x, 0f, t.z) * drift;
    }

    void LateUpdate()
    {
        current = Vector3.Lerp(current, Wanted(), 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        transform.position = position + current;
        transform.LookAt(lookAt + current);
    }
}

using UnityEngine;

// Keeps the camera at a fixed angle above and behind the player.
public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [SerializeField] Vector3 offset = new Vector3(0f, 10f, 8.2f);
    [SerializeField] float smoothing = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, k);
        transform.LookAt(target.position + Vector3.up * 0.8f);
    }
}

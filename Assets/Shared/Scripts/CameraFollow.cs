using UnityEngine;

// Keeps the camera at a fixed angle above and behind the player,
// and moves in closer while the player is brewing at the cauldron.
public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [SerializeField] Vector3 offset = new Vector3(0f, 10f, 8.2f);
    [SerializeField] float smoothing = 8f;
    [SerializeField] float brewZoom = 0.75f; // 1 = normal distance, smaller = closer

    float zoom = 1f;

    void LateUpdate()
    {
        if (target == null) return;

        StageManager stage = StageManager.Instance;
        float zoomTarget = stage != null && (stage.Brewing || stage.State == StageState.Won) ? brewZoom : 1f;
        zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-2f * Time.deltaTime));

        Vector3 desired = target.position + offset * zoom;
        float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, k);
        transform.LookAt(target.position + Vector3.up * 0.8f);
    }
}

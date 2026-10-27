using UnityEngine;

// Slowly circles the camera around the lab behind the main menu.
public class MenuCameraOrbit : MonoBehaviour
{
    [SerializeField] Vector3 center = new Vector3(0f, 1f, 0f);
    [SerializeField] float radius = 15f;
    [SerializeField] float height = 8f;
    [SerializeField] float degreesPerSecond = 8f;

    float angle = 20f;

    void Update()
    {
        angle += degreesPerSecond * Time.deltaTime;
        float rad = angle * Mathf.Deg2Rad;
        transform.position = center + new Vector3(Mathf.Sin(rad) * radius, height, Mathf.Cos(rad) * radius);
        transform.LookAt(center);
    }
}

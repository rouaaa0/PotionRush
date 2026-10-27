using UnityEngine;

// Throws an object into the air so it spins, falls and lands on the floor.
// Used for the wizard's hat when the cauldron explodes.
public class Tumble : MonoBehaviour
{
    Vector3 velocity;
    Vector3 spin;
    float floorY;
    bool landed;

    public void Launch(Vector3 startVelocity, Vector3 spinDegrees, float floorHeight)
    {
        velocity = startVelocity;
        spin = spinDegrees;
        floorY = floorHeight;
    }

    void Update()
    {
        if (landed) return;

        velocity += Vector3.down * 12f * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(spin * Time.deltaTime, Space.World);

        if (transform.position.y <= floorY)
        {
            Vector3 p = transform.position;
            p.y = floorY;
            transform.position = p;
            landed = true;
        }
    }
}

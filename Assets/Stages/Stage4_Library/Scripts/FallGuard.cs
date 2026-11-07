using UnityEngine;

// Stage 4: a safety net on the library room. If the wizard ever ends up below the floor
// (pushed through a gap, climbing over an edge...), she is put back where she last stood on the ground.
public class FallGuard : MonoBehaviour
{
    public float fallHeight = -2f;

    CharacterController player;
    Vector3 lastSafe;

    void Start()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.GetComponent<CharacterController>();
            lastSafe = pc.transform.position;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 p = player.transform.position;
        if (player.isGrounded && p.y > -0.1f && p.y < 0.15f) lastSafe = p;

        if (p.y < fallHeight)
        {
            // a CharacterController must be switched off to be moved by hand
            player.enabled = false;
            player.transform.position = lastSafe + Vector3.up * 0.1f;
            player.enabled = true;
        }
    }
}

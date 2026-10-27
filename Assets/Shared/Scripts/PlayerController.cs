using UnityEngine;

// Moves the wizard from the tilt / keyboard input. The camera looks from the +Z side,
// so pushing "up" moves the player toward -Z (away from the camera).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed = 4.5f;
    [SerializeField] float turnSpeed = 720f; // degrees per second

    // The cauldron turns this off while the player is brewing.
    public bool CanMove = true;

    CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (GetComponent<PlayerAnimator>() == null) gameObject.AddComponent<PlayerAnimator>();
    }

    void Update()
    {
        GameInput input = GameInput.Instance;
        if (!CanMove || input == null) return;

        // the wizard stops when time runs out
        if (StageManager.Instance != null && StageManager.Instance.State == StageState.Lost) return;

        Vector3 direction = new Vector3(input.Move.x, 0f, -input.Move.y);
        if (direction.sqrMagnitude < 0.001f) return;

        controller.SimpleMove(direction * speed);

        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }
}

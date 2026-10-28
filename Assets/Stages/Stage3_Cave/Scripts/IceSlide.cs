using UnityEngine;

// Lives on the player in the Frozen Cave. On ice the wizard keeps sliding after letting go
// and takes a moment to change direction. It only corrects the movement PlayerController
// already made, so the shared player scripts stay untouched.
[RequireComponent(typeof(CharacterController))]
public class IceSlide : MonoBehaviour
{
    [SerializeField] float iceGrip = 2.2f;      // how fast the slide follows the tilt on ice (lower = more slippery)
    [SerializeField] float iceFriction = 0.9f;  // how fast the slide stops on ice when the player lets go
    [SerializeField] float groundFriction = 14f;

    CharacterController controller;
    PlayerController player;
    Vector3 lastPosition;
    Vector3 slide;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        player = GetComponent<PlayerController>();
        lastPosition = transform.position;
    }

    // runs after PlayerController has moved the wizard this frame
    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 moved = transform.position - lastPosition;
        moved.y = 0f;
        Vector3 wanted = moved / dt;

        StageManager stage = StageManager.Instance;
        bool free = (player == null || player.CanMove) && (stage == null || stage.State == StageState.Playing);
        bool teleported = wanted.sqrMagnitude > 400f; // faster than any walk: the wizard was moved by hand, don't pull it back

        if (!free || teleported)
        {
            slide = Vector3.zero;
        }
        else
        {
            bool steering = GameInput.Instance != null && GameInput.Instance.Move.sqrMagnitude > 0.001f;
            bool onIce = IceZone.Contains(transform.position);
            float rate = onIce ? (steering ? iceGrip : iceFriction) : (steering ? 1000f : groundFriction);
            slide = Vector3.Lerp(slide, steering ? wanted : Vector3.zero, 1f - Mathf.Exp(-rate * dt));

            Vector3 correction = (slide - wanted) * dt;
            if (correction.sqrMagnitude > 0.0000001f) controller.Move(correction);
        }

        lastPosition = transform.position;
    }
}

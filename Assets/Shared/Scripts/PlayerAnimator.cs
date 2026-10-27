using UnityEngine;

// Makes the wizard feel alive: breathing when idle, a bouncy waddle when walking,
// a kick on every shake while brewing and a happy jump when the potion is done.
// It is added automatically by PlayerController.
public class PlayerAnimator : MonoBehaviour
{
    Transform model;
    Vector3 lastPosition;
    float phase;
    float shakeKick;

    void Start()
    {
        model = transform.Find("Model");
        lastPosition = transform.position;
        if (GameInput.Instance != null) GameInput.Instance.Shaken += OnShaken;
    }

    void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.Shaken -= OnShaken;
    }

    void OnShaken()
    {
        shakeKick = 1f;
    }

    void Update()
    {
        if (model == null) return;

        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        float speed = Time.deltaTime > 0f ? new Vector2(delta.x, delta.z).magnitude / Time.deltaTime : 0f;
        bool moving = speed > 0.3f;

        StageManager stage = StageManager.Instance;
        bool won = stage != null && stage.State == StageState.Won;
        bool brewing = stage != null && stage.Brewing;

        float height = 0f;
        float scaleY = 1f;
        float lean = 0f;
        float roll = 0f;

        shakeKick = Mathf.MoveTowards(shakeKick, 0f, Time.deltaTime * 4f);

        if (won)
        {
            phase += Time.deltaTime * 9f;
            height = Mathf.Abs(Mathf.Sin(phase)) * 0.5f;
        }
        else if (brewing)
        {
            phase += Time.deltaTime * 14f;
            roll = Mathf.Sin(phase) * 3f;
            height = shakeKick * 0.18f;
            scaleY = 1f - shakeKick * 0.06f;
        }
        else if (moving)
        {
            phase += Time.deltaTime * 11f;
            height = Mathf.Abs(Mathf.Sin(phase)) * 0.08f;
            roll = Mathf.Sin(phase) * 4f;
            lean = 8f;
        }
        else
        {
            phase += Time.deltaTime * 2f;
            scaleY = 1f + Mathf.Sin(phase) * 0.02f;
        }

        float follow = 1f - Mathf.Exp(-20f * Time.deltaTime);

        Vector3 position = model.localPosition;
        position.y = Mathf.Lerp(position.y, height, follow);
        model.localPosition = position;

        Vector3 scale = model.localScale;
        scale.y = Mathf.Lerp(scale.y, scaleY, follow);
        model.localScale = scale;

        model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(lean, 0f, roll), follow);
    }
}

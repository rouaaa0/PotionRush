using UnityEngine;

// Makes the wizard feel alive: breathing when idle, a bouncy waddle when walking,
// a kick on every shake while brewing and a happy jump when the potion is done.
// When time runs out the cauldron explodes: the hat flies off and the wizard falls flat.
// It is added automatically by PlayerController.
public class PlayerAnimator : MonoBehaviour
{
    Transform model;
    Vector3 lastPosition;
    float phase;
    float shakeKick;
    float lostTime;
    bool hatLaunched;

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
        bool lost = stage != null && stage.State == StageState.Lost;
        bool brewing = stage != null && stage.Brewing;

        float height = 0f;
        float scaleY = 1f;
        float lean = 0f;
        float roll = 0f;
        float followSpeed = 20f;

        shakeKick = Mathf.MoveTowards(shakeKick, 0f, Time.deltaTime * 4f);

        if (lost)
        {
            lostTime += Time.deltaTime;
            followSpeed = 7f;

            // explosion: the hat flies off, then the wizard falls flat on her back
            if (!hatLaunched && lostTime > 0.1f)
            {
                hatLaunched = true;
                LaunchHat("HatBrim");
                LaunchHat("HatTop");
            }
            if (lostTime > 0.15f)
            {
                lean = -88f;
                height = 0.42f;
            }
        }
        else if (won)
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

        float follow = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        Vector3 position = model.localPosition;
        position.y = Mathf.Lerp(position.y, height, follow);
        model.localPosition = position;

        Vector3 scale = model.localScale;
        scale.y = Mathf.Lerp(scale.y, scaleY, follow);
        model.localScale = scale;

        model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(lean, 0f, roll), follow);
    }

    void LaunchHat(string partName)
    {
        Transform part = model.Find(partName);
        if (part == null) return;

        part.SetParent(null, true);
        Vector3 away = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 2.5f;
        part.gameObject.AddComponent<Tumble>().Launch(away + Vector3.up * 7f, new Vector3(Random.Range(-400f, 400f), Random.Range(-200f, 200f), Random.Range(-400f, 400f)), 0.05f);
    }
}

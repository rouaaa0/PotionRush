using UnityEngine;

// An optional gem of frost: walking over it gives bonus points.
// Its spin and float come from an animation clip; its sparkle burst is a child particle system.
public class FrostGem : MonoBehaviour
{
    public int points = 25;
    public float pickupRadius = 0.9f;
    public ParticleSystem burst;

    Transform player;

    void Start()
    {
        PlayerController p = FindAnyObjectByType<PlayerController>();
        if (p != null) player = p.transform;
    }

    void Update()
    {
        StageManager stage = StageManager.Instance;
        if (player == null || stage == null) return;

        Vector3 d = player.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > pickupRadius * pickupRadius) return;
        if (!stage.AddBonus(points)) return;

        if (burst != null)
        {
            burst.transform.SetParent(null, true); // keeps sparkling after the gem is gone
            burst.Play();
            Destroy(burst.gameObject, 2f);
        }
        if (GameSettings.SoundOn && Camera.main != null)
            AudioSource.PlayClipAtPoint(SoundBank.Collect(IngredientType.IceFlower), Camera.main.transform.position, 0.7f);
        Haptics.Tap(20);
        Destroy(gameObject);
    }
}

using System.Collections;
using UnityEngine;

// A crack in the floor that breathes out freezing air. It rumbles first (warning puff),
// then blasts: a wizard standing close is frozen in an ice block for a moment.
// The puffs, the jet and the ice block are objects in the scene; this only times them.
public class FreezeVent : MonoBehaviour
{
    public float cycle = 4.5f;      // seconds between two blasts
    public float offset;            // shifts the timing so the vents don't blast together
    public float warning = 0.9f;
    public float blast = 1.1f;
    public float radius = 1.4f;
    public float freezeTime = 1.6f;

    public ParticleSystem warningPuff;
    public ParticleSystem blastJet;
    public Light glow;

    static bool playerFrozen;

    PlayerController player;
    GameObject shell;
    float glowBase;
    int phase = -1; // 0 idle, 1 warning, 2 blast

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Transform s = player.transform.Find("FrozenShell");
            if (s != null) shell = s.gameObject;
        }
        if (glow != null) glowBase = glow.intensity;
        playerFrozen = false;
    }

    void Update()
    {
        StageManager stage = StageManager.Instance;
        bool playing = stage == null || stage.State == StageState.Playing;

        float t = Mathf.Repeat(Time.time + offset, cycle);
        int now = !playing ? 0 : t < cycle - warning - blast ? 0 : t < cycle - blast ? 1 : 2;

        if (now != phase)
        {
            phase = now;
            if (warningPuff != null) { if (phase == 1) warningPuff.Play(); else warningPuff.Stop(); }
            if (blastJet != null) { if (phase == 2) blastJet.Play(); else blastJet.Stop(); }
        }

        if (glow != null) glow.intensity = glowBase * (phase == 2 ? 4f : phase == 1 ? 1.5f + Mathf.Sin(Time.time * 30f) * 0.5f : 0.6f);

        if (phase == 2 && player != null && !playerFrozen && player.CanMove && (stage == null || !stage.Brewing))
        {
            Vector3 d = player.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude <= radius * radius) StartCoroutine(Freeze(stage));
        }
    }

    IEnumerator Freeze(StageManager stage)
    {
        playerFrozen = true;
        player.CanMove = false;
        if (shell != null) shell.SetActive(true);
        Haptics.Tap(80);

        yield return new WaitForSeconds(freezeTime);

        if (shell != null) shell.SetActive(false);
        // the cauldron also locks the player while brewing: leave that lock alone
        if (stage == null || (stage.State == StageState.Playing && !stage.Brewing)) player.CanMove = true;
        playerFrozen = false;
    }
}

using UnityEngine;
using UnityEngine.Rendering;

// Stage 4: the wizard turns see-through for a few seconds and the flying books ignore her.
// Started by InvisibilityPickup, which adds this to the player automatically.
// During the last 1.5 seconds she blinks, to warn that she is about to be seen again.
public class PlayerInvisibility : MonoBehaviour
{
    // FlyingBook checks this before hitting the wizard.
    public static bool Active { get; private set; }

    public float TimeLeft { get; private set; }

    Material ghost;
    MeshRenderer[] renderers;
    Material[][] originals;
    ShadowCastingMode[] shadows;
    bool showingGhost;

    public static void Begin(GameObject player, Material ghostMaterial, float seconds)
    {
        PlayerInvisibility inv = player.GetComponent<PlayerInvisibility>();
        if (inv == null) inv = player.AddComponent<PlayerInvisibility>();
        inv.StartEffect(ghostMaterial, seconds);
    }

    void StartEffect(Material ghostMaterial, float seconds)
    {
        if (renderers == null) Capture();
        ghost = ghostMaterial;
        TimeLeft = seconds; // picking up a second bottle restarts the timer
        Active = true;
        SetGhost(true);
    }

    // Remembers the wizard's own materials so they can be put back afterwards.
    void Capture()
    {
        Transform model = transform.Find("Model");
        renderers = (model != null ? model : transform).GetComponentsInChildren<MeshRenderer>(true);
        originals = new Material[renderers.Length][];
        shadows = new ShadowCastingMode[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originals[i] = renderers[i].sharedMaterials;
            shadows[i] = renderers[i].shadowCastingMode;
        }
    }

    void Update()
    {
        if (!Active) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            Active = false;
            SetGhost(false);
            return;
        }

        bool blinkOff = TimeLeft < 1.5f && Mathf.Repeat(TimeLeft, 0.3f) < 0.15f;
        SetGhost(!blinkOff);
    }

    void SetGhost(bool on)
    {
        if (ghost == null) on = false;
        if (renderers == null || on == showingGhost) return; // nothing to change
        showingGhost = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer r = renderers[i];
            if (r == null) continue;

            if (on)
            {
                Material[] mats = new Material[originals[i].Length];
                for (int m = 0; m < mats.Length; m++) mats[m] = ghost;
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            else
            {
                r.sharedMaterials = originals[i];
                r.shadowCastingMode = shadows[i];
            }
        }
    }

    void OnDestroy()
    {
        Active = false; // never carry the effect over to the next scene
    }
}

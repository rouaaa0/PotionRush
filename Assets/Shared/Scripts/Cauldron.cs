using UnityEngine;

// When the recipe is complete and the player walks up to it, the player is locked in place
// and each shake fills the brew until the potion is ready.
public class Cauldron : MonoBehaviour
{
    [SerializeField] float activateRadius = 2.2f;

    PlayerController player;
    bool brewing;
    int shakes;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (GameInput.Instance != null) GameInput.Instance.Shaken += OnShaken;
    }

    void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.Shaken -= OnShaken;
    }

    void Update()
    {
        StageManager stage = StageManager.Instance;
        if (brewing || stage == null || player == null) return;
        if (stage.State != StageState.Playing || !stage.RecipeComplete) return;

        if (Vector3.Distance(player.transform.position, transform.position) <= activateRadius)
        {
            brewing = true;
            shakes = 0;
            player.CanMove = false;
        }
    }

    void OnShaken()
    {
        if (!brewing) return;

        StageManager stage = StageManager.Instance;
        shakes++;
        stage.ReportBrewProgress((float)shakes / stage.config.shakesRequired);

        if (shakes >= stage.config.shakesRequired)
        {
            brewing = false;
            stage.CompleteBrew();
        }
    }
}

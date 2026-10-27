using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Plays the music, the sound effects and the vibrations. It creates itself when the game starts and
// stays alive between scenes, so no stage has to add it. It only listens to the StageManager.
public class GameFeedback : MonoBehaviour
{
    const float MusicVolume = 0.3f;

    static GameFeedback instance;

    AudioSource music;
    AudioSource effects;
    StageManager stage;
    float musicTarget = MusicVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null) return;

        GameObject go = new GameObject("GameFeedback");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameFeedback>();
    }

    void Awake()
    {
        music = gameObject.AddComponent<AudioSource>();
        music.clip = SoundBank.Music();
        music.loop = true;
        music.playOnAwake = false;
        music.volume = MusicVolume;

        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameSettings.Changed += ApplySettings;
        ApplySettings();
        music.Play();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameSettings.Changed -= ApplySettings;
        Unsubscribe();
    }

    void ApplySettings()
    {
        music.mute = !GameSettings.SoundOn;
        effects.mute = !GameSettings.SoundOn;
    }

    // every stage has its own StageManager, so listen to the new one whenever a scene loads
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Unsubscribe();
        musicTarget = MusicVolume;

        stage = StageManager.Instance;
        if (stage == null) return;

        stage.IngredientCollected += OnCollected;
        stage.BrewProgressed += OnBrewProgressed;
        stage.StageWon += OnWon;
        stage.StageLost += OnLost;
    }

    void Unsubscribe()
    {
        if (stage == null) return;
        stage.IngredientCollected -= OnCollected;
        stage.BrewProgressed -= OnBrewProgressed;
        stage.StageWon -= OnWon;
        stage.StageLost -= OnLost;
        stage = null;
    }

    void OnCollected(IngredientType type)
    {
        Play(SoundBank.Collect(type), 1f);
        Haptics.Tap(30);
    }

    void OnBrewProgressed(float progress)
    {
        Play(SoundBank.Brew(), 0.8f + progress * 0.9f); // higher and higher as the potion fills up
        Haptics.Tap(40);
    }

    void OnWon(int score)
    {
        musicTarget = 0.1f;
        Play(SoundBank.Win(), 1f);
        Haptics.Success();
    }

    void OnLost()
    {
        musicTarget = 0.05f;
        Play(SoundBank.Lose(), 1f);
        Haptics.Heavy();
    }

    void Play(AudioClip clip, float pitch)
    {
        effects.pitch = pitch;
        effects.PlayOneShot(clip);
    }

    void Update()
    {
        music.volume = Mathf.MoveTowards(music.volume, musicTarget, Time.unscaledDeltaTime * 0.3f);

        // a small click whenever the player presses something on the screen interface
        if (EventSystem.current == null) return;

        bool pressed = false;
        bool overUI = false;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pressed = true;
            overUI = EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
            overUI = EventSystem.current.IsPointerOverGameObject();
        }

        if (pressed && overUI) Play(SoundBank.Click(), 1f);
    }
}

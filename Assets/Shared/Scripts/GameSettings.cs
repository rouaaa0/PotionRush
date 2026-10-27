using System;
using UnityEngine;

// The player's sound and vibration choices, saved on the device.
public static class GameSettings
{
    public static event Action Changed;

    public static bool SoundOn
    {
        get { return PlayerPrefs.GetInt("sound_on", 1) == 1; }
        set { Save("sound_on", value); }
    }

    public static bool VibrationOn
    {
        get { return PlayerPrefs.GetInt("vibration_on", 1) == 1; }
        set { Save("vibration_on", value); }
    }

    static void Save(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}

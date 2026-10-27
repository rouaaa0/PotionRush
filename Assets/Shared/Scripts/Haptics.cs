using UnityEngine;

// Phone vibration. It does nothing in the Unity editor, so it can only be tested on a real phone.
// Android can vibrate for any length of time, so we use short taps and patterns there.
// iPhones only allow one fixed buzz, so we keep it for the big moments (win and lose).
public static class Haptics
{
#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator;

    static AndroidJavaObject Vibrator()
    {
        if (vibrator == null)
        {
            using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
        return vibrator;
    }
#endif

    // A short tap, for small events such as picking up an ingredient or a shake.
    public static void Tap(int milliseconds)
    {
        if (!GameSettings.VibrationOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try { Vibrator().Call("vibrate", (long)milliseconds); }
        catch { Handheld.Vibrate(); }
#endif
    }

    // Buzz, pause, buzz... (pattern in milliseconds: pause, buzz, pause, buzz...)
    public static void Pattern(long[] pattern)
    {
        if (!GameSettings.VibrationOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try { Vibrator().Call("vibrate", pattern, -1); }
        catch { Handheld.Vibrate(); }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    public static void Success()
    {
        Pattern(new long[] { 0, 60, 70, 60, 70, 220 });
    }

    public static void Heavy()
    {
        Pattern(new long[] { 0, 500 });
    }
}

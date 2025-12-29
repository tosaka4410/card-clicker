// Assets/Scripts/Utils/TutorialState.cs
using UnityEngine;

public static class TutorialState
{
    private const string KEY = "tutorial_completed";

    public static bool IsCompleted() => PlayerPrefs.GetInt(KEY, 0) == 1;

    public static void SetCompleted(bool completed)
    {
        PlayerPrefs.SetInt(KEY, completed ? 1 : 0);
        PlayerPrefs.Save(); // ★WebGLでも確実に
        Debug.Log($"[TutorialState] completed={completed}");
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}

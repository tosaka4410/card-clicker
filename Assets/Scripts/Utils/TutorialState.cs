using UnityEngine;
public static class TutorialState
{
    public static bool IsCompleted => PlayerPrefs.GetInt(SaveKeys.TutorialDone, 0) == 1;

    public static void SetCompleted()
    {
        PlayerPrefs.SetInt(SaveKeys.TutorialDone, 1);
        PlayerPrefs.Save();
    }

    // デバッグ用
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(SaveKeys.TutorialDone);
    }
}
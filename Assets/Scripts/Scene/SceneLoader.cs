using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public const string Menu = "MenuScene";
    public const string Tutorial = "TutorialScene";
    public const string Game = "GameScene";

    public static void LoadMenu() => SceneManager.LoadScene(Menu);
    public static void LoadTutorial() => SceneManager.LoadScene(Tutorial);
    public static void LoadGame() => SceneManager.LoadScene(Game);

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

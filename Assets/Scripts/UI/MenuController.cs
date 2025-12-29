using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    private Button startButton;

    [SerializeField]
    private Button tutorialButton;

    [SerializeField]
    private Button quitButton;

    [SerializeField]
    private SettingsUI settingsUI;
    private readonly ModalGuard modalGuard = new();
    public ModalGuard ModalGuard => modalGuard;

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                if (TutorialState.IsCompleted())
                    SceneLoader.LoadGame();
                else
                    SceneLoader.LoadTutorial();
            });
        }

        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(SceneLoader.LoadTutorial);

        if (quitButton != null)
            quitButton.onClick.AddListener(SceneLoader.Quit);

        settingsUI?.Init(modalGuard);

        AudioManager.Instance?.PlayBGM(BGMType.Menu);
    }

    public void OnClickTutorial()
    {
        SceneLoader.LoadTutorial();
    }
}

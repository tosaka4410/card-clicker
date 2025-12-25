using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button quitButton;

    void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(SceneLoader.LoadGame);

        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(SceneLoader.LoadTutorial);

        if (quitButton != null)
            quitButton.onClick.AddListener(SceneLoader.Quit);
        
        AudioManager.Instance?.PlayBGM(BGMType.Menu);
        
    }
}

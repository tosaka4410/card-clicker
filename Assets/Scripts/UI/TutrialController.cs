using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    [System.Serializable]
    public class Page
    {
        public string title;
        [TextArea] public string body;

        [Header("Image")]
        public Sprite sprite;          // ★ページ画像
        public bool keepAspect = true; // ★縦横比維持
    }

    [Header("UI")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Image pageImage; // ★追加（表示先）

    [Header("Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;

    [Header("Pages")]
    [SerializeField] private Page[] pages;

    private int index = 0;

    void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(() => { index--; Render(); });
        if (nextButton != null) nextButton.onClick.AddListener(() => { index++; Render(); });

        if (playButton != null) playButton.onClick.AddListener(SceneLoader.LoadGame);
        if (backButton != null) backButton.onClick.AddListener(SceneLoader.LoadMenu);

        Render();
    }

    void Render()
    {
        if (pages == null || pages.Length == 0)
        {
            if (titleText != null) titleText.text = "Tutorial";
            if (bodyText != null) bodyText.text = "No pages set.";
            if (pageImage != null)
            {
                pageImage.sprite = null;
                pageImage.enabled = false;
            }
            if (prevButton != null) prevButton.interactable = false;
            if (nextButton != null) nextButton.interactable = false;
            return;
        }

        index = Mathf.Clamp(index, 0, pages.Length - 1);
        var p = pages[index];

        if (titleText != null) titleText.text = p.title;
        if (bodyText != null) bodyText.text = p.body;

        // ★画像表示
        if (pageImage != null)
        {
            pageImage.sprite = p.sprite;
            pageImage.preserveAspect = p.keepAspect;
            pageImage.enabled = (p.sprite != null);
        }

        if (prevButton != null) prevButton.interactable = index > 0;
        if (nextButton != null) nextButton.interactable = index < pages.Length - 1;
    }
}

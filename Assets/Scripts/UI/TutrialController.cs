// Assets/Scripts/UI/TutorialController.cs
// 既存の TutrialController.cs をこれで差し替えてOK（クラス名は TutorialController）
// ローカルファイルは読まず、GitHub Pages のみを使用します。

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialController : MonoBehaviour
{
    [System.Serializable]
    public class Page
    {
        public string title;

        [TextArea]
        public string body;

        [Header("Video (GitHub Pages, optional)")]
        [Tooltip("例: tutorial1.mp4（拡張子込み） 空なら画像を表示します")]
        public string videoFileName;

        [Header("Image (optional)")]
        public Sprite image;
        public bool keepAspect = true;

        [Header("Video Options")]
        public bool loop = true;

        [Tooltip("自動再生を試みます（WebGLではユーザー操作がないとブロックされる場合あり）")]
        public bool autoPlay = true;
    }

    [Header("Video Hosting")]
    [Tooltip("例: https://tosaka4410.github.io/card-clicker/ （末尾/はどちらでもOK）")]
    [SerializeField]
    private string videoBaseUrl = "https://tosaka4410.github.io/card-clicker/";

    [Header("UI")]
    [SerializeField]
    private Text titleText;

    [SerializeField]
    private Text bodyText;

    [Header("Image View (for Image pages)")]
    [SerializeField]
    private Image pageImage;

    [Header("Video View (for Video pages)")]
    [SerializeField]
    private VideoPlayer videoPlayer;

    [SerializeField]
    private RawImage videoRawImage; // RenderTexture貼る想定（なくてもOK）

    [Header("Buttons")]
    [SerializeField]
    private Button prevButton;

    [SerializeField]
    private Button nextButton;

    [SerializeField]
    private Button playButton;

    [SerializeField]
    private Button backButton;

    [Header("Pages")]
    [SerializeField]
    private Page[] pages;

    private int index = 0;

    void Start()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(() =>
            {
                index--;
                Render();
            });

        if (nextButton != null)
            nextButton.onClick.AddListener(() =>
            {
                index++;
                Render();
            });

        if (playButton != null)
            playButton.onClick.AddListener(() =>
            {
                // ★チュートリアル完了を保存してからゲームへ
                TutorialState.SetCompleted(true);

                // 念のため停止（動画の残処理が邪魔しないように）
                StopAllCoroutines();
                SceneLoader.LoadGame();
            });
        if (backButton != null)
            backButton.onClick.AddListener(SceneLoader.LoadMenu);

        // VideoPlayerの音を確実に消す
        if (videoPlayer != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.source = VideoSource.Url;
        }

        Render();
    }

    void OnDisable()
    {
        StopVideo(); // 停止＆イベント解除
    }

    void Render()
    {
        if (pages == null || pages.Length == 0)
        {
            if (titleText != null)
                titleText.text = "Tutorial";
            if (bodyText != null)
                bodyText.text = "No pages set.";
            ShowVideo(false);
            ShowImage(false);
            SetButtons(false, false);
            return;
        }

        index = Mathf.Clamp(index, 0, pages.Length - 1);
        var p = pages[index];

        if (titleText != null)
            titleText.text = p.title;
        if (bodyText != null)
            bodyText.text = p.body;

        // 表示切替：動画優先、なければ画像
        if (!string.IsNullOrEmpty(p.videoFileName))
        {
            ShowImage(false);
            ShowVideo(true);
            PlayVideoPage(p);
        }
        else if (p.image != null)
        {
            ShowVideo(false);
            StopVideo();

            ShowImage(true);
            RenderImagePage(p);
        }
        else
        {
            ShowVideo(false);
            StopVideo();
            ShowImage(false);
        }

        SetButtons(index > 0, index < pages.Length - 1);
    }

    void SetButtons(bool canPrev, bool canNext)
    {
        if (prevButton != null)
            prevButton.interactable = canPrev;
        if (nextButton != null)
            nextButton.interactable = canNext;
    }

    void RenderImagePage(Page p)
    {
        if (pageImage == null)
            return;

        pageImage.sprite = p.image;
        pageImage.preserveAspect = p.keepAspect;
        pageImage.enabled = (p.image != null);
    }

    void ShowVideo(bool show)
    {
        if (videoRawImage != null)
            videoRawImage.gameObject.SetActive(show);
        if (videoPlayer != null)
            videoPlayer.gameObject.SetActive(show);
    }

    void ShowImage(bool show)
    {
        if (pageImage != null)
            pageImage.gameObject.SetActive(show);

        if (!show && videoRawImage != null)
            videoRawImage.texture = null;
    }

    void PlayVideoPage(Page p)
    {
        if (videoPlayer == null)
            return;

        // 前の動画を確実に止めてイベント解除
        StopVideo();

        var url = BuildVideoUrl(p.videoFileName);
        Debug.Log($"[Tutorial] Video url={url}");

        videoPlayer.isLooping = p.loop;
        videoPlayer.url = url;

        // Prepare完了で再生（autoPlay=true のときだけ）
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.errorReceived += OnVideoError;

        videoPlayer.Prepare();

        // autoPlay=falseならPrepareだけして待機（外部ボタンから videoPlayer.Play() してOK）
        // ※WebGLの自動再生ブロック対策にもなる
    }

    void OnPrepared(VideoPlayer vp)
    {
        var p = GetCurrentPageSafe();
        if (p == null)
        {
            vp.Play();
            return;
        }

        if (p.autoPlay)
            vp.Play();
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[Tutorial] VideoPlayer error: {message}\nurl={vp.url}");
    }

    void StopVideo()
    {
        if (videoPlayer == null)
            return;

        // イベント解除（必須：ページ切替で多重登録しない）
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.errorReceived -= OnVideoError;

        if (videoPlayer.isPlaying)
            videoPlayer.Stop();
        else
            videoPlayer.Stop(); // Prepare中でも止める意図で呼ぶ

        // RawImageにRenderTextureを貼ってる場合の残像対策（必要なら）
        // targetTextureを使っていないなら不要
        if (videoPlayer.targetTexture != null)
        {
            RenderTexture.active = videoPlayer.targetTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = null;
        }
    }

    Page GetCurrentPageSafe()
    {
        if (pages == null || pages.Length == 0)
            return null;
        int i = Mathf.Clamp(index, 0, pages.Length - 1);
        return pages[i];
    }

    string BuildVideoUrl(string fileName)
    {
        // baseUrl末尾を/に揃える
        var baseUrl = string.IsNullOrEmpty(videoBaseUrl) ? "" : videoBaseUrl.Trim();
        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        // fileName先頭の/を除去（ダブり防止）
        var f = (fileName ?? "").Trim();
        while (f.StartsWith("/"))
            f = f.Substring(1);

        // URLは必ず / で連結（Path.Combine禁止）
        return baseUrl + f;
    }
}

// Assets/Scripts/UI/TutorialController.cs
// ※ 既存の TutrialController.cs をこれで差し替えてOK（クラス名は TutorialController）
//    ファイル名とクラス名は一致しているのが望ましいです。

using System.IO;
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

        [Header("Video (StreamingAssets, optional)")]
        [Tooltip("例: tutorial1.mp4（拡張子込み） 空なら画像を表示します")]
        public string videoFileName;

        [Header("Image (optional)")]
        public Sprite image;
        public bool keepAspect = true;

        [Header("Video Options")]
        public bool loop = true;

        [Tooltip(
            "WebGLの自動再生がブロックされる場合、ONだと自動再生を試みます。ダメなら再生ボタン等で vp.Play() を呼んでください。"
        )]
        public bool autoPlay = true;
    }

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
    private RawImage videoRawImage; // RenderTexture を貼る想定（なければ空でも可）

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
            playButton.onClick.AddListener(SceneLoader.LoadGame);
        if (backButton != null)
            backButton.onClick.AddListener(SceneLoader.LoadMenu);

        // VideoPlayerの音を確実に消す（WebGLでも安全）
        if (videoPlayer != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
        }

        Render();
    }

    void OnDisable()
    {
        // 画面が閉じるときにイベント解除＆停止
        UnhookPrepareCompleted();
        StopVideo();
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
            // どちらも無し
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
            videoPlayer.gameObject.SetActive(show); // 必要ならON/OFF
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

        // 多重登録防止（ページ切替で必須）
        UnhookPrepareCompleted();

        // URL生成（Editorは file:/// を付けると安定）
        var url = ToVideoUrl(p.videoFileName);

        // ファイル存在チェック（Editor/Standaloneでは効く。WebGLではfalseになり得る）
#if !UNITY_WEBGL || UNITY_EDITOR
        var physicalPath = Path.Combine(Application.streamingAssetsPath, p.videoFileName);
        bool exists = File.Exists(physicalPath);
        Debug.Log($"[Tutorial] Video physicalPath={physicalPath} exists={exists}");
#endif
        Debug.Log($"[Tutorial] Video url={url}");

        videoPlayer.Stop();
        videoPlayer.isLooping = p.loop;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;

        // Prepare→完了でPlay（WebGLは自動再生がブロックされる場合あり）
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.errorReceived += OnVideoError; // 何かあったときログ出す

        videoPlayer.Prepare();

        // autoPlay=falseなら、外部ボタン等から videoPlayer.Play() を呼べる
        if (!p.autoPlay)
        {
            // Prepareだけして待機（OnPrepared内でPlayしない）
        }
    }

    void OnPrepared(VideoPlayer vp)
    {
        // 現在ページの設定を参照して再生判断
        var p = GetCurrentPageSafe();
        if (p == null)
        {
            vp.Play();
            return;
        }

        if (p.autoPlay)
        {
            vp.Play();
        }
        // autoPlay=false の場合はここでは再生しない（手動再生）
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[Tutorial] VideoPlayer error: {message}\nurl={vp.url}");
    }

    void StopVideo()
    {
        if (videoPlayer == null)
            return;

        UnhookPrepareCompleted();

        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        videoPlayer.errorReceived -= OnVideoError;

        if (videoPlayer.targetTexture != null)
        {
            RenderTexture.active = videoPlayer.targetTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = null;
        }
    }

    void UnhookPrepareCompleted()
    {
        if (videoPlayer == null)
            return;
        videoPlayer.prepareCompleted -= OnPrepared;
    }

    Page GetCurrentPageSafe()
    {
        if (pages == null || pages.Length == 0)
            return null;
        int i = Mathf.Clamp(index, 0, pages.Length - 1);
        return pages[i];
    }

    string ToVideoUrl(string fileName)
    {
        // StreamingAssets 内のパス
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        path = path.Replace("\\", "/");

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGLはHTTP(S)で配信されるパスになることが多いのでそのまま
        return path;
#else
        // Editor/Standaloneは file:// を付けると安定
        // 先頭のスラッシュ数を揃える（Windows対応）
        return "file:///" + path;
#endif
    }
}

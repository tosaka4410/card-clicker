using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ResultController : MonoBehaviour
{
    [SerializeField]
    private GameObject resultModal;

    [Header("UI")]
    [SerializeField]
    private CanvasGroup canvasGroup; // ★追加（全体フェード用）

    [SerializeField]
    private Text resultText;

    [SerializeField]
    private RectTransform resultTextRect; // ★追加（ポップ用）

    [SerializeField]
    private CanvasGroup buttonsGroup; // ★追加（ボタンフェード用）

    [SerializeField]
    private Button nextButton;

    [SerializeField]
    private Button retryButton;

    [SerializeField]
    private Image resultArtImage; // ★追加：表示先

    [SerializeField]
    private Sprite clearArt; // ★追加：クリア用イラスト

    [SerializeField]
    private Sprite gameOverArt; // ★追加：ゲームオーバー用イラスト

    [SerializeField]
    private CanvasGroup artCanvasGroup; // ★任意：フェード用

    [SerializeField]
    private RectTransform artRect; // ★任意：ポップ用

    [Header("Anim")]
    [SerializeField]
    private float fadeDuration = 0.18f;

    [SerializeField]
    private float popDuration = 0.22f;

    [SerializeField]
    private float popScale = 1.18f;

    private ModalGuard modal;
    private Tween seqTween;

    public bool IsOpen => resultModal != null && resultModal.activeInHierarchy;

    public void Init(ModalGuard modal)
    {
        this.modal = modal;
    }

    public void Show(bool cleared, int currentstage,Action onNext, Action onRetry)
    {
        modal?.Lock();

        resultModal.SetActive(true);
        resultModal.transform.SetAsLastSibling();

        // 文言
        resultText.text = cleared ? $"STAGE {currentstage} CLEAR!" : "GAME OVER";
        if (resultArtImage != null)
        {
            resultArtImage.sprite = cleared ? clearArt : gameOverArt;
            resultArtImage.enabled = resultArtImage.sprite != null;
        }

        // （任意）演出の初期化
        PrepareArtVisual();

        // （任意）演出開始
        PlayArtAnimation(cleared);

        // ボタン表示
        nextButton.gameObject.SetActive(cleared);
        retryButton.gameObject.SetActive(true);

        // 一旦押せない＆透明
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        // 初期化（演出）
        PrepareVisuals();
        PlayAnimation(cleared);

        // クリック
        nextButton.onClick.RemoveAllListeners();
        retryButton.onClick.RemoveAllListeners();

        if (cleared)
        {
            nextButton.onClick.AddListener(() =>
            {
                Hide();
                onNext?.Invoke();
            });
        }

        retryButton.onClick.AddListener(() =>
        {
            Hide();
            onRetry?.Invoke();
        });
    }

    private void PrepareVisuals()
    {
        // Tween残りを消す
        seqTween?.Kill();
        if (resultTextRect != null)
            resultTextRect.DOKill();
        if (canvasGroup != null)
            canvasGroup.DOKill();
        if (buttonsGroup != null)
            buttonsGroup.DOKill();

        // 全体フェード初期値
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // テキスト初期値
        if (resultTextRect != null)
        {
            resultTextRect.localScale = Vector3.one * 0.85f;
            resultTextRect.anchoredPosition = new Vector2(
                resultTextRect.anchoredPosition.x,
                resultTextRect.anchoredPosition.y - 20f
            );
        }
        if (resultText != null)
            resultText.color = new Color(
                resultText.color.r,
                resultText.color.g,
                resultText.color.b,
                0f
            );
    }

    private void PlayAnimation(bool cleared)
    {
        // SE/BGM もここで鳴らすなら
        AudioManager.Instance?.PlaySE(cleared ? SEType.StageClear : SEType.GameOver);

        var seq = DOTween.Sequence().SetUpdate(true);

        // 全体フェードイン
        if (canvasGroup != null)
            seq.Append(canvasGroup.DOFade(1f, fadeDuration));

        // テキスト：移動 + フェード + ポップ
        if (resultTextRect != null)
        {
            seq.Join(
                resultTextRect
                    .DOAnchorPosY(resultTextRect.anchoredPosition.y + 20f, popDuration)
                    .SetEase(Ease.OutCubic)
            );
            seq.Join(resultTextRect.DOScale(popScale, popDuration).SetEase(Ease.OutBack));
            seq.Append(resultTextRect.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
        }

        if (resultText != null)
            seq.Join(resultText.DOFade(1f, popDuration));

        // ちょいキラ/揺れ（クリア時だけ）
        if (cleared && resultTextRect != null)
        {
            seq.Append(
                resultTextRect.DOPunchRotation(new Vector3(0, 0, 4f), 0.22f, 10, 1f).SetUpdate(true)
            );
        }

        // ボタン表示
        seq.AppendInterval(0.05f);
        if (buttonsGroup != null)
        {
            seq.Append(buttonsGroup.DOFade(1f, 0.15f));
            seq.OnComplete(() =>
            {
                buttonsGroup.interactable = true;
                buttonsGroup.blocksRaycasts = true;
            });
        }

        seqTween = seq;
    }

    public void Hide()
    {
        // 即閉じ（演出で閉じたいならここもTweenにできる）
        seqTween?.Kill();
        resultModal.SetActive(false);
        modal?.Unlock();
    }

    private void PrepareArtVisual()
    {
        if (artCanvasGroup != null)
        {
            artCanvasGroup.DOKill();
            artCanvasGroup.alpha = 0f;
        }

        if (artRect != null)
        {
            artRect.DOKill();
            artRect.localScale = Vector3.one * 0.95f;
        }
    }

    private void PlayArtAnimation(bool cleared)
    {
        // timeScale=0でも動かす
        if (artCanvasGroup != null)
            artCanvasGroup.DOFade(1f, 0.18f).SetUpdate(true);

        if (artRect != null)
        {
            // クリアはちょい派手、ゲームオーバーは控えめ
            float scale = cleared ? 1.05f : 1.02f;
            artRect.DOScale(scale, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
            artRect.DOScale(1f, 0.10f).SetEase(Ease.OutQuad).SetUpdate(true).SetDelay(0.18f);
        }
    }
}

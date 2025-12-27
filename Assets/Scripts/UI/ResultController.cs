using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ResultController : MonoBehaviour
{
    [SerializeField] private GameObject resultModal;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text resultText;
    [SerializeField] private RectTransform resultTextRect;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button retryButton;

    [Header("Art")]
    [SerializeField] private Image resultArtImage;
    [SerializeField] private Sprite clearArt;
    [SerializeField] private Sprite gameOverArt;

    [Header("Optional Final Art")]
    [SerializeField] private Sprite finalClearArt; // ★最終クリア用（未設定ならclearArtを使う）

    [Header("Optional Art Anim")]
    [SerializeField] private CanvasGroup artCanvasGroup;
    [SerializeField] private RectTransform artRect;

    [Header("Anim")]
    [SerializeField] private float fadeDuration = 0.18f;
    [SerializeField] private float popDuration = 0.22f;
    [SerializeField] private float popScale = 1.18f;

    private ModalGuard modal;
    private Tween seqTween;

    public bool IsOpen => resultModal != null && resultModal.activeInHierarchy;

    public void Init(ModalGuard modal) => this.modal = modal;

    // ★変更：isFinal を追加
    public void Show(bool cleared, int currentstage, bool isFinal, Action onNext, Action onRetry)
    {
        modal?.Lock();

        resultModal.SetActive(true);
        resultModal.transform.SetAsLastSibling();

        // ---- 文言 & アート ----
        ApplyHeaderAndArt(cleared, currentstage, isFinal);

        // ---- 初期化 ----
        PrepareArtVisual();
        PrepareVisuals();

        // ---- 演出 ----
        PlayArtAnimation(cleared, isFinal);
        PlayAnimation(cleared, isFinal);

        // ---- ボタン表示 ----
        // クリア時は Next を出す（最終もNext＝メニューへ等に使える）
        nextButton.gameObject.SetActive(cleared);
        retryButton.gameObject.SetActive(true);

        // 一旦押せない＆透明
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        // ---- クリック ----
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

    private void ApplyHeaderAndArt(bool cleared, int currentstage, bool isFinal)
    {
        // 文言
        if (!cleared)
        {
            resultText.text = "GAME OVER";
        }
        else
        {
            resultText.text = isFinal ? "GAME CLEAR!" : $"STAGE {currentstage} CLEAR!";
        }

        // アート
        if (resultArtImage != null)
        {
            if (!cleared)
            {
                resultArtImage.sprite = gameOverArt;
            }
            else
            {
                // 最終用があればそれ、なければ通常クリア絵
                var s = isFinal && finalClearArt != null ? finalClearArt : clearArt;
                resultArtImage.sprite = s;
            }

            resultArtImage.enabled = resultArtImage.sprite != null;
        }
    }

    private void PrepareVisuals()
    {
        seqTween?.Kill();
        if (resultTextRect != null) resultTextRect.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
        if (buttonsGroup != null) buttonsGroup.DOKill();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (resultTextRect != null)
        {
            resultTextRect.localScale = Vector3.one * 0.85f;
            resultTextRect.anchoredPosition = new Vector2(
                resultTextRect.anchoredPosition.x,
                resultTextRect.anchoredPosition.y - 20f
            );
        }

        if (resultText != null)
        {
            resultText.color = new Color(
                resultText.color.r,
                resultText.color.g,
                resultText.color.b,
                0f
            );
        }
    }

    private void PlayAnimation(bool cleared, bool isFinal)
    {
        // SE：最終クリアは専用SEが無ければ StageClear を流用
        var se = !cleared ? SEType.GameOver : SEType.StageClear;
        AudioManager.Instance?.PlaySE(se);

        var seq = DOTween.Sequence().SetUpdate(true);

        if (canvasGroup != null)
            seq.Append(canvasGroup.DOFade(1f, fadeDuration));

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
            // 最終は少し派手にしてもOK（ここは好み）
            float punch = isFinal ? 6f : 4f;
            seq.Append(
                resultTextRect
                    .DOPunchRotation(new Vector3(0, 0, punch), 0.24f, 10, 1f)
                    .SetUpdate(true)
            );
        }

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

    private void PlayArtAnimation(bool cleared, bool isFinal)
    {
        if (artCanvasGroup != null)
            artCanvasGroup.DOFade(1f, 0.18f).SetUpdate(true);

        if (artRect != null)
        {
            // 最終はちょい派手に
            float scale =
                !cleared ? 1.02f :
                isFinal ? 1.08f :
                1.05f;

            artRect.DOScale(scale, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
            artRect.DOScale(1f, 0.10f).SetEase(Ease.OutQuad).SetUpdate(true).SetDelay(0.18f);
        }
    }
}

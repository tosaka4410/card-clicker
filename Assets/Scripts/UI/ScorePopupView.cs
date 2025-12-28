using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ScorePopupView : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    Tween tween;

    public void Play(long amount, Vector3 startPos)
    {
        if (text != null) text.text = $"+{amount}";
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        var rt = (RectTransform)transform;
        rt.anchoredPosition = startPos;

        tween?.Kill();

        // 初期スケール
        rt.localScale = Vector3.one * 0.9f;

        // ふわっと上に & フェードアウト & ちょいポップ
        tween = DOTween.Sequence()
            .Append(rt.DOScale(1.05f, 0.10f).SetEase(Ease.OutBack))
            .Append(rt.DOScale(1.00f, 0.08f).SetEase(Ease.OutQuad))
            .Join(rt.DOAnchorPosY(((Vector2)startPos).y + 60f, 0.6f).SetEase(Ease.OutQuad))
            .Join(canvasGroup.DOFade(0f, 0.6f).SetEase(Ease.OutQuad))
            .OnComplete(() => gameObject.SetActive(false))
            .SetUpdate(true); // timeScale=0でも動かす（好みで）
    }

}

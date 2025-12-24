using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HoverScaleAnimator :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Hover Scale")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private Ease easeIn = Ease.OutBack;
    [SerializeField] private Ease easeOut = Ease.OutQuad;

    private RectTransform rect;
    private Vector3 baseScale;
    private Tween tween;
    private bool enabledHover = true;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseScale = rect.localScale;
    }

    public void SetEnabled(bool enabled)
    {
        enabledHover = enabled;
        if (!enabled)
        {
            tween?.Kill();
            rect.localScale = baseScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enabledHover) return;

        tween?.Kill();
        tween = rect.DOScale(baseScale * hoverScale, duration)
            .SetEase(easeIn)
            .SetUpdate(true); // ← Time.timeScale = 0 でも動く
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enabledHover) return;

        tween?.Kill();
        tween = rect.DOScale(baseScale, duration)
            .SetEase(easeOut)
            .SetUpdate(true);
    }
}

using DG.Tweening;
using UnityEngine;

public class LogoAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform target; // 未指定なら自分

    [Header("Intro (appear)")]
    [SerializeField] private bool playIntro = true;
    [SerializeField] private float introDelay = 0.15f;
    [SerializeField] private float introDuration = 0.28f;
    [SerializeField] private float introPopScale = 1.12f;

    [Header("Idle (float)")]
    [SerializeField] private bool playIdleFloat = true;
    [SerializeField] private float floatAmplitudeY = 6f;   // 上下移動px
    [SerializeField] private float floatDuration = 2.6f;   // 秒
    [SerializeField] private float tiltAngle = 1.2f;       // 回転度
    [SerializeField] private float tiltDuration = 3.4f;    // 秒

    private Vector3 baseScale;
    private Vector2 basePos;
    private float baseRotZ;

    private Tween introTween;
    private Tween floatTween;
    private Tween tiltTween;

    void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        baseScale = target.localScale;
        basePos = target.anchoredPosition;
        baseRotZ = target.localEulerAngles.z;
    }

    void OnEnable()
    {
        Play();
    }

    void OnDisable()
    {
        KillAll();
    }

    public void Play()
    {
        KillAll();

        // 初期状態を基準に戻す
        target.localScale = baseScale;
        target.anchoredPosition = basePos;
        target.localEulerAngles = new Vector3(0, 0, baseRotZ);

        if (playIntro)
            PlayIntro();

        if (playIdleFloat)
            PlayIdle();
    }

    void PlayIntro()
    {
        target.localScale = baseScale * 0.92f;

        introTween = DOTween.Sequence()
            .SetUpdate(true)
            .AppendInterval(introDelay)
            .Append(target.DOScale(baseScale * introPopScale, introDuration).SetEase(Ease.OutBack))
            .Append(target.DOScale(baseScale, 0.12f).SetEase(Ease.OutQuad));
    }

    void PlayIdle()
    {
        // ふわふわ（上下）
        floatTween = target
            .DOAnchorPosY(basePos.y + floatAmplitudeY, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        // かすかな傾き
        tiltTween = target
            .DOLocalRotate(new Vector3(0, 0, baseRotZ + tiltAngle), tiltDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    void KillAll()
    {
        introTween?.Kill();
        floatTween?.Kill();
        tiltTween?.Kill();
        introTween = floatTween = tiltTween = null;
    }
}

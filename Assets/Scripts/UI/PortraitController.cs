using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public enum Actor
{
    CowGirl,
    DogGirl,
}

[Serializable]
public class PortraitEntry
{
    public CardKind kind;
    public Sprite sprite;
}

public class PortraitController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField]
    private Image cowImage;

    [SerializeField]
    private Image dogImage;

    [Header("Mappings")]
    [SerializeField]
    private List<PortraitEntry> cowMap = new();

    [SerializeField]
    private List<PortraitEntry> dogMap = new();

    [Header("Defaults")]
    [SerializeField]
    private Sprite cowDefault;

    [SerializeField]
    private Sprite dogDefault;

    [Header("Anim (DOTween)")]
    [SerializeField]
    private float popScale = 1.05f;

    [SerializeField]
    private float popDuration = 0.10f;

    [Header("Auto Revert")]
    [SerializeField]
    private float revertDelay = 1.2f; // 何秒で戻すか

    [SerializeField]
    private bool revertToDefault = true;

    private Dictionary<CardKind, Sprite> cowDict;
    private Dictionary<CardKind, Sprite> dogDict;
    private Tween cowRevertTween;
    private Tween dogRevertTween;

    void Awake()
    {
        cowDict = ToDict(cowMap);
        dogDict = ToDict(dogMap);

        if (cowImage != null && cowDefault != null)
            cowImage.sprite = cowDefault;
        if (dogImage != null && dogDefault != null)
            dogImage.sprite = dogDefault;
    }

    public void React(CardData card, Actor actor)
    {
        if (card == null)
            return;

        var img = actor == Actor.CowGirl ? cowImage : dogImage;
        if (img == null)
            return;

        var dict = actor == Actor.CowGirl ? cowDict : dogDict;

        Sprite next = null;
        if (!dict.TryGetValue(card.kind, out next) || next == null)
            next = actor == Actor.CowGirl ? cowDefault : dogDefault;

        img.sprite = next;

        // ちょいアニメ（timeScaleに影響されない）
        img.rectTransform.DOKill();
        var baseScale = Vector3.one;
        img.rectTransform.localScale = baseScale;
        img.rectTransform.DOScale(baseScale * popScale, popDuration)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);

        // ★一定時間後にデフォルトへ戻す
        if (revertToDefault)
            ScheduleRevert(actor);
    }

    private Dictionary<CardKind, Sprite> ToDict(List<PortraitEntry> list)
    {
        var d = new Dictionary<CardKind, Sprite>();
        foreach (var e in list)
        {
            if (e == null)
                continue;
            d[e.kind] = e.sprite;
        }
        return d;
    }

    private void ScheduleRevert(Actor actor)
    {
        if (revertDelay <= 0f)
            return;

        var img = actor == Actor.CowGirl ? cowImage : dogImage;
        if (img == null)
            return;

        var defaultSprite = actor == Actor.CowGirl ? cowDefault : dogDefault;
        if (defaultSprite == null)
            return;

        // actorごとにタイマーを持つ（連続Reactで上書き）
        ref Tween t = ref (actor == Actor.CowGirl ? ref cowRevertTween : ref dogRevertTween);

        t?.Kill();
        t = DOVirtual
            .DelayedCall(
                revertDelay,
                () =>
                {
                    img.sprite = defaultSprite;

                    // 戻る時にも少しだけポップ（任意）
                    img.rectTransform.DOKill();
                    img.rectTransform.localScale = Vector3.one;
                    img.rectTransform.DOScale(Vector3.one * popScale, popDuration)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetUpdate(true);
                }
            )
            .SetUpdate(true); // モーダルで timeScale=0 でも戻したいなら true
    }
}

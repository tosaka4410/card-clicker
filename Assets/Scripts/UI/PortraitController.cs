using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum Actor
{
    CowGirl,
    DogGirl
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
    [SerializeField] private Image cowImage;
    [SerializeField] private Image dogImage;

    [Header("Mappings")]
    [SerializeField] private List<PortraitEntry> cowMap = new();
    [SerializeField] private List<PortraitEntry> dogMap = new();

    [Header("Defaults")]
    [SerializeField] private Sprite cowDefault;
    [SerializeField] private Sprite dogDefault;

    [Header("Anim (DOTween)")]
    [SerializeField] private float popScale = 1.05f;
    [SerializeField] private float popDuration = 0.10f;

    private Dictionary<CardKind, Sprite> cowDict;
    private Dictionary<CardKind, Sprite> dogDict;

    void Awake()
    {
        cowDict = ToDict(cowMap);
        dogDict = ToDict(dogMap);

        if (cowImage != null && cowDefault != null) cowImage.sprite = cowDefault;
        if (dogImage != null && dogDefault != null) dogImage.sprite = dogDefault;
    }

    public void React(CardData card, Actor actor)
    {
        if (card == null) return;

        var img = actor == Actor.CowGirl ? cowImage : dogImage;
        if (img == null) return;

        var dict = actor == Actor.CowGirl ? cowDict : dogDict;
        Sprite next = null;

        if (!dict.TryGetValue(card.kind, out next) || next == null)
        {
            next = actor == Actor.CowGirl ? cowDefault : dogDefault;
        }

        img.sprite = next;

        // ちょいアニメ（timeScaleに影響されない）
        img.rectTransform.DOKill();
        var baseScale = Vector3.one;
        img.rectTransform.localScale = baseScale;
        img.rectTransform.DOScale(baseScale * popScale, popDuration)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private Dictionary<CardKind, Sprite> ToDict(List<PortraitEntry> list)
    {
        var d = new Dictionary<CardKind, Sprite>();
        foreach (var e in list)
        {
            if (e == null) continue;
            d[e.kind] = e.sprite;
        }
        return d;
    }
}

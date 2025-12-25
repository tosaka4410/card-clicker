using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TickerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField]
    private RectTransform viewport; // Mask付きの枠（TickerRoot）

    [SerializeField]
    private Text tickerText; // 流れるText

    [Header("Messages (fallback)")]
    [TextArea]
    [SerializeField]
    private List<string> messages = new();

    [Header("Timing")]
    [SerializeField]
    private float interval = 4.0f; // 次の文が出るまで

    [SerializeField]
    private float speed = 140f; // px/sec（大きいほど速い）

    [SerializeField]
    private bool ignoreTimeScale = true;

    [Header("No-repeat")]
    [SerializeField]
    private int noRepeatCount = 3;

    private readonly Queue<string> recent = new();
    private readonly HashSet<string> recentSet = new();

    private Func<IEnumerable<string>> messageProvider; // 外から差し替えたい場合用
    private Tween tween;
    private float timer;

    public void SetProvider(Func<IEnumerable<string>> provider) => messageProvider = provider;

    void OnDisable()
    {
        tween?.Kill();
        tween = null;
    }

    void Update()
    {
        if (viewport == null || tickerText == null)
            return;

        // 再生中に tween が動いているなら何もしない
        if (tween != null && tween.IsActive() && tween.IsPlaying())
            return;

        timer += Time.deltaTime;
        if (timer < interval)
            return;

        timer = 0f;
        PlayNext();
    }

    void PlayNext()
    {
        var list = BuildMessageList();
        if (list.Count == 0)
            return;

        string msg = PickWithoutRecent(list);
        if (string.IsNullOrEmpty(msg))
            return;

        tickerText.text = msg;

        // recent更新
        PushRecent(msg);

        // レイアウト反映
        Canvas.ForceUpdateCanvases();

        var textRt = (RectTransform)tickerText.transform;

        // レイアウト反映後に幅取得
        Canvas.ForceUpdateCanvases();
        float textW = textRt.rect.width;

        // viewport のローカル座標範囲（Anchor/Pivot無関係で正しい）
        var r = viewport.rect;

        // 右外 → 左外（マージン）
        float margin = 20f;
        float startX = r.xMax + textW * 0.5f + margin;
        float endX = r.xMin - textW * 0.5f - margin;

        // 開始位置
        var p = textRt.anchoredPosition;
        textRt.anchoredPosition = new Vector2(startX, p.y);

        // 移動
        float distance = Mathf.Abs(startX - endX);
        float duration = Mathf.Max(1f, distance / Mathf.Max(1f, speed));

        tween?.Kill();
        tween = textRt.DOAnchorPosX(endX, duration).SetEase(Ease.Linear).SetUpdate(ignoreTimeScale);
    }

    List<string> BuildMessageList()
    {
        var src = messageProvider != null ? messageProvider.Invoke() : messages;
        var list = new List<string>();
        if (src == null)
            return list;

        foreach (var s in src)
            if (!string.IsNullOrWhiteSpace(s))
                list.Add(s.Trim());

        return list;
    }

    string PickWithoutRecent(List<string> list)
    {
        // noRepeatCount が 0 なら完全ランダム
        if (noRepeatCount <= 0 || list.Count <= 1)
            return list[UnityEngine.Random.Range(0, list.Count)];

        // 候補から recent を除外
        var candidates = new List<string>(list.Count);
        foreach (var s in list)
            if (!recentSet.Contains(s))
                candidates.Add(s);

        // 全部 recent だった場合は諦めて全体から選ぶ（候補が少ない時の救済）
        if (candidates.Count == 0)
            candidates = list;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    void PushRecent(string msg)
    {
        if (noRepeatCount <= 0)
            return;

        if (recentSet.Contains(msg))
            return; // 念のため（通常はここに来ない）

        recent.Enqueue(msg);
        recentSet.Add(msg);

        while (recent.Count > noRepeatCount)
        {
            var old = recent.Dequeue();
            recentSet.Remove(old);
        }
    }
}

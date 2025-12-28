using System.Collections.Generic;
using UnityEngine;

public class ScorePopupSpawner : MonoBehaviour
{
    [SerializeField]
    private RectTransform canvasRoot; // Canvas (RectTransform)

    [SerializeField]
    private RectTransform anchor; // scoreText等のRectTransform

    [SerializeField]
    private ScorePopupView popupPrefab;

    [SerializeField]
    private int prewarm = 8;

    private readonly List<ScorePopupView> pool = new();

    void Awake()
    {
        for (int i = 0; i < prewarm; i++)
            CreateOne();
    }

    ScorePopupView CreateOne()
    {
        var v = Instantiate(popupPrefab, canvasRoot);
        v.gameObject.SetActive(false);
        pool.Add(v);
        return v;
    }

    ScorePopupView Rent()
    {
        foreach (var v in pool)
            if (!v.gameObject.activeSelf)
                return v;
        return CreateOne();
    }

    public void Show(long amount)
    {
        Debug.Log($"[Popup] Show called amount={amount}");
        if (amount <= 0)
        {
            Debug.Log($"[Popup] amount<=0, skipped");
            return;
        }
        if (canvasRoot == null || anchor == null || popupPrefab == null)
            return;

        var v = Rent();
        v.gameObject.SetActive(true);

        // Canvas の描画カメラを取得（Overlayなら null でOK）
        var canvas = canvasRoot.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        // anchor(ワールド) → スクリーン → canvasRoot(ローカル) に変換
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, anchor.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            screen,
            cam,
            out var localPoint
        );

        // ちょいランダムで被り回避（任意）
        localPoint.x += Random.Range(-20f, 20f);
        localPoint.y += Random.Range(-10f, 10f);

        // popup の位置設定（anchoredPositionを使うのが安全）
        var rt = (RectTransform)v.transform;
        rt.anchoredPosition = localPoint;
        rt.SetAsLastSibling(); // 前面に

        v.Play(amount, localPoint);
    }
}

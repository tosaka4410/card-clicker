using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CountdownController : MonoBehaviour
{
    [SerializeField]
    private Text countdownText;

    [SerializeField]
    private Color failedColor = Color.red;

    [SerializeField]
    private Color successColor = Color.white;

    // ---- end-of-stage countdown ----
    private int lastEndShown = -1;

    // ---- start countdown ----
    private string lastStartShown = null;

    private Coroutine startRoutine;
    private bool active = false;

    // 残り時間の 5..1 表示（既存）
    public void UpdateTime(float timeLeft, bool isGoalMet)
    {
        int sec = Mathf.CeilToInt(timeLeft);

        if (sec > 5 || sec <= 0)
        {
            Hide();
            return;
        }

        if (sec == lastEndShown)
            return;

        lastEndShown = sec;
        // 目標未達なら赤、達成なら白
        ShowText(sec.ToString(), isGoalMet ? successColor : failedColor);
    }

    private void ShowText(string text, Color color)
    {
        if (countdownText == null)
            return;

        active = true;
        countdownText.gameObject.SetActive(true);
        countdownText.text = text;
        countdownText.color = color;

        // アニメ（ここは「表示が変わった時だけ」呼ばれる）
        countdownText.transform.DOKill();
        countdownText.transform.localScale = Vector3.one * 0.5f;

        countdownText.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);

        countdownText
            .transform.DOScale(1f, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetDelay(0.2f)
            .SetUpdate(true);
    }

    public void Hide()
    {
        if (!active)
            return;

        active = false;
        lastEndShown = -1;
        lastStartShown = null;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    public void PlayStartCountdown(
        Action on3 = null,
        Action on2 = null,
        Action on1 = null,
        Action onStart = null,
        float interval = 1f
    )
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);
        StopStartCountdown();
        startRoutine = StartCoroutine(CoStartCountdown(on3, on2, on1, onStart, interval));
    }

    public void StopStartCountdown()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }
        Hide();
    }

    private IEnumerator CoStartCountdown(
        Action on3,
        Action on2,
        Action on1,
        Action onStart,
        float interval
    )
    {
        // 3
        ShowText("3", Color.white);
        on3?.Invoke();
        yield return new WaitForSecondsRealtime(interval);

        // 2
        ShowText("2", Color.white);
        on2?.Invoke();
        yield return new WaitForSecondsRealtime(interval);

        // 1
        ShowText("1", Color.white);
        on1?.Invoke();
        yield return new WaitForSecondsRealtime(interval);

        // START!
        ShowText("START!", Color.white);
        onStart?.Invoke();
        yield return new WaitForSecondsRealtime(0.35f);

        Hide();
        startRoutine = null;
    }
}

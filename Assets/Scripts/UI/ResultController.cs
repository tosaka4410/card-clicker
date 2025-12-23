using System;
using UnityEngine;
using UnityEngine.UI;

public class ResultController : MonoBehaviour
{
    [SerializeField] private GameObject resultModal;
    [SerializeField] private Text resultText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button retryButton;

    private ModalGuard modal;

    public bool IsOpen => resultModal != null && resultModal.activeInHierarchy;

    public void Init(ModalGuard modal)
    {
        this.modal = modal;
    }

    public void Show(bool cleared, Action onNext, Action onRetry)
    {
        modal?.Lock();

        resultModal.SetActive(true);
        resultText.text = cleared ? "STAGE CLEAR!" : "GAME OVER";

        nextButton.gameObject.SetActive(cleared);
        retryButton.gameObject.SetActive(true);

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

    public void Hide()
    {
        resultModal.SetActive(false);
        modal?.Unlock();
    }
}

using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField]
    private GameObject settingsModal;

    [SerializeField]
    private Slider bgmSlider;

    [SerializeField]
    private Slider seSlider;

    [SerializeField]
    private Button openSettingsButton;

    [SerializeField]
    private Button closeSettingsButton;

    private ModalGuard modal;

    public bool IsOpen => settingsModal != null && settingsModal.activeInHierarchy;

    public void Init(ModalGuard modal)
    {
        this.modal = modal;

        openSettingsButton.onClick.RemoveAllListeners();
        closeSettingsButton.onClick.RemoveAllListeners();

        openSettingsButton.onClick.AddListener(Open);
        closeSettingsButton.onClick.AddListener(Close);

        // Slider初期設定（範囲は0..1想定）
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
        }

        if (seSlider != null)
        {
            seSlider.minValue = 0f;
            seSlider.maxValue = 1f;
        }
    }

    public void Open()
    {
        if(modal != null && modal.IsLocked) return;
        modal?.Lock();
        settingsModal.SetActive(true);
        settingsModal.transform.SetAsLastSibling();

        Refresh();
        HookSliders();

        AudioManager.Instance?.PlaySE(SEType.CardDraw); // 適当：開くSEが欲しければ専用SEを作るの推奨
    }

    public void Close()
    {
        settingsModal.SetActive(false);
        modal?.Unlock();

        AudioManager.Instance?.PlaySE(SEType.CardPlay); // 適当：閉じるSEが欲しければ専用SEを作るの推奨
    }

    private void Refresh()
    {
        var am = AudioManager.Instance;
        if (am == null) return;

        // イベント発火なしで値をセット
        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(am.BgmVolume);

        if (seSlider != null)
            seSlider.SetValueWithoutNotify(am.SeVolume);
    }

    private void HookSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetBgmVolume(v);
            });
        }

        if (seSlider != null)
        {
            seSlider.onValueChanged.RemoveAllListeners();
            seSlider.onValueChanged.AddListener(v =>
            {
                AudioManager.Instance?.SetSeVolume(v);
            });
        }
    }
}

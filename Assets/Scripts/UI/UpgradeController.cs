using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeController : MonoBehaviour
{
    [SerializeField] private GameObject upgradeModal;
    [SerializeField] private Text upgradeTitleText;

    // ★差し替え
    [SerializeField] private UpgradeOptionView optionPrefab;

    [SerializeField] private Transform upgradeOptionsRoot;

    private ModalGuard modal;

    public bool IsOpen => upgradeModal != null && upgradeModal.activeInHierarchy;

    public void Init(ModalGuard modal) => this.modal = modal;

    public void Show(List<UpgradeChoice> choices, Action<UpgradeChoice> onPick)
    {
        modal?.Lock();

        upgradeModal.SetActive(true);
        upgradeModal.transform.SetAsLastSibling();
        upgradeTitleText.text = "アップグレードを選択！";

        for (int i = upgradeOptionsRoot.childCount - 1; i >= 0; i--)
            Destroy(upgradeOptionsRoot.GetChild(i).gameObject);

        foreach (var choice in choices)
        {
            var view = Instantiate(optionPrefab, upgradeOptionsRoot);
            view.Bind(choice, () =>
            {
                upgradeModal.SetActive(false);
                modal?.Unlock();
                onPick?.Invoke(choice);
            });
        }
    }
}

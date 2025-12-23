using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeController : MonoBehaviour
{
    [SerializeField] private GameObject upgradeModal;
    [SerializeField] private Text upgradeTitleText;
    [SerializeField] private Button upgradeButtonPrefab;
    [SerializeField] private Transform upgradeOptionsRoot;

    private ModalGuard modal;

    public bool IsOpen => upgradeModal != null && upgradeModal.activeInHierarchy;

    public void Init(ModalGuard modal) => this.modal = modal;

    public void Show(List<UpgradeChoice> choices, Action<UpgradeChoice> onPick)
    {
        modal?.Lock();

        upgradeModal.SetActive(true);
        upgradeModal.transform.SetAsLastSibling();
        upgradeTitleText.text = "Choose one upgrade";

        for (int i = upgradeOptionsRoot.childCount - 1; i >= 0; i--)
            Destroy(upgradeOptionsRoot.GetChild(i).gameObject);

        foreach (var choice in choices)
        {
            var btn = Instantiate(upgradeButtonPrefab, upgradeOptionsRoot);
            var view = btn.GetComponent<UpgradeButtonView>();

            string title = choice.upgrade.title;
            string desc = $"{choice.upgrade.description}\n\nTarget: {choice.targetCard.cardName}";
            view.Bind(title, desc);

            btn.onClick.AddListener(() =>
            {
                upgradeModal.SetActive(false);
                modal?.Unlock();
                onPick?.Invoke(choice);
            });
        }
    }
}

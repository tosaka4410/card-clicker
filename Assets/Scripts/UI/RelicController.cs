using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RelicController : MonoBehaviour
{
    [SerializeField] private GameObject relicModal;
    [SerializeField] private Transform relicOptionsRoot;
    [SerializeField] private Button relicButtonPrefab;

    private ModalGuard modal;
    public bool IsOpen => relicModal != null && relicModal.activeInHierarchy;

    public void Init(ModalGuard modal) => this.modal = modal;

    public void Show(List<RelicData> relics, Action<RelicData> onPick)
    {
        modal?.Lock();

        relicModal.SetActive(true);
        relicModal.transform.SetAsLastSibling();

        for (int i = relicOptionsRoot.childCount - 1; i >= 0; i--)
            Destroy(relicOptionsRoot.GetChild(i).gameObject);

        foreach (var relic in relics)
        {
            var btn = Instantiate(relicButtonPrefab, relicOptionsRoot);
            var texts = btn.GetComponentsInChildren<Text>();

            texts[0].text = relic.relicName;
            texts[1].text = relic.description;

            btn.onClick.AddListener(() =>
            {
                relicModal.SetActive(false);
                modal?.Unlock();
                onPick?.Invoke(relic);
            });
        }
    }
}

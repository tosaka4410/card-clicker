using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckViewController : MonoBehaviour
{
    [SerializeField] private GameObject deckViewModal;
    [SerializeField] private Transform deckViewContent;
    [SerializeField] private GameObject deckCardItemPrefab;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    private ModalGuard modal;
    private Func<IReadOnlyList<CardData>> getDeck;

    public bool IsOpen => deckViewModal != null && deckViewModal.activeInHierarchy;

    public void Init(ModalGuard modal, Func<IReadOnlyList<CardData>> getDeck)
    {
        this.modal = modal;
        this.getDeck = getDeck;

        openButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        modal?.Lock();
        deckViewModal.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        deckViewModal.SetActive(false);
        modal?.Unlock();
    }

    private void Refresh()
    {
        if (deckViewContent == null || deckCardItemPrefab == null) return;

        for (int i = deckViewContent.childCount - 1; i >= 0; i--)
            Destroy(deckViewContent.GetChild(i).gameObject);

        var deck = getDeck?.Invoke();
        if (deck == null) return;

        foreach (var card in deck)
        {
            var go = Instantiate(deckCardItemPrefab, deckViewContent);
            var view = go.GetComponent<CardView>();
            if (view == null) continue;
            view.Bind(card, CardDisplayMode.Deck);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    [SerializeField]
    private GameObject shopModal;

    [SerializeField]
    private Transform shopItemsRoot;

    [SerializeField]
    private Button shopItemButtonPrefab;


    [SerializeField]
    private Button openShopButton;

    [SerializeField]
    private Button closeShopButton;

    private ShopSystem shopSystem;
    private ModalGuard modal;

    private Func<int> getScore;
    private Func<int, bool> tryPay;
    private Action<CardData> onBuy;

    public bool IsOpen => shopModal != null && shopModal.activeInHierarchy;

    public void Init(
        ShopSystem shopSystem,
        ModalGuard modal,
        Func<int> getScore,
        Func<int, bool> tryPay,
        Action<CardData> onBuy
    )
    {
        this.shopSystem = shopSystem;
        this.modal = modal;
        this.getScore = getScore;
        this.tryPay = tryPay;
        this.onBuy = onBuy;

        openShopButton.onClick.RemoveAllListeners();
        closeShopButton.onClick.RemoveAllListeners();
        openShopButton.onClick.AddListener(Open);
        closeShopButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        shopModal.SetActive(true);
        Refresh(false);
    }

    public void Close()
    {
        shopModal.SetActive(false);
    }

    private void Refresh(bool isReroll)
    {
        if (isReroll && !tryPay(shopSystem.rerollCost))
            return;

        for (int i = shopItemsRoot.childCount - 1; i >= 0; i--)
            Destroy(shopItemsRoot.GetChild(i).gameObject);

        shopSystem.GenerateLineup();
        var items = shopSystem.CurrentItems;

        foreach (var item in items)
        {
            var go = Instantiate(shopItemButtonPrefab, shopItemsRoot);

            var view = go.GetComponent<CardView>();
            if (view == null)
            {
                Debug.LogError("[Shop] ShopItemPrefab has no CardView");
                continue;
            }

            view.Bind(
                item.card,
                CardDisplayMode.Shop,
                onClick: _ =>
                {
                    if (!tryPay(item.cost))
                    {
                        AudioManager.Instance?.PlaySE(SEType.Error);
                        return;
                    }
                    AudioManager.Instance?.PlaySE(SEType.Buy);
                    onBuy?.Invoke(item.card);
                    view.SetSold(true);
                },
                cost: item.cost,
                sold: false,
                tag: null
            );
        }

    }
}

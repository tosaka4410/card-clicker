// Assets/Scripts/UI/ShopController.cs
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
    private Text openCostText;

    [SerializeField]
    private Button openShopButton;

    [SerializeField]
    private Button closeShopButton;

    private ShopSystem shopSystem;
    private ModalGuard modal;

    private Func<long> getScore;
    private Func<int, bool> tryPay;
    private Action<CardData> onBuy;

    public bool IsOpen => shopModal != null && shopModal.activeInHierarchy;

    void Update()
    {
        // ショップが閉じている時だけ更新（開いてる最中はボタン触らないので）
        if (!IsOpen)
            UpdateOpenCostView();
    }

    public void Init(
        ShopSystem shopSystem,
        ModalGuard modal,
        Func<long> getScore,
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

        UpdateOpenCostView();
    }

    public void Open()
    {
        // ★開く時だけコスト
        int openCost = shopSystem.GetOpenCost();
        if (!tryPay(openCost))
        {
            AudioManager.Instance?.PlaySE(SEType.Error);
            UpdateOpenCostView();
            return;
        }

        shopSystem.NotifyOpened();
        AudioManager.Instance?.PlaySE(SEType.Buy); // 開店SE（専用があれば差し替え）

        modal?.Lock(); // モーダルとして扱うなら推奨（他UI操作を止めたい場合）
        shopModal.SetActive(true);
        shopModal.transform.SetAsLastSibling();

        Refresh();
    }

    public void Close()
    {
        shopModal.SetActive(false);
        UpdateOpenCostView();
        modal?.Unlock();
    }

    private void Refresh()
    {
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
                    // ★購入は無料
                    AudioManager.Instance?.PlaySE(SEType.Buy);
                    onBuy?.Invoke(item.card);

                    // 好み：買ったら売り切れ表示にする or そのまま何度でも買える
                    view.SetSold(true);
                    Close();
                },
                cost: null, // ★Cost表示を消す（CardView側がnull許容なら）
                sold: false,
                tag: null
            );
        }
    }

    void UpdateOpenCostView()
    {
        if (openCostText == null || shopSystem == null)
            return;

        int cost = shopSystem.GetOpenCost();
        openCostText.text = $"{cost}";

        // 押せるかどうかで色を変える（任意）
        bool canOpen = getScore != null && getScore() >= cost;

        if (openShopButton != null)
            openShopButton.interactable = canOpen;
    }

    public void RefreshOpenCostUI()
    {
        UpdateOpenCostView();
    }
}

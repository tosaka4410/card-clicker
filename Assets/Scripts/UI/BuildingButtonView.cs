using UnityEngine;
using UnityEngine.UI;

public class BuildingButtonView : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text dpsText;
    [SerializeField] private Text costText;

    [Header("Button")]
    [SerializeField] private Button buyButton;

    public Button BuyButton => buyButton;

    public void SetTexts(string name, string dps, string cost)
    {
        if (nameText != null) nameText.text = name;
        if (dpsText != null) dpsText.text = dps;
        if (costText != null) costText.text = cost;
    }

    public void SetInteractable(bool interactable)
    {
        if (buyButton != null) buyButton.interactable = interactable;
    }
}

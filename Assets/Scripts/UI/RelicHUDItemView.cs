using UnityEngine;
using UnityEngine.UI;

public class RelicHUDItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text countText;

    public void Bind(RelicData relic, int count)
    {
        if (iconImage != null)
        {
            iconImage.sprite = relic != null ? relic.icon : null;
            iconImage.enabled = (relic != null && relic.icon != null);
        }

        if (countText != null)
            countText.text = $"{count}";
    }
}

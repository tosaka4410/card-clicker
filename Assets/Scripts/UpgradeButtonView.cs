using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonView : MonoBehaviour
{
    public Text titleText;
    public Text descriptionText;

    public void Bind(string title, string description)
    {
        titleText.text = title;
        descriptionText.text = description;
    }
}

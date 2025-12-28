using UnityEngine;
using UnityEngine.UI;

public class MenuInfoController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text leftInfoText;
    [SerializeField] private Text rightInfoText;

    [Header("Format")]
    [SerializeField] private string versionPrefix = "v";
    [SerializeField] private string creditText = "© 2025 One More Drink";

    const string PREF_BEST = "best_score";
    const string PREF_LAST = "last_score";

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        // Left: version + credits
        string ver = Application.version;
        if (leftInfoText != null)
        {
            leftInfoText.text = $"{versionPrefix}{ver}\n{creditText}";
        }

        // Right: best / last (stored as strings to support long)
        long best = GetPrefLong(PREF_BEST, 0);
        long last = GetPrefLong(PREF_LAST, 0);

        if (rightInfoText != null)
        {
            if (last > 0)
                rightInfoText.text = $"BEST {best:N0}\nLAST {last:N0}";
            else
                rightInfoText.text = $"BEST {best:N0}";
        }
    }

    // ゲーム側から更新したい場合用（任意）
    public static void SaveScores(long lastScore)
    {
        // Save as string to support long values
        PlayerPrefs.SetString(PREF_LAST, lastScore.ToString());

        long best = GetPrefLong(PREF_BEST, 0);
        if (lastScore > best)
            PlayerPrefs.SetString(PREF_BEST, lastScore.ToString());

        PlayerPrefs.Save();
    }

    private static long GetPrefLong(string key, long defaultValue = 0)
    {
        string s = PlayerPrefs.GetString(key, null);
        if (!string.IsNullOrEmpty(s) && long.TryParse(s, out long v))
            return v;

        // Fallback to old int storage
        return PlayerPrefs.GetInt(key, (int)defaultValue);
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RelicHUDController : MonoBehaviour
{
    [SerializeField] private Transform root;              // 並べる親（HorizontalLayoutGroup推奨）
    [SerializeField] private RelicHUDItemView itemPrefab; // アイコン＋×N のPrefab

    // 表示を安定させたいなら並び順を固定（追加順 or 名前順）
    public void Refresh(IReadOnlyDictionary<RelicData, int> counts)
    {
        if (root == null || itemPrefab == null) return;

        // 既存を全削除（レリック数が少ないならこれで十分）
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        if (counts == null || counts.Count == 0)
            return;

        // 並び順：ここは好みで（例：relicName順）
        foreach (var kv in counts.OrderBy(k => k.Key != null ? k.Key.relicName : ""))
        {
            var relic = kv.Key;
            var count = kv.Value;
            if (relic == null || count <= 0) continue;

            var v = Instantiate(itemPrefab, root);
            v.Bind(relic, count);
        }
    }
}

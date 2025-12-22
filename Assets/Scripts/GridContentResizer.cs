using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GridLayoutGroup))]
public class GridContentResizer : MonoBehaviour
{
    RectTransform rt;
    GridLayoutGroup grid;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        grid = GetComponent<GridLayoutGroup>();
    }

    public void Rebuild()
    {
        int count = transform.childCount;

        int cols = 1;
        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            cols = Mathf.Max(1, grid.constraintCount);

        int rows = Mathf.CeilToInt(count / (float)cols);

        float height =
            grid.padding.top + grid.padding.bottom +
            rows * grid.cellSize.y +
            Mathf.Max(0, rows - 1) * grid.spacing.y;

        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    void LateUpdate() => Rebuild(); // MVPはこれでOK（軽くしたければ手動呼びに）
}

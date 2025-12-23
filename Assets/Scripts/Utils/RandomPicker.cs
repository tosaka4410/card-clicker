using System.Collections.Generic;
using UnityEngine;

public static class RandomPicker
{
    // 重複なしで最大count件
    public static List<T> PickUnique<T>(IList<T> source, int count)
    {
        var result = new List<T>(count);
        if (source == null || source.Count == 0 || count <= 0) return result;

        var temp = new List<T>(source);

        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }
}

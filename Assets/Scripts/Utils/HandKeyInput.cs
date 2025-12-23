using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandKeyInput : MonoBehaviour
{
    public Func<bool> CanPlay;
    public Func<int> GetHandCount;
    public Action<int> PlayHandIndex; // 0-based

    void Update()
    {
        if (CanPlay == null || GetHandCount == null || PlayHandIndex == null) return;
        if (!CanPlay()) return;

        if (Keyboard.current == null) return;

        int max = Mathf.Min(GetHandCount(), 9);
        for (int i = 0; i < max; i++)
        {
            var key = Key.Digit1 + i;
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                PlayHandIndex(i);
                return;
            }
        }
    }
}

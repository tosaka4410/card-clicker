using UnityEngine;

public class ModalGuard
{
    private int lockCount = 0;

    public bool IsLocked => lockCount > 0;

    public void Lock()
    {
        lockCount++;
        Time.timeScale = 0f;
    }

    public void Unlock()
    {
        lockCount = Mathf.Max(0, lockCount - 1);
        if (lockCount == 0)
            Time.timeScale = 1f;
    }

    public void ForceReset()
    {
        lockCount = 0;
        Time.timeScale = 1f;
    }
}

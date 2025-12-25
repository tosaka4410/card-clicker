using UnityEngine;
using UnityEngine.InputSystem;

public class DebugCheats : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private int addScoreAmount = 10000;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current == null) return;

        // F1で+10000
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            if (gameManager != null)
            {
                gameManager.AddScore(addScoreAmount);
                Debug.Log($"[Cheat] +{addScoreAmount} score");
            }
        }
#endif
    }
}

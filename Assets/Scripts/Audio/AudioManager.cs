using UnityEngine;
using System.Collections.Generic;

public enum SEType
{
    CardPlay,
    CardDraw,
    Build,
    Buy,
    Error,
    StageClear,
    GameOver
}

public enum BGMType
{
    Stage,
    Shop,
    Result
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioSources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("BGM")]
    [SerializeField] private List<BGMEntry> bgms;

    [Header("SE")]
    [SerializeField] private List<SEEntry> ses;

    private Dictionary<BGMType, AudioClip> bgmDict;
    private Dictionary<SEType, AudioClip> seDict;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmDict = new();
        foreach (var e in bgms)
            if (!bgmDict.ContainsKey(e.type))
                bgmDict.Add(e.type, e.clip);

        seDict = new();
        foreach (var e in ses)
            if (!seDict.ContainsKey(e.type))
                seDict.Add(e.type, e.clip);
    }

    public void PlayBGM(BGMType type)
    {
        if (!bgmDict.TryGetValue(type, out var clip))
            return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    public void PlaySE(SEType type)
    {
        if (!seDict.TryGetValue(type, out var clip))
            return;

        seSource.PlayOneShot(clip);
    }
}

[System.Serializable]
public class BGMEntry
{
    public BGMType type;
    public AudioClip clip;
}

[System.Serializable]
public class SEEntry
{
    public SEType type;
    public AudioClip clip;
}

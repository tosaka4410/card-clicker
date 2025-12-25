using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SEType
{
    CardPlay,
    CardDraw,
    Build,
    Buy,
    Error,
    StageClear,
    GameOver,
}

public enum BGMType
{
    Stage,
    Shop,
    Result,
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioSources")]
    [SerializeField]
    private AudioSource bgmSource;

    [SerializeField]
    private AudioSource seSource;

    [Header("Mixer")]
    [SerializeField]
    private AudioMixer mixer;

    [SerializeField]
    private string bgmVolumeParam = "BGM_Vol";

    [SerializeField]
    private string seVolumeParam = "SE_Vol";

    // 0..1
    public float BgmVolume { get; private set; } = 1f;
    public float SeVolume { get; private set; } = 1f;

    private const string PREF_BGM = "vol_bgm";
    private const string PREF_SE = "vol_se";

    [Header("BGM")]
    [SerializeField]
    private List<BGMEntry> bgms;

    [Header("SE")]
    [SerializeField]
    private List<SEEntry> ses;

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

        // 保存済み音量を読み込み（なければ1.0）
        BgmVolume = PlayerPrefs.GetFloat(PREF_BGM, 1f);
        SeVolume = PlayerPrefs.GetFloat(PREF_SE, 1f);

        ApplyVolumes();
    }

    public void SetBgmVolume(float v01)
    {
        BgmVolume = Mathf.Clamp01(v01);
        PlayerPrefs.SetFloat(PREF_BGM, BgmVolume);
        ApplyVolumes();
    }

    public void SetSeVolume(float v01)
    {
        SeVolume = Mathf.Clamp01(v01);
        PlayerPrefs.SetFloat(PREF_SE, SeVolume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (mixer == null)
            return;

        // 0..1 を dB に変換（0は-80dB扱い）
        mixer.SetFloat(bgmVolumeParam, ToDb(BgmVolume));
        mixer.SetFloat(seVolumeParam, ToDb(SeVolume));
    }

    private float ToDb(float v01)
    {
        if (v01 <= 0.0001f)
            return -80f; // ほぼ無音
        return Mathf.Log10(v01) * 20f; // 1.0 => 0dB
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

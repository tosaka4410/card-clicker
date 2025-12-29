using UnityEngine;
using UnityEngine.Video;

public class WebGLVideo : MonoBehaviour
{
    [SerializeField] private VideoPlayer vp;
    [SerializeField] private string fileName = "tutorial.mp4";

    void Start()
    {
        // WebGLではStreamingAssetsはHTTP経由で配信される想定
        vp.source = VideoSource.Url;
        vp.audioOutputMode = VideoAudioOutputMode.None;

        vp.url = "https://tosaka4410.github.io/card-clicker/" + fileName;

        // 自動再生はブラウザに止められることがあるので、基本はユーザー操作後に Play 推奨
        vp.Prepare();
        vp.prepareCompleted += _ => vp.Play();
    }
}

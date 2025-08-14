// TtsDemo.cs
using UnityEngine;

public class TtsDemo : MonoBehaviour
{
    public AudioSource audioSource;
    public string voiceId = "en_US-joe-medium";
    [TextArea] public string text = "Hello from Piper offline TTS on Quest Pro.";

    private ITtsService _tts;

    private async void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

    _tts = new AndroidPiperTtsService();
#else
        _tts = new EditorStubTtsService();
#endif
    }

    [ContextMenu("Speak")]
    public async void Speak()
    {
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        var pcm48 = await _tts.Synthesize48kAsync(text, voiceId);
        var clip = AudioClip.Create("TTS", pcm48.Length, 1, 48000, false);
        clip.SetData(pcm48, 0);
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void Start()
    {
        Speak();
    }
}

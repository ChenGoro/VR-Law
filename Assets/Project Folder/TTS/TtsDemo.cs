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
        Debug.Log("[TTS]Using AndroidPiperTtsService");
#else
        _tts = new EditorStubTtsService();
        Debug.Log("[TTS]Using EditorStubTtsService");
#endif
    }

    [ContextMenu("Speak")]
    // TtsDemo.cs
    [ContextMenu("Speak")]
    public async void Speak()
    {
        try
        {
            Debug.Log("[TTS]Speak called");
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; audioSource.volume = 1f; audioSource.mute = false;
            UnityEngine.AudioListener.pause = false; UnityEngine.AudioListener.volume = 1f;

            var pcm48 = await _tts.Synthesize48kAsync(text, voiceId);
            if (pcm48 == null || pcm48.Length == 0)
                throw new System.Exception("[TTS] Got 0 samples from TTS");

            var clip = AudioClip.Create("TTS", pcm48.Length, 1, 48000, false);
            clip.SetData(pcm48, 0);
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"[TTS] Playing clip len={clip.samples} @ {clip.frequency}Hz");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TTS] Speak failed: " + ex);

            // fallback beep so we can confirm audio path is OK
            int sr = 48000, samples = (int)(0.25f * sr);
            var beep = new float[samples];
            double f = 880.0, t = 0.0, dt = 1.0 / sr;
            for (int i = 0; i < samples; i++) { beep[i] = (float)(System.Math.Sin(2 * System.Math.PI * f * t) * 0.4); t += dt; }
            var clip = AudioClip.Create("Beep", beep.Length, 1, sr, false);
            clip.SetData(beep, 0);
            audioSource.clip = clip;
            audioSource.Play();
        }
    }


    private void Start()
    {
        Speak();
    }
}


using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class TtsCaller : MonoBehaviour
{
    public static TtsCaller I { get; private set; }

    [Header("Piper")]
    [SerializeField] private string defaultVoiceId = "en_US-joe-medium";
    [SerializeField] private float defaultVolume = 1f;

    private AudioSource _audio;
#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidPiperTtsService _svc;
#else
    private ITtsService _svc; // optional: a stub for editor
#endif

    private CancellationTokenSource _speakCts; // used by SpeakNow

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _audio = gameObject.GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f; // 2D

#if UNITY_ANDROID && !UNITY_EDITOR
        _svc = new AndroidPiperTtsService();
#else
        _svc = new EditorNoopTtsService(); // optional stub below
#endif
    }

    // Fire-and-forget speak; does NOT cancel a previous one.
    public void Speak(string text, string voiceId = null, float? volume = null)
        => SpeakAsync(text, voiceId ?? defaultVoiceId, volume ?? defaultVolume, this.GetCancellationTokenOnDestroy()).Forget();

    // Speak but cancel any current synth/playback first.
    public void SpeakNow(string text, string voiceId = null, float? volume = null)
    {
        Cancel(); // cancels current synth + stops audio
        _speakCts = new CancellationTokenSource();
        SpeakAsync(text, voiceId ?? defaultVoiceId, volume ?? defaultVolume, _speakCts.Token).Forget();
    }


    private async UniTaskVoid SpeakAsync(string text, string voiceId, float volume, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            var samples = await _svc.Synthesize48kAsync(text, voiceId, ct);
            if (samples == null || samples.Length == 0) return;
            ct.ThrowIfCancellationRequested();

            // make sure we’re on the main thread for Unity APIs
            await UniTask.SwitchToMainThread(ct);

            var clip = AudioClip.Create($"TTS_{voiceId}", samples.Length, 1, 48000, false);
            clip.SetData(samples, 0);

            _audio.volume = volume;
            _audio.PlayOneShot(clip);
        }
        catch (System.OperationCanceledException) { /* cancelled */ }
        catch (System.Exception e) { Debug.LogError("[TTS] Speak failed: " + e); }
    }

    // cancel synth + stopAudio=true stop audio, stopAudio=let current clip finish
    public void Cancel(bool stopAudio = true)
    {
        _speakCts?.Cancel();
        _speakCts?.Dispose();
        _speakCts = null;

        if (stopAudio && _audio != null && _audio.isPlaying)
            _audio.Stop();
    }

#if !UNITY_ANDROID || UNITY_EDITOR
    // Minimal editor stub so you can call Speak in Play Mode without Android libs.
    private class EditorNoopTtsService : ITtsService
    {
        public async System.Threading.Tasks.Task<float[]> Synthesize48kAsync(string text, string voiceId, CancellationToken ct = default)
        {
            Debug.Log($"[TTS Editor Stub] Would speak: \"{text}\" (voice={voiceId})");
            await UniTask.Yield(); // keep the await shape
            return System.Array.Empty<float>();
        }
    }
#endif
}

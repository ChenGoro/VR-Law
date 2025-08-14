// AndroidPiperTtsService.cs
// No using directives needed; everything is fully qualified.

#if UNITY_ANDROID && !UNITY_EDITOR
public sealed class AndroidPiperTtsService : ITtsService
{
    const int PIPER_OK = 0;
    const int PIPER_DONE = 1;

    // ---------- Native bindings ----------
    [System.Runtime.InteropServices.DllImport("piper",
        EntryPoint = "piper_create",
        CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern System.IntPtr piper_create(
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPUTF8Str)] string model_path,
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPUTF8Str)] string config_path,
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPUTF8Str)] string espeak_data_path);

    [System.Runtime.InteropServices.DllImport("piper",
        EntryPoint = "piper_free",
        CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern void piper_free(System.IntPtr synth);

    [System.Runtime.InteropServices.DllImport("piper",
        EntryPoint = "piper_default_synthesize_options",
        CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern PiperSynthesizeOptions piper_default_synthesize_options(System.IntPtr synth);

    [System.Runtime.InteropServices.DllImport("piper",
        EntryPoint = "piper_synthesize_start",
        CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern int piper_synthesize_start(
        System.IntPtr synth,
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPUTF8Str)] string text,
        ref PiperSynthesizeOptions options);

    [System.Runtime.InteropServices.DllImport("piper",
        EntryPoint = "piper_synthesize_next",
        CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern int piper_synthesize_next(
        System.IntPtr synth,
        ref PiperAudioChunk chunk);

    // ---------- Structs ----------
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct PiperAudioChunk
    {
        public System.IntPtr samples;         // float*
        public System.UIntPtr num_samples;    // size_t
        public int sample_rate;               // Hz
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        public bool is_last;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct PiperSynthesizeOptions
    {
        public int   speaker_id;
        public float length_scale;
        public float noise_scale;
        public float noise_w_scale;
    }

    // ---------- Instance state ----------
    System.IntPtr _synth = System.IntPtr.Zero;
    string _loadedVoiceId = null;
    string _voiceModelPath, _voiceConfigPath, _espeakDataPath;

    public async System.Threading.Tasks.Task<float[]> Synthesize48kAsync(
        string text, string voiceId, System.Threading.CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return System.Array.Empty<float>();

        // 1) Ensure assets exist on device
        await EnsureAssetsAsync(voiceId, ct);

        // 2) (Re)create synthesizer if voice changed
        if (_synth == System.IntPtr.Zero || _loadedVoiceId != voiceId)
        {
            if (_synth != System.IntPtr.Zero) { piper_free(_synth); _synth = System.IntPtr.Zero; }
            _synth = piper_create(_voiceModelPath, _voiceConfigPath, _espeakDataPath);
            if (_synth == System.IntPtr.Zero)
                throw new System.Exception("piper_create failed (null synthesizer)");
            _loadedVoiceId = voiceId;
        }

        // 3) Options
        var opts = piper_default_synthesize_options(_synth);

        // 4) Stream chunks
        int rc = piper_synthesize_start(_synth, text, ref opts);
        if (rc != PIPER_OK) throw new System.Exception($"piper_synthesize_start failed rc={rc}");

        var pcm = new System.Collections.Generic.List<float>(64 * 1024);
        int srcRate = 22050;
        var chunk = new PiperAudioChunk();

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            int step = piper_synthesize_next(_synth, ref chunk);
            if (step == PIPER_DONE) break;
            if (step != PIPER_OK) throw new System.Exception($"piper_synthesize_next rc={step}");

            int n = checked((int)chunk.num_samples);
            if (n > 0 && chunk.samples != System.IntPtr.Zero)
            {
                var buf = new float[n];
                System.Runtime.InteropServices.Marshal.Copy(chunk.samples, buf, 0, n);
                pcm.AddRange(buf);
                srcRate = chunk.sample_rate;
            }
            if (chunk.is_last) break;
        }

        var mono = pcm.ToArray();
        var out48 = AudioResampler.Resample(mono, srcRate, 48000);
        return out48;
    }

    // ---------- Asset install via manifests ----------
    async System.Threading.Tasks.Task EnsureAssetsAsync(string voiceId, System.Threading.CancellationToken ct)
    {
        string baseSA = UnityEngine.Application.streamingAssetsPath.Replace("\\", "/");
        string basePD = UnityEngine.Application.persistentDataPath.Replace("\\", "/");

        // Voice
        string saVoiceDir = $"{baseSA}/piper/voices/{voiceId}";
        string pdVoiceDir = $"{basePD}/piper/voices/{voiceId}";
        System.IO.Directory.CreateDirectory(pdVoiceDir);
        await CopyUsingManifestAsync($"{saVoiceDir}/.manifest.txt", saVoiceDir, pdVoiceDir, ct);

        _voiceModelPath  = $"{pdVoiceDir}/{voiceId}.onnx";
        _voiceConfigPath = $"{pdVoiceDir}/{voiceId}.onnx.json";

        // espeak-ng-data
        string saEspeak = $"{baseSA}/piper/espeak-ng-data";
        string pdEspeak = $"{basePD}/piper/espeak-ng-data";
        System.IO.Directory.CreateDirectory(pdEspeak);
        await CopyUsingManifestAsync($"{saEspeak}/.manifest.txt", saEspeak, pdEspeak, ct);
        _espeakDataPath = pdEspeak;
    }

    static async System.Threading.Tasks.Task CopyUsingManifestAsync(
        string manifestUrl, string srcRootUrl, string dstRoot, System.Threading.CancellationToken ct)
    {
        // fetch manifest
        string manifest;
        using (var req = UnityEngine.Networking.UnityWebRequest.Get(manifestUrl))
        {
            var dh = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.downloadHandler = dh;
            var op = req.SendWebRequest();
            while (!op.isDone) { ct.ThrowIfCancellationRequested(); await System.Threading.Tasks.Task.Yield(); }
#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
                throw new System.Exception("Missing manifest: " + manifestUrl);
            manifest = System.Text.Encoding.UTF8.GetString(dh.data);
        }

        var lines = manifest.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var rel in lines)
        {
            string src = $"{srcRootUrl}/{rel}".Replace("\\", "/");
            string dst = System.IO.Path.Combine(dstRoot, rel).Replace("\\", "/");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dst));
            using (var req = UnityEngine.Networking.UnityWebRequest.Get(src))
            {
                req.downloadHandler = new UnityEngine.Networking.DownloadHandlerFile(dst) { removeFileOnAbort = true };
                var op = req.SendWebRequest();
                while (!op.isDone) { ct.ThrowIfCancellationRequested(); await System.Threading.Tasks.Task.Yield(); }
#if UNITY_2020_3_OR_NEWER
                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                    throw new System.Exception($"Failed to copy {src} -> {dst}: {req.error}");
            }
        }
    }
}
#endif

// AndroidPiperTtsService.cs
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.Networking;

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

    // Quick check that required files exist (per voice)
    static void LogPiperAssets(string root, string voiceId)
    {
        string es = System.IO.Path.Combine(root, "espeak-ng-data");
        string vd = System.IO.Path.Combine(root, "voices", voiceId);

        bool ExistsFile(string p) => System.IO.File.Exists(p);
        bool ExistsDir (string p) => System.IO.Directory.Exists(p);

        Debug.Log("[Piper] Exists espeak-ng-data=" + ExistsDir(es));
        Debug.Log("[Piper] Exists phondata=" + ExistsFile(System.IO.Path.Combine(es, "phondata")));
        Debug.Log("[Piper] Exists phontab=" + ExistsFile(System.IO.Path.Combine(es, "phontab")));
        Debug.Log("[Piper] Exists pa_dict=" + ExistsFile(System.IO.Path.Combine(es, "pa_dict")));
        Debug.Log("[Piper] Exists pap_dict=" + ExistsFile(System.IO.Path.Combine(es, "pap_dict")));
        Debug.Log("[Piper] Model=" + ExistsFile(System.IO.Path.Combine(vd, voiceId + ".onnx")));
        Debug.Log("[Piper] Config=" + ExistsFile(System.IO.Path.Combine(vd, voiceId + ".onnx.json")));
    }

    // Optional finalizer — free native handle if the app tears down without Dispose
    ~AndroidPiperTtsService()
    {
        try { if (_synth != System.IntPtr.Zero) piper_free(_synth); } catch {}
        _synth = System.IntPtr.Zero;
    }

    public async System.Threading.Tasks.Task<float[]> Synthesize48kAsync(
        string text, string voiceId, System.Threading.CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return System.Array.Empty<float>();

        // 1) Ensure assets exist on device (creates paths used below)
        await EnsureAssetsAsync(voiceId, ct);

        // Optional: sanity log
        LogPiperAssets(Application.persistentDataPath.Replace("\\", "/") + "/piper", voiceId);

        // switch to a background thread for native synth & resample
        await Cysharp.Threading.Tasks.UniTask.SwitchToThreadPool();

        // 2) Create (or reuse) the native synth for this voice
        if (_synth == System.IntPtr.Zero || _loadedVoiceId != voiceId)
        {
            if (_synth != System.IntPtr.Zero) { piper_free(_synth); _synth = System.IntPtr.Zero; }
            try
            {
                Debug.Log($"[Piper] calling piper_create(model={_voiceModelPath}, config={_voiceConfigPath}, espeak={_espeakDataPath})");
                _synth = piper_create(_voiceModelPath, _voiceConfigPath, _espeakDataPath);
            }
            catch (System.DllNotFoundException e)        { Debug.LogError("[Piper] DllNotFoundException: " + e.Message); }
            catch (System.EntryPointNotFoundException e) { Debug.LogError("[Piper] EntryPointNotFoundException: " + e.Message); }
            catch (System.Exception e)                   { Debug.LogError("[Piper] piper_create threw: " + e); }

            if (_synth == System.IntPtr.Zero)
            {
                Debug.LogError("[Piper] piper_create returned NULL (native init failed). " +
                               "Check .so placement/ABI and look for 'dlopen' / 'UnsatisfiedLinkError' in logcat.");
                return System.Array.Empty<float>();
            }
            _loadedVoiceId = voiceId;
            Debug.Log("[Piper] piper_create OK");
        }

        // 3) Options
        var opts = piper_default_synthesize_options(_synth);

        // 4) Stream chunks
        int rc = piper_synthesize_start(_synth, text, ref opts);
        if (rc != PIPER_OK) throw new System.Exception($"piper_synthesize_start failed rc={rc}");
        Debug.Log("[Piper] piper_synthesize_start OK");

        var pcm = new System.Collections.Generic.List<float>(64 * 1024);
        int srcRate = 22050;
        var chunk = new PiperAudioChunk();

        Debug.Log("[Piper] entering synthesis loop");
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            int step = piper_synthesize_next(_synth, ref chunk);
            if (step == PIPER_DONE) { Debug.Log("[Piper] DONE"); break; }
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
        if (out48.Length == 0)
            throw new System.Exception("[Piper] Synthesis produced 0 samples");

        return out48;
    }

    // ---------- Asset install via manifests ----------
    async System.Threading.Tasks.Task EnsureAssetsAsync(string voiceId, System.Threading.CancellationToken ct)
    {
        string baseSA = Application.streamingAssetsPath.Replace("\\", "/");
        string basePD = Application.persistentDataPath.Replace("\\", "/");

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

        // Optional sanity check of critical files
        string[] must = { "phondata", "phontab", "pa_dict", "pap_dict" };
        foreach (var f in must)
        {
            var path = System.IO.Path.Combine(pdEspeak, f);
            if (!System.IO.File.Exists(path))
                throw new System.Exception($"[Piper] Missing espeak-ng-data file: {f} at {path}");
        }
    }

    static async System.Threading.Tasks.Task CopyUsingManifestAsync(
        string manifestUrl, string srcRootUrl, string dstRoot, System.Threading.CancellationToken ct)
    {
        // 1) Fetch manifest
        string manifest;
        using (var req = UnityWebRequest.Get(manifestUrl))
        {
            var dh = new DownloadHandlerBuffer();
            req.downloadHandler = dh;
            var op = req.SendWebRequest();
            while (!op.isDone) { ct.ThrowIfCancellationRequested(); await System.Threading.Tasks.Task.Yield(); }
#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
                throw new System.Exception("Missing manifest: " + manifestUrl);
            manifest = System.Text.Encoding.UTF8.GetString(dh.data);
        }

        // 2) Copy each file listed in the manifest (idempotent)
        var lines = manifest.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var rel in lines)
        {
            string src = $"{srcRootUrl}/{rel}".Replace("\\", "/");
            string dst = System.IO.Path.Combine(dstRoot, rel).Replace("\\", "/");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dst));

            // Skip if already present
            if (System.IO.File.Exists(dst)) continue;

            using (var req = UnityWebRequest.Get(src))
            {
                req.downloadHandler = new DownloadHandlerFile(dst) { removeFileOnAbort = true };
                var op = req.SendWebRequest();
                while (!op.isDone) { ct.ThrowIfCancellationRequested(); await System.Threading.Tasks.Task.Yield(); }
#if UNITY_2020_3_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                    throw new System.Exception($"Failed to copy {src} -> {dst}: {req.error}");
            }
        }
    }
}
#endif

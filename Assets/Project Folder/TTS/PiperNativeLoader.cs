#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

public static class PiperNativeLoader
{
    static bool _loaded;

    // load as early as possible
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preload()
    {
        if (_loaded) return;
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer?.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null)
            {
                Debug.LogWarning("[Piper] UnityPlayer.currentActivity is null; falling back to System.loadLibrary only");
                SystemLoadChain(null);
            }
            else
            {
                using var appInfo = activity.Call<AndroidJavaObject>("getApplicationInfo");
                var libDir = appInfo?.Get<string>("nativeLibraryDir"); // /data/app/.../lib/arm64
                if (string.IsNullOrEmpty(libDir))
                {
                    Debug.LogWarning("[Piper] nativeLibraryDir unavailable; falling back to System.loadLibrary only");
                    SystemLoadChain(null);
                }
                else
                {
                    Debug.Log("[Piper] nativeLibraryDir=" + libDir);
                    SystemLoadChain(libDir);
#if UNITY_EDITOR // (won't run, but keeps analyzers happy)
#endif
                }
            }
            _loaded = true;
            Debug.Log("[Piper] native libs loaded");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Piper] Preload fatal: " + e);
        }
    }

    static void SystemLoadChain(string libDirOrNull)
    {
        using var sys = new AndroidJavaClass("java.lang.System");

        // 1) libc++_shared (idempotent)
        TryLoad(sys, libDirOrNull == null ? null : $"{libDirOrNull}/libc++_shared.so", "libc++_shared");
        TryLoadLibrary(sys, "c++_shared"); // some ROMs expose this alias

        // 2) onnxruntime
        if (!TryLoadLibrary(sys, "onnxruntime") && libDirOrNull != null)
            TryLoad(sys, $"{libDirOrNull}/libonnxruntime.so", "onnxruntime");

        // 3) piper
        if (!TryLoadLibrary(sys, "piper") && libDirOrNull != null)
            TryLoad(sys, $"{libDirOrNull}/libpiper.so", "piper");

        // optional: directory dump once (useful, but not spammy)
        if (libDirOrNull != null)
        {
            try
            {
                var files = System.IO.Directory.GetFiles(libDirOrNull);
                Debug.Log("[Piper] nativeLibraryDir contains: " + string.Join(", ", files));
            }
            catch { /* ignore */ }
        }
    }

    static bool TryLoadLibrary(AndroidJavaClass sys, string name)
    {
        try { sys.CallStatic("loadLibrary", name); Debug.Log("[Piper] loadLibrary(" + name + ") OK"); return true; }
        catch (System.Exception e) { Debug.LogWarning("[Piper] loadLibrary(" + name + ") failed: " + e.Message); return false; }
    }

    static bool TryLoad(AndroidJavaClass sys, string absPath, string tag)
    {
        if (string.IsNullOrEmpty(absPath)) return false;
        try { sys.CallStatic("load", absPath); Debug.Log("[Piper] load(abs " + tag + ") OK"); return true; }
        catch (System.Exception e) { Debug.LogWarning("[Piper] load(abs " + tag + ") failed: " + e.Message); return false; }
    }
}
#endif

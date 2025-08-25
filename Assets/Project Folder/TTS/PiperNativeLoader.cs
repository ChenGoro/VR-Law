#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

public static class PiperNativeLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preload()
    {
        try
        {
            using var build = new AndroidJavaClass("android.os.Build");
            var abis = build.GetStatic<string[]>("SUPPORTED_ABIS");
            Debug.Log("[Piper] SUPPORTED_ABIS=" + string.Join(",", abis ?? new string[0]));

            string libDir = null;
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var appInfo = activity.Call<AndroidJavaObject>("getApplicationInfo"))
            {
                libDir = appInfo.Get<string>("nativeLibraryDir");
                Debug.Log("[Piper] nativeLibraryDir=" + libDir);
            }

            using var sys = new AndroidJavaClass("java.lang.System");

            // onnxruntime
            bool ok = false;
            try { sys.CallStatic("loadLibrary", "onnxruntime"); ok = true; Debug.Log("[Piper] loadLibrary(onnxruntime) OK"); }
            catch (System.Exception e) { Debug.LogWarning("[Piper] loadLibrary(onnxruntime) failed: " + e.Message); }

            if (!ok && !string.IsNullOrEmpty(libDir))
            {
                try { sys.CallStatic("load", libDir + "/libonnxruntime.so"); ok = true; Debug.Log("[Piper] load(abs onnxruntime) OK"); }
                catch (System.Exception e) { Debug.LogWarning("[Piper] load(abs onnxruntime) failed: " + e.Message); }
            }

            // piper
            bool ok2 = false;
            try { sys.CallStatic("loadLibrary", "piper"); ok2 = true; Debug.Log("[Piper] loadLibrary(piper) OK"); }
            catch (System.Exception e) { Debug.LogWarning("[Piper] loadLibrary(piper) failed: " + e.Message); }

            if (!ok2 && !string.IsNullOrEmpty(libDir))
            {
                try { sys.CallStatic("load", libDir + "/libpiper.so"); ok2 = true; Debug.Log("[Piper] load(abs piper) OK"); }
                catch (System.Exception e) { Debug.LogWarning("[Piper] load(abs piper) failed: " + e.Message); }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Piper] Preload fatal: " + e);
        }
    }
}
#endif

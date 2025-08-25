#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
public static class PiperNativeLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preload()
    {
        try {
            using (var sys = new AndroidJavaClass("java.lang.System"))
            {
                sys.CallStatic("loadLibrary", "onnxruntime");
                sys.CallStatic("loadLibrary", "piper");
            }
            Debug.Log("[Piper] System.loadLibrary OK for onnxruntime & piper");
        } catch (System.Exception e) {
            Debug.LogError("[Piper] System.loadLibrary failed: " + e);
        }
    }
}
#endif

using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BackfaceBlurBinder : MonoBehaviour
{
    private static readonly int BlurTexId = Shader.PropertyToID("_BlurTex");

    private Renderer _r; private MaterialPropertyBlock _mpb;
    private int _logged; // 0 = not yet, 1 = seen tex, -1 = logged null

    private void Awake()
    { _r = GetComponent<Renderer>(); _mpb = new MaterialPropertyBlock(); }

    private void LateUpdate()
    {
        var tex = Shader.GetGlobalTexture(BlurTexId);
        if (tex == null)
        {
            if (_logged != -1) { Debug.LogWarning("[BackfaceBlurBinder] _BlurTex is NULL"); _logged = -1; }
            return;
        }

        if (_logged != 1)
        {
            if (tex is RenderTexture rt)
                Debug.Log($"[BackfaceBlurBinder] _BlurTex bound: {rt.width}x{rt.height}, {rt.format}");
            else
                Debug.Log("[BackfaceBlurBinder] _BlurTex is non-RT texture");
            _logged = 1;
        }

        _r.GetPropertyBlock(_mpb);
        _mpb.SetTexture(BlurTexId, tex);
        _r.SetPropertyBlock(_mpb);
    }
}

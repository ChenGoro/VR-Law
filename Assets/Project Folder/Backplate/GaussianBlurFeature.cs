using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianBlurFeature : ScriptableRendererFeature
{
    public enum Downsample { Full = 1, Half = 2, Quarter = 4 }

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingTransparents;
        public Downsample downsample = Downsample.Quarter;
        [Range(0.5f, 10f)] public float sigma = 2.5f;
        public string globalTextureName = "_BlurTex";
        public bool debugBindCameraColor = false;   // <-- add
    }

    private class BlurPass : ScriptableRenderPass
    {
        private readonly Settings settings;
        private readonly Material mat; // Hidden/UIBackface/GaussianBlur
        private readonly ProfilingSampler profiler = new ProfilingSampler("UIBackface Gaussian Blur");

        private RTHandle rtA, rtB;
        private int globalTexID;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData data)
        {
            // declare we need camera color
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public BlurPass(Settings settings, Material mat)
        {
            this.settings = settings;
            this.mat = mat;
            renderPassEvent = settings.passEvent;
            globalTexID = Shader.PropertyToID(settings.globalTextureName);
        }

        public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
        {
            Debug.Log("GaussianBlurFeature.Execute");

            if (mat == null) return;

            var cmd = CommandBufferPool.Get("GaussianBlurFeature");
            using (new ProfilingScope(cmd, profiler))
            {
                var renderer = data.cameraData.renderer;
                var src = renderer.cameraColorTargetHandle;

                // allocate downsampled RTs
                var desc = data.cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;

                int scale = (int)settings.downsample;
                desc.width = Mathf.Max(1, desc.width / scale);
                desc.height = Mathf.Max(1, desc.height / scale);

                RenderingUtils.ReAllocateIfNeeded(ref rtA, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_UIBlurA");
                RenderingUtils.ReAllocateIfNeeded(ref rtB, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_UIBlurB");

                // set shared uniforms
                Vector2 texel = new Vector2(1f / desc.width, 1f / desc.height);
                mat.SetVector("_TexelSize", texel);
                mat.SetFloat("_Sigma", settings.sigma);

                // horizontal pass
                mat.SetVector("_Direction", new Vector2(1, 0));
                Blitter.BlitCameraTexture(cmd, src, rtA, mat, 0);

                // vertical pass
                mat.SetVector("_Direction", new Vector2(0, 1));
                Blitter.BlitCameraTexture(cmd, rtA, rtB, mat, 0);

                // expose to all shaders
                cmd.SetGlobalTexture(globalTexID, rtB);
            }

            ctx.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            rtA?.Release();
            rtB?.Release();
        }
    }

    public Settings settings = new Settings();

    private Material _mat;
    private BlurPass _pass;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/UIBackface/GaussianBlur");
        if (shader == null) { Debug.LogError("GaussianBlur shader not found (Hidden/UIBackface/GaussianBlur)"); return; }
        _mat = CoreUtils.CreateEngineMaterial(shader);
        _pass = new BlurPass(settings, _mat);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_mat == null) return;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_mat);
        _pass?.Dispose();
    }
}

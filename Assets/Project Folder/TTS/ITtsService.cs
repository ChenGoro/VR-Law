using System;
using System.Threading;
using System.Threading.Tasks;

public interface ITtsService
{
    Task<float[]> Synthesize48kAsync(string text, string voiceId, CancellationToken ct = default);
}

#if UNITY_EDITOR || UNITY_STANDALONE
public sealed class EditorStubTtsService : ITtsService
{
    public async Task<float[]> Synthesize48kAsync(string text, string voiceId, CancellationToken ct = default)
    {
        await Task.Yield();
        int sr = 48000;
        string[] words = (text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int samples = Math.Max(1, words.Length) * (int)(0.2f * sr);
        var data = new float[samples];
        double f = 440.0, t = 0.0, dt = 1.0 / sr;
        for (int i = 0; i < samples; i++) { data[i] = (float)(Math.Sin(2 * Math.PI * f * t) * 0.15); t += dt; }
        return data;
    }
}
#endif

public static class AudioResampler
{
    public static float[] Resample(float[] src, int srcRate, int dstRate)
    {
        if (srcRate == dstRate) return (float[])src.Clone();
        double ratio = (double)srcRate / dstRate;
        int dstLen = (int)Math.Ceiling(src.Length / ratio);
        var dst = new float[dstLen];
        for (int i = 0; i < dstLen; i++)
        {
            double sp = i * ratio;
            int i0 = (int)sp; int i1 = Math.Min(i0 + 1, src.Length - 1);
            double f = sp - i0;
            dst[i] = (float)((1 - f) * src[i0] + f * src[i1]);
        }
        return dst;
    }
}

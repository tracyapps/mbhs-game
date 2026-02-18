using UnityEngine;

namespace MBHS.Systems.FormationEditor
{
    public static class WaveformGenerator
    {
        public static Texture2D Generate(AudioClip clip, int width, int height, Color waveColor)
        {
            if (clip == null || width <= 0 || height <= 0)
                return GeneratePlaceholder(width, height);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            int channelCount = clip.channels;
            int samplesPerPixel = samples.Length / width;
            int halfHeight = height / 2;

            var bgColor = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[width * height];

            // Fill transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = bgColor;

            for (int x = 0; x < width; x++)
            {
                int startSample = x * samplesPerPixel;
                int endSample = Mathf.Min(startSample + samplesPerPixel, samples.Length);

                float maxAmp = 0f;
                for (int s = startSample; s < endSample; s += channelCount)
                {
                    float amp = Mathf.Abs(samples[s]);
                    if (amp > maxAmp) maxAmp = amp;
                }

                int barHeight = Mathf.RoundToInt(maxAmp * halfHeight);
                barHeight = Mathf.Clamp(barHeight, 1, halfHeight);

                for (int y = halfHeight - barHeight; y <= halfHeight + barHeight; y++)
                {
                    if (y >= 0 && y < height)
                        pixels[y * width + x] = waveColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2D GeneratePlaceholder(int width, int height)
        {
            if (width <= 0) width = 256;
            if (height <= 0) height = 64;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color[width * height];
            var bgColor = new Color(0.15f, 0.15f, 0.18f, 0.5f);
            var lineColor = new Color(0.25f, 0.25f, 0.3f, 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Center line
                    if (y == height / 2)
                        pixels[y * width + x] = lineColor;
                    else
                        pixels[y * width + x] = bgColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}

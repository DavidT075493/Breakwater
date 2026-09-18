using UnityEngine;
using System.Collections;

public static class Noise {

	public enum NormalizeMode {Local, Global};

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
		float[,] noiseMap = new float[mapWidth,mapHeight];

		System.Random prng = new System.Random (seed);
		Vector2[] octaveOffsets = new Vector2[octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.Next (-100000, 100000) + offset.x;
			float offsetY = prng.Next (-100000, 100000) - offset.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}

		if (scale <= 0) {
			scale = 0.0001f;
		}

		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;


		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++) {
					float sampleX = (x-halfWidth + octaveOffsets[i].x) / scale * frequency;
					float sampleY = (y-halfHeight + octaveOffsets[i].y) / scale * frequency;

					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				if (noiseHeight > maxLocalNoiseHeight) {
					maxLocalNoiseHeight = noiseHeight;
				} else if (noiseHeight < minLocalNoiseHeight) {
					minLocalNoiseHeight = noiseHeight;
				}
				noiseMap [x, y] = noiseHeight;
			}
		}

		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				if (normalizeMode == NormalizeMode.Local) {
					noiseMap [x, y] = Mathf.InverseLerp (minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap [x, y]);
				} else {
					float normalizedHeight = (noiseMap [x, y] + 1) / (maxPossibleHeight/0.9f);
					noiseMap [x, y] = Mathf.Clamp(normalizedHeight,0, int.MaxValue);
				}
			}
		}

		return noiseMap;
	}


    public static int[,] GenerateRadialBiomeMap(
    int width,
    int height,
    int seed,
    int biomeCount,
    Vector2 offset,
    float warpScale,
    float warpStrength,
    float angularNoiseScale,
    float angularNoiseStrength
)
    {
        int[,] biomeMap = new int[width, height];

        System.Random prng = new System.Random(seed);

        Vector2 center = new Vector2(
            width * (0.45f + (float)prng.NextDouble() * 0.1f),
            height * (0.45f + (float)prng.NextDouble() * 0.1f)
        );

        float rotationOffset = (float)prng.NextDouble() * Mathf.PI * 2f;
        float anglePerBiome = (Mathf.PI * 2f) / biomeCount;
        float twoPi = Mathf.PI * 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // --- Domain warp (smooth, no islands)
                float wx = Mathf.PerlinNoise(
                    (x + offset.x) / warpScale,
                    (y + offset.y) / warpScale
                ) * 2f - 1f;

                float wy = Mathf.PerlinNoise(
                    (x + offset.x + 1000f) / warpScale,
                    (y + offset.y + 1000f) / warpScale
                ) * 2f - 1f;

                Vector2 warped = new Vector2(x, y) + new Vector2(wx, wy) * warpStrength;
                Vector2 p = warped - center;

                // --- Base angle
                float angle = Mathf.Atan2(p.y, p.x);

                // --- Low-frequency angular noise ONLY
                float angularNoise = Mathf.PerlinNoise(
                    (p.x + offset.x) / angularNoiseScale,
                    (p.y + offset.y) / angularNoiseScale
                ) * 2f - 1f;

                angle += angularNoise * angularNoiseStrength;
                angle += rotationOffset;

                // --- Proper wrap to [0, 2π)
                angle = (angle % twoPi + twoPi) % twoPi;

                int biomeIndex = Mathf.FloorToInt(angle / anglePerBiome);
                biomeIndex %= biomeCount; // 👈 critical, NOT clamp

                biomeMap[x, y] = biomeIndex;
            }
        }

        return biomeMap;
    }

}

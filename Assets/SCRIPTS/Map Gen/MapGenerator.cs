using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : NetworkBehaviour
{
    public const int islandCount = 9;

    public static MapGenerator instance;
    public const float islandBoundsMultiplier = 0.8f;
    [Tooltip("Skips the map generation, used for testing")]
    public bool dontGenerate;

    public enum DrawMode { HillMap, ColorMap, ValeBiome };
    public DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

    //size 500 for rivers
    public const int mapChunkSize = 500;
    public Transform valeLocation;
    public const int valeMapSize = 650;
    [Range(0, 6)]
    public int editorPreviewLOD;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public Vector2 offset;

    public bool useFalloff;


    public bool autoUpdate;


    //float[,] falloffMap;
    public GameObject planePreview;

    [Header("Biomes")]
    public float noiseScaleGrassy = 67.5f;
    public float noiseScaleSwamp = 37.5f;
    public float noiseScaleDesert = 77.5f;
    public float noiseScaleSnow = 70;
    public float noiseScaleVolcanic = 50;
    public float noiseScaleVale = 60;

    public enum biome
    {
        grassy,
        swamp,
        desert,
        snow,
        volcanic,
        vale

    }

    public TerrainType[] regions;
    public TerrainType[] swampRegions;
    public TerrainType[] desertRegions;
    public TerrainType[] snowRegions;
    public TerrainType[] volcanicRegions;
    
    public RegionGroup[] valeRegions;

    Dictionary<int, int> biomeIslandCounts = new Dictionary<int, int>();
    public int[] minBiomeCounts;

    public HillData[] hillData;

    public Tilemap hillsTilemap;

    static readonly Color[] biomeDebugColors =
{
    new Color(0, 0.5f, 1),  // Region 0
    new Color(0.8f, 0.8f, 0.8f), // Region 1
    new Color(0.9f, 0.1f, 0f),  // Region 2
};

    [Header("Rivers")]
    public int shallowWaterIndex;
    public int[] deepWaterIndex;
    public bool doRivers;
    public int generatingRiverIndex;
    public River[] riverData = new River[10];
    public Tilemap riverTilemap;
    [Range(0, 9)]
    public int riverSetIndex;

    [Header("Islands")]
    public List<Island> islands = new List<Island>();
    [HideInInspector] public bool finishedCreatingTiles;
    public bool creatingIsland;
    int[] islandBiomes;

    public biome editorGenerateBiome;

    void Awake()
    {
        //gameManager.instance.ToggleDefaultLayerCulling(false);
        //falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
        instance = this;
        planePreview.gameObject.SetActive(false);
    }

    public void CreateMap(int Seed)
    {

        MapDisplay display = FindAnyObjectByType<MapDisplay>();
        display.ClearTiles();

        //last digit of seed
        generatingRiverIndex = int.Parse(Seed.ToString().Last().ToString());

        StartCoroutine(_GenerateIslands(Seed));

    }

    public TerrainType[] GetRegionsFromBiome(biome Biome, int radialBiome = 0)
    {
        switch (Biome)
        {
            case biome.grassy:
                return regions;
            case biome.swamp:
                return swampRegions;
            case biome.desert:
                return desertRegions;
            case biome.snow:
                return snowRegions;
            case biome.volcanic:
                return volcanicRegions;
            case biome.vale:
                return valeRegions[radialBiome].regions;
        }
        return null;
    }


    IEnumerator _GenerateIslands(int Seed)
    {
        MapDisplay display = FindAnyObjectByType<MapDisplay>();

        Vector2Int potentialPosition = Vector2Int.zero;

        yield return new WaitForSeconds(0.3f);

        //generate islands
        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            islands.Add(new Island(mapChunkSize, Vector2Int.zero, null, (int)gameManager.instance.startBiome, 0));

            if (!gameManager.instance.disableIslands)
            {
                int islandsAmount = islandCount;
                islandBiomes = new int[islandsAmount];

                //make sure all biomes appear once

                int loopCount = 0;

                while (true)
                {
                    loopCount++;

                    for (int i = 0; i < islandsAmount; i++)
                    {

                        islandBiomes[i] = UnityEngine.Random.Range(0, Enum.GetValues(typeof(biome)).Length - 1);
                        if (biomeIslandCounts.ContainsKey(islandBiomes[i]))
                        {
                            biomeIslandCounts[islandBiomes[i]]++;
                        }
                        else
                        {
                            biomeIslandCounts.TryAdd(islandBiomes[i], 1);
                        }
                    }

                    bool allBiomes = true;

                    for (int i = 0; i < Enum.GetValues(typeof(biome)).Length - 1; i++)
                    {
                        if (!islandBiomes.Contains(i))
                        {
                            allBiomes = false;
                        }

                        //make sure count isnt less than 2
                        int count = 0;
                        foreach (int biomeI in islandBiomes)
                        {
                            if (biomeI == i)
                            {
                                count++;
                            }
                        }
                        if (count < minBiomeCounts[i]) allBiomes = false;

                    }
                    //break loop
                    if (allBiomes)
                    {
                        //print("all biomes");
                        break;
                    }

                    if (loopCount > 1000)
                    {
                        Debug.LogError("biome picking failsafe");
                        break;
                    }

                }
                // 1/2 length and width of the square which islands can spawn
                int posDiameter = 900;

                //start creating islands
                for (int i = 0; i < islandsAmount; i += 1)
                {
                    bool canPlaceIsland = false;
                    int failsafe = 0;

                    int islandSize = UnityEngine.Random.Range(380, 460);

                    while (!canPlaceIsland)
                    {
                        potentialPosition = new Vector2Int(UnityEngine.Random.Range(-posDiameter, posDiameter), UnityEngine.Random.Range(-posDiameter, posDiameter));

                        while (potentialPosition.magnitude < 600)
                        {
                            potentialPosition = new Vector2Int(UnityEngine.Random.Range(-posDiameter, posDiameter), UnityEngine.Random.Range(-posDiameter, posDiameter));
                        }
                        bool validSpot = true;

                        foreach (Island isl in islands)
                        {
                            if (Vector2.Distance(potentialPosition, isl.center) < 2 * (Mathf.Sqrt(2 * Mathf.Pow(islandSize / 2, 2) + Mathf.Sqrt(2 * Mathf.Pow(isl.size / 2, 2)))))
                            {
                                validSpot = false;
                            }
                        }
                        if (validSpot)
                        {
                            canPlaceIsland = true;
                        }

                        if (failsafe == 1000)
                        {
                            islandSize = 330;
                            print("island failsafe 1");
                        }
                        if (failsafe == 2000)
                        {
                            islandSize = 300;
                            print("island failsafe 2");
                        }


                        if (failsafe == 3000)
                        {
                            posDiameter = 950;
                            print("island failsafe 3");
                        }
                        if (failsafe == 4000)
                        {
                            Debug.LogError("island failsafe final");
                            break;
                        }

                        failsafe++;
                    }

                    islands.Add(new Island(islandSize, potentialPosition, null, islandBiomes[i], (short)(i + 1)));
                }

            }

            if (!gameManager.instance.disableValeGeneration)
            {
                //add vale to island array
                islands.Add(new Island(valeMapSize, new Vector2Int(0, -7000), null, (int)biome.vale, (short)islands.Count()));
            }

            yield return null;
            gameManager.instance.ReceiveIslands(islands.ToArray());
        }

        if (gameManager.instance.loadingScreenInfo)
            gameManager.instance.connectingText.text = "Receiving Island Data...";
        yield return null;

        while (!gameManager.doneReceivingIslands) yield return null;

        
        for (int i = 0; i < islands.Count; i++)
        {
            offset = new Vector2(offset.x + mapChunkSize, offset.y);

            MapData mapData2;
            HillData hills = HillDataFromBiome((biome)islands[i].biome);

            if (hills.name != string.Empty)
                mapData2 = GenerateMapData(Vector2.zero, Seed, islands[i].size, (biome)Enum.ToObject(typeof(biome), islands[i].biome), GenerateHillData(islands[i].size, Seed, new Vector2Int(5000, 5000)
                    , hills.noiseScale, hills.octaves, hills.persistance, hills.lacunarity, hills.cutoff).heightMap);
            else
                mapData2 = GenerateMapData(Vector2.zero, Seed, islands[i].size, (biome)Enum.ToObject(typeof(biome), islands[i].biome));

            Texture2D tex = TrimTexture(TextureGenerator.TextureFromColourMap(mapData2.colourMap, Mathf.RoundToInt(Mathf.Sqrt(mapData2.colourMap.Length)), Mathf.RoundToInt(Mathf.Sqrt(mapData2.colourMap.Length))), islandBoundsMultiplier);

            islands[i].tex = tex;

            if (gameManager.instance.loadingScreenInfo)
                gameManager.instance.connectingText.text = "Creating Island " + i + "...";
            yield return null;

            creatingIsland = true;
            yield return null;

            BiomeMapData biomeData = new BiomeMapData();
            if (islands[i].biome == (int)biome.vale)
            {
                biomeData = GenerateBiomeMapData(Vector2.zero, Seed, islands[i].size);
            }

            if (!gameManager.instance.inArena)
            {
                StartCoroutine(display._GenerateTilemap(mapData2, (Vector3Int)islands[i].center, islands[i].size, (biome)islands[i].biome, hills.name != string.Empty
                    , (biome)islands[i].biome == biome.volcanic
                    , (biome)islands[i].biome == biome.vale || (biome)islands[i].biome == biome.volcanic
                    , biomeData, i));
            
                islands[i].size = Mathf.RoundToInt(islands[i].size * islandBoundsMultiplier);

                while (creatingIsland) yield return null;
            }
        }
        //while (MapDisplay.Instance.islandsData.Contains(null) && !gameManager.instance.disableTiles) yield return null;

        if (gameManager.instance.loadingScreenInfo)
            gameManager.instance.connectingText.text = "Filling Tilemap...";
        yield return null;

        //for disable islands
        while (MapDisplay.Instance.islandsData[0] == null && !gameManager.instance.disableTiles) yield return null;

        creatingIsland = true;
        yield return MapDisplay.Instance.PopulateAllTilemaps();
        while (creatingIsland && !gameManager.instance.disableTiles) yield return null;

        display.DrawTexture((Texture2D)islands[0].tex);
        menu.instance.FixMapMat();
        menu.instance.CreateFullMap();
        
        
        finishedCreatingTiles = true;

        StartCoroutine(display._WaitForLoad());
    }


    HillData HillDataFromBiome(biome b)
    {
        switch (b)
        {
            case biome.snow:
                return hillData[0];
            case biome.volcanic:
                return hillData[1];
            case biome.vale:
                return hillData[2];
        }

        return new HillData();
    }

    public void DrawMapInEditor()
    {

        MapDisplay display = FindFirstObjectByType<MapDisplay>();

        //float chunkSize = mapChunkSize/2 - 1;

        //camBounds.SetPath(0,new Vector2[] {new Vector2(chunkSize,chunkSize), new Vector2(-chunkSize, chunkSize) , new Vector2(-chunkSize, -chunkSize) , new Vector2(chunkSize, -chunkSize) });

        switch (drawMode)
        {

            case DrawMode.ColorMap:

                MapData colorData = GenerateMapData(Vector2.zero, seed.instance.currentSeed, mapChunkSize, editorGenerateBiome);
                display.DrawTexture(TextureGenerator.TextureFromColourMap(colorData.colourMap, mapChunkSize, mapChunkSize));
                break;

            case DrawMode.HillMap:
                MapData hillData;
                HillData hills = HillDataFromBiome(editorGenerateBiome);
                hillData = GenerateHillData(500, seed.instance.currentSeed, Vector2.one * 1000, hills.noiseScale, hills.octaves, hills.persistance, hills.lacunarity, hills.cutoff);
                display.DrawTexture(TextureGenerator.TextureFromColourMap(hillData.colourMap, mapChunkSize, mapChunkSize));

                break;

            case DrawMode.ValeBiome:
                BiomeMapData biomeData = GenerateBiomeMapData(
                Vector2.zero,
                seed.instance.currentSeed,
                valeMapSize,
                3
            );

                display.DrawTexture(
                    TextureGenerator.TextureFromColourMap(
                        biomeData.colourMap,
                        valeMapSize,
                        valeMapSize
                    )
                );
                break;

        }

    }



    MapData GenerateMapData(Vector2 centre, int seed, int mapSize = mapChunkSize, biome Biome = biome.grassy, float[,] hillData = null)
    {
        TerrainType[] currentBiomeRegions = GetRegionsFromBiome(Biome);
        bool doRivers = false;

        bool doRadialBiome = false;

        switch (Biome)
        {
            case biome.swamp:
                noiseScale = noiseScaleSwamp;
                break;
            case biome.desert:
                noiseScale = noiseScaleDesert;
                break;
            case biome.snow:
                noiseScale = noiseScaleSnow;
                break;
            case biome.volcanic:
                noiseScale = noiseScaleVolcanic;
                break;
            case biome.vale:
                doRadialBiome = true;
                noiseScale = noiseScaleVale;
                break;
            case biome.grassy:
                doRivers = mapSize == mapChunkSize;
                noiseScale = noiseScaleGrassy;
                break;


        }
        
        BiomeMapData biomeData = new BiomeMapData();
        if (doRadialBiome) biomeData = GenerateBiomeMapData(centre,seed,mapSize);


        float[,] noiseMap = Noise.GenerateNoiseMap(mapSize, mapSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);

        Color[] colourMap = new Color[mapSize * mapSize];

        //rotate river based on second to last digit of seed
        int rotationAmount = int.Parse(seed.ToString()[seed.ToString().Length - 2].ToString());
        rotationAmount /= 3;

        float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(mapSize);

        bool[] riverTilesRotated = RotateTilemap(riverData[generatingRiverIndex].filledTiles, rotationAmount);



        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                if (doRadialBiome)
                {
                    currentBiomeRegions = valeRegions[biomeData.biomeMap[x, y]].regions;
                }

                if (useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }

                //check for hill
                if (hillData != null && hillData.GetLength(0) > x && hillData.GetLength(1) > y && hillData[x, y] == 1 && noiseMap[x, y] > currentBiomeRegions[shallowWaterIndex + 1].height + 0.06f)
                {
                    noiseMap[x, y] = 1;
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < currentBiomeRegions.Length; i++)
                {
                    if (currentHeight >= currentBiomeRegions[i].height)
                    {
                        //check river data for a filled space
                        if (!deepWaterIndex.Contains(i) && doRivers && riverTilesRotated[y * mapChunkSize + x])
                        {
                            colourMap[y * mapSize + x] = currentBiomeRegions[shallowWaterIndex].colour;
                            noiseMap[x, y] = currentBiomeRegions[shallowWaterIndex].height + 0.05f;
                        }
                        //no filled space on river data
                        else
                        {
                            colourMap[y * mapSize + x] = currentBiomeRegions[i].colour;
                        }

                    }
                    else
                    {
                        break;
                    }
                }
            }
        }


        return new MapData(noiseMap, colourMap);
    }

    MapData GenerateHillData(int mapSize, int seed, Vector2 centre, float noiseScale, int octaves, float persistance, float lacunarity, float cutoff)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapSize, mapSize, seed, noiseScale, octaves, persistance, lacunarity, centre, normalizeMode);
        Color[] colourMap = new Color[mapSize * mapSize];
        float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(mapSize);

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                if (useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }

                colourMap[y * mapSize + x] = new Color(noiseMap[x, y], noiseMap[x, y], noiseMap[x, y], 1);

                if (noiseMap[x, y] > cutoff)
                {
                    noiseMap[x, y] = 1;
                    colourMap[y * mapSize + x] = new Color(0, 1, 0, 1);
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    BiomeMapData GenerateBiomeMapData(
    Vector2 centre,
    int seed,
    int mapSize = mapChunkSize,
    int biomeRegionCount = 3
)
    {
        int[,] biomeMap = Noise.GenerateRadialBiomeMap(
            mapSize,
            mapSize,
            seed,
            biomeRegionCount,
            centre,
            80,
            40,
            60,
            0.6f
        );

        Color[] colourMap = new Color[mapSize * mapSize];

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int biomeIndex = biomeMap[x, y];

                // Clamp in case region count < color array
                biomeIndex = Mathf.Clamp(biomeIndex, 0, biomeDebugColors.Length - 1);

                colourMap[y * mapSize + x] = biomeDebugColors[biomeIndex];
            }
        }

        return new BiomeMapData(biomeMap, colourMap);
    }

    // Created by ChatGPT: Call this method to rotate the tilemap array
    public bool[] RotateTilemap(bool[] inputArray, int rotationAmount)
    {
        int size = inputArray.Length;
        bool[] rotatedArray = new bool[size];

        // Perform the rotation based on the rotation amount
        switch (rotationAmount % 4)
        {
            case 0: // No rotation
                rotatedArray = inputArray;
                break;

            case 1: // 90 degrees clockwise
                for (int i = 0; i < size; i++)
                {
                    int row = i / 500;
                    int col = i % 500;
                    int rotatedRow = col;
                    int rotatedCol = 500 - 1 - row;
                    int rotatedIndex = rotatedRow * 500 + rotatedCol;
                    rotatedArray[rotatedIndex] = inputArray[i];
                }
                break;

            case 2: // 180 degrees
                for (int i = 0; i < size; i++)
                {
                    int row = i / 500;
                    int col = i % 500;
                    int rotatedRow = 500 - 1 - row;
                    int rotatedCol = 500 - 1 - col;
                    int rotatedIndex = rotatedRow * 500 + rotatedCol;
                    rotatedArray[rotatedIndex] = inputArray[i];
                }
                break;

            case 3: // 90 degrees counter-clockwise
                for (int i = 0; i < size; i++)
                {
                    int row = i / 500;
                    int col = i % 500;
                    int rotatedRow = 500 - 1 - col;
                    int rotatedCol = row;
                    int rotatedIndex = rotatedRow * 500 + rotatedCol;
                    rotatedArray[rotatedIndex] = inputArray[i];
                }
                break;
        }

        return rotatedArray;
    }

    public Texture2D TrimTexture(Texture2D originalTexture, float scale)
    {
        // Ensure the scale is between 0 and 1
        if (scale <= 0 || scale > 1)
        {
            Debug.LogError("Scale must be between 0 and 1.");
            return null;
        }

        // Calculate the new width and height
        int newWidth = Mathf.RoundToInt(originalTexture.width * scale);
        int newHeight = Mathf.RoundToInt(originalTexture.height * scale);

        // Calculate the start positions to trim the edges
        int startX = (originalTexture.width - newWidth) / 2;
        int startY = (originalTexture.height - newHeight) / 2;

        // Create a new Texture2D with the new dimensions
        Texture2D trimmedTexture = new Texture2D(newWidth, newHeight);

        // Copy the pixels from the original texture to the new texture
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                Color pixelColor = originalTexture.GetPixel(x + startX, y + startY);
                trimmedTexture.SetPixel(x, y, pixelColor);
            }
        }

        // Apply the changes to the new texture
        trimmedTexture.Apply();

        return trimmedTexture;
    }


    void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }

        //falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }


    public void SetRiverData()
    {

        riverData[riverSetIndex].filledTiles = new bool[mapChunkSize * mapChunkSize];

        if (riverSetIndex >= 0)
        {

            for (int y = 0; y < mapChunkSize; y++)
            {
                for (int x = 0; x < mapChunkSize; x++)
                {

                    riverData[riverSetIndex].filledTiles[y * mapChunkSize + x] = riverTilemap.GetTile(new Vector3Int(x - mapChunkSize / 2, y - mapChunkSize / 2)) != null;

                }
            }
            riverData[riverSetIndex].set = true;
        }

    }

    public void LoadRiver()
    {
        riverTilemap.ClearAllTiles();

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (riverData[riverSetIndex].filledTiles[y * mapChunkSize + x] == true)
                {
                    riverTilemap.SetTile(new Vector3Int(x - mapChunkSize / 2, y - mapChunkSize / 2, 0), regions[shallowWaterIndex].tile);
                }
            }
        }
    }

    public void ClearRiver()
    {
        riverTilemap.ClearAllTiles();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        if (instance)
        {
            foreach (Island island in islands)
            {
                Gizmos.DrawWireCube((Vector2)island.center, new Vector2(island.size, island.size));
            }

        }

    }
}

[System.Serializable]
public class TerrainType
{
    public string name;
    public float height;
    public Color colour;
    public TileBase tile;
    public Tilemap tileMap;
    [Tooltip("Generates tiles from the previous region in addition to this one")] 
    public bool generateRegionBelow;


}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}

[System.Serializable]
public struct River
{
    public bool set;
    [SerializeField]
    [HideInInspector]
    public bool[] filledTiles;
}

[System.Serializable]
public class Island
{
    public int size;
    public Vector2Int center;
    public Texture tex;
    public int biome;
    public short index;
    public bool containsUniqueTool;
    public Vector2 uniqueToolPos;

    public Island(int size, Vector2Int center, Texture2D tex, int biome, short index)
    {
        this.size = size;
        this.center = center;
        this.tex = tex;
        this.biome = biome;
        this.index = index;
    }
}

[System.Serializable]
public struct HillData
{
    public string name;

    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    [Range(0, 1)]
    public float cutoff;

}
[System.Serializable]
public struct BiomeMapData
{
    public int[,] biomeMap;
    public Color[] colourMap;

    public BiomeMapData(int[,] biomeMap, Color[] colourMap)
    {
        this.biomeMap = biomeMap;
        this.colourMap = colourMap;
    }
}
[Serializable]
public class RegionGroup
{
    public string name;
    public TerrainType[] regions;
}

using DG.Tweening;
using Fusion;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Tilemaps;


//initial loading process in this script
public class MapDisplay : NetworkBehaviour
{
    public static MapDisplay Instance;
    public static bool disableTileUpdating;

    public GameObject tileGridGameObject;
    public bool NoTilesMode;
    public Renderer textureRender;
    public AstarPath path;
    public Tilemap groundTilemap, waterTilemap, hillTilemap, roofTilemap, lavaTilemap, lavaCollisionTilemap;
    public LayerMask gridObstacleMask;
    public IslandTileData[] islandsData = new IslandTileData[MapGenerator.islandCount + 1];
    public Unity.Cinemachine.CinemachineVirtualCamera cam;
    public TilemapRenderer roofTilemapRenderer;
    public TileBase outlineTile;

    //timer
    private bool isRunning = false;
    private System.DateTime startTime;
    private System.TimeSpan elapsedLoadingTime;
    public TextMeshProUGUI loadingTimer;

    public static bool finishedLoading, finishedScreenFade;
    public TileBase shallowWater, lavaCollisionTile;


    private void Awake()
    {
        Instance = this;
        //gameManager.instance.ToggleDefaultLayerCulling(false);
        //Physics2D.simulationMode = SimulationMode2D.Script;
    }
    private void Start()
    {
        islandsData = new IslandTileData[MapGenerator.islandCount + 1];
        if (!gameManager.instance.disableValeGeneration)
        {
            islandsData = new IslandTileData[MapGenerator.islandCount + 2];
        }

        textureRender.gameObject.SetActive(false);

    }


    public void StartTimer()
    {
        if (!isRunning)
        {
            isRunning = true;
            startTime = System.DateTime.Now;
        }
    }

    public void StopTimer()
    {
        if (isRunning)
        {
            isRunning = false;
            elapsedLoadingTime = System.DateTime.Now - startTime;
        }
    }



    public void DrawTexture(Texture2D texture)
    {
        //textureRender.gameObject.SetActive(true);
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(MapGenerator.mapChunkSize / 10, 1, MapGenerator.mapChunkSize / 10);
        textureRender.transform.position = new Vector3(-MapGenerator.mapChunkSize, 0, 0);
    }

    public void ClearTiles()
    {
        MapGenerator MapGenerator = FindAnyObjectByType<MapGenerator>();
        //clear all tiles
        foreach (TerrainType type in MapGenerator.volcanicRegions)
        {
            type.tileMap.ClearAllTiles();
        }
        lavaCollisionTilemap.ClearAllTiles();
    }

    public IEnumerator _GenerateTilemap(
    MapData data,
    Vector3Int tilesOffset,
    int mapSize,
    MapGenerator.biome biome,
    bool useHill,
    bool useRoof,
    bool useLava,
    BiomeMapData biomeMap,
    int islandIndex = 0)
    {
        MapGenerator mapGen = FindAnyObjectByType<MapGenerator>();

        if (mapGen.dontGenerate || gameManager.instance.disableTiles)
            yield break;

        int total = mapSize * mapSize;

        TileBase[] groundTileArray = new TileBase[total];
        TileBase[] waterTileArray = new TileBase[total];

        TileBase[] hillTileArray = useHill ? new TileBase[total] : new TileBase[0];
        TileBase[] roofTileArray = useRoof ? new TileBase[total] : new TileBase[0];
        TileBase[] lavaTileArray = useLava ? new TileBase[total] : new TileBase[0];

        TerrainType[] regions = mapGen.GetRegionsFromBiome(biome);

        for (int h = mapSize - 1; h > 0; --h)
        {
            for (int w = mapSize - 1; w > 0; --w)
            {
                int index = h * mapSize + w;

                if (biome == MapGenerator.biome.vale)
                    regions = mapGen.GetRegionsFromBiome(biome, biomeMap.biomeMap[w, h]);

                float height = data.heightMap[w, h];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (regions[i].tile == null)
                        continue;

                    if (height > regions[i].height &&
                        (i == regions.Length - 1 || height <= regions[i + 1].height))
                    {
                        if (regions[i].generateRegionBelow)
                        {
                            int below = i - 1;

                            while (below > 0 && regions[below + 1].generateRegionBelow)
                            {
                                if (regions[below].tileMap == groundTilemap)
                                    groundTileArray[index] = regions[below].tile;

                                else if (regions[below].tileMap == waterTilemap)
                                    waterTileArray[index] = regions[below].tile;

                                else if (regions[below].tileMap == hillTilemap && useHill)
                                    hillTileArray[index] = regions[below].tile;

                                else if (regions[below].tileMap == roofTilemap && useRoof)
                                    roofTileArray[index] = regions[below].tile;

                                else if (regions[below].tileMap == lavaTilemap && useLava)
                                    lavaTileArray[index] = regions[below].tile;

                                below--;
                            }
                        }

                        if (regions[i].tileMap == groundTilemap)
                            groundTileArray[index] = regions[i].tile;

                        else if (regions[i].tileMap == waterTilemap)
                            waterTileArray[index] = regions[i].tile;

                        else if (regions[i].tileMap == hillTilemap && useHill)
                            hillTileArray[index] = regions[i].tile;

                        else if (regions[i].tileMap == roofTilemap && useRoof)
                            roofTileArray[index] = regions[i].tile;

                        else if (regions[i].tileMap == lavaTilemap && useLava)
                            lavaTileArray[index] = regions[i].tile;

                        break;
                    }
                }
            }

            if (h % 100 == 0)
                yield return null;
        }

        if (waterTileArray != null && waterTileArray.Length > 0)
        {
            waterTileArray = OutlineTiles(waterTileArray, mapSize);

            //vale biome seperating rivers
            if (biome == MapGenerator.biome.vale)
            {
                TerrainType shallow = mapGen.GetRegionsFromBiome(biome)[1];
                waterTileArray = AddBiomeRivers(
                    waterTileArray,
                    groundTileArray,
                    biomeMap.biomeMap,
                    shallow,
                    mapSize,
                    2
                );

                for (int i = 0; i < waterTileArray.Length; i++)
                {
                    if (waterTileArray[i] != null)
                        lavaTileArray[i] = null;
                }
            }
        }


        //volcanic cave borders
        if (roofTileArray != null && roofTileArray.Length > 0)
        {
            TileBase[] outlinedCliffs = OutlineCaveBorders(roofTileArray, mapSize, mapGen.volcanicRegions.Last().tile,7,20,50);

            hillTileArray = CombineTileArrays(hillTileArray, outlinedCliffs);
        }

        if (hillTileArray != null && hillTileArray.Length == total)
        {
            hillTileArray = OutlineTiles(hillTileArray, mapSize);
            hillTileArray = FillEnclosedAreas(hillTileArray, mapSize);
        }

        islandsData[islandIndex] = new IslandTileData(
            groundTileArray,
            waterTileArray,
            hillTileArray,
            roofTileArray,
            lavaTileArray,
            tilesOffset,
            mapSize
        );

        yield return null;
        mapGen.creatingIsland = false;
    }

    TileBase[] FillEnclosedAreas(
    TileBase[] originalTileArray,
    int mapSize)
    {
        TileBase[] result = (TileBase[])originalTileArray.Clone();

        bool[] blockedForDetection = new bool[result.Length];
        bool[] connectedToOutside = new bool[result.Length];

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int index = y * mapSize + x;

                if (originalTileArray[index] == null)
                    continue;

                // The actual tile is blocked.
                blockedForDetection[index] = true;

                // The tile also extends one cell downward.
                if (y > 0)
                {
                    int belowIndex = (y - 1) * mapSize + x;
                    blockedForDetection[belowIndex] = true;
                }
            }
        }

        Queue<int> queue = new Queue<int>();


        for (int x = 0; x < mapSize; x++)
        {
            int top = x;
            int bottom = (mapSize - 1) * mapSize + x;

            if (!blockedForDetection[top] && !connectedToOutside[top])
            {
                connectedToOutside[top] = true;
                queue.Enqueue(top);
            }

            if (!blockedForDetection[bottom] && !connectedToOutside[bottom])
            {
                connectedToOutside[bottom] = true;
                queue.Enqueue(bottom);
            }
        }

        for (int y = 0; y < mapSize; y++)
        {
            int left = y * mapSize;
            int right = y * mapSize + mapSize - 1;

            if (!blockedForDetection[left] && !connectedToOutside[left])
            {
                connectedToOutside[left] = true;
                queue.Enqueue(left);
            }

            if (!blockedForDetection[right] && !connectedToOutside[right])
            {
                connectedToOutside[right] = true;
                queue.Enqueue(right);
            }
        }

        // ---------------------------------------------------------
        // 4-directional flood fill.
        // ---------------------------------------------------------

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();

            int x = index % mapSize;
            int y = index / mapSize;

            for (int d = 0; d < 4; d++)
            {
                int nx = x + dx[d];
                int ny = y + dy[d];

                if (nx < 0 || ny < 0 || nx >= mapSize || ny >= mapSize)
                    continue;

                int neighborIndex = ny * mapSize + nx;

                if (connectedToOutside[neighborIndex])
                    continue;

                if (blockedForDetection[neighborIndex])
                    continue;

                connectedToOutside[neighborIndex] = true;
                queue.Enqueue(neighborIndex);
            }
        }

        bool[] processed = new bool[result.Length];

        for (int i = 0; i < result.Length; i++)
        {
            if (blockedForDetection[i] ||
                connectedToOutside[i] ||
                processed[i])
            {
                continue;
            }

            Queue<int> enclosedQueue = new Queue<int>();
            List<int> enclosedTiles = new List<int>();

            enclosedQueue.Enqueue(i);
            processed[i] = true;

            TileBase fillTile = null;

            while (enclosedQueue.Count > 0)
            {
                int index = enclosedQueue.Dequeue();

                enclosedTiles.Add(index);

                int x = index % mapSize;
                int y = index / mapSize;

                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d];
                    int ny = y + dy[d];

                    if (nx < 0 || ny < 0 || nx >= mapSize || ny >= mapSize)
                        continue;

                    int neighborIndex = ny * mapSize + nx;

                    // Find a real tile surrounding the enclosed region.
                    if (originalTileArray[neighborIndex] != null)
                    {
                        if (fillTile == null)
                        {
                            fillTile = originalTileArray[neighborIndex];
                        }

                        continue;
                    }

                    if (blockedForDetection[neighborIndex])
                        continue;

                    if (connectedToOutside[neighborIndex] ||
                        processed[neighborIndex])
                    {
                        continue;
                    }

                    processed[neighborIndex] = true;
                    enclosedQueue.Enqueue(neighborIndex);
                }
            }


            if (fillTile != null)
            {
                foreach (int index in enclosedTiles)
                {
                    result[index] = fillTile;
                }
            }
        }

        return result;
    }

    TileBase[] CombineTileArrays(TileBase[] original, TileBase[] additions)
    {
        if (original.Length != additions.Length)
            throw new System.ArgumentException("Tile arrays must be the same length.");

        TileBase[] result = (TileBase[])original.Clone();

        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == null && additions[i] != null)
            {
                result[i] = additions[i];
            }
        }

        return result;
    }

    private bool IsBiomeEdge(int[,] biomeMap, int x, int y, int mapSize)
    {
        int biome = biomeMap[x, y];

        // Check 4-neighbor (or 8-neighbor if you want diagonal edges too)
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];

            if (nx < 0 || nx >= mapSize || ny < 0 || ny >= mapSize)
                continue;

            if (biomeMap[nx, ny] != biome)
                return true;
        }

        return false;
    }

    private TileBase[] AddBiomeRivers(TileBase[] waterTileArray, TileBase[] groundTileArray, int[,] biomeMap, TerrainType shallowWaterType, int mapSize, int riverThickness = 4)
    {
        // We'll create a temporary copy so we can expand the river thickness
        bool[,] riverMask = new bool[mapSize, mapSize];

        // Step 1: mark edges
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                if (IsBiomeEdge(biomeMap, x, y, mapSize))
                    riverMask[x, y] = true;
            }
        }

        // Step 2: expand thickness
        for (int t = 1; t < riverThickness; t++)
        {
            bool[,] newMask = (bool[,])riverMask.Clone();

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    if (riverMask[x, y])
                    {
                        // Mark neighbors
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize)
                                    newMask[nx, ny] = true;
                            }
                        }
                    }
                }
            }

            riverMask = newMask;
        }

        // Step 3: apply shallow water tiles (but don't replace deep water)
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int index = y * mapSize + x;
                if (riverMask[x, y])
                {
                    if (groundTileArray[index] != null) // skip deep water
                        waterTileArray[index] = shallowWaterType.tile;
                }
            }
        }

        return waterTileArray;
    }
    TileBase[] OutlineTiles(TileBase[] originalTileArray, int mapSize)
    {
        // Pass 2: Add outline tiles into tileArray itself
        Vector2Int[] dirs = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
        };

        TileBase[] newArray = (TileBase[])originalTileArray.Clone();

        for (int h = 0; h < mapSize; h++)
        {
            for (int w = 0; w < mapSize; w++)
            {
                int index = h * mapSize + w;
                if (originalTileArray[index] == null) continue;

                TileBase outlineTile = originalTileArray[index];

                foreach (var d in dirs)
                {
                    int nx = w + d.x;
                    int ny = h + d.y;
                    if (nx < 0 || ny < 0 || nx >= mapSize || ny >= mapSize) continue;

                    int nIndex = ny * mapSize + nx;
                    if (originalTileArray[nIndex] == null) // only outline if it was originally empty
                    {
                        newArray[nIndex] = outlineTile;
                    }
                }
            }
        }

        return newArray;
    }

    TileBase[] OutlineCaveBorders(
    TileBase[] originalTileArray,
    int mapSize,
    TileBase outlineTile,
    int holeSize,
    int minInterval,
    int maxInterval)
    {
        System.Random rng = new System.Random(seed.instance.currentSeed);

        Vector2Int[] dirs = new Vector2Int[]
        {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        };

        // These are the tiles that make up the outline.
        HashSet<int> outlineIndices = new HashSet<int>();

        // These are the interior tiles touching empty space.
        List<int> boundaryTiles = new List<int>();

        // ---------------------------------------------------------
        // Find boundary tiles
        // ---------------------------------------------------------

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int index = y * mapSize + x;

                if (originalTileArray[index] == null)
                    continue;

                bool boundary = false;

                foreach (Vector2Int dir in dirs)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;

                    if (nx < 0 || ny < 0 || nx >= mapSize || ny >= mapSize)
                        continue;

                    int neighborIndex = ny * mapSize + nx;

                    if (originalTileArray[neighborIndex] == null)
                    {
                        boundary = true;

                        // Exterior outline tile
                        outlineIndices.Add(neighborIndex);
                    }
                }

                if (boundary)
                {
                    // Interior outline tile
                    outlineIndices.Add(index);

                    boundaryTiles.Add(index);
                }
            }
        }

        // ---------------------------------------------------------
        // Create holes
        // ---------------------------------------------------------

        HashSet<int> holes = new HashSet<int>();

        if (boundaryTiles.Count > 0 && holeSize > 0)
        {
            int distanceToNextHole = rng.Next(minInterval,maxInterval + 1);

            for (int i = 0; i < boundaryTiles.Count; i++)
            {
                distanceToNextHole--;

                if (distanceToNextHole <= 0)
                {
                    int centerIndex = boundaryTiles[i];

                    int centerX = centerIndex % mapSize;
                    int centerY = centerIndex / mapSize;

                    // Remove a section of the outline around this point.
                    int halfSize = holeSize / 2;

                    for (int y = centerY - halfSize; y <= centerY + halfSize; y++)
                    {
                        for (int x = centerX - halfSize; x <= centerX + halfSize; x++)
                        {
                            if (x < 0 || y < 0 || x >= mapSize || y >= mapSize)
                                continue;

                            int index = y * mapSize + x;

                            // Only remove actual outline tiles.
                            if (outlineIndices.Contains(index))
                            {
                                holes.Add(index);
                            }
                        }
                    }

                    distanceToNextHole = rng.Next(minInterval, maxInterval + 1);
                }
            }
        }

        // ---------------------------------------------------------
        // Build result
        // ---------------------------------------------------------

        TileBase[] result = new TileBase[originalTileArray.Length];

        foreach (int index in outlineIndices)
        {
            if (!holes.Contains(index))
            {
                result[index] = outlineTile;
            }
        }

        return result;
    }


    public async Task PopulateAllTilemaps()
    {
        var gm = gameManager.instance;

        gm.connectingPercentText.enabled = true;
        gm.connectingPercentText.text = "0%";

        Physics2D.simulationMode = SimulationMode2D.Script;

        await Task.Yield();

        tileGridGameObject.SetActive(false);

        int counter = 0;
        int counterMax = 0;

        foreach (IslandTileData data in islandsData)
        {
            if (data == null)
                continue;

            counterMax += data.mapSize * data.mapSize;
        }

        StringBuilder percentBuilder = new StringBuilder(8);
        int lastPercent = -1;

        int chunkSize = 100;
        int yieldCounter = 0;

        TileBase[] groundBuffer = new TileBase[chunkSize * chunkSize];
        TileBase[] waterBuffer = new TileBase[chunkSize * chunkSize];
        TileBase[] hillBuffer = new TileBase[chunkSize * chunkSize];
        TileBase[] roofBuffer = new TileBase[chunkSize * chunkSize];
        TileBase[] lavaBuffer = new TileBase[chunkSize * chunkSize];
        TileBase[] lavaCollisionBuffer = new TileBase[chunkSize * chunkSize];

        foreach (IslandTileData data in islandsData)
        {
            if (data == null)
                continue;

            int mapSize = data.mapSize;

            Vector3Int basePos = new Vector3Int(
                data.tilesOffset.x - mapSize / 2,
                data.tilesOffset.y - mapSize / 2,
                0
            );

            TileBase[] lavaCollisionArray = null;

            if (data.lavaTileArray != null)
            {
                lavaCollisionArray = new TileBase[data.lavaTileArray.Length];

                for (int i = 0; i < lavaCollisionArray.Length; i++)
                    if (data.lavaTileArray[i] != null)
                        lavaCollisionArray[i] = lavaCollisionTile;

                if (data.lavaTileArray.Length == mapSize * mapSize)
                    data.lavaTileArray = OutlineTiles(data.lavaTileArray, mapSize);
            }

            for (int x = 0; x < mapSize; x += chunkSize)
            {
                for (int y = 0; y < mapSize; y += chunkSize)
                {
                    int sizeX = Mathf.Min(chunkSize, mapSize - x);
                    int sizeY = Mathf.Min(chunkSize, mapSize - y);

                    var bounds = new BoundsInt(
                        basePos.x + x,
                        basePos.y + y,
                        0,
                        sizeX,
                        sizeY,
                        1
                    );

                    bool hasGround = GetTileChunk(data.tileArray, groundBuffer, mapSize, x, y, sizeX, sizeY);
                    bool hasWater = GetTileChunk(data.waterTileArray, waterBuffer, mapSize, x, y, sizeX, sizeY);
                    bool hasHill = GetTileChunk(data.hillTileArray, hillBuffer, mapSize, x, y, sizeX, sizeY);
                    bool hasRoof = GetTileChunk(data.roofTileArray, roofBuffer, mapSize, x, y, sizeX, sizeY);
                    bool hasLava = GetTileChunk(data.lavaTileArray, lavaBuffer, mapSize, x, y, sizeX, sizeY);
                    bool hasLavaC = GetTileChunk(lavaCollisionArray, lavaCollisionBuffer, mapSize, x, y, sizeX, sizeY);

                    if (hasGround) groundTilemap.SetTilesBlock(bounds, groundBuffer);
                    if (hasWater) waterTilemap.SetTilesBlock(bounds, waterBuffer);
                    if (hasHill) hillTilemap.SetTilesBlock(bounds, hillBuffer);
                    if (hasRoof) roofTilemap.SetTilesBlock(bounds, roofBuffer);
                    if (hasLava) lavaTilemap.SetTilesBlock(bounds, lavaBuffer);
                    if (hasLavaC) lavaCollisionTilemap.SetTilesBlock(bounds, lavaCollisionBuffer);

                    counter += sizeX * sizeY;

                    int percent = Mathf.Clamp(
                        Mathf.RoundToInt((float)counter / counterMax * 100f),
                        0,
                        100
                    );

                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        percentBuilder.Clear();
                        percentBuilder.Append(percent);
                        percentBuilder.Append('%');
                        gm.connectingPercentText.text = percentBuilder.ToString();
                    }

                    yieldCounter++;

                    if (yieldCounter >= 2)
                    {
                        yieldCounter = 0;
                        await Task.Yield();
                    }
                }
            }
        }

        gm.connectingPercentText.enabled = false;

        MapGenerator.instance.creatingIsland = false;

        groundTilemap.CompressBounds();
        waterTilemap.CompressBounds();
        hillTilemap.CompressBounds();
        roofTilemap.CompressBounds();
        lavaTilemap.CompressBounds();

        lavaTilemap.RefreshAllTiles();
        await Task.Yield();
        waterTilemap.RefreshAllTiles();
        gameManager.instance.waterScrolling.gameObject.SetActive(true);

        tileGridGameObject.SetActive(true);

        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

    }

    private bool GetTileChunk(
    TileBase[] source,
    TileBase[] buffer,
    int fullSize,
    int startX,
    int startY,
    int sizeX,
    int sizeY)
    {
        if (source == null)
            return false;

        bool hasNonNull = false;
        int index = 0;

        for (int y = 0; y < sizeY; y++)
        {
            int sourceRow = (startY + y) * fullSize + startX;

            for (int x = 0; x < sizeX; x++)
            {
                int sourceIndex = sourceRow + x;

                TileBase tile = null;

                if (sourceIndex < source.Length)
                    tile = source[sourceIndex];

                buffer[index++] = tile;

                if (tile != null)
                    hasNonNull = true;
            }
        }

        return hasNonNull;
    }

    public IEnumerator _WaitForLoad()
    {
        AudioListener.volume = 0;

        while (MapGenerator.instance.islands.Count < MapGenerator.islandCount)
        {
            yield return null;
            if (gameManager.instance.disableIslands) break;
        }

        gameManager.instance.TilesComplete();

        yield return null;

        EnemySpawner.instance.SetIslands();

        //wait until all player scenes are loaded before generating features
        gameManager.instance.connectingText.text = "Syncing Data...";
        while (!gameManager.instance.allPlayersDoneWithTiles)
        {

            yield return null;
            if (Runner.SessionInfo.PlayerCount == 1) break;
        }

        if (!MapGenerator.instance.dontGenerate)
        {
            yield return null;
            featurePlacer.Instance.GenerateFeatures(true);

            while (featurePlacer.Instance.generating || treeRPCs.Instance.generatingTreeHealths) yield return null;

            if (!gameManager.instance.disableValeGeneration)
            {
                ValeManager.instance.CreateAnchors(MapGenerator.instance.islands[MapGenerator.instance.islands.Count - 1]);
            }

            yield return new WaitForSeconds(0.5f);

            //while (!playerSpawner.instance.transform) yield return null;

            gameManager.instance.connectingText.text = "Generating Pathfinding...";
            yield return null;


            //make vale bounding box bigger
            if (!gameManager.instance.disableValeGeneration)
            {
                MapGenerator.instance.islands[MapGenerator.instance.islands.Count - 1].size = 750;
            }


            List<GridGraph> islandGraphs = new List<GridGraph>();

            //island graphs
            foreach (Island island in MapGenerator.instance.islands)
            {

                GridGraph newGraph = path.data.AddGraph(typeof(GridGraph)) as GridGraph;
                newGraph.is2D = true;
                newGraph.center = (Vector2)island.center + new Vector2(0.5f, 0.5f);
                newGraph.SetDimensions(Mathf.RoundToInt(island.size * 0.88f), Mathf.RoundToInt(island.size * 0.88f), 1);
                newGraph.collision.mask = gridObstacleMask;
                newGraph.collision.use2D = true;
                newGraph.collision.diameter = 0.7f;
                newGraph.cutCorners = false;
                islandGraphs.Add(newGraph);
            }

            //dungeon graphs
            foreach (DungeonRooms d in DungeonGenerator.instance.dungeons)
            {

                GridGraph newGraph = path.data.AddGraph(typeof(GridGraph)) as GridGraph;
                newGraph.is2D = true;
                
                Vector2 center = new Vector2(
                d.boundsMin.x + d.size.x / 2f,
                d.boundsMin.y + d.size.y / 2f);

                newGraph.center = (Vector3)center;
                newGraph.SetDimensions(Mathf.RoundToInt(d.size.x + 4), Mathf.RoundToInt(d.size.y + 4), 1);
                newGraph.collision.mask = gridObstacleMask;
                newGraph.collision.use2D = true;
                newGraph.collision.diameter = 0.7f;
                newGraph.cutCorners = false;
            }

            int frame = 0;
            foreach (Progress p in AstarPath.active.ScanAsync())
            {
                frame++;
                if (frame % 45 == 0)
                    yield return null;
            }

            foreach (GridGraph graph in islandGraphs)
            {
                //nodes set by detecting tiles - not collision
                graph.GetNodes(node =>
                {
                    Vector3 worldPos = (Vector3)node.position;
                    Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);

                    TileBase tile = groundTilemap.GetTile(cellPos);
                    TileBase waterTile = waterTilemap.GetTile(cellPos);

                    if ((tile == null && !(gameManager.instance.difficulty > 0 && waterTile != null)) || lavaTilemap.GetTile(cellPos) || hillTilemap.GetTile(cellPos))
                    {
                        node.Walkable = false;
                    }
                });

                yield return null;
            }

            Boss.instance.bossInside.SetActive(false);

            gameManager.instance.GenerationComplete();

            if(playerSpawner.instance.savedDungeon > -1)
                tileGridGameObject.SetActive(false);

            gameManager.instance.connectingText.text = "Syncing Data...";
            while (!gameManager.instance.allPlayersDone)
            {
                yield return null;
            }


            gameManager.instance.connectingText.text = "Loading Save File...";

            yield return null;

            if (DataPersistanceManager.instance.loadingSave)
            {
                gameManager.instance.spawningSavedBoats = true;
                yield return null;
                if (Runner.IsSharedModeMasterClient)
                {
                    if (gameManager.instance.loadingScreenInfo) gameManager.instance.connectingText.text = "Loading Saved Tiles...";
                    yield return null;
                    gameManager.instance.LoadSavedTiles();

                    if (gameManager.instance.loadingScreenInfo) gameManager.instance.connectingText.text = "Loading Saved Items...";
                    yield return null;
                    gameManager.instance.LoadSavedItems(gameManager.instance.savedItemData, new NetworkId());

                    if (gameManager.instance.loadingScreenInfo) gameManager.instance.connectingText.text = "Loading Saved Boats...";
                    yield return null;
                    gameManager.instance.LoadSavedBoats();
                    yield return null;
                    StartCoroutine(gameManager.instance._WaitForBoatsLoad());
                }

                float failsafeTimer = 0;

                while (gameManager.instance.spawningSavedBoats)
                {
                    yield return null;
                    failsafeTimer += Time.deltaTime;

                    if (failsafeTimer > 20)
                    {
                        Chat.Instance.Error("Failed to create saved boats");
                        break;
                    }

                }
            }
            DungeonGenerator.instance.LoadSavedChestData();


            if (gameManager.instance.loadingScreenInfo) gameManager.instance.connectingText.text = "Spawning Player...";
            yield return new WaitForSeconds(0.5f);

            playerSpawner.instance.SpawnPlayers();

            yield return null;

            while (player.instance == null || playerSpawner.instance.players == null)
                yield return null;

            if (playerSpawner.instance.players.ToList().Find(p => p && p.transform.parent == null)) yield return null;


            if (gameManager.instance.loadingScreenInfo) gameManager.instance.connectingText.text = "Riding Saved Boat...";
            yield return null;
            gameManager.instance.PlayerRideSavedBoat();



        }
        else
        {
            playerSpawner.instance.SpawnPlayers();
            gameManager.instance.ToggleDefaultLayerCulling(true);
            gameManager.instance.connectingScreen.DOFade(0, 0.1f);
        }

        if (!playerSpawner.instance.savedPlayerDead && !(DungeonEnemySpawn.currentlyInRoom && DungeonEnemySpawn.currentlyInRoom.active))
        {
            if (player.instance.inDungeon == -1)
            {
                if (!TimeManager.instance.isNight)
                    StartCoroutine(gameManager.instance._SwitchMusic("day", 1));
                else
                    StartCoroutine(gameManager.instance._SwitchMusic("night", 1));
            }
            else
            {
                menu.instance.mapGroup.DOFade(0, 0);
                StartCoroutine(gameManager.instance._SwitchMusic("dungeon", 1));
                DungeonGenerator.instance.SetModifierText(player.instance.inDungeon);
            }
        }
        StopTimer();
        pauseScreen.instance.loadTime.text = "Load Time: " + string.Format("{0}:{1:00}", elapsedLoadingTime.Minutes, elapsedLoadingTime.Seconds);

        finishedLoading = true;

        //enable physics
        //Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            StartCoroutine(TimeManager.instance._ChangeDay(TimeManager.instance.dayNum + 1, 2.9f));
        }
        else
        {
            StartCoroutine(TimeManager.instance._ChangeDay(TimeManager.instance.dayNum, 2.9f));
        }


        gameManager.instance.connectingText.text = "Done!";

        yield return null;

        cam.ForceCameraPosition(player.instance.transform.position - new Vector3(0, 0, 10), Quaternion.identity);

        DOTween.To(() => AudioListener.volume, t => AudioListener.volume = t, 1, 1);

        yield return new WaitForSeconds(1f);


        if (gameManager.instance && gameManager.instance.connectingScreen.alpha == 1)
        {
            gameManager.instance.connectingScreen.DOFade(0, 0.5f);
        }
        yield return new WaitForSeconds(0.6f);
        finishedScreenFade = true;

    }


    public IEnumerator ClearTilemapsInChunks()
    {
        foreach (IslandTileData data in islandsData)
        {
            if (data == null)
            {
                if (!gameManager.instance.disableIslands)
                    Debug.LogWarning("Null Island Tile Data");
                continue;
            }

            int mapSize = data.mapSize;
            Vector3Int basePos = new Vector3Int(data.tilesOffset.x - mapSize / 2, data.tilesOffset.y - mapSize / 2, 0);

            int chunkSize = 30; // Tiles per chunk in x/y (adjust to balance speed/responsiveness)

            for (int x = 0; x < mapSize; x += chunkSize)
            {
                for (int y = 0; y < mapSize; y += chunkSize)
                {
                    int sizeX = Mathf.Min(chunkSize, mapSize - x);
                    int sizeY = Mathf.Min(chunkSize, mapSize - y);

                    var bounds = new BoundsInt(basePos.x + x, basePos.y + y, 0, sizeX, sizeY, 1);

                    // Calculate array index offset
                    TileBase[] nullChunk = new TileBase[chunkSize * chunkSize];

                    groundTilemap.SetTilesBlock(bounds, nullChunk);
                    if (data.hillTileArray.Length > 0)
                        hillTilemap.SetTilesBlock(bounds, nullChunk);
                    if (data.roofTileArray.Length > 0)
                        roofTilemap.SetTilesBlock(bounds, nullChunk);
                    if (data.lavaTileArray.Length > 0)
                        lavaTilemap.SetTilesBlock(bounds, nullChunk);

                    yield return null; // Let the frame breathe
                }
            }

            //StartCoroutine(_RefreshTilemaps());
        }
    }


}


[System.Serializable]
public class IslandTileData
{
    public TileBase[] tileArray;
    public TileBase[] waterTileArray;
    public TileBase[] hillTileArray;
    public TileBase[] roofTileArray;
    public TileBase[] lavaTileArray;
    public Vector3Int tilesOffset;
    public int mapSize;


    public IslandTileData(TileBase[] tileArray, TileBase[] waterTileArray, TileBase[] hillTileArray, TileBase[] roofTileArray, TileBase[] lavaTileArray, Vector3Int tilesOffset, int mapSize)
    {
        this.tileArray = tileArray;
        this.waterTileArray = waterTileArray;
        this.hillTileArray = hillTileArray;
        this.roofTileArray = roofTileArray;
        this.lavaTileArray = lavaTileArray;
        this.tilesOffset = tilesOffset;
        this.mapSize = mapSize;
    }

}
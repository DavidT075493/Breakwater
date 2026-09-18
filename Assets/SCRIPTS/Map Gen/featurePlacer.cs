using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class featurePlacer : NetworkBehaviour
{
    public static featurePlacer Instance;
    public float featuresAmountMultiplier = 1;

    public NetworkObject treeView;
    public Transform treeHolder, shrubHolder, itemHolder, mineralHolder;

    public Transform planeCrash;
    public Vector2 planeCheckSize;

    public feature[] features;
    public feature[] shrubs;
    public ItemFeature[] items;
    public feature[] minerals;
    public feature[] oceanStuff;

    public Tilemap groundTilemap, hillTilemap, waterTilemap;

    public List<GameObject> treesList = new List<GameObject>();
    public List<Vector3Int> takenPositions;
    public float minDist;

    public List<GameObject> shrubsList = new List<GameObject>();
    public List<Vector3> shrubsTakenPos;
    public float shrubsMinDist;

    public List<GameObject> itemsList = new List<GameObject>();
    public List<Vector3Int> itemsTakenPos;
    public float itemsMinDist;

    public List<GameObject> mineralsList = new List<GameObject>();
    public List<Vector3Int> mineralsTakenPos;
    public float mineralsMinDist;

    public List<GameObject> oceanStuffList = new List<GameObject>();
    public List<Vector3Int> oceanStuffTakenPos;
    public float oceanStuffMinDist;

    public List<GameObject> uItemsList = new List<GameObject>();
    public List<Vector3Int> uItemsTakenPos;
    public float uItemsMinDist;

    public Transform dungeonEntranceHolder, uniqueItemHolder;

    [HideInInspector]
    short currentFeatureIndex;
    [HideInInspector]
    public bool generating;
    public int playersDoneGenerating = 0;

    const int failsafeAmt = 500;
    //bool generatingTrees, generatingItems, generatingShrubs, generatingMinerals;
    public UniqueItemBiome[] uniqueItemBiomes;
    public DungeonFeature[] dungeonFeatures;
    int[] biomeUniqueItemsIndex = new int[6];

    const int frameYieldInterval = 2000;

    private void Awake()
    {
        Instance = this;
        treeChop.lastId = 0;
        generating = true;
    }

    private void Start()
    {
        RemoveFeatures();
    }

    public async void GenerateFeatures(bool inGame)
    {
        if (inGame && !gameManager.instance.disableFeatures && !gameManager.instance.disableTiles)
        {

            UnityEngine.Random.InitState(seed.instance.currentSeed);

            foreach (Island island in MapGenerator.instance.islands)
            {
                if (island.biome < dungeonFeatures.Length && !dungeonFeatures[island.biome].alreadySpawned && island.index != 0
                        && (MapGenerator.biome)island.biome != MapGenerator.biome.vale)
                {
                    if (gameManager.instance.loadingScreenInfo)
                        gameManager.instance.connectingText.text = "Placing Dungeon " + ((MapGenerator.biome)island.biome).ToString();
                    await Task.Yield();
                    await GenerateDungeonEntrances(island.size, island.center, (MapGenerator.biome)island.biome, island.index);
                }

                if (gameManager.instance.loadingScreenInfo)
                {
                    gameManager.instance.connectingText.text = "Planting Shrubs in " + ((MapGenerator.biome)island.biome).ToString();
                }
                else
                {
                    gameManager.instance.connectingText.text = "Planting Trees...";
                }
                shrubsTakenPos.Clear();
                await Task.Yield();
                await GenerateShrubs(island.size, island.center, (MapGenerator.biome)island.biome, island.index);

            }
            DungeonGenerator.instance.CreateDungeons();
            menu.instance.GenerateDungeonCircles();

            if (gameManager.instance.loadingScreenInfo)
            {
                gameManager.instance.connectingText.text = "Putting Rocks in the Ocean...";
            }

            oceanStuffTakenPos.Clear();
            await Task.Yield();
            await GenerateOceanStuff(2100);

            short i = 0;
            foreach (Island island in MapGenerator.instance.islands)
            {
                mineralsTakenPos.Clear();
                itemsTakenPos.Clear();
                takenPositions.Clear();

                if (SingletonRunner.runner.IsSharedModeMasterClient)
                {
                    if (gameManager.instance.loadingScreenInfo)
                        gameManager.instance.connectingText.text = "Placing Unique Items in " + ((MapGenerator.biome)island.biome).ToString();
                    await Task.Yield();
                    await GenerateUniqueItems(island.size, island.center, (MapGenerator.biome)island.biome, i);
                }
                if (gameManager.instance.loadingScreenInfo)
                    gameManager.instance.connectingText.text = "Placing Ores in " + ((MapGenerator.biome)island.biome).ToString();
                await Task.Yield();
                await GenerateMinerals(inGame, island.size, island.center, (MapGenerator.biome)island.biome, i);
                if (gameManager.instance.loadingScreenInfo)
                    gameManager.instance.connectingText.text = "Planting Trees in " + ((MapGenerator.biome)island.biome).ToString();
                await Task.Yield();
                await GenerateTrees(inGame, island.size, island.center, (MapGenerator.biome)island.biome, i);
                if (gameManager.instance.loadingScreenInfo)
                    gameManager.instance.connectingText.text = "Placing Items in " + ((MapGenerator.biome)island.biome).ToString();
                await Task.Yield();
                await GenerateItems(inGame, island.size, island.center, (MapGenerator.biome)island.biome, i);


                await Task.Yield();
                i++;
            }
            RPC_FinishGenerating();

            if (SingletonRunner.runner.IsSharedModeMasterClient)
            {
                await Task.Delay(300);

                while (generating)
                {
                    gameManager.instance.connectingText.text = "Syncing Data...";
                    await Task.Yield();
                }
                treeRPCs.Instance.SendHealths();
            }


        }
        else
        {
            generating = false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_FinishGenerating()
    {
        playersDoneGenerating++;
        if (playersDoneGenerating >= SingletonRunner.runner.SessionInfo.PlayerCount)
        {
            generating = false;
        }
    }

    async Task GenerateTrees(bool inGame, int mapSize, Vector2Int center, MapGenerator.biome biome, short islandIndex)
    {
        System.Random rng = new System.Random(seed.instance.currentSeed + islandIndex);

        takenPositions.Add(new Vector3Int(0, 0, 0));

        treeChop.lastId = 0;

        int frameCounter = 0;

        currentFeatureIndex = 0;
        foreach (feature Feature in features)
        {
            if (!Feature.spawnsInBiomes.Contains(biome))
            {
                currentFeatureIndex++;
                continue;
            }

            List<NewFeature> newFeatures = new List<NewFeature>();

            for (int i = Mathf.RoundToInt(Feature.count * mapSize / MapGenerator.mapChunkSize * featuresAmountMultiplier); i > 0; i--)
            {
                bool spawned = false;

                int failsafe = 0;
                while (!spawned && failsafe < failsafeAmt)
                {
                    failsafe++;

                    frameCounter++;

                    if (frameCounter % frameYieldInterval == 0)
                    {
                        await Task.Yield();
                    }

                    Vector3Int potentialPosition = new Vector3Int(Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2)), Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2))) + (Vector3Int)center;

                    if (CheckCanSpawnOnTile(Feature.spawningTiles, potentialPosition, Feature.overrideTilemap) || CheckCanSpawnOnTile(Feature.spawningTiles, potentialPosition, MapDisplay.Instance.roofTilemap))
                    {

                        bool tooClose = false;

                        foreach (Vector3Int pos in takenPositions)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < minDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3Int pos in itemsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < itemsMinDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3Int pos in uItemsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < uItemsMinDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3Int pos in mineralsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < minDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3 pos in shrubsTakenPos)
                        {
                            float greaterMinDist = shrubsMinDist;
                            if (pos.z > greaterMinDist) greaterMinDist = pos.z;

                            if (Vector2.Distance(new Vector2(potentialPosition.x, potentialPosition.y), new Vector2(pos.x, pos.y)) < greaterMinDist)
                            {
                                tooClose = true;
                            }

                        }

                        if (!tooClose)
                        {
                            newFeatures.Add(new NewFeature(new Vector2Int(Mathf.RoundToInt(groundTilemap.CellToWorld(potentialPosition).x), Mathf.RoundToInt(groundTilemap.CellToWorld(potentialPosition).y)), (short)rng.Next(0, Feature.gameObjects.Length), currentFeatureIndex, rng.Next(0, 2) == 1));

                            takenPositions.Add(potentialPosition);

                            spawned = true;
                        }


                    }
                    else
                    {
                        spawned = false;
                    }
                }

                if (failsafe >= failsafeAmt)
                {
                    Debug.Log("Tree spawning failsafe for " + Feature.name + " in " + biome.ToString());
                    break;
                }

            }

            treeRPCs.Instance.CreateAllTrees(newFeatures, 0, islandIndex);

            currentFeatureIndex++;
        }

        //generatingTrees = false;
    }
    async Task GenerateMinerals(bool inGame, int mapSize, Vector2Int center, MapGenerator.biome biome, short islandIndex)
    {
        int frameCounter = 0;

        System.Random rng = new System.Random(seed.instance.currentSeed + islandIndex + 10);

        mineralsTakenPos.Add(new Vector3Int(0, 0, 0));

        currentFeatureIndex = 0;
        foreach (feature Feature in minerals)
        {
            if (Feature.ignoreMain && mapSize == MapGenerator.mapChunkSize)
            {
                currentFeatureIndex++;
                continue;
            }
            if (!Feature.spawnsInBiomes.Contains(biome))
            {
                currentFeatureIndex++;
                continue;
            }
            List<NewFeature> newFeatures = new List<NewFeature>();

            for (int i = Mathf.RoundToInt(Feature.count * mapSize / MapGenerator.mapChunkSize * featuresAmountMultiplier); i > 0; i--)
            {
                bool spawned = false;


                int failsafe = 0;
                while (!spawned && failsafe < failsafeAmt)
                {
                    failsafe++;

                    frameCounter++;

                    if (frameCounter % frameYieldInterval == 0)
                    {
                        await Task.Yield();
                    }

                    Vector3Int potentialPosition = new Vector3Int(Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2)), Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2))) + (Vector3Int)center;

                    if (CheckCanSpawnOnTile(Feature.spawningTiles, potentialPosition, Feature.overrideTilemap) || CheckCanSpawnOnTile(Feature.spawningTiles, potentialPosition, MapDisplay.Instance.roofTilemap))
                    {

                        bool tooClose = false;

                        foreach (Vector3Int pos in mineralsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < mineralsMinDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3Int pos in takenPositions)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < mineralsMinDist)
                            {
                                tooClose = true;
                            }

                        }

                        foreach (Vector3 pos in shrubsTakenPos)
                        {
                            float greaterMinDist = shrubsMinDist;
                            if (pos.z > greaterMinDist) greaterMinDist = pos.z;

                            if (Vector2.Distance(new Vector2(potentialPosition.x, potentialPosition.y), new Vector2(pos.x, pos.y)) < greaterMinDist)
                            {
                                tooClose = true;
                            }

                        }

                        foreach (Vector3Int pos in itemsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < itemsMinDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3Int pos in uItemsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < uItemsMinDist)
                            {
                                tooClose = true;
                            }

                        }


                        if (!tooClose)
                        {
                            newFeatures.Add(new NewFeature(new Vector2Int(Mathf.RoundToInt(groundTilemap.CellToWorld(potentialPosition).x), Mathf.RoundToInt(groundTilemap.CellToWorld(potentialPosition).y)), (short)rng.Next(0, Feature.gameObjects.Length), currentFeatureIndex, rng.Next(0, 2) == 1));

                            mineralsTakenPos.Add(potentialPosition);

                            spawned = true;
                        }


                    }
                    else
                    {
                        spawned = false;
                    }

                }
                if (failsafe >= failsafeAmt)
                {
                    Debug.Log("Ore spawning failsafe for " + Feature.name + " in " + biome.ToString());
                    break;
                }


            }

            short typ = 1;
            if (Feature.overrideFeatureType != -1) typ = (short)Feature.overrideFeatureType;

            treeRPCs.Instance.CreateAllTrees(newFeatures, typ, islandIndex);
            currentFeatureIndex++;
        }

        //generatingMinerals = false;
    }




    async Task GenerateShrubs(int mapSize, Vector2Int center, MapGenerator.biome biome, int islandIndex)
    {
        int frameCounter = 0;

        var rng = new System.Random(seed.instance.currentSeed + islandIndex + 20);

        var instance = MapDisplay.Instance;
        var roofTilemap = instance.roofTilemap;

        var island = treeRPCs.Instance.islandFeatures[islandIndex];

        float halfSize = mapSize * 0.5f;

        foreach (var feature in shrubs)
        {
            if (!feature.spawnsInBiomes.Contains(biome))
                continue;

            float baseCount = feature.count * mapSize / MapGenerator.mapChunkSize * featuresAmountMultiplier;
            int spawnCount = Mathf.RoundToInt(baseCount);

            float minDistBase = feature.overrideMinDist != -1 ? feature.overrideMinDist : shrubsMinDist;

            var prefabArray = feature.gameObjects;
            var spawnTiles = feature.spawningTiles;
            var overrideTiles = feature.overrideTilemap;

            for (int i = 0; i < spawnCount; i++)
            {
                bool spawned = false;
                int failsafe = 0;

                while (!spawned && failsafe < failsafeAmt)
                {
                    failsafe++;

                    if (++frameCounter % frameYieldInterval == 0)
                        await Task.Yield();

                    Vector3Int potentialPosition =
                        new Vector3Int(
                            rng.Next(-(int)halfSize, (int)halfSize),
                            rng.Next(-(int)halfSize, (int)halfSize),
                            0
                        ) + (Vector3Int)center;

                    bool validTile =
                        CheckCanSpawnOnTile(spawnTiles, potentialPosition, overrideTiles) ||
                        CheckCanSpawnOnTile(spawnTiles, potentialPosition, roofTilemap);

                    if (!validTile)
                        continue;

                    float minDist = minDistBase;

                    bool tooClose = false;
                    Vector2 p = new Vector2(potentialPosition.x, potentialPosition.y);

                    for (int j = 0; j < shrubsTakenPos.Count; j++)
                    {
                        Vector3 pos = shrubsTakenPos[j];

                        float effectiveDist = pos.z < minDist ? pos.z : minDist;

                        if ((p - new Vector2(pos.x, pos.y)).sqrMagnitude < effectiveDist * effectiveDist)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                        continue;

                    GameObject featureObj =
                        Instantiate(prefabArray[rng.Next(prefabArray.Length)],
                            groundTilemap.CellToWorld(potentialPosition),
                            Quaternion.identity,
                            shrubHolder);

                    shrubsList.Add(featureObj);
                    shrubsTakenPos.Add(new Vector3(potentialPosition.x, potentialPosition.y, minDist));

                    island.shrubs.Add(featureObj);

                    if (feature.randomRotate)
                        featureObj.transform.eulerAngles = new Vector3(0, 0, rng.Next(0, 4) * 90);

                    var sr = featureObj.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                        sr.flipX = rng.Next(0, 2) == 1;

                    spawned = true;
                }

                if (failsafe >= failsafeAmt)
                {
                    Debug.Log($"Shrub spawning failsafe for {feature.name} in {biome}");
                    break;
                }
            }
        }
    }

    async Task GenerateOceanStuff(int mapSize)
    {
        System.Random rng = new System.Random(seed.instance.currentSeed + mapSize);

        List<Vector3> positions = new List<Vector3>();

        int frameCounter = 0;

        currentFeatureIndex = 0;
        foreach (feature Feature in oceanStuff)
        {
            List<NewFeature> newFeatures = new List<NewFeature>();

            for (int i = Mathf.RoundToInt(Feature.count * mapSize / MapGenerator.mapChunkSize * featuresAmountMultiplier); i > 0; i--)
            {
                bool spawned = false;

                int failsafe = 0;
                while (!spawned && failsafe < failsafeAmt)
                {
                    failsafe++;

                    frameCounter++;

                    if (frameCounter % frameYieldInterval == 0)
                    {
                        await Task.Yield();
                    }

                    Vector3Int potentialPosition = new Vector3Int(Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2)), Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2)));

                    if (!groundTilemap.GetTile(potentialPosition) && CheckCircleForTiles(new TileBase[] { null },waterTilemap,potentialPosition,8,false) && !hillTilemap.GetTile(potentialPosition))
                    {
                        bool tooClose = false;

                        foreach (Vector3Int pos in oceanStuffTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < oceanStuffMinDist)
                            {
                                tooClose = true;
                            }

                        }

                        if (!tooClose)
                        {
                            if (!treeRPCs.Instance.destroyedRocks.ContainsKey(groundTilemap.CellToWorld(potentialPosition)))
                                positions.Add(potentialPosition);

                            oceanStuffTakenPos.Add(potentialPosition);

                            spawned = true;
                            newFeatures.Add(new NewFeature((Vector3)potentialPosition, (short)rng.Next(0, Feature.gameObjects.Length), currentFeatureIndex, rng.Next(0, 2) == 1));
                        }


                    }
                    else
                    {
                        spawned = false;
                    }

                }
                if (failsafe >= failsafeAmt)
                {
                    Debug.Log("Ocean stuff spawning failsafe for " + Feature.name);
                    break;
                }


            }
            
            treeRPCs.Instance.CreateOceanRocks(newFeatures);
            currentFeatureIndex++;
        }

    }

    async Task GenerateItems(bool inGame, int mapSize, Vector2Int center, MapGenerator.biome biome, short islandIndex)
    {
        System.Random rng = new System.Random(seed.instance.currentSeed + islandIndex + 30);

        itemsTakenPos.Add(new Vector3Int(0, 0, 0));

        int frameCounter = 0;

        foreach (ItemFeature Feature in items)
        {

            if (!Feature.spawnsInBiomes.Contains(biome))
            {
                continue;
            }
            List<NewItem> newItems = new List<NewItem>();

            for (int i = Mathf.RoundToInt(Feature.count * mapSize / MapGenerator.mapChunkSize * featuresAmountMultiplier); i > 0; i--)
            {
                bool spawned = false;

                int failsafe = 0;
                while (!spawned && failsafe < failsafeAmt)
                {
                    failsafe++;

                    frameCounter++;

                    if (frameCounter % frameYieldInterval == 0)
                    {
                        await Task.Yield();
                    }

                    Vector3Int potentialPosition = new Vector3Int(Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2)), Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2))) + (Vector3Int)center;

                    if (CheckCanSpawnOnTile(Feature.spawningTiles, potentialPosition, groundTilemap, 2) || CheckCanSpawnOnTile(Feature.spawningTiles, potentialPosition, MapDisplay.Instance.roofTilemap, 2))
                    {

                        bool tooClose = false;

                        foreach (Vector3Int pos in itemsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < itemsMinDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3Int pos in takenPositions)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < itemsMinDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3Int pos in mineralsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < itemsMinDist)
                            {
                                tooClose = true;
                            }

                        }
                        foreach (Vector3Int pos in uItemsTakenPos)
                        {
                            if (Vector3Int.Distance(potentialPosition, pos) < uItemsMinDist)
                            {
                                tooClose = true;
                            }

                        }

                        if (!tooClose)
                        {
                            //spawn feature

                            if (Runner.IsConnectedToServer)
                            {
                                newItems.Add(new NewItem(new Vector2Int(Mathf.RoundToInt(groundTilemap.CellToWorld(potentialPosition).x), Mathf.RoundToInt(groundTilemap.CellToWorld(potentialPosition).y)), rng.Next(Feature.countRangeIncl.x, Feature.countRangeIncl.y + 1)));
                            }
                            itemsTakenPos.Add(potentialPosition);

                            spawned = true;

                        }


                    }
                    else
                    {
                        spawned = false;
                    }

                }
                if (failsafe >= failsafeAmt)
                {
                    print("item spawning failsafe for " + Feature.name + " in " + biome);
                    break;
                }


            }
            /*
            if (Runner.SessionInfo && Runner.IsSharedModeMasterClient)
            {
                //create all items is called this amount of times
                int chunkCount = newItems.Count / 20;

                var allItems = newItems.ToArray();

                int chunkSize = Mathf.CeilToInt((float)allItems.Length / chunkCount);

                for (int i = 0; i < chunkCount; i++)
                {
                    int start = i * chunkSize;
                    int length = Mathf.Min(chunkSize, allItems.Length - start);

                    var chunkArray = new ArraySegment<NewItem>(allItems, start, length).ToArray();

                    string chunkString = treeRPCs.ArrayToString(chunkArray);

                }
            }
            */
            treeRPCs.Instance.CreateAllItems(newItems, Feature.Item.itemId, islandIndex);
        }

        //generatingItems = false;


    }

    async Task GenerateUniqueItems(int mapSize, Vector2Int center, MapGenerator.biome biome, short islandIndex)
    {
        if ((int)biome >= uniqueItemBiomes.Length || uniqueItemBiomes[(int)biome].items[0] == null) return;

        System.Random rng = new System.Random(seed.instance.currentSeed + islandIndex + 40);

        int loops = 0;

        int frameCounter = 0;

        while (true)
        {
            Vector3Int potentialPosition = new Vector3Int(Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2)), Mathf.RoundToInt(rng.Next(-mapSize / 2, mapSize / 2))) + (Vector3Int)center;

            if ((CheckCanSpawnOnTile(uniqueItemBiomes[(int)biome].spawningTiles, potentialPosition, groundTilemap) || CheckCanSpawnOnTile(uniqueItemBiomes[(int)biome].spawningTiles, potentialPosition, MapDisplay.Instance.roofTilemap))
                && !(islandIndex == 0 && potentialPosition.magnitude < 110))
            {
                GameObject newObj = SingletonRunner.runner.Spawn(uniqueItemBiomes[(int)biome].items[biomeUniqueItemsIndex[(int)biome]], potentialPosition, Quaternion.identity).gameObject;

                newObj.GetComponentInChildren<itemPickup>().naturalItemID = (uItemsTakenPos.Count + 1) * -1;
                uItemsTakenPos.Add(potentialPosition);

                uniqueItemBiomes[(int)biome].alreadySpawned = true;
                RPC_InitializeUniqueItem(islandIndex, newObj.GetComponent<NetworkObject>().Id, potentialPosition);

                //spawn each type only once
                biomeUniqueItemsIndex[(int)biome]++;

                break;
            }

            frameCounter++;

            if (frameCounter % frameYieldInterval == 0)
            {
                await Task.Yield();
            }

            loops++;
            if (loops > 5000)
            {
                print("unique item failsafe for " + biome);
                break;
            }
        }



    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    async void RPC_InitializeUniqueItem(short islandIndex, NetworkId photonID, Vector3Int pos)
    {
        if (islandIndex >= MapGenerator.instance.islands.Count()) return;

        MapGenerator.instance.islands[islandIndex].containsUniqueTool = true;
        MapGenerator.instance.islands[islandIndex].uniqueToolPos = (Vector3)pos;
        NetworkObject obj = null;

        float t = 0;
        while (!obj)
        {
            Runner.TryFindObject(photonID, out obj);
            t += Time.deltaTime;
            await Task.Yield();

            if (t > 15) return;
        }

        obj.transform.SetParent(uniqueItemHolder, true);

        if (obj.GetComponentInChildren<UniqueItemProximity>())
            obj.GetComponentInChildren<UniqueItemProximity>().island = islandIndex;

        itemPickup itemPick = obj.GetComponentInChildren<itemPickup>();
        itemPick.itemData = inventory.CreateItemData(itemPick.Item, 3);

        treeRPCs.Instance.uniqueItems.Add(obj.GetComponentInChildren<itemPickup>());

        obj.transform.position = pos;

    }

    async Task GenerateDungeonEntrances(int mapSize, Vector2Int center, MapGenerator.biome biome, short islandIndex)
    {
        if ((int)biome >= dungeonFeatures.Length || dungeonFeatures[(int)biome].dungeon == null) return;

        System.Random rng = new System.Random(seed.instance.currentSeed + islandIndex + 50);

        int loops = 0;

        int frameCounter = 0;

        DungeonFeature d = dungeonFeatures[(int)biome];

    LoopStart:
        while (true)
        {
            Vector3Int potentialPosition = new Vector3Int(Mathf.RoundToInt(rng.Next(-mapSize / 3, mapSize / 3)), Mathf.RoundToInt(rng.Next(-mapSize / 3, mapSize / 3))) + (Vector3Int)center;

            loops++;
            if (loops > 5000)
            {
                print("dungeon entrance spawning failsafe for " + biome);
                break;
            }

            for (int x = -d.spawnDimensions.x / 2; x < d.spawnDimensions.x / 2; x++)
            {
                for (int y = -d.spawnDimensions.y / 2; y < d.spawnDimensions.y / 2; y++)
                {
                    if (!d.spawningTiles.Contains(groundTilemap.GetTile(potentialPosition + new Vector3Int(x, y))))
                    {
                        goto LoopStart;
                    }

                    frameCounter++;

                    if (frameCounter % frameYieldInterval == 0)
                    {
                        await Task.Yield();
                    }
                }
            }

            dungeonFeatures[(int)biome].selectedPosition = (Vector2Int)potentialPosition;
            dungeonFeatures[(int)biome].alreadySpawned = true;

            // Clear hills around entrance
            for (int x = -d.spawnDimensions.x / 2; x < d.spawnDimensions.x / 2; x++)
            {
                for (int y = -d.spawnDimensions.y / 2; y < d.spawnDimensions.y / 2; y++)
                {
                    hillTilemap.SetTile(
                        potentialPosition + new Vector3Int(x, y, 0),
                        null
                    );
                }
            }

            Instantiate(dungeonFeatures[(int)biome].dungeon, (Vector3)potentialPosition, Quaternion.identity, dungeonEntranceHolder);
            
            break;
        }



    }

    bool CheckCanSpawnOnTile(TileBase[] spawningTiles, Vector3Int potentialPosition, Tilemap tilemap, int waterDist = 3)
    {
        if (!(Mathf.Abs(planeCrash.position.x - potentialPosition.x) > planeCheckSize.x / 2 || Mathf.Abs(planeCrash.position.y - potentialPosition.y) > planeCheckSize.y / 2)) return false;

        foreach (DungeonFeature d in dungeonFeatures)
        {
            if (!d.alreadySpawned) continue;

            if (!(Mathf.Abs(d.selectedPosition.x - potentialPosition.x) > d.spawnDimensions.x / 2 || Mathf.Abs(d.selectedPosition.y - potentialPosition.y) > d.spawnDimensions.y / 2)) return false;
        }

        //ground tilemap
        if (!tilemap)
        {
            tilemap = groundTilemap;

            bool nearWater = CheckCircleForTiles(new TileBase[] { MapDisplay.Instance.shallowWater }, waterTilemap, potentialPosition, waterDist)
                            || CheckCircleForTiles(new TileBase[0], groundTilemap, potentialPosition, waterDist, true);
            return (!nearWater && spawningTiles.Contains(tilemap.GetTile(potentialPosition)) && CheckCircleForTiles(spawningTiles, tilemap, potentialPosition, 2)
                && CheckCircleForTiles(new TileBase[] { null }, hillTilemap, potentialPosition, 2, false)
                && CheckCircleForTiles(new TileBase[] { null }, MapDisplay.Instance.lavaTilemap, potentialPosition, 3, false));

        }
        //other tilemaps like water
        else
        {
            return (spawningTiles.Contains(tilemap.GetTile(potentialPosition)) && CheckCircleForTiles(spawningTiles, tilemap, potentialPosition, 2)
                && CheckCircleForTiles(new TileBase[] { null }, hillTilemap, potentialPosition, 2, false)
                && CheckCircleForTiles(new TileBase[] { null }, MapDisplay.Instance.lavaTilemap, potentialPosition, 2, false));
        }
    }

    public bool CheckCircleForTiles(TileBase[] allowedTiles, Tilemap tilemap, Vector3Int center, int radius = 2, bool requireEmpty = false)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (Vector3Int.Distance(center, pos) > radius)
                    continue;

                TileBase tile = tilemap.GetTile(pos);

                if (requireEmpty)
                {
                    // Must be no tile at all
                    if (tile != null)
                        return false;
                }
                else
                {
                    // Tile must be one of the allowed tiles
                    if (!allowedTiles.Contains(tile))
                        return false;
                }
            }
        }

        return true;
    }


    public void RemoveFeatures()
    {
        foreach (GameObject feature in treesList)
        {
            DestroyImmediate(feature);
        }
        treesList.Clear();
        takenPositions.Clear();
        DestroyAllChildren(treeHolder);

        foreach (GameObject feature in shrubsList)
        {
            DestroyImmediate(feature);
        }
        shrubsList.Clear();
        shrubsTakenPos.Clear();
        DestroyAllChildren(shrubHolder);

        foreach (GameObject feature in itemsList)
        {
            DestroyImmediate(feature);
        }
        itemsList.Clear();
        itemsTakenPos.Clear();
        DestroyAllChildren(itemHolder);

        foreach (GameObject feature in mineralsList)
        {
            DestroyImmediate(feature);
        }
        mineralsList.Clear();
        mineralsTakenPos.Clear();
        DestroyAllChildren(mineralHolder);

    }

    public void DestroyAllChildren(Transform parentTransform)
    {
        int childCount = parentTransform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject childObject = parentTransform.GetChild(i).gameObject;
            DestroyImmediate(childObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(planeCrash.position, planeCheckSize);

        foreach (DungeonFeature d in dungeonFeatures)
        {
            if (d.selectedPosition != Vector2.zero)
            {
                Gizmos.DrawWireCube((Vector2)d.selectedPosition, (Vector2)d.spawnDimensions);
            }

        }

    }

}
[Serializable]
public class feature
{
    public string name = "Tree";
    public GameObject[] gameObjects;
    public TileBase[] spawningTiles;
    public Tilemap overrideTilemap;
    public int count;
    public bool ignoreMain;
    public MapGenerator.biome[] spawnsInBiomes = new MapGenerator.biome[1];
    public bool randomRotate = false;
    [Tooltip("-1 for default")]
    public float overrideMinDist = -1;
    public int overrideFeatureType = -1;

}
[Serializable]
public class ItemFeature
{
    public string name = "Rock";
    public item Item;
    public Vector2Int countRangeIncl;
    public TileBase[] spawningTiles;
    public int count;
    public bool ignoreMainIsland;
    public MapGenerator.biome[] spawnsInBiomes = new MapGenerator.biome[1];

}
[Serializable]
public class UniqueItemBiome
{
    public string name;
    public GameObject[] items;
    public TileBase[] spawningTiles;
    public bool alreadySpawned;
}
[Serializable]
public class DungeonFeature
{
    public string name;
    public GameObject dungeon;
    public TileBase[] spawningTiles;
    public Vector2Int spawnDimensions;
    public bool alreadySpawned;
    public Vector2Int selectedPosition;
}

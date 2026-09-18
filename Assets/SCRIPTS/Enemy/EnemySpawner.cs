using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawner : NetworkBehaviour
{
    public static EnemySpawner instance;

    const float minSpawnDistFromPlayer = 17;
    public TimeManager time;
    //public int maxEnemies = 30;
    public float enemySpawnCooldown = 2;
    float cooldown;
    float oceanCooldown;
    public List<GameObject> enemies = new List<GameObject>();
    public BiomeEnemies[] biomeEnemies;
    public TileBase[] spawningTiles;
    public Tilemap groundTilemap;
    public float maxEnemiesIncrementPerDay = 0.05f;


    public List<MapGenerator.biome> activeBiomes = new List<MapGenerator.biome>();

    int dayNumAdditional;
    public float maxEnemiesMult = 1;
    public float hardMaxEnemiesMult = 1.1f;
    public float dementiaMaxEnemiesMult = 1.3f;

    bool tryingToSpawn;

    bool finishedLoadingIslands;
    public bool disableDequeue;

    [Header("Ocean Despawn")]
    public float oceanEnemyDespawnDistance = 100f;
    public float oceanEnemyDespawnInterval = 2f;
    public int oceanEnemiesCheckedPerPass = 10;

    private float oceanDespawnTimer;
    private int oceanVarietyIndex;
    private int oceanEnemyIndex;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        foreach (BiomeEnemies biome in biomeEnemies)
        {
            foreach (EnemyVariety v in biome.varieties)
            {
                v.startingMax = v.maxAmount;
            }
        }
    }

    public void IncrementEnemyMax()
    {
        dayNumAdditional = 0;
        if (gameManager.instance.difficulty == 2)
        {
            dayNumAdditional = 1;
            maxEnemiesMult = dementiaMaxEnemiesMult;
        }
        if (gameManager.instance.difficulty == 1)
        {
            dayNumAdditional = 1;
            maxEnemiesMult = hardMaxEnemiesMult;
        }

        foreach (BiomeEnemies biome in biomeEnemies)
        {
            for (int i = 0; i < biome.varieties.Length; i++)
            {
                EnemyVariety v = biome.varieties[i];

                //less of existing enemies when a new one shows up
                if (i != biome.varieties.Length - 1 && biome.varieties[i + 1].startDay <= time.dayNum + dayNumAdditional)
                    v.startingMax = v.maxAmountAfterNext;

                if (biome.ignoreIncrement) continue;

                v.maxAmount = v.startingMax + Mathf.RoundToInt(v.startingMax * (maxEnemiesIncrementPerDay * Mathf.Clamp(TimeManager.instance.dayNum + dayNumAdditional, 1, 12)));

                if (playerSpawner.playerCountInGame == 1)
                {
                    v.maxAmount = Mathf.RoundToInt(v.maxAmount * biome.singleplayerMult);
                }

            }
        }

    }

    //called in mapdisplay when islands are finished
    public void SetIslands()
    {
        foreach (Island island in MapGenerator.instance.islands)
        {
            foreach (EnemyVariety v in biomeEnemies[island.biome].varieties)
            {
                v.islandEnemyObjects.TryAdd(island.index, new List<GameObject>());
            }
        }
        //boss
        foreach (EnemyVariety v in biomeEnemies[6].varieties)
            v.islandEnemyObjects.TryAdd(0, new List<GameObject>());

        //ocean
        foreach (EnemyVariety v in biomeEnemies[7].varieties)
            v.islandEnemyObjects.TryAdd(0, new List<GameObject>());

        finishedLoadingIslands = true;
    }


    private void FixedUpdate()
    {
        if (!player.instance ||
            gameManager.instance.disableEnemies ||
            gameManager.inSecondPhase ||
            !MapDisplay.finishedLoading ||
            !finishedLoadingIslands ||
            !SingletonRunner.runner ||
            !SingletonRunner.runner.IsSharedModeMasterClient ||
            disableDequeue)
            return;


        cooldown -= Time.fixedDeltaTime;
        oceanCooldown -= Time.fixedDeltaTime;
        enemies.RemoveAll(e => e == null);

        if (cooldown < 0)
        {

            if (time.isNight ||
            activeBiomes.Contains(MapGenerator.biome.volcanic) ||
            activeBiomes.Contains(MapGenerator.biome.vale) ||
            (gameManager.inBoss && !Boss.instance.dead))
            {

                // Island enemies
                foreach (Island island in MapGenerator.instance.islands)
                {
                    foreach (EnemyVariety v in biomeEnemies[island.biome].varieties)
                    {
                        if (v.islandEnemyObjects.TryGetValue(island.index, out List<GameObject> enemyList))
                            enemyList.RemoveAll(e => e == null);
                    }

                    bool playerOnIsland = false;

                    foreach (player p in playerSpawner.instance.players)
                    {
                        if (!p || p.inDungeon > -1)
                            continue;

                        if (p.currentIsland.center == island.center && p.onIsland)
                        {
                            playerOnIsland = true;
                            break;
                        }
                    }

                    if (!playerOnIsland)
                        continue;

                    int biome = island.biome;

                    if (gameManager.inBoss)
                        biome = 6;

                    if (!time.isNight && biome != 4 && biome != 5 && biome != 6)
                        continue;

                    int varietyNum = GetAvailableVariety(biome);

                    if (varietyNum == -1)
                    {
                        cooldown = enemySpawnCooldown;
                        continue;
                    }

                    EnemyVariety variety = biomeEnemies[biome].varieties[varietyNum];

                    if (!tryingToSpawn &&
                        variety.islandEnemyObjects[island.index].Count <
                        variety.maxAmount * maxEnemiesMult)
                    {
                        tryingToSpawn = true;
                        StartCoroutine(_ChooseIslandSpawnPos(biome, varietyNum, island));
                    }
                    else
                    {
                        cooldown = enemySpawnCooldown;
                    }
                }
            }
            else if (enemyHealth.deathQueue.Count > 0)
            {
                enemyHealth enemy = enemyHealth.deathQueue.Dequeue();

                if (enemy && !enemy.dead)
                    enemy.RPC_Die();
            }
        }


        if (oceanCooldown <= 0)
        {
            if (!gameManager.inBoss && HasPlayerAtSea())
            {
                foreach (EnemyVariety v in biomeEnemies[7].varieties)
                {
                    if (v.islandEnemyObjects.TryGetValue(0, out List<GameObject> enemyList))
                        enemyList.RemoveAll(e => e == null);
                }

                // Ocean enemies
                int oceanVarietyNum = GetAvailableVariety(7);

                if (oceanVarietyNum != -1)
                {
                    EnemyVariety oceanVariety = biomeEnemies[7].varieties[oceanVarietyNum];

                    if (!tryingToSpawn &&
                        oceanVariety.islandEnemyObjects[0].Count <
                        oceanVariety.maxAmount * maxEnemiesMult)
                    {
                        tryingToSpawn = true;
                        StartCoroutine(_ChooseOceanSpawnPos(oceanVarietyNum));
                    }
                    else
                    {
                        oceanCooldown = enemySpawnCooldown;
                    }
                }

            }
            else
            {
                oceanCooldown = enemySpawnCooldown * 3;
            }

        }
        UpdateOceanEnemyDespawn();

    }

    private int GetAvailableVariety(int biome)
    {
        EnemyVariety[] varieties = biomeEnemies[biome].varieties;

        int day = TimeManager.instance.dayNum + dayNumAdditional;

        int availableCount = 0;

        for (int i = 0; i < varieties.Length; i++)
        {
            if (day >= varieties[i].startDay)
                availableCount++;
        }

        if (availableCount == 0)
            return -1;

        int selected = Random.Range(0, availableCount);

        for (int i = 0; i < varieties.Length; i++)
        {
            if (day >= varieties[i].startDay)
            {
                if (selected == 0)
                    return i;

                selected--;
            }
        }

        return -1;
    }

    private IEnumerator _ChooseIslandSpawnPos(int biome, int varietyNum, Island island)
    {
        cooldown = enemySpawnCooldown;

        bool validPos = false;
        Vector3Int potentialPosition = Vector3Int.zero;

        int frameCounter = 0;

        EnemyVariety variety = biomeEnemies[biome].varieties[varietyNum];

        while (!validPos)
        {
            potentialPosition = new Vector3Int(
                Mathf.RoundToInt(Random.Range(-island.size / 2f, island.size / 2f)),
                Mathf.RoundToInt(Random.Range(-island.size / 2f, island.size / 2f)),
                0
            ) + (Vector3Int)island.center;

            bool validTile =
                spawningTiles.Contains(groundTilemap.GetTile(potentialPosition)) ||
                spawningTiles.Contains(MapDisplay.Instance.roofTilemap.GetTile(potentialPosition));

            if (!validTile)
            {
                frameCounter++;
                yield return GetSpawnYield(frameCounter);

                if (frameCounter >= 400)
                {
                    Debug.Log("Island enemy spawn position failed.");
                    tryingToSpawn = false;
                    yield break;
                }

                continue;
            }

            if (CheckCircleForNull(MapDisplay.Instance.hillTilemap, potentialPosition, 3, true) ||
                CheckCircleForNull(MapDisplay.Instance.lavaTilemap, potentialPosition, 3, true))
            {
                frameCounter++;
                yield return GetSpawnYield(frameCounter);

                if (frameCounter >= 400)
                {
                    Debug.Log("Island enemy spawn position failed.");
                    tryingToSpawn = false;
                    yield break;
                }

                continue;
            }

            TileData t = gameManager.instance.placedTiles.Find(
                tile => tile.position == new Vector2(potentialPosition.x, potentialPosition.y)
            );

            if (t.tileGameObject)
            {
                frameCounter++;
                yield return GetSpawnYield(frameCounter);

                if (frameCounter >= 400)
                {
                    Debug.Log("Island enemy spawn position failed.");
                    tryingToSpawn = false;
                    yield break;
                }

                continue;
            }

            if (IsTooCloseToPlayer(potentialPosition))
            {
                frameCounter++;
                yield return GetSpawnYield(frameCounter);
                continue;
            }

            if (IsTooCloseToEnemies(
                    potentialPosition,
                    variety.islandEnemyObjects[island.index],
                    variety.minEnemyDist))
            {
                frameCounter++;
                yield return GetSpawnYield(frameCounter);
                continue;
            }

            validPos = true;

            frameCounter++;

            if (frameCounter % 3 == 0)
                yield return null;
        }

        SpawnEnemy(biome, varietyNum, potentialPosition, island.index);
    }

    private bool HasPlayerAtSea()
    {
        player[] players = playerSpawner.instance.players;

        for (int i = 0; i < players.Length; i++)
        {
            player p = players[i];

            if (p != null && !p.onIsland && p.inDungeon == -1)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnEnemy(int biome, int varietyNum, Vector3 position, int islandIndex)
    {
        tryingToSpawn = false;

        GameObject enemy = SingletonRunner.runner.Spawn(
            biomeEnemies[biome].varieties[varietyNum].prefab,
            position,
            Quaternion.identity,
            SingletonRunner.runner.LocalPlayer
        ).gameObject;

        enemy.transform.SetParent(transform, true);

        enemies.Add(enemy);

        biomeEnemies[biome]
            .varieties[varietyNum]
            .islandEnemyObjects[islandIndex]
            .Add(enemy);
    }

    private player GetRandomValidPlayer()
    {
        player[] players = playerSpawner.instance.players;
        List<player> validPlayers = new List<player>();

        // Count valid players.
        for (int i = 0; i < players.Length; i++)
        {
            player p = players[i];

            if (p != null && !p.onIsland && p.inDungeon == -1)
                validPlayers.Add(p);
        }

        if (validPlayers.Count == 0)
            return null;

        return validPlayers[Random.Range(0, validPlayers.Count())];
    }

    private bool IsTooCloseToPlayer(Vector3 position)
    {
        foreach (player p in playerSpawner.instance.players)
        {
            if (!p || p.inDungeon > -1)
                continue;

            if (Vector2.Distance(position, p.transform.position) < minSpawnDistFromPlayer)
                return true;
        }

        return false;
    }

    private bool IsTooCloseToEnemies(
        Vector3 position,
        List<GameObject> enemyList,
        float minDistance)
    {
        if (minDistance <= 0)
            return false;

        foreach (GameObject e in enemyList)
        {
            if (!e)
                continue;

            if (Vector2.Distance(position, e.transform.position) < minDistance)
                return true;
        }

        return false;
    }

    private YieldInstruction GetSpawnYield(int frameCounter)
    {
        return frameCounter % 3 == 0 ? null : null;
    }

    private IEnumerator _ChooseOceanSpawnPos(int varietyNum)
    {
        oceanCooldown = enemySpawnCooldown;

        EnemyVariety variety = biomeEnemies[7].varieties[varietyNum];

        bool validPos = false;
        Vector3 potentialPosition = Vector3.zero;

        int frameCounter = 0;

        while (!validPos)
        {
            if (frameCounter >= 400)
            {
                Debug.Log("Ocean enemy spawn position failed.");
                tryingToSpawn = false;
                yield break;
            }

            player targetPlayer = GetRandomValidPlayer();

            if (!targetPlayer)
            {
                tryingToSpawn = false;
                yield break;
            }

            Vector2 direction = Random.insideUnitCircle.normalized;

            float distance = Random.Range(
                40,
                oceanEnemyDespawnDistance
            );

            potentialPosition = targetPlayer.transform.position +
                                (Vector3)(direction * distance);

            Vector3Int tilePosition = Vector3Int.RoundToInt(potentialPosition);

            // Must be outside every island.
            if (IsInsideAnyIsland(tilePosition))
            {
                frameCounter++;
                yield return GetSpawnYield(frameCounter);

                continue;
            }

            // Don't spawn directly beside a player.
            if (IsTooCloseToPlayer(tilePosition))
            {
                frameCounter++;
                yield return GetSpawnYield(frameCounter);
                continue;
            }

            // Don't overlap another ocean enemy of the same variety.
            if (IsTooCloseToEnemies(
                    tilePosition,
                    variety.islandEnemyObjects[0],
                    variety.minEnemyDist))
            {
                frameCounter++;
                yield return GetSpawnYield(frameCounter);
                continue;
            }


            validPos = true;
        }

        Vector3Int spawnPosition = Vector3Int.RoundToInt(potentialPosition);

        SpawnEnemy(7, varietyNum, spawnPosition, 0);
    }

    private bool IsInsideAnyIsland(Vector3Int position)
    {
        foreach (Island island in MapGenerator.instance.islands)
        {
            float halfSize = island.size / 2f;

            if (position.x >= island.center.x - halfSize &&
                position.x <= island.center.x + halfSize &&
                position.y >= island.center.y - halfSize &&
                position.y <= island.center.y + halfSize)
            {
                return true;
            }
        }

        return false;
    }

    public bool CheckCircleForNull(Tilemap tilemap, Vector3Int centerCoordinate, int radius = 2, bool notNull = false)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int currentCoordinate = new Vector3Int(centerCoordinate.x + x, centerCoordinate.y + y, centerCoordinate.z);

                // Check if the current coordinate is within the circle
                if (Vector3Int.Distance(centerCoordinate, currentCoordinate) <= radius)
                {
                    // Check if the tile at the current coordinate in the specified Tilemap is null
                    if (tilemap.GetTile(currentCoordinate) == notNull)
                    {
                        // There is at least one null tile in the circle
                        return true;
                    }
                }
            }
        }

        // No null tiles found in the circle
        return false;
    }

    private void UpdateOceanEnemyDespawn()
    {
        oceanDespawnTimer -= Time.fixedDeltaTime;

        if (oceanDespawnTimer > 0f)
            return;

        oceanDespawnTimer = oceanEnemyDespawnInterval;

        DespawnFarOceanEnemies();
    }

    private void DespawnFarOceanEnemies()
    {
        EnemyVariety[] varieties = biomeEnemies[7].varieties;

        if (varieties == null || varieties.Length == 0)
            return;

        int checkedCount = 0;
        int varietiesChecked = 0;

        while (checkedCount < oceanEnemiesCheckedPerPass &&
               varietiesChecked < varieties.Length)
        {
            if (oceanVarietyIndex >= varieties.Length)
                oceanVarietyIndex = 0;

            EnemyVariety variety = varieties[oceanVarietyIndex];

            if (!variety.islandEnemyObjects.TryGetValue(0, out List<GameObject> oceanEnemies) ||
                oceanEnemies == null ||
                oceanEnemies.Count == 0)
            {
                oceanVarietyIndex++;
                oceanEnemyIndex = 0;
                varietiesChecked++;
                continue;
            }

            if (oceanEnemyIndex >= oceanEnemies.Count)
            {
                oceanEnemyIndex = 0;
                oceanVarietyIndex++;
                varietiesChecked++;
                continue;
            }

            GameObject enemy = oceanEnemies[oceanEnemyIndex];

            checkedCount++;

            if (!enemy)
            {
                oceanEnemies.RemoveAt(oceanEnemyIndex);
                continue;
            }

            if (!IsEnemyWithinPlayerRange(enemy.transform.position) && !enemy.GetComponent<EnemyOceanHand>().boatTarget)
            {
                NetworkObject networkObject = enemy.GetComponent<NetworkObject>();

                if (networkObject)
                    SingletonRunner.runner.Despawn(networkObject);

                oceanEnemies.RemoveAt(oceanEnemyIndex);
                continue;
            }

            oceanEnemyIndex++;
        }
    }

    private bool IsEnemyWithinPlayerRange(Vector3 position)
    {
        float maxDistanceSqr = oceanEnemyDespawnDistance * oceanEnemyDespawnDistance;

        foreach (player p in playerSpawner.instance.players)
        {
            if (!p || p.onIsland)
                continue;

            if ((p.transform.position - position).sqrMagnitude <= maxDistanceSqr)
                return true;
        }

        return false;
    }
}


[System.Serializable]
public class EnemyVariety
{
    public GameObject prefab;
    public int startDay = 1;
    public int maxAmount = 80;
    public int maxAmountAfterNext;
    [HideInInspector]
    public int startingMax;
    public float minEnemyDist = 0;

    public SerializableDictionary<int, List<GameObject>> islandEnemyObjects;
}

[System.Serializable]
public class BiomeEnemies
{
    public string name;
    public EnemyVariety[] varieties;

    public bool ignoreIncrement;
    public float singleplayerMult = 1;
}


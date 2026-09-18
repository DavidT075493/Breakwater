
using DG.Tweening;
using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : NetworkBehaviour, IDataPersistance
{
    public static DungeonGenerator instance;

    [Header("Dungeon Data")]
    public DungeonRooms[] dungeons;

    [Header("Generation")]
    public int maxRoomsPerDungeon = 15;
    public int maxForks = 3;

    public List<Transform> dungeonInsideHolders = new();
    GameObject lastNormalRoomPrefab = null;
    Unity.Cinemachine.CinemachineConfiner2D confiner;
    public GameObject camBounds;

    public AudioSource battleMusic;
    public bool playerInBattle;

    public List<TorchIgnite> torches = new List<TorchIgnite>();
    public List<DungeonEnemySpawn> combatRooms = new List<DungeonEnemySpawn>();
    int torchCounter = 0;
    System.Random rng;
    int combatIdCounter = 0;
    public SerializableDictionary<int, bool> completedCombatRooms = new SerializableDictionary<int, bool>();
    private int chestCounter = 0;
    int itemCounter = 0;

    public List<DungeonChest> dungeonChests;
    SerializableDictionary<int, string> savedChestData;
    [SerializeField] GameObject itemPrefab;
    public List<int> collectedItems = new List<int>();

    public DungeonModifier[] positiveModifiers, negativeModifiers;
    int forkCount = 0;
    bool startedActiveCheckLoop;
    public event EventHandler onEnterDungeon;

    struct OpenExit
    {
        public Transform anchor;
        public float y;
    }

    private void Awake()
    {
        instance = this;
        battleMusic.volume = 0;
    }

    private void Update()
    {
        if (!player.instance) return;

        if (!startedActiveCheckLoop)
        {
            startedActiveCheckLoop = true;
            InvokeRepeating("CheckDungeonsActive", 0, 0.5f);
        }

        if (!confiner)
            confiner = player.instance.cam.gameObject.GetComponent<Unity.Cinemachine.CinemachineConfiner2D>();
        else
            confiner.enabled = player.instance.inDungeon > -1;
        if (player.instance.inDungeon > -1)
        {
            camBounds.SetActive(true);
            camBounds.transform.position = dungeons[player.instance.inDungeon].position;
        }
        else
        {
            camBounds.SetActive(false);
        }


        if (DungeonEnemySpawn.currentlyInRoom && DungeonEnemySpawn.currentlyInRoom.active && player.instance.inDungeon > -1)
        {
            if (!playerInBattle)
            {
                gameManager.instance.StopAllCoroutines();
                gameManager.instance.musicSource.DOFade(0, 5);
                battleMusic.Play();
                battleMusic.DOFade(1, 1);
                playerInBattle = true;
            }
        }
        else
        {
            if (playerInBattle && !playerHealth.instance.dead)
            {
                gameManager.instance.musicSource.DOFade(1, 1);
                battleMusic.DOFade(0, 1);
                playerInBattle = false;
            }
        }


    }

    public void EnterDungeon()
    {
        onEnterDungeon.Invoke(this,EventArgs.Empty);
    }

    void CheckDungeonsActive()
    {
        List<int> activeDungeons = new List<int>();
        foreach (player p in playerSpawner.instance.players)
        {
            if (p && p.inDungeon != -1)
            {
                activeDungeons.Add(p.inDungeon);
            }
        }
        for (int i = 0; i < dungeonInsideHolders.Count(); i++)
        {
            if (activeDungeons.Contains(i))
            {
                dungeonInsideHolders[i].gameObject.SetActive(true);
            }
            else if(!player.instance.inCutscene)
            {
                dungeonInsideHolders[i].gameObject.SetActive(false);
            }
        }
    }

    public void ClearDungeons()
    {
        foreach (Transform h in dungeonInsideHolders)
            DestroyImmediate(h.gameObject);

        dungeonInsideHolders.Clear();
        combatRooms.Clear();
        dungeonChests.Clear();
        torches.Clear();
        for (int i = 0; i < dungeons.Count(); i++)
        {
            dungeons[i].groundTilemaps.Clear();
            dungeons[i].waterTilemaps.Clear();
            dungeons[i].currentModifiers.Clear();
        }
        chestCounter = 0;
        combatIdCounter = 0;

    }

    public void CreateDungeons()
    {
        ClearDungeons();

        for (int i = 0; i < dungeons.Length; i++)
        {
            if (!dungeons[i].generates)
                continue;

            GameObject holder = new GameObject($"Dungeon {i} Holder");
            holder.transform.parent = transform;
            holder.transform.position = transform.position + new Vector3(-2000 + i * 500, 0);

            dungeonInsideHolders.Add(holder.transform);
            dungeons[i].position = holder.transform.position;

            GenerateDungeon(i, holder.transform);
        }

    }


    // -----------------------------------------------------

    void AddPositiveMod(int dungeonIndex)
    {
        int mIndex = rng.Next(0, positiveModifiers.Length);

        while (dungeons[dungeonIndex].currentModifiers.Find(m => m.id == positiveModifiers[mIndex].id) != null)
        {
            mIndex = rng.Next(0, positiveModifiers.Length);
        }
        dungeons[dungeonIndex].currentModifiers.Add(positiveModifiers[mIndex]);
    }
    void AddNegativeMod(int dungeonIndex)
    {
        int mIndex = rng.Next(0, negativeModifiers.Length);

        while (dungeons[dungeonIndex].currentModifiers.Find(m => m.id == negativeModifiers[mIndex].id) != null)
        {
            mIndex = rng.Next(0, negativeModifiers.Length);
        }
        dungeons[dungeonIndex].currentModifiers.Add(negativeModifiers[mIndex]);
    }

    void GenerateDungeon(int dungeonIndex, Transform holder)
    {
        rng = new System.Random(seed.instance.currentSeed + dungeonIndex + 10);

        int mIndex = rng.Next(0, positiveModifiers.Length);

        AddPositiveMod(dungeonIndex);
        if (dungeonIndex == 0)
            AddPositiveMod(dungeonIndex);

        AddNegativeMod(dungeonIndex);
        if (dungeonIndex == 4) AddNegativeMod(dungeonIndex);

        rng = new System.Random(seed.instance.currentSeed + dungeonIndex);

        forkCount = 0;
        lastNormalRoomPrefab = null;
        DungeonRooms dungeon = dungeons[dungeonIndex];

        HashSet<Vector3Int> occupiedTiles = new();
        List<OpenExit> openExits = new();

        bool midpointPlaced = false;

        // midpoint target range (loose middle)
        int midpointMin = Mathf.FloorToInt(maxRoomsPerDungeon * 0.4f);
        int midpointMax = Mathf.FloorToInt(maxRoomsPerDungeon * 0.7f);
        int midpointTarget = rng.Next(midpointMin, midpointMax + 1);

        // --- START ROOM ---
        DungeonRoom start = PlaceRoomImmediate(
            dungeon.startRoom,
            holder.position,
            holder,
            occupiedTiles
        );

        AddTilemapsToList(start, dungeonIndex);

        foreach (Transform t in start.topAnchors)
            openExits.Add(new OpenExit { anchor = t, y = t.position.y });

        int roomsPlaced = 1;

        // --- MAIN GROWTH ---
        while (openExits.Count > 0 && roomsPlaced < maxRoomsPerDungeon)
        {
            OpenExit exit = openExits[0];
            openExits.RemoveAt(0);

            DungeonRoom newRoom = null;
            bool placed = false;

            // MIDPOINT INSERTION LOGIC
            if (!midpointPlaced && roomsPlaced >= midpointTarget)
            {
                placed = TryPlaceAnyRoom(
                    new[] { dungeon.midpointRoom },
                    exit.anchor,
                    holder,
                    occupiedTiles,
                    dungeon,
                    out newRoom,
                    -1
                );

                if (placed)
                {
                    midpointPlaced = true;
                }
            }

            // Normal placement fallback (prevent same room twice in a row)
            if (!placed)
            {
                GameObject[] candidates = dungeon.normalRooms;

                // If we have a previously placed normal room, filter it out
                if (lastNormalRoomPrefab != null)
                {
                    List<GameObject> filtered = new List<GameObject>();

                    foreach (var room in dungeon.normalRooms)
                    {
                        if (room != lastNormalRoomPrefab && !(room == dungeon.normalRooms[0] && forkCount >= maxForks))
                            filtered.Add(room);
                    }

                    if (roomsPlaced > maxRoomsPerDungeon / 2 - 1 && forkCount == 0)
                    {
                        forkCount = 1;
                        filtered = new GameObject[] { dungeon.normalRooms[0] }.ToList();
                    }
                    // Try without the last used room first
                    if (filtered.Count > 0)
                    {
                        placed = TryPlaceAnyRoom(
                            filtered.ToArray(),
                            exit.anchor,
                            holder,
                            occupiedTiles,
                            dungeon,
                            out newRoom,
                            roomsPlaced * 4
                        );
                    }
                }

                // If nothing else fit, allow the same room again
                if (!placed)
                {
                    placed = TryPlaceAnyRoom(
                        dungeon.normalRooms,
                        exit.anchor,
                        holder,
                        occupiedTiles,
                        dungeon,
                        out newRoom
                    );
                }

                // If we successfully placed a normal room, remember it
                if (placed && newRoom != null)
                {
                    lastNormalRoomPrefab = newRoom.gameObject;
                    if (newRoom.gameObject == dungeon.normalRooms[0])
                    {
                        forkCount++;
                    }
                }
            }

            if (!placed)
            {
                DungeonRoom dead = PlaceDeadEnd(
                    dungeon.deadEndRoom,
                    exit.anchor,
                    holder,
                    occupiedTiles
                );

                if (dead != null)
                    AddTilemapsToList(dead, dungeonIndex);

                continue;
            }

            roomsPlaced++;

            AddTilemapsToList(newRoom, dungeonIndex);

            foreach (Transform t in newRoom.topAnchors)
                openExits.Add(new OpenExit { anchor = t, y = t.position.y });
        }

        // --- END ROOM (highest exit) ---
        if (openExits.Count > 0)
        {
            OpenExit highest = openExits[0];
            foreach (var e in openExits)
                if (e.y > highest.y)
                    highest = e;

            if (TryPlaceAnyRoom(
                new[] { dungeon.endRoom },
                highest.anchor,
                holder,
                occupiedTiles,
                dungeon,
                out var newEnd))
            {
                AddTilemapsToList(newEnd, dungeonIndex);
            }

            openExits.Remove(highest);
        }

        // --- DEAD ENDS FOR REMAINING ---
        foreach (var exit in openExits)
        {
            DungeonRoom dead = PlaceDeadEnd(
                dungeon.deadEndRoom,
                exit.anchor,
                holder,
                occupiedTiles
            );

            if (dead != null)
                AddTilemapsToList(dead, dungeonIndex);
        }

        // --- CALCULATE DUNGEON BOUNDS FROM OCCUPIED TILES ---
        if (occupiedTiles.Count > 0)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (Vector3Int tile in occupiedTiles)
            {
                if (tile.x < minX) minX = tile.x;
                if (tile.y < minY) minY = tile.y;
                if (tile.x > maxX) maxX = tile.x;
                if (tile.y > maxY) maxY = tile.y;
            }

            Vector2Int min = new Vector2Int(minX, minY);
            Vector2Int max = new Vector2Int(maxX, maxY);

            // +1 because tile coords are inclusive
            Vector2Int size = new Vector2Int(
                (max.x - min.x) + 1,
                (max.y - min.y) + 1
            );

            dungeon.size = size;

            dungeon.boundsMin = min;
            dungeon.boundsMax = max;
        }
    }

    void AddCharmItem(DungeonRoom d)
    {
        if (SingletonRunner.runner && SingletonRunner.runner.IsSharedModeMasterClient && d.itemSpawnItem != null)
        {
            if (!collectedItems.Contains(itemCounter))
            {
                d.itemId = itemCounter;
                GameObject thrownItem = Runner.Spawn(itemPrefab, d.itemSpawnPos.position, Quaternion.identity, Runner.LocalPlayer).gameObject;

                thrownItem.transform.SetParent(d.itemSpawnPos);
                //parent is set to boat in itempickup under rpcsetitem

                itemPickup ItemPickup = thrownItem.GetComponent<itemPickup>();
                ItemPickup.SetItem(d.itemSpawnItem.itemId, 1, 1,inventory.CreateItemData(d.itemSpawnItem), true);
                ItemPickup.dungeonItemId = itemCounter;
            }
            itemCounter++;
        }
    }

    void AddTilemapsToList(DungeonRoom d, int dungeonIndex)
    {
        if (!SingletonRunner.runner) return;

        if (d.groundTilemap)
        {
            dungeons[dungeonIndex].groundTilemaps.Add(d.groundTilemap);
            d.groundTilemap.GetComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        }
        if (d.waterTilemap) dungeons[dungeonIndex].waterTilemaps.Add(d.waterTilemap);

        foreach (TorchIgnite t in d.gameObject.GetComponentsInChildren<TorchIgnite>())
        {
            torches.Add(t);
            t.torchIndex = torchCounter;
            torchCounter++;
        }


        DungeonEnemySpawn enemySpawn = d.GetComponentInChildren<DungeonEnemySpawn>();
        if (enemySpawn)
        {
            enemySpawn.dungeonIndex = dungeonIndex;
            enemySpawn.combatRoomId = combatIdCounter;
            combatRooms.Add(enemySpawn);
            if (SingletonRunner.runner.IsSharedModeMasterClient
                && completedCombatRooms.ContainsKey(combatIdCounter) && completedCombatRooms[combatIdCounter] == true)
            {
                RPC_SetInCombat(false, dungeonIndex, combatIdCounter);
            }

            combatIdCounter++;
        }

        RegisterChest(d);
        AddCharmItem(d);
    }

    // -----------------------------------------------------
    // TILE-BASED PLACEMENT
    // -----------------------------------------------------

    bool TryPlaceAnyRoom(
    GameObject[] prefabs,
    Transform exitAnchor,
    Transform holder,
    HashSet<Vector3Int> occupied,
    DungeonRooms dungeon,
    out DungeonRoom placed,
    int additionalForkChance = 0
    )
    {
        placed = null;

        List<GameObject> shuffled = new(prefabs);
        Shuffle(shuffled);

        //add 4 more instances of fork if there is not yet a fork
        if (forkCount == 0)
        {
            for (int i = 0; i < additionalForkChance + 1; i++)
            {
                shuffled.Add(dungeon.normalRooms[0]);
            }
        }

        foreach (GameObject prefab in shuffled)
        {
            // prevent same room twice in a row
            if (prefab == lastNormalRoomPrefab)
                continue;

            DungeonRoom room = Instantiate(
                prefab,
                Vector3.zero,
                Quaternion.identity,
                holder
            ).GetComponent<DungeonRoom>();

            Vector3 offset = exitAnchor.position - room.bottomAnchor.position;
            room.transform.position += offset;

            if (RoomHasTileOverlap(room, occupied))
            {
                DestroyImmediate(room.gameObject);
                continue;
            }

            CommitTiles(room, occupied);

            placed = room;
            lastNormalRoomPrefab = prefab; // remember successful placement
            return true;
        }

        return false;
    }

    DungeonRoom PlaceRoomImmediate(
        GameObject prefab,
        Vector3 position,
        Transform holder,
        HashSet<Vector3Int> occupied
    )
    {
        DungeonRoom room = Instantiate(prefab, position, Quaternion.identity, holder)
            .GetComponent<DungeonRoom>();

        CommitTiles(room, occupied);
        return room;
    }

    DungeonRoom PlaceDeadEnd(
        GameObject prefab,
        Transform exitAnchor,
        Transform holder,
        HashSet<Vector3Int> occupied
    )
    {
        DungeonRoom room = Instantiate(prefab, Vector3.zero, Quaternion.identity, holder)
            .GetComponent<DungeonRoom>();

        Vector3 offset = exitAnchor.position - room.bottomAnchor.position;
        room.transform.position += offset;

        if (RoomHasTileOverlap(room, occupied))
        {
            room.transform.position -= new Vector3(0, 1, 0);
        }

        CommitTiles(room, occupied);
        return room;
    }

    void RegisterChest(DungeonRoom room)
    {
        DungeonChest newChest = room.GetComponentInChildren<DungeonChest>();
        if (newChest)
        {
            newChest.chestId = chestCounter;
            chestCounter++;
            dungeonChests.Add(newChest);
        }
    }

    // -----------------------------------------------------
    // TILE LOGIC
    // -----------------------------------------------------

    bool RoomHasTileOverlap(DungeonRoom room, HashSet<Vector3Int> occupied)
    {

        BoundsInt bounds = room.groundTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!room.groundTilemap.HasTile(cell))
                continue;

            Vector3 world = room.groundTilemap.CellToWorld(cell);
            Vector3Int worldCell = WorldToGlobalCell(world);

            if (occupied.Contains(worldCell))
                return true;
        }

        return false;
    }

    void CommitTiles(DungeonRoom room, HashSet<Vector3Int> occupied)
    {

        BoundsInt bounds = room.groundTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!room.groundTilemap.HasTile(cell))
                continue;

            Vector3 world = room.groundTilemap.CellToWorld(cell);
            Vector3Int worldCell = WorldToGlobalCell(world);

            occupied.Add(worldCell);
        }

    }

    Vector3Int WorldToGlobalCell(Vector3 world)
    {
        return new Vector3Int(
            Mathf.RoundToInt(world.x),
            Mathf.RoundToInt(world.y),
            0
        );
    }

    // -----------------------------------------------------

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = rng.Next(i, list.Count); // System.Random
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetInCombat(bool inCombat, int dungeon, int combatRoomId)
    {
        DungeonEnemySpawn e = combatRooms.Find(c => c.combatRoomId == combatRoomId);
        e.active = inCombat;

        if (!inCombat)
        {
            e.completed = true;
            e.UnlockDoors();
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RegisterEnemy(NetworkObject enemyBehavior, int dungeonIndex)
    {
        Enemy enemy = enemyBehavior.GetComponent<Enemy>();
        enemy.health.dontDieAtDay = true;
        enemy.SetDungeon(dungeonIndex);

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_LightTorch(int torchIndex, bool lit)
    {
        torches[torchIndex].SetLit(lit);
    }

    public void LoadData(GameData data)
    {
        savedChestData = data.dungeonChestData;
        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            completedCombatRooms = data.combatRoomsCompleted;
            collectedItems = data.dungeonCollectedItems;
        }
    }
    public void SaveData(GameData data)
    {

        SerializableDictionary<int, string> chestData = new SerializableDictionary<int, string>();
        dungeonChests.RemoveAll(c => c == null);
        for (int i = 0; i < dungeonChests.Count; i++)
        {
            if (!dungeonChests[i].alreadyOpened) continue;

            string stringData = "";

            for (int j = 0; j < 27; j++)
            {
                stringData += dungeonChests[i].chestItems[j] + "-";
                stringData += dungeonChests[i].chestCounts[j] + "-";
                stringData += dungeonChests[i].chestDurabilities[j] + ",";
            }

            chestData.Add(i, stringData);
        }
        data.dungeonChestData = chestData;

        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            data.combatRoomsCompleted = completedCombatRooms;
            data.dungeonCollectedItems = collectedItems;
        }

    }

    public void LoadSavedChestData()
    {
        dungeonChests.RemoveAll(c => c == null);

        if (savedChestData == null) return;

        for (int i = 0; i < dungeonChests.Count; i++)
        {
            if (!savedChestData.ContainsKey(i)) continue;

            dungeonChests[i].alreadyOpened = true;

            string[] data = savedChestData[i]
    .Split(new[] { "," }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < 27; j++)
            {
                dungeonChests[i].chestItems[j] = data[j].Split("-")[0];
                dungeonChests[i].chestCounts[j] = short.Parse(data[j].Split("-")[1]);
                dungeonChests[i].chestDurabilities[j] = short.Parse(data[j].Split("-")[2]);
            }
        }
    }

    public void SetModifierText(int dungeonIndex)
    {
        menu.instance.dungeonModifierText.color = new Color(1, 1, 1, 0);
        menu.instance.dungeonModifierText.gameObject.SetActive(true);
        menu.instance.dungeonModifierText.DOFade(1, 3);

        menu.instance.dungeonModifierText.text = "Modifiers\n";
        foreach (DungeonModifier m in dungeons[dungeonIndex].currentModifiers)
        {
            string mod = "";

            if (m.percentMod != 0)
            {
                if (m.percentMod > 0)
                {
                    mod += "+";
                }
                mod += (m.percentMod * 100) + "% ";
            }

            mod += m.displayName + "\n";
            string colorTag = positiveModifiers.Contains(m)
                                ? $"<color=#{ColorUtility.ToHtmlStringRGB(Color.green)}>"
                                : $"<color=#{ColorUtility.ToHtmlStringRGB(Color.red)}>";

            menu.instance.dungeonModifierText.text += $"{colorTag}{mod}</color>";

        }

    }

    public float GetModifier(string id, int overrideDungeon = -1)
    {
        if (!player.instance || player.instance.inDungeon == -1) return 1;
        int currentDungeon = player.instance.inDungeon;
        if (overrideDungeon > -1) currentDungeon = overrideDungeon;

        DungeonModifier m = dungeons[currentDungeon].currentModifiers.Find(d => d.id == id);
        if (m == null)
        {
            return 1;
        }

        if (m.percentMod == 0) return 2;

        return 1 + m.percentMod;
    }

}

[System.Serializable]
public class DungeonRooms
{
    public string name;
    public bool generates;
    public Color bgColor;

    [Header("Rooms")]
    public GameObject startRoom;
    public GameObject[] normalRooms;
    public GameObject deadEndRoom;
    public GameObject endRoom;
    public GameObject midpointRoom;

    public List<Tilemap> waterTilemaps = new List<Tilemap>();
    public List<Tilemap> groundTilemaps = new List<Tilemap>();
    public List<DungeonModifier> currentModifiers = new List<DungeonModifier>();

    [HideInInspector]
    public Vector2 position;
    [HideInInspector]
    public Vector2 size;
    [HideInInspector] public Vector2 boundsMin, boundsMax;
}

[System.Serializable]
public class DungeonModifier
{
    public string displayName;
    public string id;
    public float percentMod;
}

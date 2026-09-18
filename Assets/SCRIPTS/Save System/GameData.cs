using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public string ownerUserID;
    public string fileName;

    public int seed;
    public string seedInput;
    //x,y,hp,stump hp
    public string lastPlayed;
    //used to make sure save files from master client and other clients match
    public string saveFileID;
    public int enableCommands, difficulty;
    public Vector2 playerPosition;
    public string currentBoat;
    public bool piloting;
    public int boatFloor;

    public float time;
    public int dayNum;
    public float timeUntilRainStart;
    public float timeUntilRainEnd;
    public int playerHealth;
    public bool[] visitedBiomes;
    public int currentIsland;
    public Vector2 initialRespawnPos;
    public int[] statAmounts;

    public SerializableDictionary<Vector2Int, Vector2Int> treeHealths;
    public SerializableDictionary<Vector2Int, int> oreHealths;
    public SerializableDictionary<Vector2Int, int> otherHealths;
    public SerializableDictionary<int, bool> naturalItemsCollected;
    public SerializableDictionary<int, bool> uniqueItemsCollected;
    public SerializableDictionary<Vector2, bool> destroyedRocks;

    public string[] inventoryItemIDs;
    public int[] inventoryItemCounts;
    public int[] inventoryItemDurability;
    public string[] inventoryItemData;

    public string[] extraInventoryItemIDs;
    public int[] extraInventoryItemCounts;
    public int[] extraInventoryItemDurability;
    public string[] extraInventoryItemData;


    public string mouseItemId;
    public int mouseItemCount, mouseItemDurability;
    public ItemData mouseItemData;

    public int selectedHotbarSlot;
    public PlacedMarker[] placedMapMarkers;

    public SavedTileData[] placedTiles;

    public SavedItemData[] droppedItems;

    public SavedBoatData[] boatData;

    public bool dead;
    public bool duelArena;
    public bool completed;
    public bool mapMarkerDisabled;

    public bool lostSoul;
    public Vector2Int soulLocation;
    public string[] soulItemIds;
    public int[] soulItemCounts;
    public int[] soulItemDurabilities;
    public string[] soulItemDatas;
    public SerializableDictionary<string, int> valeInventory;
    public Vector2 valeTeleportInitialPos;
    public bool pyromancerDisabled;
    public Vector2Int lastGroundPos;
    public int currentDungeon;

    public SerializableDictionary<int, bool> combatRoomsCompleted;
    public SerializableDictionary<int, string> dungeonChestData;
    public List<int> dungeonCollectedItems;
    public int envyKillRequirementAdd = 0;
    public SerializableDictionary<string, int> endScreenStats;

    public GameData(int fileIndex, string fileName = "")
    {
        if(fileName == "")
            this.fileName = "File " + fileIndex;
        else
            this.fileName = fileName;  

        seed = 0;
        seedInput = "";
        
        lastPlayed = "Empty";
        saveFileID = "";
        enableCommands = 0;
        difficulty = 0;

        playerPosition = Vector2.zero;
        currentBoat = "";
        piloting = false;
        boatFloor = 0;

        time = 0;
        dayNum = 0;
        playerHealth = -1;
        visitedBiomes = new bool[4];
        currentIsland = 0;
        statAmounts = new int[5];

        treeHealths = new SerializableDictionary<Vector2Int, Vector2Int>();
        oreHealths = new SerializableDictionary<Vector2Int, int>();
        otherHealths = new SerializableDictionary<Vector2Int, int>();
        naturalItemsCollected = new SerializableDictionary<int, bool>();
        uniqueItemsCollected = new SerializableDictionary<int, bool>();
        destroyedRocks = new SerializableDictionary<Vector2, bool>();

        inventoryItemIDs = new string[inventory.totalSlotCount + 4];
        inventoryItemCounts = new int[inventory.totalSlotCount + 4];
        inventoryItemDurability = new int[inventory.totalSlotCount + 4];
        inventoryItemData = new string[inventory.totalSlotCount + 4];

        extraInventoryItemIDs = new string[inventory.totalSlotCount + 4];
        extraInventoryItemCounts = new int[inventory.totalSlotCount + 4];
        extraInventoryItemDurability = new int[inventory.totalSlotCount + 4];
        extraInventoryItemData = new string[inventory.totalSlotCount + 4];


        mouseItemId = "null";

        selectedHotbarSlot = 0;
        placedMapMarkers = new PlacedMarker[0];

        placedTiles = new SavedTileData[0];
        droppedItems = new SavedItemData[0];
        boatData = new SavedBoatData[0];

        dead = false;
        duelArena = false;
        completed = false;
        mapMarkerDisabled = false;

        lostSoul = false;
        soulLocation = new Vector2Int(0, -7000);
        soulItemIds = new string[inventory.totalSlotCount + 4];
        soulItemCounts = new int[inventory.totalSlotCount + 4];
        soulItemDurabilities = new int[inventory.totalSlotCount + 4];
        soulItemDatas = new string[inventory.totalSlotCount + 4];

        valeInventory = new SerializableDictionary<string, int>();
        valeTeleportInitialPos = Vector2.zero;
        pyromancerDisabled = false;
        lastGroundPos = Vector2Int.zero;
        currentDungeon = 0;

        combatRoomsCompleted = new SerializableDictionary<int, bool>();
        dungeonCollectedItems = new List<int>();
        envyKillRequirementAdd = 0;
    }

}

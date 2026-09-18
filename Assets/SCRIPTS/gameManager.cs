using DG.Tweening;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class gameManager : NetworkBehaviour, IDataPersistance
{
    public static gameManager instance;

    //[Header("Debug Settings")]
    public bool disableIslands;
    public bool disableValeGeneration;
    //public bool showLoadTimer;
    public bool loadingScreenInfo;
    public bool disableTiles;
    public bool disableFeatures;
    public bool enableCommands;
    public bool disableEnemies;
    public bool disableVale;
    public bool disableAutosave;
    public int difficulty = 0;
    public MapGenerator.biome startBiome = MapGenerator.biome.grassy;

    [Header("Game Manager")]
    public GameObject networkRunnerPref;
    public SerializableDictionary<string, item> itemDictionary;
    public recipe[] recipeDictionary;
    public List<TileData> placedTiles = new List<TileData>();
    public NetworkObject view;
    public CanvasGroup connectingScreen;
    [SerializeField] bool connected;
    public Transform tileHolder;
    public Transform waterScrolling;
    public Animator waterAnim;
    public SpriteRenderer waterSprite;
    public Transform camTransform;
    public Transform itemsHolder, boatsHolder;

    public int playersInOverworld;

    public bool allPlayersDone;
    public int playersDone;

    public bool allPlayersDoneWithTiles;
    public int playersDoneWithTiles;

    public List<MusicTrack> musicTracks = new List<MusicTrack>();
    public AudioClip valeMusic, valeMusic2;
    public AudioClip dementiaNightMusic, arenaMusic;
    public AmbienceTrack[] biomeAmbiences;
    public AmbienceTrack oceanAmbience;
    public AudioSource musicSource, ambienceSource;
    public string currentTrack;

    public bool onIsland = true;
    public TextMeshProUGUI connectingText, connectingPercentText;
    public GameObject loadingSpinner;
    //needed because item pickup check is done in ontriggerstay (skull emoji)
    public float pickupPressTimer;
    const float pickupPressBuffer = 0.1f;

    string savedSeed;
    [HideInInspector]
    public string saveFileID;
    public SavedTileData[] savedTileData;
    public SavedItemData[] savedItemData;
    public SavedBoatData[] savedBoatData;
    string savedPlayerBoat;
    bool savedPiloting;
    int savedBoatFloor;
    public bool writingSign;
    public Vector2 boatPreviewPositions = new Vector2(6f, 3f);
    public GameObject tilePrefab;
    public int savedCurrentIsland;
    public bool spawningSavedBoats;

    public TileEntity savedRespawnTile;
    public bool hasRespawnPoint;
    public Vector2 initialSpawnPos;

    const float autosaveInterval = 15;
    float autosaveTimer;
    int savedLegacySeed;

    public static bool doneReceivingIslands;
    public event EventHandler OnRandomTick;
    public GameObject cropItem;

    public int savedPlayerHealth;
    public static bool inBoss;
    public static bool inSecondPhase;

    int commandsSetting = 0;
    public const int maxGroundItems = 1024;

    [TextArea(3, 10)]
    [SerializeField] string[] loadingTips;
    [SerializeField] TextMeshProUGUI loadingTipText;
    List<int> usedLoadingTips = new List<int>();
    bool saidDementiaLLoadingTip;

    System.Random loadingTipRandom;

    public Transform worldBorder;
    [SerializeField] GameObject duelArena, planeCrash;
    [SerializeField] Color duelCamBgColor;
    public bool inArena;
    bool savedInArena;

    public TileBase lavaTile, lavaDummyTile;

    public static SerializableDictionary<string, int> endScreenStats;
    private void Awake()
    {
        instance = this;

        connectingPercentText.enabled = false;

        doneReceivingIslands = false;

        connectingScreen.alpha = 1;

        //for editor
        if (ConnectToServer.playerID == null) ConnectToServer.playerID = PlayerPrefs.GetString("userID");

    }


    private void Start()
    {

        duelArena.SetActive(false);

        waterScrolling.gameObject.SetActive(false);

        musicSource.bypassReverbZones = true;

        TileEntity.playersOnPuddle = 0;
        loadingTipRandom = new System.Random();

        MapDisplay.finishedLoading = false;
        NewLoadingTip();

        AudioListener.volume = 1;

        //PhotonNetwork.SerializationRate = 10;
        //PhotonNetwork.UseRpcMonoBehaviourCache = true;

        inBoss = false;
        autosaveTimer = 0;

        loadingSpinner.SetActive(true);

        MapDisplay.Instance.StartTimer();

        currentTrack = "day";

        GenerateItemIds();
        LoadRecipes();

        if (DataPersistanceManager.notInEditor || !Application.isEditor)
        {
            //prepare game is called in this coroutine
            WaitUntilBehaviorInit();

            connected = true;
            allPlayersDone = false;

        }
        //in editor
        else
        {
            Instantiate(networkRunnerPref);

            saidDementiaLLoadingTip = true;
            //allPlayersInOverworld = true;

            connectingScreen.alpha = 1;
            connected = false;
            allPlayersDone = true;

            StartGame();
        }

        MapDisplay.finishedLoading = false;

        InitializeEndStatsDict();
    }

    void InitializeEndStatsDict()
    {
        endScreenStats = new SerializableDictionary<string, int>();
        endScreenStats.Add("enemies", 0);
        endScreenStats.Add("ores", 0);
        endScreenStats.Add("trees", 0);
        endScreenStats.Add("items", 0);
        endScreenStats.Add("deaths", 0);
        endScreenStats.Add("deaths soul", 0);
        endScreenStats.Add("damage", 0);
        endScreenStats.Add("uniques", 0);
        endScreenStats.Add("dungeons", 0);
        endScreenStats.Add("kills", 0);
        endScreenStats.Add("crafted", 0);
        endScreenStats.Add("abilities", 0);
        endScreenStats.Add("asphodel", 0);
        endScreenStats.Add("boats", 0);
        endScreenStats.Add("crashed", 0);
        endScreenStats.Add("food", 0);
        endScreenStats.Add("upgrades", 0);
        endScreenStats.Add("planted", 0);
        endScreenStats.Add("steps", 0);
    }

    async void StartGame()
    {

        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,             // Important: Shared Mode
            SessionName = "testy",              // Name of the room
            PlayerCount = 5,                        // Optional: for matchmaking metadata
            SessionProperties = new Dictionary<string, SessionProperty>()
                {
                    { "isJoinable", (SessionProperty) false }
                },
            Scene = SceneRef.FromIndex(2),
            SceneManager = SingletonRunner.runner.GetComponent<NetworkSceneManagerDefault>()
        };

        StartGameResult result = await SingletonRunner.runner.StartGame(args);
        if (result.Ok)
        {

            Invoke("EditorUpdateActorIndices", 0.5f);

            Invoke("PrepareGame", 1f);
        }
        else
        {
            Debug.LogError($"StartGame failed: {result.ShutdownReason}");
        }
    }

    //might only be used for editor idk
    public void EditorUpdateActorIndices()
    {
        if (!SingletonRunner.runner || !SingletonRunner.runner.IsSharedModeMasterClient) return;

        DataPersistanceManager.instance.usernameMap = new SerializableDictionary<PlayerRef, string>() { { SingletonRunner.runner.LocalPlayer, "Player" } };

        var players = SingletonRunner.runner.ActivePlayers.ToList();
        players.Sort((p1, p2) => p1.PlayerId);

        DataPersistanceManager.instance.actorIndexMap.Clear();

        for (int i = 0; i < players.Count; i++)
        {
            PlayerRef player = players[i];
            DataPersistanceManager.instance.actorIndexMap[player] = i;

            // Send actor index to each player so they know their own
            RPC_SetActorIndex(player, i);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetActorIndex(PlayerRef targetPlayer, int index)
    {
        DataPersistanceManager.instance.actorIndexMap[targetPlayer] = index;

        if (targetPlayer == Runner.LocalPlayer)
        {
            DataPersistanceManager.LocalActorIndex = index;
        }
    }

    void NewLoadingTip()
    {
        if (MapDisplay.finishedLoading) return;

        if (usedLoadingTips.Count >= loadingTips.Length) usedLoadingTips.Clear();

        int i = loadingTipRandom.Next(0, loadingTips.Length);

        while (usedLoadingTips.Contains(i))
            i = loadingTipRandom.Next(0, loadingTips.Length);

        loadingTipText.text = loadingTips[i];


        if (!saidDementiaLLoadingTip && difficulty == 2 && !DataPersistanceManager.instance.loadingSave)
        {
            loadingTipText.text = loadingTips[14];
            saidDementiaLLoadingTip = true;
            Invoke("NewLoadingTip", 11);
            return;
        }

        Invoke("NewLoadingTip", 8);
    }


    void RandomTick()
    {
        OnRandomTick?.Invoke(this, EventArgs.Empty);
    }


    public void GenerationComplete()
    {
        RPC_GenerationComplete();
    }


    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_GenerationComplete()
    {
        playersDone++;
        if (playersDone >= Runner.SessionInfo.PlayerCount)
        {
            allPlayersDone = true;

            if (Runner.IsSharedModeMasterClient)
            {
                InvokeRepeating("RandomTick", 10, 2);
            }
        }
    }

    public void TilesComplete()
    {
        RPC_TilesComplete();
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_TilesComplete()
    {
        playersDoneWithTiles++;
        if (playersDoneWithTiles >= Runner.SessionInfo.PlayerCount)
        {
            allPlayersDoneWithTiles = true;
        }
    }

    public void ReceiveIslands(Island[] islands)
    {
        RPC_ReceiveIslandData(ArrayToString(islands));
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_ReceiveIslandData(string data)
    {
        MapGenerator.instance.islands = StringToArray<Island>(data).ToList();

        doneReceivingIslands = true;
    }

    public string ArrayToString<T>(T[] array)
    {
        if (array == null)
        {
            //print("null array");
            return "";
        }
        List<string> elementStrings = new List<string>();

        foreach (var element in array)
        {
            string elementString = JsonUtility.ToJson(element);
            elementStrings.Add(elementString);
        }

        string combinedString = string.Join(";", elementStrings.ToArray());

        byte[] compressedBytes;
        using (MemoryStream compressedStream = new MemoryStream())
        {
            using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
            {
                using (StreamWriter writer = new StreamWriter(deflateStream))
                {
                    writer.Write(combinedString);
                }
            }
            compressedBytes = compressedStream.ToArray();
        }

        string compressedString = System.Convert.ToBase64String(compressedBytes);

        return compressedString;
    }

    public T[] StringToArray<T>(string compressedArrayString)
    {
        if (string.IsNullOrEmpty(compressedArrayString))
        {
            return new T[0];
        }

        byte[] compressedBytes = System.Convert.FromBase64String(compressedArrayString);

        string decompressedString;
        using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
        {
            using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                using (StreamReader reader = new StreamReader(deflateStream))
                {
                    decompressedString = reader.ReadToEnd();
                }
            }
        }

        string[] elementStrings = decompressedString.Split(';');
        T[] array = new T[elementStrings.Length];

        for (int i = 0; i < elementStrings.Length; i++)
        {
            array[i] = JsonUtility.FromJson<T>(elementStrings[i]);
        }

        return array;
    }

    public void ToggleDefaultLayerCulling(bool isActive)
    {

        // Get the current culling mask.
        int currentCullingMask = Camera.main.cullingMask;

        // Check if the default layer is currently in the culling mask.
        bool defaultLayerActive = (currentCullingMask & (1 << LayerMask.NameToLayer("Default"))) != 0;

        // Update the culling mask based on the provided boolean input.
        if (isActive)
        {
            // Add the default layer to the culling mask if it's not already present.
            if (!defaultLayerActive)
            {
                currentCullingMask |= (1 << LayerMask.NameToLayer("Default"));
            }
        }
        else
        {
            // Remove the default layer from the culling mask if it's present.
            if (defaultLayerActive)
            {
                currentCullingMask &= ~(1 << LayerMask.NameToLayer("Default"));
            }
        }

        // Apply the updated culling mask to the camera.
        Camera.main.cullingMask = currentCullingMask;

    }

    private void Update()
    {
        if (!allPlayersDone) return;

        /*
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            DataPersistanceManager.instance.endScreenStats = gameManager.endScreenStats;
            EnemySpawner.instance.disableDequeue = true;

            Debug.Log("before");

            SingletonRunner.runner.LoadScene(
                SceneRef.FromIndex(3),
                new LoadSceneParameters(),
                true);

            Debug.Log("after");
        }
        */

        waterScrolling.position = new Vector3(Mathf.Round(camTransform.position.x), Mathf.Round(camTransform.position.y));
        if (player.instance)
        {
            waterAnim.SetBool("Blank", player.instance.inDungeon > -1);
            if (player.instance.inDungeon > -1)
            {
                waterSprite.color = DungeonGenerator.instance.dungeons[player.instance.inDungeon].bgColor;
            }
            else
            {
                waterSprite.color = Color.white;
            }
        }

        if (InputManager.actions["Interact"].started)
        {
            pickupPressTimer = pickupPressBuffer;
        }
        if (pickupPressTimer > 0)
            pickupPressTimer -= Time.deltaTime;


        if (Input.GetKeyDown(KeyCode.F11))
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed) Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            else
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(ConnectToServer.windowedX, ConnectToServer.windowedY, FullScreenMode.Windowed);
            }
        }

        if (SingletonRunner.runner.IsSharedModeMasterClient && !inBoss)
        {
            autosaveTimer += Time.deltaTime;

            if (autosaveTimer > autosaveInterval && player.instance && !disableAutosave)
            {
                autosaveTimer = 0;
                RPC_AutoSave();
            }
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_AutoSave()
    {
        DataPersistanceManager.instance.SaveGame(true);
    }

    /*
    public override void OnConnectedToMaster()
    {
        if (!connected)
        {
            PhotonNetwork.NickName = "Player";
            PhotonNetwork.CreateRoom("Test Room", new RoomOptions() { MaxPlayers = 4, BroadcastPropsChangeToAll = true });

        }
    }

    public override void OnJoinedRoom()
    {
        if (!connected)
        {
            PrepareGame();
            connected = true;
        }
    }
    */

    public void ChangeBossReady(bool ready)
    {
        RPC_ChangeBossReady(ready);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_ChangeBossReady(bool ready)
    {
        if (ready) menu.instance.playersReady++;
        else menu.instance.playersReady--;
    }
    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_ResetBossReady()
    {
        menu.instance.playersReady = 0;
    }

    async void WaitUntilBehaviorInit()
    {
        while (!view.IsValid)
        {
            await Task.Yield();
        }
        PrepareGame();
    }

    void PrepareGame()
    {
        //duel arena mode
        if ((DataPersistanceManager.roomProperties.ContainsKey("duelArena")
            && DataPersistanceManager.roomProperties["duelArena"] == "true") || savedInArena)
        {
            disableEnemies = true;
            disableTiles = true;
            TimeManager.instance.pauseTime = true;
            disableFeatures = true;
            disableIslands = true;
            disableVale = true;
            disableValeGeneration = true;
            enableCommands = true;

            duelArena.SetActive(true);
            duelArena.transform.GetChild(0).gameObject.SetActive(true);
            planeCrash.SetActive(false);
            Camera.main.backgroundColor = duelCamBgColor;
            inArena = true;
            waterScrolling.gameObject.SetActive(false);
        }


        if (DataPersistanceManager.roomProperties.ContainsKey("fileName"))
            DataPersistanceManager.instance.LoadGame(DataPersistanceManager.roomProperties["fileName"]);
        else
            DataPersistanceManager.instance.LoadGame();

        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            string newSeed;

            if (DataPersistanceManager.roomProperties.ContainsKey("seed") && DataPersistanceManager.roomProperties["seed"] != "")
            {
                newSeed = DataPersistanceManager.roomProperties["seed"];
            }
            else
            {
                newSeed = ((int)DateTime.Now.Ticks).ToString();
            }

            if (DataPersistanceManager.instance.loadingSave)
            {
                if (savedSeed != null)
                {
                    newSeed = savedSeed;
                }

            }

            RPC_ReceiveSeed(newSeed);

        }

        if (DataPersistanceManager.roomProperties.ContainsKey("commands"))
        {
            switch (int.Parse(DataPersistanceManager.roomProperties["commands"]))
            {
                case 0: enableCommands = false; break;
                case 1: enableCommands = Runner.IsSharedModeMasterClient; break;
                case 2: enableCommands = true; break;

            }

            commandsSetting = int.Parse(DataPersistanceManager.roomProperties["commands"]);
        }
        else
        {
            enableCommands = Runner.IsSharedModeMasterClient;
        }


        if (DataPersistanceManager.roomProperties.ContainsKey("difficulty"))
        {
            difficulty = int.Parse(DataPersistanceManager.roomProperties["difficulty"]);
        }


        if (DataPersistanceManager.roomProperties.ContainsKey("saveFileId"))
        {
            saveFileID = DataPersistanceManager.roomProperties["saveFileId"];
        }
        else
        {
            saveFileID = Guid.NewGuid().ToString();
        }



    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_ReceiveSeed(string Seed)
    {
        seed.instance.SetSeed(Seed);
    }

    public void GenerateItemIds()
    {
        item[] ingredients = Resources.LoadAll<item>("Items/_Ingredient Items");
        item[] tiles = Resources.LoadAll<item>("Items/_Tile Items");
        item[] tools = Resources.LoadAll<item>("Items/_Tool Items");
        item[] foods = Resources.LoadAll<item>("Items/_Food Items");
        item[] armors = Resources.LoadAll<item>("Items/_Armor Items");
        item[] charms = Resources.LoadAll<item>("Items/_Charm Items");

        List<item> newItems = new List<item>();

        //all loaded items added to list
        foreach (item it in ingredients)
        {
            newItems.Add(it);
        }
        foreach (item it in tiles)
        {
            newItems.Add(it);
        }
        foreach (item it in tools)
        {
            newItems.Add(it);
        }
        foreach (item it in foods)
        {
            newItems.Add(it);
        }
        foreach (item it in armors)
        {
            newItems.Add(it);
        }
        foreach (item it in charms)
        {
            newItems.Add(it);
        }

        itemDictionary = new SerializableDictionary<string, item>();

        itemDictionary.TryAdd("null", null);

        foreach (item it in newItems)
        {
            itemDictionary.TryAdd(it.itemId, it);
        }

    }

    public void SetConnectingText(string state)
    {
        connectingText.text = "Creating " + state + "...";
    }

    public void ReloadSave()
    {
        RPC_ReloadSave();
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_ReloadSave()
    {
        if (!SingletonRunner.runner || !SingletonRunner.runner.IsSharedModeMasterClient) return;

        SingletonRunner.runner.LoadScene(SceneRef.FromIndex(2), new LoadSceneParameters(), true);
    }

    public void LoadRecipes()
    {

        List<recipe> allRecipes = Resources.LoadAll<recipe>("Items/Recipes").ToList();
        List<recipe> newRecipes = new List<recipe>();

        int highestPriority = 0;
        foreach (recipe r in allRecipes)
        {
            if (r.priority > highestPriority) highestPriority = r.priority;
        }
        int lowestPriority = 0;
        foreach (recipe r in allRecipes)
        {
            if (r.priority < lowestPriority) lowestPriority = r.priority;
        }


        for (int i = lowestPriority; i < highestPriority + 1; i++)
        {
            foreach (recipe r in allRecipes)
            {
                if (r.priority == i && !r.disableRecipe)
                    newRecipes.Add(r);
            }
        }


        recipeDictionary = newRecipes.ToArray();
        recipeDictionary = recipeDictionary.Where(r => r != null).ToArray();

        inventory.instance.CreateRecipeButtons();
    }

    public IEnumerator _SwitchMusic(string newSong, float fadeTime = 2)
    {
        if (playerHealth.instance.dead) yield break;

        player mPlayer = player.instance;
        if (menu.instance.spectatingPlayer) mPlayer = menu.instance.spectatingPlayer;

        StartCoroutine(_SwitchAmbience());

        var fade = musicSource.DOFade(0, fadeTime);
        currentTrack = newSong;

        for (float t = fadeTime; t > 0; t -= Time.deltaTime)
        {
            if (!(mPlayer.onIsland && !HandMove.instance.trackerActivated && !(mPlayer.currentBoat && mPlayer.currentBoat.velocity.magnitude > 0) && !mPlayer.health.dead))
            {
                fade.Kill();
                t = 0;
            }
            yield return null;
        }

        musicSource.Stop();

        if (DungeonEnemySpawn.currentlyInRoom && DungeonEnemySpawn.currentlyInRoom.active) yield break;

        switch (mPlayer.currentIsland.biome)
        {
            case 0:
                musicSource.clip = musicTracks.Find(m => m.name == newSong).clip;
                break;
            case 1:
                musicSource.clip = musicTracks.Find(m => m.name == newSong).swampClip;
                break;
            case 2:
                musicSource.clip = musicTracks.Find(m => m.name == newSong).desertClip;
                break;
            case 3:
                musicSource.clip = musicTracks.Find(m => m.name == newSong).snowClip;
                break;
            case 4:
                musicSource.clip = musicTracks.Find(m => m.name == newSong).volcanicClip;
                break;
            case 5:
                musicSource.clip = valeMusic;
                if (!ValeManager.instance.lostSoul)
                    musicSource.clip = valeMusic2;
                break;
        }

        if (newSong == "night" && difficulty == 2 && mPlayer.currentIsland.biome != 5) musicSource.clip = dementiaNightMusic;

        if (inArena) musicSource.clip = arenaMusic;

        musicSource.volume = 0;
        yield return null;
        musicSource.Play();

        yield return new WaitForSeconds(0.15f);

        if ((mPlayer.onIsland || player.instance.inDungeon > -1) && !HandMove.instance.trackerActivated && !(mPlayer.currentBoat && mPlayer.currentBoat.playingMusic) && !mPlayer.health.dead)
        {
            musicSource.DOFade(1, 6);
        }

        yield return new WaitForSeconds(6f);
    }

    public void DisableMusic()
    {
        StopAllCoroutines();
        musicSource.DOFade(0, 1);
        musicSource.enabled = false;
    }

    public IEnumerator _SwitchAmbience()
    {
        player mPlayer = player.instance;
        if (menu.instance.spectatingPlayer) mPlayer = menu.instance.spectatingPlayer;

        ambienceSource.DOFade(0, 2);
        yield return new WaitForSeconds(2);

        if (inArena || player.instance.inDungeon > -1)
            yield break;

        if (!TimeManager.instance.isNight)
            ambienceSource.clip = biomeAmbiences[mPlayer.currentIsland.biome].dayClip;
        else
            ambienceSource.clip = biomeAmbiences[mPlayer.currentIsland.biome].nightClip;

        if (!mPlayer.onIsland) ambienceSource.clip = oceanAmbience.dayClip;

        if (!mPlayer.currentBoat && (!inSecondPhase || Boss.instance.heartDead))
        {
            ambienceSource.Play();
            ambienceSource.DOFade(biomeAmbiences[mPlayer.currentIsland.biome].volume, 2);
        }

    }

    public IEnumerator _WaitForBoatsLoad()
    {
        while (DataPersistanceManager.instance.loadingSave && boatsHolder.childCount < savedBoatData.Length)
        {
            yield return null;
        }

        RPC_FinishSpawningSavedBoats();
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_FinishSpawningSavedBoats()
    {
        spawningSavedBoats = false;
    }


    public async void LoadSavedTiles()
    {
        int count = 0;

        foreach (SavedTileData data in savedTileData)
        {
            PlaceBlock(data.Item, data.position, data.rotation, data.state, true, data.tileEntityData);
            count++;
            if (count % 5 == 0) await Task.Yield();
        }
    }

    public void LoadSavedItems(SavedItemData[] itemData, NetworkId boatParent)
    {
        if (!Runner.IsSharedModeMasterClient) return;

        int i = 0;
        HashSet<SavedItemData> uniqueItems = new HashSet<SavedItemData>(); // To track duplicate items

        foreach (SavedItemData data in itemData)
        {
            // Skip the item if it already exists in the set
            if (!uniqueItems.Add(data))
            {
                continue;
            }

            NetworkObject newItem = Runner.Spawn(
                inventory.instance.dropItemPrefab,
                data.position,
                Quaternion.identity,
                inputAuthority: Runner.LocalPlayer // or playerRef if needed
            ).GetComponent<NetworkObject>();

            RPC_InitializeItem(newItem.Id, data.itemID, (short)data.count, data.position, (short)data.durability, inventory.ItemDataToString(data.itemData), data.rotation, boatParent, data.boatFloor);

            if (i >= maxGroundItems)
            {
                Chat.Instance.SendInChat(new string[] { "Saved items exceed " + maxGroundItems + ", remaining " + (savedItemData.Length - maxGroundItems) + " will not be loaded" }, new Color[] { new Color(1, 0.15f, 0.15f) });
                break;
            }
            i++;
        }
    }
    public async void PlayerRideSavedBoat()
    {
        if (savedPlayerBoat == "") return;

        Boat b = null;
        float timer = 0;
        while (b == null)
        {
            for (int i = 0; i < boatsHolder.childCount; i++)
            {
                if (boatsHolder.GetChild(i).GetComponent<Boat>().boatID == savedPlayerBoat)
                {
                    b = boatsHolder.GetChild(i).GetComponent<Boat>();
                }
            }
            timer += Time.deltaTime;
            if (timer > 10)
            {
                Debug.LogError("Failed to ride saved boat");
                break;
            }

            await Task.Yield();
        }

        if (b)
        {
            b.PlayerRideBoat(player.instance.view.Id, true, true);
            player.instance.transform.SetParent(b.transform);
            player.instance.transform.localPosition = playerSpawner.instance.savedPlayerPos;

            if (savedBoatFloor != 0)
            {
                b.PlayerGoUp(savedBoatFloor, player.instance.view.Id);
            }

            if (savedPiloting)
            {
                b.EnterPilot();
            }

        }
    }

    public void LoadSavedBoats()
    {
        foreach (SavedBoatData data in savedBoatData)
        {
            StartCoroutine(_InstantiateBoat(data.boatIndex, data.position, data.scale, data.boatID, true, data.entityData, data.savedItems));
        }
    }

    public void InstantiateBoat(int boatIndex, Vector2 spawnPos, float scaleX, string boatID, bool disableAnimation, string entityData, SavedItemData[] savedItemData)
    {
        string savedItems = "";
        if (savedItemData != null)
            treeRPCs.ArrayToString(savedItemData);

        RPC_InstantiateBoat(boatIndex, spawnPos, scaleX, boatID, disableAnimation, entityData, savedItems);
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.ReliableLargeData)]
    void RPC_InstantiateBoat(int boatIndex, Vector2 spawnPos, float scaleX, string boatID, bool disableAnimation, string entityData, string savedItemString)
    {
        if (!Runner.IsSharedModeMasterClient) return;

        SavedItemData[] savedItems = new SavedItemData[0];
        if (savedItemString != "")
        {
            savedItems = treeRPCs.StringToArray<SavedItemData>(savedItemString);
        }

        StartCoroutine(_InstantiateBoat(boatIndex, spawnPos, scaleX, boatID, disableAnimation, entityData, savedItems));
    }

    public IEnumerator _InstantiateBoat(int boatIndex, Vector2 spawnPos, float scaleX, string boatID, bool disableAnimation, string entityData, SavedItemData[] savedItemData)
    {
        yield return null;
        NetworkObject newBoat = Runner.Spawn(inventory.instance.boats[boatIndex].boatObj, spawnPos, Quaternion.identity, SingletonRunner.runner.LocalPlayer);
        while (newBoat == null) yield return null;

        RPC_InitializeBoat(newBoat.gameObject.GetComponent<NetworkObject>().Id, boatIndex, scaleX, boatID, disableAnimation, entityData);

        LoadSavedItems(savedItemData, newBoat.Id);
    }


    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.ReliableLargeData)]
    async void RPC_InitializeBoat(NetworkId netId, int boatIndex, float scaleX, string boatID, bool disableAnimation, string entityData)
    {
        NetworkObject newBoat = Runner.FindObject(netId);

        while (newBoat == null)
        {
            newBoat = Runner.FindObject(netId);
            await Task.Yield();
        }

        newBoat.transform.localScale = new Vector3(scaleX, 1, 1);

        Boat b = newBoat.GetComponent<Boat>();

        b.boatID = boatID;
        b.Initialize(disableAnimation, boatIndex);

        newBoat.transform.SetParent(boatsHolder, true);

        if (entityData != "")
            b.StringToEntityData(entityData);
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.ReliableLargeData)]
    void RPC_InitializeItem(NetworkId netId, string itemID, short itemAmount, Vector2 pos, short durability, string itemData, float rotation, NetworkId boatParent, int boatFloor)
    {
        if (itemID == "") return;
        GameObject thrownItem = Runner.FindObject(netId).gameObject;
        thrownItem.transform.SetParent(itemsHolder);
        itemPickup ItemPickup = thrownItem.GetComponent<itemPickup>();
        ItemPickup.Item = itemDictionary[itemID];
        ItemPickup.SetItem(itemID, itemAmount, durability, inventory.StringToItemData(itemData), false, boatParent, boatFloor);
        thrownItem.transform.eulerAngles = new Vector3(0, 0, rotation);
    }

    public void PlaceBlock(string Item, Vector2 pos, float rotation, int state, bool disableSpawnAnim, string tileEntityData, string boatId = "", int boatTileSlot = 0)
    {
        RPC_CreateTile(DataPersistanceManager.userId, Item, pos, rotation, state, disableSpawnAnim, tileEntityData);

        if (inventory.charmEffect == "harvest")
        {
            SetWatered(pos, UnityEngine.Random.Range(itemDictionary[Item].wateredTicks.x, itemDictionary[Item].wateredTicks.y + 1), false);
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.ReliableLargeData)]
    void RPC_CreateTile(string owner, string Item, Vector2 pos, float rotation, int state, bool disableSpawnAnim, string tileEntityData)
    {
        List<TileData> TilesOnPos = new List<TileData>();
        TilesOnPos = placedTiles.FindAll(data => data.position == pos);

        if (!itemDictionary.ContainsKey(Item)) return;

        if (itemDictionary[Item].onlyPlaceOnTiles != null && itemDictionary[Item].onlyPlaceOnTiles.Count() > 0)
        {
            bool placeable = false;
            foreach (TileData data in TilesOnPos)
            {
                if (itemDictionary[Item].onlyPlaceOnTiles.Contains(data.Item))
                {
                    placeable = true;
                    break;
                }
            }
            if (!placeable) return;
        }

        GameObject tile = Instantiate(tilePrefab, (Vector3)pos, Quaternion.identity);
        tile.transform.SetParent(tileHolder);
        placedTiles.Add(new TileData(pos, rotation, itemDictionary[Item], tile, tile.GetComponent<SpriteRenderer>(), tile.GetComponent<placedTile>()));

        if (itemDictionary[Item].canRotate)
            tile.transform.eulerAngles = new Vector3(0, 0, rotation);

        tile.GetComponent<placedTile>().SetProperties(itemDictionary[Item], state, itemDictionary[Item].tileStates[state].sortLayerName, itemDictionary[Item].tileStates[state].sortOrder, (short)itemDictionary[Item].TileType,
            disableSpawnAnim, tileEntityData, owner, null, 0);

        if (itemDictionary[Item].TileType == item.tileType.sign && (owner == DataPersistanceManager.userId) && !disableSpawnAnim)
        {
            StartCoroutine(_HandleSign(tile.GetComponent<placedTile>()));
        }
        else if (itemDictionary[Item].TileType == item.tileType.vehicleStation)
        {
            SetBoatPreviewPos(tile, rotation);
        }

        if (itemDictionary[Item].TileType == item.tileType.floor)
            SetLavaDummyTile(Vector3Int.RoundToInt(pos) - new Vector3Int(1, 1), true);

    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.ReliableLargeData)]
    public void RPC_CreateBoatTile(string owner, string Item, bool disableSpawnAnim, string tileEntityData, NetworkId boatId, int boatTileSlot)
    {
        if (!itemDictionary.ContainsKey(Item)) return;

        Boat b = null;
        b = SingletonRunner.runner.FindObject(boatId).GetComponent<Boat>();

        GameObject tile = Instantiate(tilePrefab, b.tileSlots[boatTileSlot]);
        tile.transform.localPosition = Vector2.zero + itemDictionary[Item].boatTileOffset;
        tile.transform.localScale = new Vector3(b.transform.localScale.x, 1);
        placedTile t = tile.GetComponent<placedTile>();

        b.tileSlotTiles[boatTileSlot].tiles[itemDictionary[Item].floor ? 0 : 1] = t;

        short sortingLayer = (short)b.tileLayer;
        if (itemDictionary[Item].TileType == item.tileType.respawn || itemDictionary[Item].TileType == item.tileType.floor) sortingLayer -= 1;

        t.SetProperties(itemDictionary[Item], 0, "Player", sortingLayer, (short)itemDictionary[Item].TileType,
            disableSpawnAnim, tileEntityData, owner, b, boatTileSlot);

        if (itemDictionary[Item].TileType == item.tileType.sign && (owner == DataPersistanceManager.userId) && !disableSpawnAnim)
        {
            StartCoroutine(_HandleSign(tile.GetComponent<placedTile>()));
        }

    }

    void SetLavaDummyTile(Vector3Int pos, bool isDummy)
    {
        if (MapDisplay.Instance.lavaCollisionTilemap.GetTile(pos))
        {
            MapDisplay.Instance.lavaCollisionTilemap.SetTile(pos, isDummy ? lavaDummyTile : lavaTile);
            MapDisplay.Instance.lavaCollisionTilemap.SetTileAnimationFlags(pos + new Vector3Int(1, 0), TileAnimationFlags.SyncAnimation);
        }

    }

    public void RemoveTile(Vector2 position, item Item, bool effect)
    {
        RPC_RemoveTile(position, Item.itemId, effect);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_RemoveTile(Vector2 position, string Item, bool effect)
    {
        TileData tile = placedTiles.Find(data => data.position == position && data.Item == itemDictionary[Item]);
        if (tile.PlacedTile)
            tile.PlacedTile.OnRemove();

        if (!tile.tileGameObject) return;

        if (effect)
            Instantiate(HandMove.instance.destroyEffect, position, Quaternion.identity);
        if (tile.tileGameObject)
        {
            Destroy(tile.tileGameObject);
        }
        instance.placedTiles.Remove(tile);

        if (itemDictionary[Item].TileType == item.tileType.floor)
            SetLavaDummyTile(Vector3Int.RoundToInt(position) - new Vector3Int(1, 1), false);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RemoveBoatTile(string boatId, int boatTileIndex, bool effect, string Item)
    {
        Boat b = null;
        for (int i = 0; i < boatsHolder.childCount; i++)
        {
            if (boatsHolder.GetChild(i).GetComponent<Boat>().boatID == boatId)
            {
                b = boatsHolder.GetChild(i).GetComponent<Boat>();
            }
        }
        placedTile t = b.tileSlotTiles[boatTileIndex].tiles[itemDictionary[Item].floor ? 0 : 1];
        if (t == null) return;

        if (t)
            t.OnRemove();

        if (effect)
            Instantiate(HandMove.instance.destroyEffect, t.transform.position, Quaternion.identity, b.transform);
        if (t.gameObject)
        {
            Destroy(t.gameObject);
        }
    }


    public void TileTakeDamage(int damage, Vector2Int pos, int playerId)
    {
        RPC_TileTakeDamage(damage, (Vector2)pos, playerId);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_TileTakeDamage(int damage, Vector2 pos, int playerId)
    {
        TileData tile = placedTiles.Find(data => data.position == pos && !data.Item.floor);

        if (tile.PlacedTile)
        {
            tile.PlacedTile.health.TakeDamage(damage, playerId);
        }
    }

    public void OpenDoor(Vector2 pos, bool isOpen)
    {
        RPC_OpenDoor(pos, isOpen);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_OpenDoor(Vector2 pos, bool isOpen)
    {
        TileData tile = placedTiles.Find(data => data.position == pos && data.Item.TileType == item.tileType.door);

        if (tile.PlacedTile)
        {
            tile.PlacedTile.tileEntity.OpenDoor(isOpen);
        }
    }

    public void SetWatered(Vector2 pos, int wateredTime, bool disableAnim)
    {
        if (inventory.charmEffect == "harvest") wateredTime = Mathf.RoundToInt(wateredTime * 1.5f);

        RPC_SetWatered(pos, wateredTime, disableAnim);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetWatered(Vector2 pos, int wateredTime, bool disableAnim)
    {
        TileData tile = placedTiles.Find(data => data.position == pos && data.Item.TileType == item.tileType.crop);

        if (tile.PlacedTile)
        {

            tile.PlacedTile.tileEntity.SetWatered(wateredTime, disableAnim);
        }
    }

    //sent to master client to verify not pressed as same time
    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = false)]
    public void RPC_RequestStartUsing(Vector2 pos, PlayerRef player, short type, string boatId = "", int boatTileIndex = -1)
    {
        if (!Runner.IsSharedModeMasterClient) return;

        RPC_StartUsing(pos, player, type, boatId, boatTileIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = false)]
    public void RPC_StartUsing(Vector2 pos, PlayerRef player, short type, string boatId = "", int boatTileIndex = -1)
    {
        if (boatId != "")
        {
            GetTileOnBoat(boatId, boatTileIndex, false).StartUsing(player, type);

            return;
        }

        TileData tile = placedTiles.Find(data => data.position == pos && (data.Item.TileType == item.tileType.furnace || data.Item.TileType == item.tileType.purifier || data.Item.TileType == item.tileType.chest));

        if (tile.PlacedTile)
        {
            tile.PlacedTile.tileEntity.StartUsing(player, type);
        }
    }
    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = false)]
    public void RPC_SetPlayerRespawnPoint(Vector2 pos, string userId, bool spawnPoint, bool disableEffect, string boatId = "", int boatTileIndex = -1)
    {

        TileData tile = placedTiles.Find(data => data.position == pos && (data.Item.TileType == item.tileType.respawn));
        placedTile p = tile.PlacedTile;

        if (boatId != "")
        {
            p = GetTileOnBoat(boatId, boatTileIndex, false).tile;
        }

        if (!spawnPoint)
        {
            if (p.tileEntity.respawnUsers.Contains(userId))
                p.tileEntity.respawnUsers.Remove(userId);
        }
        else
        {
            p.tileEntity.respawnUsers.Add(userId);
            if (!disableEffect)
            {
                p.tileEntity.RespawnSetEffect();
            }
        }

    }

    public void StopUsingFurnace(Vector2 pos, string fuelItem, short fuelCount, string ingredItem, short ingredCount, string resultItem, short resultCount, float smeltTimeLeft, float fuelTimeLeft, float maxFuelTime, string boatId = "", int boatTileIndex = -1)
    {
        RPC_StopUsingFurnace(pos, fuelItem, fuelCount, ingredItem, ingredCount, resultItem, resultCount, smeltTimeLeft, fuelTimeLeft, maxFuelTime, boatId, boatTileIndex);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_StopUsingFurnace(Vector2 pos, string fuelItem, short fuelCount, string ingredItem, short ingredCount, string resultItem, short resultCount, float smeltTimeLeft, float fuelTimeLeft, float maxFuelTime, string boatId, int boatTileIndex)
    {
        if (boatId != "")
        {
            GetTileOnBoat(boatId, boatTileIndex, false).StopUsingFurnace(fuelItem, fuelCount, ingredItem, ingredCount, resultItem, resultCount, smeltTimeLeft, fuelTimeLeft, maxFuelTime);
            return;
        }

        TileData tile = placedTiles.Find(data => data.position == pos && (data.Item.TileType == item.tileType.furnace || data.Item.TileType == item.tileType.purifier));

        if (tile.PlacedTile)
        {
            tile.PlacedTile.tileEntity.StopUsingFurnace(fuelItem, fuelCount, ingredItem, ingredCount, resultItem, resultCount, smeltTimeLeft, fuelTimeLeft, maxFuelTime);
        }
    }
    public void SetFurnaceCooking(Vector2 pos, bool cooking, string boatId = "", int boatTileIndex = -1)
    {
        RPC_SetFurnaceCooking(pos, cooking, boatId, boatTileIndex);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetFurnaceCooking(Vector2 pos, bool cooking, string boatId, int boatTileIndex)
    {
        if (boatId != "")
        {
            GetTileOnBoat(boatId, boatTileIndex, false).SetFurnaceCooking(cooking);
            return;
        }

        TileData tile = placedTiles.Find(data => data.position == pos && (data.Item.TileType == item.tileType.furnace || data.Item.TileType == item.tileType.purifier));

        if (tile.PlacedTile)
        {
            tile.PlacedTile.tileEntity.SetFurnaceCooking(cooking);
        }
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetChestItem(Vector2 pos, short slotNumber, string itemId, short itemCount, short durability, string itemData, string boatId = "", int boatTileIndex = -1)
    {
        if (boatId != "")
        {
            GetTileOnBoat(boatId, boatTileIndex, false).SetChestItem(slotNumber, itemId, itemCount, itemData, durability);
            return;
        }

        TileData tile = placedTiles.Find(data => data.position == pos && data.Item.TileType == item.tileType.chest);

        if (tile.PlacedTile)
        {
            tile.PlacedTile.tileEntity.SetChestItem(slotNumber, itemId, itemCount, itemData, durability);
        }
    }
    TileEntity GetTileOnBoat(string boatId, int boatTileIndex, bool floor)
    {
        Boat boat = null;
        foreach (Boat b in menu.instance.boats)
        {
            if (b && b.boatID == boatId)
            {
                boat = b;
                break;
            }
        }

        if (boat != null)
        {
            return boat.tileSlotTiles[boatTileIndex].tiles[floor ? 0 : 1].tileEntity;
        }
        else
        {
            return null;
        }

    }

    public void StopUsingChest(Vector2 pos, string boatId = "", int boatTileIndex = -1)
    {
        RPC_StopUsingChest(pos, boatId, boatTileIndex);
    }
    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_StopUsingChest(Vector2 pos, string boatId, int boatTileIndex)
    {
        if (boatId != "")
        {
            GetTileOnBoat(boatId, boatTileIndex, false).StopUsingChest();
            return;
        }

        TileData tile = placedTiles.Find(data => data.position == pos && data.Item.TileType == item.tileType.chest);

        if (tile.PlacedTile)
        {
            tile.PlacedTile.tileEntity.StopUsingChest();
        }
    }

    public void SetCropParent(NetworkId viewId, Vector2 pos, string itemId, int itemAmount, Vector2 itemPos, string boatId, int boatTileIndex)
    {
        RPC_SetCropParent(viewId, pos, itemId, itemAmount, itemPos, boatId, boatTileIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    async void RPC_SetCropParent(NetworkId viewId, Vector2 pos, string itemId, int itemAmount, Vector2 itemPos, string boatId, int boatTileIndex)
    {
        placedTile t = null;
        Boat boat = null;
        if (boatId == "")
        {
            TileData tile = placedTiles.Find(data => data.position == pos && data.Item.TileType == item.tileType.crop);
            t = tile.PlacedTile;
        }
        else
        {
            foreach (Boat b in menu.instance.boats)
            {
                if (b && b.boatID == boatId)
                {
                    boat = b;
                    break;
                }
            }
            if (boat) t = boat.tileSlotTiles[boatTileIndex].tiles[1];

        }

        if (t)
        {
            NetworkObject n;
            while (!SingletonRunner.runner.TryFindObject(viewId, out n))
                await Task.Yield();

            itemPickup i = n.GetComponent<itemPickup>();

            i.transform.SetParent(t.gameObject.transform);
            i.transform.position = itemPos;
            t.tileEntity.attatchedCropItem = i;
            i.farmCropTile = t.tileEntity;

            i.naturallySpawned = true;
            i.farmGrownItem = true;
            i.Item = itemDictionary[itemId];
            i.count = itemAmount;

            if (boat)
            {
                i.boatParent = boat;
                i.boatFloor = 0;
            }

            await Task.Yield();

            if (i.Item.naturalSpawnSprite)
                i.sprite.sprite = i.Item.naturalSpawnSprite;
            else
                i.sprite.sprite = i.Item.overworldSprite;
        }

    }

    public void SetGrowthStage(Vector2 pos, int growthStage, string boatId, int boatTileIndex)
    {
        RPC_SetGrowthStage(pos, growthStage, boatId, boatTileIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetGrowthStage(Vector2 pos, int growthStage, string boatId, int boatTileIndex)
    {
        if (SingletonRunner.runner.IsSharedModeMasterClient) return;

        placedTile t = null;
        if (boatId == "")
        {
            TileData tile = placedTiles.Find(data => data.position == pos && data.Item.TileType == item.tileType.crop);
            t = tile.PlacedTile;
        }
        else
        {
            Boat boat = null;
            foreach (Boat b in menu.instance.boats)
            {
                if (b && b.boatID == boatId)
                {
                    boat = b;
                    break;
                }
            }
            if (boat) t = boat.tileSlotTiles[boatTileIndex].tiles[1];

        }

        if (t)
        {
            t.tileEntity.SetGrowthStage(growthStage);
        }
    }

    IEnumerator _HandleSign(placedTile tile)
    {
        writingSign = true;
        menu.instance.signUi.SetActive(true);
        menu.instance.signWriteText.transform.parent.parent.gameObject.GetComponent<TMP_InputField>().text = "";
        menu.instance.signWriteText.transform.parent.parent.gameObject.GetComponent<TMP_InputField>().ActivateInputField();

        while (writingSign && tile)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        menu.instance.signUi.SetActive(false);
        writingSign = false;

        string boatId = "";
        if (tile.tileEntity.boatParent)
        {
            boatId = tile.tileEntity.boatParent.boatID;
        }

        if (tile)
            RPC_SetSignText((Vector2)tile.transform.position, menu.instance.signWriteText.text, boatId,tile.boatTileIndex);

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetSignText(Vector2 pos, string text, string boatId, int boatTileIndex)
    {
        placedTile t = null;
        if (boatId == "")
        {
            TileData tile = placedTiles.Find(data => data.position == pos && data.Item.TileType == item.tileType.crop);
            t = tile.PlacedTile;
        }
        else
        {
            Boat boat = null;
            foreach (Boat b in menu.instance.boats)
            {
                if (b && b.boatID == boatId)
                {
                    boat = b;
                    break;
                }
            }
            if (boat) t = boat.tileSlotTiles[boatTileIndex].tiles[1];

        }

        if (t)
        {
            t.tileEntity.SetSignText(text);
        }
    }

    void SetBoatPreviewPos(GameObject newTile, float tileRotation)
    {
        placedTile t = newTile.GetComponent<placedTile>();

        switch (tileRotation)
        {
            case 0:
                t.boatPreview.transform.localPosition = new Vector3(boatPreviewPositions.x, 0);
                t.boatPreview.transform.localScale = new Vector3(1, 1);
                break;
            case 90:
                t.boatPreview.transform.localPosition = new Vector3(0, boatPreviewPositions.y);
                t.boatPreview.transform.localScale = new Vector3(1, 1);
                break;
            case 180:
                t.boatPreview.transform.localPosition = new Vector3(-boatPreviewPositions.x, 0);
                t.boatPreview.transform.localScale = new Vector3(-1, 1);
                break;
            case 270:
                t.boatPreview.transform.localPosition = new Vector3(0, -boatPreviewPositions.y);
                t.boatPreview.transform.localScale = new Vector3(1, 1);
                break;
        }
    }

    public static bool PercentChance(float percent)
    {
        // Apply multiplier
        float finalChance = HandMove.instance.jackpotActive ? percent : percent * 1.3f;

        // Clamp between 0 and 100
        finalChance = Mathf.Clamp(finalChance, 0f, 100f);

        // Roll
        return UnityEngine.Random.Range(0f, 100f) < finalChance;
    }

    public void LoadEnding()
    {
        SingletonRunner.runner.LoadScene(SceneRef.FromIndex(3), new LoadSceneParameters(), true);
    }

    public void SaveData(GameData data)
    {
        data.ownerUserID = DataPersistanceManager.gameOwnerID;
        data.seed = seed.instance.currentSeed;
        data.seedInput = seed.instance.seedInput;
        data.lastPlayed = DateTime.Now.ToString("MM/dd/yyyy");
        data.currentIsland = player.instance.currentIsland.index;

        data.initialRespawnPos = initialSpawnPos;

        data.playerHealth = playerHealth.instance.currentHealth;

        //save riding boat
        if (player.instance.currentBoat)
        {
            data.piloting = player.instance.pilotingBoat;
            data.currentBoat = player.instance.currentBoat.boatID;
            data.boatFloor = player.instance.boatFloor;
        }
        else
        {
            data.piloting = false;
            data.currentBoat = "";
            data.boatFloor = 0;
        }


        if (Runner.IsSharedModeMasterClient)
        {
            //view.RPC("RPC_ReceiveSaveID", RpcTarget.OthersBuffered, saveFileID);

            //placed tiles
            data.placedTiles = new SavedTileData[placedTiles.Count];
            for (int i = 0; i < placedTiles.Count; i++)
            {
                data.placedTiles[i] = new SavedTileData(placedTiles[i].Item.itemId, placedTiles[i].rotation, placedTiles[i].position, placedTiles[i].PlacedTile.health.health, placedTiles[i].PlacedTile.TileEntityDataToString(), placedTiles[i].PlacedTile.state);
            }

            //dropped items
            int itemCount = 0;
            for (int i = 0; i < itemsHolder.childCount; i++)
            {
                itemPickup iP = itemsHolder.GetChild(i).GetComponent<itemPickup>();
                if (!iP.dontSave)
                {
                    itemCount++;
                }
            }

            data.droppedItems = new SavedItemData[itemCount];
            int j = 0;
            for (int i = 0; i < itemsHolder.childCount; i++)
            {
                itemPickup iP = itemsHolder.GetChild(i).GetComponent<itemPickup>();
                if (!iP.dontSave)
                {
                    data.droppedItems[j] = new SavedItemData(iP.Item.itemId, iP.count, iP.durability, iP.itemData, iP.transform.position, iP.transform.eulerAngles.z, iP.boatFloor);
                    j++;
                }
            }

            data.enableCommands = commandsSetting;
            data.difficulty = difficulty;
            data.duelArena = inArena || savedInArena;
        }

        //boats
        data.boatData = new SavedBoatData[boatsHolder.childCount];
        for (int i = 0; i < boatsHolder.childCount; i++)
        {
            Boat b = boatsHolder.GetChild(i).GetComponent<Boat>();

            SavedItemData[] droppedBoatItems = new SavedItemData[0];
            if (b.itemHolder)
            {
                int itemCount = 0;
                for (int j = 0; j < b.itemHolder.childCount; j++)
                {
                    itemPickup iP = b.itemHolder.GetChild(j).GetComponent<itemPickup>();
                    if (!iP.dontSave)
                    {
                        itemCount++;
                    }
                }

                droppedBoatItems = new SavedItemData[itemCount];
                int k = 0;
                for (int j = 0; j < b.itemHolder.childCount; j++)
                {
                    itemPickup iP = b.itemHolder.GetChild(j).GetComponent<itemPickup>();
                    if (!iP.dontSave)
                    {
                        droppedBoatItems[k] = new SavedItemData(iP.Item.itemId, iP.count, iP.durability, iP.itemData, iP.transform.position, iP.transform.eulerAngles.z, iP.boatFloor);
                        k++;
                    }
                }
            }



            data.boatData[i] = new SavedBoatData(b.boatTypeIndex, b.transform.position, b.transform.localScale.x, b.boatID, b.EntityDataToString(), droppedBoatItems);
        }


        data.saveFileID = saveFileID;
        data.endScreenStats = endScreenStats;

    }
    public void LoadData(GameData data)
    {
        savedSeed = data.seedInput;
        savedLegacySeed = data.seed;
        savedPlayerBoat = data.currentBoat;
        savedPiloting = data.piloting;
        savedBoatFloor = data.boatFloor;
        savedCurrentIsland = data.currentIsland;
        initialSpawnPos = data.initialRespawnPos;

        if (Runner.IsSharedModeMasterClient)
        {
            saveFileID = data.saveFileID;
            //view.RPC("RPC_ReceiveSaveID", RpcTarget.Others, saveFileID);

            savedTileData = data.placedTiles;
            savedItemData = data.droppedItems;
            savedBoatData = data.boatData;
        }

        savedPlayerHealth = data.playerHealth;

        difficulty = data.difficulty;
        savedInArena = data.duelArena;
        
        if(data.endScreenStats != null && data.endScreenStats.Count > 0)
        {
            endScreenStats = data.endScreenStats;
        }

    }


}

[Serializable]
public struct TileData
{
    public float rotation;
    public item Item;
    public Vector2 position;
    public GameObject tileGameObject;
    public SpriteRenderer sprite;
    public placedTile PlacedTile;

    public bool destroyed;

    public TileData(Vector2 pos, float rot, item it, GameObject gameobject, SpriteRenderer sp, placedTile pt, bool destroyed = false)
    {
        position = pos;
        rotation = rot;
        Item = it;
        tileGameObject = gameobject;
        sprite = sp;
        PlacedTile = pt;

        this.destroyed = destroyed;
    }

}
[Serializable]
public struct SavedTileData
{
    public string Item;
    public float rotation;
    public Vector2 position;
    public int health;
    public string tileEntityData;
    public int state;

    public SavedTileData(string item, float rotation, Vector2 position, int health, string tileEntityData, int state)
    {
        Item = item;
        this.rotation = rotation;
        this.position = position;
        this.health = health;
        this.tileEntityData = tileEntityData;
        this.state = state;
    }

}
[Serializable]
public struct SavedItemData
{
    public string itemID;
    public int count;
    public int durability;
    public ItemData itemData;
    public Vector2 position;
    public float rotation;
    public int boatFloor;

    public SavedItemData(string itemID, int count, int durability, ItemData itemData, Vector2 position, float rotation, int boatFloor)
    {
        this.itemID = itemID;
        this.count = count;
        this.durability = durability;
        this.itemData = itemData;
        this.position = position;
        this.rotation = rotation;
        this.boatFloor = boatFloor;
    }

}
[Serializable]
public struct SavedBoatData
{
    public int boatIndex;
    public Vector2 position;
    public float scale;
    public string boatID;
    public string entityData;
    public SavedItemData[] savedItems;

    public SavedBoatData(int boatIndex, Vector2 position, float scale, string boatID, string entityData, SavedItemData[] savedItems)
    {
        this.boatIndex = boatIndex;
        this.position = position;
        this.scale = scale;
        this.boatID = boatID;
        this.entityData = entityData;
        this.savedItems = savedItems;
    }

}


[Serializable]
public class MusicTrack
{
    public string name;
    public AudioClip clip, swampClip, desertClip, snowClip, volcanicClip;
    public float volume;
}
[Serializable]
public class AmbienceTrack
{
    public string name;
    public AudioClip dayClip, nightClip;
    public float volume = 1;

}
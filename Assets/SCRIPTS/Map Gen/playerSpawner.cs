using Fusion;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class playerSpawner : NetworkBehaviour, IDataPersistance
{
    public static playerSpawner instance;

    public Vector2 spawnAreaSize;
    public Vector3Int PlayerSpawnPos;
    public Tilemap[] safeTilemap;
    public NetworkObject view;

    public GameObject[] playerPrefabs;
    public Sprite[] playerSprites;

    List<Vector2Int> takenSpots = new List<Vector2Int>();

    public player[] players;

    public Vector2 savedPlayerPos;
    //public Vector2 savedValeTeleportInitialPos;
    public Vector2Int savedDrownRespawnPos;
    public bool savedPyroDisabled;
    public bool savedDisableMarker;
    public int savedDungeon;

    public LayerMask planeCrashMask;
    public static int playerCountInGame, playerCountNotDead;

    public bool savedPlayerDead;
    public static bool finishedSpawningPlayers;

    int playersSpawned;
    public int envyKillRequirementAdd = 0;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        players = null;
    }

    public async void SpawnPlayers()
    {
        while (Runner.SessionInfo.PlayerCount == 0)
        {
            await Task.Yield();
        }

        GameObject playerToSpawn = playerPrefabs[0];

        playerToSpawn = playerPrefabs[DataPersistanceManager.instance.localSkin];
        playerToSpawn.GetComponent<player>().skinIndex = DataPersistanceManager.instance.localSkin;

        UnityEngine.Random.InitState(Runner.LocalPlayer.GetHashCode());

        Vector3 spawnPos = Vector3.zero;

        //get saved player position
        if (DataPersistanceManager.instance.loadingSave && savedPlayerPos != Vector2.zero) spawnPos = savedPlayerPos;
        else
        {
            spawnPos = (Vector3)randomPlayerSpawnPos();
            gameManager.instance.initialSpawnPos = spawnPos;
        }

        NetworkObject newPlayer = SingletonRunner.runner.Spawn(playerToSpawn, spawnPos, quaternion.identity, Runner.LocalPlayer);
        newPlayer.transform.SetParent(transform);


        RPC_BlacklistPosition(spawnPos, newPlayer.Id, DataPersistanceManager.LocalActorIndex);

    }


    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_BlacklistPosition(Vector2 pos, NetworkId playerId, int index)
    {
        StartCoroutine(_BlacklistPosition(pos, playerId, index));
    }

    IEnumerator _BlacklistPosition(Vector2 pos, NetworkId playerId, int index)
    {

        if (players == null)
        {
            players = new player[Runner.SessionInfo.PlayerCount];
        }

        NetworkObject n;
        SingletonRunner.runner.TryFindObject(playerId, out n);

        while (!SingletonRunner.runner.TryFindObject(playerId, out n)) yield return null;


        takenSpots.Add(Vector2Int.RoundToInt(pos));
        SingletonRunner.runner.FindObject(playerId).transform.SetParent(transform);
        //normal behavior
        if (index < players.Length)
        {
            //editor
            if (index == -1) index = 0;

            players[index] = n.GetComponent<player>();
            players[index].usernameText.color = Chat.Instance.usernameColors[index];

            playersSpawned++;

            if (playersSpawned >= SingletonRunner.runner.ActivePlayers.Count()) finishedSpawningPlayers = true;

        }
    }


    public Vector3Int randomPlayerSpawnPos()
    {
        if (gameManager.instance && gameManager.instance.disableTiles) 
            return Vector3Int.zero + new Vector3Int(-2 + DataPersistanceManager.LocalActorIndex, 0);

        int failsafe = 0;

        spawnAreaSize = new Vector2(15, 15);
        PlayerSpawnPos = new Vector3Int(0, -7);

        bool isSafe = false;

        Vector3Int spawnPos = Vector3Int.zero;

        List<Vector3Int> unsafeSpots = new List<Vector3Int>();
        featurePlacer feature = FindFirstObjectByType<featurePlacer>();
        MapDisplay mapDisplay = FindAnyObjectByType<MapDisplay>();

        while (!isSafe)
        {
            Vector3Int spawnPosCandidate = new Vector3Int(Mathf.RoundToInt(UnityEngine.Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2)), Mathf.RoundToInt(UnityEngine.Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)));

            int failsafe2 = 0;

            while (unsafeSpots.Contains(spawnPosCandidate))
            {
                spawnPosCandidate = new Vector3Int(Mathf.RoundToInt(UnityEngine.Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2)), Mathf.RoundToInt(UnityEngine.Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)));

                failsafe2++;
                if (failsafe2 > 2000)
                {
                    return new Vector3Int(DataPersistanceManager.LocalActorIndex, -8);
                }
            }

            foreach (Tilemap tilemap in safeTilemap)
            {

                if (tilemap.GetTile(spawnPosCandidate) != null &&
                    tilemap.GetTile(spawnPosCandidate + new Vector3Int(0, 1)) != null &&
                    tilemap.GetTile(spawnPosCandidate + new Vector3Int(0, -1)) != null &&
                    tilemap.GetTile(spawnPosCandidate + new Vector3Int(1, 0)) != null &&
                    tilemap.GetTile(spawnPosCandidate + new Vector3Int(-1, 0)) != null &&
                    mapDisplay.hillTilemap.GetTile(spawnPosCandidate) == null
                    )
                {
                    isSafe = true;
                    spawnPos = spawnPosCandidate;

                }
                else
                {
                    unsafeSpots.Add(spawnPosCandidate);
                }
            }

            //check for features in that spot
            if (feature.takenPositions.Contains(spawnPosCandidate) && isSafe)
            {
                unsafeSpots.Add(spawnPosCandidate);
                isSafe = false;
            }
            //check for players in that spot
            if (takenSpots.Contains(new Vector2Int(spawnPosCandidate.x, spawnPosCandidate.y)) && isSafe)
            {
                unsafeSpots.Add(spawnPosCandidate);
                isSafe = false;
            }
            //check for plane
            if (Physics2D.BoxCast(new Vector2Int(spawnPosCandidate.x, spawnPosCandidate.y), Vector2.one, 0, Vector2.zero, Mathf.Infinity, planeCrashMask) && isSafe)
            {
                unsafeSpots.Add(spawnPosCandidate);
                isSafe = false;
            }

            if (unsafeSpots.Count == spawnAreaSize.x * spawnAreaSize.y && !isSafe)
            {
                spawnAreaSize *= 2;
            }

            if (spawnAreaSize.magnitude > MapGenerator.mapChunkSize)
            {
                return new Vector3Int(DataPersistanceManager.LocalActorIndex, -8);
            }

            failsafe++;

            if (failsafe > 2000)
            {
                print("failsafe");
                break;
            }
        }

        return spawnPos;

    }

    private void Update()
    {
        if (players == null) return;

        int playerCount = players.Length;
        foreach (player p in players)
        {
            if (!p) playerCount--;
        }

        playerCountInGame = playerCount;

        foreach (player p in players)
        {
            if (p && p.health.dead) playerCount--;
        }

        playerCountNotDead = playerCount;
    }

    public void SaveData(GameData data)
    {
        data.playerPosition = player.instance.transform.localPosition;
        data.dead = playerHealth.instance.dead;

        //if (playerHealth.instance.dead) data.playerPosition = (Vector3)PlayerSpawnPos;

        if (player.instance.inBoatAltAction && player.instance.currentBoat)
        {
            data.playerPosition = player.instance.currentBoat.altPlayerPosExit[player.instance.currentAltAction].localPosition;
        }

        data.mapMarkerDisabled = menu.instance.markersDisabled[DataPersistanceManager.LocalActorIndex];
        //data.valeTeleportInitialPos = HandMove.instance.valeTeleportStartPos;

        data.pyromancerDisabled = !player.instance.fireCircleEnabled;
        data.lastGroundPos = playerWater.instance.drownRespawnPos;
        data.currentDungeon = player.instance.inDungeon + 1;
        data.envyKillRequirementAdd = envyKillRequirementAdd;
    }
    public void LoadData(GameData data)
    {
        savedPlayerPos = data.playerPosition;
        savedPlayerDead = data.dead;

        if (data.mapMarkerDisabled)
        {
            savedDisableMarker = true;
        }

        //savedValeTeleportInitialPos = data.valeTeleportInitialPos;
        savedPyroDisabled = data.pyromancerDisabled;
        savedDrownRespawnPos = data.lastGroundPos;
        savedDungeon = data.currentDungeon - 1;
        if (savedDungeon < -1) savedDungeon = -1;

        envyKillRequirementAdd = data.envyKillRequirementAdd;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(Vector2.zero, spawnAreaSize);
        Gizmos.DrawSphere(PlayerSpawnPos, 0.5f);

    }



}

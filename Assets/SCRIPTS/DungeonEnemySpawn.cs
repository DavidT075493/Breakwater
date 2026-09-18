using System;
using System.Collections;
using UnityEngine;

using System.Collections.Generic;
using Fusion;

public class DungeonEnemySpawn : MonoBehaviour
{
    public int dungeonIndex;
    public int combatRoomId;

    [Header("Activation")]
    public bool active;
    public Vector3 activationBoxOffset;
    public Vector2 activationBoxSize = new Vector2(12, 8);
    public float activationAdditional;
    public Vector2 activationBoxGlobalAdditional = new Vector2(0.5f,1);
    public LayerMask playerLayer, localPlayerLayer;

    [Header("Spawning")]
    public Transform[] spawners;
    public EnemyWaveSet[] waveSets;
    float timeBetweenWaves = 2;
    public List<Enemy> enemies = new List<Enemy>();

    [Header("Doors")]
    public Animation doorAnimator;
    public Animation entranceDoorAnimator;

    int currentWaveIndex;

    EnemyWave[] activeWaves;
    public bool completed;
    public static DungeonEnemySpawn currentlyInRoom;
    int currentSpawner;

    bool closedDoor;
    public bool hasPlayersInCombat;
    Coroutine waveRoutine, spawnRoutine;

    public bool isFinalRoom;
    // -----------------------------------------------------

    private void Start()
    {
        DungeonGenerator.instance.onEnterDungeon += OnEnterDungeon;
    }

    void FixedUpdate()
    {
        if (!MapDisplay.finishedScreenFade) return;

        // -----------------------------
        // Track current room (local)
        // -----------------------------
        if (CheckPlayerInside(true, true) && !playerHealth.instance.dead)
        {
            currentlyInRoom = this;
        }
        else if (currentlyInRoom == this && !CheckPlayerInside(true, true))
        {
            currentlyInRoom = null;
        }

        // -----------------------------
        // Activate room
        // -----------------------------
        if (SingletonRunner.runner.IsSharedModeMasterClient && !active && 
            !completed && CheckPlayerInside(false, false))
        {
            ActivateRoom();
        }

        // -----------------------------
        // Close doors when activated
        // -----------------------------
        if (CheckPlayerInside(true, false) && !closedDoor && !completed)
        {
            entranceDoorAnimator.Play("dungeon door close");
            closedDoor = true;
        }
        
        //bug fix for trapped in dungeon room upon non-master loading into one that has been completed
        if(completed && closedDoor)
        {
            closedDoor = false;
            UnlockDoors();
        }

        // -----------------------------
        // Reset if players leave
        // -----------------------------
        if (active)
        {
            hasPlayersInCombat = CheckPlayerInside(false, true);

            if (!hasPlayersInCombat)
            {
                ResetRoom();
            }
        }
    }

    void OnEnterDungeon(object sender, EventArgs e)
    {
        entranceDoorAnimator.clip = entranceDoorAnimator.GetClip("dungeon door open");
        entranceDoorAnimator.Play();
    }

    void ResetRoom()
    {
        active = false;
        hasPlayersInCombat = false;
        completed = false;

        // Stop waves
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        // Kill enemies (master only)
        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            foreach (Enemy e in enemies)
            {
                if (e != null)
                    e.health.RPC_Die();
            }

            enemies.Clear();
        }

        // Open doors
        entranceDoorAnimator.clip = entranceDoorAnimator.GetClip("dungeon door open");
        entranceDoorAnimator.Play();

        closedDoor = false;
    }

    // -----------------------------------------------------
    // ACTIVATION
    // -----------------------------------------------------

    bool GetActivationBounds(bool extended, out Vector2 center, out Vector2 size)
    {
        size = activationBoxSize + activationBoxGlobalAdditional;

        center = transform.position
            + activationBoxOffset
            + new Vector3(0, activationBoxGlobalAdditional.y / 2f);

        if (extended)
        {
            size += new Vector2(0, activationAdditional);

            center += new Vector2(0, -activationAdditional / 2f);
        }

        return true;
    }

    bool CheckPlayerInside(bool onlyLocal, bool extended)
    {
        GetActivationBounds(extended, out Vector2 center, out Vector2 size);

        int layer = onlyLocal ? localPlayerLayer : playerLayer;

        return Physics2D.OverlapBox(
            center,
            size,
            0f,
            layer
        );
    }

    void ActivateRoom()
    {
        active = true;

        // choose random wave set
        EnemyWaveSet set =
            waveSets[UnityEngine.Random.Range(0, waveSets.Length)];

        activeWaves = set.waves;
        currentWaveIndex = 0;

        waveRoutine = StartCoroutine(WaveRoutine());
        DungeonGenerator.instance.RPC_SetInCombat(true, dungeonIndex, combatRoomId);
    }

    // -----------------------------------------------------
    // WAVES
    // -----------------------------------------------------

    IEnumerator WaveRoutine()
    {
        while (currentWaveIndex < activeWaves.Length)
        {
            spawnRoutine = StartCoroutine(_SpawnWave(activeWaves[currentWaveIndex]));
            currentWaveIndex++;

            while (enemies.Count > 0)
            {
                yield return null;
                enemies.RemoveAll(e => e == null || e.health.dead);
            }

            yield return new WaitForSeconds(timeBetweenWaves);
        }

        EndRoom();
    }

    IEnumerator _SpawnWave(EnemyWave wave)
    {
        for (int i = 0; i < wave.enemies.Length; i++)
        {
            GameObject prefab = wave.enemies[i];
            int count = wave.enemyCounts[i];

            for (int j = 0; j < count; j++)
            {
                Transform spawn = spawners[currentSpawner];
                currentSpawner++;
                if (currentSpawner >= spawners.Length) currentSpawner = 0;

                NetworkObject enemy = SingletonRunner.runner.Spawn(
                    prefab,
                    spawn.position + new Vector3(UnityEngine.Random.Range(-1.5f,1.5f), UnityEngine.Random.Range(-0.5f, 0.5f)),
                    Quaternion.identity
                );
                Enemy e = enemy.GetComponent<Enemy>();

                enemies.Add(e);
                DungeonGenerator.instance.RPC_RegisterEnemy(enemy, dungeonIndex);

                yield return new WaitForSeconds(1f);
            }
        }
    }


    // -----------------------------------------------------
    // DOORS
    // -----------------------------------------------------


    public void UnlockDoors()
    {
        doorAnimator.Play();
        entranceDoorAnimator.clip = entranceDoorAnimator.GetClip("dungeon door open");
        entranceDoorAnimator.Play();
        completed = true;
        closedDoor = false;
    }

    // -----------------------------------------------------

    void EndRoom()
    {
        if(currentlyInRoom == this && isFinalRoom)
        {
            gameManager.endScreenStats["dungeons"]++;
        }

        completed = true;
        closedDoor = false;
        active = false;
        DungeonGenerator.instance.completedCombatRooms.TryAdd(combatRoomId, true);

        DungeonGenerator.instance.RPC_SetInCombat(false,dungeonIndex, combatRoomId);
    }

    // -----------------------------------------------------
    // DEBUG
    // -----------------------------------------------------

    void OnDrawGizmosSelected()
    {
        // Normal activation bounds
        GetActivationBounds(false, out Vector2 normalCenter, out Vector2 normalSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(normalCenter, normalSize);

        // Extended activation bounds
        GetActivationBounds(true, out Vector2 extendedCenter, out Vector2 extendedSize);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(extendedCenter, extendedSize);
    }

}


[System.Serializable]
public class EnemyWave
{
    public GameObject[] enemies;
    public int[] enemyCounts;

}

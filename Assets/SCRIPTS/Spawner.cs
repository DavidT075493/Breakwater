using Fusion;
using System.Collections;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    public Vector2 spawnCooldown = new Vector2(10, 20);
    public GameObject[] enemies;
    public bool disabled;

    public IEnumerator _SpawnEnemies()
    {
        while (!disabled)
        {
            EnemySpawner.instance.enemies.RemoveAll(e => e == null);

            if (EnemySpawner.instance.enemies.Count < 3 + (playerSpawner.playerCountNotDead * 2) && !disabled)
            {
                //spawn eldritch boys starting at wood heart
                int maxEnemyType = 2;
                if (Boss.instance.heartHealth.bossHeartPhase > 0)
                {
                    maxEnemyType = 3;
                }
                if (Boss.instance.heartHealth.bossHeartPhase > 1)
                {
                    maxEnemyType = enemies.Length;
                    yield return new WaitForSeconds(1.5f);
                }

                if (Boss.instance.heartDead) yield break;

                NetworkObject enemy = Runner.Spawn(enemies[Random.Range(0, maxEnemyType)], transform.position, Quaternion.identity);
                //RPC_SetParent(enemy.Id);


                EnemySpawner.instance.enemies.Add(enemy.gameObject);
            }
            yield return new WaitForSeconds(Random.Range(spawnCooldown.x, spawnCooldown.y)
                * (1.3f - (Mathf.Clamp(playerSpawner.playerCountNotDead, 1, 3) * 0.3f)));
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetParent(NetworkId enemyId)
    {
        Runner.FindObject(enemyId).transform.SetParent(transform, true);
    }

}

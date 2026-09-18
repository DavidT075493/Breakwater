using Fusion;
using System.Collections;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    public ParticleSystem explosion;
    public AudioSource explosionSound;
    NetworkObject view;

    private void Start()
    {
        view = GetComponent<NetworkObject>();
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Shoot(Vector3 targetPos, int direction, Vector3 rockPosition, NetworkObject enemyHand = null) 
    {
        StartCoroutine(_Shoot(targetPos, direction, rockPosition,enemyHand));
    }

    IEnumerator _Shoot(Vector3 targetPos, int direction, Vector3 rockPosition, NetworkObject enemyHand = null)
    {
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            if (direction == 1)
            {
                transform.localScale = new Vector2(-1, 1);
            }
            else
            {
                transform.localScale = new Vector2(1, 1);
            }

            transform.Translate(new Vector3(direction * 35 * Time.deltaTime, 0, 0));
            yield return null;
        }
        float moveSpeed = 25;
        while (Vector2.Distance(transform.position, targetPos) > 1f)
        {
            Vector3 dir = new Vector3(targetPos.x - transform.position.x, targetPos.y - transform.position.y).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            transform.eulerAngles = new Vector3(0, 0, angle);

            transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            moveSpeed += Time.deltaTime * 25f;
            yield return null;
        }

        explosion.transform.SetParent(null);
        explosion.Play();
        explosionSound.Play();

        transform.GetChild(0).gameObject.SetActive(false);

        if(enemyHand == null)
        {
            treeRPCs.Instance.destroyedRocks.TryAdd(rockPosition, true);
            Destroy(featurePlacer.Instance.oceanStuffList.Find(x => x != null && (Vector2)x.transform.position == (Vector2)rockPosition));
        }
        else
        {
            EnemyOceanHand enemyOceanHand = enemyHand.GetComponent<EnemyOceanHand>();
            if (enemyOceanHand.view.HasStateAuthority)
            {
                enemyOceanHand.health.RPC_Die();
            }

        }
        if (view.HasInputAuthority)
        {
            yield return new WaitForSeconds(1.5f);

            Runner.Despawn(view);
        }
    }
}

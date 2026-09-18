using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    Vector2 target;
    Vector3 direction;
    public float speed = 3;
    public float hardSpeed = 3;
    [Tooltip("0 = go to exact player position")]
    public float moveTime = 3;
    public float moveTimeVariance;
    public GameObject spawnOnDestroy;
    public float hardModeExplosionScale = 1;
    public NetworkObject view;
    public Rigidbody2D rb;
    [SerializeField] Collider2D collider;
    bool stop;
    public Transform owner;
    bool reflected;
    float moveTimer = 0;

    private void Awake()
    {
        stop = true;

        if (gameManager.instance.difficulty > 0) speed = hardSpeed;
    }

    [Rpc(RpcSources.All,RpcTargets.All)]
    public void RPC_SetDirection(Vector2 targetTransform, NetworkId ownerId, float angleSkew = 0)
    {
        target = targetTransform;
        NetworkObject o = SingletonRunner.runner.FindObject(ownerId);
        if (o) owner = o.transform;

        direction = new Vector3(target.x - transform.position.x, target.y - transform.position.y).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        stop = false;

        transform.eulerAngles = new Vector3(0, 0, angle + angleSkew);

        StartCoroutine(_Destroy());
    }

    IEnumerator _Destroy()
    {
        moveTimer = 0;
        float totalMoveTime = Random.Range(moveTime - (moveTimeVariance / 2), moveTime + (moveTimeVariance / 2));
        if(moveTime != 0)
        {
            while(moveTimer < moveTime)
            {
                moveTimer += Time.deltaTime;
                yield return null;
            }

        }
        else
        {
            while(Vector2.Distance(transform.position,target) > 0.3f)
            {
                yield return null;
            }
        }
        
        if(view.HasStateAuthority)
        RPC_Destroy();
    }

    public void Destroy()
    {
        RPC_Destroy();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    async void RPC_Destroy()
    {
        GameObject expl = Instantiate(spawnOnDestroy, transform.position, Quaternion.identity);

        if(hardModeExplosionScale != 1 && gameManager.instance.difficulty > 0)
        {
            expl.transform.localScale = new Vector3(hardModeExplosionScale, hardModeExplosionScale);
        }

        gameObject.SetActive(false);
        await Task.Delay(1000);
        
        if (view.HasStateAuthority)
        {
            Runner.Despawn(view);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (reflected) return;

        //shield
        if(collision.CompareTag("Weapon Hitbox") && !collision.isTrigger && view.HasStateAuthority && !stop)
        {
            //reflect
            if(inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemData.tier >= 4)
            {
                RPC_SetDirection(owner.position, owner.GetComponent<NetworkObject>().Id, 0);
                reflected = true;
                moveTimer = -1.5f;
                speed += 2;

                GetComponent<damagePlayer>().damageEnemy = true;
                StartCoroutine(_RefreshCollider());
                HandMove.instance.RPC_ShieldReflectSound();

                return;
            }

            stop = true;
            RPC_Destroy();
            collider.enabled = false;
        }
    }

    IEnumerator _RefreshCollider()
    {
        collider.enabled = false;
        yield return new WaitForFixedUpdate();
        collider.enabled = true;
    }

    void FixedUpdate()
    {
        if(!stop)
        rb.MovePosition(transform.position + (transform.right * speed * Time.fixedDeltaTime));
    }
}

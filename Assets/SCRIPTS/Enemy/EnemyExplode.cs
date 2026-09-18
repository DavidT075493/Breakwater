using Fusion;
using System.Collections;
using UnityEngine;

public class EnemyExplode : NetworkBehaviour
{
    public float explodeRange;
    public float explodeStartup;
    public GameObject explosion;
    public Enemy enemy;
    public LayerMask playerLayer;
    public bool exploding;
    public Animator anim;
    Rigidbody2D rb;
    public AudioSource startExplosionSound;

    public Vector2 explodeStartDelay;
    float explodeStartTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        explodeStartTimer = Random.Range(explodeStartDelay.x, explodeStartDelay.y);
    }

    void FixedUpdate()
    {
        if (!enemy.target || !HasStateAuthority) return;

        Collider2D playerCheck = Physics2D.OverlapCircle(transform.position, explodeRange,playerLayer);

        if (playerCheck && !exploding && enemy.health.currentHealth > 0 && enemy.HasLineOfSight(enemy.target))
        {
            explodeStartTimer -= Time.fixedDeltaTime;

            if(explodeStartTimer < 0)
            {
                exploding = true;
                RPC_Explode();
            }
            
        }

    }


    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_Explode()
    {
        explodeStartTimer = Random.Range(explodeStartDelay.x,explodeStartDelay.y);
        StartCoroutine(_Explosion());
    }

    IEnumerator _Explosion()
    {
        if (enemy.health.stunned) yield break;

        exploding = true;
        rb.bodyType = RigidbodyType2D.Kinematic;

        enemy.ai.enabled = false;

        anim.SetBool("Walking", false);
        anim.SetFloat("ExplodeSpeed", 1);
        anim.SetTrigger("Explode");

        if(gameManager.instance.difficulty == 2)
        {
            anim.SetFloat("ExplodeSpeed", 1.2f);
            startExplosionSound.pitch = 1.1f;
            startExplosionSound.Play();
            yield return new WaitForSeconds(explodeStartup / 1.2f);
        }
        else
        {
            startExplosionSound.pitch = 1f;
            startExplosionSound.Play();
            yield return new WaitForSeconds(explodeStartup);
        }
        

        if(enemy.health.currentHealth > 0 && !enemy.health.stunned && !enemy.health.frozen)
        {
            GameObject expl = Instantiate(explosion,transform.position,Quaternion.identity,transform);
            yield return new WaitForSeconds(0.15f);
            expl.GetComponent<Collider2D>().enabled = false;
        }

        if(gameManager.instance.difficulty == 0)
            yield return new WaitForSeconds(0.75f);
        if (gameManager.instance.difficulty == 1)
            yield return new WaitForSeconds(0.5f);

        enemy.ai.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;

        yield return new WaitForSeconds(0.5f);
        exploding = false;

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRange);
    }

}

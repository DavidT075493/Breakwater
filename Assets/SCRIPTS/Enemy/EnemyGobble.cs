using Fusion;
using System.Collections;
using UnityEngine;

public class EnemyGobble : NetworkBehaviour
{
    bool gobbling;
    [SerializeField] Animator anim;
    [SerializeField] Collider2D healthCollider,damageCollider;
    [SerializeField] AudioSource sound;
    [SerializeField] enemyHealth health;
    [SerializeField] damagePlayer damage;

    private void Start()
    {
        DisableHitbox();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if((collision.CompareTag("Main Player")
            || (collision.gameObject.layer == 25 && Runner.IsSharedModeMasterClient)) 
            && !gobbling && health.currentHealth > 0)
        {
            RPC_Gobble();
        }

    }

    [Rpc(RpcSources.All,RpcTargets.All)]
    void RPC_Gobble()
    {
        sound.pitch = Random.Range(1.02f, 1.07f);
        sound.Play();
        gobbling = true;
        anim.SetTrigger("Gobble");
    }

    void DisableHitbox()
    {
        healthCollider.enabled = false;
        damageCollider.enabled = false;
        damage.active = false;
    }
    void DisableDamage()
    {
        damageCollider.enabled = false;
        damage.active = false;
    }

    void EnableHitbox()
    {
        healthCollider.enabled = true;
        damageCollider.enabled = true;
        damage.active = true;
    }

    public void FinishGobble()
    {
        gobbling = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}

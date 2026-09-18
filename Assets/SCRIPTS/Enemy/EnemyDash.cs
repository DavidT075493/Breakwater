using Fusion;
using System.Collections;
using UnityEngine;

public class EnemyDash : MonoBehaviour
{
    Enemy enemy;
    public Vector2 dashWaitTime;
    public float dashChargeDelay;
    float dashWaitTimer;
    public bool dashing;
    public Animator anim;
    Rigidbody2D rb;
    NetworkObject view;
    [SerializeField] AudioSource dashSound;
    public Transform hands;
    public float dashForce;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
        view = GetComponent<NetworkObject>();
        rb = GetComponent<Rigidbody2D>();

        dashWaitTimer = Random.Range(dashWaitTime.x, dashWaitTime.y) - 1;
    }

    void FixedUpdate()
    {
        if (!view.HasStateAuthority)
        {
            if (dashWaitTimer < 1) dashWaitTimer = Random.Range(dashWaitTime.x, dashWaitTime.y);

            return;
        }

        if (enemy.target && !dashing && enemy.health.currentHealth > 0 && enemy.HasLineOfSight(enemy.target) 
            && !enemy.health.frozen)
        {
            dashWaitTimer -= Time.fixedDeltaTime;

            if (dashWaitTimer < 0)
            {
                dashWaitTimer = Random.Range(dashWaitTime.x, dashWaitTime.y);
                RPC_Dash();
            }

        }

    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Dash()
    {
        StartCoroutine(_Dash());
    }

    IEnumerator _Dash()
    {
        anim.SetTrigger("Hands");
        dashing = true;
        yield return new WaitForSeconds(dashChargeDelay);

        if (!enemy.target || enemy.health.dead || enemy.health.frozen)
        {
            dashing = false;
            yield break;
        }
        Vector2 targetPos = transform.position + (enemy.target.transform.position - transform.position)
            .normalized * dashForce;
        Vector2 startPos = transform.position;

        float dashTime = 1.35f;
        dashSound.Play();

        for(float t = dashTime; t > 0; t -= Time.deltaTime)
        {
            hands.transform.eulerAngles += new Vector3(0, 0, 720 * Time.deltaTime);

            float progress = 1 - t / dashTime;
            Vector2 next = Vector2.Lerp(startPos, targetPos, progress);

            rb.MovePosition(next);

            if (enemy.health.dead || enemy.health.frozen) yield break;

            yield return null;
        }

        dashing = false;

    }

}

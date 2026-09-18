using DG.Tweening;
using Fusion;
using System.Collections;
using UnityEngine;

public class EnemyLeap : NetworkBehaviour
{
    Enemy enemy;
    public float leapMaxDist;
    public Vector2 leapWaitTime;
    public float leapChargeDelay;
    float leapWaitTimer;
    public bool leaping;
    public bool inAir;
    public LayerMask playerLayer;
    public Animator anim;
    Rigidbody2D rb;
    NetworkObject view;
    public Transform shadow;
    [SerializeField] AudioSource jumpSound, soarSound;
    public float leapTime;
    public Collider2D normalCollider, leapCollider;

    public float targetRandomness = 2.5f;

    public float leapBackDist = 0;
    private Coroutine leapCo;
    float shadowAlpha;
    SpriteRenderer shadowSprite;

    private void Start()
    {
        shadowSprite = shadow.GetComponent<SpriteRenderer>();
        shadowAlpha = shadowSprite.color.a;

        enemy = GetComponent<Enemy>();
        view = GetComponent<NetworkObject>();
        rb = GetComponent<Rigidbody2D>();

        leapCollider.enabled = false;
        leapWaitTimer = 0;
        if (gameManager.instance.difficulty == 0)
        {
            if (Random.Range(0, 4) < 3)
            {
                leapWaitTimer = leapWaitTime.x;
            }
        }
        else
        {
            if (Random.Range(0, 2) < 1)
            {
                leapWaitTimer = leapWaitTime.x;
            }
        }

    }

    void FixedUpdate()
    {
        if (!view.HasStateAuthority || enemy.target == null)
        {
            return;
        }
        Collider2D playerCheck = Physics2D.OverlapCircle(transform.position, leapMaxDist, playerLayer);

        if (playerCheck && !leaping && enemy.health.currentHealth > 0 && enemy.HasLineOfSight(enemy.target) 
            && !enemy.health.stunned && !enemy.health.frozen)
        {
            leapWaitTimer -= Time.fixedDeltaTime;

            if (leapWaitTimer < 0)
            {
                RPC_Leap();
            }

        }

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Leap()
    {
        leapCo = StartCoroutine(_Leap());
    }

    IEnumerator _Leap()
    {
        leapWaitTimer = Random.Range(leapWaitTime.x, leapWaitTime.y);

        leaping = true;
        anim.SetBool("Jump", true);
        yield return new WaitForSeconds(leapChargeDelay);
        if (enemy.target == null || enemy.health.frozen || enemy.health.stunned)
        {
            leaping = false;
            anim.SetBool("Jump", false);
            yield break;
        }
        jumpSound.Play();
        soarSound.Play();
        soarSound.volume = 0;

        Vector2 startPos = transform.position;

        Vector2 targetPos;

        if (Vector2.Distance(transform.position,enemy.target.transform.position) > leapBackDist)
        {
            //leap towards target
            targetPos = enemy.target.transform.position
                + (enemy.target.transform.position - transform.position).normalized * 4
                + new Vector3(Random.Range(-targetRandomness, targetRandomness), Random.Range(-targetRandomness, targetRandomness));
        }
        else
        {
            //leap away
            targetPos = (transform.position - (enemy.target.transform.position.normalized * 4.2f))
                + new Vector3(Random.Range(-targetRandomness, targetRandomness), Random.Range(-targetRandomness, targetRandomness));

            enemy.shootTimer -= 1.5f;
        }
        float jumpTime = Vector2.Distance(startPos, targetPos) * leapTime;
        float jumpHeight = 1f;

        enemy.ai.enabled = false;

        enemy.sprite.sortingOrder = 12;

        Vector3 direction = Vector3.zero;

        inAir = true;
        shadowSprite.color = new Color(shadowSprite.color.r, shadowSprite.color.g, shadowSprite.color.b, 0);
        shadowSprite.DOFade(shadowAlpha, 0.5f);
        shadow.gameObject.SetActive(true);
        leapCollider.enabled = true;
        normalCollider.enabled = false;

        for (float t = 0; t < jumpTime; t += Time.deltaTime)
        {
            soarSound.volume = Mathf.Lerp(soarSound.volume, 1, Time.deltaTime * 2);

            float progress = t / jumpTime;
            Vector2 flatPos = Vector2.Lerp(startPos, targetPos, progress);
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            Vector3 nextPos = flatPos + Vector2.up * height;

            float angle = 0;
            direction = Vector3.Slerp(direction, new Vector3(nextPos.x - transform.position.x, nextPos.y - transform.position.y).normalized, Time.deltaTime * 3);
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            enemy.sprite.transform.parent.eulerAngles = new Vector3(0, 0, angle - 90);

            shadow.transform.localPosition = new Vector2(0, -0.344f - height);
            if (view.HasStateAuthority && Vector2.Distance(rb.position, nextPos) < 2)
                rb.MovePosition(nextPos);

            yield return null;

            if (enemy.health.currentHealth <= 0 || enemy.health.frozen)
            {
                StartCoroutine(_CancelJump());
                yield break;
            }
        }

        shadow.gameObject.SetActive(false);

        leapCollider.enabled = false;
        normalCollider.enabled = true;

        enemy.sprite.sortingOrder = 10;

        shadow.transform.position = transform.position - new Vector3(0, 0.344f);

        //transform.position = targetPos;
        anim.SetBool("Jump", false);
        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            soarSound.volume = Mathf.Lerp(soarSound.volume, 0, Time.deltaTime * 10);

            float angle = 0;
            direction = Vector3.Slerp(direction, Vector2.right, Time.deltaTime * 10);
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            enemy.sprite.transform.parent.eulerAngles = new Vector3(0, 0, angle);
            yield return null;
            
            if(t >= 0.2f)
            {
                inAir = false;  
            }

        }
        inAir = false;
        StartCoroutine(enemy._WalkToNearestLand());

        soarSound.Stop();

        enemy.sprite.transform.parent.eulerAngles = new Vector3(0, 0, 0);
        leaping = false;
    }

    IEnumerator _CancelJump()
    {
        if (leapCo != null) StopCoroutine(leapCo);

        anim.SetBool("Jump", false);
        enemy.sprite.transform.parent.DORotate(new Vector3(0, 0, 0), 0.2f);
        soarSound.Stop();
        enemy.sprite.sortingOrder = 10;

        Vector2 shadowPos = shadow.transform.position;

        shadowSprite.DOFade(0, 0.5f);

        for (float t = 0.5f; t > 0; t -= Time.deltaTime)
        {
            rb.MovePosition(Vector2.MoveTowards(transform.position, shadowPos, Time.fixedDeltaTime * 5));
            yield return new WaitForFixedUpdate();
        }

        anim.SetBool("Jump", false);

        inAir = false;
        leaping = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(!collision.gameObject.CompareTag("Main Player") && leapCo != null && leaping)
        {
            StartCoroutine(_CancelJump());
        }

    }

}

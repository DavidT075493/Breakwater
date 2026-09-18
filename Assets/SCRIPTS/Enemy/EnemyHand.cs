using DG.Tweening;
using Fusion;
using System.Collections;
using UnityEngine;

public class EnemyHand : NetworkBehaviour
{
    public Enemy enemy;
    public Vector2 grabTime;
    float grabTimer;
    [SerializeField] SpriteRenderer handSprite;
    [SerializeField] GameObject hand;
    public bool grabbing;
    [SerializeField] float grabSpeed = 2;
    [SerializeField] LineRenderer grabLine;
    [SerializeField] AudioSource grabSound;
    [SerializeField] Transform grabTarget;
    bool dead;
    [SerializeField] NetworkObject view;
    bool grabbedLocalPlayer;
    public bool pullingPlayer;
    [SerializeField] LayerMask mainPlayerLayer;
    [SerializeField] NetworkTransform networkTransform;

    [SerializeField] Vector2Int handDamageAmount;
    Coroutine pullCo;
    bool stunned;
    public static bool playerGrabCooldown = false;

    private void Start()
    {
        grabTimer = grabTime.y;
        handSprite.enabled = false;
        grabLine.enabled = false;
        
    }

    void Update()
    {
        if (enemy.target != null && view.HasStateAuthority)
        {
            if (!grabbing && !dead && !enemy.health.stunned)
            {
                grabTimer -= Time.deltaTime;

                if (grabTimer <= 0)
                {
                    grabTimer = Random.Range(grabTime.x, grabTime.y);

                    RPC_Grab();
                    grabbing = true;
                }
            }

        }
        else
        {
            grabTimer = grabTime.y;
        }

        if (grabbing)
        {
            if (!dead)
                grabLine.enabled = true;
            else
                grabLine.enabled = false;
            
            Vector2 direction = grabTarget.transform.position - transform.position;
            float rot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (transform.lossyScale.x < 0) rot *= -1;

            grabLine.SetPosition(1, transform.parent.position);
            grabLine.SetPosition(0, hand.transform.position - (Vector3)direction.normalized * 0.2f);

            grabLine.material.SetFloat("_PreRotation", -rot);
            grabLine.material.SetFloat("_PostRotation", rot);

            handSprite.material.SetFloat("_PreRotation", rot - 90);

            if (enemy.health.currentHealth <= 0 && !dead)
            {
                dead = true;
                handSprite.DOFade(0, 1);
                StopAllCoroutines();

                if (grabbing)
                    StartCoroutine(_CancelGrab());

            }

            if (enemy.health.stunned && grabbing && !stunned)
            {
                StopAllCoroutines();
                stunned = true;

                StartCoroutine(_CancelGrab());
            }

            if(grabbedLocalPlayer && playerWater.instance.drowning)
            {
                StopAllCoroutines();
                StartCoroutine(_CancelGrab());
            }

        }
        else
        {
            grabLine.enabled = false;
        }

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_Grab()
    {
        StartCoroutine(_Grab());
    }

    IEnumerator _Grab()
    {
        stunned = false;
        grabbing = true;
        pullingPlayer = false;
        grabbedLocalPlayer = false;

        Transform target = enemy.target.transform;
        transform.localPosition = Vector2.zero;

        handSprite.enabled = true;
        handSprite.color = new Color(1, 1, 1, 0);
        handSprite.DOFade(1, 0.4f);

        hand.transform.localPosition = Vector2.zero;

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            direction = Vector3.Slerp(direction, ((Vector2)target.position - (Vector2)transform.position).normalized, Time.deltaTime * 2);
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            grabTarget.transform.parent.localEulerAngles = new Vector3(0, 0, angle);
            yield return null;
        }

        grabSound.Play();

        handSprite.color = Color.white;

        for (float t = 0; t < 0.7f; t += Time.deltaTime)
        {
            Collider2D enterCheck = Physics2D.OverlapBox(hand.transform.position,
                new Vector2(0.5f, 0.5f), 0, mainPlayerLayer);

            if (enterCheck && !player.instance.inCutscene && !player.instance.beingGrabbedBy && !playerGrabCooldown) grabbedLocalPlayer = true;

            if (grabbedLocalPlayer)
            {
                RPC_PullInPlayer(player.instance.view.Id);

                yield break;
            }

            direction = Vector3.Slerp(direction, ((Vector2)target.position - (Vector2)transform.position).normalized, Time.deltaTime * 1f);
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            grabTarget.transform.parent.localEulerAngles = new Vector3(0, 0, angle);

            hand.transform.position = Vector2.MoveTowards(hand.transform.position, grabTarget.position, grabSpeed * Time.deltaTime * 1f);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        //pull back without player grabbed
        for (float t = 0; t < 0.7f; t += Time.deltaTime)
        {
            hand.transform.localPosition = Vector2.MoveTowards(hand.transform.localPosition, Vector2.zero, grabSpeed * Time.deltaTime * 1f);
            yield return null;
        }

        handSprite.DOFade(0, 0.5f);
        grabbing = false;

        yield return new WaitForSeconds(0.5f);

        handSprite.enabled = false;
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_PullInPlayer(NetworkId playerId)
    {
        if (pullingPlayer) return;

        pullingPlayer = true;
        StopAllCoroutines();
        pullCo = StartCoroutine(_PullInPlayer(player.instance.view.Id == playerId));
    }

    IEnumerator _PullInPlayer(bool localPlayer)
    {
        if (localPlayer)
        {
            player.instance.beingGrabbedBy = this;
            playerGrabCooldown = true;
        }

        for (float t = 0; t < 0.6f; t += Time.deltaTime)
        {
            if (localPlayer)
            {
                hand.transform.position = Vector2.Lerp(hand.transform.position, grabTarget.position, grabSpeed * Time.deltaTime * 1.5f);
                player.instance.transform.position = hand.transform.position;
            }

            yield return null;
        }

        float damageInterval = 0.8f;
        float nextDamageTime = damageInterval;

        for (float t = 0; t < 3f; t += Time.deltaTime)
        {
            if (localPlayer)
            {
                player.instance.transform.position = hand.transform.position;
                if (t > nextDamageTime)
                {
                    nextDamageTime += damageInterval;
                    int d = handDamageAmount.x;
                    if (gameManager.instance.difficulty > 0) d = handDamageAmount.y;

                    playerHealth.instance.hurtPlayer(d, 0, 0, transform.position,
                        "was smothered by shadow boy", false, true, -1, "null", transform.position);

                    handSprite.transform.DOShakePosition(0.4f, 0.35f);
                }

            }

            hand.transform.localPosition = Vector2.MoveTowards(hand.transform.localPosition, Vector2.zero, grabSpeed * Time.deltaTime * 0.2f);
            yield return null;
        }

        handSprite.DOFade(0, 0.5f);
        grabbing = false;

        if (localPlayer)
        {
            player.instance.beingGrabbedBy = null;
        }

        yield return new WaitForSeconds(0.5f);

        pullingPlayer = false;
        handSprite.enabled = false;

        yield return new WaitForSeconds(0.5f);

        playerGrabCooldown = false;
    }

    public IEnumerator _CancelGrab()
    {
        if (pullCo != null) StopCoroutine(pullCo);

        if (grabbedLocalPlayer)
        {
            player.instance.beingGrabbedBy = null;
        }

        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            hand.transform.localPosition = Vector2.MoveTowards(hand.transform.localPosition, Vector2.zero, grabSpeed * Time.deltaTime * 2f);
            yield return null;
        }

        handSprite.DOFade(0, 0.5f);
        grabbing = false;
        yield return null;
        grabLine.enabled = false;
        grabLine.SetPosition(0, Vector3.zero);
        grabLine.SetPosition(1, Vector3.zero);

        yield return new WaitForSeconds(0.5f);
        pullingPlayer = false;
        handSprite.enabled = false;
        playerGrabCooldown = false;
    }

}

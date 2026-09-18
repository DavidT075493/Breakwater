using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Processors;
using UnityEngine.UIElements;

public class SoulBreakerHeart : MonoBehaviour
{
    public Rigidbody2D heartRb;
    Vector2 heartTargetPos;
    [SerializeField]
    float heartMaxDist, heartSpeed;
    [SerializeField]
    SpriteRenderer sprite;
    private bool dead;
    [SerializeField]
    AudioSource deadSound;
    [SerializeField] LineRenderer line;
    public Transform attachedObj;
    public enemyHealth attachedEnemy;
    public item soulBreakerItem;

    void Start()
    {
        sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0);
        sprite.DOFade(1, 0.5f);
        StartCoroutine(_FadeLineIn());
        heartRb.linearVelocity = Vector2.zero;

        heartTargetPos = (Vector2)attachedObj.position + new Vector2(
                Random.Range(-heartMaxDist, heartMaxDist),
                Random.Range(-heartMaxDist, heartMaxDist)
            );
    }

    // Update is called once per frame
    void Update()
    {
        line.SetPosition(0, transform.position);
        
        if(attachedObj == null || (attachedEnemy && attachedEnemy.dead))
        {
            if (!dead)
            {
                dead = true;
                StartCoroutine(_Die());
            }
            return;
        }

        Vector2 currentPos = transform.position;

        if (Vector2.Distance(currentPos, heartTargetPos) < 0.3f)
        {
            // Pick a new random target position around the object
            heartTargetPos = (Vector2)attachedObj.position + new Vector2(
                Random.Range(-heartMaxDist, heartMaxDist),
                Random.Range(-heartMaxDist, heartMaxDist)
            );
        }
        else
        {
            // Calculate desired velocity
            Vector2 desiredVelocity = (heartTargetPos - currentPos).normalized * heartSpeed;

            heartRb.linearVelocity = Vector2.Lerp(heartRb.linearVelocity, desiredVelocity, Time.deltaTime * 9);
        }

        line.SetPosition(1, attachedObj.position);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Weapon Hitbox") && !dead &&
            inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.breaker)
        {
            StartCoroutine(_Die());
            dead = true;
        }
    }

    IEnumerator _Die()
    {
        StartCoroutine(_FadeLineOut());
        sprite.DOKill();
        sprite.DOFade(0, 0.3f);
        transform.DOScale(1.4f, 0.3f);
        deadSound.Play();

        yield return new WaitForSeconds(0.2f);

        if (attachedEnemy && !attachedEnemy.dead)
        {
            attachedEnemy.TakeDamage(28, 0, 0, transform.position, false, true, soulBreakerItem.itemId, 1, true, -1, "soul crit");
            HandMove.instance.PvpHit(attachedEnemy.currentHealth <=0 , false, transform.position, false, false, 0);
        }

        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }

    IEnumerator _FadeLineOut()
    {
        float startAlpha = line.material.GetFloat("_Sprite_Alpha");

        float t = 0;
        float fadeDuration = 0.5f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Lerp(1, 0, t / fadeDuration);

            float c = startAlpha;
            c = alpha;

            line.material.SetFloat("_Sprite_Alpha",c);

            yield return null;
        }

        line.material.SetFloat("_Sprite_Alpha",0);
    }

    IEnumerator _FadeLineIn()
    {
        float startAlpha = 0;

        float t = 0;
        float fadeDuration = 0.5f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Lerp(0, 1, t / fadeDuration);

            float c = startAlpha;
            c = alpha;

            line.material.SetFloat("_Sprite_Alpha", c);

            yield return null;
        }

        line.material.SetFloat("_Sprite_Alpha", 1);
    }
}

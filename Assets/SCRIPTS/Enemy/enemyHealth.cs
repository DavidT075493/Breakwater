using DG.Tweening;
using Fusion;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class enemyHealth : NetworkBehaviour
{
    public static Queue<enemyHealth> deathQueue = new Queue<enemyHealth>();

    public float startAlpha = 1;
    public float knockbackScale = 0.9f;
    public int maxHealth = 20;
    public int healthPerNight = 3;
    public int currentHealth;
    public Rigidbody2D rb;
    public bool beingKnocked;
    public bool disableKnockback;
    public SpriteRenderer sprite, shadowSprite;
    public NetworkObject view;
    public camShakeController camShake;
    public GameObject hitParticles;
    public bool hitParticlesAccuratePosition;
    public AIPath path;
    public Collider2D[] Colliders;
    public Animator anim;
    public AudioSource deathSound;
    public bool flying;
    [SerializeField] Enemy enemy;
    public bool dead;

    Coroutine knockbackCo;
    public item lootItem, uniqueLootItem;
    public item[] uniqueLootItems;
    public Vector2Int lootAmountRange = new Vector2Int(1, 2);
    public GameObject itemPrefab;
    public float uniqueLootChance = 0.2f;

    public AudioSource ambientSound;
    public string ignoreDamageType;
    public Boss boss;
    public bool dontDieAtDay;
    public Light2D Light;
    int iceEffectStacks;

    public enum damageType
    {
        damage,
        treeDamage,
        rockDamage
    }
    public damageType effectiveDamage = damageType.damage;
    public bool bossHeart;
    [HideInInspector]
    public int bossHeartPhase;
    [SerializeField] Sprite[] altSprites;
    bool beingKnockedByOther;

    public GameObject damageNum;
    public bool stunned;

    public float deathTime = 0.6f;
    public bool daytimeDead;

    Color iceColor = new Color(0.8f, 0.85f, 1f, 1);
    public SpriteRenderer frozenSprite;
    public int freezeRequirement = 5;
    public int freezeStacks = 0;
    public bool frozen = false;

    bool beenHitThisSwing = false;

    public ParticleSystem stunPs;
    Vector2 stunScale;

    public LineRenderer bindingChainLine;
    public enemyHealth boundDirectlyTo;
    public List<enemyHealth> boundTo = new List<enemyHealth>();
    public float boundTime;
    Color chainStartColor = new Color(1, 0.3f, 1);
    public bool bound;
    public GameObject soulBreakerHeartPrefab;
    SoulBreakerHeart currentHeart;

    private void Awake()
    {
        Light = GetComponentInChildren<Light2D>();

        if (Light)
        {
            if (gameManager.inSecondPhase)
            {
                Light.enabled = false;
            }
            else
            {
                float r = Light.pointLightOuterRadius;
                Light.pointLightOuterRadius = 0;
                DOTween.To(() => Light.pointLightOuterRadius, x => Light.pointLightOuterRadius = x, r, 1.2f);
            }
        }


    }

    void Start()
    {
        rb.simulated = true;

        if (gameManager.instance.difficulty > 0)
        {
            lootAmountRange.y -= 1;
            if (healthPerNight > 0)
                healthPerNight += 1;
        }
        if (!gameManager.inSecondPhase && !gameManager.instance.inArena && maxHealth > 0)
        {
            int currentNight = Mathf.Clamp(TimeManager.instance.dayNum, 0, 15);
            maxHealth += (healthPerNight * (TimeManager.instance.dayNum - 1));
        }
        currentHealth = maxHealth;


        if (frozenSprite)
            frozenSprite.color = new Color(1, 1, 1, 0);
        if (stunPs)
            stunScale = stunPs.transform.localScale;

    }

    public void TakeDamage(int damage, float knockback, float knockbackTime, Vector3 collisionPoint, bool fromPlayer, bool iceEffect, string itemUsed = "null", float itemEfficiencyMult = 1, bool stun = false, float minKnockback = -1, string damagetype = "default")
    {
        if (beingKnocked && !fromPlayer)
            return;
        if (fromPlayer && beenHitThisSwing)
            return;

        beingKnocked = true;

        boundTo.RemoveAll(e => e == null);
        foreach (enemyHealth e in boundTo)
        {
            if (e == this) continue;
            e.TakeDamage(damage, knockback, knockbackTime, collisionPoint, fromPlayer, iceEffect, itemUsed, itemEfficiencyMult, stun, minKnockback, damagetype);
        }
        if (bound) damage = Mathf.RoundToInt(damage * 1.5f);

        if (path)
            path.canMove = false;

        float knockbackToApply = knockback * knockbackScale;

        //negative knockback is from mush charm and negates negative enemy knockback scale
        if (knockback < 0)
        {
            if (knockbackScale <= 0)
                knockbackToApply = 2f;
            else
                knockbackToApply = Mathf.Abs(knockback * knockbackScale);
        }
        //for shield to still work on dementia mode with negative knockback
        if (minKnockback != -1) knockbackToApply = Mathf.Clamp(knockbackToApply, minKnockback, 7);


        int damageAmount = damage;
        //damage calculation before sending rpc
        if (itemUsed != "null" && damagetype != "jackpot")
        {
            switch (effectiveDamage)
            {
                case damageType.treeDamage:
                    damageAmount = Mathf.RoundToInt(gameManager.instance.itemDictionary[itemUsed].treeDamage * (float)itemEfficiencyMult);
                    break;
                case damageType.rockDamage:
                    damageAmount = Mathf.RoundToInt(gameManager.instance.itemDictionary[itemUsed].rockDamage * (float)itemEfficiencyMult);
                    break;

            }

            //variance
            if (damage != 0)
                damageAmount += Random.Range(-gameManager.instance.itemDictionary[itemUsed].damageVariance, gameManager.instance.itemDictionary[itemUsed].damageVariance + 1);
        }

        if (boss && damagetype == "jackpot" && damageAmount > 150)
        {
            damageAmount = 150;
        }

        if (fromPlayer && inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.durability > 0
            && !(gameManager.instance.itemDictionary[itemUsed].uniqueType == item.UniqueType.execution && maxHealth == 0))
        {
            HandMove.instance.PvpHit(currentHealth - damageAmount <= 0, maxHealth == 0, transform.position, bound, damagetype == "jackpot", (int)effectiveDamage);
            HandMove.instance.enemiesHitThisSwing.Add(this);
        }

        RPC_TakeDamage(damageAmount, knockbackToApply, knockbackTime, collisionPoint, iceEffect, itemUsed, (double)itemEfficiencyMult, damagetype, DataPersistanceManager.LocalActorIndex);

        if (stun && currentHealth - damageAmount > 0 && !stunned)
        {
            RPC_Stun();
        }

        if (damagetype == "soul breaker heart" && !currentHeart && soulBreakerHeartPrefab)
        {
            GameObject newHeart = Instantiate(soulBreakerHeartPrefab, transform.position + (player.instance.transform.position - transform.position).normalized * -3f, Quaternion.identity);
            currentHeart = newHeart.GetComponent<SoulBreakerHeart>();
            currentHeart.attachedObj = transform;
            currentHeart.attachedEnemy = this;
        }

        if (fromPlayer)
            StartCoroutine(_WaitUntilPlayerSwingFinish());

    }

    IEnumerator _WaitUntilPlayerSwingFinish()
    {
        beenHitThisSwing = true;
        while (handEvents.instance.hitboxEnabled)
        {
            yield return null;
        }
        beenHitThisSwing = false;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_Stun()
    {
        StartCoroutine(_Stun());
    }

    IEnumerator _Stun()
    {
        if (boss || dead) yield break;

        stunned = true;

        if (stunPs)
        {
            stunPs.transform.localScale = Vector2.zero;
            stunPs.Play();
            stunPs.transform.DOScale(stunScale, 0.1f);
        }

        if (anim)
            anim.SetBool("Walking", false);
        yield return new WaitForSeconds(0.7f);

        if (stunPs)
        {
            stunPs.transform.DOScale(Vector2.zero, 0.2f);
            yield return new WaitForSeconds(0.2f);
            stunPs.Stop();
        }

        stunned = false;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_TakeDamage(int damage, float knockback, float knockbackTime, Vector3 collisionPoint, bool iceEffect, string itemUsed, double itemEfficiencyMult, string damageType, int fromPlayerIndex)
    {
        if (sprite.isVisible || damageType == "jackpot" || damageType == "soul crit")
        {
            GameObject particles = hitParticles;
            TextMeshPro damageNumText = Instantiate(damageNum, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)), Quaternion.identity).GetComponent<TextMeshPro>();

            //soul breaker crit
            if (damageType == "soul crit" && itemUsed != "null" && gameManager.instance.itemDictionary[itemUsed].uniqueType == item.UniqueType.breaker)
            {
                particles = playerHealth.instance.soulBreakerParticles;
                damage = Mathf.RoundToInt(damage * item.soulBreakerCritDamageMult);
                damageNumText.enableAutoSizing = false;
                damageNumText.fontSize = 14;
            }


            if (itemUsed != "null")
            {
                damageNumText.color = gameManager.instance.itemDictionary[itemUsed].damageNumColor;

                //execution sword
                if (gameManager.instance.itemDictionary[itemUsed].uniqueType == item.UniqueType.execution)
                {
                    particles = playerHealth.instance.executeParticles;
                }
                //attune spear
                if (gameManager.instance.itemDictionary[itemUsed].uniqueType == item.UniqueType.attunement)
                {
                    particles = playerHealth.instance.attuneParticles;
                    if (currentHealth - damage <= 0)
                    {
                        particles = playerHealth.instance.attuneKillParticles;
                    }
                }
            }

            damageNumText.text = damage.ToString();

            if (damage < 0)
            {
                damageNumText.color = new Color(0.9f, 0, 0);
            }
            else if (damage == 0)
            {
                knockbackTime = 0;
                damageNumText.text = "MISS";
                damageNumText.color = new Color(0.6f, 0.6f, 0.6f);
                damageNumText.GetComponent<AudioSource>().Play();
            }

            if (damageType == "freeze chance" && Random.Range(0, 3) == 0)
            {
                damageType = "freeze instant";
            }

            if (damageType == "freeze" || (damageType == "freeze instant" && !frozen))
            {
                if (frozen) return;

                damageNumText.color = new Color(0, 0.75f, 1);
                particles = HandMove.instance.freezeGustPs;

                if (freezeRequirement > 0)
                {
                    if (damageType == "freeze" && Random.Range(0, 2) == 0)
                    {
                        freezeStacks++;
                    }
                    else if (damageType == "freeze instant" && freezeRequirement > 0)
                    {
                        freezeStacks = freezeRequirement;
                    }

                    if (freezeStacks >= freezeRequirement && HasStateAuthority)
                    {
                        RPC_Freeze();
                    }
                }

            }
            else
                if (frozen)
                {
                    frozen = false;
                    particles = HandMove.instance.freezeShatterPs;
                    damage = Mathf.RoundToInt(damage * 2);
                    damageNumText.text = damage.ToString();
                    damageNumText.enableAutoSizing = false;
                    damageNumText.fontSize = 15;
                }


            if (damageType == "jackpot")
            {
                damageNumText.text = "777";
                damageNumText.enableAutoSizing = false;
                damageNumText.fontSize = 16;
                damageNumText.color = new Color(0, 1, 0);

                Instantiate(HandMove.instance.jackpotHitPs, transform.position, Quaternion.identity);
            }

            Destroy(damageNumText.gameObject, 0.9f);

            //particles
            if (damage != 0)
            {
                if (!hitParticlesAccuratePosition)
                    Instantiate(particles, transform.position, Quaternion.identity);
                else
                {
                    Instantiate(particles, Colliders[0].ClosestPoint(collisionPoint), Quaternion.identity);
                    damageNumText.transform.position = Colliders[0].ClosestPoint(collisionPoint) + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
                }
            }

            if (enemy && enemy.enemyHand && enemy.enemyHand.pullingPlayer && enemy.enemyHand.grabbing)
            {
                StartCoroutine(enemy.enemyHand._CancelGrab());
            }

        }

        currentHealth -= damage;

        if(fromPlayerIndex == DataPersistanceManager.LocalActorIndex)
        {
            if(boss || bossHeart)
                gameManager.endScreenStats["asphodel"] += damage;
        }

        beingKnockedByOther = true;
        if (knockbackCo != null) StopCoroutine(knockbackCo);

        if (itemUsed != "null" && gameManager.instance.itemDictionary[itemUsed].uniqueType == item.UniqueType.profiteer) damageType = "profiteer";

        knockbackCo = StartCoroutine(_Knockback(knockback, knockbackTime, collisionPoint, iceEffect, damageType));


    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_Freeze()
    {
        StartCoroutine(_Frozen());
        freezeStacks = 0;
    }

    public IEnumerator _Knockback(float knockback, float knockbackTime, Vector3 collisionPoint, bool iceEffect, string damageType)
    {
        if (enemy && enemy.dungeonIndex > -1)
        {
            if (DungeonGenerator.instance.GetModifier("knockback") > 1 && knockback < 2)
                knockback = 2;

            knockback *= DungeonGenerator.instance.GetModifier("knockback", enemy.dungeonIndex);
        }

        if (!disableKnockback)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce((transform.position - collisionPoint).normalized * knockback, ForceMode2D.Impulse);
        }
        if (knockbackTime > 0)
        {
            StartCoroutine(_damageFlash());
            yield return new WaitForSeconds(4 * (knockbackTime / 5));
        }
        if (!disableKnockback)
            rb.linearVelocity = Vector2.zero;

        if (damageType == "wrath2")
        {
            GameObject expl = Instantiate(player.instance.fireExplMini, transform.position +
                new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f)), Quaternion.identity);
            expl.GetComponent<damagePlayer>().ignorePlayers = inventory.charmEffect == "wrath";
        }
        yield return new WaitForSeconds(knockbackTime / 5);

        if (!disableKnockback)
            rb.linearVelocity = Vector2.zero;

        if (currentHealth > 0 || maxHealth == 0)
        {
            if (path)
                path.canMove = true;

            beingKnocked = false;
            beingKnockedByOther = false;

            if (damageType == "wrath")
            {
                GameObject expl = Instantiate(player.instance.fireExplMini, transform.position, Quaternion.identity);
                expl.GetComponent<damagePlayer>().ignorePlayers = inventory.charmEffect == "wrath";
            }

            if (enemy)
            {
                if (iceEffect)
                {
                    iceEffectStacks++;

                    if (enemy.iceSpeedMultiplier == 1)
                    {
                        enemy.iceSpeedMultiplier = 0.6f;
                        sprite.DOColor(iceColor, 0.3f);
                    }
                    //already frozen - reset color after damage flash to frozen color
                    else
                    {
                        sprite.color = iceColor;
                    }

                    yield return new WaitForSeconds(2);

                    iceEffectStacks--;
                    if (currentHealth > 0 && iceEffectStacks == 0)
                    {
                        sprite.DOColor(new Color(1, 1, 1, startAlpha), 0.5f);
                        enemy.iceSpeedMultiplier = 1;
                    }
                }
                else if (enemy.iceSpeedMultiplier == 1)
                {
                    iceEffectStacks = 0;
                    sprite.color = new Color(1, 1, 1, startAlpha);
                }
            }

            StartCoroutine(_RefreshCollision());
        }
        else if (!dead && maxHealth > 0)
        {
            if (deathSound)
                deathSound.Play();

            if (damageType == "wrath" || damageType == "wrath2")
            {
                GameObject expl = Instantiate(player.instance.fireExpl, transform.position, Quaternion.identity);
                expl.GetComponent<damagePlayer>().ignorePlayers = inventory.charmEffect == "wrath";
            }

            if (view.HasStateAuthority)
            {
                RPC_Die();
                if (itemPrefab)
                    spawnDrops(damageType == "soul crit" || damageType == "profiteer");
            }
        }
    }

    IEnumerator _RefreshCollision()
    {
        foreach (Collider2D c in Colliders)
        {
            if (c.isTrigger)
            {
                c.enabled = false;
            }
        }
        yield return new WaitForFixedUpdate();
        foreach (Collider2D c in Colliders)
        {
            if (c.isTrigger)
            {
                c.enabled = true;
            }
        }
    }

    IEnumerator _Frozen()
    {
        if (!frozenSprite) yield break;

        frozenSprite.enabled = true;
        frozenSprite.color = new Color(1, 1, 1, 0);
        frozenSprite.DOFade(1, 0.5f);
        frozen = true;

        for (float t = 5; t > 0; t -= Time.deltaTime)
        {
            if (!frozen || dead) break;
            beingKnocked = false;
            sprite.color = iceColor;
            yield return null;
        }
        sprite.color = Color.white;
        frozen = false;
        frozenSprite.DOFade(0, 0.6f);

    }


    void FixedUpdate()
    {
        if (!SingletonRunner.runner || !SingletonRunner.runner.IsSharedModeMasterClient || EnemySpawner.instance.disableDequeue) return;

        //die at daytime
        if ((!disableKnockback && (!TimeManager.instance.isNight || player.instance.inCutscene) && !dead && (enemy && enemy.currentBiome != MapGenerator.biome.volcanic && enemy.currentBiome != MapGenerator.biome.vale) && !gameManager.inBoss)
            || (gameManager.inBoss && Boss.instance.health.currentHealth <= 0 && !boss && !gameManager.inSecondPhase)
            )
        {
            if (!dead && !daytimeDead && !dontDieAtDay)
            {
                daytimeDead = true;
                deathQueue.Enqueue(this);
            }
        }
        
        if(!beingKnocked && rb)
        {
            rb.linearVelocity = Vector2.zero;
        }

    }


    IEnumerator _die()
    {
        currentHealth = 0;
        dead = true;

        if (enemy)
        {
            if(enemy.inSlothRange)
                Enemy.enemiesInSlothRange--;
        }

        anim.SetBool("Dead", true);
        if (ambientSound)
            ambientSound.DOFade(0, 1);

        if (enemy && enemy.waterRipple)
        {
            enemy.waterRipple.GetComponentInChildren<SpriteRenderer>().DOFade(0, 1);
        }

        if (Light)
            DOTween.To(() => Light.pointLightOuterRadius, x => Light.pointLightOuterRadius = x, 0, 1f);

        if (path)
            path.enabled = false;

        foreach (Collider2D col in Colliders)
        {
            col.enabled = false;
        }

        if (enemy && enemy.mask) sprite.color = Color.white;

        if (boss)
        {
            StartCoroutine(boss._Die());
            yield break;
        }

        if (sprite.isVisible)
        {
            sprite.DOColor(new Color(1, 0, 0, 0), deathTime);
            if (shadowSprite) shadowSprite.DOColor(Color.clear, 0.5f);
        }
        else
        {
            sprite.color = new Color(1, 0, 0, 0);
            if (shadowSprite) shadowSprite.color = Color.clear;
        }
        yield return new WaitForSeconds(0.5f);

        if (enemy && enemy.mask) enemy.mask.enabled = false;

        yield return new WaitForSeconds(3);

        if (view.HasStateAuthority)
        {
            Runner.Despawn(view);
        }
    }
    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    public void RPC_Die()
    {
        if (bossHeart)
        {
            BossHeartDie();
            beingKnocked = false;
            beingKnockedByOther = false;
            return;
        }
        StartCoroutine(_die());
    }

    void BossHeartDie()
    {
        if (dead) return;

        bossHeartPhase++;
        deathSound.Play();

        sprite.transform.DOShakeScale(0.7f, 0.7f, 8);

        switch (bossHeartPhase)
        {
            case 1:
                effectiveDamage = damageType.treeDamage;
                maxHealth = Mathf.RoundToInt(maxHealth * 0.7f);
                currentHealth = maxHealth;
                sprite.sprite = altSprites[0];
                BossHealthBar.Instance.bossNameText.text = "WOOD HEART";
                break;
            case 2:
                effectiveDamage = damageType.damage;
                sprite.sprite = altSprites[1];
                maxHealth = Mathf.RoundToInt(maxHealth * 1.5f);
                currentHealth = maxHealth;
                BossHealthBar.Instance.bossNameText.text = "FINAL HEART";
                break;
            case 3:
                dead = true;
                StartCoroutine(BossInside.Instance._Die());
                break;
        }

    }

    IEnumerator _damageFlash()
    {
        camShake.shake(10);

        while ((beingKnocked || beingKnockedByOther) && currentHealth > 0)
        {
            sprite.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (iceEffectStacks == 0)
                sprite.color = new Color(1, 1, 1, startAlpha);
            else
                sprite.color = iceColor;
            yield return new WaitForSeconds(0.05f);

        }
    }

    void spawnDrops(bool extraDrops)
    {
        if (gameManager.inSecondPhase || (enemy && enemy.inDungeon) || (lootItem == null && uniqueLootItem == null)) return;

        //less drops in volcano
        if (enemy && MapGenerator.instance.islands.Find(i => i.center == enemy.currentIsland).biome == (int)MapGenerator.biome.volcanic && boss == null)
        {
            lootAmountRange -= new Vector2Int(1, 1);
        }

        if (lootAmountRange.x < 1) lootAmountRange.x = 1;

        if (playerSpawner.playerCountInGame == 1 && Random.Range(0, 2) == 1)
        {
            lootAmountRange.x++;
        }

        if (extraDrops)
        {
            lootAmountRange.y++;
            if (Random.Range(0, 2) == 1) lootAmountRange.x++;

            uniqueLootChance += 0.1f;
        }

        if (lootAmountRange.y > 0)
        {
            GameObject Item = Runner.Spawn(itemPrefab, itemSpawnPosition(), Quaternion.identity).gameObject;
            Item.GetComponent<itemPickup>().SetItem(lootItem.itemId, (short)Random.Range(lootAmountRange.x, lootAmountRange.y + 1), 1, new ItemData(0), true);
        }

        if (uniqueLootItem != null && Random.Range(0f, 1f) <= uniqueLootChance)
        {
            GameObject Item2 = Runner.Spawn(itemPrefab, itemSpawnPosition(), Quaternion.identity).gameObject;
            Item2.GetComponent<itemPickup>().SetItem(uniqueLootItem.itemId, 1, 1, new ItemData(0), true);
        }
        if (uniqueLootItems != null && uniqueLootItems.Length > 0 && Random.Range(0f, 1f) <= uniqueLootChance)
        {
            GameObject Item2 = Runner.Spawn(itemPrefab, itemSpawnPosition(), Quaternion.identity).gameObject;
            Item2.GetComponent<itemPickup>().SetItem(uniqueLootItems[Random.Range(0, uniqueLootItems.Length)].itemId, 1, 1, new ItemData(0), true);
        }

    }

    public Vector2 itemSpawnPosition()
    {
        return transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
    }

    public void SetBoundTo(enemyHealth other)
    {
        if (boundDirectlyTo == null && bindingChainLine != null && !dead && !other.dead)
        {
            boundDirectlyTo = other;
            StopCoroutine("_Bound");
            StartCoroutine(_Bound());
        }
    }

    IEnumerator _Bound()
    {
        bound = true;

        if (boundDirectlyTo != null)
            boundDirectlyTo.bound = true;

        boundTime = 5f;
        bindingChainLine.enabled = true;

        // Initial chain connect animation
        bindingChainLine.enabled = true;

        for (float t = 0; t < 0.15f; t += Time.deltaTime)
        {
            if (boundDirectlyTo == null || boundDirectlyTo.dead || dead)
                break;

            float lerp = t / 0.15f;

            Vector3 startPos = bindingChainLine.transform.position;
            Vector3 endPos2 = boundDirectlyTo.transform.position;
            Vector3 midPoint = (startPos + endPos2) * 0.5f;

            bindingChainLine.SetPosition(0, Vector3.Lerp(midPoint, startPos, lerp));
            bindingChainLine.SetPosition(1, Vector3.Lerp(midPoint, endPos2, lerp));

            yield return null;
        }

        // Ensure exact final positions
        if (boundDirectlyTo != null && !boundDirectlyTo.dead)
        {
            bindingChainLine.SetPosition(0, bindingChainLine.transform.position);
            bindingChainLine.SetPosition(1, boundDirectlyTo.transform.position);
        }

        while (boundTime >= 0)
        {
            if (dead || boundDirectlyTo == null || boundDirectlyTo.dead)
                break;

            bindingChainLine.SetPosition(0, bindingChainLine.transform.position);
            bindingChainLine.SetPosition(1, boundDirectlyTo.transform.position);

            boundTime -= Time.deltaTime;
            yield return null;
        }

        enemyHealth cachedTarget = boundDirectlyTo;
        boundDirectlyTo = null;

        Vector2 endPos;

        if (cachedTarget != null)
            endPos = cachedTarget.transform.position;
        else
            endPos = bindingChainLine.GetPosition(1);

        bindingChainLine.DOColor(
            new Color2(chainStartColor, chainStartColor),
            new Color2(
                new Color(1, 0.3f, 1, 0),
                new Color(1, 0.3f, 1, 0)),
            0.5f);

        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            float lerp = t / 0.5f;

            bindingChainLine.SetPosition(0, transform.position);
            bindingChainLine.SetPosition(1, Vector3.Lerp(endPos, transform.position, lerp));

            yield return null;
        }

        bindingChainLine.enabled = false;

        boundTo.Clear();

        boundDirectlyTo = null;
        bound = false;
    }

}

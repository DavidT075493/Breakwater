using UnityEngine;

public class damagePlayer : MonoBehaviour
{
    public bool active = true;
    public int damage = 4;
    public int overrideHardDamage;
    public float knockback = 5;
    public float minimumEnemyKnockback = -1;
    public float knockbackTime = 0.2f;
    public bool damageEnemy = true;
    public float damageEnemyMultiplier = 1;
    public bool fromPlayer = false;
    public bool hitsFlying;
    [SerializeField] Projectile projectile;
    public string deathMessage = "died";
    public bool iceEffect;
    public string damageType = "";
    public bool playerCollisionOnly;
    public bool canHitPlayersOnBoat;
    public bool stun;
    public bool ignorePlayers;
    public bool continuousCollision;

    private void Start()
    {
        //prevent other player weapon hitboxes from damaging enemies
        player p = GetComponentInParent<player>();
        if (p)
        {
            damageEnemy = p.view.HasInputAuthority;
        }

        if (gameManager.instance.difficulty > 0 && overrideHardDamage > 0)
        {
            damage = overrideHardDamage;
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!active || continuousCollision) return;

        OnCollide(collision);
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!active || !continuousCollision) return;

        OnCollide(collision);
    }

    void OnCollide(Collider2D collision)
    {
        if (collision.CompareTag("Main Player") && !fromPlayer && !ignorePlayers && !player.instance.inCutscene)
        {
            if (playerCollisionOnly && collision.isTrigger) return;

            int fromBoatFloor = -1;
            if (canHitPlayersOnBoat)
                fromBoatFloor = -2;

            playerHealth h = collision.GetComponent<playerHealth>();

            if (damageType == "freeze")
            {
                h.iceEffectTime = 1;
                return;
            }

            Vector2 attackerPos = transform.position;
            if (projectile) attackerPos = projectile.owner.position;

            h.hurtPlayer(Mathf.RoundToInt(damage), knockback, knockbackTime, transform.position, deathMessage, false, iceEffect, fromBoatFloor, "null",attackerPos);

            //destroy on hit for projectiles
            if (projectile)
            {
                projectile.Destroy();
            }
        }
        else if (damageEnemy)
        {
            enemyHealth e = collision.GetComponent<enemyHealth>();
            if (e
                && !(e.flying && !(fromPlayer || hitsFlying))
                && !(transform.parent && transform.parent.gameObject == e.gameObject)
                && !(damageType != "" && e.ignoreDamageType == damageType)
                && !(e.frozen && damageType == "freeze")
                )
            {
                //destroy on hit for projectiles
                if (projectile)
                {
                    projectile.Destroy();
                }

                if (!fromPlayer)
                {
                    e.TakeDamage(Mathf.RoundToInt(damage * damageEnemyMultiplier), knockback, knockbackTime, transform.position, fromPlayer, iceEffect, "null", 1, stun, minimumEnemyKnockback, damageType);
                }

                //player deal damage to enemies
                else
                {
                    string Item = "null";
                    if (inventorySlot.equippedSlot.itemInSlot != null)
                    {
                        Item = inventorySlot.equippedSlot.itemInSlot.itemId;

                        if (inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.jackpot)
                        {

                            //jackpot
                            if (gameManager.PercentChance(HandMove.jackpotChance) 
                                || (HandMove.instance.wagerLevel > 0 && gameManager.PercentChance(HandMove.instance.jackpotAdditionalChance)))
                            {
                                e.TakeDamage(Mathf.RoundToInt(300), this.knockback, knockbackTime, transform.position, fromPlayer, true, Item, 1 + (inventory.instance.statAmounts[0] / 10f * inventory.instance.maxMiningMult), true, minimumEnemyKnockback, "jackpot");
                                return;
                            }

                        }

                    }

                    float knockback = this.knockback;
                    //extra mush knock
                    if (inventory.charmEffect == "mush")
                        knockback = this.knockback * -1.5f;

                    //miss chance
                    float damageDealt = damage;
                    if(Random.Range(0f,1f) < inventory.instance.GetModifierInToolSlot("miss") - 1)
                    {
                        damageDealt = 0;
                    }
                    damageDealt *= inventory.instance.GetModifierInToolSlot("attack");
                    bool stunChance = Random.Range(0f,1f) < inventory.instance.GetModifierInToolSlot("stun") - 1;

                    bool iceEffect = (inventorySlot.equippedSlot.itemInSlot != null && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.ice);

                    string damageType = "default";
                    if (inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.breaker
                        && inventorySlot.equippedSlot.itemData.tier >= 4)
                        damageType = "soul breaker heart";
                    if (inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.ice
                        && inventorySlot.equippedSlot.itemData.tier >= 4)
                        damageType = "freeze chance";
                    if (HandMove.instance.soulBreakerAwakened) 
                        damageType = "soul crit";

                    e.TakeDamage(Mathf.RoundToInt(Mathf.RoundToInt(damageDealt * damageEnemyMultiplier) * player.instance.strengthMult), knockback, knockbackTime, transform.position, fromPlayer, iceEffect, Item, 1 + (inventory.instance.statAmounts[0] / 10f * inventory.instance.maxMiningMult),
                        stun || stunChance, minimumEnemyKnockback, damageType);
                }
            }

        }
    }

    //cactus farm thing
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Main Player") && !fromPlayer && playerCollisionOnly)
        {
            int fromBoatFloor = -1;
            if (canHitPlayersOnBoat)
                fromBoatFloor = -2;

            Vector2 point = Vector2.zero;
            foreach (var p in collision.contacts)
            {
                point += p.point;
            }
            point /= collision.contactCount;

            collision.gameObject.GetComponent<playerHealth>().hurtPlayer(damage, knockback, knockbackTime, point, deathMessage, false, iceEffect, fromBoatFloor, "null",point);

            //destroy on hit for projectiles
            if (projectile)
            {
                projectile.Destroy();
            }
        }
    }

}

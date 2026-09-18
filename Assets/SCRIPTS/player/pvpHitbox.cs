using UnityEngine;

public class pvpHitbox : MonoBehaviour
{
    public HandMove handMove;
    bool alreadyHit;
    player playerParent;

    public bool overrideItemInfo;
    [SerializeField] int overrideDamage = 20;
    [SerializeField] float overrideKnockback, overrideKnockbackTime;
    public string deathMessage = "was kiled by";

    private void OnEnable()
    {
        alreadyHit = false;
    }

    private void Awake()
    {
        playerParent = GetComponentInParent<player>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.CompareTag("Main Player") && (!alreadyHit || overrideItemInfo) && collision.gameObject == player.instance.gameObject)
        {
            item currentItem = handMove.selectedItem;

            if (currentItem)
            {
                float damageMult = 1;
                if (handMove.inCleave) damageMult *= inventory.cleaveDamageMultiplier;

                if (playerParent.prideActive) damageMult *= player.prideMult;
                damageMult *= (1 / DungeonGenerator.instance.GetModifier("damage dealt"));
                damageMult *= inventory.instance.GetModifierInToolSlot("attack");

                if (currentItem.twoHanded)
                {
                    damageMult *= (1 / DungeonGenerator.instance.GetModifier("two hand damage"));
                }

                //normal items
                bool actuallyHit = false;
                if (!overrideItemInfo)
                {

                    if (currentItem.uniqueType == item.UniqueType.jackpot)
                    {
                        //jackpot
                        if (gameManager.PercentChance(HandMove.jackpotChance))
                        {
                            deathMessage = "lost it all against /p1";
                            deathMessage = deathMessage.Replace("/p1",
                                "<color=#" + ColorUtility.ToHtmlStringRGBA(handMove.playerParent.usernameText.color) + ">" + handMove.playerParent.usernameText.text + "</color>");
                            deathMessage = deathMessage.Replace("/item", currentItem.displayName);


                            actuallyHit = playerHealth.instance.hurtPlayer(
                                Mathf.RoundToInt(300),
                                currentItem.knockback * 2,
                                currentItem.knockbackTime,
                                handMove.transform.position,
                                deathMessage,
                                true,
                                true,
                                playerParent.boatFloor,
                                currentItem.itemId,
                                playerParent.transform.position,
                                true
                                );

                            handMove.ReceivePvpHit(handMove.view.InputAuthority.PlayerId, collision.GetComponent<playerHealth>().currentHealth <= 0, collision.transform.position, false, true);

                            return;
                        

                        }

                    }

                    deathMessage = currentItem.deathMessage;
                    deathMessage = deathMessage.Replace("/p1",
                        "<color=#" + ColorUtility.ToHtmlStringRGBA(handMove.playerParent.usernameText.color) + ">" + handMove.playerParent.usernameText.text + "</color>");
                    deathMessage = deathMessage.Replace("/item", currentItem.displayName);

                    actuallyHit = playerHealth.instance.hurtPlayer(
                    Mathf.RoundToInt(currentItem.damage * damageMult * handMove.playerParent.strengthMult),
                    currentItem.knockback,
                    currentItem.knockbackTime,
                    handMove.transform.position,
                    deathMessage,
                    true,
                    currentItem.uniqueType == item.UniqueType.ice, 
                    playerParent.boatFloor
                    , currentItem.itemId
                    , playerParent.transform.position
                    , false 
                    , currentItem.uniqueType == item.UniqueType.breaker && handMove.soulBreakerAwakened
                    );

                }
                //shield stuff
                else
                {
                    actuallyHit = playerHealth.instance.hurtPlayer(
                    Mathf.RoundToInt(overrideDamage * damageMult * handMove.playerParent.strengthMult),
                    overrideKnockback,
                    overrideKnockbackTime,
                    handMove.transform.position,
                    "was killed by <color=#" + ColorUtility.ToHtmlStringRGBA(handMove.playerParent.usernameText.color) + ">" +
                    handMove.playerParent.usernameText.text +
                    "</color> using " + currentItem.displayName,
                    true,
                    currentItem.uniqueType == item.UniqueType.ice, playerParent.boatFloor
                    , currentItem.itemId
                    , playerParent.transform.position
                    );
                }

                if(actuallyHit)
                    handMove.ReceivePvpHit(handMove.view.InputAuthority.PlayerId, collision.GetComponent<playerHealth>().currentHealth <= 0, collision.transform.position, false, false);

                alreadyHit = true;
            }
            else
            {
                playerHealth.instance.hurtPlayer(
                Mathf.RoundToInt(3 * handMove.playerParent.strengthMult),
                4,
                0.1f,
                handMove.transform.position,
                "was fisted by <color=#" + ColorUtility.ToHtmlStringRGBA(handMove.playerParent.usernameText.color) + ">" +
                handMove.playerParent.usernameText.text +
                "</color>",
                true,
                false, playerParent.boatFloor
                , "null"
                , playerParent.transform.position
                );

                alreadyHit = true;
            }
        }


    }
}

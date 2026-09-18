using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class treeChop : MonoBehaviour
{
    public static short lastId;

    public bool infiniteHealth;
    public int startHp = 50;
    public int currentHp;
    public int stumpStartHp = 60;
    public int stumpHp;

    public Animation fallDownAnim;
    public GameObject hitEffect;
    public Vector3 hitEffectPos;
    public GameObject healthBar;
    public healthBar healthBarScript;

    public GameObject itemPrefab;
    public Transform spawnPoints;
    public LootItem[] lootItems;
    public item rareItemDrop;
    public float rareDropChance = 0;

    public Vector2 spawnPointsScale;
    public float itemSpawnDelay = 0.583f;
    [HideInInspector]
    public bool dead;

    public float maxCamShake = 0.6f;
    public camShakeController camShake;

    public float hitCooldown = 0.3f;
    bool invincible;
    public short treeId;
    [Tooltip("0 = tree, 1 = ore, 2 = other (bush)")]
    public short featureType;

    public int requiredToolTier;
    public bool hasStump = true;

    [HideInInspector] public GameObject attachedItem;
    public SpriteRenderer[] masks;
    [SerializeField] Light2D light2d;

    public int onIsland;
    Collider2D[] colliders;
    public Transform shakeOrigin;
    public float ShakeStrength = 0.1f;
    private bool statChanged;

    private void Awake()
    {
        currentHp = startHp;
        if (healthBar)
            healthBar?.SetActive(false);
        //view = gameObject.GetPhotonView();

        treeId = lastId;
        lastId++;

        colliders = GetComponents<Collider2D>();
    }

    private void OnEnable()
    {
        if (currentHp < 0) healthBar?.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!invincible && (currentHp > 0 || stumpHp > 0) && collision.CompareTag("Weapon Hitbox") && player.instance.boatFloor <= 0 && collision.isTrigger)
        {
            StartCoroutine(_hitTimer());

            int dealDamage = 2;
            if (inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemInSlot.toolTier >= requiredToolTier)
            {
                bool correctTool = false;

                switch (featureType)
                {
                    case 0:
                        dealDamage = inventorySlot.equippedSlot.itemInSlot.treeDamage;

                        if (inventorySlot.equippedSlot.itemInSlot.treeDamage * 1.5f > inventorySlot.equippedSlot.itemInSlot.rockDamage
                            && inventorySlot.equippedSlot.itemInSlot.treeDamage * 1.5f > inventorySlot.equippedSlot.itemInSlot.damage)
                            correctTool = true;

                        if (inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.profiteer && inventorySlot.equippedSlot.itemData.tier >= 4)
                            HandMove.instance.profiteerState = 0;

                        break;
                    case 1:
                        dealDamage = inventorySlot.equippedSlot.itemInSlot.rockDamage;

                        if (inventorySlot.equippedSlot.itemInSlot.rockDamage > inventorySlot.equippedSlot.itemInSlot.treeDamage
                            && inventorySlot.equippedSlot.itemInSlot.rockDamage > inventorySlot.equippedSlot.itemInSlot.damage)
                            correctTool = true;

                        if (inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.profiteer && inventorySlot.equippedSlot.itemData.tier >= 4)
                            HandMove.instance.profiteerState = 1;

                        break;
                    case 2:
                        dealDamage = inventorySlot.equippedSlot.itemInSlot.treeDamage;

                        if (inventorySlot.equippedSlot.itemInSlot.treeDamage * 1.5f > inventorySlot.equippedSlot.itemInSlot.rockDamage
                            && inventorySlot.equippedSlot.itemInSlot.treeDamage * 1.5f > inventorySlot.equippedSlot.itemInSlot.damage)
                            correctTool = true;

                        break;
                }

                if (HandMove.instance.inCleave) dealDamage = Mathf.RoundToInt(dealDamage * inventory.cleaveDamageMultiplier);

                dealDamage = Mathf.RoundToInt(dealDamage * (1 + (inventory.instance.GetStatAmount(0) / 10f * inventory.instance.maxMiningMult))
                    * inventory.instance.GetModifierInToolSlot("mining"));


                treeRPCs.Instance.HitTreeAtPos(dealDamage, inventorySlot.equippedSlot.itemInSlot.itemId, Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), featureType, correctTool
                    , inventory.instance.GetModifierInToolSlot("loot"));

                if (inventorySlot.equippedSlot.itemInSlot.usesDurability && inventorySlot.equippedSlot.itemInSlot.uniqueType != item.UniqueType.gavel && inventorySlot.equippedSlot.itemInSlot.uniqueType != item.UniqueType.execution)
                    HandMove.instance.ChangeDurability(-1, inventorySlot.equippedSlot);

                if (inventorySlot.equippedSlot.itemInSlot)
                {
                    //drop chance
                    if (Random.Range(0f, 1f) < inventory.instance.GetModifierInToolSlot("drop") - 1)
                    {
                        inventory.instance.DropItem(1);
                        HandMove.instance.animator.SetTrigger("Reset Swing");
                    }

                    //fahrenheit ability
                    if (inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.smelt && inventorySlot.equippedSlot.itemData.tier >= 4
                        && correctTool)
                    {
                        GameObject expl = Instantiate(player.instance.fahrenheitExpl, transform.position, Quaternion.identity);
                        expl.GetComponent<damagePlayer>().ignorePlayers = true;
                    }

                }

            }
            else
            {
                //view.RPC("RPC_hit", RpcTarget.All, (short)0);
                treeRPCs.Instance.HitTreeAtPos(dealDamage, "null", Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), featureType, false, 1);
            }

        }
        else if (collision.CompareTag("Boss") && !collision.isTrigger && SingletonRunner.runner.IsSharedModeMasterClient)
        {
            treeRPCs.Instance.HitTreeAtPos(2000, "null", Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), featureType, true, 1);
        }
        else if (collision.CompareTag("Fahrenheit Explosion") && !invincible && collision.GetComponent<damagePlayer>().ignorePlayers)
        {
            treeRPCs.Instance.HitTreeAtPos(15, "null", Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), featureType, true, 1);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Boss") && !collision.collider.isTrigger && SingletonRunner.runner.IsSharedModeMasterClient && !hasStump)
        {
            treeRPCs.Instance.HitTreeAtPos(2000, "null", Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), featureType, true, 0);
        }
    }

    /*
    [PunRPC]
    public void RPC_flipScale()
    {
        transform.localScale = new Vector2(-1, 1);
        if (healthBar)
            healthBar.transform.parent.localScale = new Vector2(-1, 1);
    }
    */

    public void flipScale()
    {
        transform.localScale = new Vector2(-1, 1);
        if (healthBar)
            healthBar.transform.parent.localScale = new Vector2(-1, 1);
    }


    //uses tree rpc manager
    public void Hit(int damage, string itemIndex, bool correctTool, float extraDropsMult = 1)
    {
        camShake.shake(maxCamShake);
        float shakeStrength = correctTool ? ShakeStrength : ShakeStrength / 1.8f;
        float shakeTime = correctTool ? 0.55f : 0.3f;

        shakeOrigin?.DOShakePosition(shakeTime, new Vector2(shakeStrength, 0));
        shakeOrigin?.DOShakeRotation(shakeTime, shakeStrength * 38);

        if (!infiniteHealth)
        {
            if (currentHp > 0)
            {
                currentHp -= damage;
            }
            //tree stump damage
            else if (stumpHp > 0 && hasStump)
            {
                stumpHp -= damage;
            }

            UpdateHealth();
        }
        Instantiate(hitEffect, transform.position + hitEffectPos, Quaternion.identity);

        if (currentHp <= 0 && !dead && !infiniteHealth)
        {
            dead = true;
            currentHp = 0;
            stumpHp = stumpStartHp;
            //invincible = false;
            if (SingletonRunner.runner.IsSharedModeMasterClient)
            {
                float dropsMultiplier = 1;
                if (gameManager.instance.itemDictionary[itemIndex] && gameManager.instance.itemDictionary[itemIndex].uniqueType == item.UniqueType.profiteer && correctTool)
                    dropsMultiplier = 1.5f;

                dropsMultiplier *= extraDropsMult;

                treeRPCs.Instance.TreeDie(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), featureType, (gameManager.instance.itemDictionary[itemIndex] != null && gameManager.instance.itemDictionary[itemIndex].uniqueType == item.UniqueType.smelt), dropsMultiplier);
            }
        }
        //destroy stump
        if (currentHp <= 0 && stumpHp <= 0 && hasStump)
        {
            gameObject.SetActive(false);
        }
    }

    public void UpdateHealth(bool instant = false)
    {
        if (healthBar)
        {
            healthBar.SetActive(true);
            healthBarScript.UpdateHealth((float)currentHp / (float)startHp, instant);
        }
    }

    /*
    [PunRPC]
    private void RPC_DestroyStump()
    {
        if (gameObject)
            Runner.Despawn(gameObject);
    }
    */

    IEnumerator _hitTimer()
    {
        invincible = true;
        yield return new WaitForSeconds(hitCooldown);
        invincible = false;

        if (dead && !statChanged)
        {
            statChanged = true;

            if (featureType == 0)
            {
                gameManager.endScreenStats["trees"]++;
            }
            if (featureType == 1)
            {
                gameManager.endScreenStats["ores"]++;
            }
        }
    }


    public IEnumerator _Die(bool instant = false, bool disableItems = false, bool smelt = false, float dropsMultiplier = 1)
    {

        fallDownAnim?.Play();

        //skip to the end of the tree fall animation
        if (instant) fallDownAnim[fallDownAnim.clip.name].time = fallDownAnim[fallDownAnim.clip.name].length;

        healthBarScript?.hide(instant);

        //disable rock collider
        if (!hasStump)
        {
            foreach (Collider2D c in colliders)
                c.enabled = false;
        }

        if (attachedItem)
        {
            attachedItem.transform.SetParent(featurePlacer.Instance.itemHolder);
            //sets sprite back to normal for berries
            attachedItem.GetComponent<itemPickup>().SetSprite(false);
        }

        foreach (SpriteRenderer mask in masks) mask.enabled = false;

        if (SingletonRunner.runner.IsSharedModeMasterClient && !disableItems)
        {
            yield return new WaitForSeconds(itemSpawnDelay);

            if (itemPrefab)
                spawnDrops(smelt, dropsMultiplier);
        }

        if (light2d)
        {
            light2d.enabled = false;
        }

        if (hasStump)
        {
            yield return new WaitForSeconds(2);
            Destroy(healthBar);
        }

    }


    void spawnDrops(bool smelt, float dropsMultiplier)
    {
        foreach (LootItem l in lootItems)
        {
            for (int i = Mathf.RoundToInt(Random.Range(l.amountRange.x, l.amountRange.y + 1) * dropsMultiplier); i > 0; i--)
            {
                item it = l.itemType[Random.Range(0, l.itemType.Length)];

                GameObject Item = SingletonRunner.runner.Spawn(itemPrefab, itemSpawnPosition(), Quaternion.identity).gameObject;
                if (!smelt || (it.smeltResult == null || it.onlyInPurifier))
                {
                    Item.GetComponent<itemPickup>().SetItem(it.itemId, 1, 1, new ItemData(0));
                }
                else
                {
                    Item.GetComponent<itemPickup>().SetItem(it.smeltResult.itemId, 1, 1, new ItemData(0));
                }
            }
        }

        if (rareItemDrop && Random.Range(0f, 1f) < rareDropChance)
        {
            GameObject Item = SingletonRunner.runner.Spawn(itemPrefab, itemSpawnPosition(), Quaternion.identity).gameObject;
            Item.GetComponent<itemPickup>().SetItem(rareItemDrop.itemId, 1, 1, new ItemData(0));
        }


    }

    public Vector2 itemSpawnPosition()
    {
        return spawnPoints.position + new Vector3(Random.Range(-spawnPointsScale.x / 2, spawnPointsScale.x / 2), Random.Range(-spawnPointsScale.y, spawnPointsScale.y));
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.position + hitEffectPos, 0.2f);
        Gizmos.color = Color.magenta;
        if (spawnPoints)
            Gizmos.DrawWireCube(spawnPoints.position, spawnPointsScale);
    }

}
[System.Serializable]
public class LootItem
{
    public item[] itemType = new item[1];
    [Tooltip("Inclusive")]
    public Vector2Int amountRange;
}

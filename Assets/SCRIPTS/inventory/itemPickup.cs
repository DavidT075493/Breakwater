using DG.Tweening;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class itemPickup : NetworkBehaviour
{
    public item Item;
    [SerializeField] bool isTestItem;
    public int count = 1;
    public int durability = 1;
    public ItemData itemData;
    public bool randomCount;
    public int maxCount = 2;

    public Boat boatParent;

    public SpriteRenderer sprite, sprite2;
    public bool playerInRange;
    public Vector2 indicatorOffset;
    public GameObject indicatorPrefab, fullIndicatorPrefab;
    public GameObject indicatorInstance;
    Animator indicatorAnim;

    public float distFromPlayer;
    public Rigidbody2D rb;

    public static List<itemPickup> itemsInRange = new List<itemPickup>();
    public static Transform closestItem;

    const float pickupDelay = 0.4f;

    public NetworkObject view;

    public Collider2D pickupHitbox;
    public bool disableCollection;

    public bool naturallySpawned;
    public int naturalItemID;

    public itemMerging Merge;
    [SerializeField] bool disableOutline;

    public bool costsSouls;
    public int soulCost = 30;
    public item soulItem;
    [SerializeField] AudioSource customPickupSound, radarSound;
    public bool farmGrownItem;
    public TileEntity farmCropTile;

    [SerializeField] Material unlitOutlineMat;
    bool despawning;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public bool dontSave;
    private bool inventoryFull;
    public Collider2D solidCollider;
    public int boatFloor;

    public Vector2 localMotion;
    public int dungeonItemId = -1;

    private void Start()
    {
        sprite.sprite = null;

        if (!naturallySpawned)
        {
            disableCollection = true;
            Invoke("FinishPickupDelay", pickupDelay);
        }
        SetSprite(naturallySpawned);

        if (Item && durability == -1) durability = Item.maxDurability;

        if (!disableOutline)
        {
            sprite.material.color = new Color(sprite.material.color.r, sprite.material.color.g, sprite.material.color.b, 0);
            sprite2.material.color = new Color(sprite.material.color.r, sprite.material.color.g, sprite.material.color.b, 0);
        }

    }


    public void SetSprite(bool naturalSpawning = false)
    {
        if (Item)
        {
            sprite.sprite = Item.overworldSprite;

            if (Item.unlitWhenDropped)
            {
                sprite.material = unlitOutlineMat;
                if (sprite2)
                    sprite2.material = unlitOutlineMat;
            }

            if (naturalSpawning)
            {
                //dont make thorn hook natural spawn sprite
                if (Item.naturalSpawnSprite && !costsSouls)
                {
                    sprite.sprite = Item.naturalSpawnSprite;
                }

                if (Item.naturalSpawnInvisible) sprite.sprite = null;

                //berry bush stuff
                if (Item.spawnWithGameObj && !farmGrownItem)
                {
                    GameObject newObj = Instantiate(Item.spawnWithGameObj, new Vector3(transform.position.x, transform.position.y, 0), Quaternion.identity, featurePlacer.Instance.itemHolder);
                    transform.SetParent(newObj.transform.GetChild(0), true);
                    treeChop chop = newObj.GetComponent<treeChop>();
                    treeRPCs.Instance.otherDamagables.Add(chop);
                    chop.attachedItem = gameObject;
                    //sorting fix for berries
                    transform.position -= new Vector3(0, 0.005f, 0);

                }

                sprite.sortingLayerName = Item.spawnSortLayerName;
                sprite.sortingOrder = Item.spawnSortOrder;
                if (sprite2)
                {
                    sprite2.sortingLayerName = Item.spawnSortLayerName;
                    sprite2.sortingOrder = Item.spawnSortOrder;
                }

                if (Item.naturalSpawnFlipX)
                {
                    sprite.flipX = Random.Range(0, 2) == 1;
                    sprite2.flipX = sprite.flipX;
                }
            }
            else if (!boatParent)
            {
                transform.SetParent(gameManager.instance.itemsHolder);
            }

            sprite.transform.localScale = new Vector2(1f, 1f);

            if (randomCount)
            {
                count = Random.Range(count, maxCount);
            }

            if (count > 1 && !(naturallySpawned && Item.naturalSpawnSprite))
            {
                sprite2.sprite = Item.overworldSprite;

                sprite2.transform.localScale = new Vector2(1f, 1f);
                sprite2.transform.localPosition = new Vector2(0.08333334f, -0.08333334f);

            }
            else if (sprite2)
            {
                sprite2.sprite = null;
            }

        }
    }

    bool InventoryFull()
    {
        if (player.instance.currentIsland.biome == (int)MapGenerator.biome.vale) return false;

        int firstFreeIndex = -1;

        for (int i = inventory.instance.slots.Count() - 1; i >= 0; i--)
        {
            if (inventory.instance.slots[i].itemInSlot == null
                || (inventory.instance.slots[i].itemInSlot == Item && inventory.instance.slots[i].itemCountInSlot < (Item.maxStackSize)))
            {
                firstFreeIndex = i;
            }
        }
        return firstFreeIndex == -1;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Main Player"))
        {
            if (!disableCollection && !Boss.inRangeOfSos
                && boatParent == player.instance.currentBoat
                && (!player.instance.currentBoat || boatFloor == player.instance.boatFloor))
            {
                if (!playerInRange || inventoryFull != InventoryFull())
                {
                    if (closestItem == transform)
                    {
                        inventoryFull = InventoryFull();

                        spawnIndicator();
                        playerInRange = true;

                        if (!disableOutline)
                        {
                            sprite.material.DOFade(1, 0.2f);
                            sprite2.material.DOFade(1, 0.2f);
                        }
                        //no other items should be able to be picked up
                        foreach (itemPickup item in itemsInRange)
                        {
                            if (item != this)
                                item.playerInRange = false;
                        }

                    }

                    if (!itemsInRange.Contains(this))
                    {
                        itemsInRange.Add(this);
                    }

                    closestItem = player.instance.GetClosestItem(itemsInRange.ToArray());

                    if (closestItem != transform || (Merge && Merge.isMerging))
                    {
                        destroyIndicator();
                        playerInRange = false;

                        if (!disableOutline)
                        {
                            sprite.material.DOFade(0, 0.2f);
                            sprite2.material.DOFade(0, 0.2f);
                        }
                    }
                }

                //collect item if pickupPressTimer > 0
                if (playerInRange && !disableCollection && !(Merge && Merge.isMerging) && (gameManager.instance.pickupPressTimer > 0 || menu.instance.testEPress) && !inventory.instance.inventoryOpen && !Chat.Instance.chatOpen
                    && !pauseScreen.instance.paused && !Boss.inRangeOfSos && !gameManager.instance.writingSign && !player.instance.pilotingBoat && !player.instance.inBoatAltAction && !menu.instance.mapMaximized)
                {

                    if (inventory.instance.gameObject.activeSelf)
                    {
                        Dictionary<item, int> itemCounts = inventory.instance.GetItemCounts(false);
                        if (!(costsSouls && (!itemCounts.ContainsKey(soulItem) || itemCounts[soulItem] < soulCost)))
                            Collect();
                    }
                    //vale collect
                    else
                    {
                        int soulCount = 0;
                        if (costsSouls)
                        {
                            foreach (inventorySlot slot in ValeManager.instance.valeInventoryItems)
                            {
                                if (slot.itemInSlot == soulItem)
                                {
                                    soulCount += slot.itemCountInSlot;
                                }
                            }
                        }

                        if (!(costsSouls && soulCount < soulCost))
                        {
                            Collect();
                        }

                    }
                    gameManager.instance.pickupPressTimer = 0;
                    menu.instance.testEPress = false;
                }

            }
            else if (indicatorInstance)
            {
                playerInRange = false;
                destroyIndicator();
            }
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Campfire") && !despawning && !(Item && Item.uniqueType != item.UniqueType.none))
        {
            Instantiate(HandMove.instance.destroyEffect, collision.transform.position, Quaternion.identity);

            if (SingletonRunner.runner.IsSharedModeMasterClient)
                RPC_Despawn();
            despawning = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Main Player"))
        {
            playerInRange = false;

            closestItem = null;

            if (itemsInRange.Contains(this))
            {
                itemsInRange.Remove(this);
            }

            if (!disableOutline)
            {
                sprite.material.DOFade(0, 0.2f);
                sprite2.material.DOFade(0, 0.2f);
            }
            destroyIndicator();
        }




    }



    void spawnIndicator()
    {
        if (indicatorInstance == null)
        {
            if (!InventoryFull())
            {
                indicatorInstance = Instantiate(indicatorPrefab, transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
                if (costsSouls)
                {
                    indicatorInstance.GetComponent<TextMeshPro>().text = "Unlock for " + soulCost + " souls";
                }
            }
            else
            {
                indicatorInstance = Instantiate(fullIndicatorPrefab, transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
            }
            indicatorAnim = indicatorInstance.GetComponent<Animator>();

        }
        else
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);

            if (!InventoryFull())
            {
                indicatorInstance = Instantiate(indicatorPrefab, transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);

                if (costsSouls)
                {
                    indicatorInstance.GetComponent<TextMeshPro>().text = "Unlock for " + soulCost + " souls";
                }
            }
            else
            {
                indicatorInstance = Instantiate(fullIndicatorPrefab, transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
            }
            indicatorAnim = indicatorInstance.GetComponent<Animator>();
        }
    }

    public void destroyIndicator()
    {
        if (indicatorInstance != null)
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);
        }

    }


    void FinishPickupDelay()
    {
        disableCollection = false;
    }

    void Collect()
    {
        if (InventoryFull())
        {
            spawnIndicator();
            return;
        }
        sprite.enabled = false;
        if (sprite2)
            sprite2.enabled = false;

        disableCollection = true;

        if (indicatorInstance)
        {
            destroyIndicator();
        }

        //more food with bountiful harvest
        if ((naturallySpawned || farmCropTile) && Item.type == item.itemType.food && inventory.charmEffect == "harvest")
        {
            count++;
        }

        if (view)
        {
            RPC_RequestCollected(SingletonRunner.runner.LocalPlayer);
        }
        else
        {
            treeRPCs.Instance.RPC_RequestCollectItem(naturalItemID, player.instance.view.Id);
        }

        inventory.instance.pickupSound.Play();


    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestCollected(PlayerRef playerId)
    {
        RPC_Collected(playerId);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_Collected(PlayerRef playerId)
    {

        if (!pickupHitbox.enabled || (Merge && Merge.isMerging))
        {
            //cancel collection prolly

            sprite.enabled = true;
            sprite2.enabled = count > 1;
            disableCollection = false;

            Merge.isMerging = false;
            return;
        }
        pickupHitbox.enabled = false;


        if (SingletonRunner.runner && farmCropTile && Item.resetGrowthWhenCollected)
        {
            farmCropTile.SetGrowthStage(0);
        }

        //client who picked up - logic
        if (SingletonRunner.runner.LocalPlayer == playerId)
        {
            if (costsSouls)
            {
                if (inventory.instance.gameObject.activeSelf)
                {
                    Dictionary<item, int> itemCounts = inventory.instance.GetItemCounts(false);

                    if (itemCounts.ContainsKey(soulItem) && itemCounts[soulItem] >= soulCost)
                    {
                        inventory.instance.AddItem(Item, count, durability, itemData);
                        RemoveSouls();
                    }
                }
                else
                {
                    int soulCount = 0;
                    inventorySlot s = null;
                    foreach (inventorySlot slot in ValeManager.instance.valeInventoryItems)
                    {
                        if (slot.itemInSlot == soulItem)
                        {
                            soulCount += slot.itemCountInSlot;
                            s = slot;
                            break;
                        }
                    }
                    if (soulCount >= soulCost)
                    {
                        ValeManager.instance.AddInventoryItem(Item, count, false);
                        s.itemCountInSlot -= soulCost;
                        s.itemCountTextInstance.text = "" + s.itemCountInSlot;

                        if (s.itemCountInSlot <= 0)
                        {
                            ValeManager.instance.valeInventoryItems.Remove(s);
                            Destroy(s.gameObject);
                        }

                    }
                }

                gameManager.endScreenStats["uniques"]++;
            }
            //normal item collect
            else
            {
                if (inventory.instance.gameObject.activeSelf)
                    inventory.instance.AddItem(Item, count, durability, itemData);
                else
                    ValeManager.instance.AddInventoryItem(Item, count, false);
            }

            gameManager.endScreenStats["items"]++;
        }

        if (indicatorInstance)
            Destroy(indicatorInstance);

        //if unique item
        if (transform.parent && transform.parent.gameObject.name.Contains("Altar"))
        {
            gameObject.SetActive(false);
        }
        else if (playerId == Runner.LocalPlayer)
        {
            RPC_Despawn();
        }

        //unique items saving
        if (naturalItemID < 0)
            treeRPCs.Instance.collectedItems.TryAdd(naturalItemID, true);

        if (dungeonItemId > -1)
            DungeonGenerator.instance.collectedItems.Add(dungeonItemId);

        if (customPickupSound) customPickupSound.Play();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RespawnUnique()
    {
        gameObject.SetActive(true);
        treeRPCs.Instance.collectedItems.Remove(naturalItemID);

        pickupHitbox.enabled = true;

        sprite.enabled = true;
        disableCollection = false;

    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_Despawn()
    {
        SingletonRunner.runner.Despawn(view);
    }

    void RemoveSouls()
    {
        Dictionary<item, int> itemCounts = inventory.instance.GetItemCounts(false);

        bool canCraft = true;


        if (itemCounts.ContainsKey(soulItem))
        {
            if (itemCounts[soulItem] < soulCost)
            {
                canCraft = false;
            }
        }
        else
        {
            canCraft = false;
        }


        if (canCraft)
        {
            int removedAmount = 0;

            foreach (inventorySlot slot in inventory.instance.slots)
            {
                if (slot.itemInSlot == soulItem)
                {
                    if (slot.itemCountInSlot > soulCost - removedAmount)
                    {
                        slot.itemCountInSlot -= (soulCost - removedAmount);
                        removedAmount = soulCost;
                    }
                    else
                    {
                        removedAmount += slot.itemCountInSlot;
                        inventory.instance.removeItem(slot);
                    }

                }

                if (removedAmount == soulCost)
                {
                    break;
                }

            }
        }
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetItem(string itemIndex, short amount, short durability, string itemData, bool dontSave, NetworkId boatId, int boatFloor)
    {
        this.itemData = inventory.StringToItemData(itemData);

        Item = gameManager.instance.itemDictionary[itemIndex];
        count = amount;
        this.durability = durability;
        sprite.sprite = Item.overworldSprite;
        this.dontSave = dontSave;

        if (SingletonRunner.runner.TryFindObject(boatId, out var b))
        {
            boatParent = b.GetComponent<Boat>();
            transform.SetParent(boatParent.itemHolder,true);

            sprite.sortingOrder = boatParent.playerFloorLayers[boatFloor] - 1;
            solidCollider.gameObject.layer = 19;
            this.boatFloor = boatFloor;
        }
        else
        {
            solidCollider.excludeLayers |= (1 << LayerMask.NameToLayer("Boat"));
        }

        if (count > 1)
        {
            sprite2.sprite = Item.overworldSprite;
        }

        if (Item.unlitWhenDropped)
        {
            sprite.material = unlitOutlineMat;
            if (sprite2)
                sprite2.material = unlitOutlineMat;
        }

    }
    public void ApplyForce(string ID, Vector3 playerHandPos, float playerHandRot, Vector3 playerAxisInput)
    {
        RPC_ApplyForce(ID, playerHandPos, playerHandRot, playerAxisInput);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_ApplyForce(string itemIndex, Vector3 playerHandPos, float playerHandRot, Vector3 playerAxisInput)
    {
        StartCoroutine(_ApplyForce(itemIndex, playerHandPos, playerHandRot, playerAxisInput));
    }

    IEnumerator _ApplyForce(
    string itemIndex,
    Vector3 playerHandPos,
    float playerHandRot,
    Vector3 playerAxisInput)
    {
        if (solidCollider)
            solidCollider.enabled = true;

        transform.eulerAngles = new Vector3(0, 0, playerHandRot);

        Rigidbody2D itemRb = rb;
        itemRb.bodyType = RigidbodyType2D.Dynamic;

        // Get parent Rigidbody2D if it exists
        Rigidbody2D parentRb = GetComponentInParent<Rigidbody2D>();
        if (parentRb == itemRb)
            parentRb = null; // prevent self-reference

        Vector2 lastParentPos = parentRb ? parentRb.position : Vector2.zero;

        // Initial local throw motion (MovePosition-driven)
        localMotion =
            (transform.position - playerHandPos + (playerAxisInput * 0.2f)) *
            (gameManager.instance.itemDictionary[itemIndex].throwForce * 1.7f);

        float angularSpeed = 1015f;
        float damping = 4f;

        while (itemRb && localMotion.magnitude > 0.01f)
        {
            yield return new WaitForFixedUpdate();

            rb.linearVelocity = Vector2.zero;

            Vector2 parentDelta = Vector2.zero;
            if (boatParent) parentDelta = boatParent.dif;

            // Integrate motion
            Vector2 nextPos =
                itemRb.position +
                parentDelta +
                localMotion * Time.fixedDeltaTime;

            itemRb.MovePosition(nextPos);

            // Manual damping
            localMotion = Vector2.Lerp(
                localMotion,
                Vector2.zero,
                damping * Time.fixedDeltaTime
            );

            // Manual angular motion
            itemRb.MoveRotation(
                itemRb.rotation + angularSpeed * Time.fixedDeltaTime
            );

            angularSpeed = Mathf.Lerp(
                angularSpeed,
                0f,
                damping * Time.fixedDeltaTime
            );
        }


        // Final stop
        itemRb.MovePosition(itemRb.position);
        itemRb.MoveRotation(itemRb.rotation);

        rb.bodyType = RigidbodyType2D.Kinematic;

        if (solidCollider)
            solidCollider.enabled = false;
    }

    private void Update()
    {
        //indicator follow item
        if (indicatorInstance != null)
        {
            indicatorInstance.transform.position = transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0);
        }
    }


    public void SetItem(string ID, short amount, short durability, ItemData itemData, bool dontSave = false, NetworkId boatId = new NetworkId(), int boatFloor = 0)
    {
        RPC_SetItem(ID, amount, durability, inventory.ItemDataToString(itemData), dontSave, boatId, boatFloor);
    }

    public void Disable()
    {
        RPC_Disable();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_Disable()
    {
        gameObject.SetActive(false);
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), 0.15f);
    }
}


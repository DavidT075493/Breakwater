using DG.Tweening;
using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TileEntity : MonoBehaviour
{
    public item Item;
    public placedTile tile;

    Color highlightColor = new Color(1, 0.88f, 0.36f);
    public Boat boatParent;
    float animTimer;

    public static bool allPlayersOnPuddle;

    [Header("Interact")]
    public float playerCheckSize = 2.5f;
    public LayerMask mainPlayerLayer;
    public GameObject indicatorPrefab;
    GameObject indicatorInstance;
    Animator indicatorAnim;
    TextMeshPro indicatorText;
    public Vector2 indicatorOffset;
    public bool playerInRange;
    public Vector3 playerCheckPos;
    public static List<TileEntity> interactablesInRange = new List<TileEntity>();
    public bool inDungeon;

    public static TileEntity currentSpawnPoint;
    public List<string> respawnUsers = new List<string>();

    [Header("Sign")]
    public TextMeshPro signText, playerCountText;

    [Header("Door")]
    public Transform hinge1;
    public Transform hinge2;
    public bool open { get; private set; }
    public bool doorFlipped;
    const float doorOpenTime = 0.2f;
    public bool dommy = true;
    public placedTile childDoor;
    public placedTile parentDoor;
    public bool inDoorBond = false;

    public static Transform closestTile;
    AudioSource doorOpenSource, doorCloseSource;
    Collider2D activeCollider;

    [Header("Furnace")]
    public const float smeltTime = 7;
    public const float purifierSmeltTime = 12;
    public float smeltTimeLeft = smeltTime;
    public float fuelTimeLeft;
    public float maxFuelTime;
    public static TileEntity currentFurnace;


    public int fuelCount;
    public item fuelItem;

    public item ingredientItem;
    public int ingredientCount;

    public item resultItem;
    public int resultCount;

    int currentPlayerUsing;
    public bool beingUsed;

    [Header("Vehicle Station")]
    public static TileEntity currentVStation;
    public Vector2 boatCheckSize = new Vector2(7.02f, 2.4f);
    public Vector2 boatCheckOffset;
    public bool canPlaceBoat;
    Color boatPreviewColor;
    public Sprite boatPreviewSprite, bigBoatPreviewSprite;

    [Header("Chest")]
    public static TileEntity currentChest;
    public string[] chestItems = new string[27];
    public short[] chestCounts = new short[27];
    public short[] chestDurabililties = new short[27];
    public ItemData[] chestItemData = new ItemData[27];
    //for chests in boats

    [Header("Farm")]
    public int growthTime;
    public int growthTimeTarget;
    public int growthStage;
    public itemPickup attatchedCropItem;
    public int wateredTime;
    public placedTile parentFarmlandTile;
    public bool watered;
    public Island island;
    public float rainAwaitTime;
    bool receivingRain;
    int rainWateredTime;
    private float bossStartTimer;
    public static int playersOnPuddle;
    Material defaultMaterial;
    private void Start()
    {
        indicatorOffset = new Vector2(0, 0.5f);
        defaultMaterial = tile.sprite.material;

        mainPlayerLayer = (1 << 8);
        indicatorPrefab = tile.indicatorPrefab;
        inventory.instance.onCloseInventory += OnCloseInventory;

        if (inDungeon) playerCheckPos = transform.position;

        switch (Item.TileType)
        {
            case item.tileType.door:
                doorOpenSource = gameObject.AddComponent<AudioSource>();
                doorCloseSource = gameObject.AddComponent<AudioSource>();

                doorOpenSource.playOnAwake = false;
                doorCloseSource.playOnAwake = false;
                doorOpenSource.clip = tile.doorOpenClip;
                doorCloseSource.clip = tile.doorCloseClip;

                doorOpenSource.spatialBlend = 1;
                doorOpenSource.spread = 160;
                doorOpenSource.minDistance = 4.5f;
                doorOpenSource.maxDistance = 10;
                doorOpenSource.dopplerLevel = 0;
                doorOpenSource.rolloffMode = AudioRolloffMode.Linear;
                doorOpenSource.outputAudioMixerGroup = tile.sfxGroup;

                doorCloseSource.spatialBlend = 1;
                doorCloseSource.spread = 160;
                doorCloseSource.minDistance = 4.5f;
                doorCloseSource.maxDistance = 10;
                doorCloseSource.dopplerLevel = 0;
                doorCloseSource.rolloffMode = AudioRolloffMode.Linear;
                doorCloseSource.outputAudioMixerGroup = tile.sfxGroup;

                break;

            case item.tileType.crop:
                parentFarmlandTile = gameManager.instance.placedTiles.Find(t => t.position == (Vector2)transform.position && (t.Item.itemId == "scythe" || t.Item.itemId == "catalyst_incubator")).PlacedTile;

                if (parentFarmlandTile == null) gameManager.instance.RemoveTile((Vector2)transform.position, Item, true);

                gameManager.instance.OnRandomTick += OnRandomTick;
                if (growthTimeTarget == 0)
                {
                    SetGrowthStage(0);
                    growthTime = 0;
                    growthTimeTarget = UnityEngine.Random.Range(Item.growthTicks.x, Item.growthTicks.y + 1);
                }

                foreach (Island i in MapGenerator.instance.islands)
                {
                    if (transform.position.x < (i.center.x + i.size / 2) && transform.position.x > (i.center.x - i.size / 2)
                    && transform.position.y < (i.center.y + i.size / 2) && transform.position.y > (i.center.y - i.size / 2))
                    {
                        island = i;
                    }
                }

                break;
            case item.tileType.respawn:

                if (tile.owner == PlayerPrefs.GetString("userID") && playerHealth.instance)
                    SetSpawnPoint();

                break;

        }

        if (tile.health.activeCollider)
            activeCollider = tile.health.activeCollider.GetComponent<Collider2D>();
    }


    public void SetSpawnPoint(bool disableEffects = false)
    {
        if (currentSpawnPoint)
        {
            if (currentSpawnPoint.boatParent)
                gameManager.instance.RPC_SetPlayerRespawnPoint(currentSpawnPoint.transform.position, DataPersistanceManager.userId, false, true,
                    currentSpawnPoint.boatParent.boatID, currentSpawnPoint.tile.boatTileIndex);
            else
                gameManager.instance.RPC_SetPlayerRespawnPoint(currentSpawnPoint.transform.position, DataPersistanceManager.userId, false, true);

        }

        playerHealth.instance.respawnPos = transform.position;
        currentSpawnPoint = this;

        if (boatParent)
            gameManager.instance.RPC_SetPlayerRespawnPoint(transform.position, DataPersistanceManager.userId, true, disableEffects,
                boatParent.boatID, tile.boatTileIndex);
        else
            gameManager.instance.RPC_SetPlayerRespawnPoint(transform.position, DataPersistanceManager.userId, true, disableEffects);
    }

    public void RespawnSetEffect()
    {
        tile.respawnEffect.SetActive(false);
        tile.respawnEffect.SetActive(true);
    }

    private void Update()
    {
        if (boatParent)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * boatParent.transform.localScale.x, transform.localScale.y);
        }

        //respawn tick
        if (Item.TileType == item.tileType.respawn)
        {
            if (currentSpawnPoint == this)
            {
                tile.sprite.sprite = Item.alternateTileSprite;
                tile.sprite.material = tile.unlitMat;
            }
            else
            {
                tile.sprite.sprite = Item.tileStates[0].tileSprite;
                tile.sprite.material = defaultMaterial;
            }
        }
        //furnace tick
        else if (Item.TileType == item.tileType.furnace || Item.TileType == item.tileType.purifier)
        {
            if (!inventory.instance.inventoryOpen)
                currentFurnace = null;

            if (fuelTimeLeft <= 0 && fuelItem)
            {
                maxFuelTime = fuelItem.fuelSmeltTime;
                fuelTimeLeft = fuelItem.fuelSmeltTime;
                fuelCount -= 1;
                if (fuelCount <= 0)
                {
                    fuelItem = null;
                }

                if (currentFurnace == this)
                {
                    LoadFurnaceItems();
                }

            }

            if (ingredientItem && fuelTimeLeft > 0 && (resultItem == null || resultItem == ingredientItem.smeltResult))
            {
                fuelTimeLeft -= Time.deltaTime;
                smeltTimeLeft -= Time.deltaTime;

                if (smeltTimeLeft <= 0)
                {

                    if (Item.TileType == item.tileType.furnace)
                    {
                        smeltTimeLeft = smeltTime;
                    }
                    else if (Item.TileType == item.tileType.purifier)
                    {
                        smeltTimeLeft = purifierSmeltTime;
                    }

                    ingredientCount -= 1;

                    resultCount += 1;
                    resultItem = ingredientItem.smeltResult;

                    if (ingredientCount <= 0)
                    {
                        ingredientItem = null;
                    }

                    if (currentFurnace == this)
                    {
                        LoadFurnaceItems();
                    }

                }


                if (tile.sprite.sprite != Item.alternateTileSprite)
                {
                    if (boatParent == null)
                        gameManager.instance.SetFurnaceCooking(transform.position, true);
                    else
                        gameManager.instance.SetFurnaceCooking(transform.position, true, boatParent.boatID, tile.boatTileIndex);
                }

            }
            else
            {
                if (Item.TileType == item.tileType.furnace)
                {
                    smeltTimeLeft = smeltTime;
                }
                else if (Item.TileType == item.tileType.purifier)
                {
                    smeltTimeLeft = purifierSmeltTime;
                }


                if (tile.sprite.sprite != Item.tileStates[0].tileSprite)
                {
                    if (boatParent == null)
                        gameManager.instance.SetFurnaceCooking(transform.position, false);
                    else
                        gameManager.instance.SetFurnaceCooking(transform.position, false, boatParent.boatID, tile.boatTileIndex);
                }
            }

            if (currentFurnace == this)
            {
                if (Item.TileType == item.tileType.furnace)
                {
                    inventory.instance.UpdateFurnaceUI(tile);
                }
                else if (Item.TileType == item.tileType.purifier)
                {
                    inventory.instance.UpdatePurifierUI(tile);
                }

            }

        }
        //vehicle station tick
        else if (Item.TileType == item.tileType.vehicleStation)
        {
            if (!inventory.instance.inventoryOpen)
                currentVStation = null;

            RaycastHit2D waterCheck;
            if (tile.boatPreview.transform.localScale.x > 0)
            {
                waterCheck = Physics2D.BoxCast(tile.boatPreview.transform.position + (Vector3)boatCheckOffset, boatCheckSize, 0, Vector2.zero, Mathf.Infinity, tile.boatBlockingLayers);
            }
            else
            {
                waterCheck = Physics2D.BoxCast(tile.boatPreview.transform.position + new Vector3(boatCheckOffset.x * -1, boatCheckOffset.y), boatCheckSize, 0, Vector2.zero, Mathf.Infinity, tile.boatBlockingLayers);
            }

            if (waterCheck.collider)
            {
                boatPreviewColor = tile.boatCantColor;
            }
            else
            {
                boatPreviewColor = tile.boatCanColor;
            }
            canPlaceBoat = !waterCheck.collider;

            tile.boatPreview.color = Color.Lerp(tile.boatPreview.color, boatPreviewColor, 13 * Time.deltaTime);
        }
        //chest tick
        else if (Item.TileType == item.tileType.chest)
        {
            if (beingUsed)
            {
                tile.sprite.sprite = Item.alternateTileSprite;
            }
            else
            {
                tile.sprite.sprite = Item.tileStates[0].tileSprite;
            }
        }
        else if (Item.TileType == item.tileType.campfire)
        {
            animTimer += Time.deltaTime;
            if (animTimer > 0.15f && tile.sprite.sprite != Item.alternateTileSprite)
            {
                tile.sprite.sprite = Item.alternateTileSprite;
            }
            if (animTimer > 0.3f && tile.sprite.sprite == Item.alternateTileSprite)
            {
                tile.sprite.sprite = Item.tileStates[0].tileSprite;
                animTimer = 0;
            }

        }
        else if (Item.TileType == item.tileType.crop && Item.wateredAdditionalGrowth > 0)
        {
            if (watered)
            {
                parentFarmlandTile.sprite.color = Color.Lerp(parentFarmlandTile.sprite.color, new Color(0.55f, 0.55f, 0.55f), Time.deltaTime * 7);
            }

            //watered during rain
            if (WeatherManager.Instance.shouldBeRaining && !(
                island.biome == (int)MapGenerator.biome.desert ||
                island.biome == (int)MapGenerator.biome.snow ||
                island.biome == (int)MapGenerator.biome.volcanic
                ))
            {
                if (!receivingRain)
                {
                    rainAwaitTime = UnityEngine.Random.Range(3, 8);
                    receivingRain = true;
                    rainWateredTime = UnityEngine.Random.Range(4, 8);
                }
                else
                {
                    rainAwaitTime -= Time.deltaTime;

                    if (rainAwaitTime <= 0)
                    {
                        rainAwaitTime = 0;
                        SetWatered(rainWateredTime, true);
                    }
                }
            }
            else
            {
                receivingRain = false;
            }

        }
        else if (Item.TileType == item.tileType.ominousFlower && playerSpawner.instance.players != null && !Boss.instance.startedBoss && player.instance && !player.instance.inCutscene)
        {
            int players = 0;

            foreach (player p in playerSpawner.instance.players)
            {
                if (p && p.onGooPuddle)
                {
                    players++;
                }
            }
            playersOnPuddle = players;

            int playerCount = playerSpawner.playerCountInGame;

            if (gameManager.instance.difficulty == 2) playerCount = playerSpawner.playerCountNotDead;

            if (playerCount <= 0) playerCount = 1;

            playerCountText.text = playersOnPuddle + "/" + playerCount;

            if (playersOnPuddle == playerCount)
            {
                bossStartTimer += Time.deltaTime;

                if (bossStartTimer > 0.7 && !menu.instance.waitingForReady)
                {
                    StartCoroutine(menu.instance._WaitForReadyBoss());
                }

            }
            else
            {
                bossStartTimer = 0;
            }


        }

        if (player.instance && Item && Item.interactable)
        {
            if (Item.TileType == item.tileType.door)
            {
                activeCollider.enabled = !open && hinge1.localEulerAngles.z <= 0.01f && Mathf.Abs(hinge2.localEulerAngles.z) <= 0.01f;
            }

            if (Item.TileType == item.tileType.door && !dommy)
            {
                return;
            }
            Collider2D playerCheck;
            playerCheck = Physics2D.OverlapBox(playerCheckPos, new Vector2(Item.interactBoxSize, Item.interactBoxSize), 0, mainPlayerLayer);


            

            if (boatParent)
            {
                playerCheck = Physics2D.OverlapBox(playerCheckPos, new Vector2(Item.interactBoxSize * 0.75f, Item.interactBoxSize * 0.75f), 0, mainPlayerLayer);
                playerCheckPos = transform.position;
                //indicator scale
                //if (indicatorInstance) indicatorInstance.transform.localScale = new Vector2(parentBoat.transform.localScale.x, indicatorInstance.transform.localScale.y);

                if (!player.instance || player.instance.currentBoat != boatParent)
                {
                    return;
                }
            }

            //no pick up item and use tile at the same time
            if (itemPickup.closestItem || Chat.Instance.chatOpen || gameManager.instance.writingSign || pauseScreen.instance.paused)
                playerCheck = null;

            if (player.instance.currentBoat && (player.instance.currentBoat.inRangeOfInteractable || player.instance.boatFloor != 0))
                playerCheck = null;

            if (Item.TileType == item.tileType.respawn && currentSpawnPoint == this)
                playerCheck = null;

            if (playerCheck && !(player.instance.currentBoat && !boatParent) && !playerHealth.instance.dead && !player.instance.pilotingBoat && !Chat.Instance.chatOpen)
            {
                if (!interactablesInRange.Contains(this))
                {
                    interactablesInRange.Add(this);
                }

                closestTile = player.instance.GetClosestTile(interactablesInRange.ToArray());

                if (closestTile == transform)
                {
                    //change color
                    tile.sprite.DOColor(highlightColor, 0.1f);
                    //double doors
                    if (childDoor != null)
                    {
                        childDoor.sprite.DOColor(highlightColor, 0.1f);
                        childDoor.tileEntity.playerInRange = true;
                    }

                    playerInRange = true;

                    if (!indicatorInstance)
                        spawnIndicator();

                    if ((InputManager.actions["Interact"].started || menu.instance.testEPress)
                        && !(inventory.instance.inventoryOpen && InputManager.usingController))
                    {
                        InteractPressed();
                        menu.instance.testEPress = false;
                    }

                }
                else
                {
                    RemoveHighlightEffect();
                }

            }
            else
            {
                if (interactablesInRange.Contains(this))
                {
                    interactablesInRange.Remove(this);
                }

                destroyIndicator();

                RemoveHighlightEffect();

            }

        }

        

        if (boatParent && indicatorInstance)
        {
            indicatorInstance.transform.position = playerCheckPos + new Vector3(indicatorOffset.x, indicatorOffset.y, 0);
        }


    }


    public void OnRandomTick(object sender, EventArgs e)
    {
        if (growthStage >= Item.growthStages.Count() - 1 && attatchedCropItem != null) return;

        growthTime++;

        if (wateredTime > 0)
        {
            wateredTime--;
            growthTime += Item.wateredAdditionalGrowth;

            if (wateredTime <= 0)
            {
                gameManager.instance.SetWatered((Vector2)transform.position, 0, false);
            }

        }

        if (growthTime >= growthTimeTarget)
        {
            SetGrowthStage(growthStage + 1);
            growthTime = 0;
            growthTimeTarget = UnityEngine.Random.Range(Item.growthTicks.x, Item.growthTicks.y + 1);
        }
    }


    public async void SetGrowthStage(int stage, bool disableItem = false)
    {
        if (!tile.sprite) return;

        growthStage = Mathf.Clamp(stage, 0, Item.growthStages.Count() - 1);

        tile.sprite.sprite = Item.growthStages[growthStage].sprite;
        tile.sprite.transform.localPosition = new Vector2(0, Item.growthStages[growthStage].spriteYOffset - 0.02f);
        tile.sort.sortingLayerName = Item.growthStages[growthStage].sortingLayer;
        tile.sort.sortingOrder = Item.growthStages[growthStage].sortingOrder;

        foreach (GameObject collider in tile.colliders)
        {
            collider.SetActive(false);
        }

        if (boatParent)
        {
            tile.sort.sortingLayerName = "Player";
            tile.sort.sortingOrder = stage == 0 ? 9 : 10;
        }
        else
        {
            tile.colliders[Item.growthStages[growthStage].colliderIndex].SetActive(true);
        }

        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            string boatId = "";
            if (boatParent) boatId = boatParent.boatID;

            if (Item.growthStages[growthStage].Item != null && !disableItem)
            {
                itemPickup i = SingletonRunner.runner.Spawn(gameManager.instance.cropItem, tile.sprite.transform.position - new Vector3(0, 0.01f, 0), Quaternion.identity).GetComponent<itemPickup>();

                await Task.Yield();

                gameManager.instance.SetCropParent(i.view.Id, (Vector2)transform.position, Item.growthStages[growthStage].Item.itemId,
                    UnityEngine.Random.Range(Item.growthStages[growthStage].itemAmount.x, Item.growthStages[growthStage].itemAmount.y + 1),
                    tile.sprite.transform.position - new Vector3(0, 0.01f, 0)
                    , boatId, tile.boatTileIndex);
            }

            gameManager.instance.SetGrowthStage((Vector2)transform.position, growthStage, boatId, tile.boatTileIndex);
        }
    }

    public void SetWatered(int wateredTime, bool disableAnim)
    {
        if (Item.wateredAdditionalGrowth <= 0) return;

        if (!parentFarmlandTile)
            parentFarmlandTile = gameManager.instance.placedTiles.Find(t => t.position == (Vector2)transform.position && (t.Item.itemId == "scythe" || t.Item.itemId == "catalyst_incubator")).PlacedTile;

        this.wateredTime = wateredTime;

        if (wateredTime == 0)
        {
            parentFarmlandTile.sprite.DOColor(Color.white, 2);
            parentFarmlandTile.farmlandWatered = false;
            watered = false;
        }
        else
        {
            if (!disableAnim)
            {
                tile.wateredEffect.Play();
                tile.wateredEffect.GetComponent<AudioSource>().Play();
            }

            parentFarmlandTile.farmlandWatered = true;
            watered = true;
        }

    }

    void OnCloseInventory(object sender, EventArgs e)
    {
        if (currentPlayerUsing == DataPersistanceManager.LocalActorIndex)
        {
            currentPlayerUsing = -1;

            if (Item.TileType == item.tileType.anvil)
            {
                if (inventory.instance.anvilToolSlot.itemInSlot)
                {
                    EmptySlotToInventory(inventory.instance.anvilToolSlot);
                }
                if (inventory.instance.anvilMaterialSlot.itemInSlot)
                {
                    EmptySlotToInventory(inventory.instance.anvilMaterialSlot);
                }
            }
        }


    }

    void EmptySlotToInventory(inventorySlot slot)
    {
        item itemInSlot = slot.itemInSlot;
        int itemCountInSlot = slot.itemCountInSlot;
        int durabilityInSlot = slot.durability;
        ItemData itemDataInSlot = slot.itemData;
        inventory.instance.removeItem(slot);
        inventory.instance.AddItem(itemInSlot, itemCountInSlot, durabilityInSlot, itemDataInSlot);
    }


    public void SetFurnaceCooking(bool cooking)
    {
        if (cooking)
        {
            tile.sprite.sprite = Item.alternateTileSprite;
            switch (Item.TileType)
            {
                case item.tileType.furnace:
                    tile.furnacePs.Play();
                    tile.furnaceSound.volume = 0;
                    tile.furnaceSound.Play();
                    tile.furnaceSound.DOFade(1, 1);
                    break;
                case item.tileType.purifier:
                    tile.purifierPs.Play();
                    tile.purifierSound.volume = 0;
                    tile.purifierSound.Play();
                    tile.purifierSound.DOFade(0.35f, 1);
                    break;
            }

        }
        else
        {
            tile.sprite.sprite = Item.tileStates[0].tileSprite;
            switch (Item.TileType)
            {
                case item.tileType.furnace:
                    tile.furnacePs.Stop();
                    tile.furnaceSound.DOFade(0, 1);
                    break;
                case item.tileType.purifier:
                    tile.purifierPs.Stop();
                    tile.purifierSound.DOFade(0, 1);
                    break;
            }
            Invoke("StopFurnaceSound", 1.1f);
        }
        tile.furnaceLight.SetActive(cooking);
    }

    void StopFurnaceSound()
    {
        if (tile.furnaceSound.volume == 0)
            tile.furnaceSound.Stop();
        if (tile.purifierSound.volume == 0)
            tile.purifierSound.Stop();
    }

    // drop all items in furnace on destroy
    public void FurnaceDestroyed()
    {
        if (currentPlayerUsing == DataPersistanceManager.LocalActorIndex)
        {
            if (inventory.instance.inventoryOpen)
                inventory.instance.CloseInventory();
            inventory.instance.inFurnace = false;
            currentPlayerUsing = -1;
        }
        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            Vector2 itemOffset = new Vector2(0, 0);
            NetworkId boatId = new NetworkId();
            if (boatParent)
            {
                itemOffset = new Vector2(0, -0.4f);
                boatId = boatParent.view.Id;
            }
            if (fuelItem)
            {
                GameObject droppedItem = SingletonRunner.runner.Spawn(inventory.instance.dropItemPrefab, (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f)) + itemOffset, Quaternion.identity).gameObject;
                itemPickup ItemPickup = droppedItem.GetComponent<itemPickup>();
                ItemPickup.Item = fuelItem;
                ItemPickup.count = fuelCount;
                ItemPickup.GetComponent<NetworkTransform>().enabled = false;
                ItemPickup.SetItem(fuelItem.itemId, (short)fuelCount, 1, new ItemData(0), false, boatId);
            }
            if (ingredientItem)
            {
                GameObject droppedItem = SingletonRunner.runner.Spawn(inventory.instance.dropItemPrefab, (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f)) + itemOffset, Quaternion.identity).gameObject;
                itemPickup ItemPickup = droppedItem.GetComponent<itemPickup>();
                ItemPickup.Item = ingredientItem;
                ItemPickup.count = ingredientCount;
                ItemPickup.GetComponent<NetworkTransform>().enabled = false;
                ItemPickup.SetItem(ingredientItem.itemId, (short)ingredientCount, 1, new ItemData(0), false, boatId);
            }
            if (resultItem)
            {
                GameObject droppedItem = SingletonRunner.runner.Spawn(inventory.instance.dropItemPrefab, (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f)) + itemOffset, Quaternion.identity).gameObject;
                itemPickup ItemPickup = droppedItem.GetComponent<itemPickup>();
                ItemPickup.Item = resultItem;
                ItemPickup.count = resultCount;
                ItemPickup.GetComponent<NetworkTransform>().enabled = false;
                ItemPickup.SetItem(resultItem.itemId, (short)resultCount, 1, new ItemData(0), false, boatId);
            }

        }

    }
    public void ChestDestroyed()
    {
        Vector2 itemOffset = new Vector2(0, 0);
        NetworkId boatId = new NetworkId();
        if (boatParent)
        {
            itemOffset = new Vector2(0, -0.4f);
            boatId = boatParent.view.Id;
        }

        if (currentPlayerUsing == DataPersistanceManager.LocalActorIndex)
        {
            for (int i = 0; i < inventory.instance.chestSlots.Length; i++)
            {
                if (!inventory.instance.chestSlots[i].itemInSlot) continue;

                GameObject droppedItem = SingletonRunner.runner.Spawn(inventory.instance.dropItemPrefab, (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f)) + itemOffset, Quaternion.identity).gameObject;
                itemPickup ItemPickup = droppedItem.GetComponent<itemPickup>();
                ItemPickup.Item = inventory.instance.chestSlots[i].itemInSlot;
                ItemPickup.count = inventory.instance.chestSlots[i].itemCountInSlot;
                ItemPickup.GetComponent<NetworkTransform>().enabled = false;
                ItemPickup.SetItem(inventory.instance.chestSlots[i].itemInSlot.itemId, (short)inventory.instance.chestSlots[i].itemCountInSlot, (short)inventory.instance.chestSlots[i].durability, inventory.instance.chestSlots[i].itemData
                    , false, boatId);
            }
            if (inventory.instance.inventoryOpen)
                inventory.instance.CloseInventory();
            currentPlayerUsing = -1;
        }
        else if (currentPlayerUsing == -1 && SingletonRunner.runner.IsSharedModeMasterClient)
        {
            for (int i = 0; i < inventory.instance.chestSlots.Length; i++)
            {
                if (chestItems[i] == "" || chestItems[i] == null || gameManager.instance.itemDictionary[chestItems[i]] == null) continue;

                GameObject droppedItem = SingletonRunner.runner.Spawn(inventory.instance.dropItemPrefab, (Vector2)transform.position + new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f)) + itemOffset, Quaternion.identity).gameObject;
                itemPickup ItemPickup = droppedItem.GetComponent<itemPickup>();
                ItemPickup.Item = gameManager.instance.itemDictionary[chestItems[i]];
                ItemPickup.count = chestCounts[i];
                ItemPickup.GetComponent<NetworkTransform>().enabled = false;
                ItemPickup.SetItem(chestItems[i], chestCounts[i], chestDurabililties[i], chestItemData[i], false, boatId);
                
            }
        }

    }



    void RemoveHighlightEffect()
    {
        destroyIndicator();

        if (tile.sprite.color == highlightColor)
        {
            playerInRange = false;
            tile.sprite.DOColor(Color.white, 0.1f);

            if (Item.TileType == item.tileType.door && childDoor)
            {
                childDoor.sprite.DOColor(Color.white, 0.1f);
                childDoor.tileEntity.playerInRange = false;
            }
        }
    }

    void InteractPressed()
    {
        if (menu.instance.mapMaximized) return;

        switch (Item.TileType)
        {
            case item.tileType.door:

                if (childDoor)
                {
                    gameManager.instance.OpenDoor(childDoor.transform.position,!open);
                }
                gameManager.instance.OpenDoor(transform.position, !open);

                break;
            case item.tileType.furnace:
                if (!inventory.instance.inventoryOpen)
                {
                    if (beingUsed) break;

                    if (!boatParent)
                        gameManager.instance.RPC_RequestStartUsing(transform.position, SingletonRunner.runner.LocalPlayer, 0);
                    else
                        gameManager.instance.RPC_RequestStartUsing(transform.position, SingletonRunner.runner.LocalPlayer, 0, boatParent.boatID, tile.boatTileIndex);

                    //inventory.instance.OpenInventory(inventory.interfaceType.furnace);
                    //inventory.instance.inFurnace = true;
                    //currentFurnace = this;
                }
                else
                {
                    inventory.instance.CloseInventory();
                    //CloseFurnace();
                }

                break;
            case item.tileType.purifier:
                if (!inventory.instance.inventoryOpen)
                {
                    if (beingUsed) break;

                    if (!boatParent)
                        gameManager.instance.RPC_RequestStartUsing(transform.position, SingletonRunner.runner.LocalPlayer, 1);
                    else
                        gameManager.instance.RPC_RequestStartUsing(transform.position, SingletonRunner.runner.LocalPlayer, 1, boatParent.boatID, tile.boatTileIndex);


                    //inventory.instance.OpenInventory(inventory.interfaceType.purifier);
                    //inventory.instance.inFurnace = true;
                    //currentFurnace = this;
                }
                else
                {
                    inventory.instance.CloseInventory();
                    //CloseFurnace();
                }

                break;

            case item.tileType.vehicleStation:
                if (!inventory.instance.inventoryOpen)
                {
                    currentVStation = this;
                    inventory.instance.OpenInventory(inventory.interfaceType.vehicleStation);
                    //inventory.instance.boatSpawnPos = tile.boatPreview.transform.position + new Vector3(0, 2.034f);
                    inventory.instance.boatSpawnScale = tile.boatPreview.transform.localScale.x;

                }
                else
                {
                    inventory.instance.CloseInventory();
                    currentVStation = null;
                }
                break;
            case item.tileType.chest:
                if (!inventory.instance.inventoryOpen)
                {
                    if (beingUsed) break;

                    if (!boatParent)
                        gameManager.instance.RPC_RequestStartUsing(transform.position, SingletonRunner.runner.LocalPlayer, 2);
                    else
                        gameManager.instance.RPC_RequestStartUsing(transform.position, SingletonRunner.runner.LocalPlayer, 2, boatParent.boatID, tile.boatTileIndex);
                }
                else
                {
                    inventory.instance.CloseInventory();
                    //CloseChest();

                }
                break;
            case item.tileType.crafting:
                if (!inventory.instance.inventoryOpen)
                {
                    inventory.instance.OpenInventory(inventory.interfaceType.crafting);
                    currentPlayerUsing = DataPersistanceManager.LocalActorIndex;
                }
                else
                {
                    inventory.instance.CloseInventory();
                    currentPlayerUsing = -1;
                }
                break;

            case item.tileType.respawn:
                SetSpawnPoint();

                break;
            case item.tileType.anvil:
                if (!inventory.instance.inventoryOpen)
                {
                    inventory.instance.OpenInventory(inventory.interfaceType.anvil);
                    currentPlayerUsing = DataPersistanceManager.LocalActorIndex;
                }
                else
                {
                    inventory.instance.CloseInventory();
                    currentPlayerUsing = -1;
                }
                break;

        }



    }

    public void StartUsing(PlayerRef playerId, short type)
    {
        if (beingUsed) return;

        beingUsed = true;
        if (playerId == SingletonRunner.runner.LocalPlayer)
        {
            currentPlayerUsing = DataPersistanceManager.LocalActorIndex;

            switch (type)
            {
                case 0:
                    inventory.instance.OpenInventory(inventory.interfaceType.furnace);
                    inventory.instance.inFurnace = true;
                    currentFurnace = this;

                    beingUsed = true;

                    LoadFurnaceItems();

                    break;
                case 1:
                    inventory.instance.OpenInventory(inventory.interfaceType.purifier);
                    inventory.instance.inFurnace = true;
                    currentFurnace = this;

                    beingUsed = true;

                    LoadFurnaceItems();

                    break;
                case 2:
                    LoadChestItems();
                    inventory.instance.OpenInventory(inventory.interfaceType.chest);
                    currentChest = this;
                    break;

            }


        }

    }

    public void CloseChest()
    {
        if (!Application.isPlaying) return;

        currentChest = null;
        string boatId = "";

        if (boatParent) boatId = boatParent.boatID;

        gameManager.instance.StopUsingChest(transform.position, boatId, tile.boatTileIndex);
        beingUsed = false;


        for (short i = 0; i < inventory.instance.chestSlots.Length; i++)
        {

            if (inventory.instance.chestSlots[i].itemInSlot)
            {
                string itemDataJson = inventory.ItemDataToString(inventory.instance.chestSlots[i].itemData);
                gameManager.instance.RPC_SetChestItem(transform.position, i, inventory.instance.chestSlots[i].itemInSlot.itemId, (short)inventory.instance.chestSlots[i].itemCountInSlot, (short)inventory.instance.chestSlots[i].durability, itemDataJson, boatId, tile.boatTileIndex);
            }
            else
            {
                gameManager.instance.RPC_SetChestItem(transform.position, i, "null", 0, 1, "null", boatId, tile.boatTileIndex);
            }
        }
    }

    void LoadChestItems()
    {
        for (short i = 0; i < inventory.instance.chestSlots.Length; i++)
        {
            if (chestItems[i] == string.Empty || chestItems[i] == null)
                chestItems[i] = "null";

            inventory.instance.removeItem(inventory.instance.chestSlots[i]);
            if (chestItems[i] != "null")
                inventory.instance.addItemInSlot(gameManager.instance.itemDictionary[chestItems[i]], chestCounts[i], chestDurabililties[i], chestItemData[i], inventory.instance.chestSlots[i], inventory.instance.chestSlots[i].transform.position);
        }

    }

    void LoadFurnaceItems()
    {
        switch (Item.TileType)
        {
            case item.tileType.furnace:
                inventory.instance.removeItem(inventory.instance.furnaceIngr);
                inventory.instance.removeItem(inventory.instance.furnaceResult);
                inventory.instance.removeItem(inventory.instance.furnaceFuel);

                foreach (Transform child in inventory.instance.furnaceIngr.transform)
                {
                    Destroy(child.gameObject);
                }
                foreach (Transform child in inventory.instance.furnaceResult.transform)
                {
                    Destroy(child.gameObject);
                }
                foreach (Transform child in inventory.instance.furnaceFuel.transform)
                {
                    Destroy(child.gameObject);
                }


                if (ingredientItem)
                    inventory.instance.addItemInSlot(ingredientItem, ingredientCount, 1, new ItemData(0), inventory.instance.furnaceIngr, inventory.instance.furnaceIngr.transform.position);

                if (resultItem)
                    inventory.instance.addItemInSlot(resultItem, resultCount, 1, new ItemData(0), inventory.instance.furnaceResult, inventory.instance.furnaceResult.transform.position);

                if (fuelItem)
                    inventory.instance.addItemInSlot(fuelItem, fuelCount, 1, new ItemData(0), inventory.instance.furnaceFuel, inventory.instance.furnaceFuel.transform.position);
                break;


            case item.tileType.purifier:
                inventory.instance.removeItem(inventory.instance.purifierIngr);
                inventory.instance.removeItem(inventory.instance.purifierResult);
                inventory.instance.removeItem(inventory.instance.purifierFuel);

                foreach (Transform child in inventory.instance.purifierIngr.transform)
                {
                    Destroy(child.gameObject);
                }
                foreach (Transform child in inventory.instance.purifierResult.transform)
                {
                    Destroy(child.gameObject);
                }
                foreach (Transform child in inventory.instance.purifierFuel.transform)
                {
                    Destroy(child.gameObject);
                }


                if (ingredientItem)
                    inventory.instance.addItemInSlot(ingredientItem, ingredientCount, 1, new ItemData(0), inventory.instance.purifierIngr, inventory.instance.purifierIngr.transform.position);

                if (resultItem)
                    inventory.instance.addItemInSlot(resultItem, resultCount, 1, new ItemData(0), inventory.instance.purifierResult, inventory.instance.purifierResult.transform.position);

                if (fuelItem)
                    inventory.instance.addItemInSlot(fuelItem, fuelCount, 1, new ItemData(0), inventory.instance.purifierFuel, inventory.instance.purifierFuel.transform.position);

                break;
        }


    }
    public void SaveFurnaceItems()
    {
        switch (Item.TileType)
        {
            case item.tileType.furnace:
                ingredientItem = inventory.instance.furnaceIngr.itemInSlot;
                ingredientCount = inventory.instance.furnaceIngr.itemCountInSlot;

                resultItem = inventory.instance.furnaceResult.itemInSlot;
                resultCount = inventory.instance.furnaceResult.itemCountInSlot;

                fuelItem = inventory.instance.furnaceFuel.itemInSlot;
                fuelCount = inventory.instance.furnaceFuel.itemCountInSlot;
                break;

            case item.tileType.purifier:
                ingredientItem = inventory.instance.purifierIngr.itemInSlot;
                ingredientCount = inventory.instance.purifierIngr.itemCountInSlot;

                resultItem = inventory.instance.purifierResult.itemInSlot;
                resultCount = inventory.instance.purifierResult.itemCountInSlot;

                fuelItem = inventory.instance.purifierFuel.itemInSlot;
                fuelCount = inventory.instance.purifierFuel.itemCountInSlot;
                break;
        }
    }


    public void spawnIndicator()
    {
        if (indicatorInstance == null)
        {
            indicatorInstance = Instantiate(indicatorPrefab, playerCheckPos + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
        }
        else
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);

            indicatorInstance = Instantiate(indicatorPrefab, playerCheckPos + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
        }
        indicatorAnim = indicatorInstance.GetComponent<Animator>();
        indicatorText = indicatorInstance.GetComponent<TextMeshPro>();
        if (Item)
        {
            indicatorText.text = Item.interactAction[0].ToString().ToUpper() + Item.interactAction.Substring(1);

            if (Item.TileType == item.tileType.door && open) indicatorText.text = "Close";

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

    public void SetSignText(string text)
    {
        signText.gameObject.SetActive(true);
        signText.text = text;

    }

    public void OpenDoor(bool isOpen)
    {
        if (dommy)
            Invoke("spawnIndicator", 0.1f);

        if (!isOpen)
        {
            open = false;

            if (dommy)
                doorCloseSource.Play();

            if (!doorFlipped)
            {
                hinge1.DOLocalRotate(new Vector3(0, 0, 0), doorOpenTime);
            }
            else
            {
                hinge2.DOLocalRotate(new Vector3(0, 0, 0), doorOpenTime);
            }

        }
        else
        {
            open = true;

            if (dommy)
                doorOpenSource.Play();

            if (!doorFlipped)
            {
                hinge1.DOLocalRotate(new Vector3(0, 0, 90), doorOpenTime);
            }
            else
            {
                hinge2.DOLocalRotate(new Vector3(0, 0, -90), doorOpenTime);
            }

        }

    }
    public void SetOpen()
    {
        open = true;

        if (!doorFlipped)
        {
            hinge1.localEulerAngles = new Vector3(0, 0, 90);
        }
        else
        {
            hinge2.localEulerAngles = new Vector3(0, 0, -90);
        }
    }

    public void CloseFurnace()
    {
        string fuelId = "null";
        string ingredientId = "null";
        string resultId = "null";

        if (fuelItem) fuelId = fuelItem.itemId;
        if (ingredientItem) ingredientId = ingredientItem.itemId;
        if (resultItem) resultId = resultItem.itemId;

        if (boatParent == null)
            gameManager.instance.StopUsingFurnace(transform.position, fuelId, (short)fuelCount, ingredientId, (short)ingredientCount, resultId, (short)resultCount, smeltTimeLeft, fuelTimeLeft, maxFuelTime);
        else
            gameManager.instance.StopUsingFurnace(transform.position, fuelId, (short)fuelCount, ingredientId, (short)ingredientCount, resultId, (short)resultCount, smeltTimeLeft, fuelTimeLeft, maxFuelTime, boatParent.boatID, tile.boatTileIndex);

        beingUsed = false;
        currentPlayerUsing = -1;
        inventory.instance.inFurnace = false;
        currentPlayerUsing = -1;
    }

    public void StopUsingFurnace(string fuelItem, short fuelCount, string ingredItem, short ingredCount, string resultItem, short resultCount, float smeltTimeLeft, float fuelTimeLeft, float maxFuelTime)
    {
        beingUsed = false;
        this.fuelItem = gameManager.instance.itemDictionary[fuelItem];
        ingredientItem = gameManager.instance.itemDictionary[ingredItem];
        this.resultItem = gameManager.instance.itemDictionary[resultItem];
        this.fuelCount = fuelCount;
        ingredientCount = ingredCount;
        this.resultCount = resultCount;

        this.smeltTimeLeft = smeltTimeLeft;
        this.fuelTimeLeft = fuelTimeLeft;
        this.maxFuelTime = maxFuelTime;

        if (currentPlayerUsing == DataPersistanceManager.LocalActorIndex)
        {
            LoadFurnaceItems();
        }
    }


    public void SetFurnaceItems(string fuelItem, short fuelCount, string ingredItem, short ingredCount, string resultItem, short resultCount, float smeltTimeLeft, float fuelTimeLeft, float maxFuelTime)
    {
        this.fuelItem = gameManager.instance.itemDictionary[fuelItem];
        ingredientItem = gameManager.instance.itemDictionary[ingredItem];
        this.resultItem = gameManager.instance.itemDictionary[resultItem];
        this.fuelCount = fuelCount;
        ingredientCount = ingredCount;
        this.resultCount = resultCount;

        this.smeltTimeLeft = smeltTimeLeft;
        this.fuelTimeLeft = fuelTimeLeft;
        this.maxFuelTime = maxFuelTime;
    }


    public void StopUsingChest()
    {
        beingUsed = false;
    }

    public void SetChestItem(short slotNumber, string itemId, short itemCount, string itemData, short durability)
    {
        chestItems[slotNumber] = itemId;
        chestCounts[slotNumber] = itemCount;
        chestDurabililties[slotNumber] = durability;

        if (itemData != string.Empty)
        {
            try
            {
                chestItemData[slotNumber] = inventory.StringToItemData(itemData);
            }
            catch
            {
                Debug.LogError(Item + " failed to load data");
            }
        }
        else
        {
            chestItemData[slotNumber] = new ItemData(0);
        }



        beingUsed = false;
    }

    public void OnTileDestroy()
    {
        if (!Application.isPlaying) return;

        switch (Item.TileType)
        {
            case item.tileType.door:
                if (childDoor != null)
                {
                    childDoor.tileEntity.inDoorBond = false;
                    childDoor.tileEntity.dommy = true;
                    childDoor.tileEntity.destroyIndicator();
                }
                else if (parentDoor != null)
                {
                    parentDoor.tileEntity.childDoor = null;
                    parentDoor.tileEntity.destroyIndicator();
                    parentDoor.tileEntity.playerCheckPos = parentDoor.transform.position;
                    parentDoor.tileEntity.inDoorBond = false;

                }
                break;
            case item.tileType.vehicleStation:
                if (currentVStation == this)
                {
                    inventory.instance.CloseInventory();
                    currentVStation = null;
                }
                break;
            case item.tileType.chest:
                ChestDestroyed();
                if (currentChest == this)
                {
                    inventory.instance.CloseInventory();
                    currentChest = null;
                }
                break;
            case item.tileType.furnace:
                FurnaceDestroyed();

                if (currentFurnace == this)
                {
                    inventory.instance.CloseInventory();
                    currentChest = null;
                }
                break;
            case item.tileType.purifier:
                FurnaceDestroyed();

                if (currentFurnace == this)
                {
                    inventory.instance.CloseInventory();
                    currentChest = null;
                }
                break;
            case item.tileType.crafting:
                if (currentPlayerUsing == DataPersistanceManager.LocalActorIndex)
                {
                    if (inventory.instance.inventoryOpen)
                        inventory.instance.CloseInventory();
                }
                break;
            case item.tileType.crop:
                if (parentFarmlandTile && parentFarmlandTile.sprite)
                {
                    parentFarmlandTile.sprite.DOColor(Color.white, 0.5f);
                    parentFarmlandTile.farmlandWatered = false;
                }
                break;
            case item.tileType.respawn:
                if (currentSpawnPoint == this)
                    currentSpawnPoint = null;
                break;


        }

        if (indicatorInstance) Destroy(indicatorInstance);



    }

    private void OnDrawGizmosSelected()
    {
        if (Item.TileType == item.tileType.vehicleStation)
        {
            Gizmos.DrawWireCube(tile.boatPreview.transform.position + (Vector3)boatCheckOffset, boatCheckSize);

        }

    }

}

[System.Serializable]
public class GrowthStage
{
    public Sprite sprite;
    public item Item;
    public int colliderIndex = 8;
    public float spriteYOffset;
    public string sortingLayer;
    public int sortingOrder;
    public Vector2Int itemAmount;
}
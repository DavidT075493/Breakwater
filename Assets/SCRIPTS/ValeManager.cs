using DG.Tweening;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ValeManager : MonoBehaviour, IDataPersistance
{
    public static ValeManager instance;

    public bool lostSoul;

    public SoulRecover soul;
    public Transform soulFlashHolder, bellFlashHolder, trackerFlashHolder;
    public Light2D soulFlashLight, bellFlashLight, trackerFlashLight;
    public AudioSource soulFlashSound, trackerFlashSound;

    string[] itemIds;
    int[] itemCounts;
    int[] itemDurabilities;
    ItemData[] itemDatas;

    Vector2 lightFlashDelay = new Vector2(20, 30);

    [SerializeField] GameObject anchorPrefab;
    const int anchorAmount = 12;
    Transform[] anchors = new Transform[anchorAmount];
    AudioSource[] anchorSounds = new AudioSource[anchorAmount];
    int closestAnchor = -1;

    public bool escapingVale;

    public List<inventorySlot> valeInventoryItems = new List<inventorySlot>();
    [SerializeField] GameObject valeInventorySlotPref;
    [SerializeField] Transform slotHolder;
    [SerializeField] CanvasGroup slotsGroup;
    float slotsGroupAlpha;
    public AudioSource pickupSound;
    public SoulRecover soulRecover;

    bool trackersActive;
    private Coroutine soulFlashCo;
    public GameObject valeScreenRipple;

    public Color valeLavaColor;
    Color lavaDefaultColor;

    private void Awake()
    {
        instance = this;
        soul.gameObject.SetActive(false);
        trackersActive = true;
    }

    private void Start()
    {
        lavaDefaultColor = MapDisplay.Instance.lavaTilemap.color;
        slotsGroupAlpha = slotsGroup.alpha;
        slotsGroup.alpha = 0;
    }

    public void HideValeText()
    {
        menu.instance.soulRecoveredGroup.DOKill();
        menu.instance.soulRecoveredGroup.alpha = 0;
        menu.instance.valeText.DOKill();
        menu.instance.valeText.alpha = 0;
        menu.instance.valeText2.DOKill();
        menu.instance.valeText2.alpha = 0;
    }

    public void CreateAnchors(Island island)
    {
        BoundsInt bounds = new BoundsInt((Vector3Int)island.center - new Vector3Int(island.size, island.size), new Vector3Int(island.size * 2, island.size * 2));
        Vector2Int center = island.center;

        int perSide = anchorAmount / 4;

        int index = 0;

        // LEFT side
        for (int i = 0; i < perSide; i++)
        {
            int y = Mathf.RoundToInt(Mathf.Lerp(bounds.yMin, bounds.yMax - 1, (i + 0.5f) / perSide));
            PlaceAnchor(new Vector3Int(bounds.xMin, y, 0), center, index);
            index++;
        }

        // RIGHT side
        for (int i = 0; i < perSide; i++)
        {
            int y = Mathf.RoundToInt(Mathf.Lerp(bounds.yMin, bounds.yMax - 1, (i + 0.5f) / perSide));
            PlaceAnchor(new Vector3Int(bounds.xMax - 1, y, 0), center, index);
            index++;
        }

        // BOTTOM side
        for (int i = 0; i < perSide; i++)
        {
            int x = Mathf.RoundToInt(Mathf.Lerp(bounds.xMin, bounds.xMax - 1, (i + 0.5f) / perSide));
            PlaceAnchor(new Vector3Int(x, bounds.yMin, 0), center, index);
            index++;
        }

        // TOP side
        for (int i = 0; i < perSide; i++)
        {
            int x = Mathf.RoundToInt(Mathf.Lerp(bounds.xMin, bounds.xMax - 1, (i + 0.5f) / perSide));
            PlaceAnchor(new Vector3Int(x, bounds.yMax - 1, 0), center, index);
            index++;
        }
    }

    private void PlaceAnchor(Vector3Int start, Vector2Int center, int index)
    {
        Vector2 current = (Vector3)start;

        while (current != (Vector2)center)
        {
            Vector3Int cell = Vector3Int.RoundToInt(current);

            if (MapDisplay.Instance.groundTilemap.HasTile(cell) && !MapDisplay.Instance.waterTilemap.HasTile(cell))
            {
                Vector3 spawnPos = MapDisplay.Instance.groundTilemap.GetCellCenterWorld(cell);
                GameObject a = Instantiate(anchorPrefab, spawnPos, Quaternion.identity, transform);
                anchorSounds[index] = a.GetComponent<AudioSource>();
                anchorSounds[index].volume = 0;
                anchors[index] = a.transform;

                break;
            }

            current = Vector2.MoveTowards(current, center, 1f);
        }
    }

    private void FixedUpdate()
    {
        if (!MapDisplay.finishedLoading) return;

        if (player.instance.currentIsland.biome == (int)MapGenerator.biome.vale)
        {
            valeScreenRipple.SetActive(true);
            MapDisplay.Instance.lavaTilemap.color = valeLavaColor;

            if (!lostSoul && !escapingVale)
            {
                int closestIndex = GetClosestAnchor();

                if (closestAnchor != closestIndex)
                {
                    if (closestAnchor != -1)
                        anchorSounds[closestAnchor].DOFade(0, 2);

                    closestAnchor = closestIndex;
                    anchorSounds[closestIndex].DOFade(1, 2);
                }

            }
            else
            {
                if (lostSoul)
                {
                    foreach (AudioSource s in anchorSounds)
                    {
                        s.volume = 0;
                    }

                }
            }
        }
        else
        {
            MapDisplay.Instance.lavaTilemap.color = lavaDefaultColor;

            valeScreenRipple.SetActive(false);
        }

    }

    private void Update()
    {
        for(int i = 0; i < valeInventoryItems.Count; i++)
        {
            if (valeInventoryItems[i] == null) continue;

            valeInventoryItems[i].rect.localPosition = new Vector3(Mathf.Sin((Time.time + i*3f) / 2.2f) * 8f, - 59.5f * i);
        }
    }

    int GetClosestAnchor()
    {
        int bestTarget = 0;
        float closestDistanceSqr = Mathf.Infinity;
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null)
            {
                if (Vector2.Distance(player.instance.transform.position, anchors[i].position) < closestDistanceSqr)
                {
                    closestDistanceSqr = Vector2.Distance(player.instance.transform.position, anchors[i].position);
                    bestTarget = i;
                }

            }
        }

        return bestTarget;
    }

    public async void EscapeVale()
    {
        escapingVale = true;

        foreach (AudioSource s in anchorSounds)
        {
            s.DOFade(0, 1);
        }

        while(player.instance.currentIsland.biome == (int)MapGenerator.biome.vale)
        {
            await Task.Yield();
        }

        await Task.Delay(2000);

        escapingVale = false;

    }

    public async void DropValeInvItems()
    {
        for (int i = 0; i < valeInventoryItems.Count; i++)
        {
            NetworkId boatId = new NetworkId();
            if(TileEntity.currentSpawnPoint && TileEntity.currentSpawnPoint.boatParent)
            {
                boatId = TileEntity.currentSpawnPoint.boatParent.view.Id;
            }

            ItemData itemData = new ItemData(0);
            if(valeInventoryItems[i].itemInSlot.uniqueType != item.UniqueType.none && valeInventoryItems[i].itemInSlot.usesTier)
            {
                itemData = inventory.CreateItemData(valeInventoryItems[i].itemInSlot, 3);
            }

            inventory.instance.RPC_InstantiateItem(valeInventoryItems[i].itemInSlot.itemId, (short)valeInventoryItems[i].itemCountInSlot, player.instance.transform.position + (new Vector3(Random.Range(-1, 1), Random.Range(-1, 1)).normalized / 10), Random.Range(0,360),
                player.instance.transform.position, Vector3.zero, (short)valeInventoryItems[i].itemInSlot.maxDurability, inventory.ItemDataToString(itemData), SingletonRunner.runner.LocalPlayer, boatId,0);

            await Task.Delay(100);
        }
        ClearInventory(false);
    }

    public void ReturnSoulItems()
    {
        for (int i = 0; i < itemIds.Length; i++)
        {
            if (itemIds[i] == null || !gameManager.instance.itemDictionary.ContainsKey(itemIds[i]))
            {
                continue;
            }

            //fix execution 0 durability
            if (itemIds[i] != "null" && gameManager.instance.itemDictionary[itemIds[i]].durabilityEmptyItem && itemDurabilities[i] == 0)
            {
                itemIds[i] = gameManager.instance.itemDictionary[itemIds[i]].durabilityEmptyItem.itemId;
            }

            if (itemIds[i] != null && gameManager.instance.itemDictionary[itemIds[i]] != null && i < inventory.instance.slots.Length)
            {
                inventory.instance.removeItem(inventory.instance.slots[i]);
                inventory.instance.addItemInSlot(gameManager.instance.itemDictionary[itemIds[i]], itemCounts[i], itemDurabilities[i],itemDatas[i], inventory.instance.slots[i], inventory.instance.slots[i].transform.position);
            }
        }

        //armor
        if (itemIds[itemIds.Length - 4] != null && gameManager.instance.itemDictionary.ContainsKey(itemIds[itemIds.Length - 4]) && gameManager.instance.itemDictionary[itemIds[itemIds.Length - 4]] != null)
        {
            inventory.instance.removeItem(inventory.instance.headSlot);
            inventory.instance.addItemInSlot(gameManager.instance.itemDictionary[itemIds[itemIds.Length - 4]], itemCounts[itemIds.Length - 4], itemDurabilities[itemIds.Length - 4], itemDatas[itemIds.Length - 4], inventory.instance.headSlot, inventory.instance.headSlot.transform.position);
        }

        if (itemIds[itemIds.Length - 3] != null && gameManager.instance.itemDictionary.ContainsKey(itemIds[itemIds.Length - 3]) && gameManager.instance.itemDictionary[itemIds[itemIds.Length - 3]] != null)
        {
            inventory.instance.removeItem(inventory.instance.chestplateSlot);
            inventory.instance.addItemInSlot(gameManager.instance.itemDictionary[itemIds[itemIds.Length - 3]], itemCounts[itemIds.Length - 3], itemDurabilities[itemIds.Length - 3], itemDatas[itemIds.Length - 3], inventory.instance.chestplateSlot, inventory.instance.chestplateSlot.transform.position);
        }

        if (itemIds[itemIds.Length - 2] != null && gameManager.instance.itemDictionary.ContainsKey(itemIds[itemIds.Length - 2]) && gameManager.instance.itemDictionary[itemIds[itemIds.Length - 2]] != null)
        {
            inventory.instance.removeItem(inventory.instance.legsSlot);
            inventory.instance.addItemInSlot(gameManager.instance.itemDictionary[itemIds[itemIds.Length - 2]], itemCounts[itemIds.Length - 2], itemDurabilities[itemIds.Length - 2], itemDatas[itemIds.Length - 2], inventory.instance.legsSlot, inventory.instance.legsSlot.transform.position);
        }
        
        if (itemIds[itemIds.Length - 1] != null && gameManager.instance.itemDictionary.ContainsKey(itemIds[itemIds.Length - 1]) && gameManager.instance.itemDictionary[itemIds[itemIds.Length - 1]] != null)
        {
            inventory.instance.removeItem(inventory.instance.charmSlot);
            inventory.instance.addItemInSlot(gameManager.instance.itemDictionary[itemIds[itemIds.Length - 1]], itemCounts[itemIds.Length - 1], itemDurabilities[itemIds.Length - 1], itemDatas[itemIds.Length - 1], inventory.instance.charmSlot, inventory.instance.charmSlot.transform.position);
        }


    }


    public void RandomSoulPosition()
    {
        soul.gameObject.SetActive(true);
        while (true)
        {
            soul.transform.position = transform.position + new Vector3Int(
                Random.Range(-MapGenerator.valeMapSize / 4, MapGenerator.valeMapSize / 4),
                Random.Range(-MapGenerator.valeMapSize / 4, MapGenerator.valeMapSize / 4));

            if(MapDisplay.Instance.hillTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position)) == null
                && MapDisplay.Instance.hillTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position) + new Vector3Int(0,1)) == null
                && MapDisplay.Instance.hillTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position) + new Vector3Int(0, -1)) == null
                    && MapDisplay.Instance.hillTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position) + new Vector3Int(1, 0)) == null
                && MapDisplay.Instance.hillTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position) + new Vector3Int(-1, 1)) == null
                &&
                MapDisplay.Instance.lavaTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position)) == null
                && MapDisplay.Instance.lavaTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position) + new Vector3Int(0, 1)) == null
                && MapDisplay.Instance.lavaTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position) + new Vector3Int(0, -1)) == null
                    && MapDisplay.Instance.lavaTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position) + new Vector3Int(1, 0)) == null
                && MapDisplay.Instance.lavaTilemap.GetTile(Vector3Int.RoundToInt(soul.transform.position) + new Vector3Int(-1, 1)) == null)
            {
                break;
            }

        }
        lostSoul = true;

        soulRecover.ResetSoul();
    }

    public void StartSoulFlash()
    {
        if (soulFlashCo != null)
            StopCoroutine(soulFlashCo);
        soulFlashCo = StartCoroutine(_SoulFlashLoop());
    }

    IEnumerator _SoulFlashLoop()
    {
        if (!MapDisplay.finishedLoading)
        {
            while (!MapDisplay.finishedLoading) yield return null;
            yield return new WaitForSeconds(12);
        }

        if (!lostSoul) yield break;

        Vector3 direction = new Vector3(soul.transform.position.x - player.instance.transform.position.x, soul.transform.position.y - player.instance.transform.position.y).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //angle is slightly randomized
        soulFlashHolder.eulerAngles = new Vector3(0, 0, angle + Random.Range(-25, 25));

        if (Vector2.Distance(player.instance.transform.position, soul.transform.position) > 15)
        {
            soulFlashSound.Play();
            DOTween.To(() => soulFlashLight.intensity, x => soulFlashLight.intensity = x, 11, 0.8f);
            yield return new WaitForSeconds(0.8f);
            DOTween.To(() => soulFlashLight.intensity, x => soulFlashLight.intensity = x, 0, 4);
        }

        yield return new WaitForSeconds(Random.Range(lightFlashDelay.x, lightFlashDelay.y));

        if (lostSoul)
        {
            StartSoulFlash();
        }

    }

    public IEnumerator _BellFlash(Vector2 bellPos)
    {
        if (player.instance.currentIsland.biome != (int)MapGenerator.biome.vale) yield break;

        Vector3 direction = new Vector3(bellPos.x - player.instance.transform.position.x, bellPos.y - player.instance.transform.position.y).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        bellFlashHolder.eulerAngles = new Vector3(0, 0, angle);

        if (Vector2.Distance(player.instance.transform.position, bellPos) > 10)
        {
            DOTween.To(() => bellFlashLight.intensity, x => bellFlashLight.intensity = x, 15 - (Vector2.Distance(player.instance.transform.position, bellPos) / 80), 0.3f);
            yield return new WaitForSeconds(0.3f);
            DOTween.To(() => bellFlashLight.intensity, x => bellFlashLight.intensity = x, 0, 2);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }
    }

    public itemPickup GetValeUnique()
    {
        foreach (itemPickup i in treeRPCs.Instance.uniqueItems)
        {
            if (i.gameObject.activeSelf && i.Item.uniqueType == item.UniqueType.breaker)
            {
                return i;
            }
        }
        return null;
    }

    public IEnumerator _SoulTrackerFlash()
    {
        if (player.instance.currentIsland.biome != (int)MapGenerator.biome.vale) yield break;

        itemPickup unique = GetValeUnique();

        if (unique == null)
        {
            if (trackersActive)
            {
                trackersActive = false;
                foreach (Bell b in treeRPCs.Instance.bells)
                {
                    if (b.soulTracker)
                    {
                        b.SetTrackerActive(false);
                    }
                }
            }

        }
        else
        {
            if (!trackersActive)
            {
                trackersActive = true;
                foreach (Bell b in treeRPCs.Instance.bells)
                {
                    if (b.soulTracker)
                    {
                        b.SetTrackerActive(true);
                    }
                }
            }
            Vector3 direction = new Vector3(unique.transform.position.x - player.instance.transform.position.x, unique.transform.position.y - player.instance.transform.position.y).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            trackerFlashHolder.eulerAngles = new Vector3(0, 0, angle);

            trackerFlashSound.Play();
            DOTween.To(() => trackerFlashLight.intensity, x => trackerFlashLight.intensity = x, 15 - (Vector2.Distance(player.instance.transform.position, unique.transform.position) / 80), 0.3f);
            yield return new WaitForSeconds(0.3f);
            DOTween.To(() => trackerFlashLight.intensity, x => trackerFlashLight.intensity = x, 0, 2);
            yield return new WaitForSeconds(3.2f);
        }
    }

    public void SetSoulItems(string[] itemIds, int[] itemCounts, int[] itemDurabilities, ItemData[] itemDatas)
    {
        this.itemIds = itemIds;
        this.itemCounts = itemCounts;
        this.itemDurabilities = itemDurabilities;
        this.itemDatas = itemDatas;
    }

    public void SoulRecovered()
    {
        lostSoul = false;
        SoulTextShow(true);
    }

    //shows when you have your soul
    public async void SoulTextShow(bool recoveringSoul)
    {
        if (recoveringSoul)
            playerHealth.instance.currentHealth = playerHealth.instance.maxHealth;

        menu.instance.valeText2.DOKill();
        menu.instance.valeText2.DOFade(0, 1);

        await Task.Delay(2000);
        escapingVale = false;

        if (recoveringSoul)
        {
            menu.instance.soulRecoveredText.text = "Soul Recovered";
            menu.instance.soulRecoveredText.color = new Color(0, 225f / 255f, 1);
        }
        else
        {
            menu.instance.soulRecoveredText.text = "The Vale";
            menu.instance.soulRecoveredText.color = Color.black;
        }

        menu.instance.soulRecoveredGroup.DOFade(1, 1);
        menu.instance.soulRecoveredGroup.transform.localScale = Vector3.one;

        menu.instance.soulRecoveredGroup.transform.DOScale(1.2f, 13);

        await Task.Delay(7000);

        if (!recoveringSoul) await Task.Delay(2000);

        menu.instance.soulRecoveredGroup.DOFade(0, 2);
        menu.instance.followLightText.text = "Follow The Sound";
        await Task.Delay(2000);

        if (player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
            return;

        menu.instance.valeText2.DOFade(1, 3);
    }


    public void SaveData(GameData data)
    {
        data.soulLocation = Vector2Int.RoundToInt(soul.transform.position);

        data.soulItemIds = itemIds;
        data.soulItemCounts = itemCounts;
        data.soulItemDurabilities = itemDurabilities;
        
        string[] itemDataStr = new string[itemDatas.Length];
        for(int i = 0; i < itemDatas.Length; i++)
        {
            itemDataStr[i] = inventory.ItemDataToString(itemDatas[i]);
        }
        data.soulItemDatas = itemDataStr;

        data.lostSoul = lostSoul;

        SerializableDictionary<string, int> savedValeInv = new SerializableDictionary<string, int>();
        if(player.instance.currentIsland.biome == (int)MapGenerator.biome.vale)
        {
            foreach (inventorySlot slot in valeInventoryItems)
            {
                savedValeInv.TryAdd(slot.itemInSlot.itemId, slot.itemCountInSlot);
            }
        }
        data.valeInventory = savedValeInv;
    }

    public void LoadData(GameData data)
    {
        soul.transform.position = (Vector2)data.soulLocation;

        itemIds = data.soulItemIds;
        itemCounts = data.soulItemCounts;
        itemDurabilities = data.soulItemDurabilities;

        if (data.soulItemDatas != null)
        {
            itemDatas = new ItemData[data.soulItemDatas.Length];
            for (int i = 0; i < itemDatas.Length; i++)
            {
                itemDatas[i] = inventory.StringToItemData(data.soulItemDatas[i]);
            }
        }

        lostSoul = data.lostSoul;

        if (lostSoul)
        {
            if (!data.dead)
                StartSoulFlash();
            soul.gameObject.SetActive(true);
        }

        foreach (KeyValuePair<string, int> kv in data.valeInventory)
        {
            AddInventoryItem(gameManager.instance.itemDictionary[kv.Key], kv.Value, true);
        }
    }

    public async void RemoveNearbyEnemies()
    {
        int frameCounter = 0;

        GameObject[] enemies = EnemySpawner.instance.enemies.ToArray();

        foreach (GameObject enemy in enemies)
        {
            if(enemy && Vector2.Distance(player.instance.transform.position,enemy.transform.position) <= 20)
            {
                enemyHealth e = enemy.GetComponent<enemyHealth>();

                if(!e.dead)
                    e.RPC_Die();
            }

            frameCounter++;

            if(frameCounter % 20 == 0)
            {
                await Task.Yield();
            }
        }

    }

    public item[] GetSoulItems()
    {
        List<item> i = new List<item>();
        foreach (string item in itemIds)
        {
            i.Add(gameManager.instance.itemDictionary[item]);
        }
        return i.ToArray();
    }

    public void AddInventoryItem(item i, int count, bool disableSound)
    {
        if (!disableSound)
        {
            pickupSound.Play();
        }

        if(slotsGroup.alpha == 0)
        slotsGroup.DOFade(slotsGroupAlpha, 0.5f);

        bool found = false;
        foreach (inventorySlot slot in valeInventoryItems)
        {
            if (slot.itemInSlot == i)
            {
                slot.itemCountInSlot += count;
                slot.itemCountInSlot = Mathf.Clamp(slot.itemCountInSlot, 0, 500);
                
                slot.transform.DOKill();
                slot.transform.localScale = Vector2.one;
                slot.transform.DOShakeScale(0.25f, 0.5f, 1, 90, true, ShakeRandomnessMode.Full);
                
                found = true;
                break;
            }
        }
        if (!found)
        {
            inventorySlot newSlot = Instantiate(valeInventorySlotPref, slotHolder).GetComponent<inventorySlot>();
            inventory.instance.addItemInSlot(i, count, i.maxDurability, new ItemData(0), newSlot, newSlot.transform.position, true);
            newSlot.itemCountInSlot = Mathf.Clamp(newSlot.itemCountInSlot, 0, 500);
            newSlot.transform.localScale = Vector2.zero;
            newSlot.transform.DOScale(1, 0.3f);

            //to do - soul breaker tier

            valeInventoryItems.Add(newSlot);
        }
    }

    public void ClearInventory(bool resetUniques)
    {
        if (resetUniques)
        {
            List<item> heldUniques = new List<item>();

            foreach (inventorySlot i in valeInventoryItems)
            {
                if (i && i.itemInSlot.uniqueType != item.UniqueType.none) heldUniques.Add(i.itemInSlot);
            }


            foreach (itemPickup iP in treeRPCs.Instance.uniqueItems)
            {
                if (heldUniques.Contains(iP.Item))
                {
                    iP.RPC_RespawnUnique();
                }

            }
        }

        slotsGroup.DOFade(0, 0.75f);
        foreach (inventorySlot slot in valeInventoryItems)
        {
            Destroy(slot.gameObject,1);
        }

        valeInventoryItems.Clear();
    }

}

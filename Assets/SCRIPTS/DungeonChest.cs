using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DungeonChest : MonoBehaviour
{
    public int chestId;

    public static DungeonChest currentDungeonChest;
    public GameObject indicatorPrefab;
    public Vector2 indicatorOffset;
    public Vector2 interactBoxSize;
    public LayerMask mainPlayerLayer;
    GameObject indicatorInstance;
    Animator indicatorAnim;
    TextMeshPro indicatorText;
    bool playerInRange;
    public string[] chestItems;
    public int[] chestCounts;
    public int[] chestDurabilities;
    public ItemData[] chestItemDatas;
    public bool generated;
    System.Random rng;
    public LootTable[] lootTables;
    public Vector2Int rolls;
    public bool alreadyOpened;
    public Sprite[] sprites;
    public SpriteRenderer sprite;

    public List<LootEntry> lootItemsInChest = new List<LootEntry>();
    List<LootEntry> totalEntries = new List<LootEntry>();

    private void Start()
    {
        GenerateLoot();
    }

    private void Update()
    {
        Collider2D playerCheck;
        playerCheck = Physics2D.OverlapBox(transform.position, interactBoxSize, 0, mainPlayerLayer);

        //no pick up item and use tile at the same time
        if (itemPickup.closestItem || Chat.Instance.chatOpen)
            playerCheck = null;

        if (playerCheck && !player.instance.currentBoat && !playerHealth.instance.dead && !player.instance.pilotingBoat && !Chat.Instance.chatOpen)
        {
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
            destroyIndicator();
        }

        if (!alreadyOpened)
        {
            if(currentDungeonChest == this)
            {
                sprite.sprite = sprites[1];
            }
            else
            {
                sprite.sprite = sprites[0];
            }
        }
        else
        {
            if (currentDungeonChest == this)
            {
                sprite.sprite = sprites[3];
            }
            else
            {
                sprite.sprite = sprites[2];
            }
        }

    }

    void GenerateLoot()
    {
        if (generated || lootTables == null)
            return;

        rng = new System.Random(
            seed.instance.currentSeed +
            DataPersistanceManager.userId.GetHashCode() +
            transform.position.GetHashCode()
        );

        int rolls = rng.Next(this.rolls.x, this.rolls.y + 1);

        chestItems = new string[inventory.instance.chestSlots.Length];
        chestCounts = new int[inventory.instance.chestSlots.Length];
        chestDurabilities = new int[inventory.instance.chestSlots.Length];
        chestItemDatas = new ItemData[inventory.instance.chestSlots.Length];

        foreach(LootTable table in lootTables)
        {
            foreach(LootEntry e in table.lootEntries)
            {
                totalEntries.Add(e);
            }
        }

        for (int r = 0; r < rolls; r++)
        {
            LootEntry selected = GetRandomLootEntry();
            if (selected == null)
                continue;
            
            while (selected.onePerChest && lootItemsInChest.Contains(selected))
            {
                selected = GetRandomLootEntry();
            }

            int slotIndex = GetEmptyChestSlot();
            if (slotIndex == -1)
                break;

            lootItemsInChest.Add(selected);

            item selectedItem = selected.item;
            if(selected.altItems.Count > 0)
            {
                List<item> possibleItems = new List<item>(selected.altItems);
                possibleItems.Add(selected.item);

                selectedItem = possibleItems[rng.Next(0, possibleItems.Count)];
            }

            // Generate normalized value
            float percent = Mathf.Lerp(
                selected.minDurability,
                selected.maxDurability,
                (float)rng.NextDouble()
            );

            // Convert to real durability
            int realDurability = Mathf.RoundToInt(
                percent * selectedItem.maxDurability
            );

            // Clamp for safety
            realDurability = Mathf.Clamp(realDurability, 0, selectedItem.maxDurability);

            chestItems[slotIndex] = selectedItem.itemId;
            chestDurabilities[slotIndex] = realDurability;
            chestCounts[slotIndex] = rng.Next(
                selected.minCount,
                selected.maxCount + 1
            );

            if (selectedItem.usesDurability)
                chestCounts[slotIndex] = 1;

            if (selectedItem.usesTier)
            {
                chestItemDatas[slotIndex] = inventory.CreateItemData(selectedItem);
            }

        }

        generated = true;
    }

    public void spawnIndicator()
    {
        if (indicatorInstance == null)
        {
            indicatorInstance = Instantiate(indicatorPrefab, transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
        }
        else
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);

            indicatorInstance = Instantiate(indicatorPrefab, transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
        }
        indicatorAnim = indicatorInstance.GetComponent<Animator>();
        indicatorText = indicatorInstance.GetComponent<TextMeshPro>();

        indicatorText.text = "Open";
    }

    public void destroyIndicator()
    {
        if (indicatorInstance != null)
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);
        }

    }

    void LoadChestItems()
    {
        alreadyOpened = true;

        for (short i = 0; i < inventory.instance.chestSlots.Length; i++)
        {
            if (chestItems[i] == string.Empty || chestItems[i] == null)
                chestItems[i] = "null";

            inventory.instance.removeItem(inventory.instance.chestSlots[i]);
            if (chestItems[i] != "null")
                inventory.instance.addItemInSlot(gameManager.instance.itemDictionary[chestItems[i]], chestCounts[i], chestDurabilities[i],chestItemDatas[i], inventory.instance.chestSlots[i], inventory.instance.chestSlots[i].transform.position);
        }

    }

    public void CloseChest()
    {
        currentDungeonChest = null;
        alreadyOpened = true;

        for (int i = 0; i < inventory.instance.chestSlots.Length; i++)
        {
            if (inventory.instance.chestSlots[i].itemInSlot)
            {
                chestItems[i] = inventory.instance.chestSlots[i].itemInSlot.itemId;
                chestCounts[i] = inventory.instance.chestSlots[i].itemCountInSlot;
                chestDurabilities[i] = inventory.instance.chestSlots[i].durability;
            }
            else
            {
                chestItems[i] = "null";
            }

        }
    }

    void InteractPressed()
    {
        if (inventory.instance.inventoryOpen)
        {
            inventory.instance.CloseInventory();
            return;
        }

        currentDungeonChest = this;
        LoadChestItems();
        inventory.instance.OpenInventory(inventory.interfaceType.chest);
    }

    LootEntry GetRandomLootEntry()
    {
        int totalWeight = 0;

        foreach (var entry in totalEntries)
            totalWeight += Mathf.Max(0, entry.rarityWeight);

        if (totalWeight <= 0)
            return null;

        int roll = rng.Next(0, totalWeight);

        int cumulative = 0;

        foreach (var entry in totalEntries)
        {
            cumulative += entry.rarityWeight;

            if (roll < cumulative)
                return entry;
        }

        return null;
    }

    int GetEmptyChestSlot()
    {
        List<int> emptySlots = new List<int>();

        for (int i = 0; i < chestItems.Length; i++)
        {
            if (string.IsNullOrEmpty(chestItems[i]) || chestItems[i] == "null")
            {
                emptySlots.Add(i);
            }
        }

        if (emptySlots.Count == 0)
            return -1;

        int randomIndex = rng.Next(0, emptySlots.Count);

        return emptySlots[randomIndex];
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, interactBoxSize);


    }
}

[System.Serializable]
public class LootEntry
{
    public item item;
    public List<item> altItems;

    [Header("Chance Weight (Higher = More Likely)")]
    public int rarityWeight = 1;
    public bool onePerChest;

    [Header("Stack Amount")]
    public int minCount = 1;
    public int maxCount = 1;

    [Header("Durability")]
    public float minDurability = 1;
    public float maxDurability = 1;
}
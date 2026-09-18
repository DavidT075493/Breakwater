using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class inventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public RectTransform rect;
    public item itemInSlot;
    public int itemCountInSlot;
    public int durability;
    public ItemData itemData;
    public static item itemInMouse;
    public static int itemCountInMouse;
    public static int durabilityInMouse;
    public static ItemData itemDataInMouse;
    public GameObject icon;
    public static inventorySlot selectedSlot;
    public static inventorySlot equippedSlot;
    //public int IndexInArray;

    public GameObject itemCountText;
    public TextMeshProUGUI itemCountTextInstance;
    public Animator anim;

    public const float doubleClickTime = 0.25f;
    float clickTimer;
    public Slider durabilitySlider;
    public Image brokenImage;
    public Transform slotIcon;
    public Image durabilitySliderImage;

    public Image tierHighlight;
    public Sprite[] tierHighlightSprites;
    public Color[] tierHighlightColors;

    [Header("Furnace")]
    public bool fuelSlot;
    public bool furnaceIngredientSlot;
    public bool takeOnly;
    public bool reactorSlot;
    //only accept cesium or uranium
    public bool uraniumPurifier;
    [Header("Armor")]
    public bool armorSlot;
    public enum armorType { head, chest, legs, charm }
    public armorType ArmorType;
    public AudioSource placeSound, takeSound;

    [Header("Anvil")]
    public bool anvilToolSlot;
    public bool anvilMaterialSlot;
    private bool broken;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        anim = GetComponent<Animator>();

        if (brokenImage)
            brokenImage.fillAmount = 0;

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventory.instance.inventoryOpen)
        {
            selectedSlot = this;
            anim.SetBool("Selected", true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (inventory.instance.inventoryOpen)
        {
            selectedSlot = null;
            anim.SetBool("Selected", false);
        }
    }

    public bool ValidPlaceInSlot(item i, int durability)
    {
        //furnace slot checks
        if (takeOnly) return false;

        if (fuelSlot)
        {
            if (uraniumPurifier)
            {
                if (!i.onlyInPurifier || !i.fuel) return false;
            }
            else
            {
                if (i.onlyInPurifier || !i.fuel) return false;
            }
        }
        if (furnaceIngredientSlot)
        {
            if (uraniumPurifier)
            {
                if (!i.onlyInPurifier || !i.smeltResult) return false;
            }
            else
            {
                if (i.onlyInPurifier || !i.smeltResult) return false;
            }
        }
        if (reactorSlot)
        {
            if (!i.reactorFuel) return false;
        }

        //armor slot check
        if (armorSlot)
        {
            if (durability <= 0) return false;

            if (ArmorType == armorType.head && i.type != item.itemType.armorHead) return false;
            if (ArmorType == armorType.chest && i.type != item.itemType.armorChest) return false;
            if (ArmorType == armorType.legs && i.type != item.itemType.armorLegs) return false;
            if (ArmorType == armorType.charm && i.type != item.itemType.charm) return false;
        }

        if (anvilToolSlot)
        {
            if (!i.usesDurability) return false;
        }
        if (anvilMaterialSlot)
        {
            if (!i.isRepairMaterial && i != inventory.instance.upgradeTemplateItem && !i.usesDurability && i.soulRepairAmount == 0)
                return false;
        }

        return true;
    }

    public static inventorySlot[] FurnaceSlots(bool purifier)
    {
        List<inventorySlot> slots = new List<inventorySlot>();
        if (!purifier)
        {
            slots.Add(inventory.instance.furnaceFuel);
            slots.Add(inventory.instance.furnaceIngr);
            slots.Add(inventory.instance.furnaceResult);
        }
        else
        {
            slots.Add(inventory.instance.purifierFuel);
            slots.Add(inventory.instance.purifierIngr);
            slots.Add(inventory.instance.purifierResult);
        }
        return slots.ToArray();

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!inventory.instance.inventoryOpen || selectedSlot != this || inventory.instance.statsOpen) return;

        if (itemCountTextInstance == null && icon != null)
        {
            itemCountTextInstance = icon.GetComponentInChildren<TextMeshProUGUI>();
        }

        //left click
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            //click to pick up an item
            if (itemInSlot != null && itemInMouse == null)
            {
                if (takeOnly && inventory.instance.anvilUI.activeInHierarchy)
                {
                    inventory.instance.AnvilRepairTake(inventory.instance.anvilMaterialSlot.itemInSlot == inventory.instance.upgradeTemplateItem);
                }

                clickTimer = doubleClickTime;

                inventory.instance.addItemInMouse(itemInSlot, itemCountInSlot, durability, itemData, icon.transform.position);

                //shift click
                if (InputManager.actions["Shift Click"].started || InputManager.actions["Shift Click"].held)
                {
                    inventorySlot[] toSlots = null;

                    //chest
                    if (inventory.instance.chestUi.activeSelf)
                    {
                        if (!inventory.instance.chestSlots.Contains(this))
                        {
                            toSlots = inventory.instance.chestSlots;
                        }
                        else
                        {
                            toSlots = (inventory.instance.slots);
                        }

                    }
                    //furnace / purifier
                    else if (inventory.instance.furnaceUI.activeSelf || inventory.instance.purifierUI.activeSelf)
                    {
                        if (!FurnaceSlots(inventory.instance.purifierUI.activeSelf).Contains(this))
                        {
                            toSlots = FurnaceSlots(inventory.instance.purifierUI.activeSelf);
                        }
                        else
                        {
                            toSlots = (inventory.instance.slots);
                        }

                    }
                    //speedboat fuel
                    else if (inventory.instance.speedboatUI.activeSelf)
                    {
                        if (inventory.instance.speedboatFuelSlot != this)
                        {
                            toSlots = new inventorySlot[] { inventory.instance.speedboatFuelSlot };
                        }
                        else
                        {
                            toSlots = (inventory.instance.slots);
                        }
                    }
                    else if (inventory.instance.battleshipUI.activeSelf)
                    {
                        if (inventory.instance.battleshipFuelSlot != this)
                        {
                            toSlots = new inventorySlot[] { inventory.instance.battleshipFuelSlot };
                        }
                        else
                        {
                            toSlots = (inventory.instance.slots);
                        }

                    }
                    else if (inventory.instance.anvilUI.activeSelf)
                    {
                        if (inventory.instance.anvilToolSlot != this && inventory.instance.anvilMaterialSlot != this && inventory.instance.anvilOutputSlot != this)
                        {
                            toSlots = new inventorySlot[] { inventory.instance.anvilToolSlot, inventory.instance.anvilMaterialSlot };
                        }
                        else
                        {
                            toSlots = (inventory.instance.slots);
                        }

                    }
                    else if (inventory.instance.normalUI.activeSelf)
                    {
                        inventorySlot[] armorSlots = new inventorySlot[]
                        {
                            inventory.instance.headSlot,
                            inventory.instance.chestplateSlot,
                            inventory.instance.legsSlot,
                            inventory.instance.charmSlot,
                        };

                        if (!armorSlots.Contains(this))
                        {
                            toSlots = armorSlots;
                        }
                        else
                        {
                            toSlots = (inventory.instance.slots);
                        }
                    }

                    if (toSlots != null)
                    {
                        ShiftClick(toSlots);
                        return;
                    }

                }
                itemInSlot = null;
                itemCountTextInstance = null;
                Destroy(icon);
                icon = null;
                itemCountInSlot = 0;

                if (takeSound) takeSound.Play();


            }
            else if (itemInMouse != null)
            {

                //put down an item on empty slot
                if (itemInSlot == null)
                {
                    //double click to get all of a type instead
                    if (clickTimer > 0)
                    {
                        int toAdd = 0;

                        //check chests
                        if (inventory.instance.chestUi.activeInHierarchy)
                        {
                            foreach (inventorySlot slot in inventory.instance.chestSlots)
                            {
                                if (slot.itemInSlot == itemInMouse)
                                {
                                    if (itemCountInMouse + slot.itemCountInSlot <= itemInMouse.maxStackSize)
                                    {
                                        toAdd += slot.itemCountInSlot;
                                        inventory.instance.removeItem(slot);
                                    }
                                    else
                                    {
                                        int amtToTake = itemInMouse.maxStackSize - itemCountInMouse;
                                        slot.itemCountInSlot -= amtToTake;
                                        toAdd += amtToTake;
                                        break;
                                    }

                                }

                            }
                        }
                        //check furnace
                        if (inventory.instance.furnaceUI.activeInHierarchy || inventory.instance.purifierUI.activeInHierarchy)
                        {
                            inventorySlot[] furnaceSlots = FurnaceSlots(inventory.instance.purifierUI.activeInHierarchy);
                            foreach (inventorySlot slot in furnaceSlots)
                            {
                                if (slot.itemInSlot == itemInMouse)
                                {
                                    if (itemCountInMouse + slot.itemCountInSlot <= itemInMouse.maxStackSize)
                                    {
                                        toAdd += slot.itemCountInSlot;
                                        inventory.instance.removeItem(slot);
                                    }
                                    else
                                    {
                                        int amtToTake = itemInMouse.maxStackSize - itemCountInMouse;
                                        slot.itemCountInSlot -= amtToTake;
                                        toAdd += amtToTake;
                                        break;
                                    }

                                }

                            }
                            TileEntity.currentFurnace.SaveFurnaceItems();
                        }
                        //check inventory
                        foreach (inventorySlot slot in inventory.instance.slots)
                        {
                            if (slot.itemInSlot == itemInMouse)
                            {
                                if (itemCountInMouse + slot.itemCountInSlot <= itemInMouse.maxStackSize)
                                {
                                    toAdd += slot.itemCountInSlot;
                                    inventory.instance.removeItem(slot);
                                }
                                else
                                {
                                    int amtToTake = itemInMouse.maxStackSize - itemCountInMouse;
                                    slot.itemCountInSlot -= amtToTake;
                                    toAdd += amtToTake;
                                    break;
                                }

                            }

                        }
                        itemCountInMouse += toAdd;

                        if (itemCountInMouse > itemInMouse.maxStackSize)
                        {
                            int removeAmt = itemCountInMouse - itemInMouse.maxStackSize;
                            itemCountInMouse -= removeAmt;
                            inventory.instance.AddItem(itemInMouse, removeAmt, 1, new ItemData(0));
                        }

                        if (toAdd > 0)
                        {
                            if (placeSound) placeSound.Play();
                            if (takeSound) takeSound.Play();
                        }

                        return;
                    }

                    //furnace slot check
                    if (!ValidPlaceInSlot(itemInMouse, durabilityInMouse)) return;

                    if (placeSound) placeSound.Play();

                    //put down an item on empty slot
                    itemInSlot = itemInMouse;
                    itemInMouse = null;

                    itemCountInSlot = itemCountInMouse;
                    itemCountInMouse = 0;

                    durability = durabilityInMouse;
                    SetBrokenInstant();
                    itemData = itemDataInMouse;
                    durabilityInMouse = 0;

                    itemCountTextInstance = inventory.instance.itemDragCountText;
                    inventory.instance.itemDragCountText = null;

                    icon = inventory.instance.itemDragIcon;
                    icon.transform.SetParent(transform);
                    icon.transform.SetAsFirstSibling();
                    icon.transform.position = inventory.instance.itemDragIcon.transform.position;
                    icon.transform.localScale = new Vector2(inventory.itemIconScale, inventory.itemIconScale);
                    inventory.instance.itemDragIcon = null;

                    if (itemCountInSlot == 1)
                    {
                        itemCountTextInstance.text = string.Empty;
                    }

                }

                //put down items on a slot with the same item
                else if (itemInSlot == itemInMouse)
                {
                    //take items out of a takeonly slot instead of adding
                    if (takeOnly)
                    {
                        itemInMouse = itemInSlot;
                        //under stack size
                        if (itemCountInMouse + itemCountInSlot <= itemInSlot.maxStackSize)
                        {
                            itemCountInMouse += itemCountInSlot;
                            itemCountInSlot = 0;
                        }
                        //over stack size
                        else
                        {
                            int amtToTake = itemInSlot.maxStackSize - itemCountInMouse;
                            itemCountInSlot -= amtToTake;
                            itemCountInMouse += amtToTake;
                        }

                        if (takeSound) placeSound.Play();

                        return;
                    }

                    if (placeSound) placeSound.Play();

                    //under the stack size
                    if (itemCountInMouse + itemCountInSlot <= itemInSlot.maxStackSize)
                    {
                        itemInMouse = null;
                        itemCountInSlot += itemCountInMouse;
                        itemCountInMouse = 0;

                        if (itemCountTextInstance != null)
                            itemCountTextInstance.text = itemCountInSlot.ToString();
                        if (itemCountInSlot == 1 && itemCountTextInstance)
                        {
                            itemCountTextInstance.text = string.Empty;
                        }

                        Destroy(inventory.instance.itemDragIcon);

                    }
                    //over the stack size
                    else if (!(itemCountInSlot == itemInSlot.maxStackSize))
                    {
                        while (itemCountInSlot < itemInSlot.maxStackSize)
                        {
                            itemCountInSlot += 1;
                            itemCountInMouse -= 1;
                        }

                        itemCountTextInstance.text = itemCountInSlot.ToString();
                        if (itemCountInSlot == 1)
                        {
                            itemCountTextInstance.text = string.Empty;
                        }

                        inventory.instance.itemDragCountText.text = itemCountInMouse.ToString();
                    }
                    else
                        //switch 2 of the same items between slot and mouse
                        if (itemInSlot && itemCountInSlot == itemInSlot.maxStackSize && itemInMouse != null)
                        {
                            int slotDurability = durability;
                            ItemData slotItemData = itemData;

                            itemCountInSlot = itemCountInMouse;
                            itemCountTextInstance.text = itemCountInSlot.ToString();
                            durability = durabilityInMouse;
                            SetBrokenInstant();
                            itemData = itemDataInMouse;

                            itemCountInMouse = itemInSlot.maxStackSize;
                            inventory.instance.itemDragCountText.text = itemCountInMouse.ToString();
                            durabilityInMouse = slotDurability;
                            itemDataInMouse = slotItemData;

                            if (itemCountInSlot == 1)
                            {
                                itemCountTextInstance.text = string.Empty;
                            }
                            if (itemCountInMouse == 1)
                            {
                                inventory.instance.itemDragCountText.text = string.Empty;
                            }

                            //update item name box
                            if (selectedSlot && selectedSlot.itemInSlot)
                            {
                                inventory.instance.SetItemNameBoxText(selectedSlot.itemInSlot, selectedSlot.durability, selectedSlot.itemData, selectedSlot.transform.position, false);
                            }
                        }

                }



            }
            //switch 2 different items
            if (itemInMouse && itemInSlot && itemInMouse != itemInSlot)
            {
                //repair unique items
                if (itemInMouse.soulRepairAmount > 0 && itemInSlot.uniqueType != item.UniqueType.none && durability < itemInSlot.maxDurability)
                {
                    int durabilityPerItem = itemInMouse.soulRepairAmount;

                    durabilityPerItem = Mathf.RoundToInt(durabilityPerItem * itemInSlot.uniqueRepairMultiplier);

                    if (itemCountInMouse - inventory.instance.soulRepairAmount > 0)
                    {
                        int soulStartAmount = itemCountInMouse;
                        Vector2 itemPos = inventory.instance.itemDragIcon.transform.position;
                        string itemId = itemInMouse.itemId;
                        inventory.instance.removeMouseItem();
                        inventory.instance.addItemInMouse(gameManager.instance.itemDictionary[itemId], soulStartAmount - inventory.instance.soulRepairAmount, 1, new ItemData(0), itemPos);
                    }
                    else
                    {
                        inventory.instance.removeMouseItem();
                    }
                    int dur = durability;

                    durability += inventory.instance.soulRepairAmount * durabilityPerItem;

                    if (dur <= 0 && equippedSlot == this)
                    {
                        HandMove.instance.RPC_SetItem(itemInSlot.itemId, false);
                        HandMove.instance.RPC_RefreshHeldItem();
                    }
                    
                    inventory.instance.repairSound.Play();
                    durability = Mathf.Clamp(durability, 0, itemInSlot.maxDurability);
                    return;
                }

                //furnace slot checks
                if (!ValidPlaceInSlot(itemInMouse, durabilityInMouse)) return;

                if (placeSound) placeSound.Play();

                item itemCurrentlyInSlot = itemInSlot;
                itemInSlot = itemInMouse;
                itemInMouse = itemCurrentlyInSlot;

                int itemCountCurrentlyInSlot = itemCountInSlot;
                itemCountInSlot = itemCountInMouse;
                itemCountInMouse = itemCountCurrentlyInSlot;

                int durabilityInSlot = durability;
                durability = durabilityInMouse;
                SetBrokenInstant();
                durabilityInMouse = durabilityInSlot;

                ItemData itemDataInSlot = itemData;
                itemData = itemDataInMouse;
                itemDataInMouse = itemDataInSlot;

                GameObject slotIcon = icon;
                icon = inventory.instance.itemDragIcon;
                inventory.instance.itemDragIcon = slotIcon;
                inventory.instance.itemDragIcon.transform.SetParent(inventory.instance.transform);
                slotIcon.transform.localScale = new Vector2(inventory.itemIconScale, inventory.itemIconScale);

                icon.transform.SetParent(transform);
                icon.transform.SetAsFirstSibling();
                icon.transform.localScale = new Vector2(inventory.itemIconScale, inventory.itemIconScale);
                icon.transform.localPosition = Vector3.zero;

                itemCountTextInstance = GetComponentInChildren<TextMeshProUGUI>();
                inventory.instance.itemDragCountText = inventory.instance.itemDragIcon.GetComponentInChildren<TextMeshProUGUI>();

                itemCountTextInstance.text = itemCountInSlot.ToString();
                inventory.instance.itemDragCountText.text = itemCountInMouse.ToString();

                if (itemCountInSlot == 1)
                {
                    itemCountTextInstance.text = string.Empty;
                }
                if (itemCountInMouse == 1)
                {
                    inventory.instance.itemDragCountText.text = string.Empty;
                }

            }



        }
        //right click
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (itemInMouse && itemInSlot && itemInMouse != itemInSlot)
            {
                //repair unique items
                if (itemInMouse.soulRepairAmount > 0 && itemInSlot.uniqueType != item.UniqueType.none && durability < itemInSlot.maxDurability)
                {
                    int durabilityPerItem = itemInMouse.soulRepairAmount;

                    durabilityPerItem = Mathf.RoundToInt(durabilityPerItem * itemInSlot.uniqueRepairMultiplier);

                    if (itemCountInMouse - 1 > 0)
                    {
                        int soulStartAmount = itemCountInMouse;
                        Vector2 itemPos = inventory.instance.itemDragIcon.transform.position;
                        string itemId = itemInMouse.itemId;
                        inventory.instance.removeMouseItem();
                        inventory.instance.addItemInMouse(gameManager.instance.itemDictionary[itemId], soulStartAmount - 1, 1, new ItemData(0), itemPos);
                    }
                    else
                    {
                        inventory.instance.removeMouseItem();
                    }
                    int dur = durability;
                    
                    durability += durabilityPerItem;
                    
                    if (dur <= 0 && equippedSlot == this)
                    {
                        HandMove.instance.RPC_SetItem(itemInSlot.itemId, false);
                        HandMove.instance.RPC_RefreshHeldItem();
                    }
                    
                    inventory.instance.repairSound.Play();

                    durability = Mathf.Clamp(durability, 0, itemInSlot.maxDurability);
                    return;
                }


            }
            else
                //click to pick up half the items
                if (itemInSlot != null && itemInMouse == null)
                {
                    if (takeSound) takeSound.Play();

                    int leavingHalf = itemCountInSlot - itemCountInSlot / 2;
                    int stayingHalf = itemCountInSlot - leavingHalf;


                    itemCountTextInstance.text = stayingHalf.ToString();
                    itemCountInSlot = stayingHalf;


                    inventory.instance.addItemInMouse(itemInSlot, leavingHalf, durability, itemData, icon.transform.position);

                    if (takeOnly && inventory.instance.anvilUI.activeInHierarchy)
                    {
                        inventory.instance.AnvilRepairTake(inventory.instance.anvilMaterialSlot.itemInSlot == inventory.instance.upgradeTemplateItem);
                    }

                    if (itemCountInSlot == 1)
                    {
                        itemCountTextInstance.text = string.Empty;
                    }

                }
                else if (itemInMouse != null)
                {
                    //min stack size
                    if (itemCountInMouse <= 0)
                    {
                        return;
                    }

                    //put down one single item on empty slot
                    if (itemInSlot == null)
                    {

                        if (!ValidPlaceInSlot(itemInMouse, durabilityInMouse)) return;

                        if (placeSound) placeSound.Play();

                        int newMouseCount = itemCountInMouse - 1;

                        inventory.instance.addItemInSlot(itemInMouse, 1, durabilityInMouse, itemDataInMouse, this, inventory.instance.itemDragIcon.transform.position);

                        itemInSlot = itemInMouse;

                        Vector2 itemPos = inventory.instance.itemDragIcon.transform.position;
                        inventory.instance.removeMouseItem();
                        inventory.instance.addItemInMouse(itemInSlot, newMouseCount, 1, itemDataInMouse, itemPos);

                        if (itemCountInSlot == 1)
                        {
                            itemCountTextInstance.text = string.Empty;
                        }

                    }
                    //put down an item on a slot with the same item
                    else if (itemInSlot == itemInMouse)
                    {
                        if (takeOnly) return;

                        //max stack size
                        if (itemCountInSlot >= itemInSlot.maxStackSize)
                        {
                            return;
                        }

                        if (placeSound) placeSound.Play();

                        itemCountInSlot += 1;
                        itemCountInMouse -= 1;

                        itemCountTextInstance.text = itemCountInSlot.ToString();
                        inventory.instance.itemDragIcon.GetComponentInChildren<TextMeshProUGUI>().text = itemCountInMouse.ToString();

                    }

                }


        }


        //save furnace items
        if ((fuelSlot || furnaceIngredientSlot || takeOnly) && TileEntity.currentFurnace)
        {
            TileEntity.currentFurnace.SaveFurnaceItems();
        }

    }
    private void Update()
    {
        if (clickTimer > 0)
        {
            clickTimer -= Time.deltaTime;
        }

        if (slotIcon) slotIcon.SetAsFirstSibling();

        if (icon && icon.transform.localPosition != Vector3.zero)
        {
            icon.transform.localPosition = Vector2.Lerp(icon.transform.localPosition, Vector2.zero, inventory.itemDragLerpSpeed * Time.deltaTime);
        }

        if (itemCountInMouse <= 0 && inventory.instance.itemDragIcon)
        {
            itemInMouse = null;
            Destroy(inventory.instance.itemDragIcon);
        }

        if (itemCountInSlot <= 0 && icon)
        {
            itemInSlot = null;
            Destroy(icon);
        }

        if (itemCountTextInstance)
        {
            itemCountTextInstance.text = itemCountInSlot.ToString();

            if (itemCountInSlot == 1)
            {
                itemCountTextInstance.text = string.Empty;
            }
        }
        if (inventory.instance.itemDragCountText)
        {
            inventory.instance.itemDragCountText.text = itemCountInMouse.ToString();

            if (itemCountInMouse == 1)
            {
                inventory.instance.itemDragCountText.text = string.Empty;
            }

        }
        if (icon && itemCountTextInstance == inventory.instance.itemDragCountText)
        {
            itemCountTextInstance = GetComponentInChildren<TextMeshProUGUI>();
            inventory.instance.itemDragCountText = inventory.instance.itemDragIcon.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (durabilitySlider)
        {
            if (itemInSlot && itemInSlot.usesDurability && (durability < itemInSlot.maxDurability || itemInSlot.maxDurability == 1))
            {
                durabilitySlider.value = (float)durability / (float)itemInSlot.maxDurability;
                durabilitySlider.gameObject.SetActive(true);
                if (durabilitySliderImage && itemInSlot.durabilityBarColor != null)
                    durabilitySliderImage.color = itemInSlot.durabilityBarColor;

                if (durability <= 0 && itemInSlot.uniqueType != item.UniqueType.gavel)
                {
                    if (!broken)
                    {
                        broken = true;
                        brokenImage.DOKill();
                        brokenImage.DOFillAmount(1, 0.5f);
                    }
                }
                else
                {
                    if (broken)
                    {
                        broken = false;
                        brokenImage.DOKill();
                        brokenImage.DOFillAmount(0, 0.75f);
                    }
                }

            }
            else
            {
                durabilitySlider.gameObject.SetActive(false);
            }

        }

        if (tierHighlight)
        {
            if (itemInSlot && itemData.tier > 0)
            {
                tierHighlight.gameObject.SetActive(true);
                tierHighlight.sprite = tierHighlightSprites[itemData.tier - 1];
                tierHighlight.color = tierHighlightColors[itemData.tier - 1];
            }
            else
            {
                tierHighlight.gameObject.SetActive(false);
            }
        }



    }

    void SetBrokenInstant()
    {
        if (brokenImage == null) return;

        if (durability <= 0 && itemInSlot.uniqueType != item.UniqueType.gavel)
        {
            broken = true;
            brokenImage.fillAmount = 1;
        }
        else
        {
            broken = false;
            brokenImage.fillAmount = 0;
        }

    }

    void ShiftClick(inventorySlot[] toSlots)
    {
        int amountToAdd = itemCountInMouse;
        item mouseItem = itemInMouse;
        int mouseDur = durabilityInMouse;
        ItemData mouseItemData = itemDataInMouse;

        foreach (inventorySlot slot in toSlots)
        {
            // Skip empty slots
            if (slot.itemInSlot == null) continue;

            // Must match the item type
            if (slot.itemInSlot != mouseItem) continue;

            if (!slot.ValidPlaceInSlot(mouseItem, mouseDur)) continue;

            int slotCount = slot.itemCountInSlot;
            int max = mouseItem.maxStackSize;

            // If the slot is already full, skip
            if (slotCount >= max) continue;

            int space = max - slotCount;
            int amountToMove = Mathf.Min(space, amountToAdd);

            // Remove old stack
            inventory.instance.removeItem(slot);

            // Add new updated stack
            inventory.instance.addItemInSlot(
                mouseItem,
                slotCount + amountToMove,
                mouseDur,
                mouseItemData,
                slot,
                transform.position,
                false
            );

            amountToAdd -= amountToMove;
            if (amountToAdd <= 0) break;
        }

        if (amountToAdd > 0)
        {
            foreach (inventorySlot slot in toSlots)
            {
                if (slot.itemInSlot != null) continue;

                if (!slot.ValidPlaceInSlot(mouseItem, mouseDur)) continue;

                int amountToMove = Mathf.Min(mouseItem.maxStackSize, amountToAdd);

                inventory.instance.addItemInSlot(
                    mouseItem,
                    amountToMove,
                    mouseDur,
                    mouseItemData,
                    slot,
                    transform.position,
                    false
                );

                if (takeSound)
                {
                    takeSound.Play();
                }
                if (slot.placeSound)
                {
                    slot.placeSound.Play();
                }

                amountToAdd -= amountToMove;
                if (amountToAdd <= 0) break;
            }
        }

        // ------------------------------------------
        // 3. UPDATE THE MOUSE ITEM
        // ------------------------------------------
        if (amountToAdd <= 0)
        {
            // everything placed
            inventory.instance.removeMouseItem();
            inventory.instance.removeItem(this);

        }
        else
        {
            inventory.instance.removeMouseItem();
            inventory.instance.removeItem(this);
            // some remain in mouse
            inventory.instance.addItemInSlot(
                mouseItem,
                amountToAdd,
                mouseDur,
                mouseItemData,
                this,
                transform.position,
                false
            );
        }

        //save furnace items
        if (TileEntity.currentFurnace)
        {
            TileEntity.currentFurnace.SaveFurnaceItems();
        }


    }

}
[System.Serializable]
public struct ItemData
{
    public int tier;
    public SerializableDictionary<string, int> pMods;
    public SerializableDictionary<string, int> nMods;

    public ItemData(int tier)
    {
        this.tier = tier;
        pMods = new SerializableDictionary<string, int>();
        nMods = new SerializableDictionary<string, int>();

    }

}


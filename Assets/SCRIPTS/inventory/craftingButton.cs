using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class craftingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public static craftingButton selectedRecipe;
    public recipe Recipe;
    public Image image;
    public Color canCraftColor, cantCraftColor;
    const float colorUpdateSpeed = 0.2f;
    public TextMeshProUGUI countText;

    public AudioSource craftSound;

    private void Start()
    {
        InvokeRepeating("CheckCanCraft", colorUpdateSpeed, colorUpdateSpeed);
        countText.text = Recipe.result.count.ToString();
        if(countText.text == "0")
        {
            countText.text = "";
        }
            
    }

    private void OnEnable()
    {
        CheckCanCraft();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventory.instance.inventoryOpen)
        {
            selectedRecipe = this;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (inventory.instance.inventoryOpen)
        {
            selectedRecipe = null;
        }
    }

    private void OnDisable()
    {
        if (inventory.instance.inventoryOpen)
        {
            selectedRecipe = null;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectedRecipe != this || (inventorySlot.itemInMouse && inventorySlot.itemInMouse != Recipe.result.Item))
        {
            return;
        }

        Dictionary<item, int> itemCounts = inventory.instance.GetItemCounts(false);

        bool canCraft = true;

        foreach (ingredient Ingredient in Recipe.Ingredients)
        {
            if (itemCounts.ContainsKey(Ingredient.Item))
            {
                if (itemCounts[Ingredient.Item] < Ingredient.count)
                {
                    canCraft = false;
                    break;
                }
            }
            else
            {
                canCraft = false;
                break;
            }
        }

        if (canCraft && inventorySlot.itemCountInMouse <= Recipe.result.Item.maxStackSize - Recipe.result.count)
        {
            foreach (ingredient Ingredient in Recipe.Ingredients)
            {
                int removedAmount = 0;

                foreach (inventorySlot slot in inventory.instance.slots)
                {
                    if (slot.itemInSlot == Ingredient.Item)
                    {
                        if (slot.itemCountInSlot > Ingredient.count - removedAmount)
                        {
                            slot.itemCountInSlot -= (Ingredient.count - removedAmount);
                            removedAmount = Ingredient.count;
                        }
                        else
                        {
                            removedAmount += slot.itemCountInSlot;
                            inventory.instance.removeItem(slot);
                        }

                    }

                    if (removedAmount == Ingredient.count)
                    {
                        break;
                    }

                }
            }

            inventory.instance.addItemInMouse(Recipe.result.Item, Recipe.result.count,Recipe.result.Item.maxDurability,new ItemData(0), transform.position);
            if(Recipe.result.Item.usesTier)
            inventorySlot.itemDataInMouse = inventory.CreateItemData(inventorySlot.itemInMouse);

            gameManager.endScreenStats["crafted"]++;

            foreach (craftingButton cb in inventory.instance.craftingButtons)
            {
                cb.CheckCanCraft();
            }

            craftSound.pitch = Random.Range(0.95f, 1.05f);
            craftSound.Play();
        }

    }

    public void CheckCanCraft()
    {
        if (!inventory.instance || !inventory.instance.inventoryOpen)
        {
            return;
        }


        Dictionary<item, int> itemCounts = inventory.instance.GetItemCounts(true);

        bool canCraft = true;

        foreach (ingredient Ingredient in Recipe.Ingredients)
        {
            if (itemCounts.ContainsKey(Ingredient.Item))
            {
                if (itemCounts[Ingredient.Item] < Ingredient.count)
                {
                    canCraft = false;
                    break;
                }
            }
            else
            {
                canCraft = false;
                break;
            }
        }

        if (canCraft && image.color != canCraftColor)
        {
            image.DOColor(canCraftColor, 0.1f);
        }
        else if(!canCraft && image.color != cantCraftColor)
        {
            image.DOColor(cantCraftColor, 0.1f);
        }


    }
}

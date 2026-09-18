using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Collections;
using TMPro;
using UnityEngine;

public class inventoryHightlighter : MonoBehaviour
{
    public static inventoryHightlighter instance;
    public int equipppedSlot;
    public float highlighterSpeed = 10;
    public float scrollCooldown = 0.05f;
    public TextMeshProUGUI itemNameText;
    Coroutine fadeOutCo;
    TweenerCore<Color,Color,ColorOptions> tween;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        itemNameText.color = new Color(itemNameText.color.r, itemNameText.color.g, itemNameText.color.b, 0);
    }

    public void SwitchSlot()
    {
        transform.position = inventorySlot.equippedSlot.transform.position;
        transform.SetParent(inventorySlot.equippedSlot.transform);
        transform.SetAsFirstSibling();
    }

    private void Update()
    {
        if (HandMove.instance && !inventory.instance.inventoryOpen && !menu.instance.mapMaximized && !menu.instance.signUi.activeSelf && !Chat.Instance.chatOpen 
            && !HandMove.instance.eating && !playerHealth.instance.dead && !player.instance.inCutscene)
        {
            int oldEquippedSlot = equipppedSlot;

            if (InputManager.instance.mouseScroll < 0)
            {
                equipppedSlot++;
                HandMove.instance.tilePreview.position = HandMove.instance.roundedMousePos;

                if (equipppedSlot >= 9)
                {
                    equipppedSlot = 0;
                }

            }
            if (InputManager.instance.mouseScroll > 0)
            {
                equipppedSlot--;
                HandMove.instance.tilePreview.position = HandMove.instance.roundedMousePos;


                if (equipppedSlot < 0)
                {
                    equipppedSlot = 8;
                }


            }

            //number keys
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                equipppedSlot = 0;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                equipppedSlot = 1;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                equipppedSlot = 2;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                equipppedSlot = 3;
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                equipppedSlot = 4;
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                equipppedSlot = 5;
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                equipppedSlot = 6;
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                equipppedSlot = 7;
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                equipppedSlot = 8;
            }

            //show item name text
            if (inventory.instance.slots[equipppedSlot].itemInSlot != inventory.instance.slots[oldEquippedSlot].itemInSlot)
            {
                ShowItemNameText();
            }
        }
        
        if(HandMove.instance && HandMove.instance.canScroll)
            inventorySlot.equippedSlot = inventory.instance.slots[equipppedSlot];

        transform.localPosition = Vector2.Lerp(transform.localPosition, new Vector2(-222.5f + (equipppedSlot * 55), transform.localPosition.y), highlighterSpeed * Time.deltaTime);
    }
    public void SetSelected(int selected)
    {
        equipppedSlot = selected;
        transform.localPosition = new Vector2(-222.5f + (equipppedSlot * 55), transform.localPosition.y);
    }


    public void ShowItemNameText()
    {
        if (fadeOutCo != null) StopCoroutine(fadeOutCo);
        if (tween != null) tween.Kill();

        if (inventory.instance.slots[equipppedSlot].itemInSlot)
        {
            itemNameText.text = inventory.instance.slots[equipppedSlot].itemInSlot.displayName;
            itemNameText.DOFade(1, 0.2f);
            fadeOutCo = StartCoroutine(_FadeOutItemName());
        }
        else
        {
            tween = itemNameText.DOFade(0, 0.5f);
        }
    }


    IEnumerator _FadeOutItemName()
    {
        yield return new WaitForSeconds(1.5f);
        tween = itemNameText.DOFade(0, 0.5f);

    }

}

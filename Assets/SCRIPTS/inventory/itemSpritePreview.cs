using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class itemSpritePreview : MonoBehaviour
{
    [SerializeField]
    public bool generateSpriteAndName;
    public SpriteRenderer sprite;
    public item Item;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        Item = GetComponent<itemPickup>().Item;
    }

    // Update is called once per frame
    void Update()
    {
        if (generateSpriteAndName)
        {
            if(Item == null)
            {
                Item = GetComponent<itemPickup>().Item;
            }
            
            generateSpriteAndName = false;
            sprite.sprite = Item.overworldSprite;
            gameObject.name = Item.displayName + " Item";
        }
    }
}


using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(itemPickup))]
public class ItemPickupEditor : Editor
{
    
    public override void OnInspectorGUI()
    {
        itemPickup ItemPickup = (itemPickup)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Generate Sprite"))
        {
            ItemPickup.SetSprite();
        }
        if (GUILayout.Button("Set Name"))
        {
            if (ItemPickup.Item)
                ItemPickup.gameObject.name = ItemPickup.Item.displayName + " Item";
            else
                ItemPickup.gameObject.name = "Item";
        }

    }

}

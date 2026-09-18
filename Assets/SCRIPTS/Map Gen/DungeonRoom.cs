using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonRoom : MonoBehaviour
{
    public Transform bottomAnchor;
    public Transform[] topAnchors;
    public Tilemap groundTilemap, waterTilemap;

    public Transform itemSpawnPos;
    public item itemSpawnItem;
    public int itemId;

}

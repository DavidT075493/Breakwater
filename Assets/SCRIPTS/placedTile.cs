using DG.Tweening;
using Pathfinding;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class placedTile : MonoBehaviour
{
    public GameObject[] colliders;
    public SpriteRenderer sprite;
    public Material unlitMat;
    public static placedTile highlightedTile;
    public placedTileHealth health;
    public item Item;
    public SortingGroup sort;
    public int state;
    public string owner;

    public TileEntity tileEntity;
    public GameObject indicatorPrefab;
    public AudioClip doorOpenClip, doorCloseClip;
    public AudioMixerGroup sfxGroup;
    public AudioSource placeSource, placeSource2, placeSource3;

    public TileEntity attatchedFarmPlant;
    public bool farmlandWatered;
    public ParticleSystem wateredEffect;
    public ParticleSystem furnacePs, purifierPs;
    public AudioSource furnaceSound, purifierSound;
    public GameObject furnaceLight;
    public GameObject respawnEffect;

    public GameObject tileEntityExtras;
    public int boatTileIndex;

    [Header("Vehicle Station")]
    public SpriteRenderer boatPreview;
    public LayerMask boatBlockingLayers;
    public Color boatCanColor, boatCantColor;

    public void SetProperties(item Item, int stateIndex, string layer, short sortOrder, short tileType, bool disableSpawnAnimation, string tileEntityData, string owner, Boat boatParent, int boatTileIndex)
    {
        if (Item.TileType != item.tileType.crop)
            sprite.sprite = Item.tileStates[stateIndex].tileSprite;

        this.Item = Item;
        if (Item.unlitWhenDropped) sprite.material = unlitMat;

        sort.sortingLayerName = layer;
        sort.sortingOrder = sortOrder;
        state = stateIndex;
        this.owner = owner;

        //set collider based on item settings     
        if ((item.tileType)tileType != item.tileType.floor)
        {
            colliders[Item.tileStates[stateIndex].tileColliderIndex].SetActive(!boatParent);
            health = GetComponentInChildren<placedTileHealth>();
            health.health = Item.health;
            health.activeCollider = Instantiate(colliders[Item.tileStates[stateIndex].tileColliderIndex],health.transform).transform;
            Collider2D[] newCols = health.activeCollider.GetComponents<Collider2D>();
            foreach (Collider2D newCol in newCols)
            {
                newCol.compositeOperation = Collider2D.CompositeOperation.None;
                
                
                if(!Item.tileDealDamage && (item.tileType)tileType != item.tileType.door)
                {
                    newCol.excludeLayers = LayerMask.GetMask("Player Collision", "Boat", "Boat Water Collision", "Boat Rock Collision");
                }
                //damaging tiles enable collision with player
                else
                {
                    newCol.excludeLayers = LayerMask.GetMask("Boat", "Boat Water Collision","Boat Rock Collision");
                    newCol.gameObject.SetActive(true);
                }

                DynamicGridObstacle g = newCol.gameObject.AddComponent<DynamicGridObstacle>();
                g.DoUpdateGraphs();
                g.checkTime = 5;
                
                if((item.tileType)tileType == item.tileType.door)
                {
                    colliders[Item.tileStates[stateIndex].tileColliderIndex].SetActive(false);
                    g.checkTime = 0.3f;
                }
            }
            if (Item.tileDealDamage)
            {
                damagePlayer d = health.activeCollider.transform.parent.gameObject.AddComponent<damagePlayer>();
                d.damage = Item.tileDamage;
                d.knockback = Item.knockback;
                d.deathMessage = Item.tileDeathMessage;
                d.damageEnemy = true;
                d.active = true;
                d.playerCollisionOnly = true;

                d.canHitPlayersOnBoat = boatParent != null;

                colliders[Item.tileStates[stateIndex].tileColliderIndex].SetActive(false);
            }

        }
        //floor tile hitbox to collide with boat
        else
        {
            if(!boatParent)
            colliders[0].SetActive(true);
            
            colliders[0].transform.SetParent(health.transform);
            health.disabled = true;
            colliders[0].layer = 15;

        }


        switch (Item.TileType)
        {
            case item.tileType.door:
                CreateTileEntity();

                tileEntity.hinge1 = transform.Find("Hinge Left");
                tileEntity.hinge2 = transform.Find("Hinge Right");

                if (stateIndex == 0)
                {
                    sprite.transform.SetParent(tileEntity.hinge1);
                    tileEntity.doorFlipped = false;
                }
                else if (stateIndex == 1)
                {
                    sprite.transform.SetParent(tileEntity.hinge2);
                    tileEntity.doorFlipped = true;
                }

                colliders[Item.tileStates[stateIndex].tileColliderIndex].transform.SetParent(sprite.transform);


                if (!tileEntity.inDoorBond)
                {

                    GameObject t = gameManager.instance.placedTiles.Find(t => Vector2.Distance(transform.position, t.tileGameObject.transform.position) == 1 && t.PlacedTile.Item.TileType == item.tileType.door).tileGameObject;

                    if (t)
                    {
                        placedTile tile = t.GetComponent<placedTile>();

                        if (tile.tileEntity && tile.Item.TileType == item.tileType.door && !tile.tileEntity.inDoorBond)
                        {

                            tile.tileEntity.dommy = false;
                            tile.tileEntity.inDoorBond = true;
                            tile.tileEntity.parentDoor = this;
                            tile.tileEntity.destroyIndicator();
                            tileEntity.destroyIndicator();
                            tileEntity.inDoorBond = true;
                            tileEntity.childDoor = tile;

                            tileEntity.playerCheckPos = new Vector3((transform.position.x + t.transform.position.x) / 2, (transform.position.y + t.transform.position.y) / 2, 0);

                            Collider2D playerCheck = Physics2D.OverlapBox(tileEntity.playerCheckPos, new Vector2(tileEntity.playerCheckSize, tileEntity.playerCheckSize), 0, tileEntity.mainPlayerLayer);
                            if (playerCheck)
                            {
                                tileEntity.spawnIndicator();
                            }
                        }

                    }


                }
                break;

            case item.tileType.torch:
                tileEntityExtras.transform.Find("Torch Light").gameObject.SetActive(true);
                break;
            case item.tileType.campfire:
                CreateTileEntity();
                tileEntityExtras.transform.Find("Campfire Light").gameObject.SetActive(true);
                gameObject.tag = "Campfire";
                health.gameObject.tag = "Campfire";
                break;
            case item.tileType.sign:
                CreateTileEntity();
                tileEntity.signText = tileEntityExtras.transform.Find("Text").gameObject.GetComponent<TextMeshPro>();
                if (owner == PlayerPrefs.GetString("userID"))
                {
                    Cursor.visible = true;
                    //select sign input field
                    menu.instance.signInput.ActivateInputField();
                }
                break;
            case item.tileType.furnace:
                Cursor.visible = true;
                CreateTileEntity();
                tileEntity.smeltTimeLeft = TileEntity.smeltTime;
                break;
            case item.tileType.purifier:
                CreateTileEntity();
                tileEntity.smeltTimeLeft = TileEntity.smeltTime;
                break;
            case item.tileType.vehicleStation:
                boatPreview.gameObject.SetActive(true);
                CreateTileEntity();
                break;
            case item.tileType.chest:
                CreateTileEntity();
                break;
            case item.tileType.crafting:
                CreateTileEntity();
                break;

            case item.tileType.respawn:
                CreateTileEntity();

                break;
            case item.tileType.crop:
                CreateTileEntity();
                break;

            case item.tileType.anvil:
                CreateTileEntity();
                break;

            case item.tileType.ominousFlower:
                CreateTileEntity();
                tileEntity.playerCountText = tileEntityExtras.transform.Find("Player Count").gameObject.GetComponent<TextMeshPro>();
                tileEntity.playerCountText.gameObject.SetActive(true);
                break;


            default:
                Destroy(tileEntityExtras);
                break;
        }


        if (boatParent)
        {
            if (Item.tileDamage == 0)
            {
                foreach (GameObject c in colliders)
                {
                    c.SetActive(false);
                }
            }
            else
            {
                foreach (GameObject c in colliders)
                {
                    Collider2D cc = c.GetComponent<Collider2D>();

                    if (cc)
                    {
                        cc.excludeLayers |= (1 << LayerMask.NameToLayer("Boat"));
                        cc.includeLayers |= (1 << LayerMask.NameToLayer("Player Collision in Boat"));
                    }
                    c.layer = 19;
                }
            }

            health.disabled = true;

            this.boatTileIndex = boatTileIndex;

            if (tileEntity)
            {
                tileEntity.boatParent = boatParent;
            }
        }

        //load tile data for tile entities
        if (tileEntityData != "" && tileEntityData != null)
        {
            StringToTileEntityData(tileEntityData);
        }

        if (!disableSpawnAnimation)
        {
            if (Item.overrideSwingSound) placeSource.clip = Item.overrideSwingSound;

            placeSource.Play();
            if (placeSource2.isActiveAndEnabled) placeSource2.Play();
            if (placeSource3.isActiveAndEnabled) placeSource3.Play();

            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBounce);
        }
    }

    void CreateTileEntity()
    {
        tileEntity = gameObject.AddComponent<TileEntity>();
        tileEntity.Item = Item;
        tileEntity.tile = this;
        tileEntity.playerCheckPos = transform.position;
    }

    public void OnRemove()
    {
        tileEntity?.OnTileDestroy();
    }

    private void FixedUpdate()
    {
        if (highlightedTile == this)
        {
            sprite.color = new Color(1, 0.7f, 0.7f, sprite.color.a);
        }
        else if (!(tileEntity && tileEntity.playerInRange) && !farmlandWatered)
        {
            sprite.color = new Color(1, 1, 1, sprite.color.a);
        }

    }

    public string TileEntityDataToString()
    {
        if (!tileEntity && Item.TileType != item.tileType.respawn) return "";

        string data = "";

        switch (Item.TileType)
        {
            case item.tileType.sign:
                data = tileEntity.signText.text;
                break;
            case item.tileType.door:
                data = tileEntity.open.ToString();
                break;
            case item.tileType.chest:
                for (int i = 0; i < 27; i++)
                {
                    data += tileEntity.chestItems[i] + "-";
                    data += tileEntity.chestCounts[i] + "-";
                    data += tileEntity.chestDurabililties[i] + "-";
                    data += inventory.ItemDataToString(tileEntity.chestItemData[i]).Replace(',', '|') + ",";
                }

                break;
            case item.tileType.furnace:
                if (tileEntity.ingredientItem) data += tileEntity.ingredientItem.itemId + ", "; else data += "null, ";
                data += tileEntity.ingredientCount + ", ";
                if (tileEntity.fuelItem) data += tileEntity.fuelItem.itemId + ", "; else data += "null, ";
                data += tileEntity.fuelCount + ", ";
                if (tileEntity.resultItem) data += tileEntity.resultItem.itemId + ", "; else data += "null, ";
                data += tileEntity.resultCount + ", ";
                data += tileEntity.smeltTimeLeft + ", ";
                data += tileEntity.fuelTimeLeft;

                break;

            case item.tileType.purifier:
                if (tileEntity.ingredientItem) data += tileEntity.ingredientItem.itemId + ", "; else data += "null, ";
                data += tileEntity.ingredientCount + ", ";
                if (tileEntity.fuelItem) data += tileEntity.fuelItem.itemId + ", "; else data += "null, ";
                data += tileEntity.fuelCount + ", ";
                if (tileEntity.resultItem) data += tileEntity.resultItem.itemId + ", "; else data += "null, ";
                data += tileEntity.resultCount + ", ";
                data += tileEntity.smeltTimeLeft + ", ";
                data += tileEntity.fuelTimeLeft;

                break;

            case item.tileType.respawn:
                data = string.Join(", ", tileEntity.respawnUsers);
                break;
            case item.tileType.crop:
                data += tileEntity.growthStage;
                data += ",";
                data += tileEntity.growthTime;
                data += ",";
                data += tileEntity.growthTimeTarget;
                data += ",";
                data += tileEntity.attatchedCropItem != null;

                break;


        }

        byte[] compressedBytes;
        using (MemoryStream compressedStream = new MemoryStream())
        {
            using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
            {
                using (StreamWriter writer = new StreamWriter(deflateStream))
                {
                    writer.Write(data);
                }
            }
            compressedBytes = compressedStream.ToArray();
        }

        return System.Convert.ToBase64String(compressedBytes);
    }

    public void StringToTileEntityData(string tileEntityData)
    {
        if (tileEntityData == "") return;

        try
        {
            byte[] compressedBytes = System.Convert.FromBase64String(tileEntityData);

            string decompressedString;
            using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
            {
                using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    using (StreamReader reader = new StreamReader(deflateStream))
                    {
                        decompressedString = reader.ReadToEnd();
                    }
                }
            }

            tileEntityData = decompressedString;
        }
        catch
        {
            Debug.LogError(Item + " failed to load data");
            return;
        }


        if (tileEntityData == "") return;

        switch (Item.TileType)
        {
            case item.tileType.sign:
                tileEntity.signText.text = tileEntityData;
                tileEntity.signText.gameObject.SetActive(true);
                break;
            case item.tileType.door:
                if (tileEntityData == "True") tileEntity.SetOpen();
                break;
            case item.tileType.chest:
                string[] data = tileEntityData.Split(",");
                for (int i = 0; i < 27; i++)
                {
                    string[] split = data[i].Split("-");

                    tileEntity.chestItems[i] = split[0];
                    tileEntity.chestCounts[i] = short.Parse(split[1]);
                    tileEntity.chestDurabililties[i] = short.Parse(split[2]);
                    tileEntity.chestItemData[i] = inventory.StringToItemData(split[3].Replace('|', ','));
                }
                break;
            case item.tileType.furnace:
                string[] data2 = tileEntityData.Split(", ");

                tileEntity.ingredientItem = gameManager.instance.itemDictionary[data2[0]];
                tileEntity.ingredientCount = short.Parse(data2[1]);
                tileEntity.fuelItem = gameManager.instance.itemDictionary[data2[2]];
                tileEntity.fuelCount = short.Parse(data2[3]);
                tileEntity.resultItem = gameManager.instance.itemDictionary[data2[4]];
                tileEntity.resultCount = short.Parse(data2[5]);
                tileEntity.smeltTimeLeft = float.Parse(data2[6]);
                tileEntity.fuelTimeLeft = float.Parse(data2[7]);

                break;
            case item.tileType.purifier:
                string[] data3 = tileEntityData.Split(", ");
                tileEntity.ingredientItem = gameManager.instance.itemDictionary[data3[0]];
                tileEntity.ingredientCount = short.Parse(data3[1]);
                tileEntity.fuelItem = gameManager.instance.itemDictionary[data3[2]];
                tileEntity.fuelCount = short.Parse(data3[3]);
                tileEntity.resultItem = gameManager.instance.itemDictionary[data3[4]];
                tileEntity.resultCount = short.Parse(data3[5]);
                tileEntity.smeltTimeLeft = float.Parse(data3[6]);
                tileEntity.fuelTimeLeft = float.Parse(data3[7]);

                break;
            case item.tileType.respawn:

                string[] respawnUsers = tileEntityData.Split(", ");
                tileEntity.respawnUsers = respawnUsers.ToList();
                if (respawnUsers.Contains(DataPersistanceManager.userId))
                {
                    gameManager.instance.hasRespawnPoint = true;

                    gameManager.instance.savedRespawnTile = tileEntity;
                    if (playerHealth.instance)
                    {
                        tileEntity.SetSpawnPoint(true);
                    }

                }

                break;

            case item.tileType.crop:
                string[] data4 = tileEntityData.Split(",");
                tileEntity.growthStage = short.Parse(data4[0]);
                tileEntity.growthTime = short.Parse(data4[1]);
                tileEntity.growthTimeTarget = short.Parse(data4[2]);

                tileEntity.SetGrowthStage(tileEntity.growthStage, data4[3] == "False");
                break;

            default: break;

        }
    }

}

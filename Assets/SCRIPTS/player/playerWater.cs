using DG.Tweening;
using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class playerWater : NetworkBehaviour
{
    public static playerWater instance;
    public player playerParent;
    public Tilemap groundTilemap, waterTilemap;
    public AdvancedRuleTileEntity shallowWater, sandTile;
    public Animator playerAnim;
    public PlayerEvents playerEvents;

    public int waterValue;
    public NetworkObject view;
    public GameObject hand;
    public SpriteRenderer tilePreview;
    public HandMove handMove;
    public bool waterWalkActive;
    public bool waterWalkInWater;
    public LayerMask additionalGroundLayers, lavaLayer;

    bool savedDrownRespawnPos;
    public Vector2Int drownRespawnPos;


    [Header("Stamina")]
    [SerializeField]
    float maxStamina = 30;
    [HideInInspector]
    public float totalMaxStamina;
    public float stamina = 30;
    public float staminaRegenRate = 3;
    public bool drowning;
    public AudioSource drownSound;
    public bool outOfStamina;
    Color normalStaminaColor;
    public Color staminaOutColor, staminaRegenColor;
    public bool usingStamina;
    float staminaBarSpeed = 1;
    public bool infiniteStamina;

    float hue;

    void Awake()
    {
    }
    private void Start()
    {
        if (view.HasStateAuthority)
        {
            instance = this;
        }

        groundTilemap = MapDisplay.Instance.groundTilemap;
        waterTilemap = MapDisplay.Instance.waterTilemap;

        stamina = maxStamina * (1 + (inventory.instance.GetStatAmount(2) / 10f * inventory.instance.maxStaminaMult))
                * inventory.instance.GetModifierInArmorSlots("stam");

        normalStaminaColor = menu.instance.staminaBarImage.color;

        if (playerSpawner.instance.savedDrownRespawnPos != Vector2Int.zero)
        {
            drownRespawnPos = playerSpawner.instance.savedDrownRespawnPos;
            savedDrownRespawnPos = true;
        }

    }


    void Update()
    {
        Vector2 tileOffset = new Vector2(-1, -1.5f);

        TileBase currentGroundTile = null;
        TileBase currentWaterTile = null;
        TileBase currentLavaTile = null;

        if (playerParent.inDungeon > -1)
        {
            tileOffset = new Vector2(-0.5f, -1f);

            if (DungeonGenerator.instance.dungeons[player.instance.inDungeon].waterTilemaps != null)
            {
                foreach (Tilemap t in DungeonGenerator.instance.dungeons[player.instance.inDungeon].waterTilemaps)
                {
                    currentWaterTile = t.GetTile(new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));

                    if (currentWaterTile != null) currentWaterTile = t.GetTile(new Vector3Int(1, 0) +
                        new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));
                    if (currentWaterTile != null) currentWaterTile = t.GetTile(new Vector3Int(-1, 0) +
                        new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));
                    if (currentWaterTile != null) currentWaterTile = t.GetTile(new Vector3Int(0, 1) +
                        new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));

                    if (currentWaterTile != null) break;

                }
                foreach (Tilemap t in DungeonGenerator.instance.dungeons[player.instance.inDungeon].groundTilemaps)
                {
                    currentGroundTile = t.GetTile(new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));
                    if (currentGroundTile != null) break;
                }
            }

        }
        else
        {
            currentGroundTile = groundTilemap.GetTile(new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));
            currentWaterTile = waterTilemap.GetTile(new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));
            currentLavaTile = MapDisplay.Instance.lavaTilemap.GetTile(new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));

            if (currentWaterTile != null) currentWaterTile = waterTilemap.GetTile(new Vector3Int(1, 0) +
                new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));
            if (currentWaterTile != null) currentWaterTile = waterTilemap.GetTile(new Vector3Int(-1, 0) +
                new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));
            if (currentWaterTile != null) currentWaterTile = waterTilemap.GetTile(new Vector3Int(0, 1) +
                new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));
            if (currentWaterTile != null) currentWaterTile = waterTilemap.GetTile(new Vector3Int(0, -1) +
                new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));

            
        }

        if(currentGroundTile is AdvancedRuleTileEntity)
        {
            playerEvents.currentTileSurface = ((AdvancedRuleTileEntity)currentGroundTile).surface;
        }
        else if (currentGroundTile is AdvancedRuleTile)
        {
            playerEvents.currentTileSurface = ((AdvancedRuleTile)currentGroundTile).surface;
        }


        if (view.HasStateAuthority)
        {
            totalMaxStamina = maxStamina * (1 + (inventory.instance.GetStatAmount(2) / 10f * inventory.instance.maxStaminaMult))
                * inventory.instance.GetModifierInArmorSlots("stam");

            menu.instance.staminaAnim.SetFloat("Speed", staminaBarSpeed);

            if (infiniteStamina) stamina = 10000;

            if (stamina <= 0 && !outOfStamina)
            {
                outOfStamina = true;
                menu.instance.staminaBarImage.DOColor(staminaOutColor, 0.5f);
            }

            if (player.instance.inCutscene) stamina = totalMaxStamina;

            if (outOfStamina)
            {
                stamina += staminaRegenRate * 0.8f * Time.deltaTime * (1 + (inventory.instance.GetStatAmount(2) / 15f * inventory.instance.maxStaminaMult * 0.5f));
                if (inventory.charmEffect == "fractured") stamina += Time.deltaTime * 10;

                if (stamina >= totalMaxStamina)
                {
                    outOfStamina = false;
                    menu.instance.staminaBarImage.DOColor(normalStaminaColor, 0.5f);
                }

            }
            else
            {
                if (!infiniteStamina)
                {
                    if (usingStamina)
                    {
                        menu.instance.staminaBarImage.DOColor(normalStaminaColor, 0.5f);
                    }
                    else
                    {
                        menu.instance.staminaBarImage.DOColor(staminaRegenColor, 0.5f);
                    }
                }
                else
                {
                    hue += Time.deltaTime * 0.8f;
                    if (hue > 1f)
                        hue -= 1f; // Loop hue back to 0

                    // Convert HSV to RGB (Hue, Saturation, Value)
                    Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);

                    menu.instance.staminaBarImage.color = rainbowColor;
                }

            }

            playerAnim.SetInteger("Water Level", waterValue);
            if(playerParent.health.dead)
                playerAnim.SetInteger("Water Level", 0);

            stamina = Mathf.Clamp(stamina, 0, totalMaxStamina);

            if (usingStamina)
            {
                DOTween.To(() => staminaBarSpeed, x => staminaBarSpeed = x, 1.65f, 0.3f);
            }
            else
            {
                DOTween.To(() => staminaBarSpeed, x => staminaBarSpeed = x, 1f, 0.8f);
            }

            //boat logic
            if (player.instance.currentBoat || handEvents.instance.grappling || Boss.inRangeOfSos || gameManager.instance.disableTiles)
            {
                waterValue = 0;
                playerAnim.SetInteger("Water Level", waterValue);
                if (!hand.activeSelf)
                {
                    hand.SetActive(true);
                    handMove.soulBreakerAwakened = false;
                    handMove.recallSound.Stop();
                    RPC_EnterWater(false);
                }
                if (waterWalkInWater)
                {
                    waterWalkInWater = false;
                    RPC_SetFrostWalking(false);
                }
                if (!player.instance.sprinting && !outOfStamina)
                {
                    stamina += staminaRegenRate * Time.deltaTime * (1 + (inventory.instance.GetStatAmount(2) / 15f * inventory.instance.maxStaminaMult * 0.5f));
                    usingStamina = false;
                }

                return;
            }

            //save position when grounded (not when water walking)
            if (currentGroundTile != null && currentWaterTile == null && currentLavaTile == null)
            {
                savedDrownRespawnPos = true;
                drownRespawnPos = Vector2Int.RoundToInt(transform.position);
            }


            TileData currentPlacedTile;
            currentPlacedTile = gameManager.instance.placedTiles.Find(data => data.position == new Vector2(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y - 0.32f)) && data.Item && data.Item.floor);

            Collider2D additionalFloor = Physics2D.OverlapBox(new Vector2(transform.position.x, transform.position.y - 0.32f), new Vector2(0.1f, 0.1f), 0, additionalGroundLayers);

            if (currentPlacedTile.Item || additionalFloor)
            {
                currentGroundTile = sandTile;
                currentWaterTile = null;
            }
            if (player.instance.dashing || player.instance.beingGrabbedBy)
            {
                currentGroundTile = sandTile;
                currentWaterTile = null;
            }

            if (inventory.instance.legsSlot.itemInSlot && inventory.instance.legsSlot.itemInSlot.uniqueType == item.UniqueType.frost
                && (currentWaterTile == shallowWater || (currentGroundTile == null && currentWaterTile == null)))
            {
                currentGroundTile = sandTile;
                currentWaterTile = null;
                waterWalkInWater = true;
            }
            else
            {
                waterWalkInWater = false;
            }

            if (waterWalkInWater != playerEvents.frostWalking)
            {
                RPC_SetFrostWalking(waterWalkInWater);
            }

            if (((currentGroundTile != null && currentWaterTile == null) || waterWalkInWater))
            {

                if (waterValue == 2)
                {
                    RPC_EnterWater(false);
                }
                waterValue = 0;
                hand.SetActive(true);

                if (!player.instance.sprinting && !outOfStamina && !handEvents.instance.grappling)
                {
                    stamina += staminaRegenRate * Time.deltaTime * (1 + (inventory.instance.GetStatAmount(2) / 15f * inventory.instance.maxStaminaMult * 0.5f));
                    usingStamina = false;
                }

                Collider2D lavaCheck = Physics2D.OverlapBox(player.instance.collision.transform.position, new Vector2(0.1f, 0.1f), 0, lavaLayer);
                if (!player.instance.beingGrabbedBy && !drowning && lavaCheck && lavaCheck.gameObject == MapDisplay.Instance.lavaCollisionTilemap.gameObject)
                {
                    drowning = true;
                    StartCoroutine(_Drown(true));
                }

            }
            //shallow water
            else if (currentWaterTile == shallowWater)
            {
                if (waterValue == 2)
                {
                    RPC_EnterWater(false);
                }
                waterValue = 1;
                hand.SetActive(true);
                //HandMove.instance.boatPreview.gameObject.SetActive(false);

                if (!player.instance.sprinting && !outOfStamina && !handEvents.instance.grappling)
                {
                    stamina += staminaRegenRate * Time.deltaTime * (1 + (inventory.instance.GetStatAmount(2) / 15f * inventory.instance.maxStaminaMult * 0.5f)) / 2;
                    usingStamina = false;
                }
            }
            //deep water
            else if (currentGroundTile == null)
            {
                if (waterValue != 2)
                {
                    RPC_EnterWater(true);
                    menu.instance.OpenMap(false, true);
                    waterValue = 2;
                }
                tilePreview.enabled = false;

                player.instance.HandEvents.CancelGrapple();
                handMove.swingTrail.emitting = false;
                hand.SetActive(false);
                handMove.grappleCooldown = false;
                HandMove.instance.boatPreview.gameObject.SetActive(false);

                if (player.instance.axisInput != Vector3.zero)
                {
                    stamina -= Time.deltaTime * 1.4f;
                }
                else
                {
                    stamina -= Time.deltaTime / 2;
                }

                //lose stamina rapidly when swimming in vale or in dungeon out of bounds
                if (player.instance.currentIsland.biome == (int)MapGenerator.biome.vale && player.instance.onIsland)
                    stamina -= Time.deltaTime * 5;

                if (playerParent.inDungeon > -1)
                    stamina -= Time.deltaTime * 20;

                usingStamina = true;

                if ((stamina <= 0 || outOfStamina) && !drowning)
                {
                    drowning = true;
                    StartCoroutine(_Drown(false));
                }

            }


        }
        if (!hand.activeSelf)
        {
            handMove.handMask.enabled = false;
            handMove.handMask2.enabled = false;
            handMove.itemMask.enabled = false;
        }


    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_PlayerDrown()
    {
        drownSound.Play();
    }


    IEnumerator _Drown(bool lava)
    {
        if (playerHealth.instance.dead) yield break;

        if (playerParent.inDungeon == -1)
            gameManager.instance.musicSource.DOFade(0, 1);

        CanvasGroup drownScreen = !lava ? menu.instance.drownScreen : menu.instance.lavaDrownScreen;

        drownScreen.DOFade(1, 0.5f);
        RPC_PlayerDrown();
        yield return new WaitForSeconds(0.7f);

        if (playerParent.inDungeon == -1)
        {
            if (savedDrownRespawnPos)
            {
                transform.position = (Vector2)drownRespawnPos;
            }
            //respawn at player's respawn pos if there is no last grounded
            else
            {
                if (player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
                    transform.position = playerHealth.instance.respawnPos;
                else
                    transform.position = playerHealth.instance.RandomValePos();

            }

        }
        //dungeon entrance
        else
        {
            transform.position = DungeonGenerator.instance.dungeons[playerParent.inDungeon].position + new Vector2(0, 2);
        }

        if (playerHealth.instance.currentHealth > 20)
        {
            playerHealth.instance.currentHealth = Mathf.RoundToInt(playerHealth.instance.currentHealth / 1.5f);
        }

        yield return new WaitForSeconds(0.5f);
        drownScreen.DOFade(0, 1);
        yield return new WaitForSeconds(1f);
        drowning = false;

        if (playerParent.inDungeon == -1)
        {
            if (!TimeManager.instance.isNight)
                StartCoroutine(gameManager.instance._SwitchMusic("day", 0));
            else
                StartCoroutine(gameManager.instance._SwitchMusic("night", 0));
        }
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_EnterWater(bool enter)
    {
        if (view.HasStateAuthority) return;

        hand.SetActive(!enter);
        if (enter)
        {
            handMove.playerParent.heldItemSprite.sprite = null;
            handMove.playerParent.heldItemSprite.material = null;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetFrostWalking(bool frostWalking)
    {
        playerEvents.frostWalking = frostWalking;
    }

}

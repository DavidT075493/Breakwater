using DG.Tweening;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HandMove : NetworkBehaviour
{
    public static HandMove instance;
    public const int handDamage = 3;

    public bool canScroll = true;
    public bool handCanFlip = true;

    public SpriteRenderer sprite;
    public SpriteRenderer twoHandSprite;
    public Vector3 mousePosition;
    public GameObject Player;
    public SpriteRenderer playerSprite;
    public float cooldown;
    private float cooldownTime;
    public static Vector2 direction;
    public bool swingStored;
    public Animator animator;
    public float handSpeed;
    public item selectedItem;
    private bool heldItemBroken;
    public player playerParent;

    public NetworkObject view;
    public GameObject tilePrefab;
    public Transform tilePreview;
    public SpriteRenderer tilePreviewSprite;
    NetworkMecanimAnimator netAnim;

    float tileRotation;
    int tileState;
    public Color canColor, CantColor;
    public LayerMask obstructionLayers;
    public LayerMask floorObstructionLayers;
    bool obstructed;

    public bool canBuild;
    public float buildDist = 5.5f;
    public GameObject destroyEffect;
    public Vector2 roundedMousePos;

    public float placeBuffer;
    float placeBufferTimer;

    handEvents HandEvents;
    [SerializeField] damagePlayer[] hitboxes;
    public AudioSource toolBreakSound;
    public AudioClip toolBreakClip, uniqueBreakClip;
    public SpriteMask handMask, itemMask, handMask2;
    float armorSwitchCooldown = 0.3f;
    bool inArmorSwitchCooldown;
    public Material defaultMat;

    public Vector2 mousePosCorrection;
    public ParticleSystem recallPs, valeRecallPs;
    float recallTimer;
    const float recallTime = 2.5f;
    public AudioSource recallSound;
    float recallSoundVol = 0.8f;
    [SerializeField] AudioSource bucketPickupSound;
    [SerializeField] AudioSource dropSound;

    [SerializeField] AudioSource executionObtainSound;
    [SerializeField] ParticleSystem executionObtainPs;

    [SerializeField] GameObject deathScythePs;

    public const float jackpotChance = 0.5f;
    public SpriteRenderer slothCircle;
    public bool disableSlothGain;
    public GameObject freezeShatterPs, freezeGustPs;

    public TextMeshPro wagerText;
    public int wagerLevel;
    public const int hpPerWager = 3;
    public float jackpotAdditionalChance = 0;

    [Header("Boat Station")]
    public SpriteRenderer boatPreview;
    public LayerMask waterLayer;
    public Vector2 boatCheckSize = new Vector2(7.02f, 2.4f);

    [Header("Eating")]
    public int handEatingSortOrder = 10;
    int normalHandSortOrder;
    public bool eating;
    float eatTimer;
    bool inEatCooldown;
    public AudioSource eatFinishSound;
    AudioClip defaultEatFinishSound;

    public Light2D catalystLight;
    public ParticleSystem catalystPs;
    float catalystLightIntensity;
    public AudioSource catalystSound, catalystDoneSound;
    float swingCooldownTime;
    bool controllingBoat;

    const float handScaleLerpSpeed = 11;
    public TrailRenderer cleaveTrail, swingTrail;
    private TileData waterableTile;
    bool hoveringOverWaterTile;
    [SerializeField] LineRenderer flowerTrail;
    private bool flowerTrailEnabled;
    private bool networkedRecalling;

    public bool inGrappleAnim, grappleCooldown;
    public AudioSource chargeSound;
    public ParticleSystem shieldPs;

    public GameObject jackpotHitPs;
    private bool disableShield;
    float angle;
    public bool unlockingExecution;
    bool trackerWasActive;
    public ParticleSystem jackpotPs;
    public AudioSource jackpotStartSound, jackpotActiveSound;
    public bool jackpotActive;
    public int healthOnJackpotStart;

    public bool teleporting;
    public bool soulBreakerAwakened;
    public AudioSource soulBreakerAwakenSound;
    [SerializeField] float[] catalystEffectTime = new float[4];
    bool[] underCatalystEffect = new bool[4];

    [SerializeField] item[] catalysts;
    [SerializeField] Color[] catalystColors = new Color[4];
    List<int> activeCatalysts = new List<int>();
    private int catalystStackLimit;

    //public Vector2 valeTeleportStartPos;
    private Vector3 lastFramePosition;
    public float jackpotAdditionalTime;
    public float jackpotRegenHealth;
    public Coroutine jackpotCo;
    public const float slothRange = 3.2f;
    float slothMeterTimer = 0;
    private Vector2 tilePreviewBoatOffset;
    private bool grappleBuffered;

    public List<enemyHealth> enemiesHitThisSwing;
    public AudioSource bindingSound;
    bool lifeStealThisHit;
    public AudioSource reflectSound;
    bool wagerShow;
    Tweener wagerTween;

    public int profiteerState = 0;
    public item[] profiteerStateItems;
    public AudioSource profiteerSwitchSound;
    private float holdCharmTimer = 0;
    private bool trackerPulsing;
    float trackerPulseTimer = 0;
    public AudioSource trackerSound;

    [Header("Network")]
    [Networked] public float yScale { get; set; }
    [Networked] public float xScale { get; set; }

    [Networked] public bool recalling { get; set; }
    [Networked] public bool valeRecalling { get; set; }
    [Networked] public bool trackerActivated { get; set; }
    [Networked] public bool inCleave { get; set; }
    [Networked] public float net_angle { get; set; }
    [Networked] public bool net_extendedReach { get; set; }

    [Networked] public string net_charmEffect { get; set; }


    private void Awake()
    {
        flowerTrail.material.color = new Color(flowerTrail.material.color.r, flowerTrail.material.color.g, flowerTrail.material.color.b, 0);

        catalystLightIntensity = catalystLight.intensity;
        playerParent = GetComponentInParent<player>();
        HandEvents = GetComponentInChildren<handEvents>();
        netAnim = GetComponentInChildren<NetworkMecanimAnimator>();
    }

    void Start()
    {
        if (view.HasInputAuthority)
        {
            instance = this;
            tilePreview.SetParent(null);
        }
        else
        {
            slothCircle.enabled = false;
        }

        wagerText.alpha = 0;

        cleaveTrail.emitting = false;
        swingTrail.time = 0;

        catalystLight.intensity = 0;
        defaultEatFinishSound = eatFinishSound.clip;

        cooldownTime = cooldown;
        normalHandSortOrder = sprite.sortingOrder;

        recallSoundVol = recallSound.volume;
        recallSound.volume = 0;

        //valeTeleportStartPos = playerSpawner.instance.savedValeTeleportInitialPos;
    }

    void Update()
    {
        //base multiplier
        float swingSpeedMult = 1.1f;

        if (jackpotActive)
        {
            swingSpeedMult *= 1.3f;
        }
        if (playerParent.health.inEnvyState)
        {
            swingSpeedMult *= 1.35f;
        }

        swingSpeedMult *= DungeonGenerator.instance.GetModifier("attack speed");
        swingSpeedMult *= inventory.instance.GetModifierInToolSlot("swing");

        if (view.HasStateAuthority)
        {
            animator.SetFloat("Speed", 1 * swingSpeedMult);
        }
        else
        {
            animator.SetFloat("Speed", 1.3f * swingSpeedMult);
        }

        twoHandSprite.color = sprite.color;

        handMask.transform.position = sprite.transform.position;
        handMask.transform.rotation = sprite.transform.rotation;
        handMask.sprite = sprite.sprite;
        handMask.transform.localScale = sprite.transform.lossyScale;
        handMask.enabled = playerParent.mask.enabled;

        itemMask.transform.position = playerParent.heldItemSprite.transform.position;
        itemMask.transform.rotation = playerParent.heldItemSprite.transform.rotation;
        itemMask.sprite = playerParent.heldItemSprite.sprite;
        itemMask.transform.localScale = playerParent.heldItemSprite.transform.lossyScale;
        itemMask.enabled = playerParent.heldItemSprite.enabled && playerParent.mask.enabled;

        handMask2.transform.position = twoHandSprite.transform.position;
        handMask2.transform.rotation = twoHandSprite.transform.rotation;
        handMask2.sprite = twoHandSprite.sprite;
        handMask2.enabled = twoHandSprite.gameObject.activeSelf && twoHandSprite.enabled && playerParent.mask.enabled;
        handMask2.transform.localScale = twoHandSprite.transform.lossyScale;

        if (playerParent.currentBoat && playerParent.currentBoat.transform.localScale.x < 0)
        {
            handMask.transform.localScale = new Vector2(handMask.transform.localScale.x * -1, handMask.transform.localScale.y);
            handMask2.transform.localScale = new Vector2(handMask2.transform.localScale.x * -1, handMask2.transform.localScale.y);
            itemMask.transform.localScale = new Vector2(itemMask.transform.localScale.x * -1, itemMask.transform.localScale.y);
        }

        if (selectedItem)
        {
            handSpeed = selectedItem.mouseSpeed;

            float damageMult = 1;
            if (inCleave) damageMult *= inventory.cleaveDamageMultiplier;
            if (playerParent.prideActive) damageMult *= player.prideMult;
            if (selectedItem.twoHanded) damageMult *= DungeonGenerator.instance.GetModifier("two hand damage");
            damageMult *= DungeonGenerator.instance.GetModifier("damage dealt");

            foreach (damagePlayer p in hitboxes)
            {
                p.damage = Mathf.RoundToInt(selectedItem.damage * damageMult * (1 + (inventory.instance.GetStatAmount(4) / 10f * inventory.instance.maxStrengthMult)));
            }

            animator.SetBool("Sword", selectedItem.type == item.itemType.tool && !selectedItem.disableSwordAnim);

            swingTrail.widthMultiplier = selectedItem.trailThicknessMult;
        }
        else
        {
            animator.SetBool("Sword", false);

            handSpeed = 10;

            foreach (damagePlayer p in hitboxes)
            {
                p.damage = Mathf.RoundToInt(handDamage * (1 + (inventory.instance.GetStatAmount(4) / 10f * inventory.instance.maxStrengthMult)));
            }
        }

        controllingBoat = playerParent.pilotingBoat || (playerParent.inBoatAltAction && (playerParent.currentBoat.BoatType == Boat.boatType.battleship
            || (playerParent.currentBoat.BoatType == Boat.boatType.ghost && playerParent.currentAltAction == 1)));

        if (handCanFlip)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(transform.localScale.x, yScale), Time.deltaTime * handScaleLerpSpeed);
        }
        else
        {
            float yScale = transform.localScale.y > 0 ? 1 : -1;

            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(transform.localScale.x, yScale), Time.deltaTime * handScaleLerpSpeed * 3);
        }

        if (Mathf.Abs(transform.localScale.y) > 0.99) transform.localScale = Vector3Int.RoundToInt(transform.localScale);

        if (jackpotActive && net_charmEffect != "greed")
        {
            CancelJackpot();
        }
        if (jackpotActiveSound.volume == 0) jackpotActiveSound.Stop();

        //catalyst stuff
        //int catalystsActive = 0;
        activeCatalysts = new List<int>();

        for (int i = 0; i < catalystEffectTime.Length; i++)
        {
            if (catalystEffectTime[i] > 0)
            {
                activeCatalysts.Add(i);

                catalystEffectTime[i] -= Time.deltaTime;

                switch (i)
                {
                    case 0:
                        if (view.HasStateAuthority)
                            playerWater.instance.infiniteStamina = true;
                        break;
                    case 1:
                        if (view.HasStateAuthority)
                            playerHealth.instance.currentCatalystRegenInterval = (float)(catalysts[i].catalystEffectStrength) / 100;
                        break;
                    case 2:
                        playerParent.strengthMult =
                            (float)(catalysts[i].catalystEffectStrength) / 100;
                        break;
                    case 3:
                        playerParent.speedMult =
                            (float)(catalysts[i].catalystEffectStrength) / 100;
                        break;
                }
                underCatalystEffect[i] = true;

            }
            else
            {
                catalystEffectTime[i] = 0;
                switch (i)
                {
                    case 0:
                        if (view.HasStateAuthority)
                            playerWater.instance.infiniteStamina = false;
                        break;
                    case 1:
                        if (view.HasStateAuthority)
                            playerHealth.instance.currentCatalystRegenInterval = 0;
                        break;
                    case 2:
                        playerParent.strengthMult = 1;
                        break;
                    case 3:
                        playerParent.speedMult = 1;
                        break;
                }

                if (underCatalystEffect[i])
                {
                    underCatalystEffect[i] = false;

                    if (!playerHealth.instance.dead)
                        catalystDoneSound.Play();
                }

            }
        }

        // Update particle system color gradient if any catalysts are active
        if (activeCatalysts.Count > 0)
        {
            Gradient gradient = UpdateCatalystGradient(activeCatalysts);
            float r = 0;
            float g = 0;
            float b = 0;
            foreach (GradientColorKey k in gradient.colorKeys)
            {
                r += k.color.r;
                g += k.color.g;
                b += k.color.b;
            }
            catalystLight.color = Color.Lerp(catalystLight.color, new Color(r / gradient.colorKeys.Length, g / gradient.colorKeys.Length, b / gradient.colorKeys.Length), Time.deltaTime * 3);

            catalystLight.intensity = Mathf.Lerp(catalystLight.intensity, catalystLightIntensity, Time.deltaTime * 2);

            if (!catalystPs.isPlaying)
            {
                catalystPs.Play();
            }

        }
        else
        {
            catalystLight.intensity = Mathf.Lerp(catalystLight.intensity, 0, Time.deltaTime * 2);
            catalystPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Vector3 localPos = new Vector2(0, -0.1f);
        if (net_extendedReach && HandEvents.hitboxEnabled)
        {
            if (!twoHandSprite.enabled)
                transform.localPosition = Vector2.Lerp(transform.localPosition, localPos + (transform.right * 0.6f * playerParent.transform.parent.localScale.x), Time.deltaTime * 12);
            else
                transform.localPosition = Vector2.Lerp(transform.localPosition, localPos + (transform.right * 0.7f * playerParent.transform.parent.localScale.x), Time.deltaTime * 12);
        }
        else
        {
            transform.localPosition = Vector2.Lerp(transform.localPosition, localPos, Time.deltaTime * 6);
            if (transform.localPosition != localPos && Vector2.Distance(transform.localPosition, localPos) < 0.02f)
            {
                transform.localPosition = localPos;
            }

        }


        if (view.HasInputAuthority)
        {
            net_charmEffect = inventory.charmEffect;

            if (inventory.charmEffect == "catalyst")
            {
                catalystStackLimit = 1;
            }
            else
            {
                catalystStackLimit = 0;
            }

            if (inventory.charmEffect == "arc")
            {
                net_extendedReach = true;
            }
            else
            {
                net_extendedReach = false;
            }

            if (inventory.charmEffect == "sloth" && !disableSlothGain)
            {
                if (Enemy.enemiesInSlothRange > 0)
                {
                    slothCircle.color = new Color(slothCircle.color.r, slothCircle.color.g, slothCircle.color.b
                        , Mathf.Lerp(slothCircle.color.a, 0.4f, Time.deltaTime * 5));

                    slothMeterTimer += Time.deltaTime * (0.3f + (Enemy.enemiesInSlothRange));
                    if (slothMeterTimer > 0.15f && menu.instance.mushLevel < 100)
                    {
                        slothMeterTimer = 0;
                        menu.instance.mushLevel += 1;
                        menu.instance.ChangeMushLevel();
                    }
                }
                else
                {
                    slothCircle.color = new Color(slothCircle.color.r, slothCircle.color.g, slothCircle.color.b
                        , Mathf.Lerp(slothCircle.color.a, 0, Time.deltaTime * 4));
                }
            }
            else
            {
                slothCircle.color = new Color(slothCircle.color.r, slothCircle.color.g, slothCircle.color.b, 0);
            }

            if (net_charmEffect == "envy" && !playerParent.health.inEnvyState)
            {
                if (menu.instance.mushLevel < 100 && InputManager.actions["Charm"].held)
                {
                    holdCharmTimer += Time.deltaTime;
                    while (holdCharmTimer > 1 / 180f)
                    {
                        menu.instance.mushLevel++;
                        holdCharmTimer -= 1 / 180f;
                    }
                    menu.instance.ChangeMushLevel();


                }
                else if(menu.instance.mushLevel > 0)
                {
                    holdCharmTimer += Time.deltaTime;
                    while (holdCharmTimer > 1 / 240f)
                    {
                        menu.instance.mushLevel -= 1;
                        holdCharmTimer -= 1 / 240f;
                    }
                    menu.instance.ChangeMushLevel();
                }
                
                if (menu.instance.mushLevel >= 100)
                {
                    menu.instance.mushLevel = 0;
                    menu.instance.ChangeMushLevel();
                    StartCoroutine(playerParent.health._EnvyState("", true));
                }
            }
            else
            {
                holdCharmTimer = 0;
            }


            if (jackpotActive)
            {
                jackpotRegenHealth += Time.deltaTime * 25;
                if (jackpotRegenHealth > healthOnJackpotStart) jackpotRegenHealth = healthOnJackpotStart;

                if (playerParent.health.currentHealth < jackpotRegenHealth)
                {
                    playerParent.health.currentHealth = Mathf.RoundToInt(jackpotRegenHealth);
                }

            }

            if (selectedItem != inventorySlot.equippedSlot.itemInSlot)
            {
                selectedItem = inventorySlot.equippedSlot.itemInSlot;
                if (!inventory.instance.gameObject.activeSelf)
                {
                    selectedItem = null;
                }

                if (selectedItem != null)
                    RPC_SetItem(selectedItem.itemId,inventorySlot.equippedSlot.durability <= 0 && selectedItem.uniqueType != item.UniqueType.gavel);
                else
                    RPC_SetItem("null",false);
            }
            if (!inventory.instance.gameObject.activeSelf)
            {
                selectedItem = null;
            }

            if (!HandEvents.grappling)
            {
                if (angle < -90 || angle > 90)
                {
                    if (yScale != -1)
                    {
                        yScale = -1;
                    }

                    playerParent.spriteHolder.localScale = new Vector2(-1, 1);
                }
                else
                {
                    if (yScale != 1)
                    {
                        yScale = 1;
                    }

                    playerParent.spriteHolder.localScale = new Vector2(1, 1);
                }

            }

            //profiteer state switch
            if (inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.profiteer && inventorySlot.equippedSlot.itemData.tier >= 4)
            {
                if (inventorySlot.equippedSlot.itemInSlot != profiteerStateItems[profiteerState] && !HandEvents.hitboxEnabled && profiteerStateItems.Contains(inventorySlot.equippedSlot.itemInSlot))
                {
                    item i = profiteerStateItems[profiteerState];
                    ItemData itemDataTemp = inventorySlot.equippedSlot.itemData;
                    int durabilityTemp = inventorySlot.equippedSlot.durability;
                    inventory.instance.removeItem(inventorySlot.equippedSlot);
                    inventory.instance.addItemInSlot(i, 1, durabilityTemp, itemDataTemp, inventorySlot.equippedSlot
                        , inventorySlot.equippedSlot.transform.position);

                    playerParent.heldItemSprite.sprite = null;
                    playerParent.heldItemSprite.material = null;

                    playerParent.heldItemSprite.sprite = selectedItem.naturalSpawnSprite;

                    swingStored = false;
                    swingCooldownTime = 0;

                    if (selectedItem.matOverride != null && inventorySlot.equippedSlot.durability > 0)
                    {
                        playerParent.heldItemSprite.material = selectedItem.matOverride;
                    }
                    else
                    {
                        playerParent.heldItemSprite.material = defaultMat;
                    }

                    RPC_ProfiteerSwitchSound();

                }

            }

            //ominous flower trail
            if (selectedItem && selectedItem.TileType == item.tileType.ominousFlower && !gameManager.inBoss)
            {
                if (!flowerTrailEnabled)
                {
                    flowerTrailEnabled = true;
                    flowerTrail.enabled = true;
                    flowerTrail.material.DOFade(1, 0.5f);
                }
                flowerTrail.SetPosition(1, playerParent.heldItemSprite.transform.position);
                flowerTrail.SetPosition(0, new Vector2(12, -2.3f));
            }
            else
            {
                if (flowerTrailEnabled)
                {
                    flowerTrail.material.DOFade(0, 0.1f);
                    flowerTrailEnabled = false;
                }
                if (flowerTrail.material.color.a == 0)
                {
                    flowerTrail.enabled = false;
                }
                else
                {
                    flowerTrail.SetPosition(1, playerParent.heldItemSprite.transform.position);
                    flowerTrail.SetPosition(0, new Vector2(12, -2.3f));
                }
            }

            roundedMousePos = new Vector2(Mathf.RoundToInt(mousePosition.x), Mathf.RoundToInt(mousePosition.y));

            if (!inventory.instance.inventoryOpen && !playerHealth.instance.beingKnocked && !gameManager.instance.writingSign
                && !playerHealth.instance.dead && !pauseScreen.instance.paused && !Chat.Instance.chatOpen && !player.instance.inCutscene
                && !menu.instance.waitingForReady && !unlockingExecution)
            {
                if (placeBufferTimer > 0)
                    placeBufferTimer -= Time.deltaTime;

                if (InputManager.actions["Item Primary"].started && selectedItem && (selectedItem.type == item.itemType.block || selectedItem.type == item.itemType.remover
                    || selectedItem.type == item.itemType.bucketE || selectedItem.type == item.itemType.bucketF))
                    placeBufferTimer = placeBuffer;


                if (eating || recalling || menu.instance.mapMaximized)
                {
                    mousePosition = new Vector2(mousePosition.x, transform.position.y);
                }
                else
                {
                    //aim towards mouse
                    mousePosition = InputManager.instance.mousePos;
                    mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
                }

                if (!inCleave && !(HandEvents.grappleCast || HandEvents.grappling))
                {

                    //change rotation
                    direction = Vector3.Slerp(direction, new Vector3(mousePosition.x - transform.position.x, mousePosition.y - transform.position.y).normalized, Time.deltaTime * handSpeed);

                    if (DungeonGenerator.instance.GetModifier("reverse hand") == 2)
                        angle = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
                    else
                        angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                    if (controllingBoat)
                    {
                        angle = 0;

                        if (playerParent.currentBoat.transform.localScale.x < 0) angle = 180;
                    }

                    transform.eulerAngles = new Vector3(0, 0, angle);
                }

                net_angle = angle;

                if (cooldownTime < cooldown)
                {
                    cooldownTime += Time.deltaTime;
                }


                float itemBuildDist = buildDist;
                if (selectedItem && selectedItem.overrideBuildDist != -1) itemBuildDist = selectedItem.overrideBuildDist;

                canBuild = ((playerWater.instance.waterValue < 2 || player.instance.currentBoat)
                    && Vector2.Distance(player.instance.transform.position, mousePosition) <= itemBuildDist);

                if (player.instance.currentBoat && Boat.highlightedTileSlotIndex > -1)
                {
                    if (player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[0] != null
                        && player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[0].Item == selectedItem)
                        canBuild = false;
                    if (player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[1] != null
                        && player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[1].Item == selectedItem)
                        canBuild = false;
                }

                if (!canBuild)
                {
                    placedTile.highlightedTile = null;
                }

                if (!pauseScreen.instance.paused && !inventory.instance.inventoryOpen && !player.instance.inBoatAltAction && !player.instance.pilotingBoat
                    && (TileEntity.playersOnPuddle < playerSpawner.playerCountInGame || gameManager.inBoss) && !Chat.Instance.chatOpen)
                {

                    if (placeBufferTimer > 0 && selectedItem && selectedItem.type == item.itemType.block
                        && canBuild && !obstructed)
                    {

                        if (player.instance.currentBoat)
                        {
                            //boat place block
                            gameManager.instance.RPC_CreateBoatTile(DataPersistanceManager.userId, selectedItem.itemId, false, "",
                                player.instance.currentBoat.view.Id, Boat.highlightedTileSlotIndex);
                        }
                        else
                        {
                            //default place block
                            gameManager.instance.PlaceBlock(selectedItem.itemId, roundedMousePos, tileRotation, tileState, false, "");
                        }

                        if(selectedItem.growthStages != null && selectedItem.growthStages.Length > 1)
                        {
                            gameManager.endScreenStats["planted"]++;
                        }

                        placeBufferTimer = 0;
                        MouseCursor.instance.anim.SetTrigger("Place");

                        if (!inventorySlot.equippedSlot.itemInSlot.usesDurability)
                            inventorySlot.equippedSlot.itemCountInSlot -= 1;
                        else
                            ChangeDurability(-1, inventorySlot.equippedSlot);

                        RPC_PlaceAnimation();

                    }
                    else
                        //fill bucket
                        if (placeBufferTimer > 0 && selectedItem &&
                            (selectedItem.type == item.itemType.bucketE || (selectedItem.type == item.itemType.bucketF && inventorySlot.equippedSlot.durability < selectedItem.maxDurability))
                            && !player.instance.currentBoat && canBuild && hoveringOverWaterTile)
                        {
                            placeBufferTimer = 0;
                            //fill empty bucket
                            if (selectedItem.type == item.itemType.bucketE)
                            {
                                inventory.instance.removeItem(inventorySlot.equippedSlot);
                                inventory.instance.addItemInSlot(selectedItem.durabilityEmptyItem, 1, selectedItem.durabilityEmptyItem.maxDurability, new ItemData(0), inventorySlot.equippedSlot, inventorySlot.equippedSlot.transform.position);
                            }
                            //refill bucket missing some water
                            else
                            {
                                ChangeDurability(selectedItem.maxDurability - inventorySlot.equippedSlot.durability, inventorySlot.equippedSlot);
                            }
                            RPC_BucketPickupSound();
                            RPC_PlaceAnimation();

                            MouseCursor.instance.anim.SetTrigger("Place");
                        }
                        else
                            //water plants
                            if (placeBufferTimer > 0 && selectedItem && selectedItem.type == item.itemType.bucketF && !player.instance.currentBoat && canBuild)
                            {
                                if (waterableTile.Item && waterableTile.PlacedTile.tileEntity.wateredTime <= 0 && waterableTile.Item.wateredAdditionalGrowth > 0)
                                {
                                    placeBufferTimer = 0;
                                    ChangeDurability(-1, inventorySlot.equippedSlot);

                                    gameManager.instance.SetWatered((Vector2)waterableTile.PlacedTile.transform.position, UnityEngine.Random.Range(waterableTile.Item.wateredTicks.x, waterableTile.Item.wateredTicks.y + 1), false);
                                    RPC_PlaceAnimation();

                                    MouseCursor.instance.anim.SetTrigger("Place");
                                }

                            }
                            else
                                //eat food
                                if (selectedItem && (selectedItem.type == item.itemType.food ||
                                    (selectedItem.type == item.itemType.catalyst && ((activeCatalysts.Count <= catalystStackLimit && !activeCatalysts.Contains((int)selectedItem.catalystEffect - 1))
                                    || (activeCatalysts.Count <= catalystStackLimit + 1 && selectedItem.catalystEffect == item.CatalystEffect.random))))
                                    && !player.instance.pilotingBoat)
                                {
                                    if (eating)
                                    {
                                        sprite.sortingOrder = handEatingSortOrder;
                                        eatTimer -= Time.deltaTime;
                                        if (eatTimer <= 0 && !inEatCooldown)
                                        {
                                            RPC_EatSound(selectedItem.itemId);

                                            inventorySlot.equippedSlot.itemCountInSlot--;
                                            inEatCooldown = true;

                                            //catalyst
                                            if (selectedItem.catalystEffect != item.CatalystEffect.none)
                                            {
                                                float allCatalystsAdditionalTime = 0;

                                                item item = selectedItem;
                                                //rainbow catalyst
                                                if (item.catalystEffect == global::item.CatalystEffect.random)
                                                {
                                                    int failsafe = 0;

                                                    allCatalystsAdditionalTime = 2.5f;

                                                    item = catalysts[UnityEngine.Random.Range(0, catalysts.Length)];

                                                    while (activeCatalysts.Contains((int)item.catalystEffect - 1))
                                                    {
                                                        item = catalysts[UnityEngine.Random.Range(0, catalysts.Length)];
                                                        failsafe++;
                                                        if (failsafe > 100)
                                                        {
                                                            print(failsafe);
                                                            break;
                                                        }
                                                    }

                                                }
                                                RPC_CatalystEffect(item.itemId, allCatalystsAdditionalTime);
                                            }
                                            //food item
                                            else
                                            {
                                                playerHealth.instance.currentHealth += selectedItem.restoreHealth;

                                                gameManager.endScreenStats["food"]++;

                                                if (selectedItem.charmEffect == "bounce")
                                                {
                                                    player.instance.Dash(0.55f);
                                                }

                                            }

                                        }
                                    }

                                    if (InputManager.actions["Item Primary"].held && !inEatCooldown)
                                    {
                                        if (!eating)
                                        {
                                            eating = true;
                                            eatTimer = selectedItem.eatTime;
                                        }

                                    }
                                    else if (eating)
                                    {
                                        eating = false;
                                        sprite.sortingOrder = normalHandSortOrder;
                                    }

                                    //eat cooldown
                                    if (inEatCooldown)
                                    {
                                        eatTimer -= Time.deltaTime;
                                        eating = false;
                                        if (eatTimer <= -0.5f)
                                        {
                                            inEatCooldown = false;
                                            eatTimer = selectedItem.eatTime;
                                        }
                                    }
                                }
                                //recall
                                else if (selectedItem && selectedItem.type == item.itemType.recall && !teleporting)
                                {
                                    bool valeRecalling = InputManager.actions["Item Secondary"].held && !InputManager.actions["Item Primary"].held;

                                    if (((InputManager.actions["Item Primary"].held && !InputManager.actions["Item Secondary"].held || ((InputManager.actions["Item Secondary"].held && !InputManager.actions["Item Primary"].held))))
                                        && recallTimer >= 0 && (TileEntity.currentSpawnPoint || valeRecalling || playerParent.inDungeon > -1) && !(gameManager.inBoss && playerParent.health.respawnPos.magnitude >= 300)
                                        && !gameManager.inSecondPhase && !((gameManager.inBoss || playerParent.inDungeon > -1) && valeRecalling))
                                    {
                                        this.valeRecalling = valeRecalling;
                                        if (!recalling)
                                        {
                                            recalling = true;
                                            recallTimer = 0.01f;

                                            ParticleSystem p = recallPs;
                                            if (valeRecalling) p = valeRecallPs;

                                            var ps = p.main;
                                            ps.simulationSpeed = 1f;
                                            p.Play();

                                            recallSound.Play();
                                            recallSound.volume = 0;
                                            recallSound.pitch = 1;
                                            if (valeRecalling) recallSound.pitch = 0.85f;

                                            recallSound.DOFade(recallSoundVol, 1);
                                        }
                                        else
                                        {
                                            recallTimer += Time.deltaTime;

                                            if (recallTimer > recallTime)
                                            {
                                                teleporting = true;

                                                StartCoroutine(_TetherTeleport(valeRecalling, playerParent.health.respawnPos, playerParent.inDungeon > -1));
                                            }

                                        }
                                    }
                                    else
                                    {
                                        if (recalling)
                                        {
                                            recalling = false;
                                            recallPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                                            valeRecallPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                                            StartCoroutine(_StopRecallSound());

                                            ParticleSystem p = recallPs;
                                            if (this.valeRecalling) p = valeRecallPs;

                                            var ps = p.main;
                                            ps.simulationSpeed = 2f;
                                            if (recallTimer >= 0)
                                            {
                                                recallTimer = -2.5f;
                                            }

                                            this.valeRecalling = false;
                                        }
                                    }


                                }
                                else if (!player.instance.duelGhostSpectator)
                                {
                                    //sword swing
                                    if ((InputManager.actions["Item Primary"].started || swingStored) && !pauseScreen.instance.paused && !(selectedItem && (selectedItem.type == item.itemType.block || selectedItem.type == item.itemType.remover
                                        || selectedItem.type == item.itemType.armorHead || selectedItem.type == item.itemType.armorChest || selectedItem.type == item.itemType.armorLegs || selectedItem.type == item.itemType.map
                                        || selectedItem.type == item.itemType.bucketF || selectedItem.type == item.itemType.bucketE || selectedItem.type == item.itemType.grapple || selectedItem.uniqueType == item.UniqueType.shield || selectedItem.type == item.itemType.charm
                                        || (heldItemBroken && inventorySlot.equippedSlot.itemInSlot.uniqueType != item.UniqueType.gavel))))
                                    {
                                        if (swingCooldownTime <= 0)
                                        {
                                            if (inventorySlot.equippedSlot.itemInSlot && (inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.execution
                                                || inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.attunement))
                                                netAnim.SetTrigger("Swing Exec",true);
                                            else
                                                netAnim.SetTrigger("Swing",true);

                                            if (((inventorySlot.equippedSlot.itemInSlot && !inventorySlot.equippedSlot.itemInSlot.twoHanded
                                                && inventorySlot.equippedSlot.itemInSlot.uniqueType != item.UniqueType.execution))
                                                || !inventorySlot.equippedSlot.itemInSlot)
                                            {
                                                swingCooldownTime = 0.6f / swingSpeedMult;
                                                MouseCursor.instance.anim.SetFloat("Spin Speed", 1.05f);
                                            }
                                            else
                                            {
                                                swingCooldownTime = 0.9f / swingSpeedMult;
                                                MouseCursor.instance.anim.SetFloat("Spin Speed", 0.75f);
                                            }

                                            swingStored = false;
                                        }
                                        //prevents double click
                                        else if (swingCooldownTime < 0.3f)
                                        {
                                            swingStored = true;
                                        }

                                    }
                                }

                    //shielding
                    if (selectedItem && selectedItem.uniqueType == item.UniqueType.shield && inventorySlot.equippedSlot.durability > 0)
                    {
                        animator.SetBool("Shielding", InputManager.actions["Item Secondary"].held);

                        if (InputManager.actions["Item Primary"].started || swingStored)
                        {
                            if (!disableShield && swingCooldownTime <= 0)
                            {
                                RPC_ShieldBash();
                                disableShield = true;
                                swingCooldownTime = 0.57f * (1 / DungeonGenerator.instance.GetModifier("attack speed")) / swingSpeedMult;
                                Invoke("EnableShield", 0.57f * (1 / DungeonGenerator.instance.GetModifier("attack speed")) / swingSpeedMult);

                                MouseCursor.instance.anim.SetFloat("Spin Speed", 1f);
                                MouseCursor.instance.anim.SetTrigger("Spin");

                                swingStored = false;
                            }
                            else if (swingCooldownTime < 0.3f)
                            {
                                swingStored = true;
                            }
                        }

                    }
                    else
                    {
                        animator.SetBool("Shielding", false);
                    }


                    //jackpot wager
                    if (selectedItem && selectedItem.uniqueType == item.UniqueType.jackpot && inventorySlot.equippedSlot.itemData.tier >= 4 && !playerHealth.instance.inEnvyState
                        && inventorySlot.equippedSlot.durability > 0)
                    {
                        if (InputManager.actions["Item Secondary"].started && wagerLevel < 5)
                        {
                            wagerLevel++;

                            if (wagerTween != null && wagerTween.IsActive())
                                wagerTween.Kill();

                            if (!wagerShow)
                            {
                                wagerShow = true;
                                wagerText.DOFade(1, 0.3f);
                            }

                            wagerText.transform.localScale = Vector2.one;
                            wagerTween = wagerText.transform.DOShakeScale(0.15f, 0.4f, 2, 0, true, ShakeRandomnessMode.Harmonic);
                            wagerText.GetComponent<AudioSource>().Play();

                            switch (wagerLevel)
                            {
                                case 1:
                                    jackpotAdditionalChance = 25f; break;
                                case 2:
                                    jackpotAdditionalChance = 35f; break;
                                case 3:
                                    jackpotAdditionalChance = 40f; break;
                                case 4:
                                    jackpotAdditionalChance = 45f; break;
                                case 5:
                                    jackpotAdditionalChance = 50f; break;
                            }
                        }

                        if (wagerLevel > 0)
                        {
                            wagerText.text = (wagerLevel * hpPerWager) + " HP Wagered";
                            wagerText.text += "\nJackpot Chance: " + (jackpotAdditionalChance) + "%";
                        }
                    }
                    else
                    {
                        wagerLevel = 0;
                        jackpotAdditionalChance = 0;

                        if (wagerShow)
                        {
                            wagerShow = false;
                            wagerText.DOFade(0, 0.2f);
                        }

                    }

                }

                if (!selectedItem || (selectedItem && (selectedItem.type != item.itemType.food && selectedItem.type != item.itemType.catalyst)) && eating)
                {
                    eating = false;
                    sprite.sortingOrder = normalHandSortOrder;
                }
                playerParent.heldItemSprite.sortingOrder = sprite.sortingOrder - 1;

                animator.SetBool("Eating", eating);
                animator.SetBool("Recalling", recalling);

                //place

                if (cooldownTime < cooldown)
                {
                    cooldownTime += Time.deltaTime;
                }

                tilePreviewSprite.enabled = false;
                boatPreview.gameObject.SetActive(false);

                //tile tips 
                menu.instance.showTileTips = (inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemInSlot.type == item.itemType.block && !player.instance.currentBoat && !inventory.instance.inventoryOpen && !playerHealth.instance.dead
                    && (inventorySlot.equippedSlot.itemInSlot.canRotate || inventorySlot.equippedSlot.itemInSlot.TileType == item.tileType.vehicleStation));

                if (inventorySlot.equippedSlot && !controllingBoat)
                {
                    //building and removing
                    if (selectedItem)
                    {
                        animator.SetBool("Disable Hit", selectedItem.type == item.itemType.block
                            || selectedItem.type == item.itemType.remover
                            || selectedItem.type == item.itemType.map
                            || selectedItem.type == item.itemType.food
                            || selectedItem.type == item.itemType.armorHead
                            || selectedItem.type == item.itemType.armorLegs
                            || selectedItem.type == item.itemType.armorChest
                            || selectedItem.type == item.itemType.catalyst
                            );


                        switch (selectedItem.type)
                        {
                            case item.itemType.block:
                                if (!canBuild)
                                {
                                    tilePreviewSprite.enabled = false;
                                    boatPreview.gameObject.SetActive(false);
                                    break;
                                }

                                obstructed = false;

                                if (!selectedItem.floor)
                                {
                                    RaycastHit2D[] obstCheck = Physics2D.BoxCastAll(roundedMousePos, new Vector2(0.9f, 0.9f), 0, Vector2.zero, Mathf.Infinity, obstructionLayers);

                                    foreach (RaycastHit2D obst in obstCheck)
                                    {
                                        //player collision for other players is trigger
                                        if (obst.collider.isTrigger == false || obst.collider.gameObject.layer == 10)
                                        {
                                            obstructed = true;
                                            break;
                                        }
                                    }

                                }
                                else
                                {
                                    RaycastHit2D[] obstCheck = Physics2D.BoxCastAll(roundedMousePos, new Vector2(0.95f, 0.95f), 0, Vector2.zero, Mathf.Infinity, floorObstructionLayers);

                                    foreach (RaycastHit2D obst in obstCheck)
                                    {
                                        if (obst.collider.isTrigger == false)
                                        {
                                            obstructed = true;
                                            break;
                                        }
                                    }
                                }

                                if (MapDisplay.Instance.groundTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + mousePosCorrection)) == playerWater.instance.shallowWater
                                    || MapDisplay.Instance.groundTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + mousePosCorrection)) == null
                                    || MapDisplay.Instance.lavaCollisionTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + mousePosCorrection)) != null)
                                {
                                    item.tileType tType = inventorySlot.equippedSlot.itemInSlot.TileType;


                                    if (tType == item.tileType.floor || tType == item.tileType.wall || tType == item.tileType.door)
                                    {
                                        if (gameManager.instance.placedTiles.Find(t => Vector2.Distance(t.position, roundedMousePos) <= 1f).Item == null)
                                        {
                                            obstructed = true;
                                        }
                                        else
                                        {
                                            obstructed = false;
                                        }
                                    }
                                    //only allow interactable tiles to be on floors when in water
                                    else
                                    {
                                        if (gameManager.instance.placedTiles.Find(t => t.position == roundedMousePos && t.Item.floor).Item == null)
                                        {
                                            obstructed = true;
                                        }
                                    }

                                }

                                //obstructed outside of world border
                                if (roundedMousePos.magnitude > 4180) obstructed = true;

                                if (selectedItem.TileType == item.tileType.respawn && MapDisplay.Instance.lavaTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + mousePosCorrection)))
                                    obstructed = true;

                                //only allow boat station in water
                                if (selectedItem.TileType == item.tileType.vehicleStation)
                                {
                                    Vector3 boatCenter = roundedMousePos + (Vector2)boatPreview.transform.localPosition;
                                    Vector3 halfSize = boatCheckSize / 2f;

                                    // Calculate tile-aligned bounds
                                    Vector3 bottomLeft = boatCenter - halfSize;
                                    Vector3 topRight = boatCenter + halfSize;

                                    Vector3Int min = playerWater.instance.groundTilemap.WorldToCell(bottomLeft);
                                    Vector3Int max = playerWater.instance.groundTilemap.WorldToCell(topRight);

                                    for (int x = min.x; x <= max.x; x++)
                                    {
                                        for (int y = min.y; y <= max.y; y++)
                                        {
                                            Vector3Int tilePos = new Vector3Int(x, y, 0);
                                            var tile = playerWater.instance.groundTilemap.GetTile(tilePos);

                                            if (tile != null && tile != playerWater.instance.shallowWater)
                                            {
                                                obstructed = true;
                                                break;
                                            }
                                        }
                                        if (obstructed) break;
                                    }

                                }

                                List<TileData> TilesOnPos = new List<TileData>();
                                TilesOnPos = gameManager.instance.placedTiles.FindAll(data => data.position == roundedMousePos);

                                tilePreviewSprite.enabled = true;

                                //dont allow 2 floors or 2 walls on same tile
                                foreach (TileData data in TilesOnPos)
                                {
                                    if (data.Item.floor == selectedItem.floor)
                                    {
                                        obstructed = true;
                                        if (data.Item == selectedItem) tilePreviewSprite.enabled = false;

                                        break;
                                    }
                                }

                                //only place seeds on farmland
                                if (selectedItem.onlyPlaceOnTiles != null && selectedItem.onlyPlaceOnTiles.Count() > 0)
                                {
                                    bool placeable = false;
                                    foreach (TileData data in TilesOnPos)
                                    {
                                        if (selectedItem.onlyPlaceOnTiles.Contains(data.Item))
                                        {
                                            placeable = true;
                                            break;
                                        }
                                    }
                                    if (!placeable) obstructed = true;

                                }
                                //only place plants on grass or sand
                                if (selectedItem.onlyPlaceOnGroundTiles != null && selectedItem.onlyPlaceOnGroundTiles.Count() > 0)
                                {
                                    if (!selectedItem.onlyPlaceOnGroundTiles.Contains(MapDisplay.Instance.groundTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + new Vector2(-0.7f, -0.7f))))
                                        || !MapDisplay.Instance.groundTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + new Vector2(-1.7f, -0.7f)))
                                        || !MapDisplay.Instance.groundTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + new Vector2(0.3f, -0.7f)))
                                        || !MapDisplay.Instance.groundTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + new Vector2(-0.7f, -1.7f)))
                                        || !MapDisplay.Instance.groundTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + new Vector2(-0.7f, 0.3f)))
                                        //no place farmland on water
                                        || MapDisplay.Instance.waterTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + new Vector2(-0.7f, -0.7f)))
                                        || MapDisplay.Instance.lavaTilemap.GetTile(Vector3Int.RoundToInt(roundedMousePos + new Vector2(-0.7f, -0.7f)))
                                        )
                                    {
                                        obstructed = true;
                                    }
                                }
                                //only place flower on the spot
                                if (selectedItem.onlyPlaceAtPositions.Length > 0)
                                {
                                    obstructed = true;
                                    foreach (Vector2 pos in selectedItem.onlyPlaceAtPositions)
                                    {
                                        if (roundedMousePos == pos && !gameManager.inBoss) obstructed = false;
                                    }

                                    //no fight boss in arena
                                    if (gameManager.instance.inArena) obstructed = true;

                                }

                                if (player.instance.currentBoat && Boat.highlightedTileSlotIndex > -1 && selectedItem.allowOnBoat)
                                {
                                    obstructed = false;

                                    if (player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[0] != null
                                        && !(selectedItem.onlyPlaceOnTiles.Contains(player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[0].Item)))
                                        obstructed = true;
                                    if (player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[1] != null
                                        && !(selectedItem.onlyPlaceOnTiles.Contains(player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[1].Item)))
                                        obstructed = true;
                                }

                                if (!obstructed)
                                {
                                    tilePreviewSprite.material.SetColor("_Color", canColor);
                                    boatPreview.color = canColor;
                                }
                                else
                                {
                                    tilePreviewSprite.material.SetColor("_Color", CantColor);
                                    boatPreview.color = CantColor;
                                }

                                placedTile.highlightedTile = null;

                                if (InputManager.actions["Item Secondary"].started)
                                {
                                    tileState += 1;
                                }

                                if (tileState >= selectedItem.tileStates.Length)
                                {
                                    tileState = 0;
                                }

                                tilePreviewSprite.sprite = selectedItem.tileStates[tileState].tileSprite;
                                tilePreviewSprite.sortingLayerName = selectedItem.tileStates[tileState].sortLayerName;
                                tilePreviewSprite.sortingOrder = selectedItem.tileStates[tileState].sortOrder;

                                if (player.instance.currentBoat)
                                {
                                    tilePreviewSprite.sortingLayerName = "Player";
                                    tilePreviewSprite.sortingOrder = 11;
                                }

                                if (selectedItem.overrideBuildPreviewSprite)
                                    tilePreviewSprite.sprite = selectedItem.overrideBuildPreviewSprite;


                                if (player.instance.currentBoat)
                                {
                                    // Get mouse in world space
                                    Vector2 posWorld = mousePosition;

                                    if (Boat.highlightedTileSlotIndex > -1)
                                    {
                                        posWorld = player.instance.currentBoat.tileSlots[Boat.highlightedTileSlotIndex].position + (Vector3)selectedItem.boatTileOffset;
                                    }

                                    Transform boat = player.instance.currentBoat.transform;
                                    Vector2 targetOffset = posWorld - (Vector2)boat.position;

                                    // Smooth the offset instead of world position
                                    tilePreviewBoatOffset = Vector2.Lerp(tilePreviewBoatOffset, targetOffset, Time.deltaTime * 15);

                                    // Rebuild final world position
                                    tilePreview.position = (Vector2)boat.position + tilePreviewBoatOffset;
                                }
                                //default tile preview snapping
                                else
                                {
                                    tilePreview.position = Vector2.Lerp(tilePreview.position, roundedMousePos, Time.deltaTime * 50);
                                }

                                if (selectedItem.canRotate)
                                {

                                    if (InputManager.actions["Rotate"].started)
                                    {
                                        tileRotation += 90;

                                    }

                                    if (tilePreview.eulerAngles.z >= 359 && tileRotation >= 360)
                                    {
                                        tilePreview.rotation = Quaternion.identity;
                                        tileRotation = 0;
                                    }

                                    tilePreview.eulerAngles = Vector3.Slerp(tilePreview.eulerAngles, new Vector3(0, 0, Mathf.Clamp(tileRotation, 0, 359.99f)), 40 * Time.deltaTime);


                                }
                                else
                                {
                                    //vehicle station needs to rotate
                                    if (selectedItem.TileType != item.tileType.vehicleStation)
                                        tileRotation = 0;

                                    tilePreview.rotation = Quaternion.identity;
                                }
                                //vehicle station press rotate to rotate preview of boat
                                if (selectedItem.TileType == item.tileType.vehicleStation)
                                {
                                    boatPreview.gameObject.SetActive(true);

                                    if (InputManager.actions["Rotate"].started)
                                    {
                                        tileRotation += 90;
                                    }
                                    if (tileRotation >= 360)
                                    {
                                        tileRotation = 0;
                                    }

                                    switch (tileRotation)
                                    {
                                        case 0:
                                            boatPreview.transform.localPosition = new Vector3(gameManager.instance.boatPreviewPositions.x, 0);
                                            boatPreview.transform.localScale = new Vector3(1, 1);
                                            break;
                                        case 90:
                                            boatPreview.transform.localPosition = new Vector3(0, gameManager.instance.boatPreviewPositions.y);
                                            boatPreview.transform.localScale = new Vector3(1, 1);
                                            break;
                                        case 180:
                                            boatPreview.transform.localPosition = new Vector3(-gameManager.instance.boatPreviewPositions.x, 0);
                                            boatPreview.transform.localScale = new Vector3(-1, 1);
                                            break;
                                        case 270:
                                            boatPreview.transform.localPosition = new Vector3(0, -gameManager.instance.boatPreviewPositions.y);
                                            boatPreview.transform.localScale = new Vector3(1, 1);
                                            break;
                                    }


                                }
                                else
                                {
                                    boatPreview.gameObject.SetActive(false);
                                }

                                break;
                            case item.itemType.remover:
                                if (!canBuild) break;

                                //boat logic
                                if (player.instance.currentBoat)
                                {
                                    if (Boat.highlightedTileSlotIndex > -1
                                        && player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex] != null)
                                    {
                                        placedTile t = player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[1];
                                        if (!t) t = player.instance.currentBoat.tileSlotTiles[Boat.highlightedTileSlotIndex].tiles[0];

                                        placedTile.highlightedTile = t;

                                        if (placeBufferTimer > 0 && !pauseScreen.instance.paused && t.Item != null)
                                        {
                                            ChangeDurability(-1, inventorySlot.equippedSlot);

                                            item currentItem = t.Item;
                                            placeBufferTimer = 0;
                                            gameManager.instance.RPC_RemoveBoatTile(player.instance.currentBoat.boatID, t.boatTileIndex, true, t.Item.itemId);
                                            if (!currentItem.usesDurability)
                                                inventory.instance.AddItem(currentItem, 1, 1, new ItemData(0));
                                            RPC_RemoveTileAnim();
                                        }

                                    }
                                    else
                                    {
                                        placedTile.highlightedTile = null;
                                    }


                                    break;
                                }
                                //default logic

                                tilePreviewSprite.enabled = false;

                                TileData currentTile = gameManager.instance.placedTiles.Find(data => data.position == roundedMousePos && data.Item.TileType != item.tileType.floor);
                                if (currentTile.Item == null)
                                    currentTile = gameManager.instance.placedTiles.Find(data => data.position == roundedMousePos && data.Item.TileType == item.tileType.floor);

                                //remove
                                if (currentTile.Item != null && currentTile.Item.TileType != item.tileType.ominousFlower
                                    && !(currentTile.PlacedTile.tileEntity && currentTile.PlacedTile.tileEntity.beingUsed))
                                {
                                    placedTile.highlightedTile = currentTile.PlacedTile;

                                    if (placeBufferTimer > 0 && !pauseScreen.instance.paused && currentTile.Item != null)
                                    {
                                        ChangeDurability(-1, inventorySlot.equippedSlot);

                                        item currentItem = currentTile.Item;
                                        placeBufferTimer = 0;
                                        gameManager.instance.RemoveTile(currentTile.position, currentTile.Item, true);
                                        if (!currentItem.usesDurability)
                                            inventory.instance.AddItem(currentItem, 1, 1, new ItemData(0));
                                        RPC_RemoveTileAnim();
                                    }

                                }
                                else
                                {
                                    placedTile.highlightedTile = null;
                                }

                                break;
                            case item.itemType.bucketF:
                                hoveringOverWaterTile = featurePlacer.Instance.CheckCircleForTiles(new UnityEngine.Tilemaps.TileBase[] { playerWater.instance.shallowWater }, MapDisplay.Instance.waterTilemap, Vector3Int.RoundToInt(roundedMousePos) + new Vector3Int(-1, -1), 1)
                                    && player.instance.inDungeon == -1;

                                waterableTile = gameManager.instance.placedTiles.Find(t => t.position == roundedMousePos && t.PlacedTile.Item.TileType == item.tileType.crop && !t.PlacedTile.tileEntity.watered && t.PlacedTile.Item.wateredAdditionalGrowth > 0);
                                tilePreviewSprite.sprite = selectedItem.overrideBuildPreviewSprite;
                                tilePreviewSprite.enabled = canBuild;
                                tilePreview.position = Vector2.Lerp(tilePreview.position, roundedMousePos, Time.deltaTime * 50);

                                if (waterableTile.Item || (hoveringOverWaterTile && inventorySlot.equippedSlot.durability < selectedItem.maxDurability))
                                {
                                    tilePreviewSprite.material.SetColor("_Color", canColor);
                                }
                                else
                                {
                                    tilePreviewSprite.material.SetColor("_Color", CantColor);
                                }

                                break;
                            case item.itemType.bucketE:
                                hoveringOverWaterTile = featurePlacer.Instance.CheckCircleForTiles(new UnityEngine.Tilemaps.TileBase[] { playerWater.instance.shallowWater }, MapDisplay.Instance.waterTilemap, Vector3Int.RoundToInt(roundedMousePos) + new Vector3Int(-1, -1), 1);

                                tilePreviewSprite.sprite = selectedItem.overrideBuildPreviewSprite;
                                tilePreviewSprite.enabled = canBuild;
                                tilePreview.position = Vector2.Lerp(tilePreview.position, roundedMousePos, Time.deltaTime * 50);

                                if (hoveringOverWaterTile)
                                {
                                    tilePreviewSprite.material.SetColor("_Color", canColor);
                                }
                                else
                                {
                                    tilePreviewSprite.material.SetColor("_Color", CantColor);
                                }

                                break;
                            //put on armor by left clicking
                            case item.itemType.armorHead:
                                tilePreviewSprite.enabled = false;
                                boatPreview.gameObject.SetActive(false);
                                placedTile.highlightedTile = null;
                                if (InputManager.actions["Item Primary"].started)
                                {
                                    ArmorSwitch(inventory.instance.headSlot);
                                }
                                break;
                            case item.itemType.armorChest:
                                tilePreviewSprite.enabled = false;
                                boatPreview.gameObject.SetActive(false);
                                placedTile.highlightedTile = null;
                                if (InputManager.actions["Item Primary"].started)
                                {
                                    ArmorSwitch(inventory.instance.chestplateSlot);
                                }
                                break;
                            case item.itemType.armorLegs:
                                tilePreviewSprite.enabled = false;
                                boatPreview.gameObject.SetActive(false);
                                placedTile.highlightedTile = null;
                                if (InputManager.actions["Item Primary"].started)
                                {
                                    ArmorSwitch(inventory.instance.legsSlot);
                                }
                                break;
                            case item.itemType.charm:
                                tilePreviewSprite.enabled = false;
                                boatPreview.gameObject.SetActive(false);
                                placedTile.highlightedTile = null;
                                if (InputManager.actions["Item Primary"].started)
                                {
                                    ArmorSwitch(inventory.instance.charmSlot);
                                }
                                break;
                            case item.itemType.map:
                                tilePreviewSprite.enabled = false;
                                boatPreview.gameObject.SetActive(false);
                                placedTile.highlightedTile = null;

                                if (!menu.instance.waitingForReady && !gameManager.instance.inArena && !gameManager.inSecondPhase
                                    && player.instance.inDungeon == -1)
                                {
                                    if (InputManager.actions["Item Primary"].started && !menu.instance.mapMaximized)
                                    {
                                        menu.instance.OpenMap(true);
                                    }
                                    if ((InputManager.actions["Exit"].started || Input.GetKeyDown(KeyCode.M) || InputManager.actions["Inventory"].started) && menu.instance.mapMaximized)
                                    {
                                        menu.instance.OpenMap(false);
                                    }

                                }
                                break;
                            case item.itemType.grapple:
                                if ((InputManager.actions["Item Primary"].started || grappleBuffered) && !inGrappleAnim && !player.instance.currentBoat && !grappleCooldown 
                                    && inventorySlot.equippedSlot.durability > 0)
                                {
                                    RPC_Grapple();
                                    inGrappleAnim = true;
                                    grappleBuffered = false;
                                }
                                if (!HandEvents.grappling && grappleCooldown && InputManager.actions["Item Primary"].started && !player.instance.currentBoat)
                                {
                                    grappleBuffered = true;
                                }
                                break;

                            default:
                                if(tilePreviewSprite)
                                    tilePreviewSprite.enabled = false;
                                boatPreview.gameObject.SetActive(false);
                                placedTile.highlightedTile = null;
                                break;

                        }

                        if (selectedItem.type != item.itemType.grapple && inventorySlot.equippedSlot.durability > 0)
                        {
                            grappleBuffered = false;
                        }

                    }
                    else
                    {
                        animator.SetBool("Disable Hit", false);
                        placedTile.highlightedTile = null;
                        tilePreviewSprite.enabled = false;
                        boatPreview.gameObject.SetActive(false);

                        grappleBuffered = false;
                    }


                }
                else
                {
                    tilePreviewSprite.enabled = false;
                    boatPreview.gameObject.SetActive(false);
                    placedTile.highlightedTile = null;

                    if (recalling)
                    {
                        recalling = false;

                        ParticleSystem p = recallPs;
                        if (valeRecalling) p = recallPs;

                        var ps = p.main;
                        ps.simulationSpeed = 2f;
                        p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        recallTimer = -1.5f;
                        animator.SetBool("Recalling", false);

                        StartCoroutine(_StopRecallSound());
                    }
                }

            }
            else
            {
                eating = false;
                inEatCooldown = true;
                eatTimer = 0;
                animator.SetBool("Eating", false);
                tilePreviewSprite.enabled = false;

                if (recalling)
                {
                    recalling = false;

                    ParticleSystem p = recallPs;
                    if (valeRecalling) p = valeRecallPs;

                    var ps = p.main;
                    ps.simulationSpeed = 2f;
                    p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    recallTimer = -1.5f;
                    animator.SetBool("Recalling", false);

                    StartCoroutine(_StopRecallSound());
                }
            }

            animator.SetBool("Map", menu.instance.mapMaximized);
            if (menu.instance.mapMaximized && (!selectedItem || (selectedItem && selectedItem.type != item.itemType.map))
                || player.instance.inDungeon > -1 || gameManager.inSecondPhase) menu.instance.OpenMap(false);

            if (swingCooldownTime > 0)
            {
                swingCooldownTime -= Time.deltaTime;
            }

            if (!recalling)
            {
                if (recallTimer < 0) recallTimer += Time.deltaTime;
                else recallTimer = 0;
            }

            trackerActivated = selectedItem != null && selectedItem.type == item.itemType.tracker && player.instance.onIsland && player.instance.currentIsland.containsUniqueTool;

        }
        //no input authority
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, net_angle), Time.deltaTime * 14);
        }

        if (tilePreviewSprite.enabled == false)
        {
            if (player.instance.currentBoat)
            {
                Transform boat = player.instance.currentBoat.transform;
                tilePreviewBoatOffset = (Vector2)mousePosition - (Vector2)boat.position;
            }
            else
            {
                tilePreview.position = mousePosition;
            }
        }

        //boat flipping scale
        if (playerParent.transform.parent)
        {
            transform.localScale = new Vector2(playerParent.transform.parent.localScale.x, transform.localScale.y);

            if (view.HasStateAuthority && playerParent.transform.parent.localScale.x < 0) playerParent.spriteHolder.localScale *= new Vector2(-1, 1);
        }
        else
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
        }

        if (!selectedItem || selectedItem.type != item.itemType.grapple)
        {
            inGrappleAnim = false;
        }

        if (!selectedItem || selectedItem.uniqueType != item.UniqueType.shield)
        {
            animator.ResetTrigger("Bash");
        }

        if (selectedItem != null && !controllingBoat)
        {
            playerParent.heldItemSprite.enabled = false;

            Sprite correctSprite = selectedItem.overworldSprite;
            if (selectedItem.flippedSprite && yScale == -1)
            {
                correctSprite = selectedItem.flippedSprite;
            }

            if (playerParent.heldItemSprite.sprite != correctSprite)
            {
                RefreshHeldItem();
            }
            playerParent.heldItemSprite.enabled = true;

            //tracker
            if (trackerActivated)
            {
                playerParent.heldItemSprite.sprite = null;
                playerParent.heldItemSprite.material = null;

                if(trackerPulsing)
                    playerParent.heldItemSprite.sprite = selectedItem.naturalSpawnSprite;
                else
                    playerParent.heldItemSprite.sprite = selectedItem.overworldSprite;

                if (view.HasInputAuthority && !trackerWasActive)
                {
                    gameManager.instance.musicSource.DOFade(0, 0.8f);
                    trackerWasActive = true;
                }

                if (selectedItem.matOverride != null)
                {
                    playerParent.heldItemSprite.material = selectedItem.matOverride;
                }
                else
                {
                    playerParent.heldItemSprite.material = defaultMat;
                }

                trackerPulseTimer += Time.deltaTime;
                float pulseTarget = (Vector2.Distance(transform.position, playerParent.currentIsland.uniqueToolPos)) / 50;

                if (pulseTarget < 0.1f)
                {
                    pulseTarget = 0;
                    if (!trackerPulsing)
                    {
                        trackerPulsing = true;
                        trackerSound.Play();
                    }
                }
                else
                {
                    if (trackerPulseTimer > pulseTarget)
                    {
                        trackerSound.volume = 1.2f - Mathf.Clamp((Vector2.Distance(transform.position, playerParent.currentIsland.uniqueToolPos) / 120), 0.2f, 0.9f);
                        trackerSound.pitch = 0.7f + ((trackerSound.volume - 0.2f) * 0.6f);
                        trackerSound.panStereo = Mathf.Clamp((playerParent.currentIsland.uniqueToolPos.x - transform.position.x)/300,-0.2f,0.2f);

                        //disable 3d for this client to allow panning
                        if (view.HasStateAuthority) trackerSound.spatialBlend = 0;

                        //dont play the sound on other clients who are also holding a tracker
                        if(view.HasStateAuthority || !instance.trackerActivated)
                        trackerSound.Play();
                        
                        trackerPulseTimer = pulseTarget * -0.3f - 0.1f;
                        trackerPulseTimer = Mathf.Clamp(trackerPulseTimer, -1f, -0.05f);
                    }
                    trackerPulsing = trackerPulseTimer < 0;
                }
            }

            //grapple
            if (selectedItem.type == item.itemType.grapple && HandEvents.grappleLine.enabled)
            {
                playerParent.heldItemSprite.sprite = null;
                playerParent.heldItemSprite.material = null;

                playerParent.heldItemSprite.sprite = selectedItem.naturalSpawnSprite;

                if (selectedItem.matOverride != null && inventorySlot.equippedSlot.durability > 0)
                {
                    playerParent.heldItemSprite.material = selectedItem.matOverride;
                }
                else
                {
                    playerParent.heldItemSprite.material = defaultMat;
                }
            }

            if (soulBreakerAwakened && selectedItem.uniqueType == item.UniqueType.breaker)
            {
                playerParent.heldItemSprite.sprite = null;
                playerParent.heldItemSprite.material = null;

                playerParent.heldItemSprite.sprite = selectedItem.naturalSpawnSprite;

                if (selectedItem.matOverride != null)
                {
                    playerParent.heldItemSprite.material = selectedItem.matOverride;
                }
                else
                {
                    playerParent.heldItemSprite.material = defaultMat;
                }
            }

            playerParent.heldItemSprite.transform.localPosition = Vector3.zero + selectedItem.holdOffset;
            playerParent.heldItemSprite.transform.localEulerAngles = new Vector3(0, 0, selectedItem.holdRotation);

            playerParent.heldItemSprite.transform.localScale = new Vector2(1f, 1f);

        }
        else
        {
            playerParent.heldItemSprite.enabled = false;
        }

        if (trackerWasActive && !trackerActivated && gameManager.instance.musicSource.volume == 0 && !(player.instance.currentBoat && player.instance.currentBoat.velocity.magnitude > 0))
        {
            trackerWasActive = false;
            gameManager.instance.musicSource.DOFade(1, 0.8f);
        }
        if (animator.GetBool("Shielding"))
        {
            if (!shieldPs.isPlaying)
            {
                shieldPs.Play();
            }
        }
        else
        {
            if (shieldPs.isPlaying)
            {
                shieldPs.Stop();
            }
        }

        if (!view.HasInputAuthority)
        {
            eating = animator.GetBool("Eating");

            if (eating)
            {
                sprite.sortingOrder = handEatingSortOrder;
            }
            else
            {
                sprite.sortingOrder = normalHandSortOrder;
            }
            playerParent.heldItemSprite.sortingOrder = sprite.sortingOrder - 1;

            animator.SetBool("Recalling", recalling);

            if (recalling)
            {
                networkedRecalling = true;

                if (!recallPs.isPlaying && !valeRecallPs.isPlaying)
                {
                    ParticleSystem p = recallPs;
                    if (valeRecalling) p = valeRecallPs;

                    var ps = p.main;
                    ps.simulationSpeed = 1f;
                    p.Play();
                }
                if (recallSound.volume == 0)
                {
                    recallSound.pitch = 1;
                    if (valeRecalling) recallSound.pitch = 0.85f;

                    recallSound.Play();
                    recallSound.DOFade(recallSoundVol, 1);
                }
            }
            else
            {
                if (networkedRecalling == true)
                    StartCoroutine(_StopRecallSound());

                networkedRecalling = false;
                ParticleSystem p = recallPs;
                var ps = p.main;
                ps.simulationSpeed = 2f;
                p.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                p = valeRecallPs;
                var ps2 = p.main;
                ps2.simulationSpeed = 2f;
                p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

        }
        /*
        if (sprite.isVisible && playerParent.sprite.isVisible)
            view.Synchronization = ViewSynchronization.UnreliableOnChange;
        else
            view.Synchronization = ViewSynchronization.Off;
        */
    }


    void ArmorSwitch(inventorySlot slot)
    {
        if (inArmorSwitchCooldown || heldItemBroken) return;

        netAnim.SetTrigger("Equip");

        MouseCursor.instance.anim.SetTrigger("Place");

        inArmorSwitchCooldown = true;
        Invoke("FinishArmorCooldown", armorSwitchCooldown);

        if (slot == inventory.instance.charmSlot)
            inventory.instance.armorPutOnSound.clip = inventory.instance.charmSlot.placeSound.clip;
        else
            inventory.instance.armorPutOnSound.clip = inventory.instance.chestplateSlot.placeSound.clip;
        inventory.instance.armorPutOnSound.Play();

        if (slot.itemInSlot == null)
        {
            inventory.instance.addItemInSlot(selectedItem, 1, inventorySlot.equippedSlot.durability, inventorySlot.equippedSlot.itemData, slot, slot.transform.position);
            inventory.instance.removeItem(inventorySlot.equippedSlot);
        }
        else
        {
            item itemInSlot = slot.itemInSlot;
            int durabilityTemp = slot.durability;
            ItemData itemDataTemp = slot.itemData;
            inventory.instance.removeItem(slot);
            inventory.instance.addItemInSlot(selectedItem, 1, inventorySlot.equippedSlot.durability, inventorySlot.equippedSlot.itemData, slot, slot.transform.position);

            inventory.instance.removeItem(inventorySlot.equippedSlot);
            inventory.instance.addItemInSlot(itemInSlot, 1, durabilityTemp, itemDataTemp, inventorySlot.equippedSlot, inventorySlot.equippedSlot.transform.position);

            inventoryHightlighter.instance.ShowItemNameText();
        }
    }

    [Rpc(RpcSources.All,RpcTargets.All)]
    void RPC_ProfiteerSwitchSound()
    {
        profiteerSwitchSound.volume = 0.4f;
        profiteerSwitchSound.pitch = 1.1f;
        profiteerSwitchSound.Play();
    }

    public IEnumerator _TetherTeleport(bool vale, Vector2 toPos, bool dungeonWarp = false)
    {
        teleporting = true;
        menu.instance.deathFade.DOFade(1, 0.5f);
        yield return new WaitForSeconds(0.5f);

        playerParent.CheckIsland();
        int island = playerParent.currentIsland.index;
        if (!playerParent.onIsland) island = -1;

        if (dungeonWarp)
        {
            toPos = DungeonGenerator.instance.dungeons[player.instance.inDungeon].position + new Vector2(0, 1.5f);
        }
        else
        {
            playerParent.inDungeon = -1;
        }

        if (!vale)
        {
            playerParent.transform.position = toPos;

            if (!dungeonWarp && TileEntity.currentSpawnPoint && TileEntity.currentSpawnPoint.boatParent)
            {
                player.instance.BoatRespawn();
            }
        }
        //teleport to vale
        else
        {
            List<inventorySlot> allSlots = inventory.instance.slots.ToList();
            allSlots.Add(inventory.instance.headSlot);
            allSlots.Add(inventory.instance.chestplateSlot);
            allSlots.Add(inventory.instance.legsSlot);
            allSlots.Add(inventory.instance.charmSlot);

            string[] itemIds = new string[allSlots.Count];
            int[] itemCounts = new int[allSlots.Count];
            int[] itemDurabilities = new int[allSlots.Count];
            ItemData[] itemDatas = new ItemData[allSlots.Count];

            for (int i = 0; i < allSlots.Count; i++)
            {
                if (allSlots[i].itemInSlot != null)
                    itemIds[i] = allSlots[i].itemInSlot.itemId;
                else
                    itemIds[i] = "null";
                itemCounts[i] = allSlots[i].itemCountInSlot;
                itemDurabilities[i] = allSlots[i].durability;
                itemDatas[i] = allSlots[i].itemData;
            }

            //valeTeleportStartPos = playerParent.transform.position;

            ValeManager.instance.SetSoulItems(itemIds, itemCounts, itemDurabilities, itemDatas);
            playerParent.transform.position = playerHealth.instance.RandomValePos(true);
            ValeManager.instance.RemoveNearbyEnemies();

            playerHealth.instance.currentHealth = playerHealth.instance.maxHealth;
        }


        yield return null;
        playerParent.CheckIsland();

        //boat recall - maybe remove ts someday
        /*
        if (playerParent.currentBoat && !vale)
        {
            if (island != playerParent.currentIsland.index)
            {
                playerParent.currentBoat.RandomIslandPosition(playerParent.currentIsland.size, playerParent.currentIsland.center);
                playerParent.transform.position = playerParent.health.respawnPos;
            }
        }
        */

        playerParent.cam.ForceCameraPosition(playerParent.transform.position, playerParent.cam.transform.rotation);
        yield return new WaitForSeconds(0.1f);

        float failsafeTimer = 0;

        while (player.instance.loadingFeatures && failsafeTimer < 6 && !dungeonWarp)
        {
            failsafeTimer += Time.deltaTime;
            yield return null;
        }
        if (failsafeTimer >= 6) Debug.LogError("feature load failsafe");

        yield return new WaitForSeconds(1);

        menu.instance.deathFade.DOFade(0, 0.5f);

        recallTimer = -2.5f;

        RPC_RecallSuccessSound();

        if (!dungeonWarp)
            ChangeDurability(-1, inventorySlot.equippedSlot);

        recalling = false;

        yield return new WaitForSeconds(0.5f);

        teleporting = false;
    }

    Gradient UpdateCatalystGradient(List<int> activeIndices)
    {
        Gradient gradient = new Gradient();
        int count = activeIndices.Count;
        if (count == 0) return gradient;

        GradientColorKey[] colorKeys = new GradientColorKey[count];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[1];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / Mathf.Max(1, count - 1); // evenly spaced 0→1
            int index = activeIndices[i];

            Color color = catalystColors[index];

            colorKeys[i] = new GradientColorKey(color, t);
        }

        gradient.SetKeys(colorKeys, alphaKeys);

        var main = catalystPs.main;
        var minMax = new ParticleSystem.MinMaxGradient(gradient)
        {
            mode = ParticleSystemGradientMode.RandomColor
        };
        main.startColor = minMax;

        return gradient;
    }

    public void FinishCatalyst()
    {
        for (int i = 0; i < catalystEffectTime.Length; i++)
        {
            catalystEffectTime[i] = 0;
        }
    }

    public void HitDuringJackpot()
    {
        jackpotRegenHealth = playerParent.health.currentHealth;
    }


    public void VortexCleave()
    {
        RPC_VortexCleave();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_VortexCleave()
    {
        StartCoroutine(_VortexCleave());
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_Grapple()
    {
        chargeSound.Play();
        animator.SetTrigger("Grapple");
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_ShieldBash()
    {
        HandEvents.deactivateHitbox();
        if(view.HasStateAuthority)
        netAnim.SetTrigger("Bash",true);
    }

    void EnableShield()
    {
        disableShield = false;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetItem(string id, bool broken)
    {
        selectedItem = gameManager.instance.itemDictionary[id];
        heldItemBroken = broken;
    }

    public IEnumerator _VortexCleave()
    {
        inCleave = true;
        bool flip = playerParent.spriteHolder.transform.localScale.x == -1;
        animator.SetTrigger("Cleave");
        yield return new WaitForSeconds(0.25f);
        cleaveTrail.emitting = true;
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            if (flip)
                transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z + (Time.deltaTime * 1700));
            else
                transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z - (Time.deltaTime * 1700));
            angle = transform.eulerAngles.z;
            yield return null;
        }
        inCleave = false;
        cleaveTrail.emitting = false;
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_RecallSuccessSound()
    {
        playerParent.health.respawnSound.Play();

        recallPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        valeRecallPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        StartCoroutine(_StopRecallSound());
    }


    IEnumerator _StopRecallSound()
    {

        recallSound.DOFade(0, 0.5f);
        yield return new WaitForSeconds(0.5f);
        recallSound.Stop();
    }

    public void FinishSwing()
    {
        if (selectedItem &&
            selectedItem.uniqueType == item.UniqueType.lifesteal &&
            inventorySlot.equippedSlot.itemData.tier >= 4)
        {
            if (enemiesHitThisSwing.Count > 1)
            {
                List<NetworkId> boundIds = new();

                foreach (enemyHealth e in enemiesHitThisSwing)
                {
                    if (e != null && !e.dead)
                        boundIds.Add(e.view.Id);
                }

                string boundString = JsonUtility.ToJson(
                    new NetworkIdList { ids = boundIds });

                RPC_SetBound(boundString);
            }
        }

        lifeStealThisHit = false;
        enemiesHitThisSwing.Clear();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetBound(string boundString)
    {
        bool changed = false;

        NetworkIdList data =
            JsonUtility.FromJson<NetworkIdList>(boundString);

        if (data?.ids == null)
            return;

        List<enemyHealth> enemiesBound = new();

        foreach (NetworkId id in data.ids)
        {
            if (Runner.TryFindObject(id, out NetworkObject n))
            {
                enemyHealth eh = n.GetComponent<enemyHealth>();

                if (eh != null)
                    enemiesBound.Add(eh);
            }
        }

        if (enemiesBound.Count == 0)
            return;

        foreach (enemyHealth e in enemiesBound)
        {
            // Detect if bound status changes
            if (!e.bound)
                changed = true;

            e.boundTo = enemiesBound;
            e.boundDirectlyTo = null;
        }

        for (int i = 0; i < enemiesBound.Count - 1; i++)
        {
            if (enemiesBound[i].bindingChainLine == null)
                continue;

            enemiesBound[i].SetBoundTo(enemiesBound[i + 1]);
        }

        if (changed)
        {
            bindingSound.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            bindingSound.Play();
        }
    }

    public void ReceivePvpHit(int playerId, bool causeDeath, Vector2 position, bool overrideLifestealCooldown, bool jackpot)
    {
        RPC_PvpHit(playerId, causeDeath, position, overrideLifestealCooldown, jackpot);
    }

    //invoke on others
    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false, TickAligned = false)]
    void RPC_PvpHit(int playerId, bool causeDeath, Vector2 position, bool overrideLifestealCooldown, bool jackpot)
    {
        if (playerId == Runner.LocalPlayer.PlayerId)
            PvpHit(causeDeath, true, position, overrideLifestealCooldown, jackpot,0);
    }


    //called on the client that does the hit to take durability and apply lifesteal
    public void PvpHit(bool death, bool onPlayer, Vector2 position, bool overrideLifestealCooldown, bool jackpot, int toolType)
    {
        if (death)
        {
            if (!onPlayer)
            {
                gameManager.endScreenStats["enemies"]++;
            }
            else
            {
                gameManager.endScreenStats["kills"]++;
            }

        }

        if (inventorySlot.equippedSlot.itemInSlot)
        {
            if (death && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.lifesteal
                && (!lifeStealThisHit || overrideLifestealCooldown))
            {
                if (playerHealth.instance.currentHealth < playerHealth.instance.maxHealth)
                {
                    if (overrideLifestealCooldown)
                    {
                        playerHealth.instance.currentHealth += UnityEngine.Random.Range(1, 3);
                    }
                    else
                    {
                        playerHealth.instance.currentHealth += UnityEngine.Random.Range(3, 5);
                    }

                    if (view.HasStateAuthority)
                        RPC_LifeStealSound(position);

                    lifeStealThisHit = true;
                }
            }

            if (inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.gavel)
            {
                if (!onPlayer) return;

                ChangeDurability(1, inventorySlot.equippedSlot);

                return;
            }

            if (inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.breaker)
            {
                if (!soulBreakerAwakened)
                    RPC_SetSoulBreakerAwakened(true, onPlayer);
                else
                    RPC_SetSoulBreakerAwakened(false, onPlayer);
            }

            if (inventorySlot.equippedSlot.itemInSlot.usesDurability)
            {
                ChangeDurability(-1, inventorySlot.equippedSlot);
            }

            if (net_charmEffect == "mush")
            {
                if (menu.instance.mushLevel < 100)
                {
                    menu.instance.mushLevel += 10;
                    menu.instance.ChangeMushLevel();
                }
            }

            if (death & net_charmEffect == "wrath" && menu.instance.mushLevel < 100)
            {
                menu.instance.mushLevel += 10;
                menu.instance.ChangeMushLevel();
            }
            if (net_charmEffect == "greed" && !jackpotActive)
            {
                bool greedJackpot = UnityEngine.Random.Range(0, 32) == 0;
                if (selectedItem.uniqueType == item.UniqueType.jackpot) greedJackpot = UnityEngine.Random.Range(0, 28) == 0;

                if (onPlayer) greedJackpot = UnityEngine.Random.Range(0, 20) == 0;

                if (greedJackpot)
                {
                    RPC_Jackpot(jackpotAdditionalTime);
                    jackpotAdditionalTime = 0;
                }
                else
                {
                    jackpotAdditionalTime += 0.1f;
                    if (jackpotAdditionalTime > 4)
                    {
                        jackpotAdditionalTime = 4;
                    }
                }

            }

            if (wagerLevel > 0 && !onPlayer)
            {
                int hpChange = wagerLevel * hpPerWager;
                wagerLevel = 0;
                wagerShow = false;
                jackpotAdditionalChance = 0;

                if (!jackpot)
                {
                    playerHealth.instance.currentHealth -= hpChange;
                    if (playerHealth.instance.currentHealth <= 1) playerHealth.instance.currentHealth = 1;

                    wagerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(1, 0, 0))}>" + "-" + hpChange + " HP" + "</color>";
                }
                else
                {
                    hpChange = 5;
                    playerHealth.instance.currentHealth += hpChange;
                    wagerText.text = "+" + hpChange + " HP";
                }

                if (wagerTween != null && wagerTween.active)
                {
                    wagerTween.Kill();
                }
                wagerTween = wagerText.DOFade(0, 1.2f);

            }

            if(inventorySlot.equippedSlot.itemInSlot 
                && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.profiteer && inventorySlot.equippedSlot.itemData.tier >= 4)
            {
                switch (toolType)
                {
                    case 0:
                    profiteerState = 2; break;
                    case 1:
                        profiteerState = 0; break;
                    case 2:
                        profiteerState = 1; break;
                }
            }

            //weapon drop chance - keep on bottom
            if (DungeonGenerator.instance.GetModifier("weapon drop") == 2 && inventorySlot.equippedSlot.itemInSlot)
            {
                if (UnityEngine.Random.Range(0, 25) == 0)
                {
                    inventory.instance.DropItem(1);
                }
            }
        }

        if (death)
        {
            if (playerParent.health.inEnvyState)
            {
                playerParent.health.envyEnemiesKilled++;
            }

            if (playerParent.prideActive && UnityEngine.Random.Range(0, 7) == 0)
            {
                RPC_CatalystEffect(catalysts[UnityEngine.Random.Range(2, 4)].itemId, 0);
            }
        }

    }

    [Rpc(RpcSources.All,RpcTargets.All)]
    public void RPC_RefreshHeldItem()
    {
        RefreshHeldItem();
    }

    public void RefreshHeldItem()
    {
        if (!selectedItem)
        {
            playerParent.heldItemSprite.enabled = false;
            return;
        }
        Sprite correctSprite = selectedItem.overworldSprite;
        if (selectedItem.flippedSprite && yScale == -1)
        {
            correctSprite = selectedItem.flippedSprite;
        }

        playerParent.heldItemSprite.sprite = null;
        playerParent.heldItemSprite.material = null;

        playerParent.heldItemSprite.sprite = correctSprite;

        if (selectedItem.matOverride != null && !heldItemBroken)
        {
            playerParent.heldItemSprite.material = selectedItem.matOverride;
        }
        else
        {
            playerParent.heldItemSprite.material = defaultMat;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_Jackpot(float additionalTime)
    {
        if (jackpotCo != null)
            return;

        jackpotCo = StartCoroutine(playerParent._Jackpot(additionalTime, this));
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ShieldReflectSound()
    {
        if (reflectSound.isPlaying) return;

        reflectSound.volume = 0.5f;
        reflectSound.pitch = UnityEngine.Random.Range(1.1f, 1.22f);
        reflectSound.Play();
    }


    void CancelJackpot()
    {
        if (jackpotCo != null) StopCoroutine(jackpotCo);
        jackpotActiveSound.DOFade(0, 0.5f);
        jackpotPs.Stop();
        jackpotActive = false;
        jackpotCo = null;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetSoulBreakerAwakened(bool awakened, bool hitPlayer)
    {
        soulBreakerAwakened = awakened;
        if (awakened)
        {
            soulBreakerAwakenSound.Play();
            StartCoroutine(_SoulBreakerAwakenCancel(hitPlayer));
        }

    }

    IEnumerator _SoulBreakerAwakenCancel(bool hitPlayer)
    {
        float awakenedTime = 1.4f;
        if (hitPlayer) awakenedTime = 2.4f;

        for (float t = 0; t < awakenedTime; t += Time.deltaTime)
        {
            if (!soulBreakerAwakened) yield break;
            yield return null;
        }

        catalystDoneSound.Play();
        soulBreakerAwakened = false;
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_LifeStealSound(Vector2 position)
    {
        playerParent.lifestealSound.Play();

        StartCoroutine(_LifeStealPs(position));
    }

    IEnumerator _LifeStealPs(Vector2 position)
    {
        GameObject lifeStealPs = Instantiate(deathScythePs, playerParent.transform.position, Quaternion.identity);
        lifeStealPs.transform.SetParent(playerParent.transform);

        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            Vector2 direction = new Vector3(position.x - playerParent.transform.position.x, position.y - playerParent.transform.position.y).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            lifeStealPs.transform.eulerAngles = new Vector3(0, 0, angle);
            yield return null;
        }

        Destroy(lifeStealPs);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_CatalystEffect(string item, float allAditionalTime)
    {
        CatalystEffect(item, allAditionalTime);
    }
    void CatalystEffect(string item, float allAditionalTime)
    {
        catalystSound.clip = gameManager.instance.itemDictionary[item].catalystSound;
        catalystSound.Play();
        catalystPs.Play();

        if (gameManager.instance.itemDictionary[item].catalystEffect == global::item.CatalystEffect.all)
        {
            for (int i = 0; i < catalystEffectTime.Length; i++)
            {
                catalystEffectTime[i] =
                gameManager.instance.itemDictionary[item].catalystEffectTime;
            }

        }
        else
        {
            catalystEffectTime[(int)gameManager.instance.itemDictionary[item].catalystEffect - 1] =
                gameManager.instance.itemDictionary[item].catalystEffectTime;
        }

        for (int i = 0; i < catalystEffectTime.Length; i++)
        {
            if (catalystEffectTime[i] > 0)
                catalystEffectTime[i] += allAditionalTime;
        }

    }

    void FinishArmorCooldown()
    {
        inArmorSwitchCooldown = false;
    }

    public void ChangeDurability(int addAmount, inventorySlot slot)
    {
        if (slot.itemInSlot == null || player.instance.currentIsland.biome == (int)MapGenerator.biome.vale) return;

        // 2/3 durability loss in dungeons
        if (player.instance.inDungeon > -1
            && slot.itemInSlot.uniqueType == item.UniqueType.none
            && slot.itemInSlot.type != item.itemType.block && !slot.itemInSlot.ignoreDurabilityModifiers
            && slot.durability < slot.itemInSlot.maxDurability)
        {
            if (UnityEngine.Random.Range(0, 3) == 0) return;
        }

        if (UnityEngine.Random.Range(0f, 1f) < DungeonGenerator.instance.GetModifier("durability") - 1)
        {
            return;
        }

        if (UnityEngine.Random.Range(0.01f, 1f) < inventory.instance.GetModifierInToolSlot("dur") - 1)
        {
            return;
        }
        if (UnityEngine.Random.Range(-1f, -0.01f) > inventory.instance.GetModifierInToolSlot("dur") - 1)
        {
            slot.durability += addAmount;
        }

        slot.durability += addAmount;
        if (slot.durability <= 0 && slot.itemInSlot.uniqueType != item.UniqueType.gavel)
        {
            if (slot.itemInSlot.durabilityEmptyItem == null)
            {
                RPC_ToolBreak(slot.itemInSlot.uniqueType != item.UniqueType.none);
                
                if(slot.itemInSlot.uniqueType == item.UniqueType.none)
                inventory.instance.removeItem(slot);
            }
            else
            {
                if (slot.itemInSlot.uniqueType == item.UniqueType.execution)
                {
                    DurabilityChangeItem(slot);
                    return;
                }

                item i = slot.itemInSlot.durabilityEmptyItem;
                ItemData itemDataTemp = slot.itemData;
                inventory.instance.removeItem(slot);
                inventory.instance.addItemInSlot(i, 1, i.maxDurability, itemDataTemp, slot, slot.transform.position);
            }
        }

        //gavel
        if (slot.itemInSlot != null && slot.durability >= slot.itemInSlot.maxDurability && slot.itemInSlot.uniqueType == item.UniqueType.gavel)
        {
            RPC_ExecutionPs();
            UnlockExecution();
        }
    }

    //for giving gavel again
    async void DurabilityChangeItem(inventorySlot slot)
    {
        unlockingExecution = true;
        swingStored = false;

        await Task.Delay(500);

        item i = slot.itemInSlot.durabilityEmptyItem;
        ItemData itemDataTemp = slot.itemData;
        inventory.instance.removeItem(slot);
        inventory.instance.addItemInSlot(i, 1, 0, itemDataTemp, slot, slot.transform.position);

        await Task.Delay(200);

        unlockingExecution = false;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_ExecutionPs()
    {
        executionObtainPs.Play();
        executionObtainSound.Play();
    }

    async void UnlockExecution()
    {
        inventorySlot slot = inventorySlot.equippedSlot;

        unlockingExecution = true;
        swingCooldownTime = 1.5f;

        await Task.Delay(400);

        inventory.instance.removeItem(slot);
        inventory.instance.addItemInSlot(gameManager.instance.itemDictionary["executioners_sword"], 1, 1, new ItemData(4), slot, slot.transform.position);

        StartCoroutine(Chat.Instance._KitPopup());

        await Task.Delay(200);

        unlockingExecution = false;

    }


    public void ToolBreakSound(bool unique)
    {
        RPC_ToolBreak(unique);
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_ToolBreak(bool unique)
    {
        toolBreakSound.clip = toolBreakClip;
        if (unique)
        {
            heldItemBroken = true;
            RefreshHeldItem();
            toolBreakSound.clip = uniqueBreakClip;
        }
        toolBreakSound.Play();
    }

    public void DropAnimation()
    {
        RPC_DropAnimation();
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_DropAnimation()
    {
        animator.SetTrigger("Place");
        dropSound.Play();
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_PlaceAnimation()
    {
        animator.SetTrigger("Place");
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_EatSound(string item)
    {
        if (gameManager.instance.itemDictionary[item].overrideEatCompleteSound != null)
        {
            eatFinishSound.clip = gameManager.instance.itemDictionary[item].overrideEatCompleteSound;
        }
        else
        {
            eatFinishSound.clip = defaultEatFinishSound;
        }

        eatFinishSound.Play();
        HandEvents.eatSound.pitch = handEvents.eatSoundPitch;
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_RemoveTileAnim()
    {
        animator.SetTrigger("Remove");
    }

    /*
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SwingSword()
    {
        print(view.HasInputAuthority);

        if (view.HasInputAuthority)
        {
            animator.SetFloat("Speed", 1);
        }
        //faster animation on other players to compensate for delay - pvp hit detection is calculated by the person getting hit
        else
        {
            animator.SetFloat("Speed", 1.5f);
        }

        print(animator.GetFloat("Speed"));

        animator.SetTrigger("Swing");
    }
    */


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_BucketPickupSound()
    {
        bucketPickupSound.Play();
    }

    private void LateUpdate()
    {
        var delta = playerParent.transform.position - lastFramePosition;
        lastFramePosition = playerParent.transform.position;

        if (cleaveTrail.emitting)
        {
            var positions = new Vector3[cleaveTrail.positionCount];
            cleaveTrail.GetPositions(positions);

            for (var i = 0; i < cleaveTrail.positionCount; i++)
            {
                positions[i] += delta;
            }
            cleaveTrail.SetPositions(positions);

        }

        if (swingTrail.emitting)
        {
            var positions = new Vector3[swingTrail.positionCount];
            swingTrail.GetPositions(positions);

            for (var i = 0; i < swingTrail.positionCount; i++)
            {
                positions[i] += delta;
            }

            swingTrail.SetPositions(positions);

        }

    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(boatPreview.transform.position, boatCheckSize);

    }

}
[System.Serializable]
public class NetworkIdList
{
    public List<NetworkId> ids;
}
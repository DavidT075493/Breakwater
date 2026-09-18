
using DG.Tweening;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class Boat : NetworkBehaviour
{
    const float playerYOffset = -0.19f;

    public string boatID;

    public NetworkObject view;
    public int boatTypeIndex;

    public Transform grabbedPoint;
    public EnemyOceanHand grabbedByHand;

    [SerializeField] Collider2D waterCollider;
    public LayerMask mainPlayerLayer, boatLayer;
    public GameObject indicatorPrefab;
    GameObject indicatorInstance;
    Animator indicatorAnim;
    public Vector2 pilotIndicatorOffset;
    public bool playerInRange;
    TextMeshPro indicatorText;

    public float camLookahead = 2.5f;
    public Transform enterCheckPos, exitCheckPos, pilotCheckPos, pilotPosition, unpilotPosition, camFollow;

    public float exitCheckYOffset;
    public float enterTextOffset, exitTextOffset;

    public player currentPilot;
    public float pilotCamSize;
    float normalCamSize;

    public Vector2 velocity;
    public float velocityChangeSpeed = 2;
    public float moveSpeed = 5;
    Rigidbody2D rb;
    public FootstepSurface surface;

    public List<player> playersOnBoat = new List<player>();

    Vector2 oldPos;
    public Vector2 dif;
    public bool beingPiloted;

    public bool beingConstructed = true;
    public GameObject waterCollision;
    public GameObject sailTethers;


    public SpriteRenderer[] sprites;
    public bool playingMusic, playingAmbience;
    public AudioSource boatMusic, boatAmbience, constructSound, moveAmbience;
    float timeInSong;
    public float constructTime = 1.2f;
    bool altActionCooldown;

    [SerializeField] ParticleSystem wakeParticles;
    ParticleSystem.EmissionModule wakeEmissionMod;
    float maxWakeEmission;


    bool musicSwitchCooldown;

    [SerializeField] bool usesSails;
    bool sailsUp;
    [SerializeField] Transform[] sailsCheckPos = new Transform[0];
    [SerializeField] SpriteRenderer sailsSprite;
    [SerializeField] Sprite sailsOpenSprite, sailsClosedSprite;
    [SerializeField] AudioSource sailRaiseSound, climbSound, altActionSound, enterSound;

    float sailStartup = 0;


    [SerializeField] AudioSource collisionSound;
    const float networkTeleportDist = 8;
    const float networkLerpSpeed = 5f;

    bool pilotCooldown = false;


    public LayerMask oceanRockLayer;
    public SpriteMask boatBaseMask;
    public GameObject missileReticle;
    public Animation reticleAnim;
    public SpriteRenderer reticleSprite;

    public GameObject missilePrefab;
    public Transform missileSpawnPoint;

    List<Vector3> alreadyDestroyedRockLocations = new List<Vector3>();

    bool playedCollideSound;

    public enum boatType
    {
        sailboat,
        pirateShip,
        speedboat,
        battleship,
        ghost

    }
    public boatType BoatType;

    Vector2 indicatorLocalPos;

    public Transform itemHolder;

    public Transform[] tileSlots;
    public BoatTileSlot[] tileSlotTiles;
    public item[] startingTiles;
    public static int highlightedTileSlotIndex = -1;
    public bool inRangeOfInteractable;

    [Header("Alt Action")]
    public float altTextOffset;
    public Transform[] altPlayerPos;
    public Transform[] altPlayerPosExit, altCheckPos;
    public player[] currentPlayerAltAction;
    public float altActionCamSize;
    public string[] altActionIndicatorText;
    public int[] altActionLayer;
    bool usingSkull;
    public float abilityCooldown;
    float abilityCooldownTime;
    [SerializeField] ParticleSystem abilityPs;
    [SerializeField] AudioSource abilitySound;
    [SerializeField] Collider2D oceanRockCollider;
    public float dashForce = 0.4f;
    Dictionary<item, int> itemCounts;
    [SerializeField] item soulItem;

    [Header("Pirate Ship")]
    public Light2D[] fadeInLights;
    public SpriteRenderer[] insideSprites;
    public Transform[] floorEnterCheck, floorExitCheck, floorInsideCheck;
    public float[] floorEnterTextOffset, floorExitTextOffset, insideFadeOpacity;
    bool[] inside;
    public GameObject insideColliders;
    public int[] playerFloorLayers = { 12, 13 };
    public int tileLayer = 10;

    [Header("Speedboat")]
    public ParticleSystem smoke;
    public item fuelItem;

    public float maxFuelTime;
    public static Boat currentSpeedboatFueling;
    bool playerRefueling;
    public bool skullAbilityActive;
    private Vector2 dashVelocity;
    private bool enterCooldown;

    [Header("Network")]

    [Networked] bool net_stopped { get; set; }
    [Networked] Vector2 net_position { get; set; }
    //position of boat on the client of whoever is piloting
    [Networked] public float velocityMagnitude { get; set; }
    [Networked] Vector2 pilotInput { get; set; }
    [Networked] public bool consistentDirection { get; set; }
    [Networked] public short fuelCount { get; set; }
    [Networked] public float fuelTime { get; set; }


    public override void FixedUpdateNetwork()
    {
        if (view.HasStateAuthority)
        {
            velocityMagnitude = velocity.magnitude;
        }
        else
        {

        }
    }

    private void Awake()
    {

        tileSlotTiles = new BoatTileSlot[tileSlots.Length];
        for (int i = 0; i < tileSlots.Length; i++)
        {
            tileSlotTiles[i] = new BoatTileSlot();
            tileSlotTiles[i].tiles = new placedTile[2];
        }

        rb = GetComponent<Rigidbody2D>();
        boatMusic.volume = 0;

        beingConstructed = true;

        foreach (SpriteRenderer s in sprites)
        {
            s.enabled = false;
        }
    }
    private void Start()
    {
        if (reticleSprite)
            reticleSprite.color = new Color(1, 1, 1, 0);

        if (sailTethers)
            sailTethers.SetActive(false);

        wakeEmissionMod = wakeParticles.emission;
        maxWakeEmission = wakeEmissionMod.rateOverTimeMultiplier;

        wakeEmissionMod.rateOverTimeMultiplier = 0;
        wakeParticles.Play();

        if (usesSails)
        {
            sailsSprite.sprite = sailsClosedSprite;
            sailsUp = false;

        }

        inside = new bool[floorInsideCheck.Length];

    }

    void MusicSwitchCooldownOver()
    {
        musicSwitchCooldown = false;
    }

    public void Grabbed(EnemyOceanHand hand)
    {
        grabbedByHand = hand;
        sailStartup = 0;
    }

    public void RandomIslandPosition(int mapSize, Vector2Int center)
    {
        int maxAttempts = 100;
        List<Vector3Int> validPositions = new List<Vector3Int>();

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate a potential position within the map bounds
            Vector3Int potentialPosition = new Vector3Int(
                Mathf.RoundToInt(UnityEngine.Random.Range(-mapSize / 2, mapSize / 2)),
                Mathf.RoundToInt(UnityEngine.Random.Range(-mapSize / 2, mapSize / 2)),
                0
            ) + (Vector3Int)center;

            // Check if the tile is valid (on water or nothing)
            if (featurePlacer.Instance.waterTilemap.GetTile(potentialPosition) != null || featurePlacer.Instance.groundTilemap.GetTile(potentialPosition) == null)
            {
                // Check if there's already a placed tile at the position
                if (gameManager.instance.placedTiles.Find(t => t.position == (Vector2Int)potentialPosition).tileGameObject == null)
                {
                    bool tileInWay = false;

                    // Check the surrounding tiles for any placed tiles
                    for (int x = -6; x < 6; x++)
                    {
                        if (tileInWay) break;

                        for (int y = -5; y < 3; y++)
                        {
                            if (gameManager.instance.placedTiles.Find(t => t.position == (Vector2Int)potentialPosition + new Vector2Int(x, y)).tileGameObject != null)
                            {
                                tileInWay = true;
                                break;
                            }
                        }
                    }

                    if (!tileInWay)
                    {
                        // Perform a boxcast to check if there are any colliders in the way
                        Vector2 boxSize = new Vector2(20, 8);
                        RaycastHit2D hit = Physics2D.BoxCast((Vector2Int)potentialPosition, boxSize, 0f, Vector2.zero);

                        if (hit.collider == null)
                        {
                            // If no colliders are hit, store the valid position
                            validPositions.Add(potentialPosition);
                        }
                    }
                }
            }
        }

        // Find the closest position to the center
        if (validPositions.Count > 0)
        {
            Vector3Int closestPosition = validPositions.OrderBy(pos => Vector2Int.Distance((Vector2Int)pos, center)).First();

            // Set the transform position to the closest valid position
            transform.position = closestPosition;
            RPC_FixPosition(closestPosition.x, (float)closestPosition.y);
        }
    }


    public void Initialize(bool disableSpawnAnim, int typeIndex)
    {
        pilotInput = new Vector2(transform.localScale.x, 0);
        menu.instance.boats.Add(this);

        boatTypeIndex = typeIndex;

        normalCamSize = FindAnyObjectByType<Unity.Cinemachine.CinemachineVirtualCamera>().m_Lens.OrthographicSize;
        oldPos = transform.position;

        if (Runner.IsSharedModeMasterClient) RPC_FixPosition(transform.position.x, transform.position.y);

        if (!disableSpawnAnim)
        {
            constructSound.Play();

            beingConstructed = true;

            foreach (SpriteRenderer s in sprites)
            {
                s.enabled = true;
                s.color = new Color(1, 1, 1, 0);
                s.DOFade(1, constructTime);
            }

            if (SingletonRunner.runner.IsSharedModeMasterClient)
            {
                StartCoroutine(_CreateStartingTiles());
            }

            foreach (Light2D l in fadeInLights)
            {
                float i = l.intensity;
                l.intensity = 0;
                DOTween.To(() => l.intensity, x => l.intensity = x, i, constructTime);

            }

            Invoke("FinishConstruction", constructTime + 0.1f);
        }
        else
        {
            foreach (SpriteRenderer s in sprites)
            {
                s.enabled = true;
                s.color = new Color(1, 1, 1, 1);
            }

            beingConstructed = false;
        }

        if (insideColliders)
            insideColliders.SetActive(false);
        //insideRoofSprite.enabled = false;

        if (fuelItem)
        {
            maxFuelTime = fuelItem.fuelSmeltTime;
            fuelTime = fuelItem.fuelSmeltTime;
        }

    }

    IEnumerator _CreateStartingTiles()
    {
        while (true)
        {
            bool found = false;
            for (int i = 0; i < gameManager.instance.boatsHolder.childCount; i++)
            {
                if (gameManager.instance.boatsHolder.GetChild(i).GetComponent<Boat>().boatID == boatID)
                {
                    found = true;
                }
            }
            yield return null;

            if (found) break;
        }

        int j = 0;
        foreach (item i in startingTiles)
        {
            gameManager.instance.RPC_CreateBoatTile(DataPersistanceManager.userId, i.itemId, true, "", view.Id, j);
            j++;
        }
    }

    void FinishConstruction()
    {
        beingConstructed = false;
    }

    bool CanInteract()
    {
        return !(playerHealth.instance.dead || playerHealth.instance.beingKnocked || pauseScreen.instance.paused || gameManager.instance.writingSign
                || inventory.instance.inFurnace || Chat.Instance.chatOpen || inventory.instance.inventoryOpen || menu.instance.mapMaximized);
    }

    void Update()
    {
        wakeEmissionMod.rateOverTimeMultiplier = dif.magnitude > 0 ? ((velocityMagnitude / moveSpeed) - 0.5f) * (maxWakeEmission * 2)
                                                : 0;

        var ve = wakeParticles.velocityOverLifetime;
        ve.xMultiplier = Mathf.Abs(ve.xMultiplier) * -1 * transform.localScale.x;

        inRangeOfInteractable = false;

        if (net_stopped)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }


        if (view.HasStateAuthority)
        {
            net_stopped = velocity.magnitude == 0;
        }

        rb.linearVelocity = Vector2.zero;

        if (beingConstructed || !player.instance) return;

        //enable mask when on island for trees and stuff
        if (boatBaseMask)
        {
            if (playersOnBoat.Contains(player.instance))
            {
                boatBaseMask.gameObject.SetActive(player.instance.onIsland && !grabbedByHand);
            }
            else
            {
                boatBaseMask.gameObject.SetActive(!grabbedByHand);
            }
        }


        Collider2D enterCheck = Physics2D.OverlapBox(rb.position + (Vector2)(enterCheckPos.position - (Vector3)rb.position),
            enterCheckPos.localScale, 0, mainPlayerLayer);

        if ((BoatType == boatType.speedboat && currentPilot) || playerWater.instance.drowning || playerHealth.instance.dead
            || itemPickup.closestItem || skullAbilityActive)
        {
            enterCheck = null;
        }

        if (enterCheck && !(BoatType == boatType.speedboat && boatMusic.volume != 0) && !enterCooldown)
        {
            if (!indicatorInstance)
                spawnIndicator(enterCheckPos.position + new Vector3(0, enterTextOffset), "Enter");

            inRangeOfInteractable = true;

            if (InputManager.actions["Interact"].started
                && CanInteract())
            {
                destroyIndicator();
                player.instance.transform.position = exitCheckPos.position + new Vector3(0, playerYOffset, 0);
                player.instance.currentBoat = this;

                player.instance.transform.parent = transform;

                RPC_PlayerRideBoat(player.instance.view.Id, true, false);
                enterCooldown = true;
                Invoke("FinishEnterCooldown", 0.8f);

                //automatically pilot boat on enter with speedboat
                if (BoatType == boatType.speedboat)
                {
                    EnterPilot();
                    pilotCooldown = true;
                    Invoke("FinishPilotCooldown", 0.8f);
                }

                StartCoroutine(gameManager.instance._SwitchAmbience());
            }
        }
        //exit boat
        Collider2D exitCheck = Physics2D.OverlapBox(rb.position + (Vector2)(exitCheckPos.position - (Vector3)rb.position) + new Vector2(0, exitCheckYOffset), exitCheckPos.localScale, 0, mainPlayerLayer);

        if (itemPickup.closestItem || skullAbilityActive) exitCheck = null;

        if (BoatType != boatType.speedboat)
        {
            if (exitCheck && !enterCooldown)
            {
                inRangeOfInteractable = true;

                //stop from exiting a boat and ending up in another boat
                Collider2D[] boatBlockingCheck = Physics2D.OverlapBoxAll(enterCheckPos.position + new Vector3(0, playerYOffset), enterCheckPos.localScale, 0, boatLayer);
                bool exitBlocked = false;
                Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>();

                foreach (Collider2D c in boatBlockingCheck)
                {
                    if (!ownColliders.Contains(c)) exitBlocked = true;
                }

                if (!exitBlocked)
                {
                    if (!indicatorInstance)
                        spawnIndicator(exitCheckPos.position + new Vector3(0, exitTextOffset), "Exit");

                    if (InputManager.actions["Interact"].started
                        && CanInteract())
                    {
                        destroyIndicator();
                        player.instance.transform.position = enterCheckPos.position + new Vector3(0, playerYOffset, 0);
                        player.instance.currentBoat = null;

                        player.instance.transform.parent = playerSpawner.instance.transform;

                        enterCooldown = true;
                        Invoke("FinishEnterCooldown", 0.8f);

                        RPC_PlayerRideBoat(player.instance.view.Id, false, false);
                        if (insideColliders)
                            insideColliders.SetActive(false);

                        StartCoroutine(gameManager.instance._SwitchAmbience());
                    }
                }

            }
        }
        //speedboat fuel
        else
        {
            if (exitCheck && !currentPilot)
            {
                if (!indicatorInstance)
                    spawnIndicator(exitCheckPos.position + new Vector3(0, exitTextOffset), "Open Fuel");

                if (InputManager.actions["Interact"].started && (CanInteract() || inventory.instance.inventoryOpen && playerRefueling && !InputManager.usingController))
                {
                    if (!currentSpeedboatFueling && !playerRefueling)
                    {
                        currentSpeedboatFueling = this;
                        LoadFuelItem();
                        inventory.instance.OpenInventory(inventory.interfaceType.speedboat);
                        RPC_SetPlayerFueling();
                    }
                    else
                    {
                        inventory.instance.CloseInventory();
                    }

                }
            }
            else if (currentSpeedboatFueling == this)
            {
                inventory.instance.CloseInventory();
                currentSpeedboatFueling = null;
            }
        }

        bool engineCheck = false;


        //battleship fuel
        if (BoatType == boatType.battleship)
        {
            Collider2D fuelCheck = Physics2D.OverlapBox(rb.position + (Vector2)(floorEnterCheck[3].position - (Vector3)rb.position), floorEnterCheck[3].localScale, 0, mainPlayerLayer);

            if (itemPickup.closestItem) fuelCheck = null;

            if (fuelCheck)
            {
                engineCheck = true;
                inRangeOfInteractable = true;

                if (!indicatorInstance)
                    spawnIndicator(floorEnterCheck[3].position + new Vector3(0, floorEnterTextOffset[3]), "Open Reactor");

                if (InputManager.actions["Interact"].started
                    && (CanInteract() || (inventory.instance.inventoryOpen && playerRefueling && !InputManager.usingController)))
                {
                    if (!currentSpeedboatFueling && !playerRefueling)
                    {
                        currentSpeedboatFueling = this;
                        LoadFuelItem();
                        inventory.instance.OpenInventory(inventory.interfaceType.battleship);
                        RPC_SetPlayerFueling();
                    }
                    else
                    {
                        inventory.instance.CloseInventory();
                    }

                }
            }
            else if (currentSpeedboatFueling == this)
            {
                inventory.instance.CloseInventory();
                currentSpeedboatFueling = null;
            }
        }



        //exit pilot
        if (currentPilot == player.instance && (InputManager.actions["Interact"].started || InputManager.actions["Exit"].started) && !Chat.Instance.chatOpen && !pauseScreen.instance.paused && !pilotCooldown)
        {
            ExitPilot();
            if (BoatType == boatType.speedboat)
            {
                destroyIndicator();
                player.instance.transform.position = enterCheckPos.position + new Vector3(0, playerYOffset, 0);
                player.instance.currentBoat = null;

                player.instance.transform.parent = playerSpawner.instance.transform;

                RPC_PlayerRideBoat(player.instance.view.Id, false, false);
                if (insideColliders)
                    insideColliders.SetActive(false);

            }
            pilotCooldown = true;
            Invoke("FinishPilotCooldown", 0.8f);

        }

        //exit alt action
        for (int j = 0; j < currentPlayerAltAction.Length; j++)
        {
            if (currentPlayerAltAction[j] == player.instance && (InputManager.actions["Interact"].started
                || (InputManager.actions["Exit"].started && !menu.instance.mapMaximized)) && CanInteract() && !altActionCooldown)
            {
                ExitAltAction(j);

                MouseCursor.state = 0;

                altActionCooldown = true;
                Invoke("FinishAltCooldown", 0.8f);

            }
        }

        //enter pilot
        Collider2D pilotCheck = Physics2D.OverlapBox(rb.position + new Vector2(pilotCheckPos.localPosition.x * transform.localScale.x, pilotCheckPos.localPosition.y), pilotCheckPos.localScale, 0, mainPlayerLayer);
        if (BoatType == boatType.speedboat || itemPickup.closestItem) pilotCheck = null;

        if (pilotCheck && player.instance.currentBoat == this && currentPilot == null && !playerHealth.instance.dead && !pilotCooldown)
        {
            inRangeOfInteractable = true;

            if (!indicatorInstance)
                spawnIndicator(pilotCheckPos.position + new Vector3(pilotIndicatorOffset.x * transform.localScale.x, pilotIndicatorOffset.y), "Pilot");

            if (InputManager.actions["Interact"].started
                && CanInteract())
            {
                EnterPilot();
                pilotCooldown = true;
                Invoke("FinishPilotCooldown", 0.8f);

            }
        }

        //enter crows nest/guns
        Collider2D altCheck = null;
        if (itemPickup.closestItem) altCheck = null;
        for (int j = 0; j < altCheckPos.Length; j++)
        {
            altCheck = Physics2D.OverlapBox(rb.position + new Vector2(altCheckPos[j].localPosition.x * transform.localScale.x, altCheckPos[j].localPosition.y), altCheckPos[j].localScale, 0, mainPlayerLayer);

            if (altCheck && player.instance.currentBoat == this && currentPlayerAltAction[j] == null && !playerHealth.instance.dead && !altActionCooldown && !inventory.instance.inventoryOpen)
            {
                if (!indicatorInstance)
                    spawnIndicator(altCheckPos[j].position + new Vector3(0, altTextOffset), altActionIndicatorText[j]);

                inRangeOfInteractable = true;

                if (InputManager.actions["Interact"].started
                    && !(playerHealth.instance.beingKnocked || pauseScreen.instance.paused || gameManager.instance.writingSign || inventory.instance.inFurnace || Chat.Instance.chatOpen))
                {
                    EnterAltAction(j);
                    altActionCooldown = true;
                    Invoke("FinishAltCooldown", 0.8f);

                }

                break;
            }
        }

        //alt actions
        if (currentPlayerAltAction.Length >= 1 && currentPlayerAltAction[0] == player.instance)
        {
            if (BoatType == boatType.battleship)
            {
                Collider2D missileCheck = Physics2D.OverlapCircle(HandMove.instance.mousePosition, 1, oceanRockLayer);

                MouseCursor.state = 1;

                if (missileCheck)
                {
                    EnemyOceanHand oceanHand = missileCheck.GetComponentInParent<EnemyOceanHand>();

                    //ocean rock
                    if (oceanHand == null && !alreadyDestroyedRockLocations.Contains(missileCheck.transform.position))
                    {
                        missileReticle.transform.position = missileCheck.transform.position + new Vector3(-0.54f, 0.85f);

                        if (!missileReticle.activeSelf)
                        {
                            missileReticle.SetActive(true);
                            reticleAnim.Play();
                            reticleSprite.color = new Color(1, 1, 1, 0);
                            reticleSprite.DOFade(0.8f, 0.3f);
                        }

                        if (InputManager.actions["Item Primary"].started)
                        {
                            GameObject missile = Runner.Spawn(missilePrefab, missileSpawnPoint.position, Quaternion.identity).gameObject;
                            missile.GetComponent<Missile>().RPC_Shoot(missileReticle.transform.position, (int)transform.localScale.x, missileCheck.transform.position);

                            alreadyDestroyedRockLocations.Add(missileCheck.transform.position);
                        }

                    }
                    //ocean hand 
                    else if(oceanHand && !oceanHand.health.dead)
                    {
                        missileReticle.transform.position = missileCheck.transform.position + new Vector3(0, 2.34f);

                        if (!missileReticle.activeSelf)
                        {
                            missileReticle.SetActive(true);
                            reticleAnim.Play();
                            reticleSprite.color = new Color(1, 1, 1, 0);
                            reticleSprite.DOFade(0.8f, 0.3f);
                        }

                        if (InputManager.actions["Item Primary"].started)
                        {
                            GameObject missile = Runner.Spawn(missilePrefab, missileSpawnPoint.position, Quaternion.identity).gameObject;
                            missile.GetComponent<Missile>().RPC_Shoot(missileReticle.transform.position, (int)transform.localScale.x, missileCheck.transform.position,oceanHand.view);

                            oceanHand.health.stunned = true;
                            oceanHand.health.rb.bodyType = RigidbodyType2D.Static;
                        }
                    }


                }
                else
                {
                    missileReticle.SetActive(false);
                }

            }

            camFollow.localPosition = Vector2.Lerp(camFollow.localPosition, velocity * camLookahead * transform.localScale, Time.deltaTime * 2.5f);
        }
        else if (missileReticle)
        {
            missileReticle.SetActive(false);
        }
        //ghost ship skull
        if (currentPlayerAltAction.Length >= 2 && currentPlayerAltAction[1] == player.instance)
        {
            camFollow.localPosition = Vector2.Lerp(camFollow.localPosition, velocity * camLookahead * transform.localScale, Time.deltaTime * 2.5f);

            if (!usingSkull)
            {
                inventory.instance.hotbarGroup.DOFade(0, 0.5f);
                menu.instance.phantomShiftGroup.DOFade(0.9f, 1);
                usingSkull = true;

                itemCounts = inventory.instance.GetItemCounts(false);
            }

            bool canActivate = abilityCooldownTime <= 0 && sailsUp;

            if (InputManager.actions["Ability"].started && canActivate && !skullAbilityActive)
            {
                itemCounts = inventory.instance.GetItemCounts(false);

                if (itemCounts.ContainsKey(soulItem) && itemCounts[soulItem] > 0)
                {
                    RPC_ActivateAbility();

                    foreach (inventorySlot slot in inventory.instance.slots)
                    {
                        if (slot.itemInSlot == soulItem)
                        {
                            if (slot.itemCountInSlot > 1)
                            {
                                slot.itemCountInSlot -= 1;
                            }
                            else
                            {
                                inventory.instance.removeItem(slot);
                            }

                            break;
                        }

                    }
                    itemCounts = inventory.instance.GetItemCounts(false);
                }
            }


            if (abilityCooldownTime < abilityCooldown && abilityCooldownTime > 0)
            {
                menu.instance.phantomShiftText.text = "Cooldown: " + (Mathf.Round(abilityCooldownTime + 0.5f)) + "s";
            }
            else if (!skullAbilityActive)
            {
                //change text color
                if (itemCounts.ContainsKey(soulItem) && itemCounts[soulItem] > 0)
                {
                    menu.instance.phantomShiftText.color = menu.instance.phantomShiftTextColor;
                    menu.instance.phantomShiftText.text = "Cost: 1 Soul  (" + itemCounts[soulItem] + ")";
                }
                else
                {
                    menu.instance.phantomShiftText.color = new Color(0.3f, 0, 0, 0.7f);
                    menu.instance.phantomShiftText.text = "Cost: 1 Soul  (0)";
                }

            }
            else
            {
                menu.instance.phantomShiftText.text = "";
            }


            if (canActivate)
            {
                menu.instance.phantomShiftAbilityText.color = menu.instance.phantomShiftTextColor;
            }
            else if (!skullAbilityActive)
            {
                menu.instance.phantomShiftAbilityText.color = menu.instance.phantomShiftText.color = new Color(0.4f, 0.5f, 0.5f, 0.7f);
                menu.instance.phantomShiftText.color = menu.instance.phantomShiftAbilityText.color;
            }


        }
        else if (usingSkull)
        {
            inventory.instance.hotbarGroup.DOFade(1, 0.5f);
            menu.instance.phantomShiftGroup.DOFade(0, 1);
            usingSkull = false;
        }

        if (view.HasStateAuthority)
        {
            velocityMagnitude = velocity.magnitude;
            net_position = transform.position;
        }

        if (velocityMagnitude > 0)
        {
            if (!moveAmbience.isPlaying)
            {
                moveAmbience.Play();
            }
            moveAmbience.volume = velocityMagnitude / moveSpeed;
        }
        else if (moveAmbience.isPlaying)
        {
            moveAmbience.Stop();
        }

        if (abilityCooldownTime > 0) abilityCooldownTime -= Time.deltaTime;


        if (!grabbedByHand)
        {
            if (!skullAbilityActive)
            {
                //default change velocity
                if (!(!usesSails && !currentPilot) && !(usesSails && !sailsUp))
                {
                    if (pilotInput.magnitude < 0.9)
                        velocity = Vector2.Lerp(velocity, pilotInput * moveSpeed, Time.deltaTime * velocityChangeSpeed);
                    else
                        velocity = Vector2.Lerp(velocity, pilotInput.normalized * moveSpeed, Time.deltaTime * velocityChangeSpeed);
                }
                if (!(usesSails && sailsUp))
                {
                    velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * velocityChangeSpeed);
                }

                dashVelocity = velocity;
            }
            else
            {
                velocity = dashVelocity.normalized * dashForce;
            }

        }
        //grabbed
        else
        {
            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * 3);
        }

        float oldScale = transform.localScale.x;

        if (velocity.x > 0.02f)
        {
            transform.localScale = new Vector2(1, 1);
        }
        if (velocity.x < -0.02f)
        {
            transform.localScale = new Vector2(-1, 1);
        }

        //destroy indicator when flip scale
        if (oldScale != transform.localScale.x)
        {
            destroyIndicator();
            if (smoke) smoke.Clear();
        }

        if (usesSails)
        {
            if (!sailsUp)
            {
                sailStartup = velocity.magnitude / moveSpeed;
            }
            else if (sailStartup < 1 && !grabbedByHand)
            {
                sailStartup += Time.deltaTime / 3;
            }
            sailStartup = Mathf.Clamp(sailStartup, 0, 1);
        }
        else
        {
            sailStartup = 1;
        }

        //pilot controls
        if (currentPilot == player.instance && !((BoatType == boatType.speedboat || BoatType == boatType.battleship) && fuelTime <= 0 && fuelCount <= 0) && !(usesSails && !sailsUp) && !Chat.Instance.chatOpen)
        {
            //dont stop if the sails are up
            if (usesSails)
            {
                if (InputManager.instance.movementInput.magnitude != 0)
                {
                    pilotInput = Vector2.Lerp(pilotInput, InputManager.instance.movementInput, Time.deltaTime * velocityChangeSpeed);
                }
                else
                {
                    pilotInput = Vector2.Lerp(pilotInput, pilotInput.normalized, Time.deltaTime * velocityChangeSpeed * 5);
                }

            }
            else
            {
                pilotInput = Vector2.Lerp(pilotInput, InputManager.instance.movementInput, Time.deltaTime * velocityChangeSpeed);

                if (pilotInput.x < 0.05f && InputManager.instance.movementInput.x == 0) pilotInput = new Vector2(0, pilotInput.y);
                if (pilotInput.y < 0.05f && InputManager.instance.movementInput.y == 0) pilotInput = new Vector2(pilotInput.x, 0);
            }

            if (pilotInput.x > 0.93f && InputManager.instance.movementInput.x == 1) pilotInput = new Vector2(1, pilotInput.y);

            if (pilotInput.y > 0.93f && InputManager.instance.movementInput.y == 1) pilotInput = new Vector2(pilotInput.x, 1);


            //close map
            if (menu.instance.mapMaximized)
            {
                menu.instance.OpenMap(false);
            }

            //move cam

            camFollow.localPosition = Vector2.Lerp(camFollow.localPosition, velocity * camLookahead * transform.localScale, Time.deltaTime * 2.5f);

        }
        else if ((!currentPilot && !usesSails) || ((BoatType == boatType.speedboat || BoatType == boatType.battleship) && fuelTime <= 0))
        {
            camFollow.localPosition = Vector2.zero;

            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * velocityChangeSpeed * 2.5f);
            pilotInput = Vector2.zero;
            if (smoke)
                smoke?.Stop();
        }

        if (view.HasStateAuthority) consistentDirection = pilotInput.magnitude >= 0.9f;

        if (currentPilot)
            currentPilot.transform.localPosition = pilotPosition.localPosition + new Vector3(0, playerYOffset, 0);

        if (velocityMagnitude >= 0.02f)
        {
            if (smoke)
                smoke?.Play();
        }
        else
        {
            if (smoke)
                smoke?.Stop();
        }


        //round small distances

        if (pilotInput.x == 0 || (usesSails && !sailsUp))
        {
            if (MathF.Abs(velocity.x) <= 0.007f)
            {
                velocity = new Vector2(0, velocity.y);
            }

        }
        if (pilotInput.y == 0 || (usesSails && !sailsUp))
        {
            if (MathF.Abs(velocity.y) <= 0.007f)
            {
                velocity = new Vector2(velocity.x, 0);
            }
        }

        //make boat not pushable when standing still
        if (velocityMagnitude == 0)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        //music
        if (!musicSwitchCooldown)
        {


            if (((!usesSails && currentPilot) || (usesSails && sailsUp)) &&
                (player.instance.currentBoat == this || (menu.instance.spectatingPlayer && menu.instance.spectatingPlayer.currentBoat == this))
                && !((BoatType == boatType.speedboat || BoatType == boatType.battleship) && fuelTime <= 0 && fuelCount <= 0))
            {
                if (!playingMusic)
                {
                    musicSwitchCooldown = true;
                    Invoke("MusicSwitchCooldownOver", 1);

                    if (boatMusic.volume == 0)
                    {
                        boatMusic.time = timeInSong;
                    }

                    boatMusic.DOFade(1, 3);
                    playingMusic = true;

                    if (player.instance.currentBoat == this || (menu.instance.spectatingPlayer && menu.instance.spectatingPlayer.currentBoat == this))
                    {
                        gameManager.instance.musicSource.DOKill();
                        gameManager.instance.musicSource.DOFade(0, 1f);
                        StartCoroutine(gameManager.instance._SwitchAmbience());
                    }

                }
            }

            else if (playingMusic)
            {
                musicSwitchCooldown = true;
                Invoke("MusicSwitchCooldownOver", 1);

                playingMusic = false;
                boatMusic.DOFade(0, 3);

                //dont play song if far out at sea
                if (player.instance.onIsland)
                {
                    if (!TimeManager.instance.isNight)
                    {
                        StartCoroutine(gameManager.instance._SwitchMusic("day", 0.5f));
                    }
                    else
                    {
                        StartCoroutine(gameManager.instance._SwitchMusic("night", 0.5f));
                    }
                }


                timeInSong = boatMusic.time;
            }
        }

        //ambient sound
        if ((player.instance.currentBoat == this || (menu.instance.spectatingPlayer && menu.instance.spectatingPlayer.currentBoat == this)) && !playingAmbience)
        {
            playingAmbience = true;
            boatAmbience.DOFade(0.9f, 2f);
        }
        else if (!((player.instance.currentBoat == this || (menu.instance.spectatingPlayer && menu.instance.spectatingPlayer.currentBoat == this))) && playingAmbience)
        {
            playingAmbience = false;
            boatAmbience.DOFade(0, 2f);
        }


        //inside check
        int i = 0;
        if (!player.instance.pilotingBoat)
        {
            foreach (Transform insideCheckPos in floorInsideCheck)
            {
                if (insideCheckPos && player.instance.currentBoat == this)
                {
                    Collider2D insideCheck = Physics2D.OverlapBox(rb.position + new Vector2(insideCheckPos.localPosition.x * transform.localScale.x, insideCheckPos.localPosition.y), insideCheckPos.localScale, 0, mainPlayerLayer);
                    if (insideCheck && !inside[i])
                    {
                        insideSprites[i].DOFade(insideFadeOpacity[i], 0.5f);
                        inside[i] = true;
                    }
                    else if (!insideCheck && inside[i])
                    {
                        insideSprites[i].DOFade(1f, 0.5f);
                        inside[i] = false;
                    }

                }
                else if (inside[i])
                {
                    insideSprites[i].DOFade(1f, 0.5f);
                    inside[i] = false;
                }
                i++;
            }
        }


        //sails
        bool anySailCheck = false;

        if (usesSails && player.instance.currentBoat == this && !player.instance.pilotingBoat && !player.instance.inBoatAltAction)
        {
            foreach (Transform t in sailsCheckPos)
            {
                if (itemPickup.closestItem) continue;

                Collider2D sailsCheck = Physics2D.OverlapBox(rb.position + new Vector2(t.localPosition.x * transform.localScale.x, t.localPosition.y), t.localScale, 0, mainPlayerLayer);
                if (sailsCheck && !altCheck)
                {
                    anySailCheck = true;
                    inRangeOfInteractable = true;

                    if (!sailsUp)
                    {
                        if (!indicatorInstance)
                            spawnIndicator(t.position + new Vector3(0, 0.7f, 0), "Open Sails");

                        if (InputManager.actions["Interact"].started && CanInteract())
                        {
                            destroyIndicator();
                            RPC_SetSails(true);
                        }
                    }
                    else
                    {
                        if (!indicatorInstance)
                            spawnIndicator(t.position + new Vector3(0, 0.7f, 0), "Close Sails");

                        if (InputManager.actions["Interact"].started && CanInteract())
                        {
                            destroyIndicator();
                            RPC_SetSails(false);
                        }
                    }

                }
            }

        }

        bool inRangeOfLadder = false;

        //ladder
        if (player.instance.currentBoat == this && !player.instance.pilotingBoat)
        {
            i = 0;
            foreach (Transform f2EnterCheckPos in floorEnterCheck)
            {
                if (i > floorExitCheck.Length - 1 || itemPickup.closestItem) continue;

                //enter f2
                Collider2D f2enterCheck = Physics2D.OverlapBox(rb.position + new Vector2(f2EnterCheckPos.localPosition.x * transform.localScale.x, f2EnterCheckPos.localPosition.y), f2EnterCheckPos.localScale, 0, mainPlayerLayer);

                if (itemPickup.closestItem || usingSkull) f2enterCheck = null;

                if (f2enterCheck && !enterCooldown)
                {
                    inRangeOfLadder = true;
                    inRangeOfInteractable = true;

                    if (!indicatorInstance)
                        spawnIndicator(f2EnterCheckPos.position + new Vector3(0, floorEnterTextOffset[i]), "Climb");

                    if (InputManager.actions["Interact"].started && CanInteract())
                    {
                        enterCooldown = true;
                        Invoke("FinishEnterCooldown", 0.4f);

                        destroyIndicator();
                        player.instance.transform.position = floorExitCheck[i].position + new Vector3(0, playerYOffset, 0);
                        RPC_PlayerGoUp(i + 1, player.instance.view.Id);
                    }
                }
                i++;
            }

            i = 0;
            foreach (Transform f2ExitCheckPos in floorExitCheck)
            {
                Collider2D f2exitCheck = Physics2D.OverlapBox(rb.position + new Vector2(f2ExitCheckPos.localPosition.x * transform.localScale.x, f2ExitCheckPos.localPosition.y), f2ExitCheckPos.localScale, 0, mainPlayerLayer);

                if (itemPickup.closestItem || usingSkull) f2exitCheck = null;

                if (f2exitCheck && !enterCooldown)
                {
                    inRangeOfLadder = true;
                    inRangeOfInteractable = true;

                    if (!indicatorInstance)
                        spawnIndicator(f2ExitCheckPos.position + new Vector3(0, floorEnterTextOffset[i]), "Climb");

                    if (InputManager.actions["Interact"].started && CanInteract())
                    {
                        enterCooldown = true;
                        Invoke("FinishEnterCooldown", 0.4f);

                        destroyIndicator();
                        player.instance.transform.position = floorEnterCheck[i].position + new Vector3(0, playerYOffset, 0);

                        //silly solution for basement
                        if (i != 2)
                            RPC_PlayerGoUp(i, player.instance.view.Id);
                        else
                            RPC_PlayerGoUp(0, player.instance.view.Id);
                    }
                }
                i++;
            }


        }

        if (!enterCheck && !exitCheck && !pilotCheck && !anySailCheck && !inRangeOfLadder && !engineCheck && !altCheck)
        {
            destroyIndicator();
        }

        if (BoatType == boatType.speedboat || BoatType == boatType.battleship)
        {

            if (fuelTime <= 0 && fuelCount > 0 && fuelItem)
            {
                if (currentSpeedboatFueling == this)
                    SaveFuelUI();

                maxFuelTime = fuelItem.fuelSmeltTime;
                fuelTime = fuelItem.fuelSmeltTime;
                fuelCount -= 1;
                if (fuelCount <= 0)
                {
                    fuelItem = null;
                }

                if (currentSpeedboatFueling == this)
                    LoadFuelItem();
            }

            if (currentSpeedboatFueling == this)
            {
                inventory.instance.UpdateSpeedboatUI();
            }

            if (fuelTime > 0 && velocityMagnitude >= 0.02f)
            {
                fuelTime -= Time.deltaTime;
            }


            //update upon placing item
            inventorySlot slot = inventory.instance.speedboatFuelSlot;
            if (BoatType == boatType.battleship) slot = inventory.instance.battleshipFuelSlot;

            if (currentSpeedboatFueling == this && fuelCount != slot.itemCountInSlot)
            {
                SaveFuelUI();
                LoadFuelItem();
            }

        }

        //close sails when no people in boat
        if (usesSails && playersOnBoat.Count == 0 && sailsUp)
        {
            RPC_SetSails(false);
        }

        if (indicatorInstance) indicatorInstance.transform.position = transform.TransformPoint(indicatorLocalPos);

        //building
        if (player.instance.currentBoat == this)
        {
            float tileSlotCheckSize = 1;
            float closestDist = tileSlotCheckSize;

            highlightedTileSlotIndex = -1;
            int j = 0;
            foreach (Transform t in tileSlots)
            {
                float d = Vector2.Distance(HandMove.instance.mousePosition, t.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    highlightedTileSlotIndex = j;
                }
                j++;
            }

        }

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetSails(bool open)
    {
        sailsUp = open;

        if (sailTethers)
            sailTethers.SetActive(open);
        if (open)
        {
            sailsSprite.sprite = sailsOpenSprite;
            sailRaiseSound.pitch = 1;
        }
        else
        {
            sailsSprite.sprite = sailsClosedSprite;
            sailRaiseSound.pitch = 0.8f;
        }
        sailRaiseSound.Play();
    }

    void FinishPilotCooldown()
    {
        pilotCooldown = false;
    }
    void FinishAltCooldown()
    {
        altActionCooldown = false;
    }


    void LoadFuelItem()
    {
        inventorySlot slot = inventory.instance.speedboatFuelSlot;
        if (BoatType == boatType.battleship) slot = inventory.instance.battleshipFuelSlot;

        inventory.instance.removeItem(slot);

        foreach (Transform child in slot.transform)
        {
            Destroy(child.gameObject);
        }

        if (fuelItem)
            inventory.instance.addItemInSlot(fuelItem, fuelCount, 1, new ItemData(0), slot, slot.transform.position);

        inventory.instance.speedboatFuelTime = fuelTime;
    }
    void SaveFuelUI()
    {
        inventorySlot slot = inventory.instance.speedboatFuelSlot;
        if (BoatType == boatType.battleship) slot = inventory.instance.battleshipFuelSlot;

        fuelCount = (short)slot.itemCountInSlot;
        fuelItem = slot.itemInSlot;
        fuelTime = inventory.instance.speedboatFuelTime;
    }

    public void CloseFuelUI()
    {
        inventorySlot slot = inventory.instance.speedboatFuelSlot;
        if (BoatType == boatType.battleship) slot = inventory.instance.battleshipFuelSlot;

        fuelCount = (short)slot.itemCountInSlot;
        fuelItem = slot.itemInSlot;
        fuelTime = inventory.instance.speedboatFuelTime;

        if (slot.itemInSlot)
            RPC_SetFuelSlot(slot.itemInSlot.itemId, (short)slot.itemCountInSlot, fuelTime);
        else
            RPC_SetFuelSlot("null", (short)slot.itemCountInSlot, fuelTime);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetPlayerFueling()
    {
        playerRefueling = true;
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetFuelSlot(string Item, short count, float fuelTime)
    {
        playerRefueling = false;
        fuelCount = count;
        fuelItem = gameManager.instance.itemDictionary[Item];
        this.fuelTime = fuelTime;
    }


    public void EnterAltAction(int actionNum = 0)
    {
        menu.instance.OpenMap(false, true);
        RPC_RequestAltAction(true, player.instance.view.Id, actionNum);
    }
    public void EnterPilot()
    {
        RPC_RequestPilot(true, player.instance.view.Id);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestPilot(bool isPilot, NetworkId viewId)
    {
        RPC_PlayerPilot(isPilot, viewId);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestAltAction(bool isPilot, NetworkId viewId, int actionNum)
    {
        RPC_PlayerAltAction(isPilot, viewId, actionNum);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_PlayerPilot(bool isPilot, NetworkId viewId)
    {
        player newPilot = Runner.FindObject(viewId).GetComponent<player>();
        newPilot.pilotingBoat = isPilot;

        if (isPilot)
        {
            currentPilot = newPilot;

            if (viewId == player.instance.view.Id)
            {
                inventory.instance.hotbarGroup.DOFade(0, 0.5f);

                menu.instance.OpenMap(false, true);

                view.RequestStateAuthority();

                DOTween.To(() => player.instance.cam.m_Lens.OrthographicSize, x => player.instance.cam.m_Lens.OrthographicSize = x, pilotCamSize, 1.3f);
                destroyIndicator();
                player.instance.pilotingBoat = true;
                player.instance.transform.position = pilotPosition.position + new Vector3(0, playerYOffset, 0);
                currentPilot = player.instance;
                player.instance.cam.Follow = camFollow;

                player.instance.rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                if (currentPilot == player.instance)
                {
                    ExitPilot();
                }
            }

        }
        else
        {
            currentPilot = null;
        }

    }

    //viaserver
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_PlayerAltAction(bool enter, NetworkId viewId, int actionNum)
    {
        player p = Runner.FindObject(viewId).GetComponent<player>();

        p.inBoatAltAction = enter;
        p.currentAltAction = actionNum;

        SortingGroup group = p.sortingGroup;

        if (altActionSound)
        {
            altActionSound.pitch = 1;
            if (!enter) altActionSound.pitch = 0.9f;

            altActionSound.Play();
        }

        if (enter)
        {
            currentPlayerAltAction[actionNum] = p;

            group.sortingOrder = altActionLayer[actionNum];

            if (viewId == player.instance.view.Id)
            {
                if (BoatType == boatType.pirateShip || BoatType == boatType.ghost)
                {
                    player.instance.cam.Follow = camFollow;
                }

                if(BoatType != boatType.pirateShip)
                    inventory.instance.hotbarGroup.DOFade(0, 0.5f);

                DOTween.To(() => player.instance.cam.m_Lens.OrthographicSize, x => player.instance.cam.m_Lens.OrthographicSize = x, altActionCamSize, 1.3f);
                destroyIndicator();
                player.instance.inBoatAltAction = true;
                player.instance.transform.position = altPlayerPos[actionNum].position + new Vector3(0, playerYOffset, 0);
                currentPlayerAltAction[actionNum] = player.instance;

                player.instance.rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                if (currentPlayerAltAction[actionNum] == player.instance)
                {
                    ExitAltAction(actionNum);
                }
            }

        }
        else
        {
            group.sortingOrder = 10;
            currentPlayerAltAction[actionNum] = null;
        }

    }


    public void ExitPilot()
    {
        inventory.instance.hotbarGroup.DOFade(1, 0.5f);

        player.instance.pilotingBoat = false;
        player.instance.transform.position = unpilotPosition.position + new Vector3(0, playerYOffset, 0);
        currentPilot = null;
        DOTween.To(() => player.instance.cam.m_Lens.OrthographicSize, x => player.instance.cam.m_Lens.OrthographicSize = x, normalCamSize, 1f);
        player.instance.cam.Follow = player.instance.camFollow;

        player.instance.rb.bodyType = RigidbodyType2D.Kinematic;
        player.instance.rb.linearVelocity = Vector2.zero;
        RPC_FixPosition(transform.position.x, transform.position.y);
        RPC_PlayerPilot(false, player.instance.view.Id);
    }

    public void ExitAltAction(int actionNum)
    {
        inventory.instance.hotbarGroup.DOFade(1, 0.5f);

        player.instance.inBoatAltAction = false;
        player.instance.transform.position = altPlayerPosExit[actionNum].position + new Vector3(0, playerYOffset, 0);
        DOTween.To(() => player.instance.cam.m_Lens.OrthographicSize, x => player.instance.cam.m_Lens.OrthographicSize = x, normalCamSize, 1f);
        player.instance.cam.Follow = player.instance.camFollow;

        player.instance.rb.bodyType = RigidbodyType2D.Kinematic;
        player.instance.rb.linearVelocity = Vector2.zero;
        RPC_PlayerAltAction(false, player.instance.view.Id, actionNum);
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (view.HasStateAuthority && !net_stopped && collision.gameObject.layer == 22)
        {
            RPC_CollideSound(0.2f + (0.8f * velocityMagnitude / moveSpeed));

            velocity = Vector2.zero;
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_CollideSound(float vol)
    {
        if (!collisionSound.isPlaying)
        {
            collisionSound.volume = vol;
            collisionSound.Play();

            if (currentPilot == player.instance)
            {
                gameManager.endScreenStats["crashed"]++;
            }

        }
    }

    private void FixedUpdate()
    {
        if (!player.instance || beingConstructed) return;

        // Sync position when not moving at full speed
        if (!view.HasStateAuthority)
        {
            if (Vector2.Distance(transform.position, net_position) > networkTeleportDist)
            {
                transform.position = net_position;
            }
            else if (!consistentDirection)
            {
                //rb.MovePosition(net_position);
                rb.MovePosition(Vector2.Lerp(rb.position, net_position, networkLerpSpeed * Time.deltaTime));
            }
        }

        dif = rb.position - oldPos;

        if (view.StateAuthority == null && Runner.IsSharedModeMasterClient)
            view.RequestStateAuthority();

        if ((view.HasStateAuthority || consistentDirection))
        {
            Vector2 moveDelta;

            if (usesSails && sailsUp)
                moveDelta = (Vector2)(velocity * sailStartup);
            else
                moveDelta = velocity;

            Vector2 nextPosition = rb.position + moveDelta;

            // Get the bounds of the collider at the *next position*
            Bounds bounds = waterCollider.bounds;
            bounds.center = nextPosition + waterCollider.offset;

            // Convert bounds to tilemap cell positions
            Vector3Int min = MapDisplay.Instance.groundTilemap.WorldToCell(bounds.min);
            Vector3Int max = MapDisplay.Instance.groundTilemap.WorldToCell(bounds.max);

            bool hitsLand = false;
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    TileBase groundTile = MapDisplay.Instance.groundTilemap.GetTile(cellPos);
                    TileBase waterTile = MapDisplay.Instance.waterTilemap.GetTile(cellPos);

                    if (waterTile == null && groundTile != null)
                    {
                        hitsLand = true;
                        break;
                    }
                }
                if (hitsLand) break;
            }

            // Only move if we�re not hitting land
            if (!hitsLand)
            {
                rb.MovePosition(nextPosition);
                playedCollideSound = false;
            }
            else
            {
                if (!playedCollideSound)
                {
                    RPC_CollideSound(0.4f + (0.6f * velocityMagnitude / moveSpeed));
                    playedCollideSound = true;
                }

                //if (Input.GetKeyDown(KeyCode.Semicolon)) rb.MovePosition(transform.position + new Vector3(0, 1, 0));
            }
        }

        // Players move with boat
        foreach (player Player in playersOnBoat)
        {
            if(Player)
            Player.boatVelocity = dif;
        }

        //if (indicatorInstance) indicatorInstance.transform.position += (Vector3)dif;

        oldPos = rb.position;

    }



    public void PlayerRideBoat(NetworkId viewId, bool isEntering, bool disableSound)
    {
        RPC_PlayerRideBoat(viewId, isEntering, disableSound);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayerRideBoat(NetworkId viewId, bool isEntering, bool disableSound)
    {
        NetworkObject newPlayer = Runner.FindObject(viewId);
        player Player = newPlayer.GetComponent<player>();

        if (isEntering)
        {
            if (!disableSound)
                enterSound.pitch = 1;

            Player.boatFloor = 0;

            if (Player.currentBoat != null)
            {
                Player.currentBoat.playersOnBoat.Remove(Player);
            }

            Player.currentBoat = this;
            newPlayer.transform.SetParent(transform);

            Player.GetComponentInChildren<SortingGroup>().sortingOrder = playerFloorLayers[0];

            if (!playersOnBoat.Contains(Player))
                playersOnBoat.Add(Player);
        }
        else
        {
            if (!disableSound)
                enterSound.pitch = 0.85f;

            Player.currentBoat = null;
            newPlayer.transform.SetParent(playerSpawner.instance.transform);
            playersOnBoat.Remove(Player);
            newPlayer.GetComponent<player>().boatVelocity = Vector2.zero;

            if (BoatType == boatType.speedboat)
            {
                beingPiloted = false;
                currentPilot = null;
            }
            Player.GetComponentInChildren<SortingGroup>().sortingOrder = 10;
        }

        if (!disableSound)
            enterSound.Play();

        if (Player == player.instance)
        {
            player.instance.disablePositionSync = true;

            if (insideColliders)
                insideColliders.SetActive(isEntering);

            if (isEntering)
                player.instance.collision.gameObject.layer = 19;
            else
                player.instance.collision.gameObject.layer = 10;
        }
        else
        {
            Player.RPC_EnablePosSync(true);
        }

    }

    public void PlayerGoUp(int floor, NetworkId viewId)
    {
        RPC_PlayerGoUp(floor, viewId);
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_PlayerGoUp(int floor, NetworkId viewId)
    {
        player p = Runner.FindObject(viewId).GetComponent<player>();
        SortingGroup group = p.sortingGroup;
        p.boatFloor = floor;

        int sortingLayer = group.sortingOrder;
        climbSound.pitch = 1;


        group.sortingOrder = playerFloorLayers[floor];

        if (group.sortingOrder < sortingLayer) climbSound.pitch = 0.82f;

        climbSound.pitch += UnityEngine.Random.Range(-0.12f, 0.12f);

        climbSound.Play();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0, 1, 0.5f);
        Gizmos.DrawCube(enterCheckPos.position, enterCheckPos.localScale);
        Gizmos.DrawCube(exitCheckPos.position + new Vector3(0, exitCheckYOffset), exitCheckPos.localScale);
        Gizmos.DrawCube(pilotCheckPos.position, pilotCheckPos.localScale);

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawSphere(enterCheckPos.position + new Vector3(0, enterTextOffset), 0.05f);
        Gizmos.color = new Color(0, 1, 1, 0.5f);
        Gizmos.DrawSphere(exitCheckPos.position + new Vector3(0, exitTextOffset), 0.05f);
        Gizmos.color = new Color(0, 1, 1, 0.5f);
        Gizmos.DrawSphere(pilotCheckPos.position + new Vector3(pilotIndicatorOffset.x * transform.localScale.x, pilotIndicatorOffset.y), 0.05f);

        int i = 0;
        foreach (Transform f2EnterCheckPos in floorEnterCheck)
        {
            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawCube(f2EnterCheckPos.position, f2EnterCheckPos.localScale);
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawSphere(f2EnterCheckPos.position + new Vector3(0, floorEnterTextOffset[i]), 0.05f);
            i++;
        }
        i = 0;
        foreach (Transform f2ExitCheckPos in floorExitCheck)
        {
            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawCube(f2ExitCheckPos.position, f2ExitCheckPos.localScale);
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawSphere(f2ExitCheckPos.position + new Vector3(0, floorExitTextOffset[i]), 0.05f);
            i++;
        }

        foreach (Transform insideCheckPos in floorInsideCheck)
        {
            Gizmos.color = new Color(0, 1, 1, 0.08f);
            Gizmos.DrawCube(insideCheckPos.position, insideCheckPos.localScale);

        }

        foreach (Transform t in sailsCheckPos)
        {
            Gizmos.color = new Color(0, 1, 1, 0.5f);
            Gizmos.DrawCube(t.position, t.localScale);
        }

        foreach (Transform t in altCheckPos)
        {
            Gizmos.color = new Color(1, 0, 1, 0.5f);
            Gizmos.DrawCube(t.position, t.localScale);
            Gizmos.color = new Color(1, 0, 0.5f, 0.5f);
            Gizmos.DrawSphere(t.position + new Vector3(0, altTextOffset), 0.05f);
        }
    }



    public void spawnIndicator(Vector2 position, string interactAction)
    {
        if (indicatorInstance == null)
        {
            indicatorInstance = Instantiate(indicatorPrefab, position, Quaternion.identity);
        }
        else
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);

            indicatorInstance = Instantiate(indicatorPrefab, position, Quaternion.identity);
        }
        indicatorLocalPos = transform.InverseTransformPoint(indicatorInstance.transform.position);

        indicatorAnim = indicatorInstance.GetComponent<Animator>();
        indicatorText = indicatorInstance.GetComponent<TextMeshPro>();

        indicatorText.text = interactAction[0].ToString().ToUpper() + interactAction.Substring(1);
    }

    public void destroyIndicator()
    {
        if (indicatorInstance != null)
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_ActivateAbility()
    {
        StartCoroutine(_SkullAbility());
    }

    IEnumerator _SkullAbility()
    {
        oceanRockCollider.isTrigger = true;

        abilitySound.Play();
        skullAbilityActive = true;

        abilityPs.Play();

        foreach (SpriteRenderer sprite in sprites)
        {
            sprite.DOColor(new Color(0.5f, 0.5f, 0.5f, 0.4f), 0.5f);
        }

        yield return new WaitForSeconds(1.5f);

        Collider2D hazardCheck = HazardCheck();

        float failsafe = 0;

        while (hazardCheck && !grabbedByHand)
        {
            hazardCheck = HazardCheck();
            failsafe += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();

            if (failsafe > 3) break;
        }

        abilityPs.Stop();

        foreach (SpriteRenderer sprite in sprites)
        {
            sprite.DOColor(new Color(1, 1, 1, 1), 0.3f);
        }

        for (int i = 0; i < inside.Length; i++)
        {
            inside[i] = false;
        }

        abilityCooldownTime = abilityCooldown;
        skullAbilityActive = false;

        yield return new WaitForSeconds(0.4f);

        oceanRockCollider.isTrigger = false;


    }

    Collider2D HazardCheck()
    {
        return Physics2D.OverlapBox((Vector2)oceanRockCollider.bounds.center, oceanRockCollider.bounds.size, 0, oceanRockLayer);
    }

    public string EntityDataToString()
    {
        string data = "";
        for (int i = 0; i < tileSlotTiles.Length; i++)
        {
            if (tileSlotTiles[i] == null)
            {
                data += "; ";
                continue;
            }
            for (int j = 0; j < tileSlotTiles[i].tiles.Length; j++)
            {
                if (!tileSlotTiles[i].tiles[j])
                {
                    data += "null:" + i + ":; ";
                    continue;
                }

                data += tileSlotTiles[i].tiles[j].Item.itemId + ":" + i + ":";
                data += tileSlotTiles[i].tiles[j].TileEntityDataToString();
                data += "; ";
            }
        }

        switch (BoatType)
        {

            case boatType.battleship:
                data += "~";

                if (fuelItem)
                    data += fuelItem.itemId + ", ";
                else data += "null, ";

                data += fuelCount + ", ";
                data += fuelTime + ", ";

                break;
            case boatType.speedboat:
                data = "";

                if (fuelItem)
                    data += fuelItem.itemId + ", ";
                else data += "null, ";

                data += fuelCount + ", ";
                data += fuelTime + ", ";

                break;

        }

        return data;
    }

    public void StringToEntityData(string data)
    {
        if (data == null || data == "") return;

        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            string[] tileData = data.Split("; ");
            for (int i = 0; i < tileData.Length; i++)
            {
                string[] data2 = tileData[i].Split(":");
                if (data2[0] != "null" && data2.Length == 3)
                {
                    gameManager.instance.RPC_CreateBoatTile(DataPersistanceManager.userId, data2[0], true, data2[2], view.Id, int.Parse(data2[1]));
                }
            }

        }

        switch (BoatType)
        {

            case boatType.battleship:
                string[] dataSplit = data.Split("~");
                string[] dataFuel = dataSplit[1].Split(", ");

                fuelItem = gameManager.instance.itemDictionary[dataFuel[0]];
                if (fuelItem)
                    maxFuelTime = fuelItem.fuelSmeltTime;
                fuelCount = short.Parse(dataFuel[1]);
                fuelTime = float.Parse(dataFuel[2]);

                break;
            case boatType.speedboat:
                string[] data2 = data.Split(", ");

                fuelItem = gameManager.instance.itemDictionary[data2[0]];
                if (fuelItem)
                    maxFuelTime = fuelItem.fuelSmeltTime;
                fuelCount = short.Parse(data2[1]);
                fuelTime = float.Parse(data2[2]);
                break;
            default: break;

        }

    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_FixPosition(float x, float y)
    {
        transform.position = new Vector3(x, y, 0);
    }

    void FinishEnterCooldown()
    {
        enterCooldown = false;
    }

}
[Serializable]
public class BoatTileSlot
{
    public placedTile[] tiles = new placedTile[2];

}
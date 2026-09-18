
using DG.Tweening;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class player : NetworkBehaviour
{
    public float positionLerpSpeed = 5;

    public static player instance;
    public int skinIndex;
    float currentSpeed;
    public float speed;
    public float waterSpeed;
    public float sprintSpeed;
    public float swimSpeed;
    public bool sprinting;
    public float sprintStaminaRate;
    public float normalCamSize;
    public float sprintCamSize;
    public float camSizeMultiplier = 1;

    bool chargingHyperdash;

    public Vector3 axisInput;
    public Rigidbody2D rb;
    public Animator anim;
    public SpriteRenderer sprite;
    public SpriteRenderer heldItemSprite;

    public NetworkObject view;
    public TextMeshPro usernameText;
    public Collider2D collision;
    public Collider2D trigger;

    public Boat currentBoat;
    public bool pilotingBoat;
    public bool inBoatAltAction;
    public int currentAltAction;
    public int boatFloor;
    public Unity.Cinemachine.CinemachineVirtualCamera cam;
    public Vector3 boatVelocity;
    bool colliderActive;

    public Island currentIsland;
    Island previousIsland;
    public playerHealth health;

    int currentSpriteNum;
    public SpriteRenderer headArmorSprite, chestArmorSprite, legArmorSprite;
    public Material defaultArmorMat;

    public Light2D nightLight;
    //float nightLightIntensity;

    item headItem, chestItem, legsItem;

    Vector2 lastPos;
    public SpriteMask mask;


    public float networkTeleportDist;

    double receiveTime;
    public double clientTimeDif;
    float lag;

    public bool underVolcanoRoof;
    private bool roofFaded;
    Coroutine roofTweenCoRo;

    public float strengthMult = 1;
    public float speedMult = 1;

    public float hardModeLightMult = 0.7f;

    public float defaultLightRadius;
    public Light2D playerLight;
    public float caveLightRadius = 9;
    public AudioSource lifestealSound;
    public float iceSpeedMult = 1;


    [SerializeField] Transform locatorArrow;
    Vector2 locatorTarget;
    [SerializeField] AudioSource oreLocatorSound, hyperStartSound;
    public bool dashing, hyperdashing;
    [SerializeField] ParticleSystem dashEffect, waterWalkPs, guardPs;
    [SerializeField] AudioSource dashSound, waterWalkSound, guardSound;
    public Transform spriteHolder;
    public damagePlayer divineGuardDamage;
    public Collider2D divineGuardHitbox;
    [SerializeField] AudioClip guardStartClip, guardStopClip, strideStartClip, strideStopClip;
    [SerializeField] AudioSource strideCancelSound;
    public Transform camFollow;
    public bool inCutscene;
    public EnemyHand beingGrabbedBy;
    public bool onGooPuddle;
    [SerializeField] Transform bossParticlesTransform;
    [SerializeField] ParticleSystem bossPs;
    int previousIslandBiome;

    public handEvents HandEvents;
    [SerializeField] PlayerEvents playerEvents;

    Vector2 dashInput;

    //for respawning
    bool cueMusicRestart = false;

    float walkAnimSpeed, walkAnimDefaultSpeed;

    public Animator fireCircle, fireCircle2;
    public bool fireCircleEquipped;
    public bool fireCircleEnabled;
    [SerializeField] GameObject sporeExplosion, slothVortex;
    bool exploding;
    public bool countering;
    public SpriteRenderer counterSprite;
    [SerializeField] AudioSource explodeStartSound, counterSound;
    public GameObject fireExpl;
    public GameObject fireExplMini, fahrenheitExpl;

    public SortingGroup sortingGroup;
    public float prideTimer;
    bool prideEffectActive;
    [SerializeField] SpriteRenderer prideSprite;
    public const float prideMult = 1.2f;
    float waterWalkTimer = 0;

    [Header("Vale")]
    public Material valeMat;
    Material defaultMat;
    bool inVale;
    public bool loadingFeatures;
    private bool onIce;
    bool shownValeText;

    [Header("Network")]
    public bool disablePositionSync;
    private Coroutine counterCo;

    [Networked] public bool onIsland { get; set; }
    [Networked] public bool duelGhostSpectator { get; set; }
    [Networked] public float objSpeed { get; set; }
    [Networked] bool net_SpriteFlip { get; set; }
    [Networked] Vector2 networkedPosition { get; set; }
    [Networked] bool moving { get; set; }
    [Networked] int net_islandIndex { get; set; }
    [Networked] int net_currentHealth { get; set; }
    [Networked] int net_maxHealth { get; set; }

    [Networked] NetworkId net_boatParent { get; set; }
    [Networked] bool net_lostSoul { get; set; }
    [Networked] public int inDungeon { get; set; }
    [Networked] public bool prideActive { get; set; }

    private void Start()
    {
        walkAnimDefaultSpeed = 0.7f;
        walkAnimSpeed = walkAnimDefaultSpeed;

        sortingGroup = GetComponentInChildren<SortingGroup>();

        defaultArmorMat = sprite.material;
        defaultMat = sprite.material;
        defaultLightRadius = playerLight.pointLightOuterRadius;

        usernameText.text = DataPersistanceManager.instance.usernameMap[view.StateAuthority];
        //text color is set in playerspawner

        fireCircle.gameObject.SetActive(false);
        fireCircle2.gameObject.SetActive(false);
        fireCircleEnabled = true;
        fireCircle2.GetComponent<damagePlayer>().ignorePlayers = view.HasStateAuthority;


        if (HasStateAuthority)
        {
            instance = this;
            cam = FindAnyObjectByType<Unity.Cinemachine.CinemachineVirtualCamera>();
            cam.Follow = camFollow;
            cam.ForceCameraPosition(new Vector3(transform.position.x, transform.position.y, cam.transform.position.z), Quaternion.identity);
            normalCamSize = cam.m_Lens.OrthographicSize;

            gameObject.tag = "Main Player";
            gameObject.layer = 8;


            if (DataPersistanceManager.instance.loadingSave && gameManager.instance.savedCurrentIsland < MapGenerator.instance.islands.Count())
            {
                currentIsland = MapGenerator.instance.islands[gameManager.instance.savedCurrentIsland];
            }
            previousIslandBiome = currentIsland.biome;

            InvokeRepeating("CheckIsland", 0.1f, 0.5f);

            if (playerSpawner.instance.savedDisableMarker)
            {
                Chat.Instance.RPC_ToggleMarker(DataPersistanceManager.LocalActorIndex, true);
            }

            if (playerSpawner.instance.savedPyroDisabled && inventory.instance.chestplateSlot.itemInSlot
                && inventory.instance.chestplateSlot.itemInSlot.uniqueType == item.UniqueType.fire && currentIsland.biome != (int)MapGenerator.biome.vale)
            {
                RPC_EnableFireCircle(true, false, inventory.instance.chestplateSlot.itemData.tier >= 4);
            }
        }
        else
        {
            gameObject.tag = "Player";
            gameObject.layer = 3;
        }

        GetComponentInChildren<HandMove>().slothCircle.color -= new Color(0, 0, 0, 1);
        currentBoat = null;

        dashInput = Vector2.right;
        inDungeon = playerSpawner.instance.savedDungeon;

        if (gameManager.instance.difficulty > 0)
        {
            defaultLightRadius *= hardModeLightMult;
            caveLightRadius *= hardModeLightMult;
        }
    }

    public void RPC_EnablePosSync(bool enable)
    {
        disablePositionSync = !enable;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }


    void FixedUpdate()
    {
        //fix for tile preview when hand is inactive
        if (HandMove.instance && (!HandMove.instance.gameObject.activeSelf || !HandMove.instance.selectedItem || (HandMove.instance.selectedItem.type != item.itemType.block && HandMove.instance.selectedItem.type != item.itemType.bucketF && HandMove.instance.selectedItem.type != item.itemType.bucketE)))
            HandMove.instance.tilePreview.position = Camera.main.ScreenToWorldPoint(InputManager.instance.mousePos);

        if (transform.parent)
            objSpeed = ((Vector2)transform.parent.InverseTransformPoint(rb.position) - lastPos).magnitude / Time.fixedDeltaTime;

        transform.localScale = Vector3.one;

        if (currentBoat && transform.parent != currentBoat.transform)
        {
            transform.SetParent(currentBoat.transform);
        }

        if (handEvents.instance && !handEvents.instance.grappling)
        {
            if (view.StateAuthority == SingletonRunner.runner.LocalPlayer && !health.dead)
            {
                if (playerEvents.currentSurface != null)
                    onIce = playerEvents.currentSurface.ice;

                if (currentIsland.biome == (int)MapGenerator.biome.volcanic || inDungeon > -1)
                {
                    //under roof (lighting)
                    underVolcanoRoof = MapDisplay.Instance.roofTilemap.GetTile(Vector3Int.RoundToInt(transform.position));

                    bool shouldFadeRoof =
                    CircleHasTile(
                    MapDisplay.Instance.roofTilemap,
                    Vector2Int.RoundToInt(transform.position),
                    3)
                    || inDungeon == 4;

                    if (shouldFadeRoof != roofFaded)
                    {
                        roofFaded = shouldFadeRoof;

                        if (roofTweenCoRo != null)
                            StopCoroutine(roofTweenCoRo);

                        float targetAlpha = roofFaded ? 0.45f : 1f;

                        roofTweenCoRo = StartCoroutine(
                            _TweenTilemapAlpha(
                                1,
                                targetAlpha,
                                MapDisplay.Instance.roofTilemap
                            )
                        );
                    }

                }
                else
                {
                    underVolcanoRoof = false;
                }

                if (!inCutscene && !beingGrabbedBy && !exploding && !countering)
                {
                    if (!dashing)
                    {
                        if (playerWater.instance.waterValue != 2)
                        {
                            if (sprinting) InputManager.instance.movementInput.Normalize();

                            //default
                            if (!onIce)
                            {
                                axisInput = ClampMinMagnitude(InputManager.instance.movementInput, 0.25f);

                                if (axisInput.magnitude < 0.08f)
                                {
                                    axisInput = Vector2.zero;
                                }

                            }
                            //ice
                            else
                            {
                                float slipAmount = 3.8f;
                                if (inventory.instance.legsSlot.itemInSlot && inventory.instance.legsSlot.itemInSlot.uniqueType == item.UniqueType.frost
                                    && inventory.instance.legsSlot.itemData.tier >= 4)
                                    slipAmount = 15f;

                                axisInput = Vector2.Lerp(axisInput, InputManager.instance.movementInput, Time.deltaTime * slipAmount);
                                if (InputManager.instance.movementInput == Vector2.zero)
                                {
                                    axisInput = Vector2.Lerp(axisInput, InputManager.instance.movementInput, Time.deltaTime * slipAmount / 3f);
                                    if (axisInput.magnitude < 0.05f) axisInput = Vector2.zero;
                                }
                            }

                        }
                        //deep water
                        else
                        {
                            axisInput = Vector2.Lerp(axisInput, InputManager.instance.movementInput, Time.deltaTime * 4);
                            if (InputManager.instance.movementInput == Vector2.zero)
                            {
                                axisInput = Vector2.Lerp(axisInput, InputManager.instance.movementInput, Time.deltaTime * 2);
                                if (axisInput.magnitude < 0.05f) axisInput = Vector2.zero;
                            }

                        }

                        if (axisInput != Vector3.zero)
                            dashInput = axisInput;
                    }
                    if (axisInput.magnitude > 1)
                    {
                        axisInput.Normalize();
                    }
                    if (dashInput.magnitude > 1)
                    {
                        dashInput.Normalize();
                    }

                    float toolSpeedCamMult = (1 + (inventory.instance.GetModifierInToolSlot("speed")
                        * inventory.instance.GetModifierInArmorSlots("speed"))) / 2;

                    if (waterWalkTimer > 0)
                        toolSpeedCamMult *= 1.12f;

                    if ((axisInput != Vector3.zero || dashing || hyperdashing) &&
                    !inventory.instance.inventoryOpen &&
                    !playerHealth.instance.beingKnocked &&
                    !pilotingBoat &&
                    !inBoatAltAction &&
                    !playerWater.instance.drowning &&
                    !pauseScreen.instance.paused &&
                    !Chat.Instance.chatOpen &&
                    !gameManager.instance.writingSign &&
                    !HandMove.instance.recalling
                    )
                    {
                        moving = true;

                        anim.SetBool("Walking", (objSpeed > 0.05f && InputManager.instance.movementInput.magnitude > 0) || hyperdashing);

                        if (currentBoat && currentBoat.skullAbilityActive) anim.SetBool("Walking", false);

                        if (playerWater.instance.waterValue == 0)
                        {
                            //sprint
                            if ((InputManager.actions["Sprint"].held || dashing || hyperdashing) && !playerWater.instance.outOfStamina && !HandMove.instance.eating && objSpeed != 0 && !chargingHyperdash
                                && !(HandMove.instance.animator.GetBool("Shielding") && !hyperdashing) && !HandEvents.grappling)
                            {
                                currentSpeed = sprintSpeed * (1 + (inventory.instance.GetStatAmount(1) / 10f * inventory.instance.maxSpeedMult));
                                playerWater.instance.usingStamina = true;

                                if (!hyperdashing)
                                    playerWater.instance.stamina -= sprintStaminaRate * Time.deltaTime
                                        * (1 / DungeonGenerator.instance.GetModifier("stamina"));
                                else
                                    playerWater.instance.stamina -= sprintStaminaRate * Time.deltaTime * 1.6f
                                        * (1 / DungeonGenerator.instance.GetModifier("stamina"));

                                if (!hyperdashing)
                                {
                                    cam.m_Lens.OrthographicSize = Mathf.Lerp(cam.m_Lens.OrthographicSize, sprintCamSize * camSizeMultiplier * toolSpeedCamMult, Time.deltaTime * 8);
                                    if (!sprinting)
                                    {
                                        sprinting = true;
                                        walkAnimSpeed = 1.18f;
                                    }
                                }
                                //hyperdash cam size
                                else
                                {
                                    sprinting = true;
                                    cam.m_Lens.OrthographicSize = Mathf.Lerp(cam.m_Lens.OrthographicSize, sprintCamSize * camSizeMultiplier * 1.15f * toolSpeedCamMult, Time.deltaTime * 8);
                                }
                            }
                            else
                            {
                                currentSpeed = speed * (1 + (inventory.instance.GetStatAmount(1) / 10f * inventory.instance.maxSpeedMult * 0.5f));
                                StopSprint();

                                cam.m_Lens.OrthographicSize = Mathf.Lerp(cam.m_Lens.OrthographicSize, normalCamSize * camSizeMultiplier * toolSpeedCamMult, Time.deltaTime * 3);
                            }
                        }
                        else
                        {
                            StopSprint();

                            cam.m_Lens.OrthographicSize = Mathf.Lerp(cam.m_Lens.OrthographicSize, normalCamSize * camSizeMultiplier * toolSpeedCamMult, Time.deltaTime * 3);

                            if (playerWater.instance.waterValue == 1)
                            {
                                currentSpeed = waterSpeed * (1 + (inventory.instance.GetStatAmount(1) / 10f * inventory.instance.maxSpeedMult * 0.4f));
                            }
                            else
                            {
                                currentSpeed = swimSpeed * (1 + (inventory.instance.GetStatAmount(1) / 10f * inventory.instance.maxSpeedMult * 0.8f));
                                if (axisInput.x > 0)
                                {
                                    spriteHolder.localScale = new Vector2(1, 1);
                                }
                                else if (axisInput.x < 0)
                                {
                                    spriteHolder.localScale = new Vector2(-1, 1);
                                }
                            }

                        }
                        if (currentBoat) rb.bodyType = RigidbodyType2D.Dynamic;
                        collision.isTrigger = rb.bodyType == RigidbodyType2D.Kinematic;

                        if (hyperdashing)
                        {
                            currentSpeed *= 1.9f;
                            walkAnimSpeed = 1.7f;
                        }
                        else if (dashing)
                        {
                            currentSpeed *= 2.1f;
                        }

                        Move(currentSpeed);
                    }
                    else
                    {
                        anim.SetBool("Walking", false);
                        StopSprint();
                        moving = false;

                        if (!pilotingBoat && !inBoatAltAction) cam.m_Lens.OrthographicSize = Mathf.Lerp(cam.m_Lens.OrthographicSize, normalCamSize * camSizeMultiplier * toolSpeedCamMult, Time.deltaTime * 3);

                    }

                    if (health.beingKnocked)
                    {
                        KnockbackMotion();
                    }


                    if (currentBoat)
                    {
                        rb.bodyType = (!moving && !health.beingKnocked) ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;

                        collision.isTrigger = rb.bodyType == RigidbodyType2D.Kinematic;

                        if (currentBoat.skullAbilityActive) collision.isTrigger = true;

                    }
                    else
                    {
                        boatVelocity = Vector2.zero;
                        rb.bodyType = RigidbodyType2D.Dynamic;
                        collision.isTrigger = false;
                    }

                }
                //cutscene
                else
                {
                    cam.m_Lens.OrthographicSize = Mathf.Lerp(cam.m_Lens.OrthographicSize, normalCamSize * camSizeMultiplier, Time.deltaTime * 3);
                    anim.SetBool("Walking", false);
                }

                if ((gameManager.inBoss || gameManager.inSecondPhase) && !inCutscene)
                {
                    if (!gameManager.inSecondPhase)
                    {
                        if (Boss.instance.inRange)
                        {
                            camFollow.localPosition = transform.InverseTransformPoint((transform.position * 4 + Boss.instance.transform.position) / 5);
                        }
                        else
                        {
                            camFollow.localPosition = Vector2.Lerp(camFollow.localPosition, Vector2.zero, 4 * Time.deltaTime);

                            if ((Vector2)camFollow.localPosition != Vector2.zero && Vector2.Distance(camFollow.localPosition, Vector2.zero) <= 0.05)
                            {
                                camFollow.localPosition = Vector2.zero;
                            }
                        }
                    }
                    else if (!Boss.instance.heartDead)
                    {
                        camFollow.localPosition = transform.InverseTransformPoint((transform.position * 4 + Boss.instance.heartHealth.transform.position) / 5);
                    }
                }

                if (!dashing)
                {
                    anim.SetFloat("Walk Speed", ((walkAnimSpeed / 2) + (walkAnimSpeed / 2 * axisInput.magnitude)) * iceSpeedMult * 0.9f);
                }
                else
                {
                    anim.SetFloat("Walk Speed", walkAnimSpeed);
                }

            }
            else
            {
                collision.enabled = true;
                rb.bodyType = RigidbodyType2D.Kinematic;
                collision.isTrigger = true;

            }
        }

        if (transform.parent)
        {
            usernameText.transform.localScale = transform.parent.localScale;

            lastPos = (Vector2)transform.parent.InverseTransformPoint(rb.position);
        }
    }
    public bool InVale()
    {
        return currentIsland.biome == (int)MapGenerator.biome.vale && onIsland;
    }

    public void CheckIsland()
    {
        if (gameManager.inBoss && !Boss.instance.dead) return;

        Transform followingPlayer = transform;

        if (menu.instance.spectatingPlayer)
        {
            followingPlayer = menu.instance.spectatingPlayer.transform;
        }

        if (gameManager.instance.inArena)
        {
            menu.instance.mapTransform.gameObject.SetActive(false);
        }

        //check which island
        bool isOnIsland = false;
        foreach (Island i in MapGenerator.instance.islands)
        {
            if (followingPlayer.position.x < (i.center.x + i.size / 2) && followingPlayer.position.x > (i.center.x - i.size / 2)
            && followingPlayer.position.y < (i.center.y + i.size / 2) && transform.position.y > (i.center.y - i.size / 2))
            {
                currentIsland = i;
                isOnIsland = true;
            }



            //feature distance is larger than the regular island bounds
            Island currentIslandFeatures = null;
            bool withinFeatureLoadDist = false;
            int additionalBoundSize = 50;

            if (followingPlayer.position.x < (i.center.x + i.size / 2 + additionalBoundSize)
                && followingPlayer.position.x > (i.center.x - i.size / 2 - additionalBoundSize)
            && followingPlayer.position.y < (i.center.y + i.size / 2 + additionalBoundSize)
            && transform.position.y > (i.center.y - i.size / 2 - additionalBoundSize))
            {
                currentIslandFeatures = i;
                withinFeatureLoadDist = true;
            }

            //island features load
            if (treeRPCs.Instance.islandFeatures[i.index].loaded != (currentIslandFeatures == i && withinFeatureLoadDist))
            {
                treeRPCs.Instance.islandFeatures[i.index].loaded = (currentIslandFeatures == i && withinFeatureLoadDist);

                LoadFeatures(i.index, currentIslandFeatures == i && withinFeatureLoadDist);
            }
        }

        onIsland = isOnIsland;

        // make music restart when respawn on same island
        if (playerHealth.instance.dead) cueMusicRestart = true;

        if (currentIsland.biome != (int)MapGenerator.biome.vale || !onIsland)
        {
            //new biomes
            if (isOnIsland && currentIsland.biome != 0 && NewBiome.Instance.biomeData[currentIsland.biome - 1].visited == false && playerWater.instance.waterValue == 0 && !currentBoat)
            {
                NewBiome.Instance.VisitBiome(currentIsland.biome - 1);
                NewBiome.Instance.biomeData[currentIsland.biome - 1].visited = true;
            }

            if (inVale)
            {
                sprite.material = defaultMat;
                HandEvents.handMove.sprite.material = defaultMat;

                menu.instance.mapGroup.alpha = menu.instance.mapAlpha;
                inventory.instance.gameObject.SetActive(true);

                nightLight.intensity = 1;

                inVale = false;
                ValeManager.instance.HideValeText();
                
                if (!isOnIsland) currentIsland = MapGenerator.instance.islands[0];
            }


        }
        //vale stuff
        else
        {
            //execute upon entering vale
            if (!inVale)
            {
                menu.instance.mapGroup.alpha = 0;
                inventory.instance.gameObject.SetActive(false);
                inventory.charmEffect = "";
                heldItemSprite.sprite = null;
                HandEvents.handMove.twoHandSprite.gameObject.SetActive(false);

                nightLight.intensity = 1.6f;

                inVale = true;

                if (!ValeManager.instance.lostSoul)
                    ValeManager.instance.SoulTextShow(false);

            }

            if (currentBoat && !ValeManager.instance.escapingVale)
            {
                currentBoat.RPC_PlayerRideBoat(view.Id, false, true);
                currentBoat = null;
            }

        }

        //disable or enable music when leaving/entering an island
        if (!onIsland && gameManager.instance.currentTrack != "none" && !currentBoat && inDungeon == -1)
        {
            gameManager.instance.currentTrack = "none";
            gameManager.instance.musicSource.DOFade(0, 6);
            StartCoroutine(gameManager.instance._SwitchAmbience());
        }

        //exit boat when its too far
        /*
        if (currentBoat)
        {
            previousIslandBiome = currentIsland.biome;

            if (Vector2.Distance(transform.position, currentBoat.transform.position) > 30)
            {
                currentBoat.RPC_PlayerRideBoat(view.Id, false, true);
                currentBoat = null;
            }

        }
        */

        //switch music when changing islands/respawning
        if (onIsland && !inCutscene && ((gameManager.instance.currentTrack == "none" && gameManager.instance.musicSource.volume == 0)
            || (gameManager.instance.currentTrack != "none" && currentIsland.biome != previousIslandBiome)
            || cueMusicRestart) && (!(currentBoat && currentBoat.playingMusic) || previousIslandBiome == (int)MapGenerator.biome.vale)
            && !playerHealth.instance.dead)
        {
            if (!TimeManager.instance.isNight)
            {
                StartCoroutine(gameManager.instance._SwitchMusic("day", 0));
            }
            else
            {
                StartCoroutine(gameManager.instance._SwitchMusic("night", 0));
            }
            previousIslandBiome = currentIsland.biome;
            cueMusicRestart = false;
        }

        EnemySpawner.instance.activeBiomes.Clear();
        if (playerSpawner.instance.players != null)
        {
            foreach (player p in playerSpawner.instance.players)
            {
                if (EnemySpawner.instance && p)
                    EnemySpawner.instance.activeBiomes.Add((MapGenerator.biome)p.currentIsland.biome);
            }

        }

    }



    async void LoadFeatures(int island, bool enable)
    {
        if (enable) loadingFeatures = true;

        GameObject[] shrubs = treeRPCs.Instance.islandFeatures[island].shrubs.ToArray();
        treeChop[] treeChops = treeRPCs.Instance.islandFeatures[island].treeChops.ToArray();
        itemPickup[] items = treeRPCs.Instance.islandFeatures[island].items.ToArray();
        int frameCounter = 0;

        foreach (treeChop tree in treeChops)
        {
            if (tree)
            {
                tree.gameObject.SetActive(enable);

                //for ores
                if (!tree.hasStump && tree.currentHp <= 0)
                {
                    tree.gameObject.SetActive(false);
                }
                //for tree stumps
                else if (tree.hasStump && tree.currentHp <= 0 && tree.stumpHp <= 0)
                {
                    tree.gameObject.SetActive(false);
                }

                frameCounter++;
                if (frameCounter % 300 == 0)
                {
                    await Task.Yield();
                }
            }
        }
        foreach (GameObject shrub in shrubs)
        {
            if (!shrub) continue;

            shrub.SetActive(enable);

            frameCounter++;
            if (frameCounter % 300 == 0)
            {
                await Task.Yield();
            }

        }
        foreach (itemPickup item in items)
        {
            if (!item) continue;

            item.gameObject.SetActive(enable);

            if (item.transform.parent != featurePlacer.Instance.itemHolder)
                item.transform.parent.gameObject.SetActive(enable);

            frameCounter++;
            if (frameCounter % 300 == 0)
            {
                await Task.Yield();
            }

        }

        await Task.Yield();

        if (enable) loadingFeatures = false;
    }

    Vector2 ClampMinMagnitude(Vector2 v, float min)
    {
        float mag = v.magnitude;

        if (mag < min && mag > 0f)
            return v * (min / mag);

        return v;
    }

    async void ValeTextShow()
    {
        await Task.Delay(4000);

        if (currentIsland.biome != (int)MapGenerator.biome.vale) return;

        menu.instance.valeText.DOFade(1, 1);
        menu.instance.valeText.transform.localScale = Vector3.one;

        menu.instance.valeText.transform.DOScale(1.2f, 11);

        await Task.Delay(9000);

        menu.instance.valeText.DOFade(0, 2);
        menu.instance.followLightText.text = "Follow The Light";
        await Task.Delay(2000);

        if (currentIsland.biome != (int)MapGenerator.biome.vale)
            return;

        menu.instance.valeText2.DOFade(1, 3);
    }


    private void Update()
    {
        if (transform.parent == null || !MapDisplay.finishedLoading) return;

        if (HandEvents.handMove.wagerText.transform.lossyScale.x < 0)
            HandEvents.handMove.wagerText.transform.localScale = new Vector3(HandEvents.handMove.wagerText.transform.localScale.x * -1
                , HandEvents.handMove.wagerText.transform.localScale.y);

        //net stuff
        if (!view.HasStateAuthority)
        {
            if (net_SpriteFlip)
            {
                spriteHolder.localScale = new Vector2(-1, 1);
            }
            else
            {
                spriteHolder.localScale = new Vector2(1, 1);
            }

            currentIsland = MapGenerator.instance.islands[net_islandIndex];

            health.currentHealth = net_currentHealth;
            health.maxHealth = net_maxHealth;

            if (net_boatParent != new NetworkId() && !currentBoat)
            {
                transform.SetParent(Runner.FindObject(net_boatParent).transform, true);

                transform.parent.GetComponent<Boat>().PlayerRideBoat(view.Id, true, true);
            }
            if (net_boatParent == new NetworkId() && currentBoat)
            {
                currentBoat.PlayerRideBoat(view.Id, false, true);

                transform.SetParent(playerSpawner.instance.transform, true);
            }

        }
        else
        {
            net_islandIndex = currentIsland.index;
            net_currentHealth = health.currentHealth;
            net_maxHealth = health.maxHealth;
            net_SpriteFlip = spriteHolder.localScale.x < 0;
            if (ValeManager.instance)
                net_lostSoul = ValeManager.instance.lostSoul;

            if (currentBoat) net_boatParent = currentBoat.view.Id;
            else net_boatParent = new NetworkId();

            if (gameManager.instance.inArena && inventory.instance.GetItemCounts(true).Count == 0)
            {
                duelGhostSpectator = true;
            }
            else
            {
                duelGhostSpectator = false;
            }

            if (inventory.charmEffect == "pride")
            {
                if (prideTimer < 12)
                {
                    prideTimer += Time.deltaTime;
                    prideActive = false;
                }
                else
                {
                    prideActive = true;
                }
            }
            else
            {
                prideTimer = 0;
                prideActive = false;
            }

        }

        if (prideActive)
        {
            if (!prideEffectActive)
            {
                prideEffectActive = true;
                prideSprite.DOFade(1, 1);
            }
        }
        else
        {
            if (prideEffectActive)
            {
                prideEffectActive = false;
                prideSprite.DOFade(0, 0.5f);
            }
        }


        if (!net_lostSoul) shownValeText = false;

        //lost soul grayscale
        if (net_lostSoul)
        {
            usernameText.gameObject.SetActive(false);
            sprite.material = valeMat;
            HandEvents.handMove.sprite.material = valeMat;

            if (view.HasStateAuthority && !shownValeText && !health.dead && !menu.instance.spectatingPlayer)
            {
                if (MapDisplay.finishedLoading)
                    ValeTextShow();
                shownValeText = true;
            }

        }
        else if (duelGhostSpectator)
        {
            sprite.material = valeMat;
            HandEvents.handMove.sprite.material = valeMat;
            if (!health.dead)
            {
                sprite.color = new Color(1, 1, 1, 0.4f);
                HandEvents.handMove.sprite.color = sprite.color;
            }
        }
        else
        {
            if (sprite.color == new Color(1, 1, 1, 0.4f))
            {
                sprite.color = Color.white;
                HandEvents.handMove.sprite.color = Color.white;
            }

            usernameText.gameObject.SetActive(true);
            if (sprite.material != defaultMat)
            {
                sprite.material = defaultMat;
                HandEvents.handMove.sprite.material = defaultMat;
            }
        }



        currentSpriteNum = int.Parse(sprite.sprite.name.Split('_')[1]);
        SetArmorSprites();

        health.damageFlashSprite.sprite = sprite.sprite;
        counterSprite.sprite = sprite.sprite;

        mask.enabled = !currentBoat;

        mask.sprite = sprite.sprite;
        mask.transform.position = sprite.transform.position;
        mask.transform.rotation = sprite.transform.rotation;
        if (spriteHolder.transform.localScale.x == -1) mask.transform.localScale = new Vector2(-1, 1);
        else mask.transform.localScale = new Vector2(1, 1);

        if (currentBoat == null) boatFloor = -1;

        if (view.HasStateAuthority)
        {
            bool updateArmor = false;

            if (headItem != inventory.instance.headSlot.itemInSlot)
            {
                headItem = inventory.instance.headSlot.itemInSlot;
                updateArmor = true;
            }

            if (chestItem != inventory.instance.chestplateSlot.itemInSlot)
            {
                chestItem = inventory.instance.chestplateSlot.itemInSlot;
                updateArmor = true;
            }

            if (legsItem != inventory.instance.legsSlot.itemInSlot)
            {
                legsItem = inventory.instance.legsSlot.itemInSlot;
                updateArmor = true;
            }

            if (updateArmor)
            {
                string headItemId = "null";
                if (headItem) headItemId = headItem.itemId;
                string chestItemId = "null";
                if (chestItem) chestItemId = chestItem.itemId;
                string legsItemId = "null";
                if (legsItem) legsItemId = legsItem.itemId;

                RPC_UpdateArmor(headItemId, chestItemId, legsItemId);
            }

            if (chestItem && chestItem.uniqueType == item.UniqueType.fire && currentIsland.biome != (int)MapGenerator.biome.vale)
            {
                if (!fireCircleEquipped)
                {
                    fireCircleEquipped = true;
                    //view.RPC("RPC_EnableFireCircle", RpcTarget.AllViaServer, true, true);
                    RPC_EnableFireCircle(true, true, inventory.instance.chestplateSlot.itemData.tier >= 4);
                }

                if (Input.GetKeyDown(KeyCode.C) &&
                    !pauseScreen.instance.paused && !Chat.Instance.chatOpen && !gameManager.instance.writingSign && !inventory.instance.inventoryOpen)
                {
                    fireCircleEnabled = !fireCircleEnabled;
                    //view.RPC("RPC_EnableFireCircle", RpcTarget.AllViaServer, true, fireCircleEnabled);
                    RPC_EnableFireCircle(true, fireCircleEnabled, inventory.instance.chestplateSlot.itemData.tier >= 4);
                }

            }
            else
            {
                if (fireCircleEquipped)
                {
                    fireCircleEquipped = false;
                    //view.RPC("RPC_EnableFireCircle", RpcTarget.AllViaServer, false, false);
                    RPC_EnableFireCircle(false, false, false);
                }
            }

            networkedPosition = transform.localPosition;
            if (!currentBoat)
            {
                networkedPosition += (Vector2)(0.2f * objSpeed * axisInput * transform.parent.localScale.x);
            }

            if (waterWalkTimer > 0)
                waterWalkTimer -= Time.deltaTime;

        }
        //sync position
        else if (transform.localPosition != (Vector3)networkedPosition)
        {
            if ((Vector2.Distance(transform.localPosition, networkedPosition) < networkTeleportDist || HandEvents.grappling)
                && (Vector2)transform.localPosition != Vector2.zero)
            {
                transform.localPosition = Vector2.Lerp(transform.localPosition, networkedPosition, Time.deltaTime * positionLerpSpeed);
            }
            //teleport when far
            else
            {
                transform.localPosition = networkedPosition;
            }
        }


        if (gameManager.inBoss && !Boss.instance.inRange && (view.StateAuthority == SingletonRunner.runner.LocalPlayer || menu.instance.spectatingPlayer == this)
            && !gameManager.inSecondPhase && !health.dead && !Boss.instance.heartDead)
        {
            if (!bossPs.isPlaying)
                bossPs.Play();

            Vector2 direction = new Vector3(Boss.instance.transform.position.x - transform.position.x, Boss.instance.transform.position.y - transform.position.y).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            bossParticlesTransform.eulerAngles = new Vector3(0, 0, angle);

        }
        else
        {
            if (bossPs.isPlaying)
                bossPs.Stop();
        }

        SetArmorSprites();
    }

    public void BoatRespawn()
    {
        transform.position = TileEntity.currentSpawnPoint.transform.position;
        TileEntity.currentSpawnPoint.boatParent.PlayerRideBoat(view.Id, true, true);
        transform.position = TileEntity.currentSpawnPoint.transform.position;
    }

    void SetArmorSprites()
    {
        bool inVale = currentIsland.biome == (int)MapGenerator.biome.vale;

        if (headItem && !inVale)
        {
            headArmorSprite.sprite = headItem.armorSpriteSheet[skinIndex].armorSprites[currentSpriteNum];
            //headArmorSprite.flipX = spriteHolder.transform.localScale.x == -1;
            headArmorSprite.color = sprite.color;

            if (headItem.armorSpriteSheet[skinIndex].materialOverride != null)
            {
                headArmorSprite.material = headItem.armorSpriteSheet[skinIndex].materialOverride;
            }
            else
            {
                headArmorSprite.material = defaultArmorMat;
            }

        }
        else
        {
            headArmorSprite.sprite = null;
        }

        if (chestItem && !inVale)
        {
            chestArmorSprite.sprite = chestItem.armorSpriteSheet[skinIndex].armorSprites[currentSpriteNum];
            //chestArmorSprite.flipX = spriteHolder.transform.localScale.x == -1;
            chestArmorSprite.color = sprite.color;

            if (chestItem.armorSpriteSheet[skinIndex].materialOverride != null)
            {
                chestArmorSprite.material = chestItem.armorSpriteSheet[skinIndex].materialOverride;
            }
            else
            {
                chestArmorSprite.material = defaultArmorMat;
            }
        }
        else
        {
            chestArmorSprite.sprite = null;
        }

        if (legsItem && !inVale)
        {
            legArmorSprite.sprite = legsItem.armorSpriteSheet[skinIndex].armorSprites[currentSpriteNum];
            //legArmorSprite.flipX = spriteHolder.transform.localScale.x == -1;
            legArmorSprite.color = sprite.color;

            if (legsItem.armorSpriteSheet[skinIndex].materialOverride != null)
            {
                legArmorSprite.material = legsItem.armorSpriteSheet[skinIndex].materialOverride;
            }
            else
            {
                legArmorSprite.material = defaultArmorMat;
            }
        }
        else
        {
            legArmorSprite.sprite = null;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_UpdateArmor(string head, string chest, string legs)
    {
        headItem = gameManager.instance.itemDictionary[head];
        chestItem = gameManager.instance.itemDictionary[chest];
        legsItem = gameManager.instance.itemDictionary[legs];
    }

    void StopSprint()
    {
        if (sprinting)
        {
            playerWater.instance.usingStamina = false;
            sprinting = false;
            //DOTween.To(() => cam.m_Lens.OrthographicSize, x => cam.m_Lens.OrthographicSize = x, normalCamSize, 0.5f);
            walkAnimSpeed = walkAnimDefaultSpeed;
        }
    }

    void EnableCollider()
    {
        collision.gameObject.SetActive(true);
    }

    private void Move(float speed)
    {
        speed *= iceSpeedMult;
        speed *= DungeonGenerator.instance.GetModifier("speed");
        speed *= inventory.instance.GetModifierInToolSlot("speed") * inventory.instance.GetModifierInArmorSlots("speed");
        if (HandEvents.handMove.jackpotActive) speed *= 1.25f;

        if (inventory.instance.legsSlot.itemInSlot && inventory.instance.legsSlot.itemInSlot.uniqueType == item.UniqueType.frost
            && inventory.instance.legsSlot.itemData.tier >= 4 && playerWater.instance.waterWalkInWater)
        {
            waterWalkTimer = 1f;
        }
        if (waterWalkTimer > 0)
        {
            speed *= 1.5f;
        }

        rb.linearVelocity = Vector2.zero;
        Vector3 extraMovement = boatVelocity;

        Vector3 input = axisInput;
        if (dashing)
        {
            input = dashInput;
        }

        //no move while phasing
        if (currentBoat && currentBoat.skullAbilityActive)
        {
            speed = 0;
        }

        rb.MovePosition((Vector3)rb.position + input * speed * speedMult * 0.01f + extraMovement);
    }

    void KnockbackMotion()
    {
        rb.MovePosition((Vector3)rb.position + boatVelocity + ((Vector3)health.knockbackVector/30));
    }

    public IEnumerator _Jackpot(float additionalTime, HandMove h)
    {
        h.healthOnJackpotStart = playerHealth.instance.currentHealth + UnityEngine.Random.Range(4, 7);
        h.jackpotRegenHealth = playerHealth.instance.currentHealth;
        h.jackpotActive = true;
        h.jackpotPs.Play();
        h.jackpotStartSound.Play();
        h.jackpotActiveSound.volume = 0.1f;
        h.jackpotActiveSound.Play();
        h.jackpotActiveSound.DOFade(0.8f, 1);

        yield return new WaitForSeconds(5 + additionalTime);

        h.jackpotActiveSound.DOFade(0, 1);
        yield return new WaitForSeconds(1);
        h.jackpotPs.Stop();
        h.jackpotActive = false;

    }
    public Transform GetClosestItem(itemPickup[] items)
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        foreach (itemPickup potentialTarget in items)
        {
            if (potentialTarget != null && !potentialTarget.disableCollection)
            {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = potentialTarget.transform;
                }

            }

        }

        return bestTarget;
    }
    public Transform GetClosestTile(TileEntity[] tiles)
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        foreach (TileEntity potentialTarget in tiles)
        {
            if (potentialTarget != null && !(!potentialTarget.dommy && potentialTarget.Item.TileType == item.tileType.door))
            {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = potentialTarget.transform;
                }

            }

        }

        return bestTarget;
    }



    public IEnumerator _OreLocator()
    {
        Vector2 targetPos = Vector2.zero;
        bool foundTarget = false;

        if (!onIsland) yield break;

        List<treeChop> oresOfType = new List<treeChop>();


        foreach (treeChop chop in treeRPCs.Instance.islandFeatures[currentIsland.index].treeChops)
        {
            if (!chop) continue;

            List<item> lootItems = new List<item>();
            if (chop.rareItemDrop != null)
                lootItems.Add(chop.rareItemDrop);
            foreach (LootItem loot in chop.lootItems)
            {
                lootItems.AddRange(loot.itemType);
            }

            foreach (item i in lootItems)
            {
                if (i == inventorySlot.equippedSlot.itemInSlot ||
                    (inventorySlot.equippedSlot.itemInSlot.resourceTrackerItem && i == inventorySlot.equippedSlot.itemInSlot.resourceTrackerItem))
                {
                    oresOfType.Add(chop);
                    break;
                }
            }
        }

        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        treeChop tree = null;
        foreach (treeChop chop in oresOfType)
        {
            if (chop != null && !chop.dead)
            {
                Vector3 directionToTarget = chop.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    targetPos = chop.transform.position;
                    foundTarget = true;
                    tree = chop;
                }

            }

        }

        itemPickup itemP = null;

        if (!foundTarget)
        {
            foreach (itemPickup i in treeRPCs.Instance.items)
            {
                if (i && i.Item == inventorySlot.equippedSlot.itemInSlot)
                {
                    Vector3 directionToTarget = i.transform.position - currentPosition;
                    float dSqrToTarget = directionToTarget.sqrMagnitude;
                    //sqrt 360000 = 600
                    if (dSqrToTarget < 360000 && dSqrToTarget < closestDistanceSqr)
                    {
                        closestDistanceSqr = dSqrToTarget;
                        targetPos = i.transform.position;
                        itemP = i;
                        foundTarget = true;
                    }

                }
            }

            if (!foundTarget)
            {
                oreLocatorSound.pitch = 0.6f;
                oreLocatorSound.Play();
                yield break;
            }
        }

        inventory.instance.specialCooldownTime = inventory.instance.specialInitialCooldown;
        gameManager.endScreenStats["abilities"]++;

        locatorArrow.GetChild(0).gameObject.SetActive(true);
        locatorArrow.localScale = Vector2.zero;
        locatorArrow.DOScale(Vector2.one, 0.5f);
        yield return null;
        oreLocatorSound.pitch = 1;
        oreLocatorSound.Play();

        bool isItem = itemP != null;

        for (float t = 0; t < 7f; t += Time.deltaTime)
        {
            Vector3 direction = new Vector3(targetPos.x - transform.position.x, targetPos.y - transform.position.y).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            locatorArrow.eulerAngles = new Vector3(0, 0, angle);

            if (Vector2.Distance(transform.position, targetPos) > 2f)
            {
                locatorArrow.localScale = Vector2.Lerp(locatorArrow.localScale, Vector2.one, 5 * Time.deltaTime);
            }
            else
            {
                locatorArrow.localScale = Vector2.Lerp(locatorArrow.localScale, Vector2.zero, 5 * Time.deltaTime);
            }

            if ((isItem && !itemP) || (tree && tree.dead))
            {
                inventory.instance.specialCooldownTime = 1.1f;
                break;
            }

            yield return null;
        }
        locatorArrow.DOScale(Vector2.zero, 1);
        HandMove.instance.catalystDoneSound.Play();
        for (float t = 0; t < 1; t += Time.deltaTime)
        {
            Vector3 direction = new Vector3(targetPos.x - transform.position.x, targetPos.y - transform.position.y).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            locatorArrow.eulerAngles = new Vector3(0, 0, angle);
            yield return null;
        }
        locatorArrow.GetChild(0).gameObject.SetActive(false);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_HyperDash()
    {
        StartCoroutine(_HyperDashCo());
    }

    IEnumerator _HyperDashCo()
    {
        gameManager.endScreenStats["abilities"]++;

        hyperStartSound.pitch = 1.4f;
        hyperStartSound.Play();

        dashEffect.Play();
        dashing = true;
        hyperdashing = true;

        waterWalkSound.volume = 1;
        waterWalkSound.clip = strideStopClip;
        waterWalkPs.Play();
        waterWalkSound.Play();

        hyperdashing = true;

        bool canceled = false;
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            if ((!moving ||
                (InputManager.actions["Ability"].started && t > 0.25f))
                && t < 1.15f)
            {
                waterWalkSound.DOFade(0, 0.4f);
                strideCancelSound.Play();
                canceled = true;

                yield return new WaitForSeconds(0.25f);

                break;
            }
            yield return null;
        }

        if (!canceled)
        {
            strideCancelSound.Play();
            yield return new WaitForSeconds(0.35f);
        }

        hyperdashing = false;
        waterWalkPs.Stop();
    }

    public void DivineGuard()
    {
        RPC_DivineGuard();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_DivineGuard()
    {
        StartCoroutine(_DivineGuardCo());
    }

    IEnumerator _DivineGuardCo()
    {
        guardSound.clip = guardStartClip;
        guardPs.Play();
        guardSound.Play();
        health.invincibility = true;
        divineGuardDamage.ignorePlayers = view.StateAuthority == SingletonRunner.runner.LocalPlayer;
        divineGuardHitbox.enabled = true;

        for (float t = 0; t < 3.5f; t += Time.deltaTime)
        {
            health.invincibility = true;
            yield return null;
        }

        guardSound.clip = guardStopClip;
        guardSound.Play();
        yield return new WaitForSeconds(1);
        guardPs.Stop();
        yield return new WaitForSeconds(0.3f);
        health.invincibility = false;
        divineGuardHitbox.enabled = false;
    }

    IEnumerator _TweenTilemapAlpha(float duration, float targetAlpha, Tilemap tilemap)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap is not assigned.");
            yield break;
        }

        Color startColor = tilemap.color;
        float startAlpha = startColor.a;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            tilemap.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            yield return null;
        }

        tilemap.color = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
    }

    public void Dash(float time = 0.27f)
    {
        RPC_Dash(time);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_Dash(float time)
    {
        StartCoroutine(_DashCo(time));
    }

    public IEnumerator _DashCo(float time)
    {
        dashEffect.Play();
        dashSound.Play();
        dashing = true;
        anim.SetTrigger("Dash");
        yield return new WaitForSeconds(time);
        dashing = false;

    }

    public IEnumerator _HyperDash()
    {
        locatorArrow.GetChild(0).gameObject.SetActive(true);
        locatorArrow.localScale = Vector2.zero;
        locatorArrow.DOScale(Vector2.one, 0.5f);
        yield return null;
        hyperStartSound.pitch = 1.1f;
        hyperStartSound.Play();

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(InputManager.instance.mousePos);
        Vector2 direction = new Vector2(mousePosition.x - transform.position.x, mousePosition.y - transform.position.y).normalized;

        float lerp = 8;

        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            chargingHyperdash = true;

            mousePosition = Camera.main.ScreenToWorldPoint(InputManager.instance.mousePos);

            direction = Vector3.Slerp(direction, new Vector2(mousePosition.x - transform.position.x, mousePosition.y - transform.position.y).normalized, Time.deltaTime * lerp);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            locatorArrow.eulerAngles = new Vector3(0, 0, angle);

            yield return null;

            if (!InputManager.actions["Ability"].held)
            {
                chargingHyperdash = false;
                locatorArrow.DOScale(Vector2.zero, 0.5f);
                inventory.instance.specialCooldownTime = 1;
                yield return new WaitForSeconds(0.5f);
                locatorArrow.GetChild(0).gameObject.SetActive(false);
                yield break;
            }

        }
        hyperStartSound.pitch = 1.3f;
        hyperStartSound.Play();
        while (InputManager.actions["Ability"].held)
        {
            mousePosition = Camera.main.ScreenToWorldPoint(InputManager.instance.mousePos);

            direction = Vector3.Slerp(direction, new Vector2(mousePosition.x - transform.position.x, mousePosition.y - transform.position.y).normalized, Time.deltaTime * lerp);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            locatorArrow.eulerAngles = new Vector3(0, 0, angle);

            locatorArrow.localScale = Vector2.Lerp(locatorArrow.localScale, new Vector2(1.5f, 1.5f), 20 * Time.deltaTime);

            yield return null;
        }
        chargingHyperdash = false;
        inventory.instance.specialCooldownTime = inventory.instance.specialInitialCooldown;

        locatorArrow.DOScale(Vector2.zero, 1);

        RPC_HyperDash();

        dashInput = direction;
        anim.SetFloat("Walk Speed", 1.8f);
        yield return new WaitForFixedUpdate();
        while (hyperdashing) yield return null;
        dashing = false;
    }

    bool CircleHasTile(Tilemap tilemap, Vector2Int center, int radius)
    {
        int radiusSquared = radius * radius;

        for (int y = -radius; y <= radius; y++)
        {
            int ySquared = y * y;

            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + ySquared > radiusSquared)
                    continue;

                if (tilemap.GetTile(new Vector3Int(
                        center.x + x,
                        center.y + y,
                        0)) != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_EnableFireCircle(bool equipped, bool active, bool doubleCircle)
    {
        fireCircleEquipped = equipped;
        fireCircleEnabled = active;
        StartCoroutine(EnableFireCircle(equipped, active, doubleCircle));
    }

    IEnumerator EnableFireCircle(bool equipped, bool active, bool doubleCircle)
    {
        yield return null;

        if (view.StateAuthority == SingletonRunner.runner.LocalPlayer) yield return new WaitForSeconds(0.08f);

        fireCircle.gameObject.SetActive(equipped);

        fireCircle.SetBool("Active", active);

        if (doubleCircle)
        {
            fireCircle2.gameObject.SetActive(equipped);
            fireCircle2.SetFloat("Offset", 0.25f);
            fireCircle2.SetBool("Active", active);
        }
        else
        {
            fireCircle2.gameObject.SetActive(false);
        }

    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SporeExplosion()
    {
        StartCoroutine(_SporeExplode());
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SlothVortex()
    {
        StartCoroutine(_SlothVortex());
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_WrathCounter()
    {
        if (counterCo != null) StopCoroutine(counterCo);

        counterCo = StartCoroutine(_WrathCounter());
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_DungeonFade(bool fadeIn)
    {
        if (!fadeIn)
        {
            sprite.DOFade(0, 1);
            HandEvents.handMove.sprite.DOFade(0, 1);
            heldItemSprite.DOFade(0, 1);
            usernameText.DOFade(0, 1);
        }
        else
        {
            sprite.DOFade(1, 1);
            HandEvents.handMove.sprite.DOFade(1, 1);
            heldItemSprite.DOFade(1, 1);
            usernameText.DOFade(1, 1);
        }

    }
    public IEnumerator _SporeExplode()
    {
        explodeStartSound.Play();
        exploding = true;
        for (int i = 0; i < 2; i++)
        {
            sprite.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
        GameObject expl = Instantiate(sporeExplosion, transform.position, Quaternion.identity);

        expl.GetComponent<damagePlayer>().ignorePlayers = view.HasStateAuthority;

        yield return new WaitForSeconds(0.2f);
        exploding = false;

    }

    public IEnumerator _WrathCounter()
    {
        HandEvents.handMove.disableSlothGain = true;
        counterSound.Play();

        counterSprite.enabled = true;
        counterSprite.color = new Color(1, 1, 1, 0);
        counterSprite.DOFade(1, 0.2f);
        counterSprite.sprite = sprite.sprite;
        countering = true;
        yield return new WaitForSeconds(0.2f);
        counterSprite.sprite = sprite.sprite;
        counterSprite.DOFade(0, 1);
        yield return new WaitForSeconds(0.4f);
        countering = false;
        yield return new WaitForSeconds(3);

        HandEvents.handMove.disableSlothGain = false;
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_CounterSuccess(Vector2 attackerPosition)
    {
        StartCoroutine(_CounterSuccess(attackerPosition));
    }

    IEnumerator _CounterSuccess(Vector2 attackerPosition)
    {
        if (counterCo != null)
            StopCoroutine(counterCo);

        countering = false;
        counterSprite.DOFade(0, 1);

        GameObject expl = Instantiate(fireExpl, attackerPosition, Quaternion.identity);

        expl.GetComponent<damagePlayer>().ignorePlayers = view.HasStateAuthority;

        yield return new WaitForSeconds(1);

        HandEvents.handMove.disableSlothGain = false;
    }

    public IEnumerator _SlothVortex()
    {
        GameObject expl = Instantiate(slothVortex, transform.position, Quaternion.identity);
        expl.GetComponent<damagePlayer>().ignorePlayers = HasStateAuthority;
        HandEvents.handMove.disableSlothGain = true;

        for (float t = 0; t < 4; t += Time.deltaTime)
        {
            expl.transform.position = transform.position;
            yield return null;
        }
        expl.GetComponent<Collider2D>().enabled = false;

        HandEvents.handMove.disableSlothGain = false;
    }

}

using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class menu : MonoBehaviour, IDataPersistance
{
    public static menu instance;

    public Image minimap;
    public GameObject signUi;
    public RectTransform mapTransform;
    public TextMeshProUGUI signWriteText;
    public TMP_InputField signInput;

    public GameObject refuelMessage;
    public TextMeshProUGUI tileTipsText;
    public bool showTileTips;
    //remove this
    public bool testEPress;
    public TextMeshProUGUI testEPressText;
    public CanvasGroup uiGroup;

    public CanvasGroup phantomShiftGroup;
    public GameObject phantomShiftPrompt1, phantomShiftPrompt2;
    public TextMeshProUGUI phantomShiftText, phantomShiftAbilityText;
    public Color phantomShiftTextColor;
    public TextMeshProUGUI dungeonModifierText;

    [Header("Map")]
    public float scaleFactor = 0.2f;
    public BiomeInfo[] biomeInfo;
    public RectTransform[] markers;
    public CanvasGroup mapGroup;
    public float mapAlpha;
    public CanvasGroup[] markerGroups;
    public RectTransform deathMarker, respawnMarker, bossMarker;

    public RectTransform biomeInfoBox;
    public CanvasGroup biomeInfoGroup;
    public TextMeshProUGUI biomeInfoName, biomeInfoResources, biomeInfoDesc;

    public bool mapMaximized;
    const float mapMaxScale = 4.5f;
    const float mapTransitionTime = 0.3f;
    float mapNormalScale;
    Vector2 mapDefaultPos;
    Texture oldTex;
    public RectTransform fullMap;
    [SerializeField] Image fullMapImage;
    [SerializeField] GameObject dungeonCircle;
    List<CanvasGroup> dungeonCircles = new List<CanvasGroup>();
    public bool trackingDungeons;
    [SerializeField] Image trackerImage;
    [SerializeField] Sprite[] trackerSprites;
    [SerializeField] item soulTrackerItem;
    [SerializeField] CanvasGroup dungeonTrackingButton;

    Texture fullMapTex;
    [HideInInspector]
    public Vector2 lastDeathPos;
    [HideInInspector]
    public bool showDeathMarker;
    public GameObject mapIslandHighlightPrefab;
    List<CanvasGroup> mapIslandHighlightGroups = new List<CanvasGroup>();
    List<Image> mapIslandHighlightImages = new List<Image>();
    float islandHighlightPPU;

    public AudioSource pageTurnSound;
    public float fullMapScale = 20;

    public List<Boat> boats = new List<Boat>();
    public List<Transform> boatIcons = new List<Transform>();
    public List<Transform> allMapIcons = new List<Transform>();
    public float minZoom = 0.7f;
    public float maxZoom = 3.0f;
    [SerializeField] float mapZoomSpeed;
    public GameObject[] boatIconPrefabs;
    [SerializeField] Color waterMapColor;
    Vector2 fullMapNormalScale;
    Vector2 mousePosition;
    public RectTransform mapMaskRect;
    Vector2 mapTargetPos, mapTargetZoom;
    //[SerializeField] float mapZoomCenterScale = 1.2f;

    [SerializeField] CanvasGroup markerButtonsGroup;
    [SerializeField] GameObject[] placeableMarkerPrefabs;
    [SerializeField] int[] markerCounts;
    public GameObject holdingMarker;
    public int holdingMarkerType;
    public List<PlacedMarker> placedMarkers = new List<PlacedMarker>();
    [SerializeField] TextMeshProUGUI holdingMarkerTips, deleteMarkerTips;
    [SerializeField]
    GameObject biomeInfoTips;
    [SerializeField] Vector2 placeMarkerPosMultiplier;
    //for placeable map markers affected by scrolling with different offsets
    [SerializeField] Transform mousePositionTransform;
    [SerializeField] AudioSource markerPlaceSound, markerRemoveSound;
    [SerializeField] TextMeshProUGUI[] markerCountTexts;
    Vector2 mouseStartDragPos;
    Vector2 mapStartDragPos;
    bool markerCooldown;

    [SerializeField] CanvasGroup specialCooldownGroup;
    [SerializeField] TextMeshProUGUI specialCooldownText;
    [SerializeField] Image specialCooldownBar;

    public bool[] markersDisabled = new bool[] { false, false, false, false, false };

    bool loadingMenu;

    public int mushLevel = 0;
    public RectTransform mushBarHolder;
    public Image mushBarImage, mushHolderImage;
    public CanvasGroup mushGroup, pressFGroup, pressSouthGroup;
    public Sprite[] mushBarSprites;

    [Header("Stamina")]
    public RectTransform staminaBar, staminaHolder;
    public CanvasGroup staminaGroup;
    public RawImage staminaBarImage;
    bool staminaShow;
    public CanvasGroup drownScreen, lavaDrownScreen;
    public Animator staminaAnim;
    public float staminaPosMult = 5;
    private Vector3 initialStaminaPos;
    public CanvasGroup unbearableColdGroup;

    [Header("Deads")]
    public CanvasGroup deadScreen;
    public Image deathScreenImage;
    public Sprite normalDeathImage, permaDeathImage;
    public TextMeshProUGUI deadScreenText;
    public CanvasGroup respawnButtonGroup;
    public AudioSource deathSound;
    public Volume lowHealthVol;
    public ParticleSystem lowHealthPs;
    public TextMeshProUGUI permaDeathButtonText, soulRecoveredText, followLightText;
    public CanvasGroup deathFade, blackFade, valeText, soulRecoveredGroup, valeText2;
    public RectTransform itemsLostHolder, uniquesLostHolder;
    [SerializeField] GameObject lostItemImagePrefab;

    [Header("No Return")]
    public CanvasGroup noReturnGroup;
    public TextMeshProUGUI continueButtonText, playersReadyText;
    public int playersReady;
    bool ready;
    public bool waitingForReady;
    [SerializeField] CanvasGroup spectateGroup;
    int spectatingPlayerId = 0;
    public player spectatingPlayer;
    [SerializeField] TextMeshProUGUI spectatingText;
    public TextMeshProUGUI respawnButtonText;
    public bool Spectating { get; private set; }
    [SerializeField] CanvasGroup permaDeathGroup;
    [SerializeField] CanvasGroup reloadSaveButtonGroup;
    private bool mushBarShow;
    private float mushShowTime;
    private bool mapClosing;
    string previousCharmEffect;
    private bool holdingBiomeInfoButton;
    private bool firstMapOpen;
    private bool coldFadingIn;

    private void Awake()
    {
        instance = this;

    }

    private void Start()
    {
        mapAlpha = mapGroup.alpha;

        phantomShiftTextColor = phantomShiftText.color;

        lowHealthVol.weight = 0;

        permaDeathGroup.alpha = 0;
        permaDeathGroup.blocksRaycasts = false;
        permaDeathGroup.interactable = false;
        reloadSaveButtonGroup.interactable = false;
        reloadSaveButtonGroup.blocksRaycasts = false;

        initialStaminaPos = staminaHolder.anchoredPosition;

        noReturnGroup.interactable = false;
        noReturnGroup.blocksRaycasts = false;
        noReturnGroup.alpha = 0;

        specialCooldownGroup.alpha = 0;

        allMapIcons.Add(markers[0]);
        allMapIcons.Add(markers[1]);
        allMapIcons.Add(markers[2]);
        allMapIcons.Add(markers[3]);
        allMapIcons.Add(markers[4]);
        allMapIcons.Add(deathMarker);
        allMapIcons.Add(respawnMarker);
        allMapIcons.Add(bossMarker);

        fullMapNormalScale = fullMapImage.rectTransform.localScale;

        deathMarker.gameObject.SetActive(false);

        mapNormalScale = mapTransform.localScale.x;
        mapDefaultPos = mapTransform.anchoredPosition;

        fullMap.gameObject.SetActive(false);

        staminaGroup.alpha = 0;
        mushGroup.alpha = 0;
        pressFGroup.alpha = 0;
        pressFGroup.transform.localScale = Vector2.zero;

        deadScreen.alpha = 0;
        markerButtonsGroup.alpha = 0;

        markerCounts = new int[placeableMarkerPrefabs.Count()];

        spectateGroup.interactable = false;
        spectateGroup.blocksRaycasts = false;
        spectateGroup.alpha = 0;

        valeText2.alpha = 0;

        //local player marker goes in front
        if (DataPersistanceManager.LocalActorIndex >= 0)
        {
            Canvas c = markers[DataPersistanceManager.LocalActorIndex].GetComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 16;
        }

    }

    public void GenerateDungeonCircles()
    {
        System.Random rng = new System.Random(seed.instance.currentSeed);
        int offset = 50;

        foreach (DungeonFeature d in featurePlacer.Instance.dungeonFeatures)
        {
            if (!d.alreadySpawned) continue;

            CanvasGroup g = Instantiate(dungeonCircle, fullMap).GetComponent<CanvasGroup>();
            g.GetComponent<RectTransform>().anchoredPosition
                = (d.selectedPosition + new Vector2(rng.Next(-offset, offset), rng.Next(-offset, offset))) / fullMapScale;
            dungeonCircles.Add(g);
            g.alpha = 0;
        }

        SetDungeonTracking(false);
    }

    public void FixMapMat()
    {
        minimap.enabled = false;
        minimap.enabled = true;
    }

    public void FinishWritingSign()
    {
        MouseCursor.state = 0;
        signUi.SetActive(false);
        gameManager.instance.writingSign = false;
    }

    public void PressETest()
    {
        StopAllCoroutines();
        StartCoroutine(_PressETest());
    }

    IEnumerator _PressETest()
    {
        testEPressText.GetComponentInParent<Button>().interactable = false;

        for (float t = 2f; t > 0; t -= Time.deltaTime)
        {
            testEPressText.text = (Mathf.Round(t * 100f) / 100f).ToString();
            yield return null;
        }

        testEPressText.text = "PRESSED";
        testEPressText.GetComponentInParent<Image>().color = Color.green;
        testEPress = true;
        testEPressText.GetComponentInParent<AudioSource>().Play();

        yield return new WaitForSeconds(0.15f);

        testEPressText.GetComponentInParent<Image>().color = Color.white;
        testEPress = false;
        testEPressText.text = "Press E";

        testEPressText.GetComponentInParent<Button>().interactable = true;
    }

    private void Update()
    {
        allMapIcons.RemoveAll(t => t == null);
        boatIcons.RemoveAll(t => t == null);

        respawnButtonGroup.alpha = deadScreen.alpha;

        if (!MapDisplay.finishedLoading || !playerSpawner.finishedSpawningPlayers || !player.instance) return;

        phantomShiftPrompt1.SetActive(!InputManager.usingController);
        phantomShiftPrompt2.SetActive(InputManager.usingController);

        if(!mapMaximized && IslandMapHighlight.selectedMapIsland)
        {
            IslandMapHighlight.selectedMapIsland.StopHighlight();
        }

        //disconnect if not connected or if i become host when not index 0
        if ((!SingletonRunner.runner.IsInSession || (SingletonRunner.runner.IsSharedModeMasterClient && DataPersistanceManager.LocalActorIndex > 0))
             && !loadingMenu)
        {
            loadingMenu = true;
            Debug.Log("Loading Menu");
            StartCoroutine(pauseScreen.instance._LoadMenu());
        }

        if (showTileTips && !gameManager.inBoss)
        {
            tileTipsText.alpha = Mathf.Lerp(tileTipsText.alpha, 1, Time.deltaTime * 20);

            if (InputManager.usingController)
            {
                tileTipsText.text = "R Stick - rotate tile    Right Button - change state";
            }
            else
            {
                tileTipsText.text = "R - rotate tile     Right Click - change state";
            }

        }
        else
        {
            tileTipsText.alpha = Mathf.Lerp(tileTipsText.alpha, 0, Time.deltaTime * 20);
        }

        playerHealth health = playerHealth.instance;

        if (Spectating && spectatingPlayer)
            health = spectatingPlayer.health;

        if (health.currentHealth <= health.maxHealth / 3)
        {
            lowHealthVol.weight = Mathf.Lerp(lowHealthVol.weight, (1 - ((float)health.currentHealth / (health.maxHealth / 3))) + 0.3f, Time.deltaTime * 1f);
        }
        else
        {
            lowHealthVol.weight = Mathf.Lerp(lowHealthVol.weight, 0, Time.deltaTime * 0.75f);
        }
        //particles
        if (health.currentHealth <= health.maxHealth / 3
            && (gameManager.instance.difficulty == 2 || gameManager.inBoss || (ValeManager.instance && ValeManager.instance.lostSoul))
            && health.currentHealth > 0 && !player.instance.pilotingBoat && !player.instance.inBoatAltAction)
        {
            if (!lowHealthPs.isPlaying)
                lowHealthPs.Play();

            var emission = lowHealthPs.emission;
            emission.rateOverTime = ((1 - ((float)health.currentHealth / (health.maxHealth / 3))) * 85) + 15;
        }
        else
        {
            if (lowHealthPs.isPlaying)
                lowHealthPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        //unbearable cold
        if(health.iceEffectTime >= 3)
        {
            if (coldFadingIn)
            {
                unbearableColdGroup.alpha = Mathf.MoveTowards(
                    unbearableColdGroup.alpha,
                    0.9f,
                    Time.deltaTime * 1f
                );

                if (unbearableColdGroup.alpha >= 0.8f)
                {
                    coldFadingIn = false;
                }
            }
            else
            {
                unbearableColdGroup.alpha = Mathf.MoveTowards(
                    unbearableColdGroup.alpha,
                    0f,
                    Time.deltaTime * 1f
                );

                if (unbearableColdGroup.alpha <= 0f)
                {
                    coldFadingIn = true;
                }
            }
        }
        else
        {
            coldFadingIn = true;
            unbearableColdGroup.alpha = Mathf.Lerp(unbearableColdGroup.alpha, 0, Time.deltaTime * 2);
        }

        if (boats.Count > boatIcons.Count)
        {
            GameObject newIcon = null;

            switch (boats[boatIcons.Count()].BoatType)
            {
                case Boat.boatType.sailboat:
                    newIcon = Instantiate(boatIconPrefabs[0], Vector2.zero, Quaternion.identity, markers[0].parent);
                    break;
                case Boat.boatType.pirateShip:
                    newIcon = Instantiate(boatIconPrefabs[1], Vector2.zero, Quaternion.identity, markers[0].parent);
                    break;
                case Boat.boatType.speedboat:
                    newIcon = Instantiate(boatIconPrefabs[2], Vector2.zero, Quaternion.identity, markers[0].parent);
                    break;
                case Boat.boatType.battleship:
                    newIcon = Instantiate(boatIconPrefabs[3], Vector2.zero, Quaternion.identity, markers[0].parent);
                    break;
                case Boat.boatType.ghost:
                    newIcon = Instantiate(boatIconPrefabs[4], Vector2.zero, Quaternion.identity, markers[0].parent);
                    break;

            }

            boatIcons.Add(newIcon.transform);
            allMapIcons.Add(newIcon.transform);

            newIcon.transform.SetAsFirstSibling();

            bossMarker.SetAsFirstSibling();
            deathMarker.SetAsFirstSibling();
            respawnMarker.SetAsFirstSibling();
        }


        if (!SingletonRunner.runner.IsConnectedToServer || !playerSpawner.instance || playerSpawner.instance.players == null || playerSpawner.instance.players[0] == null || playerSpawner.instance.players.Contains(null) || !MapDisplay.finishedLoading)
        {
            return;
        }


        if (!mapMaximized)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i].gameObject.SetActive(false);
                markerGroups[i].alpha = 1;

                if (i >= playerSpawner.instance.players.Length || !playerSpawner.instance.players[i]
                    || markersDisabled[i] == true)
                {
                    if (i == DataPersistanceManager.instance.GetActorIndex(SingletonRunner.runner.LocalPlayer))
                    {
                        markerGroups[i].alpha = 0.5f;
                    }
                    else
                    {
                        continue;
                    }
                }
                //hide marker when ded or in vale
                if (playerSpawner.instance.players[i].health.currentHealth > 0 && playerSpawner.instance.players[i].transform.position.y > -5000)
                {
                    markers[i].gameObject.SetActive(true);
                }
                if (player.instance.currentIsland.size > 0)
                    markers[i].localPosition = (((Vector2)playerSpawner.instance.players[i].transform.position - player.instance.currentIsland.center) / player.instance.currentIsland.size) * 100;

                //appear at the dungeon location
                if (playerSpawner.instance.players[i].inDungeon > -1)
                {
                    markers[i].localPosition = (((Vector2)featurePlacer.Instance.dungeonFeatures[playerSpawner.instance.players[i].inDungeon].selectedPosition
                        - player.instance.currentIsland.center) / player.instance.currentIsland.size) * 100;
                }

            }

            if (deathMarker.gameObject.activeSelf && showDeathMarker)
                deathMarker.localPosition = ((lastDeathPos - player.instance.currentIsland.center) / player.instance.currentIsland.size) * 100;

            if (TileEntity.currentSpawnPoint)
            {
                respawnMarker.localPosition = (((Vector2)TileEntity.currentSpawnPoint.transform.position - player.instance.currentIsland.center) / player.instance.currentIsland.size) * 100;
                respawnMarker.gameObject.SetActive(true);
            }
            else
            {
                respawnMarker.gameObject.SetActive(false);
            }

            if (gameManager.inBoss && (Boss.instance.inRange || Boss.instance.dead))
            {
                bossMarker.localPosition = (((Vector2)Boss.instance.transform.position - player.instance.currentIsland.center) / player.instance.currentIsland.size) * 100;
                bossMarker.gameObject.SetActive(true);
            }
            else
            {
                bossMarker.gameObject.SetActive(false);
            }

            foreach (Transform b in boatIcons)
            {
                if (!b || !player.instance || player.instance.currentIsland.tex == null) return;

                b.localPosition = (((Vector2)boats[boatIcons.IndexOf(b)].transform.position - player.instance.currentIsland.center) / player.instance.currentIsland.size) * 100;
                //b.localPosition = new Vector2(Mathf.Clamp(b.localPosition.x, -50, 50), Mathf.Clamp(b.localPosition.y, -50, 50));
            }

            foreach (PlacedMarker m in placedMarkers)
            {
                if (!player.instance || player.instance.currentIsland.tex == null) return;

                m.marker.localPosition = (((m.mapPos * fullMapScale) - player.instance.currentIsland.center) / player.instance.currentIsland.size) * 100;
                //m.marker.localPosition = m.mapPos * ((fullMapTex.width / (float)player.instance.currentIsland.tex.width) * fullMapIslandSizeMultiplier) + ((Vector2)player.instance.currentIsland.center * fullMapPositionMultiplierIdk);
            }

        }
        else
        {
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i].gameObject.SetActive(false);
                markers[i].GetComponent<CanvasGroup>().alpha = 1;

                if (i >= playerSpawner.instance.players.Length || !playerSpawner.instance.players[i]
                    || markersDisabled[i] == true)
                {
                    if (i == DataPersistanceManager.instance.GetActorIndex(SingletonRunner.runner.LocalPlayer))
                    {
                        markerGroups[i].alpha = 0.5f;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (playerSpawner.instance.players[i].health.currentHealth > 0 && playerSpawner.instance.players[i].transform.position.y > -5000)
                    markers[i].gameObject.SetActive(true);

                Vector2 mapPos = (Vector2)playerSpawner.instance.players[i].transform.position / fullMapScale;

                // clamp to map bounds
                mapPos.x = Mathf.Clamp(mapPos.x, -50, 50);
                mapPos.y = Mathf.Clamp(mapPos.y, -50, 50);

                markers[i].anchoredPosition = mapPos;

                //appear at the dungeon location
                if (playerSpawner.instance.players[i].inDungeon > -1)
                {
                    Vector2 mapPos2 = (Vector2)featurePlacer.Instance.dungeonFeatures[playerSpawner.instance.players[i].inDungeon].selectedPosition / fullMapScale;

                    // clamp to map bounds
                    mapPos2.x = Mathf.Clamp(mapPos2.x, -50, 50);
                    mapPos2.y = Mathf.Clamp(mapPos2.y, -50, 50);

                    markers[i].anchoredPosition = mapPos2;
                }
            }

            deathMarker.localPosition = lastDeathPos / fullMapScale;

            if (TileEntity.currentSpawnPoint)
            {
                respawnMarker.localPosition = playerHealth.instance.respawnPos / fullMapScale;
                respawnMarker.gameObject.SetActive(true);
            }
            else
            {
                respawnMarker.gameObject.SetActive(false);
            }

            if (gameManager.inBoss && (Boss.instance.inRange || Boss.instance.dead))
            {
                bossMarker.localPosition = Boss.instance.transform.position / fullMapScale;
                bossMarker.gameObject.SetActive(true);
            }
            else
            {
                bossMarker.gameObject.SetActive(false);
            }

            foreach (Transform b in boatIcons)
            {
                b.localPosition = boats[boatIcons.IndexOf(b)].transform.position / fullMapScale;
            }

            foreach (PlacedMarker m in placedMarkers)
            {
                m.marker.localPosition = m.mapPos;
            }


            //zoom in out
            float scrollWheel = InputManager.instance.mouseScroll * 0.25f;

            if (InputManager.usingController)
            {
                scrollWheel *= -1.5f;
            }
            mousePosition = InputManager.instance.mousePos;

            // Convert mouse position to RectTransform position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fullMapImage.rectTransform,
                mousePosition,
                null,
                out Vector2 localPoint
                );

            if (Mathf.Abs(scrollWheel) > 0.01f)
            {
                // Apply zoom around the mouse position
                if (mapMaskRect.rect.Contains(localPoint))
                    ZoomImage(scrollWheel, localPoint *= fullMap.localScale);
            }

            if (holdingMarker)
            {
                MouseCursor.state = 2;
            }
            else if (MouseCursor.state == 2)
            {
                MouseCursor.state = 1;
            }

            if ((InputManager.actions["Item Primary"].held || InputManager.actions["Pan Map"].held
                || InputManager.actions["Item Primary"].started || InputManager.actions["Pan Map"].started)
                && mapTransform.localScale.x == mapMaxScale && !markerCooldown)
            //&& mapMaskRect.rect.Contains(localPoint)
            {
                if (InputManager.actions["Item Primary"].started || InputManager.actions["Pan Map"].started)
                {
                    mouseStartDragPos = InputManager.instance.mousePos;
                    mapStartDragPos = fullMap.position;
                }

                fullMap.position = mapStartDragPos + ((Vector2)InputManager.instance.mousePos - mouseStartDragPos);

                MouseCursor.state = 2;
            }
            else if (MouseCursor.state == 2 && !holdingMarker)
            {
                MouseCursor.state = 1;
            }

            //reset map
            if (InputManager.actions["Rotate"].started)
            {
                fullMapImage.rectTransform.DOScale(fullMapNormalScale, mapTransitionTime);
                fullMapImage.rectTransform.localPosition = Vector2.zero;
                pageTurnSound.pitch = 1.15f;
                pageTurnSound.Play();

                foreach (Transform t in allMapIcons)
                {
                    t.DOScale(1 * mapNormalScale / mapMaxScale, mapTransitionTime);
                }

                foreach (Image i in mapIslandHighlightImages)
                {
                    i.pixelsPerUnitMultiplier = islandHighlightPPU;
                }

            }


            fullMap.localPosition = new Vector2(Mathf.Clamp(fullMap.localPosition.x, (fullMap.localScale.x - 1) * -50, (fullMap.localScale.x - 1) * 50), Mathf.Clamp(fullMap.localPosition.y, (fullMap.localScale.x - 1) * -50, (fullMap.localScale.x - 1) * 50));


        }

        Dictionary<item, int> counts = inventory.instance.GetItemCounts(true);
        if (!counts.ContainsKey(soulTrackerItem))
        {
            if (trackingDungeons)
                SetDungeonTracking(false);
            dungeonTrackingButton.alpha = 0;
            dungeonTrackingButton.interactable = false;
            dungeonTrackingButton.blocksRaycasts = false;
        }
        else
        {
            dungeonTrackingButton.alpha = 1;
            dungeonTrackingButton.interactable = true;
            dungeonTrackingButton.blocksRaycasts = true;
        }


        foreach (Transform t in allMapIcons)
        {
            t.localPosition = new Vector2(Mathf.Clamp(t.localPosition.x, -50, 50), Mathf.Clamp(t.localPosition.y, -50, 50));
        }

        if (player.instance.currentIsland.tex != oldTex && !mapMaximized)
        {
            oldTex = player.instance.currentIsland.tex;

            //scaleFactor = MapGenerator.instance.currentIsland.size * -0.0008f + 0.6f;
            minimap.material.mainTexture = player.instance.currentIsland.tex;

            FixMapMat();
        }


        foreach (RectTransform b in boatIcons)
        {
            if (boats[boatIcons.IndexOf(b)].transform.localScale.x < 0)
            {
                b.localScale = new Vector2(Mathf.Abs(b.localScale.x) * -1, b.localScale.y);
            }
            else
            {
                b.localScale = new Vector2(Mathf.Abs(b.localScale.x), b.localScale.y);
            }

        }


        //boat refuel text
        if (player.instance.pilotingBoat && (player.instance.currentBoat.BoatType == Boat.boatType.speedboat || player.instance.currentBoat.BoatType == Boat.boatType.battleship) && player.instance.currentBoat.fuelTime <= 0 && player.instance.currentBoat.fuelCount <= 0)
        {
            refuelMessage.SetActive(true);
        }
        else
        {
            refuelMessage.SetActive(false);
        }


        if (player.instance.pilotingBoat && player.instance.currentBoat.BoatType == Boat.boatType.speedboat)
        {
            if (InputManager.actions["Boat Map"].started)
            {
                OpenMap(!mapMaximized, false);
            }
        }

        //stamina
        if (playerWater.instance)
        {
            if (playerWater.instance.infiniteStamina || (playerWater.instance.stamina < playerWater.instance.totalMaxStamina - 0.5f)
                && !player.instance.pilotingBoat && !player.instance.inBoatAltAction && !playerHealth.instance.dead && !player.instance.inCutscene)
            {
                if (!staminaShow)
                {
                    staminaShow = true;
                    staminaGroup.DOFade(1, 0.2f);
                }
                staminaBar.localPosition = new Vector2(0, (1 - (playerWater.instance.stamina / (playerWater.instance.totalMaxStamina))) * -47f);
            }
            else if (staminaShow)
            {
                staminaShow = false;
                staminaGroup.DOFade(0, 0.2f);
            }

        }

        if (inventory.charmEffect != previousCharmEffect)
        {
            mushLevel = 0;
            previousCharmEffect = inventory.charmEffect;
        }

        if ((inventory.charmEffect == "mush" || inventory.charmEffect == "sloth" || inventory.charmEffect == "wrath" || inventory.charmEffect == "envy") && mushLevel > 0 && (mushShowTime > 0 || mushLevel == 100) && !player.instance.pilotingBoat && !player.instance.inBoatAltAction)
        {
            mushLevel = Mathf.Clamp(mushLevel, 0, 100);
            previousCharmEffect = inventory.charmEffect;

            if (!mushBarShow)
            {
                mushGroup.DOFade(1, 0.5f);
                mushBarShow = true;
                mushBarImage.fillAmount = mushLevel / 100f;
            }
            mushBarImage.fillAmount = Mathf.Lerp(mushBarImage.fillAmount, mushLevel / 100f, Time.deltaTime * 5);
            if (inventory.charmEffect == "envy")
            {
                mushBarImage.fillAmount = mushLevel / 100f;
            }

            mushShowTime -= Time.deltaTime;
        }
        else
        {
            mushBarImage.fillAmount = Mathf.Lerp(mushBarImage.fillAmount, mushLevel / 100f, Time.deltaTime * 5);

            if (mushBarShow)
            {
                mushBarShow = false;
                mushGroup.DOFade(0, 0.5f);
            }
        }

        //place marker
        if (holdingMarker)
        {
            holdingMarker.transform.position = InputManager.instance.mousePos;

            if (InputManager.actions["Inventory Click R"].started)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(mapMaskRect, InputManager.instance.mousePos))
                {
                    PlaceMarker(holdingMarker.transform.localPosition);
                }

            }

            if (InputManager.actions["Charm"].started)
            {
                PlaceMarker(markers[DataPersistanceManager.instance.GetActorIndex(SingletonRunner.runner.LocalPlayer)].localPosition);
                print("1");
            }

        }
        else
        {
            //remove
            if (MapMarker.selectedMarker)
            {
                if (InputManager.actions["Inventory Click R"].started)
                {
                    placedMarkers.RemoveAll(p => p.marker == MapMarker.selectedMarker.transform);
                    Destroy(MapMarker.selectedMarker.gameObject);
                    markerCounts[MapMarker.selectedMarker.markerType]--;
                    MapMarker.selectedMarker = null;

                    markerRemoveSound.Play();
                }

            }

        }
        holdingMarkerTips.gameObject.SetActive(holdingMarker != null);
        deleteMarkerTips.gameObject.SetActive(holdingMarker == null && MapMarker.selectedMarker);

        biomeInfoTips.gameObject.SetActive(holdingMarker == null && !deleteMarkerTips.gameObject.activeSelf
            && IslandMapHighlight.selectedMapIsland);

        if (InputManager.usingController)
        {
            holdingMarkerTips.text = "LT to place\r\n\r\nLeft button to place at player position";
            deleteMarkerTips.text = "LT to delete";
        }
        else
        {
            holdingMarkerTips.text = "Right click to place\r\n\r\nF to place at player position";
            deleteMarkerTips.text = "Right click to delete";
        }

        if (mapMaximized)
        {
            for (int i = 0; i < markerCountTexts.Length; i++)
            {
                markerCountTexts[i].text = (5 - markerCounts[i]) + "/5";
            }

        }

        if (inventory.instance.specialCooldownTime > 0)
        {
            inventory.instance.specialCooldownTime -= Time.deltaTime;
            specialCooldownGroup.alpha = Mathf.Lerp(specialCooldownGroup.alpha, 1, 5 * Time.deltaTime);
            specialCooldownBar.rectTransform.sizeDelta = Vector2.Lerp(specialCooldownBar.rectTransform.sizeDelta, new Vector2(inventory.instance.specialCooldownTime / inventory.instance.specialInitialCooldown * 100, specialCooldownBar.rectTransform.sizeDelta.y), 10 * Time.deltaTime);
            specialCooldownText.text = inventory.instance.specialCooldownType + " Cooldown - " + Mathf.RoundToInt(inventory.instance.specialCooldownTime);

            switch (inventory.instance.specialCooldownType)
            {
                case "Resource Tracker":
                    specialCooldownBar.color = new Color(1, 0.85f, 0);
                    break;
                case "Vortex Cleave":
                    specialCooldownBar.color = new Color(1, 0.1f, 1);
                    break;
                case "Sidestep":
                    specialCooldownBar.color = new Color(0, 0.7f, 1f);
                    break;
                case "Hypersprint":
                    specialCooldownBar.color = new Color(0, 1f, 0.1f);
                    break;
                case "Divine Guard":
                    specialCooldownBar.color = new Color(1, 0.05f, 0.05f);
                    break;
            }
            specialCooldownText.color = specialCooldownBar.color;
        }
        else
        {
            specialCooldownGroup.alpha = Mathf.Lerp(specialCooldownGroup.alpha, 0, 5 * Time.deltaTime);
            specialCooldownBar.rectTransform.sizeDelta = new Vector2(0, specialCooldownBar.rectTransform.sizeDelta.y);
        }

        if (Spectating && spectatingPlayer)
        {
            player.instance.cam.Follow = spectatingPlayer.camFollow;

            string colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(Chat.Instance.usernameColors[spectatingPlayerId])}>";

            spectatingText.text = "Spectating ";
            spectatingText.text += $"{colorTag}{spectatingPlayer.usernameText.text}</color>";

        }

        //move stamina relative to player
        Vector2 camPos = Camera.main.transform.position;
        Vector2 playerPos = player.instance.rb.position;

        Vector2 targetPos = (Vector2)initialStaminaPos +
            ((camPos - playerPos) * staminaPosMult / player.instance.camSizeMultiplier);
        staminaHolder.anchoredPosition = Vector2.Lerp(staminaHolder.anchoredPosition, targetPos, Time.deltaTime * 12);
        mushBarHolder.anchoredPosition = staminaHolder.anchoredPosition + new Vector2(110, 0);

        if (InputManager.usingController)
        {
            pressFGroup.gameObject.SetActive(false);
            pressSouthGroup.gameObject.SetActive(true);

            pressSouthGroup.transform.localScale = pressFGroup.transform.localScale;
            pressSouthGroup.alpha = pressFGroup.alpha;
        }
        else
        {
            pressSouthGroup.gameObject.SetActive(false);
            pressFGroup.gameObject.SetActive(true);
        }

        if (mapMaximized && IslandMapHighlight.selectedMapIsland != null)
        {

            if (InputManager.actions["Interact"].started || InputManager.actions["Interact"].held)
            {
                RectTransform island;
                Vector3[] corners;

                if (!holdingBiomeInfoButton)
                {
                    holdingBiomeInfoButton = true;

                    island = IslandMapHighlight.selectedMapIsland.image.rectTransform;
                    corners = new Vector3[4];
                    island.GetWorldCorners(corners);

                    // Match the island's vertical center
                    biomeInfoBox.position = (corners[1] + corners[2]) * 0.5f;

                    biomeInfoName.text = biomeInfo[IslandMapHighlight.selectedMapIsland.biome].name;
                    biomeInfoName.color = biomeInfo[IslandMapHighlight.selectedMapIsland.biome].nameColor;
                    biomeInfoResources.text = biomeInfo[IslandMapHighlight.selectedMapIsland.biome].localResources;
                    biomeInfoDesc.text = biomeInfo[IslandMapHighlight.selectedMapIsland.biome].description;

                    biomeInfoResources.text = biomeInfoResources.text.Replace(@"\y", $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(1, 1, 0))}>");
                    biomeInfoResources.text = biomeInfoResources.text.Replace(@"\g", $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 1, 0))}>");
                    biomeInfoResources.text = biomeInfoResources.text.Replace(@"\b", $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.75f, 1))}>");
                    biomeInfoResources.text = biomeInfoResources.text.Replace(@"\r", $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(1, 0, 0))}>");
                    biomeInfoResources.text = biomeInfoResources.text.Replace(@"\p", $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(1, 0, 1))}>");
                    biomeInfoResources.text = biomeInfoResources.text.Replace(@"\", "</color>");
                }

                biomeInfoGroup.DOFade(1, 0.5f);

                island = IslandMapHighlight.selectedMapIsland.image.rectTransform;

                corners = new Vector3[4];
                island.GetWorldCorners(corners);

                float infoHalfWidth = biomeInfoBox.rect.width * biomeInfoBox.lossyScale.x * 0.5f;
                float padding = 5f;


                // Place to the left of the island
                biomeInfoBox.position = Vector2.Lerp(biomeInfoBox.position, new Vector3(
                    corners[1].x - infoHalfWidth - padding,
                    biomeInfoBox.position.y,
                    biomeInfoBox.position.z), Time.deltaTime * 15);

                // Keep the info box on screen vertically
                Vector3[] infoCorners = new Vector3[4];
                biomeInfoBox.GetWorldCorners(infoCorners);

                float offsetY = 0;

                // Bottom edge off-screen?
                if (infoCorners[0].y < 0)
                {
                    offsetY = -infoCorners[0].y;
                }
                // Top edge off-screen?
                else if (infoCorners[2].y > Screen.height)
                {
                    offsetY = Screen.height - infoCorners[2].y;
                }

                if (offsetY != 0)
                {
                    biomeInfoBox.position += Vector3.up * offsetY;
                }



            }
            else
            {
                holdingBiomeInfoButton = false;
                biomeInfoGroup.DOFade(0, 0.5f);
            }
        }
        else
        {
            holdingBiomeInfoButton = false;
            biomeInfoGroup.DOFade(0, 0.5f);
        }

    }

    public void ChangeMushLevel()
    {

        switch (inventory.charmEffect)
        {
            case "mush":
                mushShowTime = 3f;
                mushHolderImage.sprite = mushBarSprites[0];
                mushBarImage.sprite = mushBarSprites[1];
                break;
            case "sloth":
                mushShowTime = 2.2f;
                mushHolderImage.sprite = mushBarSprites[2];
                mushBarImage.sprite = mushBarSprites[3];
                break;
            case "wrath":
                mushShowTime = 3f;
                mushHolderImage.sprite = mushBarSprites[4];
                mushBarImage.sprite = mushBarSprites[5];
                break;
            case "envy":
                mushShowTime = 3f;
                mushHolderImage.sprite = mushBarSprites[6];
                mushBarImage.sprite = mushBarSprites[7];
                break;
        }

        if (mushLevel >= 100)
        {
            pressFGroup.transform.localScale = Vector2.zero;
            pressFGroup.alpha = 1;
            pressFGroup.transform.DOScale(Vector2.one, 0.4f).SetEase(Ease.OutBounce);
        }
        else if (pressFGroup.alpha == 1)
        {
            pressFGroup.DOFade(0, 0.2f);
        }
    }

    void PlaceMarker(Vector2 position, bool disableSound = false)
    {
        markerPlaceSound.pitch = 0.7f;
        if (!disableSound)
            markerPlaceSound.Play();


        placedMarkers.Add(new PlacedMarker(holdingMarkerType, position, holdingMarker.transform));
        holdingMarker.GetComponent<Image>().maskable = true;

        holdingMarker.GetComponent<MapMarker>().markerType = holdingMarkerType;

        holdingMarker = null;

        markerCooldown = true;
        Invoke("PlaceMarkerCooldown", 0.5f);

    }
    public void ToggleTracking()
    {
        markerPlaceSound.pitch = 1;
        if (trackingDungeons) markerPlaceSound.pitch = 0.7f;
        markerPlaceSound.Play();

        SetDungeonTracking(!trackingDungeons);
    }

    public void SetDungeonTracking(bool enabled)
    {
        trackingDungeons = enabled;

        if (enabled)
        {
            trackerImage.sprite = trackerSprites[1];
            trackerImage.transform.parent.GetComponent<Image>().DOColor(Color.magenta, 0.4f);
        }
        else
        {
            trackerImage.sprite = trackerSprites[0];
            trackerImage.transform.parent.GetComponent<Image>().DOColor(new Color(0.5f, 0.5f, 0.5f, 0.8f), 0.4f);
        }
        foreach (CanvasGroup g in dungeonCircles)
        {
            if (trackingDungeons)
                g.DOFade(1, 0.4f);
            else
                g.DOFade(0, 0.4f);
        }

    }

    void PlaceMarkerCooldown()
    {
        markerCooldown = false;
    }

    private void ZoomImage(float zoomAmount, Vector2 zoomPivot)
    {
        RectTransform rt = fullMapImage.rectTransform;

        float oldScale = rt.localScale.x;
        float newScale = oldScale * (1 + zoomAmount * mapZoomSpeed);
        newScale = Mathf.Clamp(newScale, minZoom, maxZoom);

        if (Mathf.Approximately(oldScale, newScale))
            return;

        // Resize icons
        float iconScaleFactor = oldScale / newScale;
        foreach (Transform t in allMapIcons)
        {
            t.localScale *= iconScaleFactor;
        }

        foreach (Image i in mapIslandHighlightImages)
        {
            i.pixelsPerUnitMultiplier /= iconScaleFactor;
        }

        // Apply scale
        rt.localScale = new Vector3(newScale, newScale, 1f);

        // Calculate scale ratio
        float scaleRatio = newScale / oldScale;

        Vector2 unscaledPivot = zoomPivot / oldScale;
        rt.anchoredPosition -= unscaledPivot * (newScale - oldScale);
    }


    public void OpenMap(bool open, bool disableSound = false)
    {
        if (open && gameManager.inSecondPhase) return;

        if (mapClosing) return;

        fullMap.gameObject.SetActive(open);
        minimap.enabled = !open;
        fullMapImage.rectTransform.localScale = fullMapNormalScale;
        fullMapImage.rectTransform.localPosition = Vector2.zero;

        mapGroup.interactable = open;

        //dont do anything if already in the requested state
        if (open == mapMaximized) return;

        if (!waitingForReady)
            MouseCursor.state = open ? 1 : 0;

        if (open)
        {
            inventory.instance.hotbarGroup.DOFade(0, 0.5f);

            mapMaximized = true;
            mapTransform.DOAnchorPos(Vector2.zero, mapTransitionTime);
            mapTransform.DOScale(new Vector2(mapMaxScale, mapMaxScale), mapTransitionTime);

            pageTurnSound.pitch = 1;

            foreach (Transform t in allMapIcons)
            {
                t.DOScale(t.localScale * mapNormalScale / mapMaxScale, mapTransitionTime);
                t.transform.SetParent(fullMapImage.transform);
                if (boatIcons.Contains(t)) t.SetAsFirstSibling();

            }

            foreach (PlacedMarker m in placedMarkers)
            {
                m.marker.SetAsFirstSibling();
            }

            bossMarker.SetAsFirstSibling();
            deathMarker.SetAsFirstSibling();
            respawnMarker.SetAsFirstSibling();

            markerButtonsGroup.DOFade(0.9f, mapTransitionTime);
            markerButtonsGroup.interactable = true;

            foreach (CanvasGroup g in mapIslandHighlightGroups)
            {
                g.interactable = true;
                g.blocksRaycasts = true;
                g.DOFade(1, 1);
            }

            Dictionary<item, int> counts = inventory.instance.GetItemCounts(true);
            if (!firstMapOpen && counts.ContainsKey(soulTrackerItem))
            {
                firstMapOpen = true;
                SetDungeonTracking(true);

            }

        }
        else
        {
            inventory.instance.hotbarGroup.DOFade(1, 0.5f);

            mapClosing = true;

            if (holdingMarker)
            {
                markerCounts[holdingMarkerType]--;
                Destroy(holdingMarker);
            }
            mapTransform.DOAnchorPos(mapDefaultPos, mapTransitionTime);
            mapTransform.DOScale(new Vector2(mapNormalScale, mapNormalScale), mapTransitionTime);

            pageTurnSound.pitch = 0.88f;

            foreach (Transform t in allMapIcons)
            {
                t.DOScale(Vector2.one, mapTransitionTime);
                t.transform.SetParent(minimap.transform);
                if (boatIcons.Contains(t)) t.SetAsFirstSibling();
            }

            foreach (Image i in mapIslandHighlightImages)
            {
                i.pixelsPerUnitMultiplier = islandHighlightPPU;
            }

            foreach (PlacedMarker m in placedMarkers)
            {
                m.marker.SetAsFirstSibling();
            }

            bossMarker.SetAsFirstSibling();
            deathMarker.SetAsFirstSibling();
            respawnMarker.SetAsFirstSibling();

            markerButtonsGroup.DOFade(0, mapTransitionTime);
            markerButtonsGroup.interactable = false;

            StartCoroutine(_CloseMapDelay());

            foreach (CanvasGroup g in mapIslandHighlightGroups)
            {
                g.interactable = false;
                g.blocksRaycasts = false;
                g.DOFade(0, 1);
            }
        }
        if (!disableSound)
            pageTurnSound.Play();

    }

    IEnumerator _CloseMapDelay()
    {
        yield return new WaitForSeconds(0.1f);
        mapClosing = false;
        mapMaximized = false;
    }

    public void GrabMarker(int markerType)
    {
        if (!mapMaximized) return;

        markerPlaceSound.pitch = 1;
        if (markerCounts[markerType] >= 5 || holdingMarker) markerPlaceSound.pitch = 0.6f;

        markerPlaceSound.Play();
        GrabMapMarker(markerType);
    }

    void GrabMapMarker(int markerType)
    {
        if (holdingMarker)
        {
            if (holdingMarkerType == markerType)
            {
                markerCounts[holdingMarkerType]--;
                Destroy(holdingMarker);
            }
            return;

        }

        if (markerCounts[markerType] >= 5) return;

        holdingMarker = Instantiate(placeableMarkerPrefabs[markerType], markers[0].parent);
        holdingMarker.transform.SetAsFirstSibling();

        if (mapMaximized)
        {
            holdingMarker.transform.localScale *= mapNormalScale / mapMaxScale;
            holdingMarker.transform.localScale /= fullMap.localScale.x;
        }
        holdingMarkerType = markerType;

        allMapIcons.Add(holdingMarker.transform);

        markerCounts[holdingMarkerType]++;
    }


    public void OnClickRespawn()
    {
        if (!gameManager.inBoss && gameManager.instance.difficulty != 2)
        {
            playerHealth.instance.PlayerRespawn(playerHealth.instance.view.Id);
        }
        //spectate
        else
        {
            deadScreen.DOFade(0, 1);
            Spectating = true;
            spectateGroup.DOFade(1, 1);
            spectateGroup.interactable = true;
            spectateGroup.blocksRaycasts = true;

            spectatingPlayerId = 0;
            for (int i = 0; i < playerSpawner.instance.players.Length; i++)
            {
                if (playerSpawner.instance.players[i] && playerSpawner.instance.players[i] != player.instance && playerSpawner.instance.players[i].health.currentHealth > 0)
                {
                    spectatingPlayerId = i;
                    spectatingPlayer = playerSpawner.instance.players[i];
                    break;
                }

            }

            inventory.instance.hotbarGroup.DOFade(0, 1);
        }
    }

    public void PermaDead()
    {
        if (gameManager.instance.difficulty != 2)
        {
            permaDeathButtonText.text = "Reload Save";
        }
        else
        {
            permaDeathButtonText.text = "Exit to Menu";
        }

        permaDeathGroup.DOFade(1, 2);
        permaDeathGroup.interactable = true;
        permaDeathGroup.blocksRaycasts = true;

        if (SingletonRunner.runner.IsSharedModeMasterClient || gameManager.instance.difficulty == 2)
        {
            Invoke("EnableReloadButton", 3);
        }

    }

    void EnableReloadButton()
    {
        reloadSaveButtonGroup.interactable = true;
        reloadSaveButtonGroup.blocksRaycasts = true;
    }

    public void ReloadSave()
    {
        //default
        if (gameManager.instance.difficulty != 2)
            gameManager.instance.ReloadSave();
        //dementia
        else
            StartCoroutine(pauseScreen.instance._LoadMenu());

    }


    public void SpectateNext()
    {
        if (spectatingPlayerId == playerSpawner.instance.players.Length - 1) spectatingPlayerId = 0;

        for (int i = spectatingPlayerId + 1; i < playerSpawner.instance.players.Length; i++)
        {
            if (playerSpawner.instance.players[i] && playerSpawner.instance.players[i] != player.instance && playerSpawner.instance.players[i].health.currentHealth > 0)
            {
                spectatingPlayerId = i;
                spectatingPlayer = playerSpawner.instance.players[i];
                break;
            }

        }
    }

    public void CreateFullMap()
    {
        fullMapTex = CreateSolidColorTexture((int)(fullMapScale * 99.01f), (int)(fullMapScale * 99.01f), waterMapColor);
        fullMapTex.filterMode = FilterMode.Point;

        foreach (Island island in MapGenerator.instance.islands)
        {
            if (island.biome == (int)MapGenerator.biome.vale) continue;

            OverlayTextureAtPosition((Texture2D)fullMapTex, (Texture2D)island.tex, island.center.x, island.center.y);

            RectTransform t = Instantiate(mapIslandHighlightPrefab, fullMap).GetComponent<RectTransform>();
            t.SetAsFirstSibling();
            float scalePadding = 1.5f;
            t.sizeDelta = new Vector2(island.size / fullMapScale + scalePadding, island.size / fullMapScale + scalePadding);
            t.localPosition = (Vector2)island.center / (fullMapScale - 0.0001f);

            mapIslandHighlightGroups.Add(t.GetComponent<CanvasGroup>());
            mapIslandHighlightImages.Add(t.GetComponent<Image>());
            islandHighlightPPU = t.GetComponent<Image>().pixelsPerUnitMultiplier;
            t.GetComponent<IslandMapHighlight>().biome = island.biome;
        }
        fullMapImage.material = new Material(Shader.Find("UI/Default"));
        fullMapImage.material.mainTexture = fullMapTex;

    }

    void OverlayTextureAtPosition(Texture2D baseTexture, Texture2D overlayTexture, int offsetX, int offsetY)
    {
        if (baseTexture == null || overlayTexture == null)
        {
            Debug.LogError("Base texture or overlay texture is null.");
            return;
        }

        // Calculate the starting position based on the center
        int startX = baseTexture.width / 2 - overlayTexture.width / 2 + offsetX;
        int startY = baseTexture.height / 2 - overlayTexture.height / 2 + offsetY;

        // Apply overlay texture
        for (int x = 0; x < overlayTexture.width; x++)
        {
            for (int y = 0; y < overlayTexture.height; y++)
            {
                Color overlayPixel = overlayTexture.GetPixel(x, y);
                baseTexture.SetPixel(startX + x, startY + y, overlayPixel);
            }
        }

        // Apply changes
        baseTexture.Apply();
    }

    Texture2D CreateSolidColorTexture(int width, int height, Color color)
    {
        // Create a new Texture2D
        Texture2D texture = new Texture2D(width, height);

        // Set all pixels to the specified color
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);

        // Apply changes
        texture.Apply();

        return texture;
    }

    public IEnumerator _WaitForReadyBoss()
    {
        noReturnGroup.DOFade(0.9f, 1);
        noReturnGroup.interactable = true;
        noReturnGroup.blocksRaycasts = true;
        waitingForReady = true;
        MouseCursor.state = 1;
        continueButtonText.text = "Ready";

        int playerCount = playerSpawner.playerCountInGame;

        if (gameManager.instance.difficulty == 2) playerCount = playerSpawner.playerCountNotDead;


        while (playersReady < playerCount)
        {
            playersReadyText.text = playersReady + "/" + playerCount + " players";

            if (TileEntity.playersOnPuddle < playerCount)
            {
                MouseCursor.state = 0;
                waitingForReady = false;
                CancelBoss();
                yield break;
            }

            OpenMap(false, true);

            yield return null;
        }
        player.instance.inCutscene = true;
        playersReadyText.text = "Starting...";
        DataPersistanceManager.instance.SaveGame();
        yield return new WaitForSeconds(0.5f);
        noReturnGroup.DOFade(0, 0.8f);
        noReturnGroup.interactable = false;
        noReturnGroup.blocksRaycasts = false;
        yield return new WaitForSeconds(1.1f);
        waitingForReady = false;

        TileEntity.playersOnPuddle = 0;
        gameManager.instance.placedTiles.Find(t => t.position == new Vector2(12, -2) && t.Item == gameManager.instance.itemDictionary["ominous_flower"]).PlacedTile.sprite.transform.DOScaleY(0, 1.2f);
        gameManager.instance.placedTiles.Find(t => t.position == new Vector2(12, -2) && t.Item == gameManager.instance.itemDictionary["ominous_flower"]).PlacedTile.sprite.transform.DOLocalMoveY(-0.37f, 1.2f);
        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            Boss.instance.EnterAnim();
            yield return new WaitForSeconds(4);
            //gameManager.instance.RemoveTile(new Vector2(12, -2), gameManager.instance.itemDictionary["ominous_flower"], false);
        }

    }

    public void OnClickReady()
    {
        if (!ready)
        {
            gameManager.instance.ChangeBossReady(true);
            ready = true;
            continueButtonText.text = "Cancel";
        }
        else
        {
            gameManager.instance.ChangeBossReady(false);
            ready = false;
            continueButtonText.text = "Ready";
        }
    }

    public void CancelBoss()
    {
        waitingForReady = false;
        if (ready)
        {
            gameManager.instance.ChangeBossReady(false);
            ready = false;
        }
        noReturnGroup.DOFade(0, 1);
        noReturnGroup.interactable = false;
        noReturnGroup.blocksRaycasts = false;
    }

    public void SetPermaDeathScreenItems()
    {
        itemsLostHolder.anchoredPosition = new Vector2(0, itemsLostHolder.anchoredPosition.y);
        List<item> uniquesList = new List<item>();

        List<item> allItems = new List<item>();
        foreach (item i in ValeManager.instance.GetSoulItems())
        {
            if (allItems.Contains(i)) continue;
            allItems.Add(i);
        }
        foreach (inventorySlot slot in ValeManager.instance.valeInventoryItems)
        {
            if (allItems.Contains(slot.itemInSlot)) continue;
            allItems.Add(slot.itemInSlot);
        }

        foreach (item i in allItems)
        {
            if (!i) continue;
            itemsLostHolder.gameObject.SetActive(true);

            if (i.uniqueType != item.UniqueType.none)
            {
                uniquesList.Add(i);
                continue;
            }

            Image image = Instantiate(lostItemImagePrefab, itemsLostHolder.position + new Vector3(UnityEngine.Random.Range(-50, 50)
                , UnityEngine.Random.Range(-20, 20)), Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360)), itemsLostHolder).GetComponent<Image>();

            image.preserveAspect = true;
            image.sprite = i.overworldSprite;
            image.SetNativeSize();
            image.rectTransform.sizeDelta /= new Vector2(2, 2);
            image.rectTransform.SetAsFirstSibling();

        }
        if (uniquesList.Count > 0)
        {
            itemsLostHolder.anchoredPosition = new Vector2(-130, itemsLostHolder.anchoredPosition.y);
            uniquesLostHolder.anchoredPosition = new Vector2(130, itemsLostHolder.anchoredPosition.y);
            uniquesLostHolder.gameObject.SetActive(true);

            foreach (item i in uniquesList)
            {
                Image image = Instantiate(lostItemImagePrefab, uniquesLostHolder.position + new Vector3(UnityEngine.Random.Range(-50, 50)
                , UnityEngine.Random.Range(-20, 20)), Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360)), uniquesLostHolder).GetComponent<Image>();

                image.preserveAspect = true;
                image.sprite = i.overworldSprite;
                image.SetNativeSize();
                image.rectTransform.sizeDelta /= new Vector2(2, 2);
                image.rectTransform.SetAsFirstSibling();
            }

        }


    }


    public void SaveData(GameData data)
    {
        data.placedMapMarkers = placedMarkers.ToArray();
    }
    public void LoadData(GameData data)
    {
        if (data.placedMapMarkers == null) return;

        foreach (PlacedMarker marker in data.placedMapMarkers)
        {
            GrabMapMarker(marker.type);
            PlaceMarker(marker.mapPos, true);
        }
    }


}
[Serializable]
public class PlacedMarker
{
    public int type;
    public Vector2 mapPos;
    public Transform marker;

    public PlacedMarker(int type, Vector2 mapPos, Transform marker)
    {
        this.type = type;
        this.mapPos = mapPos;
        this.marker = marker;
    }

}

[Serializable]
public class BiomeInfo
{
    public string name;
    [TextArea()] public string description;
    public Color nameColor = Color.green;
    [TextArea()]
    public string localResources;

}
using DG.Tweening;
using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class inventory : NetworkBehaviour, IDataPersistance
{
    public static inventory instance;
    public const float itemIconScale = 0.46f;
    public const int totalSlotCount = 27;
    //public item[] items = new item[36];
    public inventorySlot[] slots;
    public bool inventoryOpen;
    public Animator anim;
    bool canOpenInventory = true;
    public Animator playerAnim;
    public GameObject dropItemPrefab;
    [SerializeField] CanvasGroup slotsGroup;

    public GameObject itemDragIcon;
    public TextMeshProUGUI itemDragCountText;

    public float dragSpringStrength;
    public float dragDamping;

    public LayerMask dropRaycastLayers;
    public float dropCooldown = 0.1f;
    float dropTimer;

    public CanvasGroup itemNameBoxGroup;
    public Transform ItemNameBox;
    public bool showingName;
    public TextMeshProUGUI ItemNameText, itemDesc, itemType, itemRecipe;
    public RectTransform itemNameBoxRect, itemNameTextRect;
    public Transform iconParent;
    [SerializeField] Image itemBoxImage;
    [SerializeField] Sprite itemBoxSprite, uniqueBoxSprite, repairBoxSprite;

    public enum interfaceType
    {
        normal,
        furnace,
        vehicleStation,
        chest,
        crafting,
        speedboat,
        purifier,
        battleship,
        anvil
    }

    [SerializeField] Vector2 itemNameBoxOffset;
    public GameObject normalUI;
    public Image playerImage;
    public AudioSource pickupSound, repairSound, inventoryOpenSound;
    public Slider mouseDurabilityBar;
    public Image mouseBrokenSlash;
    public Image mouseTierHighlight;
    [SerializeField] Image mouseDurabilityImage;
    public event EventHandler onCloseInventory;
    [SerializeField] Vector2 noItemDropXRange;
    Camera cam;
    public CanvasGroup hotbarGroup;

    bool appiedDifficultySettings;

    public Button pyroButton;
    public Image pyroImage;
    Vector2 itemDragVelocity;

    [Header("Crafting")]
    public GameObject crafingUI;
    public float scrollSpeedController = 500;
    public ScrollRect craftingScrollRect;
    public GameObject CraftingButtonPrefab;
    public List<recipe> allRecipes = new List<recipe>();
    public Transform[] holders;
    public Button[] categoryButtons;
    public Image[] categoryImages;
    [SerializeField] float[] sliderPositions;
    int enabledCategory;
    public Scrollbar slider;
    [HideInInspector]
    public List<craftingButton> craftingButtons = new List<craftingButton>();
    craftingButton currentDisplayingRecipe;
    public Color canCraftTextColor, cantCraftTextColor;
    public RectTransform content;
    public TextMeshProUGUI craftingTitle;

    [Header("Furnace")]
    public GameObject furnaceUI, purifierUI;
    public bool inFurnace;
    public inventorySlot furnaceIngr, furnaceFuel, furnaceResult;
    public Image fuelBar, smeltBar;

    public inventorySlot purifierIngr, purifierFuel, purifierResult;
    public Image purifierFuelBar, purifierSmeltBar;

    [Header("Vehicle Station")]
    public GameObject vehicleStationUi;
    public GameObject boatPrefab;
    public Vector3 boatSpawnPos;
    public float boatSpawnScale;

    public boatType[] boats;
    public int currentBoat;

    public float boatButtonUpdateSpeed = 0.1f;
    public Image boatCraftButtonImage;
    public Color canCraftColor, cantCraftColor;
    public TextMeshProUGUI boatRecipeText, boatDescText, boatNameText;
    public Image boatIcon;

    public AudioSource pageTurnSound;

    [Header("Chest")]
    public GameObject chestUi;
    public inventorySlot[] chestSlots;
    public TextMeshProUGUI chestText;
    public Image chestBg;
    public Color lootChestBgColor, lootChestTextColor;
    Color chestBgColor, chestTextColor;

    [Header("Speedboat")]
    public GameObject speedboatUI, battleshipUI;
    public inventorySlot speedboatFuelSlot;
    public inventorySlot battleshipFuelSlot;
    public float speedboatFuelTime;
    public Image boatFuelBar, reactorFuelBar1, reactorFuelBar2;

    [Header("Armor")]
    public inventorySlot headSlot;
    public inventorySlot chestplateSlot, legsSlot, charmSlot;
    public Image headImage, chestplateImage, legsImage;
    public float mitigationPercent;
    public TextMeshProUGUI mitigationText;
    public AudioSource armorPutOnSound;
    public static string charmEffect;

    [Header("Stats")]

    public Animator statsAnim;
    public bool statsOpen;
    public int[] statAmounts = new int[5];
    public int[] charmAddStatAmounts = new int[5];
    public Image[] statBarFills;
    public AudioSource upgradeSound;
    [SerializeField] AudioClip upgradeClip, maxUpgradeClip;
    [SerializeField] TextMeshProUGUI[] charmAddStatsTexts;

    public float maxStrengthMult = 0.5f;
    public float maxSpeedMult = 0.15f;
    public float maxStaminaMult = 0.6f;
    public float maxMiningMult = 0.4f;
    public float maxHealthMult = 0.4f;
    public const float cleaveDamageMultiplier = 1.75f;

    public float specialCooldownTime;
    public string specialCooldownType;
    public float specialInitialCooldown;

    [SerializeField] CanvasGroup newAbilityGroup, statsGroup;
    bool inNewAbilityMenu;
    [SerializeField] Button statsBackButton;
    [SerializeField] Image abilityTutorialImage;
    [SerializeField] Sprite[] tutorialSprites = new Sprite[5];
    [TextArea(3, 5)]
    [SerializeField] string[] tutorialDescriptions = new string[5];
    [SerializeField] TextMeshProUGUI tutorialDesc;
    [HideInInspector]
    public bool maxStat;
    [SerializeField] GameObject newAbilityBackButton;
    [SerializeField] TextMeshProUGUI[] shardCounts = new TextMeshProUGUI[5];
    public int soulRepairAmount;
    private inventorySlot previousSelectedSlot;
    public const float itemDragLerpSpeed = 16;
    [SerializeField] AudioSource openStatsSound, craftingTabSwitchSound;
    private float categorySwitchCd;
    private bool disableSwitchCategory;
    private bool pyroImageEnabled;

    [Header("Anvil")]
    public GameObject anvilUI;

    public inventorySlot anvilToolSlot, anvilMaterialSlot, anvilOutputSlot;
    public item upgradeTemplateItem;
    public Image anvilArrow;

    public List<inventorySlot> extraSlotsToSave = new List<inventorySlot>();
    public AudioSource anvilRepairSound;
    private int anvilRepairAmount;
    public TextMeshProUGUI repairRequireText;

    void Awake()
    {
        instance = this;

    }

    private void Start()
    {
        chestTextColor = chestText.color;
        chestBgColor = chestBg.color;

        newAbilityGroup.alpha = 0;
        newAbilityGroup.interactable = false;
        newAbilityGroup.blocksRaycasts = false;
        inNewAbilityMenu = false;
        statsGroup.interactable = false;
        statsGroup.blocksRaycasts = false;

        transform.GetChild(0).gameObject.SetActive(true);

        mouseDurabilityBar.gameObject.SetActive(false);
        mouseTierHighlight.gameObject.SetActive(false);
        InvokeRepeating("CheckCanMakeBoat", boatButtonUpdateSpeed, boatButtonUpdateSpeed);

        cam = Camera.main;
        newAbilityBackButton.SetActive(false);

        CloseInventory(true);

        itemNameBoxGroup.alpha = 0;
    }

    void FixStupidCraftingUiBug()
    {
        content.localPosition = new Vector2(0, content.localPosition.y);
        slider.value = 0;
    }

    public int GetStatAmount(int stat)
    {
        if (ValeManager.instance.lostSoul) return 0;
        if (player.instance.currentIsland.biome == (int)MapGenerator.biome.vale) return statAmounts[stat];

        return statAmounts[stat] + charmAddStatAmounts[stat];
    }

    public void CreateRecipeButtons()
    {
        sliderPositions = new float[holders.Length];
        sliderPositions[0] = 0;

        foreach (recipe Recipe in gameManager.instance.recipeDictionary)
        {
            Transform parent;

            item.itemType itemType = Recipe.result.Item.type;

            if (Recipe.result.Item.overrideRecipeLocation)
            {
                itemType = Recipe.result.Item.overrideRecipeItemType;
            }

            if (itemType == item.itemType.tool || itemType == item.itemType.remover)
            {
                parent = holders[0];
            }
            else if (itemType == item.itemType.armorLegs || itemType == item.itemType.armorChest || itemType == item.itemType.armorHead)
            {
                parent = holders[1];
            }
            else if (itemType == item.itemType.block)
            {
                parent = holders[2];
            }
            else if (itemType == item.itemType.farmRecipe)
            {
                parent = holders[3];
            }
            else if (itemType == item.itemType.charm)
            {
                parent = holders[4];
            }
            else
            {
                parent = holders[5];
            }


            GameObject newButton = Instantiate(CraftingButtonPrefab, parent);
            craftingButton cb = newButton.GetComponent<craftingButton>();
            cb.Recipe = Recipe;
            newButton.transform.GetChild(0).GetComponent<Image>().sprite = Recipe.result.Item.overworldSprite;

            if (Recipe.result.Item.type == item.itemType.block)
                newButton.transform.GetChild(0).localScale *= 0.85f;

            craftingButtons.Add(cb);
        }

        EnableCategory(0);
        content.localPosition = new Vector2(0, content.localPosition.y);
    }


    void canOpen(int CanOpen)
    {
        if (CanOpen == 1)
        {
            canOpenInventory = true;
        }
        else
        {
            canOpenInventory = false;
        }
    }


    public void EnableCategory(int index)
    {
        if (index != enabledCategory)
            sliderPositions[enabledCategory] = content.localPosition.x;

        enabledCategory = index;
        for (int i = 0; i < holders.Length; i++)
        {
            if (i == index)
            {
                holders[i].gameObject.SetActive(true);
                categoryImages[i].DOFade(1, 0.1f);
            }
            else
            {
                holders[i].gameObject.SetActive(false);
                categoryImages[i].DOFade(0, 0.1f);
            }

        }

        foreach (craftingButton cb in craftingButtons)
        {
            cb.CheckCanCraft();
        }

        content.sizeDelta = new Vector2((69 * holders[index].childCount) + 10, content.sizeDelta.y);
        craftingTitle.text = holders[index].name;

        content.localPosition = new Vector2(sliderPositions[index], content.localPosition.y);

    }

    public void OpenInventory(interfaceType type)
    {
        if (playerHealth.instance.dead || pauseScreen.instance.paused || Chat.Instance.chatOpen || player.instance.inCutscene
            || (player.instance.inBoatAltAction && player.instance.currentAltAction == 1) || player.instance.pilotingBoat
            || menu.instance.mapMaximized)
            return;

        inventoryOpen = true;
        MouseCursor.state = 1;

        normalUI.SetActive(false);
        crafingUI.SetActive(false);
        furnaceUI.SetActive(false);
        vehicleStationUi.SetActive(false);
        chestUi.SetActive(false);
        speedboatUI.SetActive(false);
        purifierUI.SetActive(false);
        battleshipUI.SetActive(false);
        anvilUI.SetActive(false);

        inventoryOpenSound.pitch = 1;
        inventoryOpenSound.Play();

        switch (type)
        {
            case interfaceType.normal:
                normalUI.SetActive(true);
                playerImage.sprite = playerSpawner.instance.playerSprites[DataPersistanceManager.instance.localSkin];
                break;
            case interfaceType.furnace:
                furnaceUI.SetActive(true);
                break;
            case interfaceType.vehicleStation:
                vehicleStationUi.SetActive(true);
                UpdateVehicleStationUI();

                if (InputManager.actions["Scroll Right"].held ||
                    InputManager.actions["Scroll Left"].held)
                    disableSwitchCategory = true;

                break;
            case interfaceType.chest:
                chestUi.SetActive(true);

                chestBg.color = chestBgColor;
                chestText.color = chestTextColor;
                chestText.text = "Storage Chest";

                if (DungeonChest.currentDungeonChest)
                {
                    chestBg.color = lootChestBgColor;
                    chestText.color = lootChestTextColor;
                    chestText.text = "Ominous Chest";
                }

                break;
            case interfaceType.crafting:
                crafingUI.SetActive(true);
                foreach (craftingButton c in craftingButtons)
                {
                    c.CheckCanCraft();
                }
                EnableCategory(enabledCategory);

                if (InputManager.actions["Scroll Right"].held ||
                    InputManager.actions["Scroll Left"].held)
                    disableSwitchCategory = true;

                break;
            case interfaceType.speedboat:
                speedboatUI.SetActive(true);
                break;
            case interfaceType.battleship:
                battleshipUI.SetActive(true);
                break;
            case interfaceType.purifier:
                purifierUI.SetActive(true);
                break;
            case interfaceType.anvil:
                anvilArrow.fillAmount = 0;
                anvilUI.SetActive(true);
                break;

        }

        //foreach (inventorySlot slot in slots)
        //{
        //slot.enabled = true;
        //}

    }

    public void CloseInventory(bool disableSound = false)
    {
        inventoryOpenSound.pitch = 0.82f;
        if (!disableSound)
            inventoryOpenSound.Play();

        MouseCursor.state = 0;

        if (TileEntity.currentFurnace)
        {
            inFurnace = false;
            TileEntity.currentFurnace.CloseFurnace();
        }
        if (Boat.currentSpeedboatFueling)
        {
            Boat.currentSpeedboatFueling.CloseFuelUI();
            Boat.currentSpeedboatFueling = null;
        }


        if (TileEntity.currentChest)
        {
            TileEntity.currentChest.CloseChest();

            foreach (inventorySlot slot in chestSlots)
            {
                if (slot.anim)
                    slot.anim.SetBool("Selected", false);
            }
        }
        if (DungeonChest.currentDungeonChest)
        {
            DungeonChest.currentDungeonChest.CloseChest();

            foreach (inventorySlot slot in chestSlots)
            {
                if (slot.anim)
                    slot.anim.SetBool("Selected", false);
            }
        }


        if (crafingUI && crafingUI.activeSelf && enabledCategory < sliderPositions.Length)
            sliderPositions[enabledCategory] = content.localPosition.x;

        inventoryOpen = false;

        if (inventorySlot.itemInMouse)
        {
            AddItem(inventorySlot.itemInMouse, inventorySlot.itemCountInMouse, inventorySlot.durabilityInMouse, inventorySlot.itemDataInMouse);
            removeMouseItem();
        }

        foreach (inventorySlot slot in slots)
        {
            if (!slot) return;

            if (slot.anim)
                slot.anim.SetBool("Selected", false);
        }

        inventorySlot.selectedSlot = null;
        craftingButton.selectedRecipe = null;
        onCloseInventory?.Invoke(this, EventArgs.Empty);

    }


    // Update is called once per frame
    void Update()
    {
        //disable slots when on dementia mode
        if (!appiedDifficultySettings && gameManager.instance.difficulty > 0)
        {
            appiedDifficultySettings = true;

            if (gameManager.instance.difficulty == 2)
            {
                inventorySlot[] newSlots = new inventorySlot[18];
                for (int i = 0; i < slots.Length; i++)
                {
                    if (i < 18)
                    {
                        newSlots[i] = slots[i];
                    }
                    else
                    {
                        slots[i].gameObject.SetActive(false);
                    }
                }
                slots = newSlots;
                slots[9].transform.parent.transform.position = new Vector3(slots[9].transform.parent.transform.position.x, slots[9].transform.parent.transform.position.y - 30);
            }

        }

        if (charmSlot.itemInSlot && player.instance && player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
        {
            charmEffect = charmSlot.itemInSlot.charmEffect;
        }
        else
        {
            charmEffect = "";
        }

        anim.SetBool("Show", inventoryOpen);

        slotsGroup.interactable = inventoryOpen;
        slotsGroup.blocksRaycasts = inventoryOpen;

        float mitigation = 0;

        //armor display in inventory
        if (player.instance && !ValeManager.instance.lostSoul && player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
        {
            if (headSlot.itemInSlot)
            {
                headImage.sprite = headSlot.itemInSlot.armorSpriteSheet[player.instance.skinIndex].armorSprites[0];
                headImage.enabled = true;
                mitigation += (headSlot.itemInSlot.armorMitigationPercent * GetModifierInArmorSlots("prot", headSlot));
            }
            else
            {
                headImage.enabled = false;
            }

            if (chestplateSlot.itemInSlot)
            {
                chestplateImage.sprite = chestplateSlot.itemInSlot.armorSpriteSheet[player.instance.skinIndex].armorSprites[0];
                chestplateImage.enabled = true;
                mitigation += chestplateSlot.itemInSlot.armorMitigationPercent * GetModifierInArmorSlots("prot", chestplateSlot);
            }
            else
            {
                chestplateImage.enabled = false;
            }

            if (legsSlot.itemInSlot)
            {
                legsImage.sprite = legsSlot.itemInSlot.armorSpriteSheet[player.instance.skinIndex].armorSprites[0];
                legsImage.enabled = true;
                mitigation += legsSlot.itemInSlot.armorMitigationPercent * GetModifierInArmorSlots("prot", legsSlot);
            }
            else
            {
                legsImage.enabled = false;
            }

            if (!inventoryOpen && !pauseScreen.instance.paused && playerHealth.instance.currentHealth > 0
                && !player.instance.pilotingBoat && !player.instance.inBoatAltAction && !Chat.Instance.chatOpen && !gameManager.instance.writingSign && !menu.instance.mapMaximized
                && !HandMove.instance.eating && !playerHealth.instance.beingKnocked)
            {
                //special ability
                if (InputManager.actions["Ability"].started && specialCooldownTime <= 0 && !gridDisplay.buildMode)
                {
                    if (statAmounts[0] == 10 && !player.instance.currentBoat)
                    {
                        specialInitialCooldown = 7f * DungeonGenerator.instance.GetModifier("cooldown");
                        specialCooldownType = "Resource Tracker";
                        StartCoroutine(player.instance._OreLocator());

                    }
                    else if (statAmounts[1] == 10 && playerWater.instance.waterValue == 0 && player.instance.axisInput != Vector3.zero && !playerWater.instance.outOfStamina)
                    {
                        gameManager.endScreenStats["abilities"]++;

                        specialInitialCooldown = 4f * DungeonGenerator.instance.GetModifier("cooldown");
                        specialCooldownTime = specialInitialCooldown;
                        specialCooldownType = "Sidestep";
                        player.instance.Dash();
                        playerWater.instance.stamina -= 3f;
                    }
                    else if (statAmounts[2] == 10 && !player.instance.currentBoat)
                    {
                        specialInitialCooldown = 8 * DungeonGenerator.instance.GetModifier("cooldown");
                        specialCooldownType = "Hypersprint";
                        StartCoroutine(player.instance._HyperDash());
                    }
                    else if (statAmounts[3] == 10)
                    {
                        gameManager.endScreenStats["abilities"]++;

                        specialInitialCooldown = 25 * DungeonGenerator.instance.GetModifier("cooldown");
                        specialCooldownTime = specialInitialCooldown;
                        specialCooldownType = "Divine Guard";
                        player.instance.DivineGuard();

                    }
                    else if (statAmounts[4] == 10)
                    {
                        item i = inventorySlot.equippedSlot.itemInSlot;

                        if (i && i.type == item.itemType.tool && HandMove.instance.canScroll && playerWater.instance.waterValue != 2)
                        {
                            specialInitialCooldown = 10 * DungeonGenerator.instance.GetModifier("cooldown");
                            specialCooldownType = "Vortex Cleave";
                            specialCooldownTime = specialInitialCooldown;
                            HandMove.instance.VortexCleave();
                        }


                    }

                }

                if (InputManager.actions["Charm"].started && !gridDisplay.buildMode)
                {
                    switch (charmEffect)
                    {
                        case "mush":
                            if (menu.instance.mushLevel >= 100)
                            {
                                player.instance.RPC_SporeExplosion();
                                menu.instance.mushLevel = 0;
                                menu.instance.ChangeMushLevel();

                            }
                            break;
                        case "sloth":
                            if (menu.instance.mushLevel >= 100)
                            {
                                player.instance.RPC_SlothVortex();
                                menu.instance.mushLevel = 0;
                                menu.instance.ChangeMushLevel();

                            }
                            break;
                        case "wrath":
                            if (menu.instance.mushLevel >= 100)
                            {
                                player.instance.RPC_WrathCounter();
                                menu.instance.mushLevel = 0;
                                menu.instance.ChangeMushLevel();

                            }
                            break;
                    }
                }


            }


            charmAddStatAmounts = new int[5];

            if (inventoryOpen)
            {
                for (int i = 0; i < charmAddStatsTexts.Length; i++)
                {
                    charmAddStatsTexts[i].text = "";
                }

                if (crafingUI.activeInHierarchy && InputManager.usingController)
                {
                    float delta = 0f;

                    ScrollRect scrollRect = craftingScrollRect;

                    if (InputManager.actions["Tab Right"].held || InputManager.actions["Tab Right"].started)
                        delta += scrollSpeedController * Time.deltaTime;

                    if (InputManager.actions["Tab Left"].held || InputManager.actions["Tab Left"].started)
                        delta -= scrollSpeedController * Time.deltaTime;

                    if (delta != 0f)
                    {
                        float contentWidth = scrollRect.content.rect.width;
                        float viewportWidth = scrollRect.viewport.rect.width;

                        if (contentWidth > viewportWidth)
                        {
                            float scrollable = contentWidth - viewportWidth;
                            float normalizedDelta = delta / scrollable;

                            scrollRect.horizontalNormalizedPosition =
                                Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + normalizedDelta);
                        }
                    }
                }

                if (player.instance.fireCircleEquipped)
                {
                    pyroButton.gameObject.SetActive(true);

                    if (player.instance.fireCircleEnabled)
                    {
                        if (!pyroImageEnabled)
                        {
                            pyroImage.DOColor(new Color(1, 0.6f, 0, 1), 0.3f);
                            pyroButton.image.DOColor(new Color(0.7f, 0, 0), 0.3f);

                            pyroImageEnabled = true;
                        }
                    }
                    else if (pyroImageEnabled)
                    {
                        pyroImage.DOColor(new Color(0.3f, 0.3f, 0.3f, 0.5f), 0.3f);
                        pyroButton.image.DOColor(new Color(0.4f, 0, 0), 0.3f);

                        pyroImageEnabled = false;
                    }

                }
                else
                {
                    pyroButton.gameObject.SetActive(false);
                }

                if (anvilUI.activeInHierarchy)
                {
                    UpdateAnvilUI();
                }

            }

            if (charmSlot.itemInSlot)
            {
                for (int i = 0; i < charmSlot.itemInSlot.charmStatsChanged.Length; i++)
                {
                    charmAddStatAmounts[(int)charmSlot.itemInSlot.charmStatsChanged[i]]
                        = charmSlot.itemInSlot.charmStatsChangedAmount[i];

                    if (!inventoryOpen) continue;

                    if (charmSlot.itemInSlot.charmStatsChangedAmount[i] > 0)
                    {
                        charmAddStatsTexts[(int)charmSlot.itemInSlot.charmStatsChanged[i]].text = "+" + charmSlot.itemInSlot.charmStatsChangedAmount[i];
                        charmAddStatsTexts[(int)charmSlot.itemInSlot.charmStatsChanged[i]].color = Color.green;
                    }
                    else if (charmSlot.itemInSlot.charmStatsChangedAmount[i] < 0)
                    {
                        charmAddStatsTexts[(int)charmSlot.itemInSlot.charmStatsChanged[i]].text = "" + charmSlot.itemInSlot.charmStatsChangedAmount[i];
                        charmAddStatsTexts[(int)charmSlot.itemInSlot.charmStatsChanged[i]].color = Color.red;
                    }
                }

            }


        }
        mitigation = Mathf.Clamp(mitigation, 0, 75);

        mitigationPercent = mitigation;
        mitigationText.text = "Protection: " + (Mathf.Round(mitigationPercent * 10) / 10) + "%";


        if (dropTimer > 0)
        {
            dropTimer -= Time.deltaTime;
        }
        if (inventorySlot.selectedSlot && inventorySlot.selectedSlot.itemInSlot)
        {
            SetItemNameBoxText(inventorySlot.selectedSlot.itemInSlot, inventorySlot.selectedSlot.durability, inventorySlot.selectedSlot.itemData, inventorySlot.selectedSlot.transform.position, false);
            if (!showingName || previousSelectedSlot != inventorySlot.selectedSlot)
            {
                showingName = true;
                itemNameBoxGroup.alpha = 0;
                itemNameBoxGroup.DOFade(0.96f, 0.3f);
                previousSelectedSlot = inventorySlot.selectedSlot;
            }
            itemRecipe.gameObject.SetActive(false);

        }
        else
        {
            if (craftingButton.selectedRecipe != null && inventoryOpen)
            {
                showingName = true;
                if (currentDisplayingRecipe != craftingButton.selectedRecipe)
                {
                    currentDisplayingRecipe = craftingButton.selectedRecipe;
                    itemNameBoxGroup.DOFade(0.96f, 0.3f);

                    itemRecipe.gameObject.SetActive(true);

                    string displayRecipe = "______________________\r\n";
                    int index = 0;
                    int amountOfIngred = 0;

                    Dictionary<item, int> itemCounts = GetItemCounts(true);

                    foreach (ingredient Ingredient in craftingButton.selectedRecipe.Recipe.Ingredients)
                    {
                        if (itemCounts.ContainsKey(Ingredient.Item))
                        {
                            amountOfIngred = itemCounts[Ingredient.Item];
                        }
                        else
                        {
                            amountOfIngred = 0;
                        }

                        string amountOfIngredString = amountOfIngred.ToString();
                        string countString = Ingredient.count.ToString();

                        if (amountOfIngred > 1000)
                            amountOfIngredString = "1000+";

                        string ingredientString = $"{amountOfIngredString} / {countString} <b>{Ingredient.Item.displayName}</b>";

                        // Set the text color based on the comparison
                        string colorTag = amountOfIngred >= Ingredient.count
                            ? $"<color=#{ColorUtility.ToHtmlStringRGB(canCraftTextColor)}>"
                            : $"<color=#{ColorUtility.ToHtmlStringRGB(cantCraftTextColor)}>";

                        displayRecipe += $"{colorTag}{ingredientString}</color>";

                        index++;

                        if (index != craftingButton.selectedRecipe.Recipe.Ingredients.Count())
                        {
                            displayRecipe += ",\n";
                        }
                    }

                    itemRecipe.text = displayRecipe;
                }

                SetItemNameBoxText(craftingButton.selectedRecipe.Recipe.result.Item, craftingButton.selectedRecipe.Recipe.result.Item.maxDurability, new ItemData(0), craftingButton.selectedRecipe.transform.position + new Vector3(-10, 10), true);
            }
            else
            {
                if (showingName)
                {
                    itemNameBoxGroup.DOFade(0, 0.3f);
                    showingName = false;
                }
                currentDisplayingRecipe = null;
            }
        }

        if (InputManager.actions["Inventory"].started || ((InputManager.actions["Pause"].started && inventoryOpen)) && canOpenInventory && !menu.instance.mapMaximized && !player.instance.pilotingBoat)
        {
            if (inventoryOpen)
            {
                if (!statsOpen)
                    CloseInventory();
                else
                {
                    if (!inNewAbilityMenu)
                        OnStatsClicked();
                    else
                        OnExitNewAbilityMenu();
                }
            }
            else
            {
                OpenInventory(interfaceType.normal);
            }
        }

        //bug fix
        if (player.instance && player.instance.pilotingBoat && inventoryOpen)
        {
            CloseInventory();
        }

        if (HandMove.instance && inventorySlot.equippedSlot && inventorySlot.equippedSlot.itemInSlot != null)
        {
            HandMove.instance.animator.SetBool("Two Hands", inventorySlot.equippedSlot.itemInSlot.twoHanded);
            HandMove.instance.animator.SetBool("Scythe", inventorySlot.equippedSlot.itemInSlot.scytheAnimation);
            //drop
            if (InputManager.actions["Drop"].started && !inventoryOpen && dropTimer <= 0 && playerWater.instance.waterValue != 2 && !playerHealth.instance.dead && !Chat.Instance.chatOpen
                && !gameManager.instance.writingSign && (inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.none || Input.GetKey(KeyCode.LeftControl)) && !pauseScreen.instance.paused && !player.instance.pilotingBoat && !player.instance.inBoatAltAction
                )
            {
                HandMove.instance.DropAnimation();
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    DropItem(inventorySlot.equippedSlot.itemCountInSlot);
                }
                else
                {
                    DropItem(1);
                }

            }


        }
        else if (player.instance)
        {
            player.instance.heldItemSprite.enabled = false;
            HandMove.instance.animator.SetBool("Two Hands", false);
            HandMove.instance.animator.SetBool("Scythe", false);
        }

        //enable second hand when piloting boat
        if (player.instance && (player.instance.pilotingBoat || (player.instance.inBoatAltAction && player.instance.currentBoat.BoatType == Boat.boatType.battleship)))
        {
            HandMove.instance.animator.SetBool("In Boat", true);
        }
        else if (player.instance)
        {
            HandMove.instance.animator.SetBool("In Boat", false);
        }

        //item dragging
        if (itemDragIcon != null)
        {
            Vector2 current = itemDragIcon.transform.position;
            Vector2 target = InputManager.instance.mousePos;

            Vector2 force = (target - current) * dragSpringStrength;

            itemDragVelocity += force * Time.deltaTime;
            itemDragVelocity *= Mathf.Exp(-dragDamping * Time.deltaTime);

            current += itemDragVelocity * Time.deltaTime;

            itemDragIcon.transform.position = current;
        }
        else
        {
            itemDragVelocity = Vector2.zero;
        }


        //click outside inventory to drop items
        if (cam.ScreenToViewportPoint(InputManager.instance.mousePos).x < noItemDropXRange.x || cam.ScreenToViewportPoint(InputManager.instance.mousePos).x > noItemDropXRange.y)
        {
            if (InputManager.actions["Item Primary"].started && inventoryOpen && inventorySlot.selectedSlot == null && inventorySlot.itemInMouse != null && craftingButton.selectedRecipe == null && !player.instance.inBoatAltAction && !playerHealth.instance.dead)
            {
                DropItem(inventorySlot.itemCountInMouse);
                HandMove.instance.DropAnimation();
            }
            if (InputManager.actions["Item Secondary"].started && inventoryOpen && inventorySlot.selectedSlot == null && inventorySlot.itemInMouse != null && craftingButton.selectedRecipe == null && !player.instance.inBoatAltAction && !playerHealth.instance.dead)
            {
                DropItem(1);
                HandMove.instance.DropAnimation();
            }
        }

        //a and d to switch category
        if (inventoryOpen && crafingUI.activeInHierarchy)
        {
            float cooldown = 0.15f;

            if (!disableSwitchCategory)
            {

                if (!InputManager.actions["Scroll Right"].held &&
                    !InputManager.actions["Scroll Left"].held)
                    categorySwitchCd = 0;

                //first tap direction
                if (InputManager.actions["Scroll Right"].started && categorySwitchCd <= 0f)
                {
                    SwitchCategory(false);

                    categorySwitchCd = 0.3f;
                }
                if (InputManager.actions["Scroll Left"].started && categorySwitchCd <= 0f)
                {
                    SwitchCategory(true);

                    categorySwitchCd = 0.3f;
                }

                //hold direction
                if (InputManager.actions["Scroll Right"].held && categorySwitchCd <= 0f)
                {
                    SwitchCategory(false);

                    categorySwitchCd = cooldown;
                }
                if (InputManager.actions["Scroll Left"].held && categorySwitchCd <= 0f)
                {
                    SwitchCategory(true);

                    categorySwitchCd = cooldown;
                }

                categorySwitchCd -= Time.deltaTime;

            }
            else if (!InputManager.actions["Scroll Right"].held &&
                    !InputManager.actions["Scroll Left"].held)
            {
                disableSwitchCategory = false;
            }

        }


        //a and d to switch category in boat station
        if (inventoryOpen && vehicleStationUi.activeInHierarchy)
        {
            float cooldown = 0.18f;

            if (!disableSwitchCategory)
            {
                if (!InputManager.actions["Scroll Right"].held &&
                    !InputManager.actions["Scroll Left"].held)
                    categorySwitchCd = 0;

                //first tap direction
                if (InputManager.actions["Scroll Right"].started && categorySwitchCd <= 0f)
                {
                    NextBoat(false);

                    categorySwitchCd = 0.33f;
                }
                if (InputManager.actions["Scroll Left"].started && categorySwitchCd <= 0f)
                {
                    NextBoat(true);

                    categorySwitchCd = 0.33f;
                }

                //hold direction
                if (InputManager.actions["Scroll Right"].held && categorySwitchCd <= 0f)
                {
                    NextBoat(false);

                    categorySwitchCd = cooldown;
                }
                if (InputManager.actions["Scroll Left"].held && categorySwitchCd <= 0f)
                {
                    NextBoat(true);

                    categorySwitchCd = cooldown;
                }

                categorySwitchCd -= Time.deltaTime;

            }
            else if (!InputManager.actions["Scroll Right"].held &&
                    !InputManager.actions["Scroll Left"].held)
            {
                disableSwitchCategory = false;
            }

        }




        //mouse durability bar
        if (inventoryOpen && inventorySlot.itemInMouse && inventorySlot.itemInMouse.usesDurability && inventorySlot.durabilityInMouse < inventorySlot.itemInMouse.maxDurability)
        {
            mouseDurabilityBar.value = inventorySlot.durabilityInMouse / (float)inventorySlot.itemInMouse.maxDurability;
            mouseDurabilityBar.gameObject.SetActive(true);
            mouseDurabilityImage.color = inventorySlot.itemInMouse.durabilityBarColor;

            mouseDurabilityBar.transform.parent.transform.position = itemDragIcon.transform.position;
            
            if(inventorySlot.itemInMouse.uniqueType != item.UniqueType.gavel && inventorySlot.durabilityInMouse <= 0)
            {
                mouseBrokenSlash.fillAmount = 1;
            }
            else
            {
                mouseBrokenSlash.fillAmount = 0;
            }

        }
        else
        {
            mouseDurabilityBar.gameObject.SetActive(false);
        }

        if (inventoryOpen && inventorySlot.itemInMouse && inventorySlot.itemDataInMouse.tier > 0)
        {
            mouseTierHighlight.transform.position = itemDragIcon.transform.position;

            mouseTierHighlight.gameObject.SetActive(true);
            mouseTierHighlight.sprite = slots[0].tierHighlightSprites[inventorySlot.itemDataInMouse.tier - 1];
            mouseTierHighlight.color = slots[0].tierHighlightColors[inventorySlot.itemDataInMouse.tier - 1];
        }
        else
        {
            mouseTierHighlight.gameObject.SetActive(false);
        }

        if (statsOpen)
        {
            for (int i = 0; i < 5; i++)
            {
                item shardItem = gameManager.instance.itemDictionary["yellow_soul_shard"];

                switch (i)
                {
                    case 1:
                        shardItem = gameManager.instance.itemDictionary["blue_soul_shard"]; break;
                    case 2:
                        shardItem = gameManager.instance.itemDictionary["green_soul_shard"]; break;
                    case 3:
                        shardItem = gameManager.instance.itemDictionary["red_soul_shard"]; break;
                    case 4:
                        shardItem = gameManager.instance.itemDictionary["purple_soul_shard"]; break;

                }
                Dictionary<item, int> itemCounts = GetItemCounts(false);

                if ((statAmounts[i] < 10 && !maxStat) || statAmounts[i] < 9)
                {
                    shardCounts[i].enabled = true;
                }
                else
                {
                    shardCounts[i].enabled = false;
                }

                if (itemCounts.ContainsKey(shardItem))
                {
                    shardCounts[i].text = itemCounts[shardItem].ToString();

                    switch (i)
                    {
                        case 0:
                            shardCounts[i].color = new Color(1, 0.85f, 0);
                            break;
                        case 1:
                            shardCounts[i].color = new Color(0, 0.7f, 1f);
                            break;
                        case 2:
                            shardCounts[i].color = new Color(0, 1f, 0.1f);
                            break;
                        case 3:
                            shardCounts[i].color = new Color(1f, 0.05f, 0.05f);
                            break;
                        case 4:
                            shardCounts[i].color = new Color(1, 0.1f, 1);
                            break;

                    }
                }
                else
                {
                    shardCounts[i].color = new Color(0.8f, 0.8f, 0.8f);
                    shardCounts[i].text = "0";
                }

            }

        }

    }

    public void ClickSound()
    {
        craftingTabSwitchSound.Play();
    }

    void SwitchCategory(bool left)
    {
        if (!left)
        {
            if (enabledCategory + 2 > categoryButtons.Length)
            {
                EnableCategory(0);
            }
            else
            {
                EnableCategory(enabledCategory + 1);
            }

            ClickSound();
        }
        else
        {
            if (enabledCategory - 1 < 0)
            {
                EnableCategory(categoryButtons.Length - 1);
            }
            else
            {
                EnableCategory(enabledCategory - 1);
            }

            ClickSound();
        }
    }

    public void DropItem(int amount)
    {
        dropTimer = dropCooldown;

        Vector2 origin = player.instance.rb.position + (Vector2)player.instance.collision.transform.localPosition;
        Vector2 dir = (player.instance.rb.position + (Vector2)HandMove.instance.sprite.transform.localPosition) - origin;
        float dist = dir.magnitude;

        Vector2 size = new Vector2(0.25f, 0.25f); // width / height of the cast

        LayerMask mask = dropRaycastLayers;

        if (player.instance.currentBoat)
            mask |= 1 << 12;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useTriggers = false;

        Unity.Collections.NativeArray<RaycastHit2D> hit = Physics2D.BoxCast(
            origin,
            size,
            0f,
            dir.normalized,
            filter,
            dist
        );

        Vector2 itemSpawnPos = HandMove.instance.sprite.transform.position;

        if (hit != null && hit.Length > 0 && hit[0].collider)
        {
            itemSpawnPos = hit[0].point;
        }

        NetworkId boatId = new NetworkId();
        if (player.instance.currentBoat != null)
        {
            boatId = player.instance.currentBoat.view.Id;
        }

        if (!inventorySlot.itemInMouse)
        {
            RPC_InstantiateItem(inventorySlot.equippedSlot.itemInSlot.itemId, Convert.ToInt16(amount), HandMove.instance.transform.position, HandMove.instance.transform.eulerAngles.z, itemSpawnPos, player.instance.axisInput.normalized, (short)inventorySlot.equippedSlot.durability, ItemDataToString(inventorySlot.equippedSlot.itemData), Runner.LocalPlayer, boatId, player.instance.boatFloor);

            inventorySlot.equippedSlot.itemCountInSlot -= amount;
            inventorySlot.equippedSlot.itemCountTextInstance.text = inventorySlot.equippedSlot.itemCountInSlot.ToString();
            if (inventorySlot.equippedSlot.itemCountInSlot == 0)
            {
                //items[inventorySlot.equippedSlot.IndexInArray] = null;
                Destroy(inventorySlot.equippedSlot.icon);
                inventorySlot.equippedSlot.itemInSlot = null;

            }

            foreach (craftingButton cb in craftingButtons)
            {
                cb.CheckCanCraft();
            }
        }
        else
        {
            RPC_InstantiateItem(inventorySlot.itemInMouse.itemId, Convert.ToInt16(amount), HandMove.instance.transform.position, HandMove.instance.transform.eulerAngles.z, itemSpawnPos, player.instance.axisInput.normalized, (short)inventorySlot.durabilityInMouse, ItemDataToString(inventorySlot.itemDataInMouse), Runner.LocalPlayer, boatId, player.instance.boatFloor);

            inventorySlot.itemCountInMouse -= amount;
            itemDragCountText.text = inventorySlot.equippedSlot.itemCountInSlot.ToString();

            if (inventorySlot.itemCountInMouse == 0)
            {

                Destroy(itemDragIcon);
                inventorySlot.itemInMouse = null;

            }
        }


    }

    public void SetItemNameBoxText(item highlightedItem, int durability, ItemData itemData, Vector2 position, bool craftingRecipe)
    {
        ItemNameText.text = highlightedItem.displayName;
        itemDesc.text = highlightedItem.desc;

        if (craftingRecipe && highlightedItem.obfuscateRecipeDesc)
        {
            ItemNameText.text = "???";
            itemDesc.text = "";
        }

        switch (highlightedItem.type)
        {
            case item.itemType.item:
                itemType.text = "(Item)";
                break;
            case item.itemType.tool:
                itemType.text = "(Tool)";
                break;
            case item.itemType.remover:
                itemType.text = "(Remover)";
                break;
            case item.itemType.block:
                itemType.text = "(Tile)";
                break;
            case item.itemType.food:
                itemType.text = "(Food)";
                break;
            case item.itemType.armorLegs:
                itemType.text = "(Legs Armor)";
                break;
            case item.itemType.armorChest:
                itemType.text = "(Chest Armor)";
                break;
            case item.itemType.armorHead:
                itemType.text = "(Head Armor)";
                break;
            case item.itemType.catalyst:
                itemType.text = "(Catalyst)";
                break;
            case item.itemType.recall:
                itemType.text = "(Tool)";
                break;
            case item.itemType.bucketE:
                itemType.text = "(Tool)";
                break;
            case item.itemType.bucketF:
                itemType.text = "(Tool)";
                break;
            case item.itemType.tracker:
                itemType.text = "(Tool)";
                break;
            case item.itemType.map:
                itemType.text = "(Map)";
                break;
            case item.itemType.charm:
                itemType.text = "(Charm)";
                break;
            default:
                itemType.text = "(Tool)";
                break;
        }

        string _seperator = "\r\n───────────────\r\n";

        if (itemData.tier > 0 && highlightedItem.type != item.itemType.charm)
        {
            string tier = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.6f, 0.6f, 0.6f))}>" + " (Tier I)" + "</color>";
            if (itemData.tier == 2) tier = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.6f, 1))}>" + " (Tier II)" + "</color>";
            else if (itemData.tier == 3) tier = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.65f, 0, 1))}>" + " (Tier III)" + "</color>";
            else if (itemData.tier == 4) tier = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(1, 0.8f, 0))}>" + " (Tier IV)" + "</color>";

            itemType.text += tier;

            if (itemData.pMods != null && itemData.nMods != null && (itemData.pMods.Count > 0 || itemData.nMods.Count > 0))
                itemDesc.text += _seperator;

            bool firstNewLine = true;
            if (itemData.pMods != null)
            {
                itemDesc.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 1, 0))}>";

                foreach (KeyValuePair<string, int> p in itemData.pMods)
                {
                    if (!firstNewLine) itemDesc.text += "\n";
                    firstNewLine = false;

                    ToolModifier mod = null;
                    foreach (ToolModifierGroup m in highlightedItem.toolModifierPool)
                    {
                        mod = m.modifiers.Find(t => t.id == p.Key);
                        if (mod != null) break;
                    }

                    if (mod == null) continue;

                    if (p.Value > 0) itemDesc.text += "+";
                    itemDesc.text += p.Value + "% " + mod.displayName;
                }

                itemDesc.text += "</color>";
            }

            if (itemData.nMods != null)
            {
                itemDesc.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(1, 0, 0))}>";
                foreach (KeyValuePair<string, int> p in itemData.nMods)
                {
                    if (!firstNewLine) itemDesc.text += "\n";
                    firstNewLine = false;

                    ToolModifier mod = null;
                    foreach (ToolModifierGroup m in highlightedItem.toolModifierPool)
                    {
                        mod = m.negativeModifiers.Find(t => t.id == p.Key);
                        if (mod != null) break;
                    }

                    if (p.Value > 0) itemDesc.text += "+";
                    itemDesc.text += p.Value + "% " + mod.displayName;
                }

                itemDesc.text += "</color>";
            }

            if (itemData.tier == 4 && highlightedItem.maxTierModifier != "")
            {
                itemDesc.text += "\n" + $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.8f, 1))}>"
                    + highlightedItem.maxTierModifier + "</color>";
            }

        }

        //for scythe
        if (highlightedItem.type == item.itemType.block && highlightedItem.usesDurability) itemType.text = "(Tool)";

        if (highlightedItem.armorMitigationPercent > 0)
        {
            itemDesc.text += _seperator + "Protection: " + highlightedItem.armorMitigationPercent + "%";
        }

        itemNameBoxRect.sizeDelta = (new Vector2(140, itemNameBoxRect.sizeDelta.y));
        itemDesc.rectTransform.sizeDelta = new Vector2(118, itemDesc.rectTransform.sizeDelta.y);

        if (highlightedItem.type == item.itemType.charm)
        {
            _seperator = "\r\n──────────────────────\r\n";

            itemNameBoxRect.sizeDelta = (new Vector2(190, itemNameBoxRect.sizeDelta.y));
            itemDesc.rectTransform.sizeDelta = new Vector2(167, itemDesc.rectTransform.sizeDelta.y);

            itemDesc.text += _seperator;
            for (int i = 0; i < highlightedItem.charmStatsChanged.Length; i++)
            {
                if (i > 0)
                    itemDesc.text += "\r\n";
                if (highlightedItem.charmStatsChangedAmount[i] > 0)
                {
                    itemDesc.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 1, 0))}>";
                    itemDesc.text += "+";
                }
                else
                {
                    itemDesc.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(1, 0, 0))}>";
                }
                itemDesc.text += "" + highlightedItem.charmStatsChangedAmount[i] + " " + highlightedItem.charmStatsChanged[i].ToString();
                itemDesc.text += "</color>";
            }
        }

        if (highlightedItem.type == item.itemType.tool)
        {
            if (highlightedItem.treeDamage * 1.4f > highlightedItem.rockDamage && highlightedItem.treeDamage * 1.4f > highlightedItem.damage)
            {
                itemDesc.text += _seperator + "Tree Damage: " + highlightedItem.treeDamage;
            }
            else if (highlightedItem.rockDamage > highlightedItem.treeDamage && highlightedItem.rockDamage > highlightedItem.damage)
            {
                itemDesc.text += _seperator + "Ore Damage: " + highlightedItem.rockDamage;
            }
            else if (highlightedItem.damage > highlightedItem.rockDamage && highlightedItem.damage > highlightedItem.treeDamage)
            {

                //jackpot hammer
                if (highlightedItem.damageVariance > 30)
                {
                    itemDesc.text += _seperator + "Attack Damage: " + (highlightedItem.damage - highlightedItem.damageVariance) + " to " + (highlightedItem.damage + highlightedItem.damageVariance);
                }
                else //soul breaker
                    if (highlightedItem.uniqueType == item.UniqueType.breaker)
                    {
                        itemDesc.text += _seperator + "Attack Damage: " + highlightedItem.damage + "/" + Mathf.RoundToInt(highlightedItem.damage * item.soulBreakerCritDamageMult);
                    }
                    //normal
                    else
                    {
                        itemDesc.text += _seperator + "Attack Damage: " + highlightedItem.damage;
                    }

            }

        }
        if (highlightedItem.soulRepairAmount > 0)
        {
            itemDesc.text += _seperator +
                $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.4f, 1))}>" + "Unique Tool \nRepair Amount: +" + highlightedItem.soulRepairAmount + "</color>";
        }
        if (highlightedItem.type == item.itemType.food)
        {
            itemDesc.text += _seperator +
                $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.78f, 0))}>" + "Restored Health: +" + highlightedItem.restoreHealth + "</color>";
        }

        itemType.color = Color.red;

        if (highlightedItem.uniqueType != item.UniqueType.none)
        {
            if (inventorySlot.itemInMouse && inventorySlot.itemInMouse.soulRepairAmount > 0 && durability < highlightedItem.maxDurability)
            {
                int durabilityPerItem = inventorySlot.itemInMouse.soulRepairAmount;

                durabilityPerItem = Mathf.RoundToInt(durabilityPerItem * highlightedItem.uniqueRepairMultiplier);

                soulRepairAmount = inventorySlot.itemCountInMouse;
                if (durability + (soulRepairAmount * durabilityPerItem) > highlightedItem.maxDurability)
                {
                    soulRepairAmount = Mathf.RoundToInt((highlightedItem.maxDurability - durability) / (float)durabilityPerItem);
                }
                if (soulRepairAmount == 0) soulRepairAmount = 1;

                itemBoxImage.sprite = repairBoxSprite;
                itemType.color = Color.blue;
                itemType.text = "(Repair)";

                itemDesc.text = "";

                itemDesc.text += "\r\n" + $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.2f, 1))}>" + highlightedItem.durabilityName + ": " + durability + "/" + highlightedItem.maxDurability + "</color>";
                itemDesc.text += _seperator;

                itemDesc.text += "-" + soulRepairAmount + " souls";
                itemDesc.text += "\r\n+" + soulRepairAmount * durabilityPerItem + " durability";
            }
            else
                itemBoxImage.sprite = uniqueBoxSprite;
        }
        else if (highlightedItem.itemId == "ominous_flower")
        {
            itemBoxImage.sprite = repairBoxSprite;
            itemType.color = Color.blue;
        }
        else
        {
            itemBoxImage.sprite = itemBoxSprite;
        }

        if (highlightedItem.usesDurability)
        {
            itemDesc.text += _seperator;
            itemDesc.text += $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.2f, 1))}>" + highlightedItem.durabilityName + ": " + durability + "/" + highlightedItem.maxDurability + "</color>";
            if(durability <= 0 && highlightedItem.uniqueType != item.UniqueType.gavel)
            {
                itemDesc.text += $"\n<color=#{ColorUtility.ToHtmlStringRGB(new Color(1, 0, 0))}>" + "BROKEN" + "</color>";
            }

        }

        // Smoothly move the parent box to the target
        ItemNameBox.position = Vector2.Lerp(ItemNameBox.position, position, Time.deltaTime * 15);

        // Force layout updates so RectTransforms are correct
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemNameTextRect);

        // Normal local position
        Vector2 targetPos = new Vector2(
            -itemNameBoxRect.sizeDelta.x / 2 + itemNameBoxOffset.x,
            itemNameBoxRect.sizeDelta.y - itemNameBoxOffset.y
        );

        // Smoothly move toward the normal position
        itemNameBoxRect.localPosition = Vector2.Lerp(
            itemNameBoxRect.localPosition,
            targetPos,
            20 * Time.deltaTime
        );

        // Keep the box on screen using its actual corners
        Vector3[] corners = new Vector3[4];
        itemNameBoxRect.GetWorldCorners(corners);

        Vector3 correction = Vector3.zero;
        const float padding = 20f;

        // Bottom
        if (corners[0].y < padding)
            correction.y += padding - corners[0].y;

        // Top
        if (corners[2].y > Screen.height - padding)
            correction.y -= corners[2].y - (Screen.height - padding);

        // Left
        if (corners[0].x < padding)
            correction.x += padding - corners[0].x;

        // Right
        if (corners[2].x > Screen.width - padding)
            correction.x -= corners[2].x - (Screen.width - padding);

        itemNameBoxRect.position += correction;
    }


    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    public void RPC_InstantiateItem(string itemID, short itemAmount, Vector3 playerHandPos, float playerHandRotation, Vector3 spawnPos, Vector3 playerAxisInput, short durability, string itemData, PlayerRef player, NetworkId boatParent, int boatFloor)
    {
        if (Runner.LocalPlayer != player) return;

        GameObject thrownItem = Runner.Spawn(dropItemPrefab, spawnPos, Quaternion.identity, Runner.LocalPlayer).gameObject;

        thrownItem.transform.SetParent(gameManager.instance.itemsHolder);
        //parent is set to boat in itempickup under rpcsetitem

        itemPickup ItemPickup = thrownItem.GetComponent<itemPickup>();
        ItemPickup.Item = gameManager.instance.itemDictionary[itemID];
        ItemPickup.SetItem(itemID, itemAmount, durability, StringToItemData(itemData), false, boatParent, boatFloor);
        ItemPickup.ApplyForce(itemID, playerHandPos, playerHandRotation, playerAxisInput);

    }

    public void AddItem(item item2Add, int itemCount, int durability, ItemData itemData)
    {
        if (item2Add == null) return;

        int amtToAdd = itemCount;

        while (amtToAdd > 0)
        {

            int firstFreeIndex = -1;

            bool emptySlot = true;
            //true if the item should be added to a new slot
            bool alreadyContainsItem = false;

            foreach (inventorySlot slot in slots)
            {
                if (slot.itemInSlot == item2Add && slot.itemCountInSlot < item2Add.maxStackSize)
                {
                    alreadyContainsItem = true;
                }
            }

            for (int i = slots.Count() - 1; i >= 0; i--)
            {
                if (slots[i].itemInSlot == null && !alreadyContainsItem)
                {
                    firstFreeIndex = i;
                    emptySlot = true;
                }
                else if (slots[i].itemInSlot == item2Add && slots[i].itemCountInSlot < item2Add.maxStackSize)
                {
                    firstFreeIndex = i;
                    emptySlot = false;
                }
            }

            if (firstFreeIndex != -1)
            {

                if (emptySlot)
                {
                    //item icon
                    GameObject icon = new GameObject("item icon");
                    Image iconImage = icon.AddComponent<Image>();
                    icon.transform.SetParent(iconParent);
                    icon.transform.SetAsFirstSibling();
                    iconImage.sprite = item2Add.overworldSprite;
                    iconImage.raycastTarget = false;
                    iconImage.preserveAspect = true;
                    
                    //item count text
                    TextMeshProUGUI countText = Instantiate(slots[firstFreeIndex].itemCountText, slots[firstFreeIndex].transform.position, Quaternion.identity).GetComponent<TextMeshProUGUI>();
                    countText.transform.SetParent(icon.transform);
                    countText.transform.localPosition = Vector2.zero;
                    countText.transform.localScale = new Vector2(1, 1);
                    countText.raycastTarget = false;
                    slots[firstFreeIndex].itemCountTextInstance = countText;

                    slots[firstFreeIndex].icon = icon;

                    //if adding more than can hold in a slot, fill this slot completely and move on
                    if (amtToAdd <= item2Add.maxStackSize)
                        slots[firstFreeIndex].itemCountInSlot = amtToAdd;
                    else
                        slots[firstFreeIndex].itemCountInSlot = item2Add.maxStackSize;

                    countText.text = slots[firstFreeIndex].itemCountInSlot.ToString();
                    icon.transform.position = slots[firstFreeIndex].gameObject.transform.position;
                    icon.transform.SetParent(slots[firstFreeIndex].gameObject.transform);
                    icon.transform.SetAsFirstSibling();

                    slots[firstFreeIndex].itemInSlot = item2Add;
                    slots[firstFreeIndex].durability = durability;
                    slots[firstFreeIndex].itemData = itemData;
                    if (!item2Add.usesTier && itemData.tier > 0)
                    {
                        slots[firstFreeIndex].itemData = new ItemData(0);
                    }

                    icon.transform.localScale = new Vector2(itemIconScale, itemIconScale);

                    if (slots[firstFreeIndex].itemCountInSlot == 1)
                    {
                        slots[firstFreeIndex].itemCountTextInstance.text = string.Empty;
                    }

                    //show item name on hotbar when picking it up
                    if (slots[firstFreeIndex] == slots[inventoryHightlighter.instance.equipppedSlot])
                    {
                        inventoryHightlighter.instance.ShowItemNameText();
                    }

                    amtToAdd -= item2Add.maxStackSize;
                }
                else
                {

                    //over stack size
                    if (amtToAdd + slots[firstFreeIndex].itemCountInSlot > slots[firstFreeIndex].itemInSlot.maxStackSize)
                    {
                        while (slots[firstFreeIndex].itemCountInSlot < slots[firstFreeIndex].itemInSlot.maxStackSize)
                        {
                            slots[firstFreeIndex].itemCountInSlot += 1;
                            amtToAdd -= 1;
                        }

                    }
                    //under the stack size
                    else
                    {
                        slots[firstFreeIndex].itemCountInSlot += amtToAdd;
                        slots[firstFreeIndex].itemCountTextInstance.text = slots[firstFreeIndex].itemCountInSlot.ToString();
                        amtToAdd = 0;
                    }
                }

            }
            //drop remainder of items that were attempting to be added
            else if (amtToAdd > 0)
            {
                NetworkId boatId = new NetworkId();
                if (player.instance.currentBoat != null) boatId = player.instance.currentBoat.view.Id;

                RPC_InstantiateItem(item2Add.itemId, Convert.ToInt16(amtToAdd), HandMove.instance.transform.position, HandMove.instance.transform.rotation.z, HandMove.instance.sprite.transform.position, player.instance.axisInput.normalized, (short)1, ItemDataToString(new ItemData(0)), Runner.LocalPlayer, boatId, player.instance.boatFloor);
                amtToAdd = 0;
            }
        }
    }

    public float GetModifierInToolSlot(string name)
    {
        if (inventorySlot.equippedSlot == null
            || inventorySlot.equippedSlot.itemInSlot == null
            || inventorySlot.equippedSlot.itemInSlot.type == item.itemType.armorHead
            || inventorySlot.equippedSlot.itemInSlot.type == item.itemType.armorChest
            || inventorySlot.equippedSlot.itemInSlot.type == item.itemType.armorLegs)
            return 1;

        ItemData data = inventorySlot.equippedSlot.itemData;
        if (data.tier == 0) return 1;

        foreach (KeyValuePair<string, int> m in data.pMods)
        {
            if (m.Key == name)
            {
                return 1 + (m.Value / 100f);
            }
        }
        foreach (KeyValuePair<string, int> m in data.nMods)
        {
            if (m.Key == name)
            {
                return 1 + (m.Value / 100f);
            }
        }
        return 1;
    }

    public float GetModifierInArmorSlots(string name, inventorySlot slot = null)
    {
        List<inventorySlot> checkingSlots = new List<inventorySlot>();

        if (slot)
        {
            checkingSlots.Add(slot);
        }
        else
        {
            checkingSlots.Add(headSlot);
            checkingSlots.Add(chestplateSlot);
            checkingSlots.Add(legsSlot);
        }
        float multiplierAdd = 0;
        foreach (inventorySlot s in checkingSlots)
        {
            ItemData data = s.itemData;

            if (s.itemInSlot == null || data.tier == 0) continue;

            foreach (KeyValuePair<string, int> m in data.pMods)
            {
                if (m.Key == name)
                {
                    multiplierAdd += (m.Value / 100f);
                }
            }
            foreach (KeyValuePair<string, int> m in data.nMods)
            {
                if (m.Key == name)
                {
                    multiplierAdd += (m.Value / 100f);
                }
            }
        }
        return 1 + multiplierAdd;
    }

    public void addItemInMouse(item item2Add, int itemCount, int durability, ItemData itemData, Vector2 iconStartPos)
    {
        if (item2Add == null) { return; }


        if (!inventorySlot.itemInMouse)
        {
            //item icon
            GameObject icon = new GameObject("item icon");
            icon.transform.SetParent(iconParent);
            icon.transform.SetAsFirstSibling();
            icon.transform.position = iconStartPos;

            Image iconImage = icon.AddComponent<Image>();
            iconImage.sprite = item2Add.overworldSprite;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            //item count text
            TextMeshProUGUI countText = Instantiate(slots[0].itemCountText, InputManager.instance.mousePos, Quaternion.identity).GetComponent<TextMeshProUGUI>();
            countText.text = itemCount.ToString();
            countText.transform.SetParent(icon.transform);
            countText.transform.localPosition = Vector2.zero;
            countText.transform.localScale = new Vector2(1, 1);
            countText.raycastTarget = false;
            itemDragCountText = countText;

            inventorySlot.itemInMouse = item2Add;
            inventorySlot.itemCountInMouse = itemCount;
            if (inventorySlot.itemCountInMouse == 1)
            {
                itemDragCountText.text = string.Empty;
            }

            itemDragIcon = icon;
            inventorySlot.itemCountInMouse = itemCount;
            inventorySlot.durabilityInMouse = durability;
            inventorySlot.itemDataInMouse = itemData;
            icon.transform.SetParent(transform);
            icon.transform.localScale = new Vector2(itemIconScale, itemIconScale);


        }
        else if (inventorySlot.itemInMouse == item2Add)
        {
            inventorySlot.itemCountInMouse += itemCount;
        }

    }

    public static ItemData CreateItemData(item Item, int overrideTier = -1)
    {
        int weightSum = Item.tierWeights.Sum();
        int rand = UnityEngine.Random.Range(1, weightSum + 1);
        ItemData newData = new ItemData();
        int p = 0;
        for (int i = 0; i < 5; i++)
        {
            p += Item.tierWeights[i];
            if (rand <= p)
            {
                newData.tier = i + 1;
                break;
            }
        }

        if (overrideTier > 0)
        {
            newData.tier = overrideTier;
        }

        int positiveMods = 0;
        int negativeMods = 0;

        switch (newData.tier)
        {
            case 1:
                positiveMods = 1;
                negativeMods = 1;
                break;
            case 2:
                positiveMods = UnityEngine.Random.Range(1, 3);
                negativeMods = 0;
                if (positiveMods == 2) negativeMods = 1;
                break;
            case 3:
                positiveMods = 2;
                negativeMods = 0;
                break;
            case 4:
                positiveMods = 3;
                negativeMods = 0;
                break;

        }

        List<KeyValuePair<string, int>> positiveModPool = new List<KeyValuePair<string, int>>();
        List<KeyValuePair<string, int>> negativeModPool = new List<KeyValuePair<string, int>>();

        foreach (ToolModifierGroup modGroup in Item.toolModifierPool)
        {
            foreach (ToolModifier mod in modGroup.modifiers)
            {
                if (positiveModPool.Find(k => k.Key == mod.id).Key != null) continue;

                positiveModPool.Add(new KeyValuePair<string, int>(mod.id, mod.percentMod
                    + (newData.tier - 1) * mod.percentPerTier));
            }
            foreach (ToolModifier mod in modGroup.negativeModifiers)
            {
                if (negativeModPool.Find(k => k.Key == mod.id).Key != null) continue;

                negativeModPool.Add(new KeyValuePair<string, int>(mod.id, mod.percentMod
                    + (newData.tier - 1) * mod.percentPerTier));
            }
        }

        newData.pMods = new SerializableDictionary<string, int>();
        newData.nMods = new SerializableDictionary<string, int>();
        if (positiveModPool.Count > 0)
        {
            for (int i = 0; i < positiveMods; i++)
            {
                int randomMod = UnityEngine.Random.Range(0, positiveModPool.Count);
                int failsafe = 0;
                while (newData.pMods.ContainsKey(positiveModPool[randomMod].Key))
                {
                    randomMod = UnityEngine.Random.Range(0, positiveModPool.Count);
                    failsafe++;
                    if (failsafe > 300)
                    {
                        Debug.LogError("failsafe for modifers " + Item);
                        break;
                    }
                }

                newData.pMods.TryAdd(positiveModPool[randomMod].Key, positiveModPool[randomMod].Value);
            }
        }
        if (negativeModPool.Count > 0)
        {
            for (int i = 0; i < negativeMods; i++)
            {
                int randomMod = UnityEngine.Random.Range(0, negativeModPool.Count);
                int failsafe = 0;
                while (newData.nMods.ContainsKey(negativeModPool[randomMod].Key)
                    || newData.pMods.ContainsKey(negativeModPool[randomMod].Key))
                {
                    randomMod = UnityEngine.Random.Range(0, negativeModPool.Count);
                    failsafe++;
                    if (failsafe > 300)
                    {
                        Debug.LogError("failsafe for modifers " + Item);
                        break;
                    }
                }

                newData.nMods.TryAdd(negativeModPool[randomMod].Key, negativeModPool[randomMod].Value);
            }
        }

        return newData;
    }

    public ItemData UpgradeItemData(item Item, ItemData data)
    {
        ItemData newData = new ItemData();
        newData.tier = data.tier;
        newData.pMods = data.pMods.Clone();
        newData.nMods = data.nMods.Clone();


        List<KeyValuePair<string, int>> positiveModPool = new List<KeyValuePair<string, int>>();
        Dictionary<string, int> percentPerTier = new Dictionary<string, int>();

        foreach (ToolModifierGroup modGroup in Item.toolModifierPool)
        {
            foreach (ToolModifier mod in modGroup.modifiers)
            {
                if (positiveModPool.Find(k => k.Key == mod.id).Key != null) continue;

                positiveModPool.Add(new KeyValuePair<string, int>(mod.id, mod.percentMod
                    + (newData.tier - 1) * mod.percentPerTier));

                percentPerTier.TryAdd(mod.id, mod.percentPerTier);
            }
        }

        // add new mod
        if ((newData.nMods.Count() == 0 || newData.pMods.Count() == 0) && Item.maxTierModifier == "")
        {
            newData.nMods.Clear();

            int randomMod = UnityEngine.Random.Range(0, positiveModPool.Count);
            int failsafe = 0;
            while (newData.pMods.ContainsKey(positiveModPool[randomMod].Key))
            {
                randomMod = UnityEngine.Random.Range(0, positiveModPool.Count);
                failsafe++;
                if (failsafe > 300)
                {
                    Debug.LogError("failsafe for modifers " + Item);
                    break;
                }
            }
            newData.pMods.TryAdd(positiveModPool[randomMod].Key, positiveModPool[randomMod].Value);

        }
        //remove negative mods
        else
        {
            newData.nMods.Clear();
        }

        newData.tier++;

        List<string> keys = new List<string>(newData.pMods.Keys);
        foreach (string key in keys)
        {
            if (newData.pMods.ContainsKey(key) && percentPerTier.ContainsKey(key))
                newData.pMods[key] += percentPerTier[key];
        }

        return newData;
    }

    public static string ItemDataToString(ItemData data = new ItemData())
    {
        if (data.tier == 0)
        {
            return "null";
        }
        else
        {
            return JsonUtility.ToJson(data);
        }

    }

    public static ItemData StringToItemData(string str)
    {
        if (str == "null")
        {
            return new ItemData();
        }
        else
        {
            try
            {
                return JsonUtility.FromJson<ItemData>(str);
            }
            catch
            {
                print("failed to deserialize: " + str);
                return new ItemData();
            }
        }

    }

    public void addItemInSlot(item item2Add, int itemCount, int durability, ItemData itemData, inventorySlot slotReference, Vector2 iconStartPos, bool ignoreStackSize = false)
    {

        inventorySlot slot;
        slot = slotReference;

        if (slot.itemInSlot == null)
        {
            //item icon
            GameObject icon = new GameObject("item icon");
            Image iconImage = icon.AddComponent<Image>();
            icon.transform.SetParent(iconParent);
            icon.transform.SetAsFirstSibling();

            iconImage.sprite = item2Add.overworldSprite;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            //item count text
            TextMeshProUGUI countText = Instantiate(slot.itemCountText, slot.transform.position, Quaternion.identity).GetComponent<TextMeshProUGUI>();
            countText.text = itemCount.ToString();
            countText.transform.SetParent(icon.transform);
            countText.transform.localPosition = Vector2.zero;
            countText.transform.localScale = new Vector2(1, 1);
            countText.raycastTarget = false;
            slot.itemCountTextInstance = countText;

            slot.icon = icon;
            slot.itemCountInSlot = itemCount;
            slot.durability = durability;

            slot.itemData = itemData;
            if (!item2Add.usesTier && itemData.tier > 0)
            {
                slot.itemData = new ItemData(0);
            }

            icon.transform.SetParent(slot.gameObject.transform);
            icon.transform.SetAsFirstSibling();
            icon.transform.position = iconStartPos;

            slot.itemInSlot = item2Add;
            icon.transform.localScale = new Vector2(itemIconScale, itemIconScale);

            if (slot.itemCountInSlot == 1)
            {
                slot.itemCountTextInstance.text = string.Empty;
            }
        }
        else
        {
            int amtToAdd = itemCount;

            //over stack size
            if (itemCount + slot.itemCountInSlot > slot.itemInSlot.maxStackSize && !ignoreStackSize)
            {
                while (slot.itemCountInSlot < slot.itemInSlot.maxStackSize)
                {
                    slot.itemCountInSlot += 1;
                    amtToAdd -= 1;
                }
                if (amtToAdd > 0)
                {
                    slot.itemCountTextInstance.text = item2Add.maxStackSize.ToString();
                    int slotIndex;

                    slotIndex = 69420;

                    for (int i = slots.Count() - 1; i >= 0; i--)
                    {
                        if (slots[i].itemInSlot == null)
                        {
                            slotIndex = i;
                        }
                    }

                    if (slotIndex != 69420)
                    {
                        //items[slotIndex] = item2Add;


                        //item icon
                        GameObject icon = new GameObject("item icon");
                        icon.transform.SetParent(iconParent);
                        icon.transform.SetAsFirstSibling();

                        Image iconImage = icon.AddComponent<Image>();
                        iconImage.sprite = item2Add.overworldSprite;
                        iconImage.raycastTarget = false;

                        //item count text
                        TextMeshProUGUI countText = Instantiate(slot.itemCountText, slot.transform.position, Quaternion.identity).GetComponent<TextMeshProUGUI>();
                        countText.text = amtToAdd.ToString();
                        countText.transform.SetParent(icon.transform);
                        countText.transform.localPosition = Vector2.zero;
                        countText.transform.localScale = new Vector2(1, 1);
                        countText.raycastTarget = false;
                        slot.itemCountTextInstance = countText;

                        slot.icon = icon;
                        slot.itemCountInSlot = amtToAdd;
                        slot.durability = durability;

                        slot.itemData = itemData;
                        if (!item2Add.usesTier && itemData.tier > 0)
                        {
                            slot.itemData = new ItemData(0);
                        }

                        icon.transform.position = slot.gameObject.transform.position;
                        icon.transform.SetParent(slot.gameObject.transform);
                        icon.transform.SetAsFirstSibling();

                        slot.itemInSlot = item2Add;
                        icon.transform.localScale = new Vector2(itemIconScale, itemIconScale);

                        if (slot.itemCountInSlot == 1)
                        {
                            slot.itemCountTextInstance.text = string.Empty;
                        }
                    }


                }


            }
            //under the stack size
            else
            {
                slot.itemCountInSlot += itemCount;
                slot.itemCountTextInstance.text = slot.itemCountInSlot.ToString();

            }
        }
    }

    public void removeItem(inventorySlot slotRef)
    {

        if (slotRef.itemInSlot == null)
            return;

        slotRef.itemInSlot = null;
        slotRef.itemCountInSlot = 0;
        Destroy(slotRef.transform.Find("item icon").gameObject);
    }

    public void removeMouseItem()
    {
        Destroy(itemDragIcon);
        inventorySlot.itemInMouse = null;
        inventorySlot.itemCountInMouse = 0;

    }

    public Dictionary<item, int> GetItemCounts(bool includeMouse)
    {
        Dictionary<item, int> itemCounts = new Dictionary<item, int>();

        foreach (inventorySlot slot in slots)
        {
            if (slot.itemInSlot == null)
            {
                continue;
            }

            if (itemCounts.ContainsKey(slot.itemInSlot))
            {
                itemCounts[slot.itemInSlot] += slot.itemCountInSlot;
            }
            else
            {
                itemCounts.Add(slot.itemInSlot, slot.itemCountInSlot);
            }


        }
        //check for mouse
        if (includeMouse)
        {
            if (inventorySlot.itemInMouse != null)
            {

                if (itemCounts.ContainsKey(inventorySlot.itemInMouse))
                {
                    itemCounts[inventorySlot.itemInMouse] += inventorySlot.itemCountInMouse;
                }
                else
                {
                    itemCounts.Add(inventorySlot.itemInMouse, inventorySlot.itemCountInMouse);
                }

            }

        }

        return itemCounts;
    }

    public void UpdateFurnaceUI(placedTile tile)
    {
        if (tile.tileEntity.fuelTimeLeft > 0)
            fuelBar.fillAmount = Mathf.MoveTowards(fuelBar.fillAmount, tile.tileEntity.fuelTimeLeft / tile.tileEntity.maxFuelTime * 0.8572f, 4 * Time.deltaTime);
        else
            fuelBar.fillAmount = Mathf.MoveTowards(fuelBar.fillAmount, 0, 4 * Time.deltaTime); ;

        if (tile.tileEntity.smeltTimeLeft < TileEntity.smeltTime)
        {
            smeltBar.fillAmount = Mathf.MoveTowards(smeltBar.fillAmount, 1 - (tile.tileEntity.smeltTimeLeft / TileEntity.smeltTime), 5 * Time.deltaTime);

        }
        else
        {
            smeltBar.fillAmount = Mathf.MoveTowards(smeltBar.fillAmount, 0, 5 * Time.deltaTime);
        }

    }
    public void UpdatePurifierUI(placedTile tile)
    {
        if (tile.tileEntity.fuelTimeLeft > 0)
            purifierFuelBar.fillAmount = Mathf.MoveTowards(purifierFuelBar.fillAmount, tile.tileEntity.fuelTimeLeft / tile.tileEntity.maxFuelTime * 0.8572f, 4 * Time.deltaTime);
        else
            purifierFuelBar.fillAmount = Mathf.MoveTowards(purifierFuelBar.fillAmount, 0, 4 * Time.deltaTime); ;

        if (tile.tileEntity.smeltTimeLeft < TileEntity.purifierSmeltTime)
        {
            purifierSmeltBar.fillAmount = Mathf.MoveTowards(purifierSmeltBar.fillAmount, 1 - (tile.tileEntity.smeltTimeLeft / TileEntity.purifierSmeltTime), 5 * Time.deltaTime);
        }
        else
        {
            purifierSmeltBar.fillAmount = Mathf.MoveTowards(purifierSmeltBar.fillAmount, 0, 5 * Time.deltaTime);
        }


    }


    public void UpdateSpeedboatUI()
    {
        if (Boat.currentSpeedboatFueling.BoatType == Boat.boatType.speedboat)
        {
            if (Boat.currentSpeedboatFueling && Boat.currentSpeedboatFueling.fuelTime > 0)
                boatFuelBar.fillAmount = Mathf.MoveTowards(boatFuelBar.fillAmount, Boat.currentSpeedboatFueling.fuelTime / Boat.currentSpeedboatFueling.maxFuelTime * 0.8572f, 4 * Time.deltaTime);
            else
                boatFuelBar.fillAmount = Mathf.MoveTowards(boatFuelBar.fillAmount, 0, 4 * Time.deltaTime);

            if (Boat.currentSpeedboatFueling && Boat.currentSpeedboatFueling.currentPilot)
            {
                CloseInventory();
            }

        }
        else if (Boat.currentSpeedboatFueling.BoatType == Boat.boatType.battleship)
        {
            if (Boat.currentSpeedboatFueling && Boat.currentSpeedboatFueling.fuelTime > 0)
            {
                reactorFuelBar1.fillAmount = Mathf.MoveTowards(reactorFuelBar1.fillAmount, (Boat.currentSpeedboatFueling.fuelTime / Boat.currentSpeedboatFueling.maxFuelTime * 2) - 1, 4 * Time.deltaTime);
                reactorFuelBar2.fillAmount = Mathf.MoveTowards(reactorFuelBar2.fillAmount, (Boat.currentSpeedboatFueling.fuelTime / Boat.currentSpeedboatFueling.maxFuelTime * 2), 4 * Time.deltaTime);
            }
            else
            {
                reactorFuelBar1.fillAmount = Mathf.MoveTowards(boatFuelBar.fillAmount, 0, 4 * Time.deltaTime);
                reactorFuelBar2.fillAmount = Mathf.MoveTowards(boatFuelBar.fillAmount, 0, 4 * Time.deltaTime);
            }
        }
    }

    public void CreateBoat()
    {
        if (TileEntity.currentVStation && TileEntity.currentVStation.canPlaceBoat)
        {
            Dictionary<item, int> itemCounts = GetItemCounts(true);

            bool canCraft = true;

            if (!Chat.Instance.freeBoat)
            {
                foreach (ingredient Ingredient in boats[currentBoat].recipe)
                {
                    if (itemCounts.ContainsKey(Ingredient.Item))
                    {
                        if (itemCounts[Ingredient.Item] < Ingredient.count)
                        {
                            canCraft = false;
                            break;
                        }
                    }
                    else
                    {
                        canCraft = false;
                        break;
                    }
                }

                if (!canCraft) return;

                //remove from inventory
                foreach (ingredient Ingredient in boats[currentBoat].recipe)
                {
                    int removedAmount = 0;

                    foreach (inventorySlot slot in slots)
                    {
                        if (slot.itemInSlot == Ingredient.Item)
                        {
                            if (slot.itemCountInSlot > Ingredient.count - removedAmount)
                            {
                                slot.itemCountInSlot -= (Ingredient.count - removedAmount);
                                removedAmount = Ingredient.count;
                            }
                            else
                            {
                                removedAmount += slot.itemCountInSlot;
                                removeItem(slot);
                            }

                        }

                        if (removedAmount == Ingredient.count)
                        {
                            break;
                        }

                    }
                }


            }
            else
            {
                Chat.Instance.freeBoat = false;
            }

            //spawn boat
            if (canCraft)
            {
                gameManager.instance.InstantiateBoat(currentBoat, (Vector2)boatSpawnPos, boatSpawnScale, Guid.NewGuid().ToString(), false, "", null);
                gameManager.endScreenStats["boats"]++;
            }

            boatCraftButtonImage.transform.localScale = new Vector2(1.1f, 1.1f);
            boatCraftButtonImage.transform.DOScale(Vector2.one, 0.1f);

        }

        UpdateVehicleStationUI();
    }



    public void CheckCanMakeBoat()
    {
        if (!inventoryOpen)
        {
            return;
        }


        Dictionary<item, int> itemCounts = GetItemCounts(true);

        bool canCraft = true;

        if (!Chat.Instance.freeBoat)
        {
            foreach (ingredient Ingredient in boats[currentBoat].recipe)
            {
                if (itemCounts.ContainsKey(Ingredient.Item))
                {
                    if (itemCounts[Ingredient.Item] < Ingredient.count)
                    {
                        canCraft = false;
                        break;
                    }
                }
                else
                {
                    canCraft = false;
                    break;
                }
            }

        }

        if (!(TileEntity.currentVStation && TileEntity.currentVStation.canPlaceBoat))
        {
            canCraft = false;
        }

        if (canCraft && boatCraftButtonImage.color != canCraftColor)
        {
            boatCraftButtonImage.DOColor(canCraftColor, 0.1f);
        }
        else if (!canCraft && boatCraftButtonImage.color != cantCraftColor)
        {
            boatCraftButtonImage.DOColor(cantCraftColor, 0.1f);
        }


    }

    public void NextBoat(bool previous)
    {
        if (previous == false)
        {
            currentBoat += 1;
            if (currentBoat >= boats.Length)
            {
                currentBoat = 0;
            }
        }
        else
        {
            currentBoat -= 1;
            if (currentBoat < 0)
            {
                currentBoat = boats.Length - 1;
            }
        }

        pageTurnSound.Play();
        UpdateVehicleStationUI();
    }

    void UpdateVehicleStationUI()
    {
        boatIcon.sprite = boats[currentBoat].iconSprite;
        boatDescText.text = boats[currentBoat].description;
        boatNameText.text = boats[currentBoat].displayName;

        Dictionary<item, int> itemCounts = GetItemCounts(true);
        int index = 0;
        string displayRecipe = "Recipe: ";
        int amountOfIngred;

        foreach (ingredient Ingredient in boats[currentBoat].recipe)
        {
            if (itemCounts.ContainsKey(Ingredient.Item))
            {
                amountOfIngred = itemCounts[Ingredient.Item];
            }
            else
            {
                amountOfIngred = 0;
            }

            string amountOfIngredString = amountOfIngred.ToString();
            string countString = Ingredient.count.ToString();

            if (amountOfIngred > 1000)
                amountOfIngredString = "1000+";

            string ingredientString = $"{amountOfIngredString} / {countString} <b>{Ingredient.Item.displayName}</b>";

            // Set the text color based on the comparison
            string colorTag = amountOfIngred >= Ingredient.count
                ? $"<color=#{ColorUtility.ToHtmlStringRGB(canCraftTextColor)}>"
                : $"<color=#{ColorUtility.ToHtmlStringRGB(cantCraftTextColor)}>";

            displayRecipe += $"{colorTag}{ingredientString}</color>";
            index++;

            if (index != boats[currentBoat].recipe.Count())
            {
                displayRecipe += ", ";
            }
        }

        boatRecipeText.text = displayRecipe;

        if (TileEntity.currentVStation)
        {
            TileEntity.currentVStation.tile.boatPreview.sprite = boats[currentBoat].previewSprite;
            TileEntity.currentVStation.boatCheckOffset = boats[currentBoat].placeCheckOffset;
            TileEntity.currentVStation.boatCheckSize = boats[currentBoat].placeCheckSize;

            if (TileEntity.currentVStation.tile.boatPreview.transform.localScale.x > 0)
            {
                boatSpawnPos = TileEntity.currentVStation.tile.boatPreview.transform.position + (Vector3)boats[currentBoat].spawnOffset;
            }
            else
            {
                boatSpawnPos = TileEntity.currentVStation.tile.boatPreview.transform.position + new Vector3(-boats[currentBoat].spawnOffset.x, boats[currentBoat].spawnOffset.y);
            }

            CheckCanMakeBoat();
        }

    }

    public void UpdateAnvilUI()
    {
        repairRequireText.enabled = false;

        //repair
        if (anvilToolSlot.itemInSlot && (anvilToolSlot.itemInSlot.repairWith != null || anvilToolSlot.itemInSlot.uniqueType != item.UniqueType.none) && anvilMaterialSlot.itemInSlot
            && (anvilToolSlot.itemInSlot.repairWith == anvilMaterialSlot.itemInSlot || anvilToolSlot.itemInSlot == anvilMaterialSlot.itemInSlot || (anvilMaterialSlot.itemInSlot.soulRepairAmount > 0 && anvilToolSlot.itemInSlot.uniqueType != item.UniqueType.none))
            && anvilToolSlot.durability < anvilToolSlot.itemInSlot.maxDurability)
        {
            repairRequireText.enabled = true;
            if (anvilToolSlot.itemInSlot.uniqueType != item.UniqueType.none)
            {
                repairRequireText.text = "Repair Requires: 1 Soul Item";
            }
            else
            {
                repairRequireText.text = "Repair Requires: 1 " + anvilToolSlot.itemInSlot.repairWith.displayName;
            }

            if (!anvilOutputSlot.itemInSlot
                || anvilOutputSlot.itemInSlot != anvilToolSlot.itemInSlot
                || anvilOutputSlot.durability != anvilToolSlot.durability)
            {
                removeItem(anvilOutputSlot);

                int repairAmount = anvilToolSlot.itemInSlot.repairAmount;
                if (anvilToolSlot.itemInSlot == anvilMaterialSlot.itemInSlot) repairAmount = anvilMaterialSlot.durability;
                if (anvilMaterialSlot.itemInSlot.soulRepairAmount > 0)
                    repairAmount = Mathf.RoundToInt(anvilMaterialSlot.itemInSlot.soulRepairAmount * 1.5f * anvilToolSlot.itemInSlot.uniqueRepairMultiplier);

                anvilRepairAmount = repairAmount;

                ItemData data = anvilToolSlot.itemData;
                if (data.tier > 0)
                {
                    foreach (KeyValuePair<string, int> m in data.pMods)
                    {
                        if (m.Key == "repair")
                        {
                            anvilRepairAmount = Mathf.CeilToInt(anvilRepairAmount * (1 + (m.Value / 100f)));
                        }
                    }
                    foreach (KeyValuePair<string, int> m in data.nMods)
                    {
                        if (m.Key == "repair")
                        {
                            anvilRepairAmount = Mathf.CeilToInt(anvilRepairAmount * (1 + (m.Value / 100f)));
                        }
                    }
                }

                addItemInSlot(anvilToolSlot.itemInSlot, 1, Mathf.Clamp(anvilToolSlot.durability
                    + (anvilRepairAmount * anvilMaterialSlot.itemCountInSlot), 0, anvilToolSlot.itemInSlot.maxDurability)
                    , anvilToolSlot.itemData, anvilOutputSlot, anvilOutputSlot.transform.position);


                int itemsToRemove = Mathf.CeilToInt(
                    (float)(anvilToolSlot.itemInSlot.maxDurability - anvilToolSlot.durability)
                    / anvilRepairAmount);

                if (itemsToRemove > anvilMaterialSlot.itemCountInSlot) itemsToRemove = anvilMaterialSlot.itemCountInSlot;

                repairRequireText.text = "Repair Requires: " + itemsToRemove + " " + anvilMaterialSlot.itemInSlot.displayName;

            }

            anvilArrow.fillAmount = Mathf.Lerp(anvilArrow.fillAmount, 1, Time.deltaTime * 9);
        }
        //upgrade tier
        else if (anvilToolSlot.itemInSlot && anvilToolSlot.itemInSlot.usesTier && anvilToolSlot.itemData.tier < 4
            && anvilMaterialSlot.itemInSlot == upgradeTemplateItem
            )
        {
            repairRequireText.enabled = true;
            if (anvilToolSlot.itemInSlot.uniqueType == item.UniqueType.none)
            {
                repairRequireText.text = "Upgrade Requires: 1 Upgrade Template";
            }
            else
            {
                repairRequireText.text = "Upgrade Requires: 3 Upgrade Templates";
            }

            if (!(anvilToolSlot.itemInSlot.uniqueType != item.UniqueType.none && anvilMaterialSlot.itemCountInSlot < 3))
            {
                if (!anvilOutputSlot.itemInSlot || anvilOutputSlot.itemData.tier <= anvilToolSlot.itemData.tier)
                {
                    removeItem(anvilOutputSlot);

                    addItemInSlot(anvilToolSlot.itemInSlot, 1, anvilToolSlot.durability
                        , UpgradeItemData(anvilToolSlot.itemInSlot, anvilToolSlot.itemData)
                        , anvilOutputSlot, anvilOutputSlot.transform.position);


                }

                anvilArrow.fillAmount = Mathf.Lerp(anvilArrow.fillAmount, 1, Time.deltaTime * 9);

            }
            else
            {
                anvilArrow.fillAmount = Mathf.Lerp(anvilArrow.fillAmount, 0, Time.deltaTime * 6);
                if (anvilOutputSlot.itemInSlot)
                    removeItem(anvilOutputSlot);
            }

        }
        else
        {
            anvilArrow.fillAmount = Mathf.Lerp(anvilArrow.fillAmount, 0, Time.deltaTime * 6);
            if (anvilOutputSlot.itemInSlot)
                removeItem(anvilOutputSlot);
        }
    }

    public void AnvilRepairTake(bool upgrade)
    {
        int itemsToRemove = Mathf.CeilToInt(
            (float)(anvilToolSlot.itemInSlot.maxDurability - anvilToolSlot.durability)
            / anvilRepairAmount);

        if (upgrade)
        {
            itemsToRemove = 1;
            if (anvilToolSlot.itemInSlot.uniqueType != item.UniqueType.none) itemsToRemove = 3;

            gameManager.endScreenStats["upgrades"]++;
        }
        anvilMaterialSlot.itemCountInSlot -= itemsToRemove;
        if (anvilMaterialSlot.itemCountInSlot <= 0)
        {
            removeItem(anvilMaterialSlot);
        }

        removeItem(anvilToolSlot);

        repairSound.Play();
        anvilRepairSound.Play();
    }

    public void OnStatsClicked()
    {
        if ((inventorySlot.itemInMouse && !statsOpen) || inNewAbilityMenu) return;

        statsOpen = !statsOpen;
        OnExitNewAbilityMenu();

        openStatsSound.pitch = 1;
        if (statsOpen)
        {
            openStatsSound.Play();

            for (int i = 0; i < statAmounts.Count(); i++)
            {
                statBarFills[i].fillAmount = statAmounts[i] / 10f;
            }
        }
        else
        {
            inventoryOpenSound.pitch = 0.82f;
            inventoryOpenSound.Play();
        }

        statsAnim.SetBool("Show", statsOpen);
    }

    public void OnUpgradeStat(int i)
    {
        if (inNewAbilityMenu || newAbilityGroup.alpha > 0) return;

        item shardItem = gameManager.instance.itemDictionary["yellow_soul_shard"];

        switch (i)
        {
            case 1:
                shardItem = gameManager.instance.itemDictionary["blue_soul_shard"]; break;
            case 2:
                shardItem = gameManager.instance.itemDictionary["green_soul_shard"]; break;
            case 3:
                shardItem = gameManager.instance.itemDictionary["red_soul_shard"]; break;
            case 4:
                shardItem = gameManager.instance.itemDictionary["purple_soul_shard"]; break;

        }

        Dictionary<item, int> itemCounts = GetItemCounts(false);
        if (itemCounts.ContainsKey(shardItem) && ((statAmounts[i] < 10 && !maxStat) || statAmounts[i] < 9))
        {

            foreach (inventorySlot slot in slots)
            {
                if (slot.itemInSlot == shardItem && slot.itemCountInSlot > 0)
                {
                    if (slot.itemCountInSlot > 1)
                    {
                        slot.itemCountInSlot -= 1;
                    }
                    else
                    {
                        removeItem(slot);
                    }

                    break;
                }

            }

            statAmounts[i]++;
            statBarFills[i].DOFillAmount(statAmounts[i] / 10f, 0.45f);

            upgradeSound.clip = upgradeClip;

            if (statAmounts[i] == 10)
            {
                maxStat = true;
                upgradeSound.clip = maxUpgradeClip;

                newAbilityBackButton.SetActive(true);
                newAbilityGroup.DOFade(1.3f, 0.8f);
                newAbilityGroup.interactable = true;
                newAbilityGroup.blocksRaycasts = true;
                inNewAbilityMenu = true;
                statsBackButton.interactable = false;

                abilityTutorialImage.sprite = tutorialSprites[i];
                tutorialDesc.text = tutorialDescriptions[i];

            }

            upgradeSound.Play();
        }

    }

    public void OnExitNewAbilityMenu()
    {
        newAbilityGroup.DOFade(0, 0.4f);
        newAbilityGroup.interactable = false;
        newAbilityGroup.blocksRaycasts = false;
        inNewAbilityMenu = false;
        statsBackButton.interactable = true;
        newAbilityBackButton.SetActive(false);
    }

    public void SaveData(GameData data)
    {
        List<inventorySlot> allSlots = slots.ToList();
        allSlots.Add(headSlot);
        allSlots.Add(chestplateSlot);
        allSlots.Add(legsSlot);
        allSlots.Add(charmSlot);

        data.inventoryItemIDs = new string[allSlots.Count()];
        data.inventoryItemCounts = new int[allSlots.Count()];
        data.inventoryItemDurability = new int[allSlots.Count()];
        data.inventoryItemData = new string[allSlots.Count()];

        for (int i = 0; i < allSlots.Count; i++)
        {
            if (allSlots[i].itemInSlot)
            {
                data.inventoryItemIDs[i] = allSlots[i].itemInSlot.itemId;
                data.inventoryItemCounts[i] = allSlots[i].itemCountInSlot;
                data.inventoryItemDurability[i] = allSlots[i].durability;
                data.inventoryItemData[i] = ItemDataToString(allSlots[i].itemData);
            }
            else
            {
                data.inventoryItemIDs[i] = "null";
                data.inventoryItemCounts[i] = 0;
                data.inventoryItemDurability[i] = 0;
                data.inventoryItemData[i] = "null";
            }

        }

        List<string> extraIds = new List<string>();
        List<int> extraCounts = new List<int>();
        List<int> extraDurabilities = new List<int>();
        List<string> extraDatas = new List<string>();

        for (int i = 0; i < extraSlotsToSave.Count; i++)
        {
            if (extraSlotsToSave[i].itemInSlot && extraSlotsToSave[i].gameObject.activeSelf)
            {
                extraIds.Add(extraSlotsToSave[i].itemInSlot.itemId);
                extraCounts.Add(extraSlotsToSave[i].itemCountInSlot);
                extraDurabilities.Add(extraSlotsToSave[i].durability);
                extraDatas.Add(ItemDataToString(extraSlotsToSave[i].itemData));
            }

        }
        data.extraInventoryItemIDs = extraIds.ToArray();
        data.extraInventoryItemCounts = extraCounts.ToArray();
        data.extraInventoryItemDurability = extraDurabilities.ToArray();
        data.extraInventoryItemData = extraDatas.ToArray();

        if (inventoryOpen && inventorySlot.itemInMouse)
        {
            data.mouseItemId = inventorySlot.itemInMouse.itemId;
            data.mouseItemCount = inventorySlot.itemCountInMouse;
            data.mouseItemDurability = inventorySlot.durabilityInMouse;
            data.mouseItemData = inventorySlot.itemDataInMouse;
        }
        else
        {
            data.mouseItemId = "null";
            data.mouseItemCount = 0;
            data.mouseItemDurability = 0;
            data.mouseItemData = new ItemData(0);
        }

        data.selectedHotbarSlot = inventoryHightlighter.instance.equipppedSlot;
        data.statAmounts = statAmounts;
    }
    public void LoadData(GameData data)
    {
        if (!DataPersistanceManager.instance.loadingSave) return;

        for (int i = 0; i < data.inventoryItemIDs.Count(); i++)
        {
            if (data.inventoryItemIDs[i] == null || !gameManager.instance.itemDictionary.ContainsKey(data.inventoryItemIDs[i]))
            {
                continue;
            }

            //fix execution 0 durability
            if (data.inventoryItemIDs[i] != "null" && gameManager.instance.itemDictionary[data.inventoryItemIDs[i]].durabilityEmptyItem && data.inventoryItemDurability[i] == 0)
            {
                data.inventoryItemIDs[i] = gameManager.instance.itemDictionary[data.inventoryItemIDs[i]].durabilityEmptyItem.itemId;
            }

            if (data.inventoryItemData == null) data.inventoryItemData = new string[data.inventoryItemIDs.Count()];

            if (data.inventoryItemIDs[i] != null && gameManager.instance.itemDictionary[data.inventoryItemIDs[i]] != null && i < slots.Count())
                addItemInSlot(gameManager.instance.itemDictionary[data.inventoryItemIDs[i]], data.inventoryItemCounts[i], data.inventoryItemDurability[i], StringToItemData(data.inventoryItemData[i]), slots[i], slots[i].transform.position);
        }

        //saved mouse item
        if (data.mouseItemId != null && gameManager.instance.itemDictionary[data.mouseItemId] != null)
        {
            AddItem(gameManager.instance.itemDictionary[data.mouseItemId], data.mouseItemCount, data.mouseItemDurability, data.mouseItemData);
        }

        //saved items in anvil slots and stuff
        if (data.extraInventoryItemIDs != null)
        {
            for (int i = 0; i < data.extraInventoryItemIDs.Count(); i++)
            {
                if (data.extraInventoryItemIDs[i] == null) continue;

                AddItem(gameManager.instance.itemDictionary[data.extraInventoryItemIDs[i]]
                    , data.extraInventoryItemCounts[i]
                    , data.extraInventoryItemDurability[i]
                    , StringToItemData(data.extraInventoryItemData[i]));
            }
        }

        inventoryHightlighter.instance.SetSelected(data.selectedHotbarSlot);

        statAmounts = data.statAmounts;
        int j = 0;
        foreach (Image fill in statBarFills)
        {
            if (statAmounts[j] == 10) maxStat = true;

            fill.fillAmount = statAmounts[j] / 10f;
            j++;
        }

        int slotCount = slots.Count();

        if (data.difficulty == 2) slotCount -= 9;

        if (data.inventoryItemIDs.Count() < slotCount) return;

        //armor
        if (data.inventoryItemIDs[slotCount] != null && gameManager.instance.itemDictionary.ContainsKey(data.inventoryItemIDs[slotCount]) && gameManager.instance.itemDictionary[data.inventoryItemIDs[slotCount]] != null)
            addItemInSlot(gameManager.instance.itemDictionary[data.inventoryItemIDs[slotCount]], data.inventoryItemCounts[slotCount], data.inventoryItemDurability[slotCount], StringToItemData(data.inventoryItemData[slotCount]), headSlot, headSlot.transform.position);

        if (data.inventoryItemIDs[slotCount + 1] != null && gameManager.instance.itemDictionary.ContainsKey(data.inventoryItemIDs[slotCount + 1]) && gameManager.instance.itemDictionary[data.inventoryItemIDs[slotCount + 1]] != null)
            addItemInSlot(gameManager.instance.itemDictionary[data.inventoryItemIDs[slotCount + 1]], data.inventoryItemCounts[slotCount + 1], data.inventoryItemDurability[slotCount + 1], StringToItemData(data.inventoryItemData[slotCount + 1]), chestplateSlot, chestplateSlot.transform.position);

        if (data.inventoryItemIDs[slotCount + 2] != null && gameManager.instance.itemDictionary.ContainsKey(data.inventoryItemIDs[slotCount + 2]) && gameManager.instance.itemDictionary[data.inventoryItemIDs[slotCount + 2]] != null)
            addItemInSlot(gameManager.instance.itemDictionary[data.inventoryItemIDs[slotCount + 2]], data.inventoryItemCounts[slotCount + 2], data.inventoryItemDurability[slotCount + 2], StringToItemData(data.inventoryItemData[slotCount + 2]), legsSlot, legsSlot.transform.position);

        if (data.inventoryItemIDs.Count() > slotCount + 3 && data.inventoryItemCounts.Count() > slotCount + 3 && data.inventoryItemDurability.Count() > slotCount + 3 &&
            data.inventoryItemIDs[slotCount + 3] != null && gameManager.instance.itemDictionary.ContainsKey(data.inventoryItemIDs[slotCount + 3]) && gameManager.instance.itemDictionary[data.inventoryItemIDs[slotCount + 3]] != null)
        {
            addItemInSlot(gameManager.instance.itemDictionary[data.inventoryItemIDs[slotCount + 3]], data.inventoryItemCounts[slotCount + 3], data.inventoryItemDurability[slotCount + 3], StringToItemData(data.inventoryItemData[slotCount + 3]), charmSlot, charmSlot.transform.position);
        }


    }

    public void TogglePyro()
    {
        player.instance.RPC_EnableFireCircle(true, !player.instance.fireCircleEnabled, chestplateSlot.itemData.tier >= 4);
        pickupSound.Play();
    }

    [Serializable]
    public struct boatType
    {
        public GameObject boatObj;
        public ingredient[] recipe;
        public Sprite previewSprite;

        public Sprite iconSprite;
        public string displayName;
        [TextArea(3, 15)]
        public string description;

        public Vector2 placeCheckSize, placeCheckOffset, spawnOffset;

    }
}

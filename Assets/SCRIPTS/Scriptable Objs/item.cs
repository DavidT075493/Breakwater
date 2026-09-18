using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
[Serializable]
public class item: ScriptableObject
{
    public Sprite overworldSprite;
    public Sprite naturalSpawnSprite;
    public bool naturalSpawnInvisible;
    public string itemId;
    public string displayName = "missingno";
    [TextArea(3,15)]
    public string desc = "uh oh looks like someone forgot to write the descritpion";
    public Material matOverride;
    public bool unlitWhenDropped;
    public enum itemType
    {
        item,
        tool,
        block,
        remover,
        food,
        armorHead,
        armorChest,
        armorLegs,
        map,
        catalyst,
        recall,
        tracker,
        bucketE, bucketF,
        grapple,
        charm,
        farmRecipe
    }
    public itemType type;
    public bool overrideRecipeLocation;
    public itemType overrideRecipeItemType;

    public int hitboxIndex;

    public float mouseSpeed = 8;
    public int damage = 1;
    public int damageVariance = 0;
    public Color damageNumColor = new Color(1, 0, 0);
    public string deathMessage = "was killed by /p1 using /item";
    public float knockback = 5;
    public float knockbackTime = 0.1f;


    public AudioClip overrideSwingSound;

    public int maxStackSize = 50;
    public float throwForce = 7.5f;

    public Vector3 holdOffset;
    public float holdRotation;

    //used to spawn berry bushes
    public GameObject spawnWithGameObj;
    
    //only when spawning naturally
    public string spawnSortLayerName = "Item";
    public bool naturalSpawnFlipX;
    public short spawnSortOrder = 5;
    public int soulRepairAmount = 0;

    [Header("Tool")]
    public int treeDamage = 1;
    public int rockDamage = 1;
    public int toolTier = 0;
    
    public int maxDurability = 1;
    public bool usesDurability;
    public item durabilityEmptyItem;
    public Color durabilityBarColor = new Color(0.2f, 1, 0);
    public string durabilityName = "Durability";
    
    public bool twoHanded;
    public bool scytheAnimation;
    public float trailThicknessMult = 1;
    public Color trailColor = Color.white;
    public bool disableSwordAnim;

    public bool usesTier;
    public int[] tierWeights = new int[]{ 5, 6, 4, 2 };
    public ToolModifierGroup[] toolModifierPool;
    public item repairWith;
    public int repairAmount;
    public bool isRepairMaterial;

    public string maxTierModifier = "";
    public Sprite flippedSprite;

    [Header("Placeable Tiles")]
    public bool floor;
    public bool canRotate = false;
    public int health = 8;
    public float overrideBuildDist = -1;
    public bool interactable;
    public float interactBoxSize = 2;
    public string interactAction = "interact";
    public bool exclusiveInteract;
    public bool allowOnBoat;
    public Vector2 boatTileOffset;
    public bool rockTile;
    public enum tileType
    {
        wall,
        floor,
        sign,
        door,
        torch,
        furnace,
        vehicleStation,
        chest,
        crafting,
        purifier,
        respawn,
        campfire,
        crop,
        ominousFlower,
        anvil
    }

    public tileType TileType;
    public FootstepSurface floorStepSound;

    public tilesSettings[] tileStates = new tilesSettings[1];
    public Vector2Int growthTicks, wateredTicks;
    public int wateredAdditionalGrowth = 2;
    public GrowthStage[] growthStages;
    public bool resetGrowthWhenCollected;
    public bool tileDealDamage;
    public int tileDamage = 4;
    public float tileKnockback = 3;
    public string tileDeathMessage = "died";
    public Sprite overrideBuildPreviewSprite;
    public item[] onlyPlaceOnTiles = new item[0];
    public TileBase[] onlyPlaceOnGroundTiles = new TileBase[0];
    public Vector2[] onlyPlaceAtPositions = new Vector2[0];

    [Header("Furnace")]
    public bool fuel;
    public bool onlyInPurifier;
    public float fuelSmeltTime = 15;
    public bool reactorFuel;
    public item smeltResult;
    public Sprite alternateTileSprite;
    [Header("Food")]
    public float eatTime = 2;
    public int restoreHealth = 3;
    public AudioClip overrideEatCompleteSound;
    [Header("Armor")]
    public ArmorSpriteSheet[] armorSpriteSheet;
    public float armorMitigationPercent;
    [Header("Catalyst")]
    public CatalystEffect catalystEffect;
    public enum CatalystEffect
    {
        none,
        stamina,
        health,
        strength,
        speed,
        random,
        all
    }
    public int catalystEffectStrength;
    public float catalystEffectTime;
    public AudioClip catalystSound;

    public enum UniqueType
    {
        none,
        lifesteal,
        smelt,
        profiteer,
        ice,
        mirror,
        frost,
        fire,
        shield,
        gavel,
        execution,
        jackpot,
        breaker,
        attunement

    }
    public UniqueType uniqueType = UniqueType.none;
    public float uniqueRepairMultiplier = 1;
    public bool ignoreDurabilityModifiers;
    public bool obfuscateRecipeDesc;

    public const float soulBreakerCritDamageMult = 2.9f;

    [Header("Charm")]
    public StatChanged[] charmStatsChanged = new StatChanged[1];
    public int[] charmStatsChangedAmount = new int[1];
    public enum StatChanged
    {
        Mining,
        Speed,
        Stamina,
        Health,
        Combat
    }

    public string charmEffect;

    public item resourceTrackerItem;
}
[Serializable]
public class tilesSettings
{
    public Sprite tileSprite;
    public int tileColliderIndex;
    public string sortLayerName = "Item";
    public short sortOrder = 3;

}
[Serializable]
public class ArmorSpriteSheet
{
    public Sprite[] armorSprites;
    public Material materialOverride;
}

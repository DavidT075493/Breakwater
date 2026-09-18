using DG.Tweening;
using Fusion;
using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class playerHealth : NetworkBehaviour
{
    public static playerHealth instance;

    public int maxHealth = 100;
    public int startMaxHealth;
    public int currentHealth;
    public Rigidbody2D rb;
    public bool beingKnocked;
    public Vector2 knockbackVector;

    public SpriteRenderer sprite;
    public NetworkObject view;
    public camShakeController camShake;
    public GameObject hitParticles, reflectParticles, executeParticles, soulBreakerParticles, attuneParticles, attuneKillParticles;
    public Animation deadAnim;
    [HideInInspector]
    public bool dead;
    [HideInInspector]
    public Vector2 respawnPos;
    public HandMove handMove;
    public AudioSource respawnSound;
    public bool invincibility;

    public TileBase[] valeSpawningTiles;

    //for saved death
    bool dontOverwriteSoulItems;

    //dementia mode only
    public bool permaDead;
    public GameObject damageNum;
    bool diedInVale;
    private bool lostSoul;

    //0 = no regen
    public float currentCatalystRegenInterval;
    float catalystRegenTimer;

    bool regenCharmActive;

    [SerializeField] SpriteRenderer sentinelShieldSprite;
    [SerializeField] AudioSource sentinelShieldSound, shieldBreakSound;
    float sentinelShieldTime;
    [SerializeField] Transform thorns;
    Coroutine thornsCo;
    Tween thornsTween;
    [SerializeReference] damagePlayer thornsDamage;
    public GameObject deathHands;
    public Animator deathHandsAnim;
    public AudioSource deathHandsSound;
    public GameObject prideBreakEffect;

    public TextMeshPro envyText, envyText2;
    public int envyEnemiesKilled;
    public bool inEnvyState;
    public ParticleSystem envyPs;
    public AudioSource envySound, envyKillSound;
    Color envyTextColor;
    public float iceEffectTime;
    bool underIceEffect;
    public SpriteRenderer damageFlashSprite;
    private bool mirrorInvincible;
    public AudioSource mirrorInvincibleSound;
    private bool counterInvincibility;
    private bool cactusThornsCooldown;
    public bool inHeatSource;
    public float freezeEffectAmount;
    public GameObject warmUpEffect;

    private void Awake()
    {
        startMaxHealth = maxHealth;
    }

    void Start()
    {
        freezeEffectAmount = -50;

        damageFlashSprite.color = new Color(1, 0.5f, 0.5f, 0);
        deathHands.SetActive(false);
        thorns.gameObject.SetActive(false);
        //currentHealth = maxHealth;

        envyTextColor = envyText.color + Color.black;
        envyText.alpha = 0;
        envyText2.alpha = 0;

        if (view.HasInputAuthority)
        {
            instance = this;

            respawnPos = gameManager.instance.initialSpawnPos;

            if (gameManager.instance.hasRespawnPoint)
            {
                gameManager.instance.savedRespawnTile.SetSpawnPoint(true);
            }

            menu.instance.respawnButtonGroup.interactable = false;
            menu.instance.respawnButtonGroup.blocksRaycasts = false;

            if (gameManager.instance.savedPlayerHealth > 0)
                currentHealth = gameManager.instance.savedPlayerHealth;
            else
                currentHealth = maxHealth;

            if (playerSpawner.instance.savedPlayerDead)
            {
                dontOverwriteSoulItems = true;
                currentHealth = 0;
                StartCoroutine(_Knockback(0, 0.5f, Vector3.zero, "", false, false,false));
            }

        }
    }

    int GetMaxHealthWithFreeze()
    {
        if (freezeEffectAmount < 0) return maxHealth;

        return maxHealth - Mathf.RoundToInt(freezeEffectAmount);
    }

    IEnumerator _CounterInvincibility()
    {
        counterInvincibility = true;
        yield return new WaitForSeconds(1);
        counterInvincibility = false;
    }

    IEnumerator _DivineGuardCactusCooldown()
    {
        cactusThornsCooldown = true;
        yield return new WaitForSeconds(0.5f);
        cactusThornsCooldown = false;
    }

    [Rpc(RpcSources.All,RpcTargets.All)]
    void RPC_DivineGuardCactusThorns()
    {
        if (thornsCo != null) StopCoroutine(thornsCo);
        if (thornsTween != null) thornsTween.Kill();

        thornsCo = StartCoroutine(_CactusThorns());
    }

    public bool hurtPlayer(int damage, float knockback, float knockbackTime, Vector3 collisionPoint, string deathMessage, bool fromPlayer, bool iceEffect, int fromBoatFloor, string Item, Vector2 attackerPos, bool jackpot = false, bool soulCrit = false)
    {
        if (invincibility)
        {
            if (handMove.net_charmEffect == "cactus" && !cactusThornsCooldown)
            {
                RPC_DivineGuardCactusThorns();
                StartCoroutine(_DivineGuardCactusCooldown());
            }
        }
        

        if (beingKnocked || invincibility || inEnvyState || counterInvincibility || (fromBoatFloor != player.instance.boatFloor && fromBoatFloor != -2)
            || player.instance.inCutscene || handEvents.instance.grappleImmunity || player.instance.duelGhostSpectator || handMove.teleporting
            || playerWater.instance.drowning || mirrorInvincible)
            return false;

        if (handMove.playerParent.countering)
        {
            StartCoroutine(_CounterInvincibility());
            handMove.playerParent.RPC_CounterSuccess(attackerPos);
            return false;
        }

        if (sentinelShieldTime > 0)
        {
            RPC_SentinelBreak();
            RPC_DamageNum(0, "null", jackpot, false);
            RPC_HurtPlayer(view.Id, knockback, knockbackTime, collisionPoint, deathMessage, fromPlayer, true, false, "none", false);
            return false;
        }

        if (inventory.charmEffect == "invincibl")
        {
            RPC_DamageNum(0, "null", jackpot, false);
            RPC_HurtPlayer(view.Id, knockback, knockbackTime, collisionPoint, deathMessage, fromPlayer, true, false, "none", false);
            return false;
        }

        if (player.instance.pilotingBoat)
        {
            player.instance.currentBoat.ExitPilot();
        }
        if (player.instance.inBoatAltAction)
        {
            player.instance.currentBoat.ExitAltAction(player.instance.currentAltAction);
        }

        //mirror helmet
        if (inventory.instance.headSlot.itemInSlot && inventory.instance.headSlot.itemInSlot.uniqueType == item.UniqueType.mirror && gameManager.PercentChance(14)
            && !player.instance.InVale())
        {
            RPC_DamageNum(0, "null", jackpot, false);
            RPC_HurtPlayer(view.Id, knockback, knockbackTime, collisionPoint, deathMessage, fromPlayer, true, false, "none", false);
            return false;
        }

        damage = Mathf.RoundToInt(damage * DungeonGenerator.instance.GetModifier("take damage"));

        if (player.instance.prideActive)
        {
            damage = Mathf.RoundToInt(damage * 1.1f);
            damage += 5;
            RPC_PrideBreak();
        }
        player.instance.prideTimer = 0;

        //variance
        if (Item != "null")
        {
            damage += Random.Range(-gameManager.instance.itemDictionary[Item].damageVariance, gameManager.instance.itemDictionary[Item].damageVariance + 1);
        }
        bool negativeDamage = damage <= 0;

        if (player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
            damage -= Mathf.RoundToInt((float)damage * (inventory.instance.mitigationPercent * DungeonGenerator.instance.GetModifier("armor")) / 100);

        if (!negativeDamage && damage < 1)
        {
            damage = 1;
        }
        if (soulCrit)
        {
            damage = Mathf.RoundToInt(damage * item.soulBreakerCritDamageMult);
        }

        if (handMove.jackpotActive)
        {
            damage = Mathf.RoundToInt(damage * 0.75f);
            if (currentHealth - damage <= 0 && currentHealth > 10)
            {
                damage = currentHealth - 1;
            }

        }

        beingKnocked = true;
        currentHealth -= damage;
        gameManager.endScreenStats["damage"] += damage;

        if ((inventory.charmEffect == "sloth" || inventory.charmEffect == "wrath") && menu.instance.mushLevel < 100)
        {
            int mushPower = 7 + (damage / 4);
            if (fromPlayer) mushPower += damage / 2;
            if (inventory.charmEffect == "wrath") mushPower += damage / 2;

            menu.instance.mushLevel += mushPower;
            menu.instance.ChangeMushLevel();
        }

        if (currentHealth < 0) currentHealth = 0;
        if (handMove.jackpotActive) handMove.HitDuringJackpot();

        //armor durability
        if (inventory.instance.headSlot.itemInSlot)
        {
            inventory.instance.headSlot.durability--;
            if (inventory.instance.headSlot.durability <= 0)
            {
                handMove.ToolBreakSound(inventory.instance.headSlot.itemInSlot.uniqueType != item.UniqueType.none);
                
                if (inventory.instance.headSlot.itemInSlot.uniqueType != item.UniqueType.none)
                    inventory.instance.AddItem(inventory.instance.headSlot.itemInSlot, 1, 0, inventory.instance.headSlot.itemData);

                inventory.instance.removeItem(inventory.instance.headSlot);
                
            }
        }
        if (inventory.instance.chestplateSlot.itemInSlot)
        {
            inventory.instance.chestplateSlot.durability--;
            if (inventory.instance.chestplateSlot.durability <= 0)
            {
                handMove.ToolBreakSound(inventory.instance.chestplateSlot.itemInSlot.uniqueType != item.UniqueType.none);
                
                if (inventory.instance.chestplateSlot.itemInSlot.uniqueType != item.UniqueType.none)
                    inventory.instance.AddItem(inventory.instance.chestplateSlot.itemInSlot, 1, 0, inventory.instance.chestplateSlot.itemData);

                inventory.instance.removeItem(inventory.instance.chestplateSlot);
                
            }
        }
        if (inventory.instance.legsSlot.itemInSlot)
        {
            inventory.instance.legsSlot.durability--;
            if (inventory.instance.legsSlot.durability <= 0)
            {
                handMove.ToolBreakSound(inventory.instance.legsSlot.itemInSlot.uniqueType != item.UniqueType.none);
                
                if (inventory.instance.legsSlot.itemInSlot.uniqueType != item.UniqueType.none)
                    inventory.instance.AddItem(inventory.instance.legsSlot.itemInSlot, 1, 0, inventory.instance.legsSlot.itemData);

                inventory.instance.removeItem(inventory.instance.legsSlot);
                
            }
        }

        string effect = "none";
        item itemUsed = gameManager.instance.itemDictionary[Item];
        if (itemUsed)
        {
            if (soulCrit)
            {
                effect = "soul crit";
            }
            if (itemUsed.uniqueType == item.UniqueType.execution)
            {
                effect = "execution";
            }
            if (itemUsed.uniqueType == item.UniqueType.attunement)
            {
                effect = "attune";
            }
        }

        knockback *= 1 / inventory.instance.GetModifierInArmorSlots("knock");

        RPC_HurtPlayer(view.Id, knockback, knockbackTime, collisionPoint, deathMessage, fromPlayer, false, iceEffect, effect, currentHealth <= 0);

        RPC_DamageNum(damage, Item, jackpot, soulCrit);

        return true;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_DamageNum(int damage, string item, bool jackpot, bool soulCrit)
    {
        TextMeshPro damageNumText;

        damageNumText = Instantiate(damageNum, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)), Quaternion.identity).GetComponent<TextMeshPro>();
        Destroy(damageNumText.gameObject, 2);
        damageNumText.color = Color.red;

        if (item != "null")
            damageNumText.color = gameManager.instance.itemDictionary[item].damageNumColor;

        damageNumText.text = damage.ToString();

        if (damage < 0)
        {
            damageNumText.color = new Color(0.9f, 0, 0);
        }
        else if (damage == 0)
        {
            damageNumText.color = new Color(0.6f, 0.6f, 0.6f);
        }

        if (jackpot)
        {
            damageNumText.text = "777";
            damageNumText.enableAutoSizing = false;
            damageNumText.fontSize = 16;
            damageNumText.color = new Color(0, 1, 0);

            Instantiate(HandMove.instance.jackpotHitPs, transform.position, Quaternion.identity);
        }
        if (soulCrit)
        {
            damageNumText.enableAutoSizing = false;
            damageNumText.fontSize = 14;
        }
    }


    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = false)]
    void RPC_HurtPlayer(NetworkId viewID, float knockback, float knockbackTime, Vector3 collisionPoint, string deathMessage, bool fromPlayer, bool reflected, bool iceEffect, string effect, bool causeDeath)
    {
        if (view.Id != viewID)
            return;

        beingKnocked = true;
        bool fromAttuneSpear = false;

        if (reflected)
        {
            Instantiate(reflectParticles, transform.position, Quaternion.identity);

            if (view.HasStateAuthority && inventory.instance.headSlot.itemInSlot && inventory.instance.headSlot.itemInSlot.uniqueType == item.UniqueType.mirror
                && inventory.instance.headSlot.itemData.tier >= 4)
                RPC_MirrorInvinclibility();
        }
        else if (effect == "attune")
        {
            if (causeDeath)
            {
                Instantiate(attuneKillParticles, transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(attuneParticles, transform.position, Quaternion.identity);
            }

        }
        else if (effect == "execution")
        {
            Instantiate(executeParticles, transform.position, Quaternion.identity);
        }
        else if (effect == "soul crit")
        {
            Instantiate(soulBreakerParticles, transform.position, Quaternion.identity);
        }
        else
        {
            Instantiate(hitParticles, transform.position, Quaternion.identity);
        }

        if (!reflected && handMove.net_charmEffect == "shield")
        {
            ActivateSentinel(fromPlayer);
        }
        if (!reflected && handMove.net_charmEffect == "cactus")
        {
            if (thornsCo != null) StopCoroutine(thornsCo);
            if (thornsTween != null) thornsTween.Kill();

            thornsCo = StartCoroutine(_CactusThorns());
        }

        if (fromAttuneSpear)
        {

        }

        StartCoroutine(_Knockback(knockback, knockbackTime, collisionPoint, deathMessage, iceEffect, reflected,fromPlayer));
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SentinelBreak()
    {
        shieldBreakSound.Play();
        sentinelShieldTime = 0;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PrideBreak()
    {
        handMove.FinishCatalyst();
        GameObject effect = Instantiate(prideBreakEffect, transform.position, Quaternion.identity);
        Destroy(effect, 2);
    }

    void ActivateSentinel(bool fromPlayer)
    {
        sentinelShieldSprite.DOFade(1, 0.2f);
        sentinelShieldTime = 6.5f;
        if (fromPlayer) sentinelShieldTime = 4f;

        sentinelShieldSound.Play();
    }

    IEnumerator _CactusThorns()
    {
        thornsDamage.ignorePlayers = view.InputAuthority == SingletonRunner.runner.LocalPlayer;
        thorns.gameObject.SetActive(false);
        yield return null;
        thorns.gameObject.SetActive(true);

        thorns.localScale = Vector2.zero;
        thornsTween = thorns.DOScale(new Vector3(1.1f, 1.1f), 0.2f).SetEase(Ease.OutBounce);
        yield return new WaitForSeconds(0.2f);
        thornsTween = thorns.DOScale(Vector2.zero, 0.3f);
        yield return new WaitForSeconds(0.3f);
        thorns.gameObject.SetActive(false);
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_PlayerDie(NetworkId netId, bool disableSound)
    {
        player deadPlayer = Runner.FindObject(netId).GetComponent<player>();
        deadPlayer.health.deadAnim.Play();
        
        deathHands.SetActive(true);
        deathHandsAnim.SetBool("Envy State", false);
        deathHandsSound.Play();
        
        beingKnocked = true;
        dead = true;
        handMove.selectedItem = null;
        deadPlayer.heldItemSprite.sprite = null;

        sprite.DOColor(new Color(1, 0, 0, 0), 4);
        deadPlayer.usernameText.DOFade(0, 4);
        deadPlayer.HandEvents.handMove.sprite.DOFade(0, 4);
        deadPlayer.heldItemSprite.DOFade(0, 4);
        deadPlayer.anim.SetBool("Walking", false);

        if (menu.instance.spectatingPlayer == deadPlayer)
        {
            menu.instance.SpectateNext();
        }

        bool permadead = false;

        if (gameManager.inBoss || gameManager.instance.difficulty == 2)
        {
            bool alive = false;

            permaDead = true;

            foreach (player p in playerSpawner.instance.players)
            {
                if (p.health.currentHealth > 0)
                {
                    alive = true;
                    break;
                }
            }
            if (!alive)
            {
                menu.instance.PermaDead();
                permadead = true;
                DOTween.To(() => AudioListener.volume, x => AudioListener.volume = x, 0, 4);
            }
        }

        deadPlayer.HandEvents.handMove.FinishCatalyst();

        handMove.shieldPs.Stop();

        if (deadPlayer == player.instance)
        {
            if (!disableSound)
                menu.instance.deathSound.Play();

            freezeEffectAmount = 0;

            gameManager.instance.musicSource.DOFade(0, 1);
            gameManager.instance.ambienceSource.DOFade(0, 1);
            DungeonGenerator.instance.battleMusic.DOFade(0, 1.5f);

            dead = true;
            player.instance.trigger.enabled = false;
            player.instance.collision.enabled = false;
            inventory.instance.CloseInventory(true);
            pauseScreen.instance.resume();
            menu.instance.OpenMap(false, true);
            menu.instance.FinishWritingSign();
            Chat.Instance.CloseChat();
            handMove.tilePreviewSprite.enabled = false;
            handEvents.instance.RPC_DeactivateHitboxes();

            if (!gameManager.inSecondPhase && player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
            {
                menu.instance.lastDeathPos = transform.position;
                if (player.instance.inDungeon > -1)
                    menu.instance.lastDeathPos = featurePlacer.Instance.dungeonFeatures[player.instance.inDungeon].selectedPosition;

                menu.instance.showDeathMarker = true;
                menu.instance.deathMarker.gameObject.SetActive(true);
            }
            //get off boat when die
            if (player.instance.currentBoat)
            {
                if (player.instance.currentBoat.insideColliders)
                    player.instance.currentBoat.insideColliders.SetActive(false);
                player.instance.currentBoat.PlayerRideBoat(player.instance.view.Id, false, true);
            }

            List<inventorySlot> allSlots = inventory.instance.slots.ToList();
            allSlots.Add(inventory.instance.headSlot);
            allSlots.Add(inventory.instance.chestplateSlot);
            allSlots.Add(inventory.instance.legsSlot);
            allSlots.Add(inventory.instance.charmSlot);

            string[] itemIds = new string[allSlots.Count];
            int[] itemCounts = new int[allSlots.Count];
            int[] itemDurabilities = new int[allSlots.Count];
            ItemData[] itemDatas = new ItemData[allSlots.Count];

            //remove all items
            for (int i = 0; i < allSlots.Count; i++)
            {
                if (!allSlots[i].itemInSlot)
                {
                    itemIds[i] = "null";
                    itemCounts[i] = 0;
                    itemDurabilities[i] = 0;
                    itemDatas[i] = new ItemData();
                    continue;
                }

                //fix execution 0 durability
                if (allSlots[i].itemInSlot.uniqueType == item.UniqueType.execution && allSlots[i].durability == 0)
                {
                    allSlots[i].itemInSlot = gameManager.instance.itemDictionary["gavel"];
                }

                //drop on ground if vale disabled
                if (gameManager.instance.disableVale && !gameManager.instance.inArena)
                {
                    GameObject droppedItem = Runner.Spawn(inventory.instance.dropItemPrefab, (Vector2)transform.position + new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f)), Quaternion.identity).gameObject;
                    itemPickup ItemPickup = droppedItem.GetComponent<itemPickup>();
                    ItemPickup.Item = allSlots[i].itemInSlot;
                    ItemPickup.count = allSlots[i].itemCountInSlot;
                    ItemPickup.SetItem(allSlots[i].itemInSlot.itemId, (short)allSlots[i].itemCountInSlot, (short)allSlots[i].durability, allSlots[i].itemData);
                }
                else
                {
                    itemIds[i] = allSlots[i].itemInSlot.itemId;
                    itemCounts[i] = allSlots[i].itemCountInSlot;
                    itemDurabilities[i] = allSlots[i].durability;
                    itemDatas[i] = allSlots[i].itemData;
                }

                inventory.instance.removeItem(allSlots[i]);
            }

            menu.instance.deadScreenText.text = "Your Soul Slips Into The Darkness";
            menu.instance.deathScreenImage.sprite = menu.instance.normalDeathImage;
            diedInVale = true;
            menu.instance.itemsLostHolder.gameObject.SetActive(false);
            menu.instance.uniquesLostHolder.gameObject.SetActive(false);

            //different death screen when dying without a soul
            if (player.instance.currentIsland.biome == (int)MapGenerator.biome.vale && ValeManager.instance.lostSoul)
            {
                if (!disableSound)
                    gameManager.endScreenStats["deaths soul"]++;

                menu.instance.deathScreenImage.sprite = menu.instance.permaDeathImage;
                menu.instance.deadScreenText.text = "You Have Failed To Recover Your Soul";
                diedInVale = false;

                menu.instance.SetPermaDeathScreenItems();

                //respawn unique items
                List<item> heldUniques = new List<item>();
                foreach (item i in ValeManager.instance.GetSoulItems())
                {
                    if (i && i.uniqueType != item.UniqueType.none) heldUniques.Add(i);
                }

                foreach (itemPickup iP in treeRPCs.Instance.uniqueItems)
                {
                    if (heldUniques.Contains(iP.Item))
                    {
                        iP.RPC_RespawnUnique();
                    }

                }

                ValeManager.instance.ClearInventory(true);

            }
            else
            {
                if (!disableSound)
                    gameManager.endScreenStats["deaths"]++;
            }

            if (gameManager.instance.difficulty == 2 || permaDead)
            {
                menu.instance.deadScreenText.text = "Your Soul Has Been Lost To The Darkness";
                menu.instance.deathScreenImage.sprite = menu.instance.permaDeathImage;
                menu.instance.itemsLostHolder.gameObject.SetActive(false);
                menu.instance.uniquesLostHolder.gameObject.SetActive(false);
            }
            else
            {
                //send all items to player's soul
                if (!gameManager.instance.disableVale && !dontOverwriteSoulItems && !ValeManager.instance.lostSoul)
                {
                    if (player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
                        ValeManager.instance.SetSoulItems(itemIds, itemCounts, itemDurabilities, itemDatas);

                    ValeManager.instance.RandomSoulPosition();
                }

                dontOverwriteSoulItems = false;

                if (inventorySlot.itemInMouse)
                {
                    GameObject droppedItem = Runner.Spawn(inventory.instance.dropItemPrefab, (Vector2)transform.position + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)), Quaternion.identity).gameObject;
                    itemPickup ItemPickup = droppedItem.GetComponent<itemPickup>();
                    ItemPickup.Item = inventorySlot.itemInMouse;
                    ItemPickup.count = inventorySlot.itemCountInMouse;
                    ItemPickup.SetItem(inventorySlot.itemInMouse.itemId, (short)inventorySlot.itemCountInMouse, (short)inventorySlot.durabilityInMouse, inventorySlot.itemDataInMouse);
                }
                inventory.instance.removeMouseItem();

            }



            if (!gameManager.inBoss && gameManager.instance.difficulty != 2)
            {
                menu.instance.respawnButtonText.text = "Respawn";
                if (!gameManager.instance.disableVale) menu.instance.respawnButtonText.text = "Proceed";
            }
            else
            {
                menu.instance.respawnButtonText.text = "Spectate";
            }


            if (!permadead)
            {
                Invoke("EnableRespawn", 3.6f);
                Invoke("DeathScreen", 1);

                //if (gameManager.instance.difficulty == 2) menu.instance.deathEmoji.sprite = menu.instance.dementiaEmoji;
            }

            MouseCursor.state = 1;
        }
        else
        {
            GetComponent<player>().playerLight.enabled = false;
        }


        //save when die
        if (!gameManager.inBoss)
        {
            DataPersistanceManager.instance.SaveGame(true);
        }

    }

    void DeathScreen()
    {
        menu.instance.deadScreen.DOFade(1, 3);
    }

    private void EnableRespawn()
    {
        menu.instance.respawnButtonGroup.interactable = true;
        menu.instance.respawnButtonGroup.blocksRaycasts = true;
    }

    public async void PlayerRespawn(NetworkId photonID)
    {
        deathHands.SetActive(false);
        menu.instance.deathFade.DOFade(1, 1);
        menu.instance.dungeonModifierText.gameObject.SetActive(false);

        await Task.Delay(1100);
        MapDisplay.Instance.tileGridGameObject.SetActive(true);

        menu.instance.deadScreen.DOFade(0, 1);

        currentHealth = maxHealth;
        dead = false;
        deadAnim.Stop();

        if (!gameManager.instance.disableVale)
        {
            //sent to vale when soul is intact
            if (diedInVale ||
                player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
            {
                //handMove.valeTeleportStartPos = Vector2.zero;
                player.instance.transform.position = RandomValePos();
                ValeManager.instance.RemoveNearbyEnemies();
            }
            else
            //sent back to overworld when dying without a soul
            {

                ValeManager.instance.EscapeVale();
                await Task.Delay(500);

                //handMove.valeTeleportStartPos = Vector2.zero;
                ValeManager.instance.lostSoul = false;
                currentHealth = maxHealth;

                player.instance.transform.position = respawnPos;
                if (TileEntity.currentSpawnPoint && TileEntity.currentSpawnPoint.boatParent)
                {
                    player.instance.BoatRespawn();
                }
            }
        }
        else
        {
            player.instance.transform.position = respawnPos;
            if (TileEntity.currentSpawnPoint && TileEntity.currentSpawnPoint.boatParent)
            {
                player.instance.BoatRespawn();
            }

        }

        await Task.Delay(500);
        
        player.instance.inDungeon = -1;
        player.instance.CheckIsland();


        float failsafeTimer = 0;

        while (player.instance.loadingFeatures && failsafeTimer < 6)
        {
            failsafeTimer += Time.deltaTime;
            await Task.Yield();
        }
        if (failsafeTimer >= 6) Debug.LogError("feature load failsafe");

        player.instance.trigger.enabled = true;
        player.instance.collision.enabled = true;

        RPC_PlayerRespawn(photonID);

        await Task.Delay(1000);

        MouseCursor.state = 0;

        menu.instance.deathFade.DOFade(0, 1);

        //dont show vale message if disabled
        if (gameManager.instance.disableVale) return;

        await Task.Delay(12000);

        ValeManager.instance.StartSoulFlash();

    }

    public async void ReturnFromVale()
    {
        menu.instance.deathFade.DOFade(1, 1);

        player.instance.inCutscene = true;

        await Task.Delay(1100);

        if (TileEntity.currentSpawnPoint && TileEntity.currentSpawnPoint.boatParent)
        {
            player.instance.BoatRespawn();
        }
        else
        {
            player.instance.transform.position = respawnPos;
        }

        //handMove.valeTeleportStartPos = Vector2.zero;

        ValeManager.instance.ReturnSoulItems();

        await Task.Delay(2000);
        player.instance.CheckIsland();
        if (!TimeManager.instance.isNight)
        {
            StartCoroutine(gameManager.instance._SwitchMusic("day", 1));
        }
        else
        {
            StartCoroutine(gameManager.instance._SwitchMusic("night", 1));
        }

        currentHealth = maxHealth;
        dead = false;

        menu.instance.deathFade.DOFade(0, 1);

        await Task.Delay(800);

        ValeManager.instance.DropValeInvItems();
        player.instance.inCutscene = false;
        ValeManager.instance.HideValeText();
    }


    public Vector2 RandomValePos(bool spawnNearOthers = false)
    {
        Vector2Int spawnPos;

        int failsafe = 0;

        List<Transform> players = new List<Transform>();
        if (spawnNearOthers)
        {
            foreach (player p in playerSpawner.instance.players)
            {
                if (p.currentIsland.biome == (int)MapGenerator.biome.vale)
                {
                    players.Add(p.transform);
                }

            }

        }

        while (true)
        {
            failsafe++;

            spawnPos = Vector2Int.RoundToInt(MapGenerator.instance.valeLocation.position) + new Vector2Int(
                Random.Range(-MapGenerator.valeMapSize / 2, MapGenerator.valeMapSize / 2),
                Random.Range(-MapGenerator.valeMapSize / 2, MapGenerator.valeMapSize / 2));

            if ((CheckCircleForTiles(valeSpawningTiles, MapDisplay.Instance.groundTilemap, (Vector3Int)spawnPos, 2)
                && CheckCircleForTiles(new TileBase[0], MapDisplay.Instance.waterTilemap, (Vector3Int)spawnPos, 2, true)
                && CheckCircleForTiles(new TileBase[0], MapDisplay.Instance.hillTilemap, (Vector3Int)spawnPos, 3, true)
                && CheckCircleForTiles(new TileBase[0], MapDisplay.Instance.lavaTilemap, (Vector3Int)spawnPos, 4, true)
                && Vector2.Distance(spawnPos, ValeManager.instance.soul.transform.position) > 100
                && Vector2.Distance(spawnPos, ValeManager.instance.soul.transform.position) < 250
                ) || failsafe > 100000)
            {
                // recall tether - spawn near others
                if (spawnNearOthers && players.Count() > 0)
                {
                    bool valid = false;

                    foreach (Transform t in players)
                    {
                        if (Vector2.Distance(spawnPos, t.position) < 80)
                        {
                            valid = true;
                        }

                    }

                    if (valid || failsafe > 100000)
                    {
                        break;
                    }

                }
                //normal
                else
                {
                    break;
                }


            }
        }

        return spawnPos;
    }

    bool CheckCircleForTiles(TileBase[] allowedTiles, Tilemap tilemap, Vector3Int center, int radius = 2, bool requireEmpty = false)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (Vector3Int.Distance(center, pos) > radius)
                    continue;

                TileBase tile = tilemap.GetTile(pos);

                if (requireEmpty)
                {
                    // Must be no tile at all
                    if (tile != null)
                        return false;
                }
                else
                {
                    // Tile must be one of the allowed tiles
                    if (!allowedTiles.Contains(tile))
                        return false;
                }
            }
        }

        return true;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_PlayerRespawn(NetworkId photonID)
    {
        player p = GetComponent<player>();
        p.iceSpeedMult = 1;
        deathHands.SetActive(false);

        damageFlashSprite.DOFade(0, 0);
        currentHealth = maxHealth;
        dead = false;
        deadAnim.Stop();
        beingKnocked = false;
        deadAnim.transform.rotation = Quaternion.identity;
        deadAnim.transform.localPosition = Vector3.zero;

        sprite.color = Color.white;
        p.usernameText.DOFade(1, 0);

        handMove.playerParent.heldItemSprite.color = Color.white;
        handMove.sprite.color = Color.white;

        player deadPlayer = Runner.FindObject(photonID).GetComponent<player>();
        if (deadPlayer == player.instance)
        {
            handMove.unlockingExecution = false;

            player.instance.collision.isTrigger = false;
            menu.instance.respawnButtonGroup.interactable = false;
            menu.instance.respawnButtonGroup.blocksRaycasts = false;
        }
        else
        {
            GetComponent<player>().playerLight.enabled = true;
        }

        deadPlayer.usernameText.color = Chat.Instance.usernameColors[DataPersistanceManager.instance.GetActorIndex(deadPlayer.view.InputAuthority)];

        //respawnSound.Play();

    }

    void StartMusic()
    {
        if (!TimeManager.instance.isNight)
            StartCoroutine(gameManager.instance._SwitchMusic("day", 0));
        else
            StartCoroutine(gameManager.instance._SwitchMusic("night", 0));

    }

    public IEnumerator _Knockback(float knockback, float knockbackTime, Vector3 collisionPoint, string deathMessage, bool iceEffect, bool negated, bool fromPlayer)
    {
        rb.linearVelocity = Vector2.zero;
        if (handMove.net_charmEffect == "cactus")
        {
            yield return new WaitForSeconds(0.1f);
            knockback *= 0.8f;
        }
        knockback *= DungeonGenerator.instance.GetModifier("knockback", handMove.playerParent.inDungeon);

        knockbackVector = (GetComponent<player>().collision.transform.position - collisionPoint).normalized * knockback;

        Coroutine co = StartCoroutine(_damageFlash(negated));
        if (!handMove.jackpotActive)
        {
            yield return new WaitForSeconds(knockbackTime * 4/5);

            knockbackVector = Vector2.zero;

            yield return new WaitForSeconds(knockbackTime * 1/5);
        }
        rb.linearVelocity = Vector2.zero;
        beingKnocked = false;

        if (currentHealth <= 0)
        {
            handMove.playerParent.anim.SetBool("Walking", false);

            sprite.color = Color.white;
            if (co != null)
                StopCoroutine(co);

            damageFlashSprite.DOFade(0, 1);

            if (view.HasInputAuthority)
            {
                if (inventory.charmEffect != "envy" || playerSpawner.instance.savedPlayerDead)
                {
                    Die(deathMessage);
                }
                else
                {
                    StartCoroutine(_EnvyState(deathMessage, false));
                }
            }

            playerSpawner.instance.savedPlayerDead = false;
        }
        else
        {
            if (iceEffect)
            {
                if (!fromPlayer)
                {
                    iceEffectTime = 3.5f;
                    freezeEffectAmount += 8;
                }
                else
                {
                    iceEffectTime = 2.5f;
                    freezeEffectAmount += 12;
                }
            }

            sprite.color = Color.white;
        }

    }


    void Die(string deathMessage)
    {
        if (inventory.charmEffect == "invincibl") return;

        if (!Chat.Instance.disableDeathMessages && deathMessage != "")
            Chat.Instance.SendInChat(new string[] { DataPersistanceManager.instance.localUsername + " ", deathMessage }, new Color[] { Chat.Instance.usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)], new Color(1, 0.5f, 0.5f) });
        if (ValeManager.instance && ValeManager.instance.lostSoul && deathMessage != "")
        {
            Chat.Instance.SendInChat(new string[] { DataPersistanceManager.instance.localUsername + " ",
                        "failed to recover their soul" }, new Color[] { Chat.Instance.usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)], new Color(0.2f, 0.9f, 1f) });
        }
        RPC_PlayerDie(view.Id, deathMessage == "");
        playerSpawner.instance.envyKillRequirementAdd = 0;
    }

    public IEnumerator _EnvyState(string deathMessage, bool manualActivated)
    {
        inEnvyState = true;
        envyEnemiesKilled = 0;

        StartCoroutine(_EnvyKillSounds());

        int killRequirement = 3 + playerSpawner.instance.envyKillRequirementAdd;

        envyText.DOKill();
        envyText2.DOKill();

        envyText.DOFade(1, 0.2f);
        envyText2.DOFade(1, 0.2f);

        RPC_EnvyEffect(true);
        envyText.transform.parent.DOShakeRotation(10, 8, 7, 90, false);

        DOTween.To(() => AudioListener.volume, x => AudioListener.volume = x, 0.4f, 0.5f);

        if (!manualActivated)
        {
            deathHands.SetActive(true);
            deathHandsAnim.SetBool("Envy State", true);
        }

        for (float t = 10; t >= 0; t -= Time.deltaTime)
        {
            if(!manualActivated)
                currentHealth = 0;

            envyText.text = envyEnemiesKilled + "/" + killRequirement
            + "\n" + (Mathf.Clamp(Mathf.Round(t * 10) / 10, 0, Mathf.Infinity));

            if (envyEnemiesKilled == killRequirement)
            {
                envyText.color = Color.green;
                t -= Time.deltaTime * 1;
            }
            else if (envyEnemiesKilled > killRequirement)
            {
                envyText.color = Color.red;
                t -= Time.deltaTime * 10;
            }
            else
            {
                envyText.color = envyTextColor;
            }

            envyText2.color = envyText.color;
            yield return null;
        }

        if (envyEnemiesKilled != killRequirement)
        {
            DOTween.To(() => AudioListener.volume, x => AudioListener.volume = x, 1, 0.5f);
            if (!manualActivated)
            {
                deathHandsAnim.SetBool("Envy State", false);

                Die(deathMessage);
            }
            else
            {
                inEnvyState = false;
                yield return new WaitForSeconds(0.3f);

                currentHealth -= 30;
                RPC_HurtPlayer(view.Id, 0, 0, transform.position, deathMessage, false, false, true, "soul crit", currentHealth <= 0);

                RPC_DamageNum(30, "null", false, true);

            }
        }
        else
        {
            sprite.color = Color.white;

            if (!manualActivated)
            {
                freezeEffectAmount = -5;
                currentHealth = Mathf.RoundToInt(maxHealth * 0.5f);
            }
            else
            {
                freezeEffectAmount -= 50;
                currentHealth += Mathf.RoundToInt(maxHealth * 0.5f);
            }


            beingKnocked = false;
            respawnSound.Play();
            
            if(!manualActivated)
            playerSpawner.instance.envyKillRequirementAdd++;

            DOTween.To(() => AudioListener.volume, x => AudioListener.volume = x, 1, 1.5f);

            deathHandsAnim.SetTrigger("Hide");
            yield return new WaitForSeconds(0.3f);
            deathHands.SetActive(false);

        }

        RPC_EnvyEffect(false);

        envyText.DOFade(0, 1.2f);
        envyText2.DOFade(0, 1.2f);

        deathHandsAnim.SetBool("Envy State", false);
        inEnvyState = false;
        yield return new WaitForSeconds(1.2f);

    }

    IEnumerator _EnvyKillSounds()
    {
        int soundsPlayed = 0;
        envyKillSound.pitch = 0.5f;
        while (inEnvyState)
        {
            if (soundsPlayed < envyEnemiesKilled)
            {
                soundsPlayed++;
                envyKillSound.pitch += 0.2f;
                envyKillSound.Play();
                yield return new WaitForSeconds(0.3f);
            }

            yield return null;
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_EnvyEffect(bool enable)
    {
        envyKillSound.ignoreListenerVolume = true;
        envySound.ignoreListenerVolume = true;

        if (enable)
        {
            envyPs.Play();
            envySound.DOKill();
            envySound.pitch = 0.7f;
            envySound.volume = 1;
            envySound.Play();
        }
        else
        {
            envyPs.Stop();
            envySound.DOFade(0, 0.4f);
        }

    }

    private void Update()
    {
        if (!beingKnocked) rb.linearVelocity = Vector2.zero;

        //other players are always kinematic. necessary for boat movement
        if (!view.HasInputAuthority)
        {
            if (beingKnocked)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
        else
        {
            if (WeatherManager.Instance.snowstormSlowness)
            {
                iceEffectTime = 3.5f;
            }

            maxHealth = Mathf.RoundToInt(startMaxHealth * (1 + (inventory.instance.GetStatAmount(3) / 10f * inventory.instance.maxHealthMult)));
            if (ValeManager.instance.lostSoul && !lostSoul)
            {
                lostSoul = true;
                SoulLostHeal();
            }

            if (currentCatalystRegenInterval > 0)
            {
                catalystRegenTimer -= Time.deltaTime;
                if (catalystRegenTimer <= 0)
                {
                    currentHealth += 1;
                    catalystRegenTimer = currentCatalystRegenInterval;
                }
            }

            if (!regenCharmActive && inventory.charmEffect == "fractured")
            {
                regenCharmActive = true;
                SoulLostHeal();
            }

            if (TileEntity.currentSpawnPoint)
            {
                respawnPos = TileEntity.currentSpawnPoint.transform.position;
            }
            else
            {
                respawnPos = gameManager.instance.initialSpawnPos;
            }

        }

        if (iceEffectTime > 0 && !inHeatSource && handMove.playerParent.currentIsland.biome != (int)MapGenerator.biome.vale
            && !(inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.smelt)
            && !(inventory.instance.chestplateSlot && inventory.instance.chestplateSlot.itemInSlot.uniqueType == item.UniqueType.fire)
            && !handMove.jackpotActive)
        {
            iceEffectTime -= Time.deltaTime;
            if (!underIceEffect)
            {
                underIceEffect = true;
                sprite.DOColor(new Color(0.6f, 0.7f, 1), 0.3f);
                handMove.sprite.DOColor(new Color(0.6f, 0.7f, 1), 0.3f);
                handMove.playerParent.iceSpeedMult = 0.86f;
            }

            if(!invincibility && !handMove.playerParent.inCutscene && !dead && !inEnvyState 
                && !handMove.playerParent.InVale())
            freezeEffectAmount += Time.deltaTime * (0.8f + (0.15f * gameManager.instance.difficulty));

            if(freezeEffectAmount >= 100 && !dead && !inEnvyState && !beingKnocked && inventory.charmEffect != "invincibl")
            {
                beingKnocked = true;
                currentHealth = 0;
                StartCoroutine(_Knockback(0, 0, transform.position, "froze to death", true, false,false));
            }

        }
        else
        {
            iceEffectTime = 0;
            if (underIceEffect)
            {
                underIceEffect = false;
                sprite.DOColor(Color.white, 0.3f);
                handMove.sprite.DOColor(Color.white, 0.3f);
                handMove.playerParent.iceSpeedMult = 1;
                
                if(freezeEffectAmount > 0 && !warmUpEffect.activeSelf && !player.instance.InVale())
                {
                    warmUpEffect.SetActive(true);
                }

            }

            freezeEffectAmount -= Time.deltaTime * 18;
        }
        freezeEffectAmount = Mathf.Clamp(freezeEffectAmount, -16, 100);


        currentHealth = Mathf.Clamp(currentHealth, 0, GetMaxHealthWithFreeze());

        if (sentinelShieldTime > 0)
        {
            sentinelShieldTime -= Time.deltaTime;
        }
        else if (sentinelShieldSprite.color.a == 1)
        {
            sentinelShieldTime = 0;
            sentinelShieldSprite.DOFade(0, 0.3f);
        }

    }

    void SoulLostHeal()
    {
        if (!ValeManager.instance.lostSoul && inventory.charmEffect != "fractured")
        {
            lostSoul = false;
            regenCharmActive = false;
            return;
        }
        if (currentHealth > 0)
            currentHealth += 1;

        if (ValeManager.instance.lostSoul)
            Invoke("SoulLostHeal", 2f);
        //fractured soul
        else
            Invoke("SoulLostHeal", 1f);
    }

    IEnumerator _damageFlash(bool negated)
    {
        camShake.shake(12);
        SpriteRenderer s = damageFlashSprite;
        if (negated) s = handMove.playerParent.counterSprite;

        s.DOFade(1, 0.15f);
        yield return new WaitForSeconds(0.2f);
        s.DOFade(0, 0.3f);
    }

    IEnumerator _MirrorInvincibility()
    {
        envyPs.Play();
        mirrorInvincibleSound.Play();
        mirrorInvincible = true;
        yield return new WaitForSeconds(3.4f);
        mirrorInvincible = false;
        envyPs.Stop();
    }

    void RPC_MirrorInvinclibility()
    {
        StartCoroutine(_MirrorInvincibility());
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.CompareTag("Heat Source"))
        {
            inHeatSource = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Heat Source"))
        {
            StartCoroutine(_CancelHeat());
        }
    }

    IEnumerator _CancelHeat()
    {
        yield return new WaitForSeconds(0.5f);
        inHeatSource = false;
    }

}

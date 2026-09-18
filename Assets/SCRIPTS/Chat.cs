using DG.Tweening;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chat : NetworkBehaviour
{
    public static Chat Instance;
    public bool chatOpen;
    public bool freeBoat = false;
    [SerializeField] CanvasGroup chatGroup, outsideChatGroup;
    [SerializeField] TMP_InputField input;
    [SerializeField] GameObject messagePrefab;
    [SerializeField] RectTransform messageHolder;
    [SerializeField] ScrollRect scrollRect;
    List<ChatMessage> messages = new List<ChatMessage>();
    public Scrollbar scroll;
    public Color[] usernameColors;
    bool messageCooldown;
    string lastChatMessage;
    public bool disableDeathMessages;
    public Kit[] kits;
    public int setKit;
    public Image kitPopupImage;
    public CanvasGroup kitPopupGroup;
    public AudioSource kitSound;

    private void Awake()
    {
        Instance = this;
    }


    void Start()
    {
        chatGroup.alpha = 0;
        chatGroup.interactable = false;
        chatGroup.blocksRaycasts = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (pauseScreen.instance.paused || !MapDisplay.finishedLoading || menu.instance.mapMaximized || (playerHealth.instance.dead && !menu.instance.Spectating) || inventory.instance.inventoryOpen || gameManager.instance.writingSign || player.instance.inCutscene) return;

        if ((InputManager.actions["Chat"].started) && !chatOpen)
        {
            Cursor.visible = true;

            chatOpen = true;
            chatGroup.interactable = true;
            chatGroup.blocksRaycasts = true;

            outsideChatGroup.DOFade(0, 0.2f);

            chatGroup.DOFade(1, 0.3f);
            input.enabled = true;
            input.ActivateInputField();
            input.caretPosition = 0;

        }

        if (chatOpen)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                ChatSend();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                input.text = lastChatMessage;
                input.caretPosition = input.text.Length;
            }

            float contentSize = scrollRect.content.rect.height;
            float viewportSize = scrollRect.viewport.rect.height;
            scroll.size = Mathf.Clamp01(viewportSize / contentSize);

            if (InputManager.actions["Exit"].started) CloseChat();

        }

    }

    public void CloseChat()
    {
        Cursor.visible = false;

        chatOpen = false;
        chatGroup.interactable = false;
        chatGroup.blocksRaycasts = false;

        chatGroup.DOFade(0, 0.3f);
        input.enabled = false;

        outsideChatGroup.DOFade(1, 0.2f);

    }

    public void ChatSend()
    {

        if (input.text == string.Empty || messageCooldown) return;

        messageCooldown = true;
        Invoke("CooldownDone", 0.4f);

        lastChatMessage = input.text;

        //command
        if (input.text[0] == '/' && gameManager.instance.enableCommands)
        {
            List<string> args = input.text.ToLower().Replace("(", "").Replace(")", "").Split(" ").ToList();
            switch (args[0])
            {
                case "/give":

                    if (args.Count == 2)
                    {
                        args.Add("1");
                    }
                    if (args.Count == 3)
                    {
                        args.Add("0");
                    }
                    try
                    {
                        if (args.Count > 4) throw new System.Exception();

                        int amountToAdd = int.Parse(args[2]);

                        if (!gameManager.instance.itemDictionary.ContainsKey(args[1]))
                        {
                            Error("item \"" + args[1] + "\" not found");
                            break;
                        }

                        for (int i = inventory.instance.slots.Count() - 1; i >= 0; i--)
                        {

                            if (inventory.instance.slots[i].itemInSlot == null
                                || (inventory.instance.slots[i].itemInSlot == gameManager.instance.itemDictionary[args[1]] && inventory.instance.slots[i].itemCountInSlot < (gameManager.instance.itemDictionary[args[1]].maxStackSize)))
                            {
                                if (inventory.instance.slots[i].itemInSlot)
                                    amountToAdd -= (gameManager.instance.itemDictionary[args[1]].maxStackSize - inventory.instance.slots[i].itemCountInSlot);
                                else
                                    amountToAdd -= gameManager.instance.itemDictionary[args[1]].maxStackSize;
                            }
                        }


                        if (amountToAdd <= 0)
                        {
                            int durability = gameManager.instance.itemDictionary[args[1]].maxDurability;
                            if (gameManager.instance.itemDictionary[args[1]].uniqueType == item.UniqueType.gavel) durability = 0;

                            if (inventory.instance.gameObject.activeSelf)
                            {
                                item i = gameManager.instance.itemDictionary[args[1]];
                                ItemData data = new ItemData(0);
                                if (i.usesTier)
                                {
                                    int tier = Mathf.Clamp(int.Parse(args[3]), 0, 4);
                                    if (tier < 1)
                                    {
                                        tier = -1;
                                        if (i.uniqueType != item.UniqueType.none)
                                        {
                                            tier = 3;
                                        }
                                    }
                                    data = inventory.CreateItemData(i, tier);
                                }

                                inventory.instance.AddItem(gameManager.instance.itemDictionary[args[1]], int.Parse(args[2]), durability, data);
                                inventory.instance.pickupSound.Play();

                            }
                            else
                            {
                                ValeManager.instance.AddInventoryItem(gameManager.instance.itemDictionary[args[1]], int.Parse(args[2]), false);

                            }
                            CloseChat();

                            SendInChat(new string[] { "gave " + args[2] + " " + gameManager.instance.itemDictionary[args[1]].displayName + " to ", DataPersistanceManager.instance.localUsername }, new Color[] { Color.white, usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)] });
                        }
                        else
                        {
                            Error("not enough inventory space to fit " + args[2] + " " + gameManager.instance.itemDictionary[args[1]].displayName);
                        }

                    }
                    catch
                    {
                        Error("give command should be in the format: /give (item id) (count)");
                    }

                    break;
                case "/help":

                    if (args.Count == 1)
                    {
                        ReceiveMessageLocal("Here is a list of all commands: ", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/give (item id) (count) (tier) - gives you item", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/tp (player index) - teleports you to specified player", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/tp (player index) me - teleports player to you", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/toggleMarker (player index) - shows/hides player's map marker", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/resetMarkers - shows all players' map markers", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/players - lists all players in game with their IDs", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/toggledm - toggles death messages", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/time (time) - sets time [decimal number between 0 and 2]", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/resetstats (player index) - removes player's stat upgrades", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/toggletime - enables/disables the daylight cycle", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/clear - clears your inventory", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/kit (kit number) - applies duel kit of the specified number", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/kit list - lists all duel kits and their numbers", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/day (day number) - changes day number", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/boat - gives a boat station and makes next boat free", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/revive - brings you back from the vale", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        ReceiveMessageLocal("/weather (clear/rain) - changes weather", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                    }
                    else
                    {
                        Error("/help should be used by itself with no arguments");
                    }

                    break;

                case "/tp":

                    try
                    {
                        if (args.Count == 2)
                        {
                            player.instance.transform.localPosition = playerSpawner.instance.players[int.Parse(args[1])].transform.localPosition;
                            ValeManager.instance.lostSoul = false;

                            SendInChat(new string[] { "teleported ", DataPersistanceManager.instance.localUsername, " to ", DataPersistanceManager.instance.usernameMap[playerSpawner.instance.players[int.Parse(args[1])].view.InputAuthority] }, new Color[] { Color.white, usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)], Color.white, usernameColors[int.Parse(args[1])] });
                        }
                        else if (args.Count == 3)
                        {
                            if (args[2] != "me" || playerSpawner.instance.players[int.Parse(args[1])] == null || playerSpawner.instance.players[int.Parse(args[1])] == player.instance)
                                throw new System.Exception();

                            RPC_TeleportOther(int.Parse(args[1]), (Vector2)player.instance.transform.position);
                            SendInChat(new string[] { "teleported ", DataPersistanceManager.instance.usernameMap[playerSpawner.instance.players[int.Parse(args[1])].view.InputAuthority], " to ", DataPersistanceManager.instance.localUsername }, new Color[] { Color.white, usernameColors[int.Parse(args[1])], Color.white, usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)] });
                        }
                        else throw new System.Exception();

                    }
                    catch
                    {
                        Error("/tp command should be in the format: /tp (player index)");
                        Error("or /tp (player index) me");
                    }

                    break;

                case "/togglemarker":

                    try
                    {
                        if (args.Count > 2) throw new System.Exception();

                        RPC_ToggleMarker(int.Parse(args[1]), false);
                    }
                    catch
                    {
                        Error("togglemarker command should be in the format: /togglemarker (player index)");
                    }

                    break;

                case "/resetmarkers":
                    try
                    {
                        if (args.Count > 1) throw new System.Exception();

                        for (int i = 0; i < playerSpawner.instance.players.Length; i++)
                        {
                            if (playerSpawner.instance.players[i] && menu.instance.markersDisabled[i])
                            {
                                RPC_ToggleMarker(i, true);
                            }

                        }

                        SendInChat(new string[] { "showing all player map markers" }, new Color[] { Color.white });
                    }
                    catch
                    {
                        Error("/resetmarkers should be used by itself with no arguments");
                    }
                    break;

                case "/players":
                    if (args.Count == 1)
                    {
                        ReceiveMessageLocal("Here is a list of all players: ", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));

                        for (int i = 0; i < playerSpawner.instance.players.Count(); i++)
                        {
                            if (playerSpawner.instance.players[i] == null) continue;

                            string message = "";

                            string colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(usernameColors[i])}>";
                            message += $"{colorTag}{DataPersistanceManager.instance.usernameMap[playerSpawner.instance.players[i].view.InputAuthority]}</color>";

                            message += " (" + i + ")";
                            RPC_RecieveMessage(message, DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                        }
                    }
                    else
                    {
                        Error("/players should be used by itself with no arguments");
                    }
                    break;
                case "/toggledm":
                    if (args.Count == 1)
                    {
                        RPC_SetDeathMessages(!disableDeathMessages);
                        if (!disableDeathMessages)
                            SendInChat(new string[] { "enabled death messages" }, new Color[] { Color.white });
                        else
                            SendInChat(new string[] { "disabled death messages" }, new Color[] { Color.white });
                    }
                    else
                    {
                        Error("/toggledm should be used by itself with no arguments");
                    }

                    break;
                case "/time":

                    try
                    {
                        if (args.Count > 2) throw new System.Exception();

                        if (float.Parse(args[1]) < 0 || float.Parse(args[1]) > 2)
                        {
                            Error("Time must be between 0 and 2");
                            break;
                        }
                        SendInChat(new string[] { "time set to " + args[1] }, new Color[] { Color.white });
                        RPC_SetTime(float.Parse(args[1]));
                    }
                    catch
                    {
                        Error("/time command should be in the format: /time (time)");
                    }
                    break;

                case "/resetstats":

                    try
                    {
                        if (args.Count > 2) throw new System.Exception();

                        RPC_ResetStats(int.Parse(args[1]), false);
                    }
                    catch
                    {
                        Error("resetstats command should be in the format: /resetstats (player index)");
                    }

                    break;
                case "/toggletime":
                    if (args.Count == 1)
                    {
                        RPC_SetDisableTime(!TimeManager.instance.pauseTime);
                        if (!TimeManager.instance.pauseTime)
                            SendInChat(new string[] { "time is now moving again" }, new Color[] { Color.white });
                        else
                            SendInChat(new string[] { "time has been stopped" }, new Color[] { Color.white });
                    }
                    else
                    {
                        Error("/toggletime should be used by itself with no arguments");
                    }

                    break;
                case "/kit":
                    if (args.Count == 2)
                    {
                        if (args[1] == "list")
                        {
                            ReceiveMessageLocal("Here is a list of all Duel Kits with their numbers:", DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                            for (int i = 0; i < kits.Length; i++)
                            {
                                ReceiveMessageLocal((i + 1) + " - " + kits[i].name + " - " + kits[i].desc, DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
                            }

                        }
                        else
                        {
                            try
                            {
                                int kitNum = int.Parse(args[1]) - 1;

                                UseKit(kitNum);
                                SendInChat(new string[] { "Applied the " + kits[kitNum].name + " kit to ", DataPersistanceManager.instance.localUsername }, new Color[] { Color.white, usernameColors[DataPersistanceManager.LocalActorIndex] });
                            }
                            catch
                            {
                                Error("/kit should be in the format: /kit (kit number) or /kit list");
                            }

                        }

                    }
                    else
                    {
                        Error("/kit should be in the format: /kit (kit number) or /kit list");
                    }

                    break;
                case "/clear":
                    if (args.Count == 1)
                    {
                        ClearInventory();

                        SendInChat(new string[] { "cleared inventory for ", DataPersistanceManager.instance.localUsername }, new Color[] { Color.white, usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)] });
                    }
                    else
                    {
                        Error("/clear should be used by itself with no arguments");
                    }

                    break;

                case "/day":
                    try
                    {
                        if (args.Count != 2) throw new System.Exception();


                        RPC_ChangeDay(int.Parse(args[1]));
                        SendInChat(new string[] { "set day to " + args[1] }, new Color[] { Color.white });
                    }
                    catch
                    {
                        Error("/day should be in the format: /day (day number)");
                    }

                    break;

                case "/revive":
                    if (args.Count == 1)
                    {
                        ValeManager.instance.SoulRecovered();
                        playerHealth.instance.ReturnFromVale();
                        ValeManager.instance.EscapeVale();

                        SendInChat(new string[] { "revived ", DataPersistanceManager.instance.localUsername }, new Color[] { Color.white, usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)] });
                    }
                    else
                    {
                        Error("/revive should be used by itself with no arguments");
                    }

                    break;

                case "/weather":
                    try
                    {
                        if (args.Count != 2) throw new System.Exception();

                        if (!new string[] { "clear", "rain" }.Contains(args[1])) throw new System.Exception();

                        RPC_SetWeather(args[1]);
                        SendInChat(new string[] { "set weather to " + args[1] }, new Color[] { Color.white });
                    }
                    catch
                    {
                        Error("/weather should be in the format: /weather (rain/clear)");
                    }
                    break;
                case "/boat":
                    if (args.Count == 1)
                    {
                        inventory.instance.AddItem(gameManager.instance.itemDictionary["boat_station"], 1, 1, new ItemData());
                        inventory.instance.pickupSound.Play();
                        freeBoat = true;

                        SendInChat(new string[] { "gave a free boat to ", DataPersistanceManager.instance.localUsername }, new Color[] { Color.white, usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)] });
                    }
                    else
                    {
                        Error("/boat should be used by itself with no arguments");
                    }
                    break;
                default:
                    Error("command \'" + args[0] + "\' not found");
                    break;

            }


        }
        //regular message
        else
        {
            string colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(usernameColors[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)])}>";
            string text = $"{colorTag}{DataPersistanceManager.instance.localUsername + ":"}</color>" + " " + input.text;
            RPC_RecieveMessage(text, DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
        }


        input.text = "";

        input.ActivateInputField();
    }

    void ClearInventory()
    {
        List<inventorySlot> allSlots = inventory.instance.slots.ToList();

        allSlots.Add(inventory.instance.headSlot);
        allSlots.Add(inventory.instance.chestplateSlot);
        allSlots.Add(inventory.instance.legsSlot);
        allSlots.Add(inventory.instance.charmSlot);

        foreach (inventorySlot slot in allSlots)
        {
            inventory.instance.removeItem(slot);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_ChangeDay(int dayNum)
    {
        StartCoroutine(TimeManager.instance._ChangeDay(dayNum, 0));
    }

    public void SendInChat(string[] texts, Color[] colors)
    {
        string message = "";
        for (int i = 0; i < texts.Length; i++)
        {
            string colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(colors[i])}>";
            message += $"{colorTag}{texts[i]}</color>";
        }
        RPC_RecieveMessage(message, DataPersistanceManager.LocalActorIndex);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetDeathMessages(bool disable)
    {
        disableDeathMessages = disable;
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetTime(float time)
    {
        TimeManager.instance.time = time;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetDisableTime(bool disable)
    {
        TimeManager.instance.pauseTime = disable;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ToggleMarker(int playerId, bool disableMessage)
    {
        menu.instance.markersDisabled[playerId] = !menu.instance.markersDisabled[playerId];
        if (playerId == DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer) && !disableMessage)
        {
            if (!menu.instance.markersDisabled[playerId])
                SendInChat(new string[] { "enabled map marker for ", DataPersistanceManager.instance.usernameMap[playerSpawner.instance.players[playerId].view.InputAuthority] }, new Color[] { Color.white, usernameColors[playerId] });
            else
                SendInChat(new string[] { "disabled map marker for ", DataPersistanceManager.instance.usernameMap[playerSpawner.instance.players[playerId].view.InputAuthority] }, new Color[] { Color.white, usernameColors[playerId] });
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_SetWeather(string weather)
    {
        if (weather == "clear")
        {
            WeatherManager.Instance.rainEndTime = 0.1f;
        }
        else if (weather == "rain")
        {
            WeatherManager.Instance.rainStartTime = 0.1f;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_ResetStats(int playerId, bool disableMessage)
    {

        if (playerId == DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer))
        {
            for (int i = 0; i < inventory.instance.statAmounts.Length; i++)
            {
                inventory.instance.statAmounts[i] = 0;
                inventory.instance.statBarFills[i].DOFillAmount(0, 0.45f);
            }
            inventory.instance.maxStat = false;
            if (!disableMessage)
            {
                SendInChat(new string[] { "reset stat upgrades for ", DataPersistanceManager.instance.usernameMap[playerSpawner.instance.players[playerId].view.InputAuthority] }, new Color[] { Color.white, usernameColors[playerId] });
            }
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_TeleportOther(int playerId, Vector2 targetPos)
    {
        if (playerId != DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)) return;

        player.instance.transform.position = targetPos;
        ValeManager.instance.lostSoul = false;
    }

    public void Error(string message)
    {
        string colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.9f, 0, 0))}>";
        string text = $"{colorTag}{message}</color>";
        ReceiveMessageLocal(text, DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer));
    }

    void CooldownDone()
    {
        messageCooldown = false;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_RecieveMessage(string text, int actorNum)
    {
        ReceiveMessageLocal(text, actorNum);
    }

    public void ReceiveMessageLocal(string text, int actorNum)
    {
        bool atBottom = scroll.value <= 0.0001f || !scroll.gameObject.activeInHierarchy;

        GameObject newMessage = Instantiate(messagePrefab, messageHolder);
        ChatMessage chatMessage = newMessage.GetComponent<ChatMessage>();

        chatMessage.messageText.text = text;

        messages.Add(chatMessage);

        messageHolder.sizeDelta = new Vector2(messageHolder.sizeDelta.x, (messages.Count * 26.5f) + 5);
        if (atBottom || actorNum == DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer))
        {
            messageHolder.transform.localPosition = new Vector2(0, 26.5f * (messages.Count + 0.0945f));

            scroll.value = 0;

        }

        GameObject newOutsideMessage = Instantiate(messagePrefab, outsideChatGroup.transform);
        ChatMessage chatMessage2 = newOutsideMessage.GetComponent<ChatMessage>();

        chatMessage2.messageText.text = text;

        StartCoroutine(_DestroyMessage(newOutsideMessage));
    }


    IEnumerator _DestroyMessage(GameObject message)
    {
        yield return new WaitForSeconds(6);
        message.GetComponent<CanvasGroup>().DOFade(0, 2);
        yield return new WaitForSeconds(2.1f);
        Destroy(message);
    }

    void UseKit(int num)
    {
        ClearInventory();

        List<inventorySlot> allSlots = kitSlots();

        for (int i = 0; i < allSlots.Count(); i++)
        {
            if (kits[num].items[i])
            {
                int durability = kits[num].items[i].maxDurability;
                if (kits[num].items[i].uniqueType == item.UniqueType.gavel) durability = 0;

                inventory.instance.addItemInSlot(kits[num].items[i], kits[num].counts[i], durability, new ItemData(3),
                    allSlots[i], allSlots[i].transform.position);
            }
        }

        for (int i = 0; i < inventory.instance.statAmounts.Count(); i++)
        {
            inventory.instance.statAmounts[i] = kits[num].stats[i];
            inventory.instance.statBarFills[i].fillAmount = inventory.instance.statAmounts[i] / 10f;
        }

        playerHealth.instance.maxHealth = Mathf.RoundToInt(playerHealth.instance.startMaxHealth * (1 + (inventory.instance.GetStatAmount(3) / 10f * inventory.instance.maxHealthMult)));

        playerHealth.instance.currentHealth = playerHealth.instance.maxHealth;
        inventory.instance.specialCooldownTime = 0;
        HandMove.instance.FinishCatalyst();

        /*
        switch (num)
        {
            case 8:
                StartCoroutine(_KitPopup());
                break;

        }
        */

        CloseChat();
    }

    public IEnumerator _KitPopup()
    {
        //kitSound.Play();

        kitPopupGroup.DOFade(1, 0.5f);
        yield return new WaitForSeconds(0.5f);
        kitPopupGroup.DOFade(0, 1f);
    }

    public void EditorSetKit()
    {
        List<inventorySlot> allSlots = kitSlots();

        for (int i = 0; i < allSlots.Count(); i++)
        {
            kits[setKit].items[i] = allSlots[i].itemInSlot;
            kits[setKit].counts[i] = allSlots[i].itemCountInSlot;
        }

        kits[setKit].stats = new int[5];

        for (int i = 0; i < inventory.instance.statAmounts.Count(); i++)
        {
            kits[setKit].stats[i] = inventory.instance.statAmounts[i];
        }


    }

    List<inventorySlot> kitSlots()
    {
        List<inventorySlot> list = new List<inventorySlot>();

        for (int i = 0; i < 9; i++)
        {
            list.Add(inventory.instance.slots[i]);
        }
        list.Add(inventory.instance.headSlot);
        list.Add(inventory.instance.chestplateSlot);
        list.Add(inventory.instance.legsSlot);
        list.Add(inventory.instance.charmSlot);

        return list;
    }

}
[System.Serializable]
public struct Kit
{
    public string name;
    public string desc;
    public item[] items;
    public int[] counts;
    public int[] stats;

}


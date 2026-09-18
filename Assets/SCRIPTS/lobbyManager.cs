using DG.Tweening;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class lobbyManager : MonoBehaviour
{
    public static lobbyManager instance;

    public const float fadeTime = 0.6f;
    public const int playerCount = 5;

    public GameObject lobbyNetworkManagerPref;

    public TMP_InputField joinInputField;
    public RectTransform lobbyPanel;
    public RectTransform roomPanel;
    public TextMeshProUGUI roomName;
    public GameObject roomItemPrefab;
    List<roomItem> roomItemsList = new List<roomItem>();
    public RectTransform contentObject;

    public float timeBetweenUpdates = 1f;
    float nextUpdateTime;

    public List<playerItem> playerItemsList = new List<playerItem>();
    public GameObject playerItemPrefab;
    public Transform playerItemParent;
    playerItem localPlayerItem;

    public Button playButton;
    public TMP_InputField seedInput;

    SerializableDictionary<string, string> roomProperties = new SerializableDictionary<string, string>();

    public CanvasGroup connectingScreen;
    public CanvasGroup fadeGroup;

    public Button backButton, joinButton, settingsButton, saveRenameButton, createButton;
    bool fullscreen;

    [SerializeField] TMP_Dropdown savesDropdown, commandsDropdown, difficultyDropdown;
    [SerializeField] TextMeshProUGUI createButtonText;
    public GameObject confirmDelete;

    const int saveSlotAmount = 5;
    [SerializeField] TextMeshProUGUI confirmDeleteText;

    public static int selectedSkin;

    public SerializableDictionary<string, GameData> profileData = new SerializableDictionary<string, GameData>();
    public TMP_InputField saveRenameInput;
    public GameObject saveRenameScreen;

    public string[] fileNames = { "File 1", "File 2", "File 3", "File 4", "File 5" };

    public Color[] usernameColors;
    private bool finishedJoiningRoom;

    [SerializeField] AudioSource clickSound;
    public ParticleSystem dementiaParticles;
    Color defaultDifficultyColor;
    [SerializeField] Color demetiaColor, hardColor;
    [SerializeField] Toggle duelToggle;

    bool loadingMenu;

    public GameObject runnerPrefab;

    string currentRoomCode;
    [SerializeField] Animation copyAnim;

    private List<SessionInfo> _cachedSessions = new List<SessionInfo>();
    private bool _sessionListReady = false;
    [SerializeField] TextMeshProUGUI joinButtonText;

    string localFileName;

    bool lobbySuccessfullyJoined;
    private int difficultyVal;

    private void Awake()
    {
        instance = this;
    }


    private void Start()
    {
        defaultDifficultyColor = difficultyDropdown.image.color;

        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();

        fullscreen = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;

        fadeGroup.alpha = 1;

        difficultyVal = 0;

        roomPanel.gameObject.SetActive(false);
        lobbyPanel.gameObject.SetActive(true);
        connectingScreen.alpha = 0;
        connectingScreen.interactable = false;
        connectingScreen.blocksRaycasts = false;


        Invoke("FadeOut", 0.15f);
    }

    public void CopyCode()
    {
        clickSound.Play();
        GUIUtility.systemCopyBuffer = currentRoomCode;
        copyAnim.Play();
    }


    public void UpdateSaveFiles()
    {
        //load save files
        for (int i = 0; i < saveSlotAmount; i++)
        {
            GameData data = null;
            profileData.TryGetValue((i + 1).ToString(), out data);

            string fileName = fileNames[i];

            if (SingletonRunner.runner.IsSharedModeMasterClient && data != null) fileName = data.fileName;

            if (data != null)
            {
                //remove time of day from old save files
                if (data.lastPlayed.Split(" ").Length > 1)
                    data.lastPlayed = data.lastPlayed.Split(" ")[0];

                string colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.66f, 1))}>";
                if (data.dead && data.difficulty == 2)
                    colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.9f, 0, 0))}>";

                string fileData = $"{colorTag}{"Day " + (data.dayNum + 1) + " (" + data.lastPlayed + ")"}</color>";

                if (data.completed) fileData += " ⭐";
                if (data.dead && data.difficulty == 2)
                {
                    fileData += " 💀";
                }
                savesDropdown.options[i] = new TMP_Dropdown.OptionData(fileName + ": " + fileData);
            }
            else
            {
                string colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.6f, 0.6f, 0.6f))}>";
                string fileData = $"{colorTag}{"(No Data)"}</color>";

                savesDropdown.options[i] = new TMP_Dropdown.OptionData(fileName + ": " + fileData);
            }

        }
    }

    public void SelectSaveSlot(int slot)
    {
        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            GameData data;
            profileData.TryGetValue((slot + 1).ToString(), out data);

            seedInput.interactable = data == null;
            if (data != null)
            {
                if (data.seedInput != null && data.seedInput != "")
                    seedInput.text = data.seedInput.ToString();
                else
                    seedInput.text = "[SEED NOT FOUND]";

                //view.RPC("RPC_SetSaveSlot", RpcTarget.AllBuffered, slot, data.saveFileID);
                LobbyManagerRpcs.Instance.RPC_SetSaveSlot(slot, data.saveFileID);

                commandsDropdown.SetValueWithoutNotify(data.enableCommands);
                difficultyDropdown.SetValueWithoutNotify(data.difficulty);
                duelToggle.isOn = data.duelArena;

                UpdateCommands();
                UpdateDifficulty();
                UpdateDuelMode();

                difficultyDropdown.interactable = false;
                duelToggle.interactable = false;
            }
            else
            {
                seedInput.text = "";
                //view.RPC("RPC_SetSaveSlot", RpcTarget.AllBuffered, slot, "");
                LobbyManagerRpcs.Instance.RPC_SetSaveSlot(slot, "");

                difficultyDropdown.SetValueWithoutNotify(0);
                duelToggle.isOn = false;

                UpdateDifficulty();
                UpdateDuelMode();

                difficultyDropdown.interactable = true;
                duelToggle.interactable = true;
            }

            difficultyDropdown.interactable = data == null;

        }
    }

    public void OnRpcSetSaveSlot(int slot, string saveFileID)
    {
        if (!SingletonRunner.runner.IsSharedModeMasterClient) savesDropdown.value = slot;

        DataPersistanceManager.instance.selectedProfileId = (slot + 1).ToString();

        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();

        GameData data = null;
        profileData.TryGetValue((slot + 1).ToString(), out data);

        DataPersistanceManager.instance.loadingSave = data != null;

        UpdateSaveFiles();
        savesDropdown.RefreshShownValue();

        //delete save file from slot with user if id doesnt match
        if (!SingletonRunner.runner.IsSharedModeMasterClient && data != null && data.saveFileID != saveFileID)
        {
            DataPersistanceManager.instance.DeleteProfileData((slot + 1).ToString());
            profileData = DataPersistanceManager.instance.GetAllProfilesGameData();

            UpdateSaveFiles();
            savesDropdown.RefreshShownValue();
        }
    }


    public void UpdateCommands()
    {
        LobbyManagerRpcs.Instance.RPC_SetCommands(commandsDropdown.value);
    }

    public void OnRpcSetCommands(int option)
    {
        if (!SingletonRunner.runner.IsSharedModeMasterClient) commandsDropdown.SetValueWithoutNotify(option);

        commandsDropdown.RefreshShownValue();
    }

    public void UpdateDifficulty()
    {
        LobbyManagerRpcs.Instance.RPC_SetDifficulty(difficultyDropdown.value);
    }

    public void OnRpcSetDifficulty(int option)
    {
        if (!SingletonRunner.runner.IsSharedModeMasterClient) difficultyDropdown.SetValueWithoutNotify(option);

        difficultyDropdown.RefreshShownValue();
    }

    public void UpdateDuelMode()
    {
        if (duelToggle.isOn)
        {
            commandsDropdown.value = 2;
            commandsDropdown.interactable = false;
            difficultyDropdown.value = 0;
            difficultyDropdown.interactable = false;
        }
        else if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            commandsDropdown.interactable = true;
            GameData data;
            profileData.TryGetValue((savesDropdown.value + 1).ToString(), out data);
            difficultyDropdown.interactable = data == null;
        }
        LobbyManagerRpcs.Instance.RPC_SetDuelMode(duelToggle.isOn);
    }

    public void OnRpcSetDuelMode(bool option)
    {
        if (!SingletonRunner.runner.IsSharedModeMasterClient) duelToggle.SetIsOnWithoutNotify(option);
    }

    public void OnRpcDeleteSaveFile()
    {
        DataPersistanceManager.instance.DeleteProfileData(DataPersistanceManager.instance.selectedProfileId.ToString());
        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();

        UpdateFileNames();
        UpdateSaveFiles();
        savesDropdown.RefreshShownValue();
    }


    public void StartClearData(bool clearing)
    {
        if (!clearing)
        {
            confirmDelete.SetActive(false);
            return;
        }

        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();

        GameData data = null;
        profileData.TryGetValue((DataPersistanceManager.instance.selectedProfileId).ToString(), out data);

        if (data != null)
            confirmDelete.SetActive(clearing);

        if (SingletonRunner.runner.IsSharedModeMasterClient)
            confirmDeleteText.text = "Confirm deletion of save file: " + $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.66f, 1))}>" + data.fileName + "</color>" + "?\r\n(This can't be undone)";
        else
            confirmDeleteText.text = "Confirm deletion of your local data from save file: " + $"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0, 0.66f, 1))}>" + fileNames[int.Parse(DataPersistanceManager.instance.selectedProfileId) - 1] + "</color>" + "?\r\n(This can't be undone)";
    }

    public void StartRenameSave(bool open)
    {
        if (!open)
        {
            saveRenameScreen.SetActive(false);
            return;
        }

        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();

        GameData data;
        profileData.TryGetValue((DataPersistanceManager.instance.selectedProfileId).ToString(), out data);

        if (data == null)
        {
            return;
        }

        saveRenameScreen.SetActive(open);
        saveRenameInput.text = data.fileName;

        saveRenameInput.Select();

    }

    public void ConfirmRenameSave()
    {
        if (saveRenameInput.text.Length <= 0) return;

        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();

        GameData data = null;
        profileData.TryGetValue((DataPersistanceManager.instance.selectedProfileId).ToString(), out data);

        //name room with empty data
        if (data == null)
        {
            localFileName = saveRenameInput.text;
            onClickPlayGame();
            return;
        }

        data.fileName = saveRenameInput.text;

        DataPersistanceManager.instance.DirectSave(data);

        StartRenameSave(false);

        UpdateSaveFiles();
        savesDropdown.RefreshShownValue();

        UpdateFileNames();
    }


    public void ConfirmClearData()
    {

        DataPersistanceManager.instance.DeleteProfileData(DataPersistanceManager.instance.selectedProfileId);
        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();
        confirmDelete.SetActive(false);
        DataPersistanceManager.instance.loadingSave = false;

        UpdateFileNames();

        UpdateSaveFiles();
        savesDropdown.RefreshShownValue();

        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            //view.RPC("RPC_DeleteSaveFile", RpcTarget.Others);
            LobbyManagerRpcs.Instance.RPC_DeleteSaveFile();

            seedInput.interactable = true;
            seedInput.text = "";

            difficultyDropdown.interactable = true;
        }

        difficultyDropdown.SetValueWithoutNotify(0);
        UpdateDifficulty();

        duelToggle.interactable = SingletonRunner.runner.IsSharedModeMasterClient;
        duelToggle.isOn = false;
        UpdateDuelMode();
    }

    public void OnEndJoinFieldEdit()
    {
        if (Input.GetKeyDown(KeyCode.Return) && joinButton.interactable)
        {
            OnClickJoin();
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed) Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            else
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(ConnectToServer.windowedX, ConnectToServer.windowedY, FullScreenMode.Windowed);
            }
        }

        if (SingletonRunner.runner && SingletonRunner.runner.IsInSession && LobbyManagerRpcs.Instance && LobbyManagerRpcs.view.IsValid)
        {
            if (!SingletonRunner.runner.IsSharedModeMasterClient)
                seedInput.text = LobbyManagerRpcs.Instance.networkedSeedInput;
            else
                LobbyManagerRpcs.Instance.networkedSeedInput = seedInput.text;
        }

        if (saveRenameScreen.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                ConfirmRenameSave();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StartRenameSave(false);
            }
        }

        if (difficultyDropdown.value == 2)
        {
            if (difficultyVal != 2)
            {
                difficultyVal = 2;
                difficultyDropdown.image.DOColor(demetiaColor, 0.5f);
            }
            
            if (!dementiaParticles.isPlaying)
            {
                dementiaParticles.Play();
            }

        }
        else
        {
            if(dementiaParticles.isPlaying) dementiaParticles.Stop();

            if (difficultyDropdown.value == 1 && difficultyVal != 1)
            {
                difficultyVal = 1;
                difficultyDropdown.image.DOColor(hardColor, 0.5f);
            }
            else if (difficultyDropdown.value == 0 && difficultyVal != 0)
            {
                difficultyVal = 0;
                difficultyDropdown.image.DOColor(defaultDifficultyColor, 0.5f);
            }
        }

    }

    void StartNameNewRoom()
    {
        saveRenameScreen.SetActive(true);
        saveRenameInput.text = "File " + DataPersistanceManager.instance.selectedProfileId;

        saveRenameInput.Select();

    }


    public void onClickPlayGame()
    {
        if (!SingletonRunner.runner.IsSharedModeMasterClient) return;

        //name popup when running new game
        GameData data;
        profileData.TryGetValue(DataPersistanceManager.instance.selectedProfileId, out data);

        if (data == null && localFileName == null)
        {
            StartNameNewRoom();
            //set eligible once save file first starts
            PlayerPrefs.SetInt("goldenEligible" + DataPersistanceManager.instance.selectedProfileId, 1);
            return;
        }


        string seed = null;

        if (seedInput.text.Length >= 2)
        {
            seed = seedInput.text;
        }


        string saveId = Guid.NewGuid().ToString();
        if (data != null && data.saveFileID != null && data.saveFileID != "")
            saveId = data.saveFileID;

        roomProperties = new()
        {
        { "seed", seed },
        { "difficulty", difficultyDropdown.value.ToString() },
        { "commands", commandsDropdown.value.ToString() },
        { "fileName",  localFileName },
        { "saveFileId" , saveId },
        { "duelArena" , duelToggle.isOn ? "true" : "false" }
        };

        if (commandsDropdown.value != 0
           || difficultyDropdown.value != 2
           || duelToggle.isOn == true
           || SingletonRunner.runner.ActivePlayers.Count() > 1
           )
            PlayerPrefs.SetInt("goldenEligible" + DataPersistanceManager.instance.selectedProfileId, 0);

        LobbyManagerRpcs.Instance.RPC_SendProperties(treeRPCs.DictionaryToString<string, string>(roomProperties));

        LobbyManagerRpcs.Instance.RPC_LoadLevel();

    }

    public void OnRpcLoadLevel()
    {
        DataPersistanceManager.notInEditor = true;

        SingletonRunner.runner.SessionInfo.IsOpen = false;

        GameData data;
        profileData.TryGetValue((savesDropdown.value + 1).ToString(), out data);

        DataPersistanceManager.instance.loadingSave = data != null;

        menuMusic.instance.MusicFadeOut();
        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            PlayerPrefs.SetString("lastSaveFile", DataPersistanceManager.instance.selectedProfileId);
        }
        StartCoroutine(_LoadLevel());


    }

    IEnumerator _LoadLevel()
    {
        Cursor.visible = false;

        connectingScreen.DOFade(1, 0.4f);
        connectingScreen.blocksRaycasts = true;
        connectingScreen.interactable = true;
        yield return new WaitForSeconds(0.4f);

        if (SingletonRunner.runner.IsSharedModeMasterClient)
            SingletonRunner.runner.LoadScene(SceneRef.FromIndex(2), new LoadSceneParameters(), true);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _cachedSessions = sessionList;
        _sessionListReady = true;
    }

    public async void OnClickJoin()
    {
        if (joinInputField.text.Length == 5 && joinButton.interactable)
        {
            SetLobbyActive(false);
            Coroutine co = StartCoroutine(_JoinTextAnim());

            Instantiate(runnerPrefab);

            /*
            _sessionListReady = false;
            await SingletonRunner.runner.JoinSessionLobby(SessionLobby.Shared, "main");

            // Wait until session list is updated
            while (!_sessionListReady)
                await Task.Yield();
            
            while (!SingletonRunner.runner || !!SingletonRunner.runner.IsConnectedToServer)
                await Task.Yield();
            */

            string targetName = joinInputField.text.ToUpper();

            print(targetName);

            SetLobbyActive(false);

            var args = new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = targetName,
                PlayerCount = playerCount,
                IsOpen = true,
                EnableClientSessionCreation = false
            };

            await Task.Delay(500);

            StartGameResult result = await SingletonRunner.runner.StartGame(args);
            if (result.Ok)
            {
                Debug.Log("Joined session: " + targetName);
            }
            else
            {
                Debug.LogError($"StartGame failed: {result.ShutdownReason}");
            }

            SetLobbyActive(true);

            await Task.Delay(400);

            StopCoroutine(co);
        }
    }

    IEnumerator _JoinTextAnim()
    {
        while (true)
        {
            joinButtonText.text = "";

            yield return new WaitForSeconds(0.25f);

            for (int i = 0; i < 3; i++)
            {
                joinButtonText.text += ".";

                yield return new WaitForSeconds(0.25f);
            }
        }

    }

    public async void OnClickCreate()
    {
        SetLobbyActive(false);

        Instantiate(runnerPrefab);
        localFileName = null;

        Coroutine co = StartCoroutine(_CreateTextAnim());

        /*
        _sessionListReady = false;
        await SingletonRunner.runner.JoinSessionLobby(SessionLobby.Shared, "main");

        while (!_sessionListReady)
            await Task.Yield();
        */

        //PhotonNetwork.JoinOrCreateRoom(joinInputField.text, new RoomOptions() { MaxPlayers = 5, BroadcastPropsChangeToAll = true, IsOpen = true }, TypedLobby.Default);

        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = GenerateLobbyCode(),
            PlayerCount = playerCount,
            IsOpen = true,
            EnableClientSessionCreation = true,

        };


        StartGameResult result = await SingletonRunner.runner.StartGame(args);
        if (result.Ok)
        {
            Debug.Log($"[StartGame] SessionName: {SingletonRunner.runner.SessionInfo.Name}");
        }
        else
        {
            Debug.LogError($"StartGame failed: {result.ShutdownReason}");
            SetLobbyActive(true);
        }

        await Task.Delay(400);

        StopCoroutine(co);

    }

    IEnumerator _CreateTextAnim()
    {
        while (true)
        {
            createButtonText.text = "Creating";

            yield return new WaitForSeconds(0.25f);

            for (int i = 0; i < 3; i++)
            {
                createButtonText.text += ".";

                yield return new WaitForSeconds(0.25f);
            }
        }

    }

    string GenerateLobbyCode(int length = 5)
    {
        const string chars = "BCDFGHJKLMNPQRSTUWXYZ123456789";
        char[] stringChars = new char[length];

        for (int i = 0; i < length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, chars.Length);
            stringChars[i] = chars[randomIndex];
        }

        return new string(stringChars);
    }

    void UpdateFileNames()
    {
        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();

        fileNames = new string[saveSlotAmount];

        for (int i = 0; i < fileNames.Length; i++)
        {
            if (profileData.ContainsKey((i + 1).ToString()))
            {
                fileNames[i] = profileData[(i + 1).ToString()].fileName;
            }
            else
            {
                fileNames[i] = "File " + (i + 1);
            }
        }
        LobbyManagerRpcs.Instance.RPC_SaveFileNames(fileNames);

    }

    public void OnRpcSaveFileNames(string[] names)
    {
        fileNames = names;

        UpdateSaveFiles();
        savesDropdown.RefreshShownValue();

    }

    public void OnRpcSetGameOwner(string userID)
    {
        lobbySuccessfullyJoined = true;

        DataPersistanceManager.gameOwnerID = userID;
        saveRenameButton.interactable = SingletonRunner.runner.IsSharedModeMasterClient;
        savesDropdown.value = int.Parse(DataPersistanceManager.instance.selectedProfileId) - 1;
        savesDropdown.interactable = SingletonRunner.runner.IsSharedModeMasterClient;

        profileData = DataPersistanceManager.instance.GetAllProfilesGameData();
        GameData data;
        profileData.TryGetValue((savesDropdown.value + 1).ToString(), out data);

        DataPersistanceManager.instance.loadingSave = data != null;

        commandsDropdown.interactable = SingletonRunner.runner.IsSharedModeMasterClient;

        if (duelToggle.isOn)
        {
            commandsDropdown.value = 2;
            commandsDropdown.interactable = false;
        }

        difficultyDropdown.interactable = SingletonRunner.runner.IsSharedModeMasterClient && data == null;
        duelToggle.interactable = SingletonRunner.runner.IsSharedModeMasterClient && data == null;

        roomName.text = "Lobby Code: " + SingletonRunner.runner.SessionInfo.Name;
        currentRoomCode = SingletonRunner.runner.SessionInfo.Name;


        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            UpdateFileNames();

            seedInput.interactable = data == null;
            if (data != null)
            {
                if (data.seedInput != null && data.seedInput != "")
                    seedInput.text = data.seedInput.ToString();
                else
                    seedInput.text = "[SEED NOT FOUND]";
                LobbyManagerRpcs.Instance.RPC_SetSaveSlot(savesDropdown.value, data.saveFileID);

                commandsDropdown.SetValueWithoutNotify(data.enableCommands);
                UpdateCommands();

                difficultyDropdown.SetValueWithoutNotify(data.difficulty);
                UpdateDifficulty();

                duelToggle.isOn = data.duelArena;
                UpdateDuelMode();
            }
            else
            {
                LobbyManagerRpcs.Instance.RPC_SetSaveSlot(savesDropdown.value, "");
            }

        }

        StartClearData(false);

    }


    public async void OnPlayerJoined(PlayerRef player)
    {
        //local
        if (player == SingletonRunner.runner.LocalPlayer)
        {
            Instantiate(lobbyNetworkManagerPref);

            SingletonRunner.runner.RegisterSceneObjects(SingletonRunner.runner.GetSceneRef("Lobby"), FindObjectsByType<NetworkObject>(FindObjectsSortMode.None));

            finishedJoiningRoom = false;

            if (SingletonRunner.runner.IsSharedModeMasterClient)
            {
                LobbyManagerRpcs.Instance.RPC_SetGameOwner(DataPersistanceManager.userId);
            }
            StartCoroutine(_RoomPanelAnimation());

            if (PlayerPrefs.HasKey("selectedSkin"))
            {
                DataPersistanceManager.instance.localSkin = PlayerPrefs.GetInt("selectedSkin");
            }

            await Task.Yield();

            while (!LobbyManagerRpcs.view || !LobbyManagerRpcs.view.IsValid)
            {
                await Task.Yield();
            }

            //send username to new clients
            LobbyManagerRpcs.Instance.RPC_RequestPlayerInfo();
        }


    }

    public void OnRPCRequestPlayerInfo()
    {
        LobbyManagerRpcs.Instance.RPC_SetUsername(DataPersistanceManager.instance.localUsername, SingletonRunner.runner.LocalPlayer);
        LobbyManagerRpcs.Instance.RPC_SetSkin(DataPersistanceManager.instance.localSkin, SingletonRunner.runner.LocalPlayer);
        UpdateDuelMode();
        UpdateDifficulty();
        UpdateCommands();

        finishedJoiningRoom = true;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        //UpdateActorIndices();
        UpdatePlayerList();

        if (DataPersistanceManager.instance.usernameMap.ContainsKey(player)) DataPersistanceManager.instance.usernameMap.Remove(player);

        if (player == SingletonRunner.runner.LocalPlayer)
        {
            DataPersistanceManager.instance.usernameMap.Clear();
            DataPersistanceManager.instance.actorIndexMap.Clear();
        }

    }

    IEnumerator _RoomPanelAnimation()
    {
        SetLobbyActive(false);

        yield return new WaitForSeconds(0.4f);

        //LobbyManagerRpcs.Instance.RPC_UpdatePlayerList();
        //LobbyManagerRpcs.Instance.RPC_SetUsername(DataPersistanceManager.instance.localUsername, SingletonRunner.runner.LocalPlayer);
        //LobbyManagerRpcs.Instance.RPC_SetSkin(DataPersistanceManager.instance.localSkin, SingletonRunner.runner.LocalPlayer);

        while (!lobbySuccessfullyJoined) yield return null;

        roomPanel.gameObject.SetActive(true);
        roomPanel.anchoredPosition = new Vector2(0, 650);
        roomPanel.DOAnchorPosY(-20, 0.4f);

        localPlayerItem.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.4f);

        localPlayerItem.gameObject.SetActive(true);
        localPlayerItem.anim.Play();

        lobbyPanel.gameObject.SetActive(false);
        roomPanel.DOAnchorPosY(0, 0.3f);

    }

    public void OnClickBack()
    {
        SetLobbyActive(false);
        fadeGroup.DOFade(1, fadeTime);
        StartCoroutine(_LoadMenu());
    }


    public void OnRunnerShutdown()
    {
        //only for leaving room
        if (loadingMenu) return;

        //balright
        LeaveRoom.instance.ReloadLobby();

    }



    IEnumerator _LoadMenu()
    {
        while (fadeGroup && fadeGroup.alpha < 1) yield return null;
        SceneManager.LoadScene("menu");
    }

    public void OnClickLeaveRoom()
    {
        if (finishedJoiningRoom)
        {
            SingletonRunner.runner.Shutdown();

            fadeGroup.blocksRaycasts = true;

        }
    }

    public void OnRpcSetUsername(string username, PlayerRef player)
    {
        if (DataPersistanceManager.instance.usernameMap.ContainsKey(player))
        {
            DataPersistanceManager.instance.usernameMap[player] = username;
        }
        else
        {
            DataPersistanceManager.instance.usernameMap.Add(player, username);
        }
        UpdatePlayerList();
    }

    public void OnRpcSetSkin(int skin, PlayerRef player)
    {
        if (!DataPersistanceManager.instance.skinMap.TryAdd(player, skin))
        {
            DataPersistanceManager.instance.skinMap[player] = skin;
        }

    }

    public void OnRpcUpdatePlayerList()
    {
        finishedJoiningRoom = true;
        UpdatePlayerList();
    }

    void UpdatePlayerList()
    {
        if (!SingletonRunner.runner.IsInSession)
            return;

        List<PlayerRef> players = GetPlayersOrderedByActorIndex();

        // Track players we already had
        var previousPlayers = playerItemsList.Select(pi => pi.player).ToList();

        // Remove playerItems for players who left
        for (int i = playerItemsList.Count - 1; i >= 0; i--)
        {
            if (!players.Contains(playerItemsList[i].player))
            {
                Destroy(playerItemsList[i].gameObject);
                playerItemsList.RemoveAt(i);
            }
        }

        DataPersistanceManager.instance.actorIndexMap.Clear();

        // Add or update items for current players
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var existing = playerItemsList.FirstOrDefault(pi => pi.player == player);

            if (existing == null)
            {
                var newPlayerItem = Instantiate(playerItemPrefab, playerItemParent).GetComponent<playerItem>();

                string username = "";
                DataPersistanceManager.instance.usernameMap.TryGetValue(player, out username);

                if (player == SingletonRunner.runner.LocalPlayer)
                {
                    localPlayerItem = newPlayerItem;
                    username = DataPersistanceManager.instance.localUsername;
                }

                newPlayerItem.SetPlayerInfo(player, i, username);
                newPlayerItem.anim.Play();
                playerItemsList.Add(newPlayerItem);
            }
            else
            {
                // Existing player - just update info

                string username = "";
                DataPersistanceManager.instance.usernameMap.TryGetValue(player, out username);

                DataPersistanceManager.instance.actorIndexMap[player] = i;
                existing.SetPlayerInfo(player, i, username);
            }
        }

        // Host / client UI state
        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            playButton.interactable = true;
            seedInput.interactable = true;
            LobbyManagerRpcs.Instance.RPC_SetGameOwner(DataPersistanceManager.userId);
        }
        else
        {
            playButton.interactable = false;
            seedInput.interactable = false;
        }

        UpdateSaveFiles();
        UpdateCommands();
    }

    List<PlayerRef> GetPlayersOrderedByActorIndex()
    {
        return SingletonRunner.runner.ActivePlayers
            .OrderBy(player => player.PlayerId)
            .ToList();
    }

    void SetLobbyActive(bool active)
    {
        //backButton.interactable = active;
        joinButton.interactable = active;
        settingsButton.interactable = active;
        createButton.interactable = active;

        foreach (roomItem room in roomItemsList)
        {
            room.button.interactable = active;
        }

    }


    void FadeOut()
    {
        if (fadeGroup.alpha == 1) fadeGroup.DOFade(0, 1);
    }


}

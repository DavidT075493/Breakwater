using DG.Tweening;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataPersistanceManager : MonoBehaviour
{
    public static DataPersistanceManager instance { get; private set; }

    public GameData gameData;
    public CanvasGroup saveIcon;

    List<IDataPersistance> dataPersistanceObjects;

    [SerializeField]
    string fileName;
    public FileDataHandler dataHandler;

    public string selectedProfileId = "1";

    public bool loadingSave = false;

    Coroutine saveIconCo;

    public static string gameOwnerID;
    public static float timeSinceSave = 0;
    public static string timeSinceSaveFormatted;

    public static string userId;

    //Discord.Discord discord;

    public SerializableDictionary<PlayerRef, int> actorIndexMap = new SerializableDictionary<PlayerRef, int>();

    public string localUsername = "Player";
    public int localSkin;

    public SerializableDictionary<PlayerRef, string> usernameMap = new SerializableDictionary<PlayerRef, string>();
    public SerializableDictionary<PlayerRef, int> skinMap = new SerializableDictionary<PlayerRef, int>();

    public static SerializableDictionary<string, string> roomProperties = new SerializableDictionary<string, string>();
    public static bool notInEditor = false;

    //public SerializableDictionary<string, int> endScreenStats;

    [Header("End Screen")]
    public string[] endScreenUsernames = new string[lobbyManager.playerCount];
    public string[] endScreenStatsString = new string[lobbyManager.playerCount];
    public int[] endScreenSkins = new int[lobbyManager.playerCount];

    private void Awake()
    {
        userId = PlayerPrefs.GetString("userID");

        saveIcon.alpha = 0;

        DontDestroyOnLoad(this);

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

        if (gameOwnerID == null || gameOwnerID == string.Empty)
        {
            gameOwnerID = PlayerPrefs.GetString("userID");
        }

    }

    private void Start()
    {
        if (!Application.isEditor)
        {
            //discord = new Discord.Discord(1304575984531537991, (ulong)Discord.CreateFlags.NoRequireDiscord);
            ChangeActivity();
        }


        endScreenUsernames = new string[lobbyManager.playerCount];
        endScreenStatsString = new string[lobbyManager.playerCount];
        endScreenSkins = new int[lobbyManager.playerCount];

        notInEditor = SceneManager.GetActiveScene().name != "overworld";
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;


        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, false);
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        //if (discord != null)
        //    discord.Dispose();
    }

    public void ChangeActivity()
    {
        /*

        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity
        {
            Assets =
            {
                LargeImage = "icon",
                LargeText = "Breakwater"
            },
            Timestamps =
            {
                Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            }
        };
        activityManager.UpdateActivity(activity, (res) =>
        { });
    */
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "ending") return;

        dataPersistanceObjects = FindAllDataPersistanceObjects();
        //LoadGame();

        if (scene.name == "menu")
        {
            if (!PlayerPrefs.HasKey("lastSaveFile"))
                PlayerPrefs.SetString("lastSaveFile", "1");

            selectedProfileId = PlayerPrefs.GetString("lastSaveFile");
        }

    }
    public void OnSceneUnloaded(Scene scene)
    {

    }

    public SerializableDictionary<string, GameData> GetAllProfilesGameData()
    {
        return dataHandler.LoadAllProfiles();
    }


    public void ChangeSelectedProfile(string newProfileId)
    {
        selectedProfileId = newProfileId;

        LoadGame();

    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "overworld")
            timeSinceSave += Time.deltaTime;
        else
            timeSinceSave = 0;

        // Format the timeSinceSave into hh:mm
        timeSinceSaveFormatted = FormatTime(timeSinceSave);

        //if (discord != null)
        //    discord.RunCallbacks();
    }

    string FormatTime(float timeInSeconds)
    {
        int hours = Mathf.FloorToInt(timeInSeconds / 3600);
        int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);

        if (hours > 0)
        {
            return string.Format("{0} hour{1} {2} minute{3}", hours, hours > 1 ? "s" : "", minutes, minutes > 1 ? "s" : "");
        }
        else
        {
            return string.Format("{0} minute{1}", minutes, minutes > 1 ? "s" : "");
        }
    }

    public void NewGame(string fileName = "")
    {
        //dataHandler.Delete(selectedProfileId);
        gameData = new GameData(int.Parse(selectedProfileId), fileName);
    }

    public void ReplaceGame(GameData newData)
    {
        dataHandler.Delete(selectedProfileId);
        gameData = newData;
    }


    public void LoadGame(string newGameFileName = "")
    {
        gameData = dataHandler.Load(selectedProfileId);

        if (gameData == null)
        {
            print("Initializing default save values");
            NewGame(newGameFileName);
        }

        if (gameData.fileName == "" || gameData.fileName == null)
        {
            gameData.fileName = "File " + selectedProfileId;
        }

        foreach (IDataPersistance dataPersistanceObj in dataPersistanceObjects)
        {
            dataPersistanceObj.LoadData(gameData);
        }

    }

    public void DeleteProfileData(string profileId)
    {
        dataHandler.Delete(profileId);

        LoadGame();

        if (lobbyManager.instance) lobbyManager.instance.UpdateSaveFiles();

    }


    public void SaveGame(bool disableIcon = false)
    {
        //dont save if host left game
        if (playerSpawner.instance.players[0] == null) return;

        if (!disableIcon)
        {
            if (saveIconCo != null)
            {
                StopCoroutine(saveIconCo);
            }
            saveIconCo = StartCoroutine(_saveIcon());
        }
        if (gameData == null)
        {
            NewGame();
        }

        foreach (IDataPersistance dataPersistanceObj in dataPersistanceObjects)
        {
            try
            {
                dataPersistanceObj.SaveData(gameData);
            }
            catch (Exception ex)
            {
                // Get the full stack trace
                string[] stackLines = ex.StackTrace.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Find the top line in the stack trace related to your script
                string relevantLine = stackLines[0];

                string errorLine = $"Error: {ex.Message}\nLocation: {relevantLine.Trim()}".Split(".")[$"Error: {ex.Message}\nLocation: {relevantLine.Trim()}".Split(".").Length - 1];

                Chat.Instance.SendInChat(new string[] { "Failed to save data for " + dataPersistanceObj.ToString() + "; Line:" + errorLine }, new Color[] { new Color(0.9f, 0, 0) });
            }
        }

        timeSinceSave = 0;

        dataHandler.Save(gameData, selectedProfileId);
    }

    public void DirectSave(GameData newData)
    {
        dataHandler.Save(newData, selectedProfileId);
    }

    public void UpdateDataPersistanceObjects()
    {
        dataPersistanceObjects = FindAllDataPersistanceObjects();
    }


    IEnumerator _saveIcon()
    {
        saveIcon.DOFade(1, 0.4f);
        yield return new WaitForSeconds(0.5f);
        saveIcon.DOFade(0, 0.2f);
        yield return new WaitForSeconds(0.3f);

    }

    List<IDataPersistance> FindAllDataPersistanceObjects()
    {
        IEnumerable<IDataPersistance> dataPersistanceObjects = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IDataPersistance>();
        return new List<IDataPersistance>(dataPersistanceObjects);
    }



    public static int LocalActorIndex { get; set; } = -1;

    public int GetActorIndex(PlayerRef player)
    {
        if (player == null) player = SingletonRunner.runner.LocalPlayer;

        if (actorIndexMap.TryGetValue(player, out var index))
            return index;

        return -1;
    }

    public static void SetRoomProperties(SerializableDictionary<string, string> properties)
    {
        roomProperties = properties;
    }


}

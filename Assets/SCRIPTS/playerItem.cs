using Fusion;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class playerItem : MonoBehaviour
{
    public TextMeshProUGUI playerName;
    public Image playerAvatar;
    public TextMeshProUGUI skinNameText;
    public GameObject rightArrow, leftArrow;
    public Animation anim;
    public string userId;

    ExitGames.Client.Photon.Hashtable playerProperties = new ExitGames.Client.Photon.Hashtable();
    public Sprite[] avatars;
    public string[] names;

    public PlayerRef player;

    public int[] skinOrder;
    int skinOrderNum;

    public void SetPlayerInfo(PlayerRef _player, int index, string username)
    {
        playerName.text = username;
        playerName.color = lobbyManager.instance.usernameColors[index];
        player = _player;

        DataPersistanceManager.instance.actorIndexMap.TryAdd(player, index);

        skinNameText.gameObject.SetActive(false);
        rightArrow.SetActive(false);
        leftArrow.SetActive(false);

        if (player == SingletonRunner.runner.LocalPlayer)
        {
            ApplyLocalChanges();
            DataPersistanceManager.LocalActorIndex = index;
        }

        if (PlayerPrefs.HasKey("selectedSkin"))
        {
            skinOrderNum = skinOrder.ToList().IndexOf(PlayerPrefs.GetInt("selectedSkin"));
        }

    }

    public void ApplyLocalChanges()
    {
        skinNameText.gameObject.SetActive(true);
        rightArrow.SetActive(true);
        leftArrow.SetActive(true);

        //skinOrderNum = Array.IndexOf(skinOrder, (int)playerProperties["playerAvatar"]);
        //skinNameText.text = names[(int)playerProperties["playerAvatar"]];
    }

    public void onClickLeft()
    {
        if (skinOrderNum == avatars.Length - 1)
        {
            skinOrderNum = 0;
        }
        else
        {
            skinOrderNum++;
        }
        if (skinOrder[skinOrderNum] == 5 && PlayerPrefs.GetInt("hasGolden") == 0)
        {
            onClickLeft();
            return;
        }
        DataPersistanceManager.instance.localSkin = skinOrder[skinOrderNum];
        lobbyManager.selectedSkin = DataPersistanceManager.instance.localSkin;

        PlayerPrefs.SetInt("selectedSkin", DataPersistanceManager.instance.localSkin);

        skinNameText.text = names[DataPersistanceManager.instance.localSkin];


        LobbyManagerRpcs.Instance.RPC_SetSkin(DataPersistanceManager.instance.localSkin,SingletonRunner.runner.LocalPlayer);
    }

    public void onClickRight()
    {
        if (skinOrderNum == 0)
        {
            skinOrderNum = avatars.Length - 1;
        }
        else
        {
            skinOrderNum--;
        }
        if (skinOrder[skinOrderNum] == 5 && PlayerPrefs.GetInt("hasGolden") == 0)
        {
            onClickRight();
            return;
        }
        DataPersistanceManager.instance.localSkin = skinOrder[skinOrderNum];
        lobbyManager.selectedSkin = DataPersistanceManager.instance.localSkin;

        PlayerPrefs.SetInt("selectedSkin", DataPersistanceManager.instance.localSkin);

        skinNameText.text = names[DataPersistanceManager.instance.localSkin];


        LobbyManagerRpcs.Instance.RPC_SetSkin(DataPersistanceManager.instance.localSkin, SingletonRunner.runner.LocalPlayer);
    }


    private void Update()
    {
        if (!lobbyManager.instance.saveRenameInput.isFocused && !lobbyManager.instance.seedInput.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || InputManager.actions["Scroll Left"].started)
            {
                onClickLeft();
            }
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || InputManager.actions["Scroll Right"].started)
            {
                onClickRight();
            }
        }
        
        playerAvatar.sprite = avatars[DataPersistanceManager.instance.skinMap[player]];
        skinNameText.text = names[DataPersistanceManager.instance.skinMap[player]];
    }


}

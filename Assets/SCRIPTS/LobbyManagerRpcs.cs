using Fusion;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class LobbyManagerRpcs : NetworkBehaviour
{
    public static LobbyManagerRpcs Instance { get; private set; }
    public static NetworkObject view;


    [Networked, Capacity(256)]
    public string networkedSeedInput { get => default; set { } }


    private void Awake()
    {
        Instance = this;
        view = GetComponent<NetworkObject>();
    }

    #region Save Slot

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetSaveSlot(int slot, string saveFileID)
    {
        lobbyManager.instance.OnRpcSetSaveSlot(slot, saveFileID);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SaveFileNames(string[] names)
    {
        lobbyManager.instance.OnRpcSaveFileNames(names);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_DeleteSaveFile()
    {
        lobbyManager.instance.OnRpcDeleteSaveFile();
    }

    #endregion

    #region Settings

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetCommands(int option)
    {
        lobbyManager.instance.OnRpcSetCommands(option);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetDifficulty(int option)
    {
        lobbyManager.instance.OnRpcSetDifficulty(option);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetDuelMode(bool option)
    {
        lobbyManager.instance.OnRpcSetDuelMode(option);
    }

    #endregion

    #region Lobby / Game Flow

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_LoadLevel()
    {
        lobbyManager.instance.OnRpcLoadLevel();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetGameOwner(string userID)
    {
        lobbyManager.instance.OnRpcSetGameOwner(userID);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_UpdatePlayerList()
    {
        lobbyManager.instance.OnRpcUpdatePlayerList();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetUsername(string username, PlayerRef player)
    {
        lobbyManager.instance.OnRpcSetUsername(username, player);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetSkin(int skin, PlayerRef player)
    {
        lobbyManager.instance.OnRpcSetSkin(skin, player);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SendProperties(string jsonData)
    {
        DataPersistanceManager.SetRoomProperties(treeRPCs.StringToDictionary<string,string>(jsonData));
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RequestPlayerInfo()
    {
        lobbyManager.instance.OnRPCRequestPlayerInfo();
    }

    #endregion
}
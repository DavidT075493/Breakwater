using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SingletonRunner : MonoBehaviour, INetworkRunnerCallbacks, ISceneLoadDone
{
    public static SingletonRunner instance;
    public static NetworkRunner runner;
    public static NetworkEvents events;

    public static Dictionary<string, object> roomProperties;
    bool loadingMenu = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(gameObject);

        runner = gameObject.GetComponent<NetworkRunner>();
        events = gameObject.GetComponent<NetworkEvents>();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (lobbyManager.instance != null) lobbyManager.instance.OnSessionListUpdated(runner, sessionList);
    }
    public void SceneLoadDone(in SceneLoadDoneArgs sceneInfo)
    {
        
        //if (SceneManager.GetActiveScene().name == "menu")
        {
        //    Destroy(gameObject);
        }
    }
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (lobbyManager.instance != null) lobbyManager.instance.OnPlayerJoined(player);
    }


    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }


    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (lobbyManager.instance) lobbyManager.instance.OnRunnerShutdown();
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

#pragma warning disable UNT0006 // Incorrect message signature
    public void OnConnectedToServer(NetworkRunner runner)
#pragma warning restore UNT0006 // Incorrect message signature
    {

    }


    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }



    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (lobbyManager.instance != null) lobbyManager.instance.OnPlayerLeft(runner, player);


        if (Chat.Instance && runner.IsSharedModeMasterClient && player != runner.LocalPlayer)
        {
            Chat.Instance.SendInChat(new string[] { DataPersistanceManager.instance.usernameMap[player], " left the game" }, new Color[] { Chat.Instance.usernameColors[DataPersistanceManager.instance.GetActorIndex(player)], Color.yellow });
        }
    }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
    {
        
    }
}

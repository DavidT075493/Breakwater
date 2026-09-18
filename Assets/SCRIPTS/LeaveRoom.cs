using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveRoom : MonoBehaviour
{
    public static LeaveRoom instance;

    private void Awake()
    {
        instance = this;
    }

    public void ReloadLobby()
    {
        lobbyManager.instance.fadeGroup.DOFade(1, 0.4f);
        Invoke("Load", 0.4f);
    }
    
    void Load()
    {
        SceneManager.LoadScene("Lobby");
    }

}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class roomItem : MonoBehaviour
{
    public TextMeshProUGUI roomName;
    public Button button;
    lobbyManager manager;

    private void Start()
    {
        manager = FindAnyObjectByType<lobbyManager>();
    }


    public void setRoomName(string _roomName)
    {
        roomName.text = _roomName;
    }
    
    public void onClickItem()
    {
        //manager.JoinRoom(roomName.text);
    }


}

using DG.Tweening;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectToServer : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI quickJoinText, quickCreateText, useridText;
    public Button connectButton, quitButton, quickCreateButton;

    public CanvasGroup fadeGroup;
    public AudioMixer mixer;

    [SerializeField] AudioSource clickSound;

    public static string playerID;
    public const int windowedX = 800;
    public const int windowedY = 450;

    private void Awake()
    {
        connectButton.interactable = false;
        quitButton.interactable = false;
        quickCreateButton.interactable = false;
        Invoke("SetButtonsActive", 0.5f);

        if (!PlayerPrefs.HasKey("userID"))
        {
            playerID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("userID", playerID);
        }
        else
        {
            playerID = PlayerPrefs.GetString("userID");
        }

        useridText.text = "User ID: " + playerID;

        fadeGroup.alpha = 1;
        Invoke("FadeOut", 0.3f);
        nameInput.text = PlayerPrefs.GetString("username");

        if (!PlayerPrefs.HasKey("musicVol"))
            PlayerPrefs.SetFloat("musicVol", 1);

        if (!PlayerPrefs.HasKey("sfxVol"))
            PlayerPrefs.SetFloat("sfxVol", 1);

        mixer.SetFloat("sfxVol", Mathf.Log10(PlayerPrefs.GetFloat("sfxVol")) * 20);
        mixer.SetFloat("musicVol", Mathf.Log10(PlayerPrefs.GetFloat("musicVol")) * 20);

        AudioListener.volume = 1;

        if (SingletonRunner.instance) Destroy(SingletonRunner.instance.gameObject);

    }

    void SetButtonsActive()
    {
        connectButton.interactable = true;
        quitButton.interactable = true;
        quickCreateButton.interactable = true;
    }

    void FadeOut()
    {
        fadeGroup.DOFade(0, lobbyManager.fadeTime);
    }

    private void Update()
    {
        MouseCursor.state = 1;

        if (Input.GetKeyDown(KeyCode.F11))
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed)
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            else
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(windowedX, windowedY, FullScreenMode.Windowed);
            }
        }
    }

    public void OnClickConnect()
    {
        if (nameInput.text.Length >= 1)
        {
            clickSound.Play();

            PlayerPrefs.SetString("username", nameInput.text);
            //buttonText.text = "Connecting...";

            fadeGroup.DOFade(1, lobbyManager.fadeTime);

            nameInput.interactable = false;
            connectButton.interactable = false;
            quitButton.interactable = false;
            quickCreateButton.interactable = false;

            DataPersistanceManager.instance.localUsername = nameInput.text;

            StartCoroutine(_LoadLobby());
        }
    }

    public void OnClickUsernameBox()
    {
        clickSound.Play();
    }


    public void QuitGame()
    {
        Application.Quit();
    }

   
    IEnumerator _LoadLobby()
    {
        while (fadeGroup.alpha < 1) yield return null;
        SceneManager.LoadScene("Lobby");
    }

}

using DG.Tweening;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class pauseScreen : NetworkBehaviour
{
    //used for settings menu in lobby screen
    public bool lobbySettings;
    public static pauseScreen instance;
    public bool paused;
    public GameObject pauseStuff;
    public CanvasGroup pauseGroup;

    [SerializeField] CanvasGroup confirmGroup;

    bool confirmScreenUp;
    bool leaving;

    public CanvasGroup fadeGroup;
    public Button confirmButton, noButton;

    public CanvasGroup settingsGroup;
    bool inSettings;
    [SerializeField] AudioMixer mixer;
    float musicVol, sfxVol;
    [SerializeField] Slider musicSlider, sfxSlider;
    [SerializeField] Toggle fpsToggle, vsyncToggle;
    public TextMeshProUGUI loadTime;
    [SerializeField] TextMeshProUGUI disconnectText, disconnectWarningText, timeSinceSaveText;

    bool saveCooldown;
    bool loadingMenu;

    private void Awake()
    {
        instance = this;

    }
    private void Start()
    {
        if (confirmGroup)
        {
            confirmGroup.alpha = 0;
            confirmGroup.interactable = false;
            confirmGroup.blocksRaycasts = false;
        }
        OnSettingsPressed(false);
        paused = false;
        if (pauseGroup)
        {
            pauseGroup.alpha = 0;
            pauseStuff.SetActive(true);
        }
        settingsGroup.alpha = 0;

        if (!PlayerPrefs.HasKey("musicVol"))
            PlayerPrefs.SetFloat("musicVol", 1);

        if (!PlayerPrefs.HasKey("sfxVol"))
            PlayerPrefs.SetFloat("sfxVol", 1);

        if (!PlayerPrefs.HasKey("showFps"))
            PlayerPrefs.SetInt("showFps", 0);

        if (!PlayerPrefs.HasKey("vsync"))
            PlayerPrefs.SetInt("vsync", 1);

        mixer.SetFloat("sfxVol", Mathf.Log10(PlayerPrefs.GetFloat("sfxVol")) * 20);

        mixer.SetFloat("musicVol", Mathf.Log10(PlayerPrefs.GetFloat("musicVol")) * 20);

        fpsToggle.isOn = PlayerPrefs.GetInt("showFps") == 1;
        SetFpsOn(fpsToggle.isOn);

        vsyncToggle.isOn = PlayerPrefs.GetInt("vsync") == 1;
        SetVsync(PlayerPrefs.GetInt("vsync") == 1);

        //clamp volumes
        PlayerPrefs.SetFloat("musicVol", Mathf.Clamp(PlayerPrefs.GetFloat("musicVol"), 0, 1));
        PlayerPrefs.SetFloat("sfxVol", Mathf.Clamp(PlayerPrefs.GetFloat("sfxVol"), 0, 1));

        //set slider values
        musicSlider.value = PlayerPrefs.GetFloat("musicVol");
        sfxSlider.value = PlayerPrefs.GetFloat("sfxVol");

        if (!lobbySettings && (!SingletonRunner.instance || SingletonRunner.runner.IsSharedModeMasterClient || !SingletonRunner.runner.IsConnectedToServer && disconnectText))
        {
            disconnectText.text = "Close Server";
            disconnectWarningText.text = "Are you sure you want to close the server? All players will be kicked and all unsaved progress will be lost";
        }

    }


    void Update()
    {
        if ((InputManager.actions["Pause"].started || (paused && InputManager.actions["Exit"].started)) && (!(playerHealth.instance && playerHealth.instance.dead) || menu.instance.Spectating) 
            && !(inventory.instance && inventory.instance.inventoryOpen) 
            && !(menu.instance && menu.instance.mapMaximized) 
            && !(player.instance && player.instance.inCutscene)
            && !loadingMenu)
        {
            OnPausePressed();
        }

        if (player.instance && player.instance.inCutscene && paused) OnPausePressed();

    }


    void OnPausePressed()
    {
        if (inSettings)
        {
            OnSettingsPressed(false);
            return;
        }

        if (lobbySettings || !MapDisplay.finishedLoading 
            || (player.instance && (player.instance.inBoatAltAction || player.instance.pilotingBoat))) 
            return;

        if (confirmScreenUp)
        {
            CancelLeave();
            return;
        }

        if (Chat.Instance.chatOpen)
        {
            Chat.Instance.CloseChat();
            return;
        }

        //normal pause stuff
        if (!paused)
        {
            paused = true;
            pauseGroup.DOFade(1, 0.15f);

            MouseCursor.state = 1;

            //hide load time
            if (MapDisplay.Instance.loadingTimer)
                MapDisplay.Instance.loadingTimer.gameObject.SetActive(false);
        }
        else
        {
            if (!inventory.instance.inventoryOpen && !loadingMenu)
                MouseCursor.state = 0;

            paused = false;
            pauseGroup.DOFade(0, 0.15f);
        }

        pauseGroup.interactable = paused;
        pauseGroup.blocksRaycasts = paused;
    }

    public void resume()
    {
        paused = false;
        pauseGroup.DOFade(0, 0.4f);
        pauseGroup.interactable = paused;
        pauseGroup.blocksRaycasts = paused;

        MouseCursor.state = 0;
    }

    public void leaveGame()
    {
        //confirmation.SetActive(true);
        confirmScreenUp = true;

        timeSinceSaveText.text = "Time since last save: " + DataPersistanceManager.timeSinceSaveFormatted;
        timeSinceSaveText.enabled = DataPersistanceManager.timeSinceSave >= 60;

        confirmGroup.DOFade(1, 0.25f);
        confirmGroup.interactable = true;
        confirmGroup.blocksRaycasts = true;

    }
    public void CancelLeave()
    {
        confirmScreenUp = false;
        //confirmation.SetActive(false);

        confirmGroup.DOFade(0, 0.35f);
        confirmGroup.interactable = false;
        confirmGroup.blocksRaycasts = false;

    }

    public void ConfirmLeaveGame()
    {
        confirmButton.interactable = false;
        noButton.interactable = false;

        StartCoroutine(_LoadMenu());
    }


    public void OnSavePressed()
    {
        if (!saveCooldown && !lobbySettings && !gameManager.inBoss)
        {
            saveCooldown = true;

            if (Runner.IsSharedModeMasterClient)
                RPC_PressedSave(gameManager.instance.saveFileID);
            else
                RPC_MasterSave();

            Invoke("FinishSaveCooldown", 2);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_MasterSave()
    {
        if (!Runner.IsSharedModeMasterClient) return;

        RPC_PressedSave(gameManager.instance.saveFileID);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_PressedSave(string saveId)
    {
        if (!Runner.IsSharedModeMasterClient)
            gameManager.instance.saveFileID = saveId;

        DataPersistanceManager.instance.SaveGame();
    }


    void FinishSaveCooldown()
    {
        saveCooldown = false;
    }

    public void OnSettingsPressed(bool open)
    {
        inSettings = open;

        if (open)
        {
            settingsGroup.DOFade(1, 0.2f);
            settingsGroup.transform.localScale = new Vector2(0.9f, 0.9f);
            settingsGroup.transform.DOScale(1, 0.2f);
        }
        else
        {
            settingsGroup.DOFade(0, 0.2f);
            settingsGroup.transform.localScale = new Vector2(1, 1);
            settingsGroup.transform.DOScale(0.9f, 0.2f);
        }
        settingsGroup.interactable = open;
        settingsGroup.blocksRaycasts = open;

    }

    public void SetMusicVol(float sliderValue)
    {
        if (sliderValue > 0)
        {
            mixer.SetFloat("musicVol", Mathf.Log10(sliderValue) * 20);
            PlayerPrefs.SetFloat("musicVol", sliderValue);
            musicVol = sliderValue;
        }
        else
        {
            mixer.SetFloat("musicVol", -80);
            PlayerPrefs.SetFloat("musicVol", -80);
            musicVol = 0;
        }
    }
    public void SetSFXVol(float sliderValue)
    {
        if (sliderValue > 0)
        {
            mixer.SetFloat("sfxVol", Mathf.Log10(sliderValue) * 20);
            PlayerPrefs.SetFloat("sfxVol", sliderValue);
            sfxVol = sliderValue;
        }
        else
        {
            mixer.SetFloat("sfxVol", -80);
            PlayerPrefs.SetFloat("sfxVol", -80);
            sfxVol = 0;
        }
    }

    public void SetFpsOn(bool on)
    {
        FpsCounter.instance?.showFps(on);

        if (on)
            PlayerPrefs.SetInt("showFps", 1);
        else
            PlayerPrefs.SetInt("showFps", 0);
    }
    public void SetVsync(bool on)
    {
        if (on)
        {
            QualitySettings.vSyncCount = 1;
            PlayerPrefs.SetInt("vsync", 1);
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            PlayerPrefs.SetInt("vsync", 0);
        }
    }

    public IEnumerator _LoadMenu()
    {
        loadingMenu = true;

        MouseCursor.state = 1;

        if (player.instance.currentBoat)
        {
            player.instance.currentBoat.boatMusic.DOFade(0, 0.5f);
            player.instance.currentBoat.boatAmbience.DOFade(0, 0.5f);
        }
        gameManager.instance.musicSource.DOFade(0, 0.5f);
        gameManager.instance.ambienceSource.DOFade(0, 0.5f);
        
        fadeGroup.gameObject.SetActive(true);
        fadeGroup.DOFade(1, 0.5f);
        yield return new WaitForSeconds(0.6f);

        DOTween.KillAll();
        if (SingletonRunner.instance) Destroy(SingletonRunner.instance.gameObject);
        MouseCursor.state = 1;
        SceneManager.LoadScene("Lobby");

    }



}

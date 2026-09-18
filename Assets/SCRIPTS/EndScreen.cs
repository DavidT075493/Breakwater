using DG.Tweening;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScreen : NetworkBehaviour
{
    public Image bgImage;
    public Sprite dementiaBg;

    public CanvasGroup screenFade;
    public Image[] playerImages;
    public Sprite[] playerSprites;
    public AudioSource music;
    public Color menuFadeColor;
    public GameObject dementiaText;

    public TextMeshProUGUI[] statsTexts;
    public GameObject statsWindow;
    public CanvasGroup statsGroup, menuButtonGroup;

    public Image title;
    public Sprite[] titleSprites;

    void Start()
    {
        menuButtonGroup.interactable = false;
        menuButtonGroup.alpha = 0;
        statsWindow.SetActive(false);
        StartCoroutine(_FadeIn());
    }


    IEnumerator _FadeIn()
    {
        screenFade.alpha = 1;
        music.volume = 0;

        for (int i = 0; i < playerImages.Length; i++)
        {
            if (DataPersistanceManager.instance.endScreenStatsString[i] == null || DataPersistanceManager.instance.endScreenStatsString[i] == "")
            {
                playerImages[i].enabled = false;
                continue;
            }

            playerImages[i].enabled = true;
            playerImages[i].sprite = playerSprites[DataPersistanceManager.instance.endScreenSkins[i]];

            SerializableDictionary<string, int> stats = treeRPCs.StringToDictionary<string, int>(DataPersistanceManager.instance.endScreenStatsString[i]);

            statsTexts[i].text = DataPersistanceManager.instance.endScreenUsernames[i];

            foreach (KeyValuePair<string, int> entry in stats)
            {
                statsTexts[i].text += "\n" + entry.Value;
            }

        }

        dementiaText.SetActive(false);

        if (DataPersistanceManager.roomProperties.ContainsKey("difficulty"))
        {
            if (int.Parse(DataPersistanceManager.roomProperties["difficulty"]) == 1)
            {
                title.sprite = titleSprites[0];
                title.rectTransform.sizeDelta = new Vector2(740, 100);
            }
            if (int.Parse(DataPersistanceManager.roomProperties["difficulty"]) == 2)
            {
                title.sprite = titleSprites[1];
                title.rectTransform.sizeDelta = new Vector2(740, 100);
                bgImage.sprite = dementiaBg;

                if (PlayerPrefs.GetInt("goldenEligible" + DataPersistanceManager.instance.selectedProfileId) == 1 && !PlayerPrefs.HasKey("hasGolden"))
                {
                    dementiaText.SetActive(true);

                    PlayerPrefs.SetInt("hasGolden", 1);
                }
            }

        }


        yield return new WaitForSeconds(0.5f);

        music.DOFade(1, 0.4f);
        music.Play();

        screenFade.DOFade(0, 2);
        Cursor.visible = true;

        yield return new WaitForSeconds(3.5f);
        statsGroup.alpha = 0;
        statsWindow.SetActive(true);

        statsGroup.DOFade(1, 2);

        yield return new WaitForSeconds(2);
        menuButtonGroup.DOFade(1, 1);
        menuButtonGroup.interactable = true;

    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.ReliableLargeData)]
    void RPC_SetPlayer(int index, int skin, string username, string endScreenStats)
    {


    }

    public void OnClickMenu()
    {
        if (screenFade.alpha == 0)
            StartCoroutine(_LoadMenu());
    }
    IEnumerator _LoadMenu()
    {
        screenFade.GetComponent<Image>().color = menuFadeColor;
        screenFade.DOFade(1, 1.5f);
        screenFade.blocksRaycasts = true;
        screenFade.interactable = true;
        music.DOFade(0, 1.2f);
        yield return new WaitForSeconds(1.6f);
        SceneManager.LoadScene("menu");
    }


}

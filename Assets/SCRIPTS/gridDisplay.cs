
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class gridDisplay : MonoBehaviour
{
    public static bool buildMode;

    Transform target;
    public Transform mask;
    public RawImage gridImage;
    public RectTransform grid;
    Vector2 startSize;
    float startCamSize;
    Unity.Cinemachine.CinemachineVirtualCamera cam;

    public List<item.itemType> typesShowGrid = new List<item.itemType>();
    bool fullScreen;

    private void Start()
    {
        target = Camera.main.transform;
        cam = FindAnyObjectByType<Unity.Cinemachine.CinemachineVirtualCamera>();
        startCamSize = cam.m_Lens.OrthographicSize;
        startSize = grid.sizeDelta;

        fullScreen = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
    }

    private void Update()
    {
        if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow != fullScreen)
        {
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(0).gameObject.SetActive(true);
        }

        // Update the previous full-screen state
        fullScreen = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;

        if (HandMove.instance)
        {
            mask.position = InputManager.instance.mousePos;
            grid.localPosition = mask.localPosition * -1;
            gridImage.uvRect = new Rect(target.position.x / 19, target.position.y / 11, gridImage.uvRect.width, gridImage.uvRect.height);
            grid.sizeDelta = new Vector2(startSize.x * startCamSize/cam.m_Lens.OrthographicSize,startSize.y * startCamSize / cam.m_Lens.OrthographicSize);

            if (HandMove.instance.selectedItem && typesShowGrid.Contains(HandMove.instance.selectedItem.type) && HandMove.instance.canBuild && !inventory.instance.inventoryOpen && playerWater.instance.waterValue <= 1 && !gameManager.instance.writingSign && !player.instance.currentBoat && !player.instance.inCutscene)
            {
                buildMode = true;

                if (gridImage && gridImage.color.a != 1)
                    gridImage.DOFade(1, 0.2f);
                if (HandMove.instance.selectedItem.type == item.itemType.remover)
                {
                    gridImage.color = new Color(1, 0.1f, 0.1f);
                }
                else
                {
                    gridImage.color = Color.white;
                }
            
            }
            else
            {
                buildMode = false;

                if (gridImage && gridImage.color.a != 0)
                    gridImage.DOFade(0,0.2f);
            }
        
            
        }
    }



}

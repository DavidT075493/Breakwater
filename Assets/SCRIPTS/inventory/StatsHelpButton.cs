using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class StatsHelpButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CanvasGroup helpBox;

    private void Start()
    {
        helpBox.alpha = 0;
        helpBox.interactable= false;
        helpBox.blocksRaycasts = false;

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        helpBox.DOFade(1, 0.25f);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        helpBox.DOFade(0, 0.4f);
    }
}

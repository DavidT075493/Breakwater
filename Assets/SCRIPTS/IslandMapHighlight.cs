using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IslandMapHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static IslandMapHighlight selectedMapIsland;
    public Image image;
    public int biome;
    float startAlpha;
    public CanvasGroup textGroup;

    private void Start()
    {
        startAlpha = image.color.a;
        textGroup.alpha = 0;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectedMapIsland == null)
        {
            selectedMapIsland = this;
            image.DOFade(0.9f, 0.5f);
            transform.DOScale(1.12f, 0.5f);
            textGroup.DOFade(0.65f, 0.5f);
        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selectedMapIsland == this)
        {
            StopHighlight();
        }
    }

    public void StopHighlight()
    {
        selectedMapIsland = null;
        image.DOFade(startAlpha, 0.5f);
        transform.DOScale(1, 0.5f);
        textGroup.DOFade(0, 0.5f);
    }


}

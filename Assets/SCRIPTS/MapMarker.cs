using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapMarker : MonoBehaviour
{
    public static MapMarker selectedMarker;
    RectTransform rTransform;
    Vector2 defaultSize;
    public int markerType;

    private void Awake()
    {
        rTransform = GetComponent<RectTransform>();
        defaultSize = rTransform.sizeDelta;
    }

    private void Update()
    {
        if (!menu.instance.mapMaximized || menu.instance.holdingMarker)
        {
            selectedMarker = null;
            rTransform.sizeDelta = defaultSize;
            return;
        }


        bool isMouseInside = RectTransformUtility.RectangleContainsScreenPoint(rTransform, InputManager.instance.mousePos);
        
        if(isMouseInside && selectedMarker == null) 
        {
            selectedMarker = this;
        }
        else if(!isMouseInside && selectedMarker == this)
        {
            selectedMarker = null;
        }

        if(selectedMarker == this)
        {
            rTransform.DOSizeDelta(defaultSize * 1.5f, 0.3f);
        }
        else
        {
            rTransform.DOSizeDelta(defaultSize, 0.5f);
        }

    }


}

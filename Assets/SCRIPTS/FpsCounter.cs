using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FpsCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private float _hudRefreshRate = 0.5f;
    public static FpsCounter instance;

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        InvokeRepeating("Refresh", _hudRefreshRate,_hudRefreshRate);
    }

    public void showFps(bool show)
    {
        if(_fpsText)
        _fpsText.gameObject.SetActive(show);
    } 

    void Refresh()
    {
        int fps = (int)(1f / Time.unscaledDeltaTime);
        _fpsText.text = "FPS: " + fps;
    }

}

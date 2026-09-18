using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class menuMusic : MonoBehaviour
{
    public static menuMusic instance;
    public AudioSource music, waves;

    void Awake()
    {

        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(this);

        music.volume = 0;

        Invoke("StartMusic", 0.5f);
    }
    void StartMusic()
    {
        music.volume = 0;
        music.Play();
        music.DOFade(1, 0.5f);
        float wavesVol = waves.volume;
        waves.volume = 0;
        waves.Play();
        waves.DOFade(wavesVol, 0.5f);

        AudioListener.volume = 1;
    }


    // Update is called once per frame
    void Update()
    {
        if(SceneManager.GetActiveScene().name == "overworld")
        {
            Destroy(gameObject);
        }

    }
    public void MusicFadeOut()
    {
        music.DOFade(0f, 0.3f);
        waves.DOFade(0, 0.4f);
    }

}

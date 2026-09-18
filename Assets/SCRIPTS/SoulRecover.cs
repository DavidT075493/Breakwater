using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SoulRecover: MonoBehaviour
{
    float playerDist;

    const float musicFadeDist = 90;
    public AudioSource ambientSound, recoverSound;

    bool fadedIn = false;

    public SpriteRenderer sprite;
    public Light2D light;

    float lightIntensity = 2.6f;

    Tween fadeTween;

    private void Start()
    {
        ResetSoul();
    }

    public void ResetSoul()
    {
        ambientSound.volume = 0;
        fadedIn = false;

        light.intensity = lightIntensity;

        sprite.transform.localScale = Vector3.one;
        sprite.color = Color.white;

    }

    private void Update()
    {
        if (!MapDisplay.finishedLoading || 
            !(player.instance && player.instance.currentIsland.biome == (int)MapGenerator.biome.vale && player.instance.onIsland))
            return;
        
        playerDist = Vector2.Distance(transform.position,player.instance.transform.position);
    
        if(playerDist < musicFadeDist && ValeManager.instance.lostSoul)
        {
            gameManager.instance.musicSource.volume = Mathf.Clamp(((playerDist - musicFadeDist / 2) / (musicFadeDist / 2)), 0, 1);
        }

        if (!fadedIn && !playerHealth.instance.dead)
        {
            fadedIn = true;
            ambientSound.Play();
            ambientSound.volume = 0;
            fadeTween = ambientSound.DOFade(1, 3);
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Main Player") && ValeManager.instance.lostSoul)
        {
            ValeManager.instance.SoulRecovered();
            if(fadeTween != null)
                fadeTween.Kill();
            ambientSound.DOFade(0, 1);
            recoverSound.Play();

            DOTween.To(() => light.intensity, zest => light.intensity = zest, 0, 1);
            sprite.DOFade(0, 1);
            sprite.transform.DOScale(1.5f, 1);
            StartCoroutine(gameManager.instance._SwitchMusic("day", 3));
        }
    }


}
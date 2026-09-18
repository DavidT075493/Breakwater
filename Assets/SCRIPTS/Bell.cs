using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Bell : MonoBehaviour
{
    [SerializeField] AudioSource sound;
    [SerializeField] Animation anim;
    float cooldown = 2.5f;
    bool bellOnCooldown;
    static bool bellFlashOnCooldown;
    static bool trackerOnCooldown;
    [SerializeField] Light2D light;
    float defaultIntensity;
    public Collider2D damageCollider;

    public bool soulTracker;

    [SerializeField] SpriteRenderer sprite;
    Sprite defaultSprite;
    [SerializeField] Sprite deActiveSprite;

    [SerializeField] Light2D healLight;
    bool hasHealing = true;
    float healLightIntensity;

    private void Start()
    {
        if(sprite)
        defaultSprite = sprite.sprite;

        defaultIntensity = light.intensity;
        if(damageCollider)
        damageCollider.enabled = false;
        
        if(healLight && healLight.intensity > 0) healLightIntensity = healLight.intensity;

    }

    private void OnEnable()
    {
        if (healLight && healLightIntensity > 0)
        {
            hasHealing = true;
            healLight.intensity = healLightIntensity;
        }
    }

    public void Ring()
    {
        if (anim)
            anim.Play();
        StopAllCoroutines();

        if (bellOnCooldown && !soulTracker)
        {
            sound.volume = 0.5f;
            sound.Play();
            sound.DOFade(0, 1);
            return;
        }
        if (trackerOnCooldown && soulTracker) return;

        if (sound)
        {
            sound.volume = 1;
            sound.Play();
        }
        //bell
        if (!soulTracker)
        {
            if(!bellFlashOnCooldown)
            StartCoroutine(_LightFlash());
            
            bellFlashOnCooldown = true;
            
            bellOnCooldown = true;
            StartCoroutine(_Damage());
            StartCoroutine(ValeManager.instance._BellFlash(transform.position));
            Invoke("FinishCooldownBell", cooldown);
        }
        //tracker
        else
        {
            trackerOnCooldown = true;
            StartCoroutine(ValeManager.instance._SoulTrackerFlash());
            Invoke("FinishCooldownTracker", cooldown);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Weapon Hitbox") && !(anim && anim.isPlaying))
        {
            //bell
            if (!soulTracker)
            {
                treeRPCs.Instance.HitTreeAtPos(1, "null", Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), 3,true,1);

                if (hasHealing)
                {
                    DOTween.To(() => healLight.intensity, x => healLight.intensity = x, 0, 0.5f);
                    hasHealing = false;
                    playerHealth.instance.currentHealth += 5;
                }
            }
            //tracker
            else
                Ring();
        }

    }

    IEnumerator _LightFlash()
    {
        DOTween.To(() => light.intensity, x => light.intensity = x, 7, 0.3f);
        yield return new WaitForSeconds(0.3f);
        DOTween.To(() => light.intensity, x => light.intensity = x, defaultIntensity, 1.2f);
    }

    IEnumerator _Damage()
    {

        damageCollider.enabled = true;
        yield return new WaitForSeconds(0.5f);
        damageCollider.enabled = false;
        
        sprite.DOColor(new Color(0.35f, 0.25f, 0.35f),0.7f);
    }

    void FinishCooldownBell()
    {
        bellOnCooldown = false;
        bellFlashOnCooldown = false;

        sprite.DOColor(Color.white, 0.6f);

    }
    void FinishCooldownTracker()
    {
        trackerOnCooldown = false;
    }

    public void SetTrackerActive(bool active)
    {
        if (active) sprite.sprite = defaultSprite;
        else sprite.sprite = deActiveSprite;
    }

}

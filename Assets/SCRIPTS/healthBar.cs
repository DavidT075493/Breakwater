using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class healthBar : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Animator anim;
    float healthPercent;
    float maxWidth = 3.4f;

    const float lerpSpeed = 0.55f;

    private void Start()
    {
        maxWidth = sprite.size.x;
    }

    public void UpdateHealth(float Percent, bool instant = false)
    {
        if (Percent > 0 && Percent < 0.062f) Percent = 0.062f;
        
        anim.SetTrigger("Hit");
        
        healthPercent = Mathf.Clamp(Percent,-3,1);

        if (instant)
        {
            sprite.size = new Vector2(Mathf.Clamp(healthPercent, 0, 1) * maxWidth, sprite.size.y);
            sprite.color = SpriteColor(healthPercent);
        }
        else
        {
            DOTween.To(() => sprite.size, x => sprite.size = x, new Vector2(Mathf.Clamp(healthPercent, 0, 1) * maxWidth, sprite.size.y), lerpSpeed);
            sprite.DOColor(SpriteColor(healthPercent),lerpSpeed);
        }
    }

    Color SpriteColor(float healthPercent)
    {
        return new Color((1.5f - healthPercent) * 0.9f, healthPercent, 0);
    }

    public void hide(bool instant)
    {
        anim.SetBool("Hide", true);

        if (instant) anim.speed = 10;
        else anim.speed = 1;

    }

    public void setActiveFalse()
    {
        gameObject.SetActive(false);
    }

}

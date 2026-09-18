using UnityEngine;
using UnityEngine.UI;

public class healthDisplay : MonoBehaviour
{

    public float healthPercent = 1;
    public float healthUpdateSpeed = 0.5f;
    public RectTransform healthBar, healthBar2;
    public Image healthBarImage, healthPreviewImage, healthIcon;
    public Sprite[] healthIconSprites;
    Color previewColor;
    float previewStartAlpha;
    public RectTransform outline;
    float startSize, fillStartSize;

    [SerializeField] Color soulLostColor;
    Color defaultColor;

    bool lostSoul;
    CanvasGroup group;
    public Image freezeEffectImage;
    float heartFreezeShakeTimer = 0;
    Vector2 heartPos;

    private void Awake()
    {
        previewColor = healthPreviewImage.color;
        previewStartAlpha = healthPreviewImage.color.a;

        startSize = ((RectTransform)transform).sizeDelta.x;
        fillStartSize = ((RectTransform)healthBar.transform.GetChild(0).transform).sizeDelta.x;
        group = GetComponent<CanvasGroup>();
        defaultColor = healthBarImage.color;
        heartPos = healthIcon.rectTransform.localPosition;
    }

    private void Update()
    {
        if (!playerHealth.instance)
            return;

        playerHealth health = playerHealth.instance;

        if (menu.instance.Spectating && menu.instance.spectatingPlayer)
        {
            health = menu.instance.spectatingPlayer.health;
        }

        healthPercent = (health.currentHealth / (float)health.maxHealth);

        healthBar.localScale = Vector2.Lerp(healthBar.localScale, new Vector2(Mathf.Clamp(healthPercent * health.maxHealth / 100f, 0, health.maxHealth / 100f), 1), healthUpdateSpeed * Time.deltaTime);

        //food health preview
        if (HandMove.instance && HandMove.instance.selectedItem && (HandMove.instance.selectedItem.type == item.itemType.food) && inventory.instance.hotbarGroup.alpha > 0)
        {
            healthPreviewImage.color = Color.Lerp(healthPreviewImage.color, new Color(previewColor.r, previewColor.g, previewColor.b, previewStartAlpha), Time.deltaTime * 7);
            healthBar2.localScale = Vector2.Lerp(healthBar2.localScale, new Vector2(Mathf.Clamp((healthPercent * health.maxHealth / 100f) + (HandMove.instance.selectedItem.restoreHealth / (float)health.maxHealth), 0, health.maxHealth / 100f), 1), healthUpdateSpeed * Time.deltaTime / 2);
        }
        else if (HandMove.instance && HandMove.instance.selectedItem && (HandMove.instance.selectedItem.catalystEffect == item.CatalystEffect.health) && inventory.instance.hotbarGroup.alpha > 0)
        {
            healthPreviewImage.color = Color.Lerp(healthPreviewImage.color, new Color(previewColor.r, previewColor.g, previewColor.b, previewStartAlpha), Time.deltaTime * 7);
            healthBar2.localScale = Vector2.Lerp(healthBar2.localScale, new Vector2(Mathf.Clamp((healthPercent * health.maxHealth / 100f) + ((HandMove.instance.selectedItem.catalystEffectTime / ((float)HandMove.instance.selectedItem.catalystEffectStrength / 100)) / (float)health.maxHealth), 0, health.maxHealth / 100f), 1), healthUpdateSpeed * Time.deltaTime / 2);
        }
        else
        {
            healthPreviewImage.color = Color.Lerp(healthPreviewImage.color, new Color(previewColor.r, previewColor.g, previewColor.b, 0), Time.deltaTime * 7);
            healthBar2.localScale = Vector2.Lerp(healthBar2.localScale, new Vector2(Mathf.Clamp((healthPercent * health.maxHealth / 100f), 0, health.maxHealth / 100f), 1), healthUpdateSpeed * Time.deltaTime / 2);

        }

        //freeze effect
        freezeEffectImage.rectTransform.localScale = new Vector3((playerHealth.instance.maxHealth / 100f) * 1.01532635127f 
            * ((playerHealth.instance.freezeEffectAmount / (float)playerHealth.instance.maxHealth) + 0.015095f) + 0.01f, 1,1);
        
        freezeEffectImage.enabled = freezeEffectImage.rectTransform.localScale.x > 0;

        if(playerHealth.instance.iceEffectTime > 0)
        {
            heartFreezeShakeTimer -= Time.deltaTime;

            if(heartFreezeShakeTimer <= 0)
            {
                heartFreezeShakeTimer = 0.08f;
                healthIcon.rectTransform.localPosition = heartPos + new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f));
            }

        }
        else
        {
            healthIcon.rectTransform.localPosition = heartPos;
        }

        ((RectTransform)transform).sizeDelta = Vector2.Lerp(((RectTransform)transform).sizeDelta, new Vector2(startSize * (health.maxHealth / 100f), ((RectTransform)transform).sizeDelta.y), 8 * Time.deltaTime);
        outline.sizeDelta = Vector2.Lerp(outline.sizeDelta, new Vector2(startSize * (playerHealth.instance.maxHealth / 100f), outline.sizeDelta.y), 8 * Time.deltaTime);

        if (ValeManager.instance.lostSoul)
        {
            healthBarImage.color = Color.Lerp(healthBarImage.color, soulLostColor, Time.deltaTime * 2);
            healthIcon.sprite = healthIconSprites[1];
        }
        else if (HandMove.instance.jackpotActive)
        {
            healthBarImage.color = Color.Lerp(healthBarImage.color, new Color(0, 1, 0), Time.deltaTime * 2);
            healthIcon.sprite = healthIconSprites[3];
        }
        else if (inventory.charmEffect == "fractured")
        {
            healthBarImage.color = Color.Lerp(healthBarImage.color, new Color(1, 0, 0.65f), Time.deltaTime * 2);
            healthIcon.sprite = healthIconSprites[2];
        }
        else if(playerHealth.instance.inEnvyState)
        {
            healthBarImage.color = Color.Lerp(healthBarImage.color, soulLostColor, Time.deltaTime * 4);
            healthIcon.sprite = healthIconSprites[4];
        }
        else
        {
            healthBarImage.color = Color.Lerp(healthBarImage.color, defaultColor, Time.deltaTime * 2);
            healthIcon.sprite = healthIconSprites[0];
        }


    }

}

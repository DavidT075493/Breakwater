using DG.Tweening;
using TMPro;
using UnityEngine;

public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance;

    [SerializeField] RectTransform barFill;
    bool barActive;
    [SerializeField] CanvasGroup group;
    public TextMeshProUGUI bossNameText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        group.alpha = 0;
    }

    void Update()
    {
        if (!Boss.instance.heartDead)
        {
            if (gameManager.inBoss && !gameManager.inSecondPhase)
            {
                if (!barActive)
                {
                    barActive = true;
                    group.DOFade(1, 2);
                }

                barFill.localScale = Vector2.Lerp(barFill.localScale, new Vector2((float)Boss.instance.health.currentHealth / Boss.instance.health.maxHealth, 1), 10 * Time.deltaTime);
            }
            else if (gameManager.inSecondPhase)
            {
                if (!barActive)
                {
                    barActive = true;
                    group.DOFade(1, 2);
                }

                barFill.localScale = Vector2.Lerp(barFill.localScale, new Vector2((float)Boss.instance.heartHealth.currentHealth / Boss.instance.heartHealth.maxHealth, 1), 10 * Time.deltaTime);
            }

        }
        else
        {
            if (barActive)
            {
                barActive = false;
                group.DOFade(0, 2);
            }
        }

    }
}

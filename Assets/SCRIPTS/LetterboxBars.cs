using UnityEngine;
using UnityEngine.UI;

public class LetterboxFull : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    public RectTransform left;
    public RectTransform right;
    public RectTransform top;
    public RectTransform bottom;

    int lastW, lastH;

    void Update()
    {
        if (Screen.width == lastW && Screen.height == lastH)
            return;

        lastW = Screen.width;
        lastH = Screen.height;

        Refresh();
    }

    void Refresh()
    {
        float window = (float)Screen.width / Screen.height;

        // reset all
        HideAll();

        // too wide  vertical bars
        if (window > targetAspect)
        {
            float drawn = targetAspect / window; // UI width
            float pad = (1f - drawn) / 2f;

            SetSide(left, pad);
            SetSide(right, pad);
        }
        // too tall horizontal bars
        else if (window < targetAspect)
        {
            float drawn = window / targetAspect; // UI height
            float pad = (1f - drawn) / 2f;

            SetTopBottom(top, pad);
            SetTopBottom(bottom, pad);
        }
    }

    void HideAll()
    {
        left.gameObject.SetActive(false);
        right.gameObject.SetActive(false);
        top.gameObject.SetActive(false);
        bottom.gameObject.SetActive(false);
    }

    void SetSide(RectTransform r, float amount)
    {
        r.gameObject.SetActive(true);
        r.anchorMin = new Vector2(0, 0);
        r.anchorMax = new Vector2(amount, 1f);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    void SetTopBottom(RectTransform r, float amount)
    {
        r.gameObject.SetActive(true);
        r.anchorMin = new Vector2(0, 0);
        r.anchorMax = new Vector2(1f, amount);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}

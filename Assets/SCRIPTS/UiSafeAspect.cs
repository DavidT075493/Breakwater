using UnityEngine;

public class UiSafeAspect : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        float window = (float)Screen.width / Screen.height;

        float scale = window / targetAspect;

        if (scale < 1f)
        {
            rt.localScale = Vector3.one;
        }
        else
        {
            rt.localScale = new Vector3(1/scale, 1/ scale, 1);
        }
    }
}

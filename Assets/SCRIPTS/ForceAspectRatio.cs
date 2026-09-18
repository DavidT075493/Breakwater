using UnityEngine;

public class ForceAspectRatio : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    Camera cam;

    Texture2D black;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // 1x1 black texture for drawing
        black = new Texture2D(1, 1);
        black.SetPixel(0, 0, Color.black);
        black.Apply();
    }

    void Start()
    {
        ApplyLetterbox();
    }

    void Update()
    {
        // refresh on resize
        if (Screen.width != lastW || Screen.height != lastH)
        {
            lastW = Screen.width;
            lastH = Screen.height;

            ApplyLetterbox();
        }
    }

    int lastW, lastH;

    // =======================
    // YOUR EXACT ASPECT CODE
    // =======================
    void ApplyLetterbox()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            cam.rect = rect;

            // store for bars
            padLeft = padRight = 0;
            padTop = (1 - scaleHeight) / 2f;
            padBottom = padTop;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = cam.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            cam.rect = rect;

            // store for bars
            padTop = padBottom = 0;
            padLeft = (1 - scaleWidth) / 2f;
            padRight = padLeft;
        }
    }

    // padding in normalized 0–1 screen coords
    float padLeft, padRight, padTop, padBottom;

    // ===========================
    // DRAW BLACK BARS WITH GUI
    // ===========================
    void OnGUI()
    {
        // left
        if (padLeft > 0)
            GUI.DrawTexture(new Rect(0,
                                     0,
                                     Screen.width * padLeft,
                                     Screen.height),
                            black);

        // right
        if (padRight > 0)
            GUI.DrawTexture(new Rect(Screen.width * (1 - padRight),
                                     0,
                                     Screen.width * padRight,
                                     Screen.height),
                            black);

        // top
        if (padTop > 0)
            GUI.DrawTexture(new Rect(0,
                                     Screen.height * (1 - padTop),
                                     Screen.width,
                                     Screen.height * padTop),
                            black);

        // bottom
        if (padBottom > 0)
            GUI.DrawTexture(new Rect(0,
                                     0,
                                     Screen.width,
                                     Screen.height * padBottom),
                            black);
    }

    public static float GetNormalizedScreenSize(Camera cam)
    {
        // Reference resolution
        const float refWidth = 1920f;
        const float refHeight = 1080f;

        // cam.rect is already excluding black bars
        float contentWidth = Screen.width * cam.rect.width;
        float contentHeight = Screen.height * cam.rect.height;

        // Because aspect is fixed, height-based scaling is stable
        float scaleH = contentHeight / refHeight;
        float scaleW = contentWidth / refWidth;

        // They should be the same, but take the smaller to be safe
        return Mathf.Min(scaleH, scaleW);
    }
}

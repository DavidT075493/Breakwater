using UnityEngine;

public class MouseCursor : MonoBehaviour
{
    public GameObject dontDestroy;

    public static MouseCursor instance;
    public static int state = 0;
    public static bool pointerMode;

    public int startState = 0;

    public Animator anim;
    RectTransform t;

    private void Awake()
    {
        t = GetComponent<RectTransform>();
        
        if (instance == null)
        {
            DontDestroyOnLoad(dontDestroy);
            DontDestroyOnLoad(this);
            instance = this;


            state = startState;

        }
        else
        {
            Destroy(dontDestroy);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Cursor.visible = false;

        anim.SetInteger("State", state);

        t.position = InputManager.instance.mousePos;
        pointerMode = state == 1;

        if (InputManager.actions["Inventory Click L"].released) 
            anim.SetTrigger("Click");

    }
}

using TMPro;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public static DungeonChest currentDungeonChest;
    public GameObject indicatorPrefab;
    public Vector2 indicatorOffset;
    public Vector2 interactBoxSize;
    public LayerMask mainPlayerLayer;
    GameObject indicatorInstance;
    Animator indicatorAnim;
    TextMeshPro indicatorText;
    bool playerInRange;


    private void Update()
    {
        Collider2D playerCheck;
        playerCheck = Physics2D.OverlapBox(transform.position, interactBoxSize, 0, mainPlayerLayer);

        //no pick up item and use tile at the same time
        if (itemPickup.closestItem || Chat.Instance.chatOpen)
            playerCheck = null;

        if (playerCheck && !player.instance.currentBoat && !playerHealth.instance.dead && !player.instance.pilotingBoat && !Chat.Instance.chatOpen)
        {
            playerInRange = true;

            if (!indicatorInstance)
                spawnIndicator();

            if ((InputManager.actions["Interact"].started || menu.instance.testEPress)
                && !(inventory.instance.inventoryOpen && InputManager.usingController) && !HandMove.instance.teleporting)
            {
                OnInteract();
            }

        }
        else
        {
            destroyIndicator();
        }
    }

    public virtual void OnInteract()
    {

    }

    public void spawnIndicator()
    {
        if (indicatorInstance == null)
        {
            indicatorInstance = Instantiate(indicatorPrefab, transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
        }
        else
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);

            indicatorInstance = Instantiate(indicatorPrefab, transform.position + new Vector3(indicatorOffset.x, indicatorOffset.y, 0), Quaternion.identity);
        }
        indicatorAnim = indicatorInstance.GetComponent<Animator>();
        indicatorText = indicatorInstance.GetComponent<TextMeshPro>();

        indicatorText.text = "Return to Entrance";
    }

    public void destroyIndicator()
    {
        if (indicatorInstance != null)
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);
        }

    }
}

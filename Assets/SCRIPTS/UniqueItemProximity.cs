using DG.Tweening;
using UnityEngine;

public class UniqueItemProximity : MonoBehaviour
{
    [SerializeField] AudioSource radarSound;
    float startVol;
    public int island;

    void Start()
    {
        startVol = radarSound.volume;
        radarSound.volume = 0;
    }

    void Update()
    {
        radarSound.enabled = false;

        if (HandMove.instance && HandMove.instance.selectedItem && HandMove.instance.selectedItem.type == item.itemType.tracker && HandMove.instance.trackerActivated)
        {
            //print(HandMove.instance && HandMove.instance.selectedItem && HandMove.instance.selectedItem.type == item.itemType.tracker && HandMove.instance.trackerActivated);
            radarSound.volume = Mathf.Lerp(radarSound.volume, startVol, 2 * Time.deltaTime);
        }
        else if (radarSound.volume > 0.01f)
        {
            radarSound.volume = Mathf.Lerp(radarSound.volume, 0, 2 * Time.deltaTime);
        }
        else
        {
            radarSound.volume = 0;
        }

        if (!transform.GetChild(0).gameObject.activeSelf && MapGenerator.instance.islands[island].containsUniqueTool)
        {
            MapGenerator.instance.islands[island].containsUniqueTool = false;
        }

    }
}

using TMPro;
using UnityEngine;

public class DungeonRecall : Interactable
{

    public override void OnInteract()
    {
        StartCoroutine(HandMove.instance._TetherTeleport(false, Vector2.zero, true));
    }

}

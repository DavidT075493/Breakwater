using DG.Tweening;
using UnityEngine;

public class Anchor : MonoBehaviour
{
    [SerializeField] AudioSource activateSound;
    [SerializeField] SpriteRenderer sprite;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Main Player") && !ValeManager.instance.lostSoul)
        {
            sprite.transform.DOShakePosition(1.5f,0.5f,5);
            activateSound.Play();

            playerHealth.instance.ReturnFromVale();
            ValeManager.instance.EscapeVale();
        }

    }


}

using DG.Tweening;
using Fusion;
using UnityEngine;

public class placedTileHealth : MonoBehaviour
{
    public bool disabled;
    public GameObject hitParticles;
    public int health;
    public Transform activeCollider;
    public camShakeController shake;
    public placedTile PlacedTile;
    public Transform sprite;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(disabled) return;

        if (collision.CompareTag("Weapon Hitbox"))
        {
            int damage = 3;

            if (inventorySlot.equippedSlot.itemInSlot)
            {
                if (!PlacedTile.Item.rockTile)
                    damage = Mathf.RoundToInt(inventorySlot.equippedSlot.itemInSlot.treeDamage / 1.2f);
                else
                    damage = Mathf.RoundToInt(inventorySlot.equippedSlot.itemInSlot.rockDamage / 1.6f);
            }
            gameManager.instance.TileTakeDamage(damage, Vector2Int.RoundToInt(transform.position), SingletonRunner.runner.LocalPlayer.PlayerId);
        }
        else if (collision.CompareTag("Boss") && SingletonRunner.runner.IsSharedModeMasterClient)
        {
            gameManager.instance.TileTakeDamage(2000, Vector2Int.RoundToInt(transform.position), SingletonRunner.runner.LocalPlayer.PlayerId);
        }

    }
    //called from gamemanager
    public void TakeDamage(int damage, int playerId)
    {

        Instantiate(hitParticles, activeCollider.position, Quaternion.identity);
        health -= damage;

        if (health <= 0)
        {
            if (SingletonRunner.runner.LocalPlayer.PlayerId == playerId)
                gameManager.instance.RemoveTile(PlacedTile.transform.position, PlacedTile.Item, false);
            activeCollider.gameObject.SetActive(false);
        }
        else
        {
            shake.shake(5);
            sprite.DOShakeScale(0.4f, 0.3f, 10, 90);

            Invoke("ResetScale", 0.43f);
        }
    }

    void ResetScale()
    {
        sprite.localScale = Vector3.one;
    }

}

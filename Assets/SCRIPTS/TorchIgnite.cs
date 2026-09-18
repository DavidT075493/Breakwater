using UnityEngine;

public class TorchIgnite : MonoBehaviour
{
    public GameObject torchLight;
    public int torchIndex;
    public bool isLit;
    public AudioSource lightSound, extinguishSound;
    public ParticleSystem extinguishPs;
    bool cooldown;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Weapon Hitbox") && !cooldown)
        {
            cooldown = true;
            Invoke("CooldownFinish", 0.25f);

            //ice fang
            if (!isLit && HandMove.instance.selectedItem && HandMove.instance.selectedItem.uniqueType == item.UniqueType.ice)
            {
                DungeonGenerator.instance.RPC_LightTorch(torchIndex, true);
                DungeonGenerator.instance.RPC_LightTorch(torchIndex, false);
                return;
            }
            //fahrenheit
            if (isLit && HandMove.instance.selectedItem && HandMove.instance.selectedItem.uniqueType == item.UniqueType.smelt)
            {
                DungeonGenerator.instance.RPC_LightTorch(torchIndex, false);
                DungeonGenerator.instance.RPC_LightTorch(torchIndex, true);
                return;
            }
            //jackpot
            if (HandMove.instance.selectedItem && HandMove.instance.selectedItem.uniqueType == item.UniqueType.jackpot)
            {
                DungeonGenerator.instance.RPC_LightTorch(torchIndex, Random.Range(0,2) == 0);
                return;
            }

            DungeonGenerator.instance.RPC_LightTorch(torchIndex, !isLit);
        
        }
    }

    void CooldownFinish()
    {
        cooldown = false;
    }

    public void SetLit(bool lit)
    {
        isLit = lit;
        torchLight.SetActive(lit);
        if (lit)
        {
            lightSound.Play();
        }
        else
        {
            extinguishPs.Play();
            extinguishSound.Play();
        }
    }

}

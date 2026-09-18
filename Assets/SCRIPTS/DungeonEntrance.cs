using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonEntrance : MonoBehaviour
{
    public int dungeonIndex;
    public bool exit;
    public Vector2 playerSpawnOffset;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Main Player")
            && !player.instance.inCutscene && !player.instance.currentBoat && !playerHealth.instance.dead)
        {
            if(!exit)
                StartCoroutine(_EnterDungeon());
            else
                StartCoroutine(_ExitDungeon());
        }
    }

    IEnumerator _EnterDungeon()
    {
        player.instance.RPC_DungeonFade(false);

        menu.instance.blackFade.DOFade(1, 0.5f);
        player.instance.inCutscene = true;
        StartCoroutine(gameManager.instance._SwitchMusic("dungeon",1f));
        menu.instance.mapGroup.DOFade(0, 1);
        yield return new WaitForSeconds(0.5f);
        player.instance.inDungeon = dungeonIndex;
        yield return new WaitForSeconds(0.6f);

        DungeonGenerator.instance.EnterDungeon();

        MapDisplay.Instance.tileGridGameObject.SetActive(false);
        player.instance.transform.position = DungeonGenerator.instance.dungeons[dungeonIndex].position + playerSpawnOffset;
        player.instance.cam.ForceCameraPosition(player.instance.transform.position, player.instance.cam.transform.rotation);

        yield return new WaitForSeconds(0.5f);
        player.instance.RPC_DungeonFade(true);

        yield return new WaitForSeconds(1.5f);

        menu.instance.blackFade.DOFade(0, 0.5f);

        yield return new WaitForSeconds(0.5f);

        player.instance.inCutscene = false;
        yield return new WaitForSeconds(0.5f);
        DungeonGenerator.instance.SetModifierText(player.instance.inDungeon);
    }

    IEnumerator _ExitDungeon()
    {
        player.instance.RPC_DungeonFade(false);
        menu.instance.deathFade.DOFade(1, 0.5f);
        player.instance.inCutscene = true;
        gameManager.instance.musicSource.DOKill();
        gameManager.instance.musicSource.DOFade(0, 1);
        yield return new WaitForSeconds(1);

        MapDisplay.Instance.tileGridGameObject.SetActive(true);
        menu.instance.dungeonModifierText.gameObject.SetActive(false);
        player.instance.transform.position = (Vector2)featurePlacer.Instance.dungeonFeatures[dungeonIndex].selectedPosition + playerSpawnOffset;
        player.instance.cam.ForceCameraPosition(player.instance.transform.position, player.instance.cam.transform.rotation);
        player.instance.CheckIsland();
        player.instance.inDungeon = -1;

        string track = TimeManager.instance.isNight ? "night" : "day";
        StartCoroutine(gameManager.instance._SwitchMusic(track, 1f));

        yield return new WaitForSeconds(0.5f);
        //gameManager.instance.musicSource.DOKill();
        //gameManager.instance.musicSource.DOFade(1, 1);
        player.instance.RPC_DungeonFade(true);

        menu.instance.mapGroup.DOFade(menu.instance.mapAlpha, 1);

        yield return new WaitForSeconds(1);
        
        menu.instance.deathFade.DOFade(0, 0.5f);

        yield return new WaitForSeconds(0.5f);

        player.instance.inCutscene = false;
    }

}

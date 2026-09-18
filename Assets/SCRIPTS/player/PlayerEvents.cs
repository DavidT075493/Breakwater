using DG.Tweening;
using Fusion;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerEvents : NetworkBehaviour
{
    public static PlayerEvents instance;
    public player playerParent;
    public playerWater water;
    public AudioSource[] footSources = new AudioSource[2];
    public FootstepSurface currentSurface, currentTileSurface;

    public float feetOffset = -0.3f;
    public FootstepSurface gooStepSound, iceStepSound, arenaStepSound, planeStepSound;

    public ParticleSystem frostWalkPs;
    [Networked]
    public bool frostWalking { get; set; }

    private void Awake()
    {
        if (playerParent.view.HasInputAuthority) instance = this;
    }

    private void Update()
    {
        if (!playerWater.instance || !player.instance.view.IsValid) return;

        if (frostWalking)
        {
            currentSurface = iceStepSound;
            if (!frostWalkPs.isPlaying)
                frostWalkPs.Play();
            return;
        }
        if (frostWalkPs.isPlaying)
            frostWalkPs.Stop();

        //placed tile
        TileData currentPlacedTile;
        
        currentPlacedTile = gameManager.instance.placedTiles.Find(data => data.position == new Vector2(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y - feetOffset)) && data.Item && data.Item.floor);
        if (currentPlacedTile.Item)
        {
            currentSurface = currentPlacedTile.Item.floorStepSound;
            return;
        }

        Collider2D additionalFloor = Physics2D.OverlapBox(new Vector2(transform.position.x, transform.position.y - feetOffset), new Vector2(0.1f, 0.1f), 0, playerWater.instance.additionalGroundLayers);

        playerParent.onGooPuddle = false;
        if (additionalFloor)
        {
            currentSurface = gooStepSound;
            playerParent.onGooPuddle = true;
            return;
        }

        //boat
        if (playerParent.currentBoat)
        {
            currentSurface = playerParent.currentBoat.surface;
            return;
        }
        if (gameManager.instance.inArena)
        {
            currentSurface = arenaStepSound;
            return;
        }

        if (Boss.inRangeOfSos)
        {
            currentSurface = planeStepSound;
            return;
        }

        //ground or water tile
        
        if(playerParent.anim.GetInteger("Water Level") == 1 || playerWater.instance.waterWalkInWater) currentTileSurface = playerWater.instance.shallowWater.surface;
        if (playerParent.anim.GetInteger("Water Level") == 2) currentTileSurface = playerWater.instance.shallowWater.surface;

        currentSurface = currentTileSurface;
        
    }

    public void OnStep(int foot)
    {
        if (currentSurface == null || playerParent.health.dead) return;

        if(playerParent.view.HasStateAuthority)
        gameManager.endScreenStats["steps"]++;

        if(foot == -1) foot = UnityEngine.Random.Range(0,2);

        footSources[foot].clip = currentSurface.sounds[foot];
        footSources[foot].pitch = UnityEngine.Random.Range(currentSurface.pitchRange.x, currentSurface.pitchRange.y);
        footSources[foot].Play();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.position + new Vector3(0, feetOffset, 0),0.1f);
    }

}

[System.Serializable]
public class FootstepSurface
{
    public AudioClip[] sounds = new AudioClip[2];
    public Vector2 pitchRange = new Vector2(0.9f,1.05f);
    public bool ice;
}
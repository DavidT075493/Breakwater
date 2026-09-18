using DG.Tweening;
using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class WeatherManager : NetworkBehaviour, IDataPersistance
{
    public static WeatherManager Instance;

    public Vector2 timeBetweenStorms, stormDuration;
    public float rainStartTime, rainEndTime;

    public ParticleSystem rainPs, snowPs;
    public AudioSource rainSound, snowSound;
    public Volume vol, snowVol;
    float startPsRate, snowStartPsRate;
    bool raining;
    public bool shouldBeRaining;
    bool snowing;
    public bool snowstormSlowness;

    bool savedRaining;

    MapGenerator.biome currentBiome;
    public ParticleSystem swampParticles, desertParticles, volcanicParticles, snowAmbParticles, valeParticles, valeParticles2
        , dungeonParticles, windParticles;

    ParticleSystem.EmissionModule wind;
    Vector2 windDir;
    float windAngle;

    public CanvasGroup snowEffectVignette;
    public Image snowEffectVignetteImage;
    bool snowVignetteActive;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        snowEffectVignette.alpha = 0;
        startPsRate = rainPs.emission.rateOverTime.constant;
        snowStartPsRate = snowPs.emission.rateOverTime.constant;
        wind = windParticles.emission;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SetRain(bool isRaining)
    {
        if (isRaining)
        {
            shouldBeRaining = true;

            //no rain in desert
            if (currentBiome != MapGenerator.biome.desert &&
                currentBiome != MapGenerator.biome.snow &&
                currentBiome == MapGenerator.biome.volcanic &&
                currentBiome == MapGenerator.biome.vale
                && !gameManager.inBoss && !gameManager.inSecondPhase && !gameManager.instance.inArena && player.instance.inDungeon > -1)
            {
                raining = true;
                StartRain();
            }
            else
            {
                raining = false;
                if (currentBiome == MapGenerator.biome.snow)
                {
                    snowing = true;
                    StartSnow();
                }
            }
        }
        else
        {
            raining = false;
            snowing = false;
            shouldBeRaining = false;

            DOTween.To(() => vol.weight, x => vol.weight = x, 0, 3f);
            DOTween.To(() => snowVol.weight, x => snowVol.weight = x, 0, 3f);

            StartCoroutine(_TweenParticleRate(0, rainPs));
            StartCoroutine(_TweenParticleRate(0, snowPs));

            rainSound.DOFade(0, 3);
            snowSound.DOFade(0, 3);
        }


    }

    private void Update()
    {
        if (!player.instance) return;

        if (savedRaining)
        {
            savedRaining = false;
            RPC_SetRain(true);
        }

        if (rainStartTime > 0)
        {
            rainStartTime -= Time.deltaTime;

            if (rainStartTime <= 0)
            {
                RPC_SetRain(true);
                rainStartTime = 0;
                rainEndTime = Random.Range(stormDuration.x, stormDuration.y);
            }
        }

        if (rainEndTime > 0)
        {
            rainEndTime -= Time.deltaTime;

            if (rainEndTime <= 0)
            {
                RPC_SetRain(false);
                rainEndTime = 0;
                rainStartTime = Random.Range(timeBetweenStorms.x, timeBetweenStorms.y);
            }
        }

        currentBiome = (MapGenerator.biome)player.instance.currentIsland.biome;
        if (!player.instance.onIsland) currentBiome = MapGenerator.biome.grassy;

        var DEmission = desertParticles.emission;
        var SEmission = swampParticles.emission;
        var VEmission = volcanicParticles.emission;
        var SnEmission = snowAmbParticles.emission;
        var valeEmission = valeParticles.emission;
        var valeEmission2 = valeParticles2.emission;
        var dungeonEmission = dungeonParticles.emission;

        DEmission.enabled = currentBiome == MapGenerator.biome.desert;
        SEmission.enabled = currentBiome == MapGenerator.biome.swamp;
        VEmission.enabled = currentBiome == MapGenerator.biome.volcanic;
        SnEmission.enabled = currentBiome == MapGenerator.biome.snow;
        valeEmission.enabled = currentBiome == MapGenerator.biome.vale;
        valeEmission2.enabled = valeEmission.enabled;
        dungeonEmission.enabled = player.instance.inDungeon > -1;

        if(currentBiome == MapGenerator.biome.vale)
        {
            if (raining)
            {
                var emission = rainPs.emission;
                emission.rateOverTime = 0;
                rainPs.Clear();
                rainSound.DOFade(0, 1.5f);
            }
            if (snowing)
            {
                var emission = snowPs.emission;
                emission.rateOverTime = 0;
                snowPs.Clear();
                snowSound.DOFade(0, 1.5f);
            }
        }

        //disable rain in desert
        if ((currentBiome == MapGenerator.biome.desert
            || currentBiome == MapGenerator.biome.volcanic
            || snowing || gameManager.inBoss || gameManager.inSecondPhase
            || gameManager.instance.inArena || player.instance.inDungeon > -1)
            && raining)
        {
            DOTween.To(() => vol.weight, x => vol.weight = x, 0, 3f);
            StartCoroutine(_TweenParticleRate(0, rainPs));
            rainSound.DOFade(0, 6);
            raining = false;
        }
        //disable snow when not in the right biome
        if (snowing && currentBiome != MapGenerator.biome.snow)
        {
            DOTween.To(() => snowVol.weight, x => snowVol.weight = x, 0, 4f);
            StartCoroutine(_TweenParticleRate(0, snowPs));
            snowSound.DOFade(0, 6);
            snowing = false;
        }
        //enable rain when leaving desert
        else if (!(currentBiome == MapGenerator.biome.desert ||
            currentBiome == MapGenerator.biome.volcanic ||
            currentBiome == MapGenerator.biome.vale ||
            gameManager.inBoss || gameManager.inSecondPhase || player.instance.inDungeon > -1)
            && shouldBeRaining && !raining && !snowing
            && !gameManager.instance.inArena)
        {
            if (currentBiome != MapGenerator.biome.snow)
            {
                StartRain();
            }
            else
            {
                StartSnow();
            }
        }
        //enable snow when entering the snow biome while its raining
        else if (currentBiome == MapGenerator.biome.snow && raining && !snowing && shouldBeRaining)
        {
            StartSnow();
        }

        //wind in boat
        if (player.instance.currentBoat)
        {
            if (!wind.enabled)
            {
                windDir = new Vector3(player.instance.currentBoat.velocity.x, player.instance.currentBoat.velocity.y).normalized;
            }
            wind.enabled = player.instance.currentBoat.dif.magnitude > 0 && player.instance.currentBoat.consistentDirection
                && player.instance.currentBoat.velocityMagnitude / player.instance.currentBoat.moveSpeed > 0.5f;

            wind.rateOverTime = player.instance.currentBoat.velocityMagnitude / player.instance.currentBoat.moveSpeed * 25;

            windDir = Vector3.Slerp(windDir, new Vector3(player.instance.currentBoat.velocity.x, player.instance.currentBoat.velocity.y).normalized, Time.deltaTime * 2);

            windAngle = Mathf.Atan2(windDir.y, windDir.x) * Mathf.Rad2Deg - 90;
            windParticles.transform.parent.eulerAngles = new Vector3(0, 0, windAngle);
        }
        else
        {
            wind.enabled = false;
        }

        if ((snowstormSlowness || playerHealth.instance.iceEffectTime > 0) && player.instance.currentIsland.biome != (int)MapGenerator.biome.vale
            && !menu.instance.spectatingPlayer)
        {
            if (!snowVignetteActive)
            {
                snowVignetteActive = true;
                snowEffectVignette.DOKill();
                snowEffectVignette.DOFade(1, 2.5f);
            }
            if (playerHealth.instance.inHeatSource || (inventorySlot.equippedSlot.itemInSlot && inventorySlot.equippedSlot.itemInSlot.uniqueType == item.UniqueType.smelt))
            {
                snowEffectVignetteImage.color = Color.Lerp(snowEffectVignetteImage.color, new Color(1, 0.6f, 0.25f, 0.3f), Time.deltaTime * 0.7f);
            }
            else
            {
                snowEffectVignetteImage.color = Color.Lerp(snowEffectVignetteImage.color, new Color(0.7f, 0.8f, 1, 1), Time.deltaTime);
            }
        }
        else
        {
            if (snowVignetteActive)
            {
                snowVignetteActive = false;
                snowEffectVignette.DOKill();
                if(player.instance.currentIsland.biome != (int)MapGenerator.biome.vale)
                    snowEffectVignette.DOFade(0, 3);
                else
                    snowEffectVignette.DOFade(0, 0.5f);
            }
        }

    }

    void StartRain()
    {
        raining = true;

        rainPs.Play();
        rainPs.time = 0;
        DOTween.To(() => vol.weight, x => vol.weight = x, 0.6f, 3f);

        var emission = rainPs.emission;
        emission.rateOverTime = 0;
        emission.enabled = true;
        StartCoroutine(_TweenParticleRate(startPsRate, rainPs));


        rainSound.volume = 0;
        rainSound.Play();
        rainSound.DOFade(1, 5);
    }

    void StartSnow()
    {
        snowing = true;

        snowPs.Play();
        snowPs.time = 0;
        DOTween.To(() => snowVol.weight, x => snowVol.weight = x, 0.6f, 4f);

        var emission = snowPs.emission;
        emission.rateOverTime = 0;
        emission.enabled = true;
        StartCoroutine(_TweenParticleRate(snowStartPsRate, snowPs));


        snowSound.Play();
        snowSound.volume = 0;
        snowSound.DOFade(1, 5);
    }

    IEnumerator _TweenParticleRate(float endRate, ParticleSystem ps)
    {
        float elapsedTime = 0f;
        var emission = ps.emission;
        float startRate = ps.emission.rateOverTime.constant;

        while (elapsedTime < 3)
        {
            // Calculate the new rate based on the time elapsed
            float newRate = Mathf.Lerp(startRate, endRate, elapsedTime / 3);
            emission.rateOverTime = newRate;

            // Increase elapsed time by the time passed since the last frame
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // Ensure the final rate is set to the end rate
        emission.rateOverTime = endRate;

        snowstormSlowness = ps == snowPs && endRate != 0;
    }

    public void LoadData(GameData data)
    {
        rainStartTime = data.timeUntilRainStart;
        rainEndTime = data.timeUntilRainEnd;

        if (rainStartTime == 0 && rainEndTime == 0)
        {
            rainStartTime = Random.Range(timeBetweenStorms.x, timeBetweenStorms.y);
        }

        if (rainEndTime > 5) savedRaining = true;

    }

    public void SaveData(GameData data)
    {
        data.timeUntilRainStart = rainStartTime;
        data.timeUntilRainEnd = rainEndTime;
    }
}

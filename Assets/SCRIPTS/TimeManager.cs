using Fusion;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TimeManager : NetworkBehaviour, IDataPersistance
{
    public static TimeManager instance;

    [HideInInspector]
    [Networked] public float net_time { get; set; }
    [Networked] public int net_dayNum { get; set; }

    [Range(0, 2)]
    public float time;
    public int dayNum;

    public Volume nightVol, dawnVol;
    public float secondsInCycle = 60;
    public float lerpSpeed = 5;

    public Light2D globalLight;
    public bool isNight;

    public float nightSpeedMult = 1.5f;
    public Vector2 hardDayNightSpeedMult, dementiaDayNightSpeedMult;

    public TextMeshProUGUI dayText;
    public Animation dayTextAnim;
    public bool pauseTime;
    public bool disableTime;

    private void Awake()
    {
        instance = this;

        if (!DataPersistanceManager.instance.loadingSave)
        {
            dayNum = 0;
            time = 0;
        }
    }

    public IEnumerator _ChangeDay(int day, float delay)
    {
        if (!Application.isPlaying || gameManager.instance.inArena) yield break;

        if (!gameManager.inBoss && !player.instance.inCutscene)
        {
            dayNum = day;
            if (day < 1)
            {
                dayNum = 1;
            }

            if (EnemySpawner.instance)
                EnemySpawner.instance.IncrementEnemyMax();

            yield return new WaitForSeconds(delay);

            //day num animation - disabled in vale
            if (!(player.instance.currentIsland.biome == (int)MapGenerator.biome.vale && player.instance.onIsland))
            {
                dayText.text = "Day " + dayNum;
                dayTextAnim.Play();

            }
        }

    }


    void UpdateTime()
    {
        if (time >= 2)
            time = 0;

        float dawn = Mathf.Clamp(time - 0.9f, 0, 1) * 10;
        float night = 0;
        if (time > 1.15f)
        {
            dawn = Mathf.Clamp(1 - ((time - 1.15f) * 5), 0, 1);
            night = 1 - dawn;

            if (time > 1.85f)
            {
                night = 1 - ((time - 1.85f) * 6.66f);

                if (isNight == true)
                {
                    if (gameManager.instance)
                    {
                        if(player.instance.inDungeon == -1)
                        StartCoroutine(gameManager.instance._SwitchMusic("day", 14));
                        
                        StartCoroutine(_ChangeDay(dayNum + 1, 15));
                    }
                    isNight = false;
                }
            }
            else
            {
                if (isNight == false)
                {
                    if (gameManager.instance)
                    {
                        if(player.instance.inDungeon == -1)
                            StartCoroutine(gameManager.instance._SwitchMusic("night", 14));
                    }
                    isNight = true;
                }
            }

        }
        else
        {
            if (isNight == true)
            {
                if (gameManager.instance)
                    StartCoroutine(gameManager.instance._SwitchMusic("day", 14));
                isNight = false;
                StartCoroutine(_ChangeDay(dayNum + 1, 15));
            }
        }

        if (player.instance)
        {
            if(player.instance.underVolcanoRoof && player.instance.inDungeon == -1)
            {
                player.instance.playerLight.intensity = Mathf.Lerp(player.instance.playerLight.intensity, 1.35f, lerpSpeed * Time.deltaTime);
            }
            else if(player.instance.InVale())
            {
                player.instance.playerLight.intensity = Mathf.Lerp(player.instance.playerLight.intensity, 1.6f, lerpSpeed * Time.deltaTime);
            }
            else
            {
                player.instance.playerLight.intensity = Mathf.Lerp(player.instance.playerLight.intensity, 1, lerpSpeed * Time.deltaTime);
            }

            //under volcano roof
            if (player.instance.underVolcanoRoof && player.instance.inDungeon == -1)
            {
                dawnVol.weight = Mathf.Lerp(dawnVol.weight, 0.3f, lerpSpeed * Time.deltaTime);
                nightVol.weight = Mathf.Lerp(nightVol.weight, 0, lerpSpeed * Time.deltaTime);
                player.instance.playerLight.pointLightOuterRadius = Mathf.Lerp(player.instance.playerLight.pointLightOuterRadius, player.instance.caveLightRadius, lerpSpeed * Time.deltaTime);

                if (globalLight)
                    globalLight.intensity = Mathf.Lerp(globalLight.intensity, 0.2f, lerpSpeed * Time.deltaTime);
            }
            //vale
            else if (player.instance.onIsland && player.instance.currentIsland.biome == (int)MapGenerator.biome.vale)
            {
                dawnVol.weight = Mathf.Lerp(dawnVol.weight, 0, lerpSpeed * Time.deltaTime);
                nightVol.weight = Mathf.Lerp(nightVol.weight, 0, lerpSpeed * Time.deltaTime);
                player.instance.playerLight.pointLightOuterRadius = Mathf.Lerp(player.instance.playerLight.pointLightOuterRadius, player.instance.caveLightRadius, lerpSpeed * Time.deltaTime);

                if (globalLight)
                    globalLight.intensity = Mathf.Lerp(globalLight.intensity, 0f, lerpSpeed * Time.deltaTime);
            }
            //arena
            else if (gameManager.instance.inArena)
            {
                dawnVol.weight = Mathf.Lerp(dawnVol.weight, 0.3f, lerpSpeed * Time.deltaTime);
                nightVol.weight = Mathf.Lerp(nightVol.weight, 0, lerpSpeed * Time.deltaTime);
                player.instance.playerLight.pointLightOuterRadius = Mathf.Lerp(player.instance.playerLight.pointLightOuterRadius, player.instance.caveLightRadius * 0.9f, lerpSpeed * Time.deltaTime);

                if (globalLight)
                    globalLight.intensity = Mathf.Lerp(globalLight.intensity, 0.5f, lerpSpeed * Time.deltaTime);
            }
            //inside dungeon
            else if (player.instance.inDungeon > -1)
            {
                dawnVol.weight = Mathf.Lerp(dawnVol.weight, 0, lerpSpeed * Time.deltaTime);
                nightVol.weight = Mathf.Lerp(nightVol.weight, 0, lerpSpeed * Time.deltaTime);
                player.instance.playerLight.pointLightOuterRadius = Mathf.Lerp(player.instance.playerLight.pointLightOuterRadius, player.instance.caveLightRadius * 0.9f, lerpSpeed * Time.deltaTime);

                float intensity = player.instance.inDungeon != 4 ? 0.14f : 0;

                if (globalLight)
                    globalLight.intensity = Mathf.Lerp(globalLight.intensity, intensity, lerpSpeed * Time.deltaTime);
            }
            //normal lighting
            else
            {
                dawnVol.weight = Mathf.Lerp(dawnVol.weight, dawn, lerpSpeed * Time.deltaTime);
                nightVol.weight = Mathf.Lerp(nightVol.weight, night, lerpSpeed * Time.deltaTime);
                player.instance.playerLight.pointLightOuterRadius = Mathf.Lerp(player.instance.playerLight.pointLightOuterRadius, player.instance.defaultLightRadius, lerpSpeed * Time.deltaTime);

                if (globalLight)
                {
                    float targetIntensity;

                    if (gameManager.instance.difficulty != 2)
                        targetIntensity = Mathf.Lerp(1f, 0.1f, night);
                    else
                        targetIntensity = 1f - night;

                    globalLight.intensity = Mathf.Lerp(
                        globalLight.intensity,
                        targetIntensity,
                        lerpSpeed * Time.deltaTime
                    );
                }
            }

            if (player.instance.inDungeon > -1 && dayTextAnim.isPlaying)
            {
                dayTextAnim.Stop();
                dayTextAnim.GetComponent<Image>().enabled = false;
                dayText.enabled = false;
            }
        }
        //editor - no lerp
        else
        {
            dawnVol.weight = dawn;
            nightVol.weight = night;

            if (globalLight)
                globalLight.intensity = ((1 - nightVol.weight) * 0.9f) + 0.1f;
        }


    }


    private void Update()
    {
        if (!MapDisplay.finishedLoading) return;

        if (!SingletonRunner.runner.IsSharedModeMasterClient)
        {
            time = net_time;
            dayNum = net_dayNum;
            UpdateTime();
            return;
        }
        else
        {
            net_time = time;
            net_dayNum = dayNum;
        }


        //boss
        if (disableTime || gameManager.instance.disableEnemies)
        {
            if (time != 0)
            {
                if (time <= 1.3)
                {
                    time = Mathf.MoveTowards(time, 0, Time.deltaTime);
                }
                else
                {
                    time = Mathf.MoveTowards(time, 2, Time.deltaTime);
                }
            }

            UpdateTime();
            return;
        }

        //time move faster at night
        if (!pauseTime)
        {

            if (!isNight)
            {
                switch (gameManager.instance.difficulty)
                {
                    case 0:
                        time += (Time.deltaTime / secondsInCycle); break;
                    case 1:
                        time += (Time.deltaTime / secondsInCycle) * hardDayNightSpeedMult.x; break;
                    case 2:
                        time += (Time.deltaTime / secondsInCycle) * dementiaDayNightSpeedMult.x; break;
                }

            }
            else
            {
                switch (gameManager.instance.difficulty)
                {
                    case 0:
                        time += (Time.deltaTime / secondsInCycle) * nightSpeedMult; break;
                    case 1:
                        time += (Time.deltaTime / secondsInCycle) * hardDayNightSpeedMult.y; break;
                    case 2:
                        time += (Time.deltaTime / secondsInCycle) * dementiaDayNightSpeedMult.y; break;
                }
            }
        }

        UpdateTime();

    }


    private void OnValidate()
    {
        //not during play mode
        if (!player.instance)
            UpdateTime();
    }

    public void SaveData(GameData data)
    {
        data.time = time;
        if (dayNum < 1) dayNum = 1;
        data.dayNum = dayNum - 1;
    }
    public void LoadData(GameData data)
    {
        if (!DataPersistanceManager.instance.loadingSave) return;

        time = data.time;
        dayNum = data.dayNum;

        isNight = time > 1.15f && time < 1.85f;

    }



}



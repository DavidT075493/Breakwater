using DG.Tweening;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Boss : Enemy
{
    public static Boss instance;

    [SerializeField] AudioSource idleSource, combatSource, rushSource, sosSource, deadSource;
    public AudioSource insideMusicSource;
    [SerializeField] AudioClip combatIntroClip, combatMainClip, bossStartClip, sosClip, heliClip;
    public bool inRange;
    Coroutine combatMusicCoroutine;
    [SerializeField] Volume volume;
    Vector2 targetPos;
    Vector2 lastTargetPos;
    [SerializeField] Animator planeAnim;
    [SerializeField] Transform cutsceneCamTarget;
    [SerializeField] ParticleSystem[] planeParticles;
    [SerializeField] camShakeController screamShake;
    [SerializeField] Animator[] eyeAnims;
    [SerializeField] Animator mouthAnim;
    [SerializeField] SpriteRenderer rushSprite;
    [SerializeField] Vector2 attackDelay = new Vector2(3, 7);
    [SerializeField] Vector2 hardAttackDelay = new Vector2(3, 7);
    [SerializeField] Vector2 dementiaAttackDelay = new Vector2(3, 7);

    public GameObject gooPuddle;
    [SerializeField] damagePlayer contactDamage;
    bool inRush;
    float startRushMoveSpeedMult = 1;
    [SerializeField] NetworkTransform transformView;
    public bool startedBoss;
    public Collider2D deadCollider;
    [SerializeField] Transform insideEnterCheck;
    [SerializeField] TextMeshPro playerCountText;
    [SerializeField] LayerMask playerLayers, mainPlayerLayer;
    public CanvasGroup screenFade;
    public Image fadeImage;
    [SerializeField] Transform[] insidePlayerSpawnPoints;
    private float insideMusicSourceVol;
    bool startedPhaseTwo;
    public bool dead;
    public GameObject bossInside;
    [SerializeField] Volume insideVolume;
    public enemyHealth heartHealth;
    public Spawner[] spawners;
    [SerializeField] Volume laserVol;
    [SerializeField] ParticleSystem dashPs;
    bool doneEscapeMove;
    public bool heartDead;
    bool cameraCutscene;
    public Transform planeInsideCheck;

    public GameObject indicatorPrefab;
    GameObject indicatorInstance;
    Animator indicatorAnim;
    TextMeshPro indicatorText;
    [SerializeField] SpriteRenderer planeOutsideSprite;
    [SerializeField] GameObject planeHeadObj;
    public static bool inRangeOfSos;

    [SerializeField] SpriteRenderer[] eyeBloodSprites;

    [Networked]
    public Vector2 NetworkLocalPosition { get; set; }

    [SerializeField] float interpolationSpeed = 15f;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (gameManager.instance.difficulty == 1) attackDelay = hardAttackDelay;
        if (gameManager.instance.difficulty == 2) attackDelay = dementiaAttackDelay;

        inRush = false;

        laserVol.weight = 0;

        insideVolume.enabled = false;

        insideMusicSourceVol = insideMusicSource.volume;
        insideMusicSource.volume = 0;

        startedPhaseTwo = false;

        volume.weight = 0;
        normalSpeed = currentSpeed;
        transform.GetChild(0).gameObject.SetActive(false);

        laserCollider = laserSprite.GetComponent<BoxCollider2D>();
        laserCollider.enabled = false;
        laserSprite.enabled = false;
        rushSprite.enabled = false;

        shootTimer = Random.Range(attackDelay.x, attackDelay.y);

        foreach (SpriteRenderer sprite in eyeBloodSprites)
        {
            sprite.color = new Color(1, 1, 1, 0);
        }

    }

    void ChooseTargetPos()
    {
        while (Vector2.Distance(lastTargetPos, targetPos) < 20 || Vector2.Distance(lastTargetPos, targetPos) > 70)
        {
            targetPos = new Vector2(Random.Range(-110, 110), Random.Range(-110, 110));
        }
        lastTargetPos = targetPos;

    }

    public void EnterAnim()
    {
        RPC_EnterAnim();

        if (Runner.IsSharedModeMasterClient)
            view.AssignInputAuthority(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_EnterAnim()
    {
        StartCoroutine(_EnterAnim());
    }

    IEnumerator _EnterAnim()
    {
        gameManager.instance.worldBorder.localScale = Vector3.one;

        gameManager.instance.DisableMusic();
        startedBoss = true;

        inventory.instance.CloseInventory(true);
        menu.instance.uiGroup.DOFade(0, 1);

        player.instance.inCutscene = true;
        yield return null;
        TimeManager.instance.disableTime = true;
        DOTween.To(() => volume.weight, x => volume.weight = x, 0.5f, 3);
        combatSource.clip = bossStartClip;
        combatSource.volume = 1;
        combatSource.Play();

        player.instance.cam.Follow = cutsceneCamTarget;

        foreach (ParticleSystem p in planeParticles)
        {
            p.Stop();
        }

        cameraCutscene = true;
        planeAnim.SetTrigger("Start");
        gooPuddle.GetComponent<SpriteRenderer>().enabled = false;
        yield return new WaitForSeconds(2.3f);
        gooPuddle.SetActive(false);
        yield return new WaitForSeconds(1.7f);
        planeAnim.gameObject.SetActive(false);
        TimeManager.instance.dayNum = 8 + playerSpawner.playerCountInGame;
        EnemySpawner.instance.IncrementEnemyMax();

        transform.GetChild(0).gameObject.SetActive(true);
        mouthAnim.SetTrigger("Scream");
        yield return new WaitForSeconds(0.83f);
        DOTween.To(() => player.instance.camSizeMultiplier, x => player.instance.camSizeMultiplier = x, 0.4f, 0.5f);
        angySound.Play();
        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 2f;
        yield return new WaitForSeconds(2f);
        cameraCutscene = false;
        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0f;
        player.instance.camFollow.position = cutsceneCamTarget.position;
        player.instance.cam.Follow = player.instance.camFollow;
        player.instance.camFollow.DOMove(player.instance.transform.position, 1);
        yield return null;
        DOTween.To(() => player.instance.camSizeMultiplier, x => player.instance.camSizeMultiplier = x, 1, 0.6f);
        yield return new WaitForSeconds(1f);
        StartBoss();
        combatSource.volume = 0;

        player.instance.inCutscene = false;

        menu.instance.uiGroup.DOFade(1, 0.7f);

        foreach (Animator anim in eyeAnims)
        {
            anim.SetTrigger("Start");
            yield return new WaitForSeconds(0.15f);
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_ShootLaser(int targetPlayer)
    {
        StartCoroutine(_ShootLaser(targetPlayer));
    }

    IEnumerator _ShootLaser(int targetPlayer)
    {
        Transform target = playerSpawner.instance.players[targetPlayer].transform;

        //faster on hard
        if (gameManager.instance.difficulty > 0)
        {
            laserSource.pitch = 1.2f;
        }
        else
        {
            laserSource.pitch = 1;
        }

        laserSource.Play();

        Vector2 direction = new Vector3(target.position.x - transform.position.x, target.position.y - transform.position.y).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        laserSprite.transform.eulerAngles = new Vector3(0, 0, angle);
        laserSprite.enabled = true;
        laserSprite.sprite = warning;
        laserSprite.size = new Vector2(1000, 3);
        laserSprite.drawMode = SpriteDrawMode.Tiled;
        laserSprite.material = laserPreviewMat;
        mouthAnim.SetTrigger("Shoot");


        laserSprite.color = new Color(1, 1, 1, 0);
        laserSprite.DOFade(0.3f, 0.2f);
        laserSprite.flipY = Mathf.Abs(angle) > 90;
        yield return new WaitForSeconds(0.2f);

        for (float t = 0; t < 0.9f; t += Time.deltaTime)
        {

            direction = Vector3.Slerp(direction, new Vector3(target.position.x - transform.position.x, target.position.y - transform.position.y).normalized, Time.deltaTime * 6f);
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            laserSprite.transform.eulerAngles = new Vector3(0, 0, angle);
            laserSprite.color = new Color(laserSprite.color.r, laserSprite.color.g, laserSprite.color.b, (Mathf.Sin(1.7f * t * Mathf.PI)) / 3 + 0.3f);

            laserSprite.flipY = Mathf.Abs(angle) > 90;

            //faster on hard
            if (gameManager.instance.difficulty > 0)
            {
                t += Time.deltaTime * 0.6f;
            }

            yield return null;
        }

        if (dead)
        {
            laserCollider.gameObject.SetActive(false);
            laserSource.DOFade(0, 0.5f);
            print("cancel laser");
            yield break;
        }

        laserSprite.DOFade(0, 0.4f);
        yield return new WaitForSeconds(0.4f);
        laserSprite.material = laserMat;
        laserSprite.color = Color.white;
        laserSprite.sprite = laser;
        laserSprite.size = new Vector2(0, laserSprite.size.y);
        laserSprite.drawMode = SpriteDrawMode.Sliced;

        laserCollider.offset = new Vector2(laserSprite.size.x / 2f, 0); // Adjust for pivot (0, 0.5)
        laserCollider.size = laserSprite.size;
        laserCollider.enabled = true;

        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 1.6f;
        DOTween.To(() => laserVol.weight, x => laserVol.weight = x, 1, 1);

        int frameCounter = 0;

        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            if (dead)
            {
                laserCollider.gameObject.SetActive(false);
                laserSource.DOFade(0, 0.5f);
                yield break;
            }

            if (t < 1f)
            {
                laserSprite.size = Vector2.MoveTowards(
                    laserSprite.size,
                    new Vector2(1000, laserSprite.size.y),
                    (30 + (500 * t) + (Mathf.Pow(10, t * 4))) * Time.deltaTime
                );

                if (frameCounter % 5 == 0) // Only update collider size every 5 frames
                {
                    laserCollider.size = laserSprite.size;
                    laserCollider.offset = new Vector2(laserSprite.size.x / 2f, 0);
                }
            }

            if (t > 1f)
            {
                laserCollider.enabled = false;
                laserSprite.size = Vector2.Lerp(laserSprite.size, new Vector2(1000, 0), 6.5f * Time.deltaTime);
            }

            frameCounter++;
            yield return null;
        }
        DOTween.To(() => laserVol.weight, x => laserVol.weight = x, 0, 2);
        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0;

        laserSprite.enabled = false;
    }

    Vector2 RotateVector2(Vector2 vector, Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x); // Get the angle of the direction
        float sin = Mathf.Sin(angle);
        float cos = Mathf.Cos(angle);

        // Rotate the vector
        float rotatedX = cos * vector.x - sin * vector.y;
        float rotatedY = sin * vector.x + cos * vector.y;

        return new Vector2(rotatedX, rotatedY);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_Rush(int targetPlayer, bool dashEscapeMove, Vector2 dashTargetPos)
    {
        StartCoroutine(_Rush(targetPlayer, dashEscapeMove, dashTargetPos));
    }

    IEnumerator _Rush(int targetPlayer, bool dashEscapeMove, Vector2 dashTargetPos)
    {
        Transform target = playerSpawner.instance.players[targetPlayer].transform;

        rushSprite.color = new Color(1, 1, 1, 0.3f);

        inRush = true;

        Vector2 direction;
        float angle;

        //move back
        if (!dashEscapeMove)
        {
            direction = new Vector3(target.position.x - transform.position.x, target.position.y - transform.position.y).normalized;
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            rushSprite.transform.eulerAngles = new Vector3(0, 0, angle);
            rushSprite.enabled = true;

            transform.DOMove((Vector2)transform.position + RotateVector2(new Vector3(-4, 0), rushSprite.transform.right), 1.1f);
        }
        else
        {
            targetPos = dashTargetPos;
            lastTargetPos = transform.position;

            direction = new Vector3(targetPos.x - transform.position.x, targetPos.y - transform.position.y).normalized;
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            rushSprite.transform.eulerAngles = new Vector3(0, 0, angle);
            rushSprite.enabled = true;

            transform.DOMove((Vector2)transform.position + RotateVector2(new Vector3(-3, 0), rushSprite.transform.right), 1.8f);
        }

        DOTween.To(() => startRushMoveSpeedMult, x => startRushMoveSpeedMult = x, 0, 0.4f);

        rushSprite.size = new Vector2(0, rushSprite.size.y);
        anim.SetTrigger("Dash");
        //warning
        if (!dashEscapeMove)
        {
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                rushSprite.size = Vector2.Lerp(rushSprite.size, new Vector2(27, rushSprite.size.y), Time.deltaTime * 2.5f);
                rushSprite.flipY = Mathf.Abs(angle) > 90;

                yield return null;
            }

            for (float t = 0; t < 1.1f; t += Time.deltaTime)
            {
                direction = Vector3.Slerp(direction, new Vector3(target.position.x - transform.position.x, target.position.y - transform.position.y).normalized, Time.deltaTime * 0.25f);
                angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                rushSprite.transform.eulerAngles = new Vector3(0, 0, angle);
                rushSprite.color = new Color(laserSprite.color.r, laserSprite.color.g, laserSprite.color.b, (Mathf.Sin(2 * t * Mathf.PI)) / 3 + 0.3f);

                rushSprite.flipY = Mathf.Abs(angle) > 90;

                //faster on hard
                if (gameManager.instance.difficulty > 0)
                {
                    t += Time.deltaTime * 0.6f;
                }

                yield return null;
            }
        }
        //escape warning
        else
        {
            angySound.Play();
            player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 1.9f;

            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            {
                rushSprite.size = Vector2.Lerp(rushSprite.size, new Vector2(Vector2.Distance(transform.position, targetPos), rushSprite.size.y), Time.deltaTime * 5f);
                rushSprite.flipY = Mathf.Abs(angle) > 90;
                yield return null;
            }

            player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0f;

            for (float t = 0; t < 1.4f; t += Time.deltaTime)
            {
                direction = Vector3.Slerp(direction, new Vector3(targetPos.x - transform.position.x, targetPos.y - transform.position.y).normalized, Time.deltaTime * 0.25f);
                angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                rushSprite.transform.eulerAngles = new Vector3(0, 0, angle);
                rushSprite.color = new Color(laserSprite.color.r, laserSprite.color.g, laserSprite.color.b, (Mathf.Sin(2 * t * Mathf.PI)) / 3 + 0.3f);

                rushSprite.flipY = Mathf.Abs(angle) > 90;

                yield return null;
            }
        }

        rushSprite.DOFade(0, 0.3f);
        yield return new WaitForSeconds(0.3f);
        if (dead)
            yield break;

        rushSource.Play();

        if (health.currentHealth > 0)
            contactDamage.active = true;

        for (int i = 2; i < health.Colliders.Length; i++)
        {
            health.Colliders[i].enabled = false;
        }
        dashPs.Play();


        //dash
        if (!dashEscapeMove)
        {
            if (view.HasInputAuthority)
                transform.DOMove((Vector2)transform.position + RotateVector2(new Vector3(17, 0), rushSprite.transform.right), 1.5f);
            yield return new WaitForSeconds(1.5f);
        }
        //escape dash
        else
        {
            if (view.HasInputAuthority)
                transform.DOMove(targetPos, 8);
            yield return new WaitForSeconds(8f);

            foreach (SpriteRenderer sprite in eyeBloodSprites)
            {
                sprite.DOFade(1, 5);
            }

            ChooseTargetPos();
        }

        for (int i = 2; i < health.Colliders.Length; i++)
        {
            health.Colliders[i].enabled = true;
        }

        //pick new position after dashing when close
        if (currentSpeed < normalSpeed)
        {
            ChooseTargetPos();
        }

        dashPs.Stop();
        contactDamage.active = false;
        yield return new WaitForSeconds(0.3f);
        inRush = false;
        startRushMoveSpeedMult = 0;
        DOTween.To(() => startRushMoveSpeedMult, x => startRushMoveSpeedMult = x, 1, 0.5f);

        currentSpeed = normalSpeed;

    }


    public void StartBoss()
    {
        if (gameManager.inBoss) return;

        gameManager.inBoss = true;
        float dist = Vector2.Distance(transform.position, player.instance.transform.position);
        inRange = false;
        idleSource.volume = 0;
        idleSource.Play();
        idleSource.DOFade(1, 1);

        health.maxHealth += (playerSpawner.playerCountNotDead - 1) * 3300;
        health.currentHealth = health.maxHealth;

        DOTween.To(() => volume.weight, x => volume.weight = x, 0.7f, 4);

        DOTween.To(() => player.instance.camSizeMultiplier, x => player.instance.camSizeMultiplier = x, 1.1f, 4);

        targetPos = new Vector2(40, 0);
        lastTargetPos = transform.position;

        SetDifficultySetting();
    }

    public override void SetDifficultySetting()
    {
        health.maxHealth = Mathf.RoundToInt(health.maxHealth * ((float)difficulties[gameManager.instance.difficulty].health / 100f));
        health.currentHealth = health.maxHealth;

        normalSpeed = difficulties[gameManager.instance.difficulty].speed;
        currentSpeed = normalSpeed;

    }

    public override void Update()
    {

        if (cameraCutscene)
        {
            player.instance.camFollow.transform.position = cutsceneCamTarget.transform.position;
        }

        if (gameManager.inBoss && view.IsValid && !view.HasStateAuthority)
        {
            transform.localPosition = Vector2.Lerp(
                transform.localPosition,
                NetworkLocalPosition,
                interpolationSpeed * Time.deltaTime);
        }

        
        if (dead)
        {
            if (startedPhaseTwo) return;

            Collider2D[] playerCheck = Physics2D.OverlapBoxAll(insideEnterCheck.position, insideEnterCheck.localScale, 0, playerLayers);

            playerCountText.text = playerCheck.Length + "/" + playerSpawner.playerCountNotDead;

            if (SingletonRunner.runner.IsSharedModeMasterClient && playerCheck.Length >= playerSpawner.playerCountNotDead)
            {
                startedPhaseTwo = true;
                player.instance.inCutscene = true;
                RPC_EnterBossInside();
            }

            return;
        }

        if (!gameManager.inBoss || !player.instance || health.currentHealth <= 0)
        {
            if (inRange)
            {
                inRange = false;
                DOTween.To(() => volume.weight, x => volume.weight = x, 0, 4f);
                combatSource.DOKill();
                idleSource.DOKill();

                combatSource.DOFade(0, 1);
                idleSource.DOFade(0, 1);
            }

            return;
        }


        float dist = Vector2.Distance(transform.position, player.instance.transform.position);

        if (menu.instance.spectatingPlayer)
        {
            dist = Vector2.Distance(transform.position, menu.instance.spectatingPlayer.transform.position);
        }

        //music
        if (dist <= range && (!inRange || (idleSource.volume == 0 && combatSource.volume == 0)))
        {
            inRange = true;
            combatMusicCoroutine = StartCoroutine(_CombatMusic());

            DOTween.To(() => volume.weight, x => volume.weight = x, 1, 4.5f);
        }
        else if (dist >= chaseRange && inRange)
        {
            if (combatMusicCoroutine != null) StopCoroutine(combatMusicCoroutine);

            inRange = false;

            combatSource.DOKill();
            idleSource.DOKill();

            combatSource.DOFade(0, 1);
            idleSource.DOFade(1, 1);

            DOTween.To(() => volume.weight, x => volume.weight = x, 0.7f, 4.5f);
        }

        if (SingletonRunner.runner.IsSharedModeMasterClient)
        {
            if (!inRush)
            {
                transform.position = Vector2.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime * (iceSpeedMultiplier * 0.6f + 0.4f) * startRushMoveSpeedMult);
                if (Vector2.Distance(transform.position, targetPos) < 5)
                {
                    currentSpeed = normalSpeed * (Vector2.Distance(transform.position, targetPos) / 5);
                    if (currentSpeed < normalSpeed * 0.2f)
                    {
                        ChooseTargetPos();
                        DOTween.To(() => currentSpeed, x => currentSpeed = x, normalSpeed, 2);
                    }

                }
            }
            FindClosestPlayer();
            if (target != null)
            {
                shootTimer -= Time.deltaTime;

                if (shootTimer <= 0)
                {
                    shootTimer = Random.Range(attackDelay.x, attackDelay.y);

                    if (health.currentHealth < health.maxHealth / 2 && !doneEscapeMove)
                    {
                        Vector2 escapeTarget = new Vector2(Random.Range(-110, 110), Random.Range(-110, 110));

                        //chose target pos
                        while (Vector2.Distance(lastTargetPos, escapeTarget) < 120 || Vector2.Distance(lastTargetPos, escapeTarget) > 140)
                        {
                            escapeTarget = new Vector2(Random.Range(-110, 110), Random.Range(-110, 110));
                        }

                        RPC_Rush(DataPersistanceManager.instance.GetActorIndex(target.view.InputAuthority), true, escapeTarget);
                        doneEscapeMove = true;
                    }
                    else
                    {
                        if (Random.Range(0, 2) == 0)
                        {
                            RPC_ShootLaser(DataPersistanceManager.instance.GetActorIndex(target.view.InputAuthority));
                        }
                        else
                        {
                            RPC_Rush(DataPersistanceManager.instance.GetActorIndex(target.view.InputAuthority), false, Vector2.zero);
                        }

                    }

                }
            }


        }


    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            NetworkLocalPosition = transform.localPosition;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_EnterBossInside()
    {
        StartCoroutine(_EnterBossInside());
    }

    IEnumerator _EnterBossInside()
    {
        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0;

        startedPhaseTwo = true;
        playerCountText.DOFade(0, 1);

        menu.instance.OpenMap(false, true);
        yield return new WaitForSeconds(0.2f);
        player.instance.inCutscene = true;
        screenFade.DOFade(1, 1);
        yield return new WaitForSeconds(1.2f);

        bossInside.SetActive(true);
        BossInside.Instance.StartPhase();
        yield return new WaitForSeconds(0.1f);
        if (!playerHealth.instance.dead)
        {
            player.instance.transform.position = insidePlayerSpawnPoints[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)].position;
        }
        gameManager.inSecondPhase = true;
        insideVolume.enabled = true;
        menu.instance.mapGroup.alpha = 0;
        menu.instance.mapGroup.interactable = false;
        menu.instance.mapGroup.blocksRaycasts = false;

        gameManager.instance.boatsHolder.gameObject.SetActive(false);
        gameManager.instance.tileHolder.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.8f);
        screenFade.DOFade(0, 1);
        yield return new WaitForSeconds(0.5f);
        insideMusicSource.volume = 0;
        insideMusicSource.Play();
        insideMusicSource.DOFade(insideMusicSourceVol, 5);
        yield return new WaitForSeconds(1.5f);
        player.instance.inCutscene = false;

        if (Runner.IsSharedModeMasterClient)
        {
            foreach (Spawner s in spawners)
            {
                yield return new WaitForSeconds(3);
                StartCoroutine(s._SpawnEnemies());
            }

        }

    }

    public override void FindClosestPlayer()
    {
        float closestDistance = Mathf.Infinity;
        player closestPlayer = null;

        foreach (player p in playerSpawner.instance.players)
        {
            if (!p) continue;

            if (!p.health.dead && !p.currentBoat)
            {
                float distance2 = Vector2.Distance(transform.position, p.transform.position);
                if (distance2 < closestDistance)
                {
                    closestDistance = distance2;
                    this.distance = distance2;
                    if (closestPlayer != p)
                    {
                        closestPlayer = p;
                    }

                }
            }

        }
        if (closestDistance > range)
        {
            closestPlayer = null;
        }


        if ((closestDistance > range && target != null) || closestPlayer == null)
        {
            target = null;
        }
        else if (closestPlayer != null)
        {
            target = closestPlayer;
        }
    }

    IEnumerator _CombatMusic()
    {
        idleSource.DOFade(0, 0.5f);

        combatSource.clip = combatIntroClip;
        combatSource.Play();
        combatSource.volume = 0;
        combatSource.DOFade(1, 0.2f);
        combatSource.loop = false;

        while (combatSource.isPlaying)
        {
            yield return null;
        }

        combatSource.loop = true;
        combatSource.clip = combatMainClip;
        combatSource.Play();

    }

    public IEnumerator _Die()
    {
        if (dead) yield break;

        StopAllCoroutines();
        laserSprite.enabled = false;
        rushSprite.enabled = false;
        laserCollider.enabled = false;


        deadCollider.enabled = true;
        combatSource.DOFade(0, 1);
        idleSource.DOFade(0, 1);
        sprite.color = Color.white;

        menu.instance.uiGroup.DOFade(0, 1);
        player.instance.inCutscene = true;
        player.instance.camFollow.DOMove(transform.position, 1);
        dead = true;
        contactDamage.active = false;
        deadSource.Play();
        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 1.4f;
        yield return new WaitForSeconds(1.5f);
        DOTween.To(() => laserVol.weight, x => laserVol.weight = x, 0, 1);
        player.instance.camFollow.DOMove(transform.position, 1);

        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0;

        foreach (Animator anim in eyeAnims)
        {
            anim.SetTrigger("Dead");
            yield return new WaitForSeconds(0.15f);
        }
        mouthAnim.SetBool("Dead", true);

        yield return new WaitForSeconds(0.5f);

        playerCountText.gameObject.SetActive(true);
        playerCountText.color = new Color(1, 1, 1, 0);
        playerCountText.DOFade(1, 0.5f);

        yield return new WaitForSeconds(2.6f);
        player.instance.camFollow.DOLocalMove(Vector2.zero, 1);
        yield return new WaitForSeconds(1);
        contactDamage.active = false;
        player.instance.inCutscene = false;
        menu.instance.uiGroup.DOFade(1, 1);

        DOTween.To(() => volume.weight, x => volume.weight = x, 0, 1);

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(targetPos, 3);

        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(insideEnterCheck.position, insideEnterCheck.localScale);

        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(planeInsideCheck.position, planeInsideCheck.localScale);

    }

    public IEnumerator _PlaneInterior()
    {
        planeHeadObj.SetActive(true);
        DOTween.To(() => insideVolume.weight, x => insideVolume.weight = x, 0, 3);

        bool interacted = false;
        while (!interacted)
        {
            yield return null;

            Collider2D playerCheck = Physics2D.OverlapBox(planeInsideCheck.position, planeInsideCheck.localScale, 0, mainPlayerLayer);

            if (playerCheck != null)
            {
                inRangeOfSos = true;

                planeOutsideSprite.color = Color.Lerp(planeOutsideSprite.color, new Color(1, 1, 1, 0.2f), Time.deltaTime * 10);

                if (!indicatorInstance)
                    spawnIndicator();

                if (InputManager.actions["Interact"].started)
                {
                    RPC_EndCutscene();

                    interacted = true;
                }

            }
            else
            {
                inRangeOfSos = false;

                planeOutsideSprite.color = Color.Lerp(planeOutsideSprite.color, new Color(1, 1, 1, 1), Time.deltaTime * 10);

                destroyIndicator();
            }

        }

    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_EndCutscene()
    {
        StartCoroutine(_EndCutscene());
    }

    IEnumerator _EndCutscene()
    {
        GameData newData = DataPersistanceManager.instance.gameData;
        newData.completed = true;

        DataPersistanceManager.instance.DirectSave(newData);

        volume.enabled = false;
        player.instance.inCutscene = true;
        sosSource.clip = sosClip;
        sosSource.Play();
        yield return new WaitForSeconds(1.4f);
        screenFade.DOFade(1, 3);
        gameManager.instance.ambienceSource.DOFade(0, 2);
        yield return new WaitForSeconds(1f);
        sosSource.clip = heliClip;
        sosSource.Play();
        
        MouseCursor.state = 1;

        RPC_SetPlayer(DataPersistanceManager.LocalActorIndex, DataPersistanceManager.instance.localSkin, DataPersistanceManager.instance.localUsername, 
            treeRPCs.DictionaryToString(gameManager.endScreenStats));
        
        EnemySpawner.instance.disableDequeue = true;
        enemyHealth.deathQueue.Clear();

        yield return new WaitForSeconds(5.5f);

        //SingletonRunner.runner.LoadScene(SceneRef.FromIndex(3),new LoadSceneParameters(),true);
        SceneManager.LoadScene("ending");
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.ReliableLargeData)]
    void RPC_SetPlayer(int index, int skin, string username, string endScreenStats)
    {
        DataPersistanceManager.instance.endScreenUsernames[index] = username;
        DataPersistanceManager.instance.endScreenSkins[index] = skin;
        DataPersistanceManager.instance.endScreenStatsString[index] = endScreenStats;
    }

    public void spawnIndicator()
    {
        if (indicatorInstance == null)
        {
            indicatorInstance = Instantiate(indicatorPrefab, planeInsideCheck.position, Quaternion.identity);
        }
        else
        {
            indicatorAnim.SetTrigger("Hide");
            Destroy(indicatorInstance, 0.35f);

            indicatorInstance = Instantiate(indicatorPrefab, planeInsideCheck.position, Quaternion.identity);
        }
        indicatorAnim = indicatorInstance.GetComponent<Animator>();
        indicatorText = indicatorInstance.GetComponent<TextMeshPro>();
        indicatorText.text = "Call for help";
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

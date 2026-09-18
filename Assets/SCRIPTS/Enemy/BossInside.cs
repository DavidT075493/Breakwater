using DG.Tweening;
using Fusion;
using System.Collections;
using UnityEngine;

public class BossInside : NetworkBehaviour
{
    public static BossInside Instance;

    public Vector2 attackCooldown;
    float attackTimer;
    public Transform handHolder;
    public bool trigger;
    public damagePlayer[] handsDamage;
    NetworkObject view;
    public enemyHealth heartHealth;
    public SpriteRenderer heartMask;
    [SerializeField] AudioSource screamSound, deathSound;
    public bool dead;
    [SerializeField] ParticleSystem deathPs, deathPs2;
    [SerializeField] Transform[] afterBossSpawnPos;

    private void Awake()
    {
        Instance = this;
    }

    public void StartPhase()
    {
        attackTimer = 10;
        handHolder.gameObject.SetActive(false);
        view = GetComponent<NetworkObject>();

        BossHealthBar.Instance.bossNameText.text = "STONE HEART";

        heartHealth.maxHealth = 1200 + ((playerSpawner.playerCountNotDead - 1) * 1200);
        heartHealth.currentHealth = heartHealth.maxHealth;

        foreach (Spawner s in Boss.instance.spawners)
        {
            s.enabled = true;
        }

    }

    void Update()
    {
        if (!SingletonRunner.runner.IsSharedModeMasterClient || !MapDisplay.finishedLoading || Boss.instance.heartDead) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer < 0)
        {
            RPC_SpinnyHands(Random.Range(0,2) == 0 ? 1 : -1);
            attackTimer = Random.Range(attackCooldown.x, attackCooldown.y);
        }

        heartMask.sprite = heartHealth.sprite.sprite;
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_SpinnyHands(int dir)
    {
        StartCoroutine(_SpinnyHands(dir));
    }

    IEnumerator _SpinnyHands(int dir)
    {
        if (dead) yield break;

        StartCoroutine(_BossRoar());

        foreach (damagePlayer damage in handsDamage) damage.active = false;

        handHolder.gameObject.SetActive(true);
        handHolder.localScale = new Vector3(0.05f,0.05f);
        handHolder.DOScale(Vector2.one, 1.5f);

        yield return new WaitForSeconds(1.8f);

        float speed = 0;

        float speedMultiplier = 0.95f;

        if (gameManager.instance.difficulty > 0) speedMultiplier = 1.12f;

        foreach (damagePlayer damage in handsDamage) damage.active = true;

        for (float t = 0; t < 7; t += Time.deltaTime)
        {
            if (dead) yield break;

            handHolder.eulerAngles = new Vector3(0, 0, handHolder.eulerAngles.z + (speed * Time.deltaTime * speedMultiplier));

            speed += (8 - t) / 115 * dir * (Time.deltaTime * 240);
            yield return null;
        }

        for (float t = 0; t < 7; t += Time.deltaTime)
        {
            if (dead) yield break;

            handHolder.eulerAngles = new Vector3(0, 0, handHolder.eulerAngles.z + (speed * Time.deltaTime * speedMultiplier));

            speed -= (8 - t) / 115 * dir * (Time.deltaTime * 240);
            yield return null;
        }

        yield return new WaitForSeconds(1);
        handHolder.DOScale(Vector2.zero, 1);
        foreach (damagePlayer damage in handsDamage) damage.active = false;
        yield return new WaitForSeconds(1);
        handHolder.gameObject.SetActive(false);
    }

    IEnumerator _BossRoar()
    {
        if (Boss.instance.heartDead) yield break;

        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 1;
        screamSound.pitch = Random.Range(0.9f, 1.1f);
        screamSound.Play();
        yield return new WaitForSeconds(3);
        player.instance.cam.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0;

    }

    public IEnumerator _Die()
    {
        deathSound.Play();
        deathPs.Play();
        player.instance.inCutscene = true;
        Boss.instance.heartDead = true;
        Boss.instance.insideMusicSource.DOFade(0, 1.5f);

        foreach (Spawner s in Boss.instance.spawners)
        {
            s.disabled = true;
        }

        yield return new WaitForSeconds(1.5f);
        Boss.instance.fadeImage.color = Color.white;
        Boss.instance.screenFade.DOFade(1, 1.5f);
        yield return new WaitForSeconds(1.5f);
        deathPs.Stop();

        player.instance.transform.position = afterBossSpawnPos[DataPersistanceManager.instance.GetActorIndex(Runner.LocalPlayer)].position;
        yield return new WaitForSeconds(2f);
        Boss.instance.screenFade.DOFade(0, 1f);
        deathPs2.Play();
        yield return new WaitForSeconds(0.8f);
        Boss.instance.transform.GetChild(0).gameObject.SetActive(false);
        StartCoroutine(Boss.instance._PlaneInterior());
        menu.instance.mapGroup.DOFade(1, 1);
        gameManager.inSecondPhase = false;

        yield return new WaitForSeconds(1.5f);
        player.instance.inCutscene = false;
        yield return new WaitForSeconds(1);
        gameManager.instance._SwitchAmbience();

    }

}

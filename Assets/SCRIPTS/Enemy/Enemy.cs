using DG.Tweening;
using Fusion;
using Pathfinding;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
public class Enemy : NetworkBehaviour
{
    public DifficultySetting[] difficulties = new DifficultySetting[3];

    public float range = 5f;
    public float chaseRange = 10f;
    [HideInInspector]
    public player target;
    public float normalSpeed;
    public float afterAttackRecoverTime = 0.5f;
    public float afterAttackSpeed = 2;
    private AIDestinationSetter aiDestination;
    public AIPath ai;
    public SpriteRenderer sprite;
    public float waterCutoff = 0.625f;
    public Animator anim;
    public RuntimeAnimatorController[] animControllers;
    public damagePlayer damage;

    public enemyHealth health;
    public AudioSource angySound;
    public NetworkObject view;

    bool onCooldown;

    public Vector2Int currentIsland;
    public MapGenerator.biome currentBiome;
    [HideInInspector]
    public float distance;

    public float currentSpeed;
    public float iceSpeedMultiplier;

    public bool lookAtTarget;
    EnemyExplode explode;
    EnemyLeap leap;
    EnemyDash dash;

    //glass boy
    public SpriteMask mask;
    public Animation shineAnim;

    public GameObject waterRipple;
    public LayerMask playerObstaclesLayer;
    bool chasing;

    public EnemyHand enemyHand;
    bool inWater;

    public bool walkingToNearestLand;
    [HideInInspector]
    public int findTargetCounter;

    [Header("Projectile")]
    public Vector2 shootDelay;
    public GameObject[] projectiles;
    public float shootStartup;

    public float shootTimer;
    public Transform firePoint;

    Transform shootTarget;
    public float recoil = 5;

    public Animator eyeAnim;

    public bool tripleShot;
    public float tripleShotAngle = 19;

    public bool burstShot;

    public AudioSource laserSource;
    public SpriteRenderer laserSprite;
    public Sprite laser, warning;
    public Material laserPreviewMat, laserMat;
    public BoxCollider2D laserCollider;

    public AudioSource shootSound, shootSoundFast;
    public float shootStandStillTime = 0.5f;
    private bool currentlyShooting;
    public bool inDungeon;
    public int dungeonIndex = -1;
    public bool inSlothRange = false;
    public static int enemiesInSlothRange = 0;
    [HideInInspector]
    public float shootSoundPitch;

    public bool oceanEnemy;

    void Start()
    {
        explode = GetComponent<EnemyExplode>();
        leap = GetComponent<EnemyLeap>();
        dash = GetComponent<EnemyDash>();

        iceSpeedMultiplier = 1;

        if (ai)
        {
            aiDestination = GetComponent<AIDestinationSetter>();
            normalSpeed = ai.maxSpeed;
            currentSpeed = normalSpeed;
        }
        else
        {
            normalSpeed = currentSpeed;
        }

        if (shootSound)
        {
            shootSoundPitch = shootSound.pitch;
        }

        foreach (Island i in MapGenerator.instance.islands)
        {
            if (transform.position.x < (i.center.x + i.size / 2) && transform.position.x > (i.center.x - i.size / 2)
            && transform.position.y < (i.center.y + i.size / 2) && transform.position.y > (i.center.y - i.size / 2))
            {
                currentIsland = i.center;
                currentBiome = (MapGenerator.biome)i.biome;
                break;
            }
        }


        view = GetComponent<NetworkObject>();

        SetDifficultySetting();

        shootTimer = Random.Range(shootDelay.x, shootDelay.y);

        if (angySound) angySound.pitch += Random.Range(-0.1f, 0.1f);
        if (shootSound) shootSound.pitch += Random.Range(-0.05f, 0.05f);

        inDungeon = !gameManager.inSecondPhase && transform.position.y > DungeonGenerator.instance.transform.position.y;

        if (inDungeon || gameManager.inSecondPhase)
        {
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();

            foreach (SpriteRenderer sprite in sprites)
            {
                if (sprite.enabled && sprite.color.a > 0)
                {
                    float endAlpha = sprite.color.a;
                    sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0);
                    sprite.DOFade(endAlpha, 1);
                }

            }
        }
        else
        {
            if (animControllers.Length > (int)currentBiome && animControllers[(int)currentBiome] != null)
                anim.runtimeAnimatorController = animControllers[(int)currentBiome];
            else if (animControllers != null && animControllers.Length > 0)
                anim.runtimeAnimatorController = animControllers[0];

        }

    }

    public void SetDungeon(int dungeonIndex)
    {
        this.dungeonIndex = dungeonIndex;
        inDungeon = true;

        if (animControllers.Length > (int)currentBiome && animControllers[dungeonIndex] != null)
            anim.runtimeAnimatorController = animControllers[dungeonIndex];
        else if (animControllers != null && animControllers.Length > 0)
            anim.runtimeAnimatorController = animControllers[0];
    }

    public virtual void SetDifficultySetting()
    {
        range = difficulties[gameManager.instance.difficulty].range;
        chaseRange = difficulties[gameManager.instance.difficulty].chaseRange;

        health.maxHealth = difficulties[gameManager.instance.difficulty].health;
        health.currentHealth = health.maxHealth;
        health.knockbackScale = difficulties[gameManager.instance.difficulty].receiveKnockback;

        damage.damage = difficulties[gameManager.instance.difficulty].damage;

        ai.maxSpeed = difficulties[gameManager.instance.difficulty].speed;
        normalSpeed = ai.maxSpeed;
        currentSpeed = ai.maxSpeed;

        if (shineAnim && gameManager.instance.difficulty == 2)
        {
            shineAnim.clip = shineAnim.GetClip("shine 1");
        }
        if (leap)
        {
            leap.leapWaitTime = difficulties[gameManager.instance.difficulty].fireRate;
            leap.leapTime = difficulties[gameManager.instance.difficulty].attackTime;
        }
        if (dash)
        {
            dash.dashWaitTime = difficulties[gameManager.instance.difficulty].fireRate;
            dash.dashForce = difficulties[gameManager.instance.difficulty].attackTime;
        }

        shootDelay = difficulties[gameManager.instance.difficulty].fireRate + new Vector2(1.5f, 1.5f);
    }

    public virtual void SetActiveOwner(bool active)
    {
        if (!ai) return;

        if ((explode && explode.exploding) || (leap && leap.leaping) || (dash && dash.dashing) || (currentlyShooting))
        {
            ai.enabled = false;
            aiDestination.enabled = false;

            return;
        }
        ai.enabled = active;
        aiDestination.enabled = active;
    }

    public virtual void Update()
    {

        if (mask)
            mask.sprite = sprite.sprite;

        if (health.currentHealth <= 0)
        {
            return;
        }
        if (ai)
        {
            if (view.HasStateAuthority)
            {
                ai.maxSpeed = currentSpeed * iceSpeedMultiplier * DungeonGenerator.instance.GetModifier("enemy speed");
                if (inWater) ai.maxSpeed *= 0.7f;

                if (anim)
                    anim.SetFloat("Walk Speed", currentSpeed / normalSpeed * iceSpeedMultiplier);
                anim.SetBool("Walking", ai.enabled && ai.velocity.magnitude > 0);
            }

            if (aiDestination.target && lookAtTarget && !(leap && leap.leaping) && !(dash && dash.dashing))
            {
                if (aiDestination.target.position.x > transform.position.x)
                {
                    sprite.flipX = false;
                }

                if (aiDestination.target.position.x < transform.position.x)
                {
                    sprite.flipX = true;
                }
            }
        }

        if (!health.stunned && !health.frozen)
            SetActiveOwner(view.HasStateAuthority);
        //disable ai when stunned
        else if (ai) ai.enabled = false;

        if (!MapDisplay.finishedLoading) return;

        findTargetCounter++;
        if (findTargetCounter > 20)
        {
            FindClosestPlayer();
            findTargetCounter = 0;
        }

        if (target != null)
        {
            SetChasing(distance <= chaseRange && !target.health.dead
                && !(enemyHand && enemyHand.pullingPlayer));

            if (waterRipple)
                CheckWater();
        }
        else
        {
            SetChasing(false);
        }

    }

    public virtual void SetChasing(bool isChasing)
    {
        if (walkingToNearestLand) return;

        if (isChasing)
        {
            if (currentSpeed == 0) currentSpeed = normalSpeed;

            aiDestination.target = target.transform;

            if (!(explode && explode.exploding))
                health.rb.bodyType = RigidbodyType2D.Dynamic;
            else
                health.rb.bodyType = RigidbodyType2D.Kinematic;

            if (projectiles.Length > 0 && !(leap && leap.leaping) && HasStateAuthority)
            {
                shootTimer -= Time.deltaTime;

                if (shootTimer <= 0 && health.currentHealth > 0)
                {
                    shootTimer = Random.Range(shootDelay.x, shootDelay.y);
                    //laser or shoot
                    if (laserSprite)
                    {
                        if (Random.Range(0, 3) == 0)
                        {
                            RPC_Laser();
                        }
                        else if (HasLineOfSight(target))
                        {
                            RPC_Shoot();
                        }

                    }
                    //default shooting
                    else
                    {
                        if (HasLineOfSight(target))
                        {
                            RPC_Shoot();
                        }
                    }
                }
            }
        }
        else
        {
            aiDestination.target = null;
            currentSpeed = 0;
            health.rb.bodyType = RigidbodyType2D.Kinematic;
        }


    }

    public void CheckWater()
    {

        Vector3 tileOffset = new Vector2(-0.5f, -1f);


        TileBase groundTile = MapDisplay.Instance.groundTilemap.GetTile(Vector3Int.RoundToInt(transform.position + tileOffset));
        TileBase waterTile = MapDisplay.Instance.waterTilemap.GetTile(Vector3Int.RoundToInt(transform.position + tileOffset));

        if (waterTile != null) waterTile = MapDisplay.Instance.waterTilemap.GetTile(new Vector3Int(1, 0) +
            new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));
        if (waterTile != null) waterTile = MapDisplay.Instance.waterTilemap.GetTile(new Vector3Int(-1, 0) +
            new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));
        if (waterTile != null) waterTile = MapDisplay.Instance.waterTilemap.GetTile(new Vector3Int(0, 1) +
            new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));
        if (waterTile != null) waterTile = MapDisplay.Instance.waterTilemap.GetTile(new Vector3Int(0, -1) +
            new Vector3Int(Mathf.RoundToInt(transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y + tileOffset.y), 0));

        if (dungeonIndex > -1)
        {
            tileOffset = new Vector2(0, -0.5f);

            foreach (Tilemap t in DungeonGenerator.instance.dungeons[player.instance.inDungeon].groundTilemaps)
            {
                groundTile = t.GetTile(new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));
                if (groundTile != null) break;
            }
            foreach (Tilemap t in DungeonGenerator.instance.dungeons[player.instance.inDungeon].waterTilemaps)
            {
                waterTile = t.GetTile(new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));

                if (waterTile != null) waterTile = t.GetTile(new Vector3Int(1, 0) +
                    new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));
                if (waterTile != null) waterTile = t.GetTile(new Vector3Int(-1, 0) +
                    new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));
                if (waterTile != null) waterTile = t.GetTile(new Vector3Int(0, 1) +
                    new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));
                if (waterTile != null) waterTile = t.GetTile(new Vector3Int(0, -1) +
                    new Vector3Int(Mathf.RoundToInt(transform.position.x - t.transform.position.x + tileOffset.x), Mathf.RoundToInt(transform.position.y - t.transform.position.y + tileOffset.y), 0));

                if (waterTile != null) break;

            }
        }

        if (!gameManager.inSecondPhase && !health.dead && ((groundTile == null && !inDungeon) || waterTile != null)
            && gameManager.instance.placedTiles.Find(x => x.position == Vector2Int.RoundToInt(transform.position + tileOffset)
            && x.Item.floor).sprite == null
            && !(leap && leap.inAir))
        {
            waterRipple.SetActive(true);
            sprite.material.SetFloat("_Cutoff", waterCutoff);
            inWater = true;
        }
        else
        {
            waterRipple.SetActive(false);
            sprite.material.SetFloat("_Cutoff", 0);
            inWater = false;
        }

    }


    public virtual void FindClosestPlayer()
    {
        float closestDistance = Mathf.Infinity;
        player closestPlayer = null;

        bool onIslandWithPlayer = false;
        bool onIsland = false;

        foreach (player p in playerSpawner.instance.players)
        {
            if (!p) continue;

            if (!p.health.dead && !(p.currentBoat && (p.currentBoat.BoatType == Boat.boatType.speedboat 
                || ((p.currentBoat.BoatType == Boat.boatType.pirateShip || p.currentBoat.BoatType == Boat.boatType.ghost) && p.currentAltAction == 0 && p.inBoatAltAction))))
            {
                float distance = Vector2.Distance(transform.position, p.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    this.distance = distance;
                    if (closestPlayer != p)
                    {
                        closestPlayer = p;
                    }

                }
                if (inventory.charmEffect == "sloth" && p == player.instance)
                {
                    if (distance < HandMove.slothRange)
                    {
                        if (!inSlothRange)
                        {
                            print("true");

                            inSlothRange = true;
                            enemiesInSlothRange++;
                        }

                    }
                    else
                    {
                        if (inSlothRange)
                        {
                            print("false");

                            inSlothRange = false;
                            enemiesInSlothRange--;
                        }
                    }

                }

            }
            else
            {
                if (inSlothRange)
                {
                    print("false");

                    inSlothRange = false;
                    enemiesInSlothRange--;
                }
            }

            if (inventory.charmEffect != "sloth")
            {
                enemiesInSlothRange = 0;
                inSlothRange = false;
            }


            foreach (Island i in MapGenerator.instance.islands)
            {
                if (transform.position.x < (i.center.x + i.size / 2) && transform.position.x > (i.center.x - i.size / 2)
                && transform.position.y < (i.center.y + i.size / 2) && transform.position.y > (i.center.y - i.size / 2))
                {
                    currentIsland = i.center;
                    onIsland = true;
                }
            }

            if (currentIsland == p.currentIsland.center && p.onIsland && onIsland)
            {
                onIslandWithPlayer = true;
            }

        }

        //all enemies ignore range inside boss
        if ((closestDistance > range && !(gameManager.inSecondPhase || inDungeon))
            || (closestDistance <= range && !HasLineOfSight(closestPlayer) && !chasing))
        {
            closestPlayer = null;
            chasing = false;
        }

        //transfer ownership to current player
        if (closestPlayer == player.instance && view.StateAuthority != closestPlayer.view.StateAuthority
            && !(enemyHand && enemyHand.grabbing) && !(leap && leap.leaping) && !(dash && dash.dashing))
        {
            view.RequestStateAuthority();
            OnTransferAuthority();
        }

        //despawn when not on island with player
        if (!oceanEnemy && !onIslandWithPlayer && view.HasStateAuthority && !gameManager.inSecondPhase
            && !inDungeon)
        {
            Runner.Despawn(view);
        }

        if (closestPlayer == null)
        {
            target = null;
        }
        else if (closestPlayer != null)
        {
            if (target == null && angySound && closestDistance <= range && !angySound.isPlaying)
            {
                angySound.Play();
            }

            chasing = true;
            target = closestPlayer;
        }
    }

    public bool HasLineOfSight(player target)
    {
        if (target == null) return false;

        if (inDungeon || gameManager.inSecondPhase || target.currentBoat) return true;

        return Physics2D.Linecast(transform.position, target.transform.position - new Vector3(0, 0.3f, 0), playerObstaclesLayer).collider == target.collision;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Main Player") && !onCooldown && ai && !health.stunned && !health.frozen)
        {
            onCooldown = true;
            RPC_Recover();
        }

    }

    public virtual void OnTransferAuthority()
    {

    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_Recover()
    {
        StartCoroutine(_SlowDown());
    }

    IEnumerator _SlowDown()
    {
        currentSpeed = afterAttackSpeed;
        yield return new WaitForSeconds(afterAttackRecoverTime);

        currentSpeed = normalSpeed * 1.4f;
        yield return new WaitForSeconds(0.5f);
        currentSpeed = normalSpeed;

        onCooldown = false;
    }

    public IEnumerator _WalkToNearestLand()
    {
        GraphNode currentNode = AstarPath.active.GetNearest(transform.position).node;

        walkingToNearestLand = true;

        while (!currentNode.Walkable)
        {
            // Find the closest walkable node
            GraphNode nearestWalkable = FindNearestWalkableNode(transform.position);

            if (nearestWalkable != null)
            {
                // Move or pathfind to the nearest walkable point
                Vector3 targetPosition = (Vector3)nearestWalkable.position;
                ai.destination = targetPosition;
                ai.SearchPath(); // force recalculation
            }
            yield return null;
        }

        walkingToNearestLand = false;
    }

    private GraphNode FindNearestWalkableNode(Vector3 position)
    {
        NNConstraint constraint = NNConstraint.Default;
        constraint.constrainWalkability = true;
        constraint.walkable = true;

        NNInfo nearestWalkable = AstarPath.active.GetNearest(position, constraint);
        return nearestWalkable.node;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Shoot()
    {
        if (health.stunned || health.frozen) return;

        shootTimer = Random.Range(shootDelay.x, shootDelay.y);

        if (!eyeAnim)
            anim.SetTrigger("Shoot");
        else
            eyeAnim.SetTrigger("Shoot");

        if (target)
            shootTarget = target.transform;


        Invoke("SpawnBullet", shootStartup);

        if (ai) StartCoroutine(_ShootDisableAI());

    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Laser()
    {
        if (health.stunned || health.frozen || target == null) return;

        shootTimer = Random.Range(shootDelay.x, shootDelay.y) + 1.5f;

        StartCoroutine(_ShootLaser(DataPersistanceManager.instance.GetActorIndex(target.view.InputAuthority)));

    }

    IEnumerator _ShootDisableAI()
    {
        ai.enabled = false;
        currentlyShooting = true;
        yield return new WaitForSeconds(shootStandStillTime);
        ai.enabled = true;
        currentlyShooting = false;
    }

    void SpawnBullet()
    {
        if (health.stunned || health.frozen) return;

        if (health.currentHealth <= 0) return;

        Recoil();

        if (!view.HasStateAuthority) return;

        if (burstShot)
        {
            StartCoroutine(_BurstShot());
            return;
        }

        shootSound.pitch = shootSoundPitch + Random.Range(-0.1f, 0.1f);
        shootSound.Play();

        GameObject projectile = projectiles[0];
        if (projectiles.Length > 1)
        {
            projectile = !inDungeon ? projectiles[(int)currentBiome] : projectiles[dungeonIndex];
        }

        GameObject bullet = Runner.Spawn(projectile, firePoint.position, Quaternion.identity).gameObject;
        bullet.GetComponent<Projectile>().RPC_SetDirection(shootTarget.position, view.Id);

        if (tripleShot)
        {
            GameObject bullet2 = Runner.Spawn(projectile, firePoint.position, Quaternion.identity).gameObject;
            bullet2.GetComponent<Projectile>().RPC_SetDirection(shootTarget.position, view.Id, tripleShotAngle);

            GameObject bullet3 = Runner.Spawn(projectile, firePoint.position, Quaternion.identity).gameObject;
            bullet3.GetComponent<Projectile>().RPC_SetDirection(shootTarget.position, view.Id, -tripleShotAngle);
        }

    }

    IEnumerator _BurstShot()
    {
        int shots = 3;

        for (int i = 0; i < shots; i++)
        {
            shootSound.pitch = shootSoundPitch + Random.Range(-0.1f, 0.1f);
            shootSoundFast.pitch = shootSound.pitch;

            if (i < shots - 1)
                shootSoundFast.Play();
            else
                shootSound.Play();

            GameObject projectile = projectiles[0];
            if (projectiles.Length > 1)
            {
                projectile = !inDungeon ? projectiles[(int)currentBiome] : projectiles[dungeonIndex];
            }

            GameObject bullet = Runner.Spawn(projectile, firePoint.position, Quaternion.identity).gameObject;
            bullet.GetComponent<Projectile>().RPC_SetDirection(shootTarget.position
                + new Vector3(Random.Range(-1, 1), Random.Range(-2, 2)), view.Id);

            yield return new WaitForSeconds(0.16f);
        }

    }

    IEnumerator _ShootLaser(int targetPlayer)
    {
        int length = 16;

        Transform target = playerSpawner.instance.players[targetPlayer].transform;

        anim.SetBool("Laser", true);

        laserSource.pitch = 1.5f;

        laserSource.Play();

        Vector2 direction = new Vector3(target.position.x - transform.position.x, target.position.y - transform.position.y).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        laserSprite.transform.eulerAngles = new Vector3(0, 0, angle);
        laserSprite.enabled = true;
        laserSprite.sprite = warning;
        laserSprite.size = new Vector2(length, 0.66f);
        laserSprite.drawMode = SpriteDrawMode.Tiled;
        laserSprite.material = laserPreviewMat;

        laserSprite.color = new Color(1, 1, 1, 0);
        laserSprite.DOFade(0.3f, 0.2f);
        laserSprite.flipY = Mathf.Abs(angle) > 90;
        yield return new WaitForSeconds(0.2f);

        float degPerSec = 40 - Vector2.Distance(target.position, transform.position) * 5;

        if (gameManager.instance.difficulty > 0) degPerSec += 10;

        Recoil(1);

        for (float t = 0; t < 0.6f; t += Time.deltaTime)
        {

            Vector3 targetDirection = (target.position - transform.position).normalized;

            direction = Vector3.RotateTowards(
                direction,
                targetDirection,
                Mathf.Deg2Rad * degPerSec * Time.deltaTime, // 90°/sec
                0f
            );

            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            laserSprite.transform.eulerAngles = new Vector3(0, 0, angle);
            laserSprite.color = new Color(laserSprite.color.r, laserSprite.color.g, laserSprite.color.b, (Mathf.Sin(1.7f * t * Mathf.PI)) / 3 + 0.3f);

            laserSprite.flipY = Mathf.Abs(angle) > 90;

            yield return null;
        }

        if (health.dead)
        {
            laserCollider.gameObject.SetActive(false);
            laserSource.DOFade(0, 0.5f);
            print("cancel laser");
            yield break;
        }


        laserSprite.DOFade(0, 0.25f);
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            Vector3 targetDirection = (target.position - transform.position).normalized;
            direction = Vector3.RotateTowards(
                direction,
                targetDirection,
                Mathf.Deg2Rad * degPerSec * Time.deltaTime, // 90°/sec
                0f
            );
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            laserSprite.transform.eulerAngles = new Vector3(0, 0, angle);
            yield return null;
        }

        Recoil(1);

        laserSprite.material = laserMat;
        laserSprite.color = Color.white;
        laserSprite.sprite = laser;
        laserSprite.size = new Vector2(0, laserSprite.size.y);
        laserSprite.drawMode = SpriteDrawMode.Sliced;

        laserCollider.offset = new Vector2(laserSprite.size.x / 2f, 0); // Adjust for pivot (0, 0.5)
        laserCollider.size = laserSprite.size;
        laserCollider.enabled = true;

        int frameCounter = 0;
        for (float t = 0; t < 2; t += Time.deltaTime)
        {
            if (health.dead)
            {
                laserCollider.gameObject.SetActive(false);
                laserSource.DOFade(0, 0.5f);
                yield break;
            }

            Vector3 targetDirection = (target.position - transform.position).normalized;

            direction = Vector3.RotateTowards(
                direction,
                targetDirection,
                Mathf.Deg2Rad * degPerSec * Time.deltaTime, // 90°/sec
                0f
            );
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            laserSprite.transform.eulerAngles = new Vector3(0, 0, angle);


            if (t < 1f)
            {
                laserSprite.size = Vector2.MoveTowards(
                    laserSprite.size,
                    new Vector2(length, laserSprite.size.y),
                    (15 + (100 * t) + (Mathf.Pow(6, t * 4))) * Time.deltaTime
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
                laserSprite.size = Vector2.Lerp(laserSprite.size, new Vector2(length, 0), 6f * Time.deltaTime);
            }

            frameCounter++;
            yield return null;
        }

        anim.SetBool("Laser", false);
        laserSprite.enabled = false;
    }

    public virtual void Recoil(float mult = 1)
    {

    }

}

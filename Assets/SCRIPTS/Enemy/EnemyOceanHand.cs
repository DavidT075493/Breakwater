using Fusion;
using System.Collections;
using UnityEngine;

public class EnemyOceanHand : Enemy
{
    public float stoppingDistance;
    public float retreatDistance;
    public float agility;
    float velocity;
    public float speed;
    public Boat boatTarget;
    public LayerMask boatLayer;
    public Transform detectionPoint;
    public Boat grabbedBoat;
    public Animator[] eyeAnims;
    public player closestPlayer;
    private int currentShootingEye = 0;
    public AudioSource grabbedSound;
    bool deadReleased;
    private bool wokeEyes;
    bool onIsland = false;
    NetworkTransform netTransform;

    private void Awake()
    {
        shootTimer = Random.Range(shootDelay.x, shootDelay.y);
        netTransform = GetComponent<NetworkTransform>();

        foreach (Animator anim in eyeAnims)
        {
            anim.SetBool("Open", false);
        }
    }

    public override void SetDifficultySetting()
    {
        range = difficulties[gameManager.instance.difficulty].range;
        chaseRange = difficulties[gameManager.instance.difficulty].chaseRange;

        health.maxHealth = difficulties[gameManager.instance.difficulty].health;
        health.currentHealth = health.maxHealth;
        health.knockbackScale = difficulties[gameManager.instance.difficulty].receiveKnockback;

        shootDelay = difficulties[gameManager.instance.difficulty].fireRate;
        stoppingDistance = difficulties[gameManager.instance.difficulty].stopDist;
        retreatDistance = difficulties[gameManager.instance.difficulty].retreatDist;

        speed = difficulties[gameManager.instance.difficulty].speed;
        agility = difficulties[gameManager.instance.difficulty].agility;
    }


    public override void SetChasing(bool isChasing)
    {

        if (isChasing)
        {
            if (view.HasStateAuthority)
            {
                if (Vector2.Distance(transform.position, boatTarget.transform.position) > stoppingDistance)
                {
                    velocity = Mathf.MoveTowards(velocity, speed, agility * Time.deltaTime);
                }
                else if (Vector2.Distance(transform.position, boatTarget.transform.position) < stoppingDistance && Vector2.Distance(transform.position, boatTarget.transform.position) > retreatDistance)
                {
                    velocity = Mathf.MoveTowards(velocity, 0, agility * Time.deltaTime);
                }
                else if (Vector2.Distance(transform.position, boatTarget.transform.position) < retreatDistance)
                {
                    velocity = Mathf.MoveTowards(velocity, -speed, agility * Time.deltaTime);
                }

                if (grabbedBoat)
                {
                    shootTimer -= Time.deltaTime;

                    if (shootTimer <= 0 && health.currentHealth > 0)
                    {
                        shootTimer = Random.Range(shootDelay.x, shootDelay.y);

                        RPC_ShootEyes();
                    }
                }

                if (!eyeAnim)
                    anim.SetBool("Stunned", health.stunned);
                else
                    eyeAnim.SetBool("Stunned", health.stunned);

            }
            if (!grabbedBoat)
                health.rb.bodyType = RigidbodyType2D.Dynamic;
            else
                health.rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            health.rb.bodyType = RigidbodyType2D.Kinematic;
            anim.SetBool("Stunned", false);

            velocity = Mathf.MoveTowards(velocity, 0, agility * Time.deltaTime);
        }

    }

    IEnumerator _WakeEyes()
    {
        foreach (Animator anim in eyeAnims)
        {
            anim.SetBool("Open", true);
            yield return new WaitForSeconds(0.3f);
        }

    }

    public void FixedUpdate()
    {

        if (!grabbedBoat)
        {
            if (boatTarget && view.HasStateAuthority && health.currentHealth > 0 && !health.stunned && !health.frozen && !health.beingKnocked && velocity > 0)
            {
                health.rb.MovePosition(Vector2.MoveTowards(transform.position, boatTarget.transform.position, velocity * Time.fixedDeltaTime
                    * iceSpeedMultiplier * DungeonGenerator.instance.GetModifier("enemy speed")));

                anim.SetBool("Moving", true);
                anim.SetFloat("Speed", 0.3f + (velocity / speed * 0.7f));

            }
            else
            {
                anim.SetBool("Moving", false);
            }

            anim.SetBool("Grab", false);
            Collider2D boatCheck = Physics2D.OverlapCircle(detectionPoint.position, 1.5f, boatLayer);

            if (boatCheck)
            {
                shootTimer = 2;
                Boat b = boatCheck.GetComponentInParent<Boat>();
                if (b && b.view.HasStateAuthority && !b.skullAbilityActive && !b.grabbedByHand && b.BoatType != Boat.boatType.speedboat)
                {
                    RPC_Grab(b.view);
                }

            }

        }
        else
        {
            anim.SetBool("Grab", true);

            health.rb.MovePosition(Vector2.MoveTowards(transform.position, grabbedBoat.grabbedPoint.position, 7 * Time.fixedDeltaTime));

            float closestDistance = range;
            closestPlayer = null;
            foreach (player p in playerSpawner.instance.players)
            {
                if (!p) continue;

                if (!p.health.dead)
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

                }
            }

            if (health.dead && !deadReleased)
            {
                StartCoroutine(_ReleaseDead());
            }

        }
    }

    IEnumerator _ReleaseDead()
    {
        yield return new WaitForSeconds(1.5f);
        grabbedBoat.grabbedByHand = null;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_Grab(NetworkObject boatNet)
    {
        Boat b = boatNet.GetComponent<Boat>();

        grabbedSound.Play();

        grabbedBoat = b;
        b.Grabbed(this);

        netTransform.enabled = false;
        //GetComponent<SortingGroup>().sortingOrder = 11;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ShootEyes()
    {
        if (health.stunned || health.frozen) return;

        shootTimer = Random.Range(shootDelay.x, shootDelay.y);

        eyeAnims[currentShootingEye].SetTrigger("Shoot");

        Invoke("SpawnBullet", shootStartup);

    }

    void SpawnBullet()
    {
        if (health.stunned || health.frozen) return;

        if (health.dead) return;

        if (!view.HasStateAuthority) return;

        shootSound.pitch = shootSoundPitch + Random.Range(-0.1f, 0.1f);
        shootSound.Play();

        GameObject projectile = projectiles[0];

        Transform firePoint = eyeAnims[currentShootingEye].transform;

        if (projectiles.Length > 1)
        {
            projectile = !inDungeon ? projectiles[(int)currentBiome] : projectiles[dungeonIndex];
        }

        float dist = Mathf.Clamp(Vector2.Distance(closestPlayer.transform.position, transform.position) / 3, 0, 2);

        Vector3 randomOffset = new Vector3(Random.Range(-dist, dist), Random.Range(-dist, dist));

        GameObject bullet = Runner.Spawn(projectile, firePoint.position, Quaternion.identity).gameObject;
        bullet.GetComponent<Projectile>().RPC_SetDirection(closestPlayer.transform.position + randomOffset, view.Id);

        if (tripleShot)
        {
            GameObject bullet2 = Runner.Spawn(projectile, firePoint.position, Quaternion.identity).gameObject;
            bullet2.GetComponent<Projectile>().RPC_SetDirection(closestPlayer.transform.position + randomOffset, view.Id, tripleShotAngle);

            GameObject bullet3 = Runner.Spawn(projectile, firePoint.position, Quaternion.identity).gameObject;
            bullet3.GetComponent<Projectile>().RPC_SetDirection(closestPlayer.transform.position + randomOffset, view.Id, -tripleShotAngle);
        }

        currentShootingEye++;
        if (currentShootingEye >= eyeAnims.Length) currentShootingEye = 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(detectionPoint.position, 1.5f);
    }

    public override void Update()
    {

        if (health.currentHealth <= 0)
        {
            return;
        }

        if (!health.stunned && !health.frozen)
            SetActiveOwner(view.HasStateAuthority);

        if (!MapDisplay.finishedLoading) return;

        findTargetCounter++;
        if (findTargetCounter > 30)
        {
            FindClosestPlayer();
            findTargetCounter = 0;
        }

        if (boatTarget != null)
        {
            SetChasing(distance <= chaseRange);
        }
        else
        {
            SetChasing(false);
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    public override void Recoil(float mult = 1)
    {
        velocity = -recoil * mult;
    }

    public override void FindClosestPlayer()
    {
        float closestDistance = boatTarget == null ? range : chaseRange;
        Boat closestBoat = null;

        foreach (Transform b in gameManager.instance.boatsHolder)
        {
            if (!b || b.GetComponent<Boat>().BoatType == Boat.boatType.speedboat) continue;

            float distance = Vector2.Distance(transform.position, b.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                this.distance = distance;
                if (closestBoat != b)
                {
                    closestBoat = b.GetComponent<Boat>();
                }

            }

        }

        foreach (Island i in MapGenerator.instance.islands)
        {
            if (transform.position.x < (i.center.x + i.size / 2f) && transform.position.x > (i.center.x - i.size / 2f)
            && transform.position.y < (i.center.y + i.size / 2f) && transform.position.y > (i.center.y - i.size / 2f))
            {
                currentIsland = i.center;
                onIsland = true;
            }
        }


        if (onIsland && !health.dead)
        {
            health.dead = true;
            health.RPC_Die();
        }

        //transfer ownership to current player
        if ((player.instance.currentBoat && closestBoat == player.instance.currentBoat
            && player.instance.currentBoat.view.StateAuthority == SingletonRunner.runner.LocalPlayer) && view.StateAuthority != closestBoat.view.StateAuthority
            && !grabbedBoat)
        {
            view.RequestStateAuthority();
            OnTransferAuthority();
        }

        if (view.HasStateAuthority && gameManager.inBoss)
        {
            Runner.Despawn(view);
        }

        if (closestBoat == null)
        {
            boatTarget = null;
        }
        else if (closestBoat != null && !closestBoat.grabbedByHand)
        {
            if (boatTarget == null && angySound && closestDistance <= range && !wokeEyes)
            {
                angySound.Play();
                StartCoroutine(_WakeEyes());
                wokeEyes = true;
            }

            boatTarget = closestBoat;
        }
    }

}

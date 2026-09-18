using Fusion;
using UnityEngine;

public class EnemyProjectile : Enemy
{
    public float stoppingDistance;
    public float retreatDistance;
    public float agility;
    float velocity;
    public float speed;
    int laserCounter = 0;

    private void Awake()
    {
        shootTimer = Random.Range(shootDelay.x, shootDelay.y);
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
                if (Vector2.Distance(transform.position, target.transform.position) > stoppingDistance)
                {
                    velocity = Mathf.MoveTowards(velocity, speed, agility * Time.deltaTime);
                }
                else if (Vector2.Distance(transform.position, target.transform.position) < stoppingDistance && Vector2.Distance(transform.position, target.transform.position) > retreatDistance)
                {
                    velocity = Mathf.MoveTowards(velocity, 0, agility * Time.deltaTime);
                }
                else if (Vector2.Distance(transform.position, target.transform.position) < retreatDistance)
                {
                    velocity = Mathf.MoveTowards(velocity, -speed, agility * Time.deltaTime);
                }

                shootTimer -= Time.deltaTime;

                if (shootTimer <= 0 && health.currentHealth > 0)
                {
                    shootTimer = Random.Range(shootDelay.x, shootDelay.y);
                    //laser or shoot
                    if (laserSprite)
                    {
                        if (Random.Range(0, 3) == 0 || laserCounter > 2)
                        {
                            RPC_Laser();
                            laserCounter = 0;
                        }
                        else if (HasLineOfSight(target))
                        {
                            laserCounter++;
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

                if (!eyeAnim)
                    anim.SetBool("Stunned", health.stunned);
                else
                    eyeAnim.SetBool("Stunned", health.stunned);

            }
            health.rb.bodyType = RigidbodyType2D.Dynamic;
        }
        else
        {
            health.rb.bodyType = RigidbodyType2D.Kinematic;
            anim.SetBool("Stunned", false);

            velocity = Mathf.MoveTowards(velocity, 0, agility * Time.deltaTime);
        }

    }

    public void FixedUpdate()
    {
        if (target && view.HasStateAuthority && health.currentHealth > 0 && !health.stunned && !health.frozen && !health.beingKnocked)
            health.rb.MovePosition(Vector2.MoveTowards(transform.position, target.transform.position, velocity * Time.fixedDeltaTime 
                * iceSpeedMultiplier * DungeonGenerator.instance.GetModifier("enemy speed")));
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

}

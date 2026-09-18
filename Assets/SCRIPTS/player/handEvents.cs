using Fusion;
using System.Collections;
using UnityEngine;

public class handEvents : NetworkBehaviour
{
    public static handEvents instance;

    public GameObject[] hitboxDictionary;
    public NetworkObject view;
    public HandMove handMove;

    bool inBoat;
    AudioSource source;
    public AudioClip oneHandSound, twoHandSound, cleaveSound;
    public AudioSource eatSound;
    public const float eatSoundPitch = 0.95f;

    public LineRenderer grappleLine;
    [SerializeField] float grappleSpeed = 10, grappleCastSpeed = 30;

    public bool grappling;
    public LayerMask grappleLayers;

    public Transform grapplePoint;
    [SerializeField] float grappleCamSize = 7;
    Vector2 targetPos;

    [SerializeField]
    float grappleDist = 9.5f;

    [SerializeField] AudioSource castSound, grappleSound;

    int grappleNetworkState = 0;

    [SerializeField] GameObject grappleExplosion;

    public bool grappleImmunity;

    float grapplePointObjSpeed;
    Vector2 lastPos;
    private bool grappleCanceled;

    public bool hitboxEnabled;
    private bool shieldBashing;

    float grappleTime = 0;
    public bool grappleCast;

    public SpriteRenderer grapplePointSprite;

    [Networked] public Vector2 GrappleNetworkLocalPosition { get; set; }

    [SerializeField] float grappleNetInterpolationSpeed = 15f;


    private void Start()
    {
        if (view.HasInputAuthority)
            instance = this;

        grapplePointSprite.enabled = false;

        source = GetComponent<AudioSource>();

        if (view.HasInputAuthority)
        {
            foreach (GameObject hitbox in hitboxDictionary)
            {
                hitbox.tag = "Weapon Hitbox";
                hitbox.layer = 7;
                hitbox.SetActive(false);
            }
        }
        else
        {
            deactivateHitbox();
        }
    }

    void Trail(float time)
    {
        if (handMove.selectedItem != null && handMove.selectedItem.type == item.itemType.tool)
        {
            handMove.swingTrail.transform.localPosition = handMove.playerParent.heldItemSprite.transform.localPosition / 2.5f;

            handMove.swingTrail.material.color = handMove.selectedItem.trailColor;
            if (handMove.swingTrail.material.color == Color.white)
                handMove.swingTrail.material.color = new Color(1, 1, 1, 0.55f);

            handMove.swingTrail.emitting = true;

            handMove.swingTrail.time = 0.25f;
            StartCoroutine(_StopTrail(time));
        }
    }

    IEnumerator _StopTrail(float time)
    {
        yield return new WaitForSeconds(time - 0.1f);
        float timeStart = handMove.swingTrail.time;
        float fadeTime = 0.12f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            handMove.swingTrail.time = timeStart - (t * (1 / fadeTime) * timeStart);
            yield return null;
        }
        handMove.swingTrail.time = 0;
        handMove.swingTrail.emitting = false;
    }

    void CanScroll()
    {
        if (view.HasInputAuthority)
            handMove.canScroll = true;

        shieldBashing = false;
    }
    void CantScroll()
    {
        if (view.HasInputAuthority)
            HandMove.instance.canScroll = false;
    }

    void CanFlipHand()
    {
        if (view.HasInputAuthority)
            handMove.handCanFlip = true;
    }
    void CantFlipHand()
    {
        if (view.HasInputAuthority)
            handMove.handCanFlip = false;
    }

    void activateHitbox()
    {
        if (view.HasStateAuthority)
        {
            MouseCursor.instance.anim.SetTrigger("Spin");
        }
        item currentItem = inventorySlot.equippedSlot.itemInSlot;

        if (!view.HasInputAuthority)
        {
            currentItem = handMove.selectedItem;
        }

        deactivateHitbox();

        if (currentItem)
        {
            hitboxDictionary[currentItem.hitboxIndex].SetActive(true);
        }
        else
        {
            hitboxDictionary[0].SetActive(true);
        }

        hitboxEnabled = true;
    }


    void PlayAudio()
    {
        source.pitch = Random.Range(0.91f, 1.03f);

        if (handMove.selectedItem)
        {
            if (handMove.selectedItem.twoHanded)
            {
                source.clip = twoHandSound;
            }
            else
            {
                source.clip = oneHandSound;
            }

            //override sound
            if (handMove.selectedItem.overrideSwingSound)
            {
                source.clip = handMove.selectedItem.overrideSwingSound;
            }
        }
        else
        {
            source.clip = oneHandSound;
            source.pitch = Random.Range(0.95f, 1.06f);
        }

        source.Play();
    }

    void CleaveAudio()
    {

        source.clip = cleaveSound;
        source.pitch = Random.Range(0.95f, 1.06f);

        source.Play();
    }

    void EatSound()
    {
        if (!handMove.eating) return;

        if (eatSound.pitch == eatSoundPitch)
        {
            eatSound.pitch = 0.78f;
        }
        else
        {
            eatSound.pitch = eatSoundPitch;
        }
        eatSound.Play();
    }

    private void Update()
    {
        if (handMove.playerParent.pilotingBoat && !inBoat)
        {
            inBoat = true;
            deactivateHitbox();
        }
        else if (!handMove.playerParent.pilotingBoat && inBoat)
        {
            inBoat = false;
        }

        if (grappling && grappleTime > 0.15f && InputManager.actions["Item Primary"].started
            && !inventory.instance.inventoryOpen
            && !pauseScreen.instance.paused
            && !Chat.Instance.chatOpen)
        {
            grappleCanceled = true;
        }

        if (!handMove.selectedItem || handMove.selectedItem.uniqueType != item.UniqueType.shield) shieldBashing = false;

        hitboxDictionary[6].SetActive(handMove.animator.GetBool("Shielding") && !shieldBashing);

        if (!view.HasStateAuthority)
        {
            grapplePoint.position = Vector2.Lerp(
                grapplePoint.position,
                GrappleNetworkLocalPosition,
                grappleNetInterpolationSpeed * Time.deltaTime);
        }
    }

    void SetShieldBashing()
    {
        shieldBashing = true;
    }
    void SetShieldBashingFalse()
    {
        shieldBashing = false;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_DeactivateHitboxes()
    {
        deactivateHitbox();
    }

    public void deactivateHitbox()
    {
        foreach (GameObject hitbox in hitboxDictionary)
        {
            hitbox.SetActive(false);
        }
        hitboxEnabled = false;

        if (view.HasStateAuthority)
        {
            handMove.FinishSwing();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            GrappleNetworkLocalPosition = grapplePoint.position;
        }
    }

    void CastSound()
    {
        castSound.Play();
    }

    void PrepareGrapple()
    {
        grapplePoint.SetParent(transform);
        grapplePoint.position = grappleLine.transform.position;
    }

    public IEnumerator _StartGrapple()
    {
        if (!view.HasInputAuthority || player.instance.currentBoat || menu.instance.waitingForReady)
        {
            handMove.inGrappleAnim = false;
            handMove.grappleCooldown = false;
            yield break;
        }
        handMove.animator.SetBool("Grappling", true);
        grappleLine.enabled = true;

        grapplePoint.SetParent(null);
        grapplePoint.position = grappleLine.transform.position;
        grapplePoint.rotation = Quaternion.identity;

        targetPos = grapplePoint.position + grappleDist * transform.parent.right;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = grappleLayers;
        filter.useTriggers = true;

        RaycastHit2D[] ray = new RaycastHit2D[1];

        RPC_Cast(targetPos);

        grappleCast = true;
        //cast
        bool grappleCancelAbility = false;
        float castTime = 0;
        while (true)
        {
            DrawGrappleLine(false);

            // Move the grapplePoint toward the target
            grapplePoint.position = Vector2.MoveTowards(grapplePoint.position, targetPos, grappleCastSpeed * Time.deltaTime);

            yield return null;

            // Check collision or target reached
            Physics2D.CircleCast(grapplePoint.position, 0.4f, Vector2.zero, filter, ray, 0.3f);

            if (ray[0].collider)
            {
                break;
            }
            if (inventorySlot.equippedSlot.itemData.tier == 4
                && InputManager.actions["Item Primary"].started && !playerWater.instance.outOfStamina)
            {
                grappleCancelAbility = true;
            }
            if(grappleCancelAbility && castTime > 0.12f)
            {
                playerWater.instance.stamina -= 8;
                break;
            }

            if (Vector2.Distance(grapplePoint.position, targetPos) < 0.1f) break;

            if (player.instance.currentBoat) break;

            castTime += Time.deltaTime;
        }

        if (ray[0].collider || grappleCancelAbility)
        {
            grappleCast = false;
            grappleCanceled = false;

            Vector2 grappleToPos = grapplePoint.position;
            handMove.playerParent.anim.SetBool("Grappling", true);

            NetworkId targetId = default;

            if (ray[0].collider)
            {
                NetworkObject targetView = ray[0].collider.GetComponentInParent<NetworkObject>();
                if(!targetView) targetView = ray[0].collider.GetComponent<NetworkObject>();

                if (targetView)
                {
                    targetId = targetView.Id;

                    // The exact collision/intersection point
                    Vector2 hitPoint = ray[0].point;

                    // Convert the intersection point into the target's local space
                    grappleToPos = targetView.transform.InverseTransformPoint(hitPoint);

                    // Attach exactly at the intersection point
                    grapplePoint.SetParent(targetView.transform);
                    grapplePoint.localPosition = grappleToPos;
                }
            }

            RPC_GrappleLand(
                targetId,
                grappleToPos,
                (Vector2)grapplePoint.position,
                grappleCancelAbility
            );
            //pull player
            handMove.playerParent.anim.SetBool("Walking", false);

            playerWater.instance.usingStamina = true;
            grappling = true;
            grappleTime = 0;

            grappleImmunity = true;

            bool isEnemy = ray[0].collider && 
                (ray[0].collider.GetComponent<Enemy>() != null || ray[0].collider.GetComponentInParent<Enemy>() || ray[0].collider.GetComponentInChildren<Enemy>());

            while (true)
            {
                player.instance.cam.m_Lens.OrthographicSize = Mathf.Lerp(player.instance.cam.m_Lens.OrthographicSize, grappleCamSize * player.instance.camSizeMultiplier, Time.deltaTime * 8);

                DrawGrappleLine(true);

                yield return new WaitForFixedUpdate();

                // Move player toward the target
                player.instance.rb.MovePosition(Vector2.MoveTowards(player.instance.transform.position, grapplePoint.position, grappleSpeed * Time.fixedDeltaTime));

                if (Vector2.Distance(player.instance.transform.position, grapplePoint.position) < 0.7f && grappleTime > 0.05f)
                {
                    break;
                }

                if (isEnemy && grappleTime > 3)
                {
                    break;
                }

                if (grappleCanceled)
                {
                    grappleCanceled = false;
                    break;
                }

                //stop if not moving
                if ((handMove.playerParent.objSpeed < 0.06f) && grappleTime > 0.15f)
                {
                    break;
                }

                grappleTime += Time.fixedDeltaTime;

                if (player.instance.currentBoat) break;
            }

            if (isEnemy)
            {
                Instantiate(grappleExplosion, transform.position, Quaternion.identity);
            }

            handMove.animator.SetBool("Grappling", false);
            handMove.playerParent.anim.SetBool("Grappling", false);
            CanScroll();

            yield return StartCoroutine(_CancelGrapple());
        }
        else
        {
            //pull back on fail
            RPC_Reel();

            while (true)
            {
                DrawGrappleLine(true);

                grapplePoint.position = Vector2.MoveTowards(grapplePoint.position, grappleLine.transform.position, grappleCastSpeed * Time.deltaTime * 2);

                yield return null;

                if (Vector2.Distance(grapplePoint.position, grappleLine.transform.position) < 0.2f)
                {
                    break;
                }

                if (player.instance.currentBoat) break;
            }

            grappleCast = false;
        }
        RPC_GrappleFinish();
        grapplePoint.SetParent(null);
        handMove.animator.SetBool("Grappling", false);
        handMove.playerParent.anim.SetBool("Grappling", false);
        grappleLine.enabled = false;
        handMove.inGrappleAnim = false;
        StartCoroutine(_GrappleCooldown());
    }

    IEnumerator _CancelGrapple()
    {
        HandMove.instance.ChangeDurability(-1, inventorySlot.equippedSlot);

        RPC_Reel();

        while (true)
        {
            DrawGrappleLine(true);

            grapplePoint.position = Vector2.MoveTowards(grapplePoint.position, grappleLine.transform.position, grappleCastSpeed * Time.deltaTime * 3);

            yield return null;

            if (Vector2.Distance(grapplePoint.position, grappleLine.transform.position) < 0.2f)
            {
                break;
            }

        }

        CanScroll();

        RPC_GrappleFinish();
        grappling = false;

        Invoke("DisableImmunity", 0.3f);
    }

    public void CancelGrapple()
    {
        grappling = false;
        RPC_GrappleFinish();
        grapplePoint.SetParent(null);
        handMove.animator.SetBool("Grappling", false);
        handMove.playerParent.anim.SetBool("Grappling", false);
        grappleLine.enabled = false;
        handMove.inGrappleAnim = false;
        
        if(gameObject.activeInHierarchy)
        StartCoroutine(_GrappleCooldown());
    }

    IEnumerator _GrappleCooldown()
    {
        handMove.grappleCooldown = true;
        yield return new WaitForSeconds(0.2f);
        handMove.grappleCooldown = false;
    }

    void DisableImmunity()
    {
        grappleImmunity = false;
    }

    void DrawGrappleLine(bool straight)
    {
        if (!straight)
        {
            int segments = 20;
            grappleLine.positionCount = segments + 1;

            Vector3 start = grappleLine.transform.position;
            Vector3 end = grapplePoint.position;
            float distanceToTarget = Vector3.Distance(grapplePoint.position, targetPos);

            // Controls wave height — fades as the point approaches the target
            float waveStrength = Mathf.Clamp01(Mathf.Pow(distanceToTarget, 2) / Mathf.Pow(grappleDist, 2)) * 1.3f;

            // Generate wavy line between start and end
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 point = Vector3.Lerp(start, end, t);

                float sine = -Mathf.Sin(t * Mathf.PI * 6);
                float offsetAmount = sine * 0.5f * waveStrength;

                Vector3 direction = (end - start).normalized;
                Vector3 normal = new Vector3(-direction.y, direction.x, 0);

                point += normal * offsetAmount;
                grappleLine.SetPosition(i, point);
            }

        }
        else
        {
            grappleLine.positionCount = 2;

            grappleLine.SetPosition(1, grappleLine.transform.position);
            grappleLine.SetPosition(0, grapplePoint.position);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_Cast(Vector2 targetPos)
    {
        if (view.HasInputAuthority) return;
        grapplePoint.SetParent(null);
        StartCoroutine(_Cast(targetPos));
    }

    IEnumerator _Cast(Vector2 targetPos)
    {
        this.targetPos = targetPos;

        grapplePoint.transform.SetParent(null);

        grapplePoint.position = grappleLine.transform.position;
        grappleLine.enabled = true;
        grappleNetworkState = 0;

        while (grappleNetworkState == 0)
        {
            DrawGrappleLine(false);

            yield return null;

        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_GrappleLand(
    NetworkId targetId,
    Vector2 localPos,
    Vector2 worldPos,
    bool cancelAbility)
    {
        StartCoroutine(_GrappleLand(targetId, localPos, worldPos, cancelAbility));
    }

    IEnumerator _GrappleLand(
    NetworkId targetId,
    Vector2 localPos,
    Vector2 worldPos,
    bool cancelAbility)
    {
        if (cancelAbility)
            grapplePointSprite.enabled = true;

        grapplePoint.localScale = Vector2.one;
        grapplePoint.rotation = Quaternion.identity;

        if (targetId.IsValid)
        {
            NetworkObject target = Runner.FindObject(targetId);

            if (target != null)
            {
                grapplePoint.SetParent(target.transform);
                grapplePoint.localPosition = localPos;
            }
        }
        else
        {
            grapplePoint.SetParent(null);
            grapplePoint.position = worldPos;
        }

        grappleLine.positionCount = 2;
        grappleNetworkState = 1;
        grappleSound.pitch = Random.Range(0.96f, 1.03f);
        grappleSound.Play();

        grappling = true;

        while (grappleNetworkState == 1)
        {
            DrawGrappleLine(true);
            yield return null;
        }

        grapplePointSprite.enabled = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_Reel()
    {
        if (view.HasInputAuthority) return;
        StartCoroutine(_Reel());
    }

    IEnumerator _Reel()
    {
        grappleLine.positionCount = 2;

        grappleNetworkState = 2;

        while (grappleNetworkState == 2)
        {
            DrawGrappleLine(true);

            yield return null;

        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_GrappleFinish()
    {
        grapplePointSprite.enabled = false;
        grappleNetworkState = 3;
        grappleLine.enabled = false;
        grappling = false;
        grapplePoint.transform.SetParent(null);
    }

    private void FixedUpdate()
    {
        grapplePointObjSpeed = ((Vector2)transform.localPosition - lastPos).magnitude / Time.fixedDeltaTime;

        lastPos = transform.position;
    }

}

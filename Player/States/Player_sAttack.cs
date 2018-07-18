using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/********************************************************************* 
 * By Alex
 * Attack state of the Player script. See Player_sState for more details
*********************************************************************/

public class Player_sAttack : Player_sBase
{
    public int numberOfHits = 0;

    public bool brokenCombo = false;
    public bool canAttack = false;
    public bool charging = false;
    public bool attacked = false;

  
    bool aerialAttack = false;
    bool downAttack = false;
    bool beginCharge = false;
    bool swiping = false;

    int jumpAttackPos; // position in coroutines list
    List<Coroutine> coroutines = new List<Coroutine>();
    HashSet<Enemy> hitEnnemies = new HashSet<Enemy>();
    public ContactFilter2D contactFilter;

    BoxCollider2D sliceAttackCollider { set; get; }

    //Debug related
    public bool haveBeenReset = true;
    float toIdleTimer = 0;

    /// <summary>
    /// Calls Standard behaviour before switching to next state. Pass state as a delegate action
    /// </summary>
    /// <param name="delegatedFonction"></param>
    void ToNextState(DelegatedFonction delegatedFonction)
    {
        toIdleTimer = 0;
        delegatedFonction();
        coroutines.ForEach(n => master.StopCoroutine(n));
        coroutines = new List<Coroutine>();
        Reset();
    }

    public override void ToIdle()
    {
        base.ToIdle();
        toIdleTimer = 0f;
        master.jumpState.attacked = false;
        master.playerAnimationController.ApplyRootMotion();
        Reset();
    }

    public override void Update() {
        toIdleTimer += Time.deltaTime;

        if (toIdleTimer > 0.5f && canAttack == false && !downAttack)
            ToNextState(() => ToIdle());

        if (Mathf.Sign(master.PlayerInput.x) == Mathf.Sign(master.Direction))
            master.playerAnimationController.SetHSpeed(Mathf.Abs(master.PlayerInput.x));

        else
            master.playerAnimationController.SetHSpeed(0f);

        if (downAttack)
        {
            SliceAttack();
        }

        if (!attacked)
        {
            if (Mathf.Sign(master.PlayerInput.x) != master.Direction && master.PlayerInput.x != 0)
                master.Turn();

            haveBeenReset = false;
            canAttack = false;
            AttackCheck();
        }

        else if (!downAttack)
        {
            if (master.controller.collisions.below)
            {
                ToNextState(() => ToIdle());
            }
            else
                ToNextState(() => ToJump());
        }

    }

    public override void Move(ref Vector3 velocity)
    {
        master.controller.VerticalCollisions(ref velocity);
        master.controller.HorizontalCollisions(ref velocity);

        if (downAttack && velocity.y > master.downAttackBaseVelocity * Time.deltaTime)
            velocity.y = master.downAttackBaseVelocity * Time.deltaTime;

        if(downAttack && !swiping)
        {
            TopCheck();
        }

        if (!downAttack)
        {
            if (brokenCombo)
            {
                if (!master.controller.collisions.below)
                    AjustAirMove(velocity);
                return;
            }

            if (master.controller.collisions.right || master.controller.collisions.left)
            {
                master.playerAnimationController.SetHSpeed(0f);
            }

            if (master.controller.collisions.below)
            {
                if (CheckForEnnemies())
                {
                    master.playerAnimationController.SetHSpeed(0f);
                }
            }
        }
        
        else if (!master.controller.collisions.below && !downAttack)
            ToNextState(() => ToJump());

        else if (master.controller.collisions.below && downAttack)
        {
            ToNextState(() => ToIdle());
            if (master.upgrades.attackAoE)
            {
                SoundManager.PlayInAvatar(SoundManager.SoundName.meleeKirbySlashImpact);
                AttackResolution(Player.AttackType.largeAoE);
                Tool_FXPooling.Instance.SpawnFX("AttackLand2", master.tr.position, Quaternion.identity);
            }
            else
            {
                SoundManager.PlayInAvatar(SoundManager.SoundName.meleeKirbySlashImpact);
                Tool_FXPooling.Instance.SpawnFX("AttackLand1", master.tr.position, Quaternion.identity);
            }

            MonoBehaviour.Destroy(sliceAttackCollider.gameObject);
            sliceAttackCollider = null;

            swiping = false;
        }

        else if (master.controller.collisions.below && aerialAttack)
        {
            ToNextState(() => ToIdle());
        }


        if(!master.controller.collisions.below)
            master.transform.Translate(velocity, Space.World);
    }

    public override void PlatformMove(Vector3 velocity)
    {
        if(master.controller.collisions.below)
            base.PlatformMove(velocity);
    }

    public override void ButtonDownA() {
        if (master.controller.collisions.below && canAttack){
            ToNextState(() => ToJump());
            master.Jump();
            master.playerAnimationController.IsJumping();
        }
    }

    public override void ButtonDownX() {
        if (numberOfHits < 3 && canAttack && master.controller.collisions.below)
        {
            if (Mathf.Sign(master.PlayerInput.x) != master.Direction && master.PlayerInput.x != 0)
                master.Turn();
            
            master.TimeToRegen = 0;
            toIdleTimer = 0;
            AttackCheck();
            if (numberOfHits == 3)
            {
                Vector3 fxPos = master.tr.position;
                fxPos.y += 1.3f;
                fxPos.x += 1f * master.Direction;
                Tool_FXPooling.Instance.SpawnFX("Hit3rd", master.playerAnimationController.leftHand.transform.position, Quaternion.identity, master.playerAnimationController.leftHand);
            }
            canAttack = false;
        }
    }

    public override void TriggerDownR() {
        if (numberOfHits < 3 && master.controller.collisions.below && !master.controller.collisions.left && !master.controller.collisions.right && master.CurrentGravityZone != Player.PlayerMode.light){
            ToNextState(() => ToDash());

            if (master.upgrades.dashDamage)
                master.playerAnimationController.DashAttack = true;

            else
                master.playerAnimationController.Dash = true;
        }
    }

    public void Reset()
    {
        hitEnnemies = new HashSet<Enemy>();
        aerialAttack = false;
        downAttack = false;
        canAttack = false;
        beginCharge = false;
        charging = false;
        attacked = false;
        numberOfHits = 0;

        haveBeenReset = true;
    }

    private void AjustAirMove(Vector3 velocity)
    {
            master.transform.Translate(velocity, Space.World);
    }

    private void AttackCheck()
    {
        master.mana -= master.attackManaCost;
        Man_GameManager.Instance.hud.UpdateManaUICurved(master.mana);

        attacked = true;
        if (!master.controller.collisions.below)
        {
            AerialAttack();
        }
        
        else
            Attack();

        switch (numberOfHits)
        {
            case 0:
                SoundManager.PlayInAvatar(SoundManager.SoundName.meleeSwoosh01);
                break;
            case 1:
                SoundManager.PlayInAvatar(SoundManager.SoundName.meleeSwoosh02);
                break;
            case 2:
                SoundManager.PlayInAvatar(SoundManager.SoundName.meleeSwoosh03);
                break;
            default:
                SoundManager.PlayInAvatar(SoundManager.SoundName.meleeSwoosh01);
                break;
        }
        numberOfHits++;

    }

    void AerialAttack()
    {
        aerialAttack = true;
        if (master.PlayerInput.y > 0.5f)
            SetAttackAnimationTrigger(Player.AttackType.airUp);

        else if (master.PlayerInput.y < -0.5f)
        {
            SoundManager.PlayInAvatar(SoundManager.SoundName.meleeKirbySlashCast);
            SetAttackAnimationTrigger(Player.AttackType.airDown);

            Vector3 endPos = master.tr.position;
            endPos.y += master.downAttackBumpDistance;

            coroutines.Add(master.MoveToXInTime(master.tr.position, endPos, master.playerAnimationController.downAttackBumpDuration, master.playerAnimationController.downAttackCurve, () => swiping = true /*master.GetComponent<LineRenderer>().enabled = true*/ ));
            jumpAttackPos = coroutines.Count - 1;
            downAttack = true;
            
        }
        else
            SetAttackAnimationTrigger(Player.AttackType.airForward);
    }

    void TopCheck()
    {
        RaycastHit2D hit = Physics2D.Raycast(master.tr.position, Vector2.up, 2.1f, master.controller.currentCollisionMask);

        if (hit){

            Vector3 pos = hit.point;
            pos.y -= 2f;
            pos.z = master.tr.position.z;
            master.tr.position = pos;
            
            if(coroutines.Count != 0)
            {
                master.StopCoroutine(coroutines[jumpAttackPos]);
                coroutines.RemoveAt(jumpAttackPos);
            }
        }
    }

    void Attack()
    {
        if (master.PlayerInput.y > 0.5f)
            SetAttackAnimationTrigger(Player.AttackType.groundUp);
        else
            SetAttackAnimationTrigger(Player.AttackType.groundForward);
    }

    RaycastHit2D CheckForEnnemies()
    {
        int layerMask = Man_EnemyManager.Instance.enemyOne | Man_EnemyManager.Instance.enemyTwo;

        float distanceCheck = 0.5f;

        return Physics2D.BoxCast(master.tr.position, master.controller.collider.size, 0f, Vector2.right * master.Direction, distanceCheck, layerMask);
    }

    /// <summary>
    /// Procs enemies in range based on attackType or integer
    /// </summary>
    /// <param name="attackType"></param>
    /// <returns></returns>
    public Collider2D[] AttackResolution (Player.AttackType attackType)
    {
        int a = 0;
        Collider2D[] target = new Collider2D[10];

        contactFilter.layerMask = master.EnemyLayer;

        //Checks for collisions based on sepcified polygon collider
        a = master.attackCollisionManager.attackCollider[(int)attackType].OverlapCollider(contactFilter, target);

        //loop through every enemies and procs theirs receiveDamage methods
        if (target[0] != null){

            List<Collider2D> targets = new List<Collider2D>(target);
            targets.RemoveRange(a, 10 - a);

            foreach(Collider2D n in targets)
            {
                Enemy temp = n.GetComponent<Enemy>();

                if (temp != null)
                {
                    n.GetComponent<Enemy>().Hit(master.swordDamage);

                    switch (Random.Range(0, 3))
                    {
                        case 0:
                            SoundManager.Play(SoundManager.SoundName.meleeValid01);
                            break;
                        case 1:
                            SoundManager.Play(SoundManager.SoundName.meleeValid02);
                            break;
                        case 2:
                            SoundManager.Play(SoundManager.SoundName.meleeValid03);
                            break;
                    }
                }
            }

            return targets.ToArray();
        }
        return null;
    }

    private void SliceAttack()
    {
        Vector3 pos = master.tr.position;
        pos = master.tr.position;
        pos.x += 2f * master.Direction;

        int a = 0;
        Collider2D[] target = new Collider2D[10];
        
        if (sliceAttackCollider == null)
            CreateSliceAttackCollider(pos);

        //Checks for collisions based on sepcified polygon collider
        a = sliceAttackCollider.OverlapCollider(contactFilter, target);

        //loop through every enemies and procs theirs receiveDamage methods
        if (target[0] != null)
        {
            List<Collider2D> targets = new List<Collider2D>(target);
            targets.RemoveRange(a, 10 - a);

            foreach (Collider2D n in targets)
            {
                Enemy temp = n.GetComponent<Enemy>();
                if (temp != null && !hitEnnemies.Contains(temp))
                {
                    n.GetComponent<Enemy>().Hit(master.swordDamage);
                    SoundManager.PlayInAvatar(SoundManager.SoundName.meleeKirbySlashImpact);
                    hitEnnemies.Add(temp);
                }
            }
        }
    }

    private void CreateSliceAttackCollider(Vector3 pos)
    {
        sliceAttackCollider = new GameObject("Slice attack").AddComponent<BoxCollider2D>();

        sliceAttackCollider.size = new Vector2(2f, 1f);
        sliceAttackCollider.transform.position = master.tr.position;
        sliceAttackCollider.transform.parent = master.tr;

        sliceAttackCollider.offset = new Vector2(1f, 1f);
    }

    private void SetAttackAnimationTrigger(Player.AttackType attackType)
    {
        switch (attackType)
        {
            case Player.AttackType.groundUp:
                master.playerAnimationController.SetAtkMeleeUp();
                break;
            case Player.AttackType.groundForward:
                master.playerAnimationController.SetAtkMeleeForward();
                break;
            case Player.AttackType.airUp:
                master.playerAnimationController.SetAtkMeleeUp();
                break;
            case Player.AttackType.airForward:
                master.playerAnimationController.SetAtkMeleeForward();
                break;
            case Player.AttackType.airDown:
                master.playerAnimationController.SetAtkMeleeDown();
                break;
            default:
                break;
        }
    }

    public Player_sAttack(Player master) : base(master)
    {
        this.master = master;
        contactFilter = new ContactFilter2D {
            useLayerMask = true,
            layerMask = (master.controller.CurrentLayerDepth == LayerDepth.front) ? Man_EnemyManager.Instance.enemyOne : Man_EnemyManager.Instance.enemyTwo
        };
    }
}

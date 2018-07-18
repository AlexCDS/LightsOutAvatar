using UnityEngine;
using System.Collections.Generic;

/********************************************************************* 
 * By Alex
 * Jump state of the Player script. See Player_sState for more details
 * Last modification 07/05/2018
*********************************************************************/

public class Player_sDash : Player_sBase
{

    bool manaPaid = false;
    CapsuleCollider2D playerCollider;

    ParticleSystem dashFX;
    HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    private void ToNextState(DelegatedFonction delegatedFonction)
    {
        if(delegatedFonction != null)
            delegatedFonction();
        Tool_FXPooling.Instance.ForcedReturnToPool("Dash", dashFX);
        manaPaid = false;

        if (master.upgrades.dashDamage)
            master.playerAnimationController.DashAttack = false;

        else
            master.playerAnimationController.Dash = false;

        playerCollider.enabled = true;
        hitEnemies = new HashSet<Enemy>();
    }

    public override void ToDeath()
    {
        ToNextState(null);
        base.ToDeath();
    }

    public override void ToIdle()
    {
        base.ToIdle();
        master.playerAnimationController.ApplyRootMotion();
    }

    public override void ToLocomotion()
    {
        base.ToLocomotion();
        master.playerAnimationController.ApplyRootMotion();
        master.playerAnimationController.SetHSpeed(1f);
    }

    public override void Update()
    {
        if (!manaPaid){
            manaPaid = true;

            dashFX = Tool_FXPooling.Instance.SpawnFX("Dash", master.playerAnimationController.spine1.transform.position, Quaternion.identity, master.playerAnimationController.spine1);

            var main = dashFX.main;
            var renderer = dashFX.GetComponent<ParticleSystemRenderer>();

            Vector3 fxPivot = renderer.pivot;
            fxPivot.x = 0.6f * -master.Direction;

            renderer.pivot = fxPivot;

            master.mana -= master.dashManaCost;
            Man_GameManager.Instance.hud.UpdateManaUICurved(master.mana);

            playerCollider.enabled = false;
        }

        if (master.upgrades.dashDamage || master.upgrades.dashStun)
        {
            EnemyCheck();
        }

    }

    public override void Move(ref Vector3 velocity)
    {
        if (master.DastTimeLeft > 0f)
        {
            velocity.x = master.Direction * master.dashSpeed * Time.deltaTime;
            velocity.y = 0f;
            master.controller.HorizontalCollisions(ref velocity);
            if (master.controller.collisions.right || master.controller.collisions.left)
            {
                StopDash(ref velocity);
                master.playerAnimationController.WallCollision();
            }
            master.DastTimeLeft -= Time.deltaTime;
        }
        else
        {
            StopDash(ref velocity);
        }

        master.transform.Translate(velocity, Space.World);

    }

    void StopDash(ref Vector3 velocity)
    {
        velocity = Vector3.zero;
        master.DastTimeLeft = master.dashTime;

        if (Mathf.Abs(master.PlayerInput.x) > 0.1f)
        {
            ToNextState(() => ToLocomotion());
        }
        else
            ToNextState(() => ToIdle());
        
    }

    public override void PlatformMove(Vector3 velocity) {
        base.PlatformMove(velocity);
    }

    void EnemyCheck()
    {
        int a = 0;
        Collider2D[] target = new Collider2D[10];

        if(master.AttackFilter.layerMask != master.EnemyLayer)
        {
            ContactFilter2D contact = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = master.EnemyLayer
            };

            master.AttackFilter = contact;
        }

        //Checks for collisions based on specified polygon collider
        a = master.boxCollider.OverlapCollider(master.AttackFilter, target);

        //loop through every enemies and procs theirs receiveDamage methods
        if (target[0] != null)
        {
            List<Collider2D> targets = new List<Collider2D>(target);
            targets.RemoveRange(a, 10 - a);

            foreach (Collider2D n in targets)
            {
                Enemy temp = n.GetComponent<Enemy>();
                if (temp != null && !hitEnemies.Contains(temp))
                {
                    hitEnemies.Add(temp);
                
                    if (master.upgrades.dashDamage)
                        n.GetComponent<Enemy>().Hit(master.swordDamage);

                   if (master.upgrades.dashStun)
                       n.GetComponent<Enemy>().Stun();
                }
            }
        }
    }

    public Player_sDash(Player master) : base(master)
    {
        this.master = master;
        playerCollider = master.GetComponent<CapsuleCollider2D>();
    }
}

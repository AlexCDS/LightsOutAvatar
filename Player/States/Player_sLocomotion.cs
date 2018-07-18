using System;
using UnityEngine;
/********************************************************************* 
 * By Alex
 * Locomotion state of the Player script. See Player_sState for more details
 * Last modification 27/03/2018
*********************************************************************/

public class Player_sLocomotion : Player_sBase
{
    float timeLeft = 0.05f;
    bool wallrun = false;
    
    public override void ToJump() {
        base.ToJump();
        master.playerAnimationController.InAir = true;
    }

    public override void ToRangedAttack() {
        base.ToRangedAttack();
        master.playerAnimationController.SetAtkLaser = true;
    }

    public override void ToIdle() {
        base.ToIdle();
        master.playerAnimationController.SetHSpeed(0f);
    }

    public override void ToDash()
    {
        base.ToDash();
        master.playerAnimationController.RemoveRootMotion();
    }

    public override void ToGrenade()
    {
        base.ToGrenade();
        master.playerAnimationController.SetFlashBombMode(true);
    }

    public override void ToWallrun()
    {
        base.ToWallrun();
        master.playerAnimationController.RemoveRootMotion();
        master.Wallrun();
    }

    public override void Update() {
        if (!master.CanMove)
            ToIdle();
    }

    public override void Move(ref Vector3 velocity)
    {
        if (master.Direction != Mathf.Sign(master.PlayerInput.x) && master.PlayerInput.x != 0)
        {
            master.Direction = (int)Mathf.Sign(master.PlayerInput.x);
            master.playerAnimationController.IsTurning();
            master.Turn(false);
            return;
        }

        else if(master.Direction != Mathf.Sign(master.PlayerInput.x) && master.PlayerInput.x != 0)
        {
            master.Direction = (int)Mathf.Sign(master.PlayerInput.x);
            return;
        }
        
		if (master.upgrades.wallrun && PlayerIsShadow () || !WallClimbCheck () && PlayerIsShadow()) {
			if (!WallrunCheck ())
				CollisionsCheck ();
		}

		else CollisionsCheck ();
		
        master.controller.VerticalCollisions(ref velocity);

        if (!master.controller.collisions.below)
        {
            ToJump();
            return;
        }

        if (CollisionBehaviour(ref velocity))
            return;

        if (master.PlayerInput.x == 0)
        {
            if (timeLeft <= 0)
                ToIdle();

            timeLeft -= Time.deltaTime;
            return;
        }

        master.playerAnimationController.SetHSpeed(Mathf.Clamp(Mathf.Abs(master.PlayerInput.x), 0.1f, (master.InDialogTrigger)? 0.8f : 1f));
    }

    private void CollisionsCheck()
    {
        Vector3 animationVelocity = new Vector3((7f * Time.deltaTime) * master.Direction, 0f, 0f);
        master.controller.HorizontalCollisions(ref animationVelocity);
        wallrun = false;
    }

    private bool WallrunCheck()
    {
        if (Mathf.Abs(master.PlayerInput.x) > 0.7)
        {
            Vector3 temp = new Vector3((7f * Time.deltaTime) * master.Direction, 0f, 0f);
            master.controller.HorizontalCollisions(ref temp);
        }

        if ((master.controller.collisions.right || master.controller.collisions.left) && master.controller.collisions.numberOfHitsX >= master.controller.horizontalRayCount)
        {
            wallrun = true;
            return true;
        }
        return false;
    }

    private RaycastHit2D WallClimbCheck()
    {
        Vector2 raycastOrigin = master.tr.position;
        raycastOrigin.y += 2.1f;
        return Physics2D.Raycast(raycastOrigin, Vector2.right * master.Direction, 1f, (master.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.layerFront : Man_LevelManager.currentLevel.layerBack);
    }

    private bool CollisionBehaviour(ref Vector3 velocity)
    {
        if (master.controller.collisions.left || master.controller.collisions.right)
        {
            if (wallrun)
            {
                ToWallrun();
                return true;
            }
            else
            {
                ToIdle();
                ResetVelocity(ref velocity);
            }
        }
        return false;
    }

    private void ResetVelocity(ref Vector3 velocity)
    {
        velocity = Vector3.zero;      
    }

    public override void PlatformMove(Vector3 velocity){
        base.PlatformMove(velocity);
    }

    public override void ButtonDownA() {
        ToJump();
        master.Jump();
        master.playerAnimationController.IsJumping();
        SoundManager.PlayInAvatar(SoundManager.SoundName.jump);
    }

    public override void ButtonDownX()
    {
        if (PlayerIsLight() && !master.attackState.brokenCombo && !master.InChangeLayer)
            ToAttack();
    }

    public override void ButtonDownY()
    {
        if (PlayerIsLight() && !master.attackState.brokenCombo)
        {
            if (master.UltimateCharge >= 100)
            {
                master.UltimateCharge = 0;
                ToUltimate();
                return;
            }
        }
    }

    public override void TriggerDownL()
    {
        if (master.CurrentGravityZone == master.CurrentMode)
        {
            SoundManager.PlayInAvatar(SoundManager.SoundName.abilityDenied);
            return;
        }
        if (master.upgrades.lightMode && PlayerIsShadow() && master.CurrentGravityZone != Player.PlayerMode.shadow)
        {
            ToGrenade();
            master.TeleportGrenade = false;
        }
        if (PlayerIsLight())
        {
            ToRangedAttack();
            master.playerAnimationController.SetHSpeed(0f);
        }
    }

    public override void TriggerDownR()
    {
        if (PlayerIsLight() && !master.controller.collisions.left && !master.controller.collisions.right && master.CurrentGravityZone != Player.PlayerMode.light)
        {
            ToDash();

            if (master.upgrades.dashDamage)
                master.playerAnimationController.DashAttack = true;

            else
                master.playerAnimationController.Dash = true;
        }
        if (master.upgrades.grenadeTeleport && PlayerIsShadow() && master.CurrentGravityZone != Player.PlayerMode.shadow)
        {
            ToGrenade();
            master.TeleportGrenade = true;
        }
    }

    public override void BumperDownR() {
        master.ChangeMode();
    }

    public Player_sLocomotion(Player master) : base(master)
    {
        this.master = master;
    }
}

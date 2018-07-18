using UnityEngine;

/********************************************************************* 
 * By Alex
 * Jump state of the Player script. See Player_sState for more details
 * Last modification 24/03/2018 - AlexC
*********************************************************************/

public class Player_sIdle : Player_sBase {

    public override void ToJump() {
        base.ToJump();
        master.playerAnimationController.InAir = true;
    }
    public override void ToRangedAttack() {
        base.ToRangedAttack();
        master.playerAnimationController.SetAtkLaser = true;
    }
    public override void ToLocomotion() {
        base.ToLocomotion();
    }
    public override void ToDash()
    {
        base.ToDash();
        master.playerAnimationController.RemoveRootMotion();
    }
    public override void ToDeath()
    {
        base.ToDeath();
    }
    public override void Update() {
        master.ResetVelocity();
    }

    public override void Move(ref Vector3 velocity) {
        if (master.CanMove)
        {
            master.controller.VerticalCollisions(ref velocity);

            if (!master.controller.collisions.below)
            {
                ToJump();
                return;
            }

            if (master.PlayerInput.x != 0f)
            {
                Vector3 direction = new Vector3(Mathf.Sign(master.PlayerInput.x) * 0.3f, 0f, 0f);
                master.controller.HorizontalCollisions(ref direction);

                if (!master.controller.collisions.left && !master.controller.collisions.right)
                {
                    ToLocomotion();
                    return;
                }
            }
        }

        master.playerAnimationController.SetHSpeed(0f);
    }

    public override void PlatformMove(Vector3 velocity) {
        base.PlatformMove(velocity);
    }

    public override void ButtonDownA() {
        if (master.CanMove)
        {
            ToJump();
            master.Jump();
            master.playerAnimationController.IsJumping();
            SoundManager.PlayInAvatar(SoundManager.SoundName.jump);
        }
    }

    public override void ButtonDownX()
    {
        if (PlayerIsLight() && master.CanMove && !master.attackState.brokenCombo && !master.InChangeLayer)
            ToAttack();
    }

    public override void ButtonDownY()
    {
        if (PlayerIsLight() && master.CanMove && !master.attackState.brokenCombo)
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
        if (master.upgrades.lightMode && PlayerIsShadow() && master.CanMove && master.CurrentGravityZone != Player.PlayerMode.shadow)
        {
            ToGrenade();
            master.playerAnimationController.SetFlashBombMode(true);
            master.TeleportGrenade = false;
        }
        if (PlayerIsLight())
            ToRangedAttack();
    }

    public override void TriggerDownR() {
        if (master.CurrentGravityZone == master.CurrentMode)
        {
            SoundManager.PlayInAvatar(SoundManager.SoundName.abilityDenied);
            return;
        }

        if (PlayerIsLight() && master.CanMove && !master.controller.collisions.left && !master.controller.collisions.right && master.CurrentGravityZone != Player.PlayerMode.light)
        {
            ToDash();

            if (master.upgrades.dashDamage)
                master.playerAnimationController.DashAttack = true;

            else
                master.playerAnimationController.Dash = true;
        }
        if (master.upgrades.grenadeTeleport && PlayerIsShadow() && master.CanMove && master.CurrentGravityZone != Player.PlayerMode.shadow)
        {
            ToGrenade();
            master.playerAnimationController.SetFlashBombMode(true);
            master.TeleportGrenade = true;
        }
    }
    
    public override void BumperDownR() {
        if(master.CanMove)
            master.ChangeMode();
    }

    public Player_sIdle(Player master):base(master)
    {
        this.master = master;
    }
}

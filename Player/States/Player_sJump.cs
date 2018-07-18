using System;
using System.Collections;
using UnityEngine;

/********************************************************************* 
 * By Alex
 * Jump state of the Player script. See Player_sState for more details
*********************************************************************/

public class Player_sJump : Player_sBase
{
    //Set by the player
    public float minJumpVelocity;
    public float maxJumpVelocity;
    public float aerialMoveSpeed;
    public float doubleJumpVelocity;
    public bool attacked = false;

    bool unstick = false;
    bool jumpedTwice = false;
    bool doubleJump = false;
    bool wallJump = false;
    bool wallSliding = false;
    bool wallGripping = false;
    
    float wallJumpInitiationTime = 0f;

    public override void ToDeath()
    {
        base.ToDeath();
        jumpedTwice = false;
        attacked = false;
    }

    public override void ToAttack() {
        base.ToAttack();
    }
    
    public override void ToIdle()
    {
        base.ToIdle();
        jumpedTwice = false;
        attacked = false;
        master.playerAnimationController.OnWall = false;
        Tool_FXPooling.Instance.SpawnFX("JumpLand", master.tr.position);
        SoundManager.Play(SoundManager.SoundName.land);
    }

    public override void ToMonkeyBarLocomotion()
    {
        master.playerAnimationController.ApplyRootMotion();
        base.ToMonkeyBarLocomotion();
        jumpedTwice = false;
    }

    public override void Update()
    {
        if (master.WallSliding)
        {
            wallSliding = true;
            master.WallSliding = false;
            SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
        }
    }

    public override void Move(ref Vector3 velocity)
    {
        if (!wallJump)
        {
            if (doubleJump && !jumpedTwice)
                DoubleJump(ref velocity);

            doubleJump = false;

            if (wallSliding)
            {
                Vector3 temp = new Vector3(master.WallDirX * 0.1f, 0f, 0f);
                master.controller.HorizontalCollisions(ref temp);
                if (!master.controller.collisions.right && !master.controller.collisions.left)
                {
                    wallSliding = false;
                    SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
                }
            }
            else
                master.controller.HorizontalCollisions(ref velocity);

            master.controller.VerticalCollisions(ref velocity);

            if (master.controller.collisions.below)
            {
                ToIdle();
                master.playerAnimationController.ApplyRootMotion();
                AjustAirMove(velocity);
                SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
                return;
            }

            if (master.controller.collisions.onMonkeyBar && PlayerIsShadow())
            {
                ToMonkeyBarLocomotion();
                master.playerAnimationController.Monkeybars();

                return;
            }
                
            if (CanWallSlide(velocity) && PlayerIsShadow() && !wallSliding)
            {
                master.WallDirX = master.controller.collisions.right ? 1 : -1;

                if (master.Direction != master.WallDirX)
                    master.Turn();

                if (master.upgrades.wallGrip && master.PlayerInput.y >= 0)
                {
                    master.playerAnimationController.Wallgrip();
                    SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
                }

                master.TimeToWallUnstick = master.wallSticktime;
                wallSliding = true;

                SoundManager.PlayInAvatar(SoundManager.SoundName.wallslide);
            }

            if (wallSliding)
                WallSlideControl(ref velocity);

            else
                master.TimeToWallUnstick = master.wallSticktime;

            master.playerAnimationController.OnWall = wallSliding;
            wallJump = false;
            unstick = false;

            if (master.controller.collisions.onCorner)
            {
                Vector3 endPos = master.controller.collisions.cornerLocation;

                endPos.z = (master.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack;
                Tool_FXPooling.Instance.SpawnFX("LedgeGrab", endPos);

                endPos.x = master.tr.position.x;
                endPos.y += 0.5f;

                master.MoveToXInTime(master.tr.position, endPos, 0.1f, () => master.playerAnimationController.ApplyRootMotion());
                master.Invoke("LedgeJump", 0.1F);

                jumpedTwice = false;
            }

            if (master.Direction != Mathf.Sign(velocity.x) && velocity.x != 0 && !wallSliding)
            {
                master.Direction = (int)Mathf.Sign(velocity.x);
                master.Turn(true);
            }

            master.playerAnimationController.SetHSpeed(Mathf.Abs(master.PlayerInput.x));

            AjustAirMove(velocity);
        }

        else if (wallJump)
        {
            wallJump = false;
            master.WallJump();
            master.Turn(true);
            velocity *= Time.deltaTime;
        }
    }

    /// <summary>
    /// Applies jump velocity to current velocity vector.
    /// </summary>
    void DoubleJump(ref Vector3 velocity)
    {
        velocity.y = doubleJumpVelocity * Time.deltaTime;
        master.playerAnimationController.IsJumping();
        Tool_FXPooling.Instance.SpawnFX("DoubleJump", new Vector3(master.tr.position.x, master.tr.position.y + 1f, master.tr.position.z), Quaternion.identity);
        jumpedTwice = true;
        doubleJump = false;
    }

    public override void ButtonDownA()
    {
        if (wallSliding && master.upgrades.wallJump)
        {
            wallJump = true;
            master.WallDirX = master.Direction;
            wallJumpInitiationTime = Time.time;
            master.playerAnimationController.IsJumping();
            wallSliding = false;

            SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
            SoundManager.PlayInAvatar(SoundManager.SoundName.jump);
        }

        else if (!jumpedTwice && master.upgrades.doubleJump && PlayerIsShadow())
        {
            doubleJump = true;
            SoundManager.PlayInAvatar(SoundManager.SoundName.doublejump);
        }
    }

    public override void ButtonDownB()
    {
        master.TimeToWallUnstick = 0;
        unstick = true;
    }

    public override void ButtonDownX()
    {
        if (PlayerIsLight() && !attacked && !master.InChangeLayer)
        {
            ToAttack();
            attacked = true;
        }
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

    public Player_sJump(Player master) : base(master)
    {
        this.master = master;
    }
    
    private void AjustAirMove(Vector3 velocity)
    {
        master.transform.Translate(velocity, Space.World);
    }

    private void WallSlideControl(ref Vector3 velocity)
    {
        float maxVelocity = 0;

        if (master.PlayerInput.y < 0 || !master.upgrades.wallGrip)
        {
            maxVelocity = -master.wallSlideMaxVelocity * Time.deltaTime;
            if (wallGripping)
            {
                master.playerAnimationController.WallSliding();
                SoundManager.PlayInAvatar(SoundManager.SoundName.wallslide);
                wallGripping = false;
            }
        }

        else if (master.upgrades.wallGrip)
        {
            maxVelocity = 0f;

            if (!wallGripping)
            {
                master.playerAnimationController.Wallgrip();
                SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
                wallGripping = true;
            }
        }

        if (velocity.y < maxVelocity)
            velocity.y = maxVelocity;

        if (master.TimeToWallUnstick > 0)
        {
            velocity.x = 0;

            if (Mathf.Sign(master.PlayerInput.x) != master.WallDirX && master.PlayerInput.x != 0)
                master.TimeToWallUnstick -= Time.deltaTime;

            else
                master.TimeToWallUnstick = master.wallSticktime;

            if (unstick)
            {
                master.playerAnimationController.OnWall = wallSliding = false;
                Vector2 unstick = new Vector2(2, 0);
                master.TimeToWallUnstick = 0;
                master.WallJump(unstick * master.WallDirX);
                SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
            }
        }
        else
        {
            master.playerAnimationController.OnWall = wallSliding = false;
            SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
        }
    }

    private void WallSlideMove()
    {
        if (!master.controller.collisions.right && !master.controller.collisions.left)
        {
            master.WallSliding = false;
            SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
        }
    }

    bool CanWallSlide(Vector3 velocity)
    {
        return ((master.controller.collisions.left || master.controller.collisions.right) && master.controller.collisions.numberOfHitsX == master.controller.horizontalRayCount);
    }

    bool WallSliding(Vector3 velocity)
    {
        return ((master.controller.collisions.left || master.controller.collisions.right));
    }


}

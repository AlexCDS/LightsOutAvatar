using UnityEngine;

/********************************************************************* 
 * By Alex
 * Template state for the Player script. See Player_sState for more details
*********************************************************************/

public class Player_sWallrun : Player_sBase
{
    float ledgeGrabAnimationTime;
    float wallrunToWallJumpAnimationTime;
    
    public override void Move(ref Vector3 velocity)
    {
        Vector3 temp = new Vector3(master.Direction, 0f, 0f);
        master.controller.HorizontalCollisions(ref temp);

        temp = new Vector3(7f * Time.deltaTime, 0f, 0f);
        master.controller.VerticalCollisions(ref temp);


        if (master.PlayerInput.x == 0)
        {
            ToJump();
            master.playerAnimationController.WallSliding();
            master.WallSliding = true;

            master.ResetVelocity();
        }

        //monkey bar check
        if (master.controller.collisions.onMonkeyBar)
        {
            ToMonkeyBarLocomotion();
            if (Man_GameManager.debugMode)
                Debug.Log("On monkey bar!");
        }
        
        if (master.controller.collisions.above && master.controller.collisions.numberOfHitsY > 2)
        {
            ToJump();
            master.playerAnimationController.IsWallSliding();
            master.WallSliding = true;
        }
        
        if (master.controller.collisions.onCorner)
        {
            ToIdle();
            master.StopMovement(true);
            master.playerAnimationController.RemoveRootMotion();
            master.playerAnimationController.IsLedgeGrabbing();

            Vector3 endPos = master.controller.collisions.cornerLocation;
            endPos.x += 0.5f * master.Direction;
            endPos.y += 0f;
            endPos.z = (master.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack;
            master.MoveToXInTime(master.tr.position, endPos, ledgeGrabAnimationTime, () => master.playerAnimationController.ApplyRootMotion());
            
        }
    }

    public override void PlatformMove(Vector3 velocity) { }

    public override void ButtonDownA()
    {
        ToJump();
        master.WallDirX = master.Direction;
        master.playerAnimationController.IsJumping();
        master.WallJump();
    }

    public Player_sWallrun(Player master, float ledgeGrabAnimationTime, float wallrunToWallJumpAnimationTime) : base(master)
    {
        this.master = master;
        this.ledgeGrabAnimationTime = ledgeGrabAnimationTime;
        this.wallrunToWallJumpAnimationTime = wallrunToWallJumpAnimationTime;
    }

}

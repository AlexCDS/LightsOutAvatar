using UnityEngine;

/********************************************************************* 
 * By Alex
 * MonkeyBar state for the Player script. See Player_sState for more details
*********************************************************************/

public class Player_sMonkeyBar : Player_sBase
{

    public override void ToJump()
    {
        base.ToJump();
        master.ResetYVelocity();
        master.playerAnimationController.IsJumping();
    }

    public override void Update() {
        master.ResetVelocity();
    }

    public override void Move(ref Vector3 velocity)
    {
        if (master.Direction != Mathf.Sign(master.PlayerInput.x) && master.PlayerInput.x != 0)
        {
            master.Direction = (int)Mathf.Sign(master.PlayerInput.x);
            master.Turn();
        }
        
        Vector3 temp = Vector3.up;
        master.controller.VerticalCollisions(ref temp);
        master.controller.HorizontalCollisions(ref velocity);

        if (!master.controller.collisions.onMonkeyBar)
        {
            ToJump();
            return;
        }

        Vector3 nextMonkeyCheck = master.tr.position;
        nextMonkeyCheck.x += 1f * master.Direction;

        RaycastHit2D hit = Physics2D.Raycast(nextMonkeyCheck, Vector2.up, 3f, (master.controller.CurrentLayerDepth == LayerDepth.front)? Man_LevelManager.currentLevel.layerFront : Man_LevelManager.currentLevel.layerBack);

        if (hit)
        {
            Ing_MonkeyBar monkeyBar = hit.collider.GetComponent<Ing_MonkeyBar>();

            if (monkeyBar == null)
                master.playerAnimationController.SetHSpeed(0);

            else
                master.playerAnimationController.SetHSpeed(Mathf.Abs(master.PlayerInput.x));
        }
    }

    public override void PlatformMove(Vector3 velocity) { }

    public override void ButtonDownA() {
            ToJump();
    }

    public override void ButtonDownB()
    {
        ToJump();
    }

    public override void TriggerDownL()
    {
        if (master.upgrades.monkeybarGrenade && master.upgrades.lightMode)
        {
            ToMonkeyBarGrenade();
            master.playerAnimationController.SetFlashBombMode(true);
            master.TeleportGrenade = false;
        }
    }

    public override void TriggerDownR()
    {
        if (master.upgrades.grenadeTeleport && master.upgrades.monkeybarGrenade)
        {
            ToMonkeyBarGrenade();
            master.playerAnimationController.SetFlashBombMode(true);
            master.TeleportGrenade = true;
        }
    }

    public Player_sMonkeyBar(Player master) : base(master)
    {
        this.master = master;
    }

}

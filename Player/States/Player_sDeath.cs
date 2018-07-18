using UnityEngine;

/********************************************************************* 
 * By Alex
 * Hang state of the Player script. See Player_sState for more details
 * Last modification 12/03/2018
*********************************************************************/

public class Player_sDeath : Player_sBase
{

    public override void ToIdle()
    {
        base.ToIdle();
        master.playerAnimationController.SetUndeath();
		master.playerAnimationController.ApplyRootMotion ();
    }

    public override void Move(ref Vector3 velocity)
    {
        master.controller.HorizontalCollisions(ref velocity);
        master.controller.VerticalCollisions(ref velocity);


        if (master.controller.collisions.below)
            master.ResetVelocity();

        master.transform.Translate(velocity, Space.World);
    }


    public override void PlatformMove(Vector3 velocity) {
        base.PlatformMove(velocity);
    }

    public override void ButtonDownA()
    {
        if (Man_GameManager.deathDone)
        {
            ToIdle();
            master.StartCoroutine(ScenesManager.Instance.FadeOut(true));
            master.Respawn();
            Man_GameManager.main.mainCamera.ResetValues();
            if (Man_GameManager.main.hud != null)
            {
                Man_GameManager.main.hud.UpdateHpUI(Man_GameManager.main.player.hitPoints);
                if (master.upgrades.lightMode)
                    Man_GameManager.main.hud.UpdateManaUICurved(Man_GameManager.main.player.mana);
            }
        }
    }

    public Player_sDeath(Player master) : base(master)
    {
        this.master = master;
    }
}

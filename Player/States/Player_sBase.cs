using UnityEngine;

/* By Alex
* Parent of the different state the Player script can have. The To*Something* functions are used to change
* from one state to the other. The update and the Move functions are called by the player every frames.
*/


public abstract class Player_sBase {

    public static bool debugMode = false; //When active, sends useful information about states to the console

    protected Player master;
    protected PlayerAnimationController AnimationController
    {
        get
        {
            if (animationController.leftFoot == null)
                animationController = master.playerAnimationController;

            return animationController;
        }
    }

    private PlayerAnimationController animationController = new PlayerAnimationController();

    #region base transition functions
    public virtual void ToJump(){
        if(debugMode)
            Debug.Log("Jump State");
        
        master.currentState = master.jumpState;
        master.playerAnimationController.InAir = true;
        master.playerAnimationController.RemoveRootMotion();
    }
    public virtual void ToAttack()
    {
        if (debugMode)
            Debug.Log("Attack State");

        //Debug reset
        if (!master.attackState.haveBeenReset && master.attackState.attacked && master.attackState.numberOfHits <= 1)
        {
            master.attackState.Reset();
            if (debugMode)
                Debug.LogError("Attack State have been forced to reset");
        }
        master.TimeToRegen = 0;
        master.currentState = master.attackState;

    }
    public virtual void ToRangedAttack(){
        if (debugMode)
            Debug.Log("Ranged Attack State");

        master.TimeToRegen = 0;
        master.currentState = master.rangedAttackState;
        SoundManager.PlayInAvatar(SoundManager.SoundName.laserFire);
    }
    public virtual void ToIdle(){
        if (debugMode)
            Debug.Log("Idle State");
        
        master.playerAnimationController.InAir = false;
        master.currentState = master.idleState;
    }
    public virtual void ToLocomotion(){
        if (debugMode)
            Debug.Log("Locomotion State");

        master.playerAnimationController.InAir = false;
        master.currentState = master.locomotionState;
    }
    public virtual void ToDeath()
    {
        if (debugMode)
            Debug.Log("Death State");
        master.currentState = master.deathState;
    }
    public virtual void ToDash()
    {
        if (debugMode)
            Debug.Log("Dash State");

        master.TimeToRegen = 0;
        master.currentState = master.dashState;
        master.DastTimeLeft = master.dashTime;
        SoundManager.PlayInAvatar(SoundManager.SoundName.dash);
    }
    public virtual void ToMonkeyBarLocomotion()
    {
        if (debugMode)
            Debug.Log("MonkeyBar Locomotion State");
        master.currentState = master.monkeyLocomotionState;
    }
    public virtual void ToMonkeyBarIdle()
    {
        if (debugMode)
            Debug.Log("MonkeyBar Idle State");
        master.currentState = master.monkeyIdleState;
    }
    public virtual void ToMonkeyBarGrenade()
    {
        if (debugMode)
            Debug.Log("MonkeyBar Grenade State");
        master.currentState = master.monkeyGrenadeState;
        SoundManager.PlayInAvatar(SoundManager.SoundName.flashbombAim);
    }
    public virtual void ToGrenade()
    {
        if (debugMode)
            Debug.Log("Grenade State");
        master.currentState = master.grenadeState;
        SoundManager.PlayInAvatar(SoundManager.SoundName.flashbombAim);
    }
    public virtual void ToWallrun()
    {
        if (debugMode)
            Debug.Log("Wallrun State");
        master.currentState = master.wallrunState;
    }
    public virtual void ToUltimate()
    {
        if (debugMode)
            Debug.Log("Ultimate state");
        SoundManager.PlayInAvatar(SoundManager.SoundName.meleeUltimate);
        master.currentState = master.ultimateState;
        master.playerAnimationController.Ultimate();
    }
    #endregion

    public virtual void Update() { }
    public virtual void Move(ref Vector3 velocity){ }

    //Called by platforms when they detect a player.
    public virtual void PlatformMove(Vector3 velocity)
    {
        master.controller.HorizontalCollisions(ref velocity);
        master.controller.VerticalCollisions(ref velocity);

        
        master.transform.Translate(velocity, Space.World);
    }

    public virtual void ButtonDownA() { }
    public virtual void ButtonUpA() { }
    public virtual void ButtonDownB() { }
    public virtual void ButtonDownX() { }
    public virtual void ButtonUpX() { }
    public virtual void ButtonDownY() { }
    public virtual void ButtonUpY() { }
    public virtual void TriggerDownL() { }
    public virtual void TriggerUpL() { }
    public virtual void TriggerDownR() { }
    public virtual void TriggerUpR() { }
    public virtual void BumperDownL() { }
    public virtual void BumperDownR() { }

    public Player_sBase(Player master)
    {
        this.master = master;
    }

    /// <summary>
    /// returns true if player is in light mode
    /// </summary>
    /// <returns></returns>
    public virtual bool PlayerIsLight()
    {
        return Man_GameManager.main.player.CurrentMode == Player.PlayerMode.light;
    }

    /// <summary>
    /// returns true if player is in shadow mode
    /// </summary>
    /// <returns></returns>
    public virtual bool PlayerIsShadow()
    {
        return Man_GameManager.main.player.CurrentMode == Player.PlayerMode.shadow;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// *********************************************************
// * By AlexC
// * Script managing player animation behaviour.
// *********************************************************

[System.Serializable]
public struct PlayerAnimationController{

    private Player player;
    private Coroutine dissolveCoroutine;

    public AnimationCurve downAttackCurve;

    public float downAttackBumpDuration;

    #region To assignation
    //Bones assignation, for FX position management
    public GameObject rightHand;
    public GameObject leftHand;
    public GameObject leftFoot;
    public GameObject rightFoot;
    public GameObject spine1;

    public SkinnedMeshRenderer playerMesh;

    [Header("Player color emission management")]
    public Material head;
    public Material skin;
    public Material hair;
    public Material clothing;
    private Material[] emissions;
    private Color emissionColor;
    #endregion

    #region Animation duration

    public float ReveilAnimationDuration { private set; get; }
    public float DeathAnimationDuration { private set; get; }

    public float WallJumpAnimationDuration { private set; get; }
    public float TurnAnimationDuration { private set; get; }

    public float FirstAttackAnimationDuration { private set; get; }
    public float SecondAttackAnimationDuration { private set; get; }
    public float ThirdAttackAnimationDuration { private set; get; }
    public float LazerStartAnimationDuration { get; private set; }

    public float UltimateAnimationDuration { private set; get; }

    public float DureeWallRunStart { private set; get; }
    public float WallRunJumpAnimationDuration { get; private set; }
    public float LedgeGrabAnimationDuration { get; private set; }
    public float DureeTransformationShadow { private set; get; }
    public float DureeTransformationLight { private set; get; }
    
    #endregion

    #region to hash
    int inAir;
    int onWall;
    int hSpeed;
    int vSpeed;
    int wallrun;
    int isLedgeGrabbing;
    int wallgrip;
    int isTurning;
    int isJumping;
    int fb_mode;
    int fb_angle;
    int fb_throw;
    int fb_detonate;
    int fb_blink;
    int fbAway;

    int dash;
    int dashAttack;
    int isTransforming;
    int forceTransformation;
    int isWallSliding;
    int monkeybars;
    int wallCollision;

    int atk_laser;
    int atk_meleeForward;
    int atk_meleeUp;
    int atk_meleeDown;
    int AOE_charge;
    int AOE_release;

    int hurt;
    int death;
    int undeath;

    int ultimate;

    int portal;
    int reveil;
    int upgradeL;
    int upgradeS;
    #endregion

    public Animator AnimatorControler { private set; get; }

    public void InitPlayerAnimationController()
    {
        player = Man_GameManager.main.player;
        AnimatorControler = player.GetComponent<Animator>();

        GetAnimationDurations();
        AnimationStringToHash();

        Material[] mats = playerMesh.materials;
        emissions = new Material[4];

        foreach (Material mat in mats)
        {
            string matName = mat.name;
            matName = matName.Remove(matName.Length - 11, 11);

            if (matName == head.name)
                emissions[0] = mat;

            if (matName == skin.name)
                emissions[1] = mat;
            
            if (matName == hair.name)
                emissions[2] = mat;

            if (matName == clothing.name)
                emissions[3] = mat;
        }

    }

    public void GetAnimationDurations()
    {
        TurnAnimationDuration = AnimationDuration("L_runTurn");

        DureeWallRunStart = AnimationDuration("S_wallrunStart");
        WallRunJumpAnimationDuration = AnimationDuration("S_wallrunToWalljump");

        LedgeGrabAnimationDuration = AnimationDuration("S_ledgeGrab");
        WallJumpAnimationDuration = AnimationDuration("S_walljump");

        DeathAnimationDuration = AnimationDuration("S_death");

        FirstAttackAnimationDuration = AnimationDuration("L_meleeGroundForward_01");
        SecondAttackAnimationDuration = AnimationDuration("L_meleeGroundForward_02");
        ThirdAttackAnimationDuration = AnimationDuration("L_meleeGroundForward_03");

        LazerStartAnimationDuration = AnimationDuration("L_laserStart");

        UltimateAnimationDuration = AnimationDuration("L_meleeAirUltimate");
       
        ReveilAnimationDuration = AnimationDuration("C_Avatar_transform");
    }

    /// <summary>
    /// Returns the animation time of a specified animation.
    /// </summary>
    /// <param name="animationName"></param>
    /// <returns></returns>
    public float AnimationDuration(string animationName)
    {
        RuntimeAnimatorController rac = AnimatorControler.runtimeAnimatorController;
        foreach (AnimationClip ac in rac.animationClips)
        {
            if (ac.name == animationName)
            {
                return ac.length;
            }
        }

        Debug.LogError("Couldn't find animation of name " + animationName);
        return -1;
    }

    void AnimationStringToHash()
    {
        inAir = Animator.StringToHash("inAir");
        onWall = Animator.StringToHash("onWall");
        hSpeed = Animator.StringToHash("hSpeed");
        vSpeed = Animator.StringToHash("vSpeed");
        wallrun = Animator.StringToHash("wallrun");
        isLedgeGrabbing = Animator.StringToHash("isLedgeGrabbing");
        wallgrip = Animator.StringToHash("wallgrip");
        isTurning = Animator.StringToHash("isTurning");
        isJumping = Animator.StringToHash("isJumping");
        fb_mode = Animator.StringToHash("fb_mode");
        fb_angle = Animator.StringToHash("fb_angle");
        fb_throw = Animator.StringToHash("fb_throw");
        fb_detonate = Animator.StringToHash("fb_detonate");
        fb_blink = Animator.StringToHash("fb_blink");
        fbAway = Animator.StringToHash("fbAway");

        dash = Animator.StringToHash("dash");
        dashAttack = Animator.StringToHash("dashattack");
        isTransforming = Animator.StringToHash("lightMode");
        forceTransformation = Animator.StringToHash("forcedTransform");
        isWallSliding = Animator.StringToHash("isWallSliding");
        monkeybars = Animator.StringToHash("monkeybars");
        wallCollision = Animator.StringToHash("wallCollision");

        atk_laser = Animator.StringToHash("atk_laser");
        atk_meleeForward = Animator.StringToHash("atk_meleeForward");
        atk_meleeUp = Animator.StringToHash("atk_meleeUp");
        atk_meleeDown = Animator.StringToHash("atk_meleeDown");
        AOE_charge = Animator.StringToHash("AOE_charge");
        AOE_release = Animator.StringToHash("AOE_release");

        hurt = Animator.StringToHash("hurt");
        death = Animator.StringToHash("death");
        undeath = Animator.StringToHash("undeath");

        ultimate = Animator.StringToHash("ultimate");

        portal = Animator.StringToHash("portal");
        reveil = Animator.StringToHash("reveil");
        upgradeL = Animator.StringToHash("upgradeL");
        upgradeS = Animator.StringToHash("upgradeS");
    }

    /// <summary>
    /// Changes the emission value of the player to represent light and shadow mode
    /// </summary>
    /// <param name="on"></param>
    public void ChangeEmission(bool on)
    {
        for (int i = 0; i < emissions.Length; i++)
        {
            emissions[i].SetFloat("_Emissive", ((on) ? 1f : 0f));
            emissions[i].SetFloat("_AvatarMode", ((on) ? 1f : 0f));
        }
    }

    /// <summary>
    /// Dissolve all material assigned to the player
    /// </summary>
    /// <param name="dissolve"></param>
    public void Dissolve(bool dissolve)
    {
        if (dissolve)
        {
            if(dissolveCoroutine != null)
                player.StopCoroutine(dissolveCoroutine);

            dissolveCoroutine = player.StartCoroutine(ToDissolve());
        }
        else
        {
            if (dissolveCoroutine != null)
                player.StopCoroutine(dissolveCoroutine);

            dissolveCoroutine = player.StartCoroutine(FromDissolve());
        }
    }

    IEnumerator ToDissolve()
    {
        float t = 0;
        while (t < 1){
            t += 0.01f;
            for (int i = 0; i < emissions.Length; i++)
                emissions[i].SetFloat("_DisolveAmount", t);
            yield return null;
        }
        for (int i = 0; i < emissions.Length; i++)
            emissions[i].SetFloat("_DisolveAmount", 1);

    }

    IEnumerator FromDissolve()
    {
        float t = 1;
        while (t > 0){

            t -= 0.01f;
            for (int i = 0; i < emissions.Length; i++)
            {
                emissions[i].SetFloat("_DisolveAmount", t);
            }

            yield return null;
        }
        for (int i = 0; i < emissions.Length; i++)
            emissions[i].SetFloat("_DisolveAmount", 0);//SetColor("_EmissionColor", emissionColor * );
    }
    
    //Bunch of prewritten commands to send to the Animator
    #region animator controller actions
    public void SetHSpeed(float speed)
    {
        if (speed < 0.1f && player.currentState == player.locomotionState)
        {
            Debug.Log(speed + " is lower than what is supposed");
            speed = 0.1f;
        }

        AnimatorControler.SetFloat(hSpeed, speed);
    }
    public void Ultimate() { AnimatorControler.SetTrigger(ultimate); }

    public void SetVSpeed(float speed) { AnimatorControler.SetFloat(vSpeed, speed); }

    public bool InAir { set { AnimatorControler.SetBool(inAir, value); } }

    public bool OnWall { set { AnimatorControler.SetBool(onWall, value); } }

    public void WallSliding() { AnimatorControler.SetTrigger(isWallSliding); }

    public void IsLedgeGrabbing() { AnimatorControler.SetTrigger(isLedgeGrabbing); }

    public void WallRun() { AnimatorControler.SetTrigger(wallrun); }

    public void Wallgrip() { AnimatorControler.SetTrigger(wallgrip); }

    public void IsTurning() { AnimatorControler.SetTrigger(isTurning); }

    public void IsJumping() { AnimatorControler.SetTrigger(isJumping); }

    public void SetFlashBombMode(bool state) { AnimatorControler.SetBool(fb_mode, state); }

    public float SetFlashBombAngle { set { AnimatorControler.SetFloat(fb_angle, value); } }

    public void SetFlashBombThrow() { AnimatorControler.SetTrigger(fb_throw); }

    public void SetFlashBombDetonate() { AnimatorControler.SetTrigger(fb_detonate); }

    public void SetFlashBombBlink() { AnimatorControler.SetTrigger(fb_blink); }

    public void SetFlashBombAway(bool isAway) { AnimatorControler.SetBool(fbAway, isAway); }

    public bool Dash { set { AnimatorControler.SetBool(dash, value); } }

    public bool DashAttack { set { AnimatorControler.SetBool(dashAttack, value); } }

    public bool IsTransforming { set { AnimatorControler.SetBool(isTransforming, value); } }

    public void IsWallSliding() { AnimatorControler.SetTrigger(isWallSliding); }

    public void Monkeybars() { AnimatorControler.SetTrigger(monkeybars); }

    public void WallCollision() { AnimatorControler.SetTrigger(wallCollision); }

    public void ForceTransformation() { AnimatorControler.SetBool(isTransforming, false); }

    public bool SetAtkLaser { set { AnimatorControler.SetBool(atk_laser, value); } }

    public void SetAtkMeleeForward() { AnimatorControler.SetTrigger(atk_meleeForward); }

    public void SetAtkMeleeUp() { AnimatorControler.SetTrigger(atk_meleeUp); }

    public void SetAtkMeleeDown() { AnimatorControler.SetTrigger(atk_meleeDown); }

    public void SetAOECharge() { AnimatorControler.SetTrigger(AOE_charge); }

    public void SetAOERelease() { AnimatorControler.SetTrigger(AOE_release); }

    public void SetHurt() { AnimatorControler.SetTrigger(hurt); }

    public void SetDeath() { AnimatorControler.SetTrigger(death); }

    public void SetUndeath() { AnimatorControler.SetTrigger(undeath); }

    public void SetUpgradeL() { AnimatorControler.SetTrigger(upgradeL); }

    public void SetUpgradeS() { AnimatorControler.SetTrigger(upgradeS); }

    public void SetPortal() { AnimatorControler.SetTrigger(portal); }

    public void Reveil() { AnimatorControler.SetTrigger(reveil); }


    public void ApplyRootMotion()
    {
        AnimatorControler.applyRootMotion = true;
    }

    public void RemoveRootMotion()
    {
        AnimatorControler.applyRootMotion = false;
    }
    #endregion
}

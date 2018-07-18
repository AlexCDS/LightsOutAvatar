using UnityEngine;
using System.Collections;
using Rewired;
using System.Collections.Generic;
using UnityEngine.UI;

// *********************************************************
// * By AlexC
// * Player behaviour, called upon from it's multiple states.
// *********************************************************

[
    RequireComponent(typeof(Controller2D)), //Custom collision detection 
    RequireComponent(typeof(BoxCollider2D)), //To be used with Controller2D
    RequireComponent(typeof(CapsuleCollider2D)), //For enemies and damage collision detection
    RequireComponent(typeof(Animator)), 
    RequireComponent(typeof(Uti_AnimatedLineRenderer)), RequireComponent(typeof(LineRenderer)) // For RangedAttack and Grenade trail rendering 
]

public class Player : MonoBehaviour
{
    public enum PlayerMode
    {
        light,
        shadow,
        zero
    }

    public enum AttackType
    {
        groundUp = 0,
        groundForward = 1,
        airUp = 2,
        airForward = 3,
        airDown = 4,
        largeAoE = 7
    }

    public Upgrades upgrades;

    [Header("Stats")]
    #region
    public float hitPoints = 100;
    public int respawnHitPoints = 100;

    public float mana = 100;
    public int manaLoss = 5;
    public int manaRegen = 5;
    public int minimumMana = 100;
    public int respawnMana = 100;
    public int minManaToTransform = 0;

    public int ultimateCharge = 0;
    [Space]
    #endregion

    [Header("Battle Param")]
    #region
    public float downAttackBumpDistance = 2f;
    public float downAttackBaseVelocity = -5f;

    public int swordDamage = 15;
    public int lazerDamage = 33;

    public float inCombatManaModifier = 2f;

    public int dashManaCost = 10;
    public int attackManaCost = 10;
    public int lazerManaCost = 20;
    
    bool canAttack = true;
    [Space]
    #endregion

    [Header("Param")]
    #region
    public float maxJumpHeight = 4f;
    public float minJumpHeight = 1f;
    [Range(0f, 100f)]
    public float terminalVelocity = 100f;

    [Range(0f, 100f)]
    public float cheatVelocity = 20f; // Player gains less speed once past this value

    [Range(0f, 1f)]
    public float cheatVelocityModifier = 0.2f;

    [Range(0f, 1f)]
    public float doubleJumpVelocityModifier = 1f;
    public float wallSlideMaxVelocity = 3f;
    public float timeToJumpApex = 0.4f;
    public float timeToDoubleJumpApex = 0.4f;

    public float accelerationTimeAirborne = 0.2f; // Time it takes to changes X velocity from one side to another

    [Range(0, 100)]
    public float doubleJumpMaxVelocity = 0;

    //dash related
    public float dashSpeed = 24f;
    public float dashTime = 0.25f;

    //grenade related
    public Grenade grenadeModel; // Insert grenade prefab

    [Header("Wall jump Params")]
    public Vector2 wallJump; //Wall jump force vector
    public Vector2 ledgeJump; //Ledge jump force vector

    public float wallSticktime = .25f;
    public float aerialMoveSpeed = 6f;

    int wallDirX; // Use to check in what direction is the wall the player on when wall sliding
    [Space]
    #endregion

    int direction = 1; // 1 = right
    
    //Physics, calculated from given parameters
    float gravity;
    float maxJumpVelocity;

    float velocityXSmoothing; // Throwaway variable for smoothdampening
    Transform[] childrenTr; //Used when turning

    Vector2 input;
    Vector3 velocity;

    #region auto-property
    public float TimeToRegen { set; get; }
    public float DastTimeLeft { get; set; }
    public float TimeToWallUnstick { get; set; }
    public bool Detected { set; get; } // When seen by ennemies
    public bool TeleportGrenade { get; set; } //grenade check
    public bool InDialogTrigger { set; get; }
    public bool InChangeLayer { set; get; }
    public bool WallSliding { set; get; }
    public bool CanMove { private set; get; }

    public PlayerMode CurrentMode { private set; get; } // Light mode or shadow mode

    public Vector2 PlayerInput { private set; get; } // Player input
    public Vector3 Velocity { private set { velocity = value;} get{ return velocity; } } //Current player velocity
    public LayerMask EnemyLayer { get; set; }
    public ContactFilter2D AttackFilter { set; get; }

    Rewired.Player RewiredInput { set; get; }
    Coroutine ManaCoroutine { set; get; }
    Coroutine DamageZoneCoroutine { set; get;}
    ParticleSystem TrailFX { set; get; } 
    ParticleSystem BubbleFX { set; get; }

    //gravityZone related
    public float GravModifier { set; get; }
    public bool InGravity { get; set; }
    public PlayerMode LastGravityZoneMode { private set; get; }
    public PlayerMode CurrentGravityZone { private set; get; }
    #endregion

    #region Components
    [HideInInspector] public Controller2D controller;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Transform tr;
    [HideInInspector] public BoxCollider2D boxCollider;
    [HideInInspector] public CapsuleCollider2D capsuleCollider;
    [HideInInspector] public CircleCollider2D circleCollider;
    public PlayerAnimationController playerAnimationController;
    #endregion

    #region State Machine
    [HideInInspector] public Player_sBase currentState;
    [HideInInspector] public Player_sIdle idleState;
    [HideInInspector] public Player_sJump jumpState;
    [HideInInspector] public Player_sAttack attackState;
    [HideInInspector] public Player_sRangedAttack rangedAttackState;
    [HideInInspector] public Player_sLocomotion locomotionState;
    [HideInInspector] public Player_sDeath deathState;
    [HideInInspector] public Player_sDash dashState;
    [HideInInspector] public Player_sMonkeyBar monkeyLocomotionState;
    [HideInInspector] public Player_sMonkeyBarIdle monkeyIdleState;
    [HideInInspector] public Player_sMonkeyBarGrenade monkeyGrenadeState;
    [HideInInspector] public Player_sGrenade grenadeState;
    [HideInInspector] public Player_sWallrun wallrunState;
    [HideInInspector] public Player_sUltimate ultimateState;
    #endregion

    public AttackCollisionManager attackCollisionManager; //Assign needed collision collider in the inspector

    #region Start

    private void Awake()
    {
        DebugLoadBootstrap();
    }

    private static void DebugLoadBootstrap()
    {
        List<UnityEngine.SceneManagement.Scene> scenes = new List<UnityEngine.SceneManagement.Scene>();
        bool isBootstrapLoaded = false;

        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            scenes.Add(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));
        
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].name == "Bootstrap")
            {
                isBootstrapLoaded = true;
                break;
            }
        }

        if (!isBootstrapLoaded)
            UnityEngine.SceneManagement.SceneManager.LoadScene("Bootstrap", UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }

    private void Start()
    {
        RewiredInput = ReInput.players.GetPlayer(0);
        
        InitComponent();

        Man_GameManager.main.player = this;
        Man_GameManager.main.respawnPosition = tr.position;

        attackCollisionManager.InitCollisionManager();

        playerAnimationController.InitPlayerAnimationController();

        SetPlayerStates();

        CalculatePhysicMetrics();

        SetInitialState();

        EnemyLayer = Man_EnemyManager.Instance.enemyOne;

        AttackFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = EnemyLayer
        };

        if (Man_GameManager.Instance != null)
            upgrades = Man_GameManager.Instance.saveFile.upgrades;

        LastGravityZoneMode = PlayerMode.zero;
        CurrentGravityZone = PlayerMode.zero;

        ManaCoroutine = StartCoroutine(ManageMana());

        upgrades.lightMode = (Man_LevelManager.currentLevel.level != Man_LevelManager.Level.levelA);
        CurrentMode = PlayerMode.shadow;
        CanMove = true;
    }

    private void InitComponent()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        circleCollider = GetComponent<CircleCollider2D>();

        controller = GetComponent<Controller2D>();
        animator = GetComponent<Animator>();
        tr = transform;
        SoundManager.PlayerSoundEmitter = GetComponentInChildren<AkGameObj>().gameObject;
    }

    private void SetInitialState()
    {
        Vector3 testVelocity = new Vector3();
        testVelocity.y += gravity;
        controller.VerticalCollisions(ref testVelocity);

        if (controller.collisions.below)
            currentState = jumpState;
        else
            currentState = idleState;
    }

    [ContextMenu("Recalculate Physics")]
    private void CalculatePhysicMetrics()
    {
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpState.maxJumpVelocity = maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        jumpState.doubleJumpVelocity = maxJumpVelocity * doubleJumpVelocityModifier;

        GravModifier = 1f;
    }

    /// <summary>
    /// Constructs the states of the player, for state machine
    /// </summary>
    private void SetPlayerStates()
    {
        idleState = new Player_sIdle(this);
        locomotionState = new Player_sLocomotion(this);
        
        jumpState = new Player_sJump(this);
        attackState = new Player_sAttack(this);
        rangedAttackState = new Player_sRangedAttack(this,GetComponent<LineRenderer>(), lazerDamage, playerAnimationController.LazerStartAnimationDuration);
        
        deathState = new Player_sDeath(this);
        dashState = new Player_sDash(this);
        monkeyLocomotionState = new Player_sMonkeyBar(this);
        monkeyIdleState = new Player_sMonkeyBarIdle(this);
        monkeyGrenadeState = new Player_sMonkeyBarGrenade(this);
        grenadeState = new Player_sGrenade(this);
        wallrunState = new Player_sWallrun(this, playerAnimationController.DureeWallRunStart, playerAnimationController.WallRunJumpAnimationDuration);
        ultimateState = new Player_sUltimate(this);
    }
    #endregion

    private void Update()
    {
        if (ManaCoroutine == null)
            ManaCoroutine = StartCoroutine(ManageMana());

        //Debug for rewired
        if (RewiredInput == null)
        {
            RewiredInput = ReInput.players.GetPlayer(0);
            return;
        }

        if (CanMove)
            PlayerInput = new Vector2(RewiredInput.GetAxisRaw("LeftJoyX"), RewiredInput.GetAxisRaw("LeftJoyY"));
        else
            PlayerInput = Vector2.zero;

        if (InChangeLayer && PlayerInput.y < 0.95f)
            InChangeLayer = false;

        // Deadzone
        if (PlayerInput.x < 0.1f && PlayerInput.x > -0.1f)
            input.x = 0;

        if (controller.collisions.above || controller.collisions.below)
            velocity.y = 0f;

        if(InGravity)
            CheckGravityZone();

        currentState.Update();
        GetButtonInputs();

        HorizontalMove();
        ApplyGravity();

        controller.ResetController(ref velocity);

        Velocity *= Time.deltaTime;
        currentState.Move(ref velocity);
        Velocity /= Time.deltaTime;

        // Possibilities of NaN since dividing by Time.deltatime, reset velocity if that was the case
        if (Velocity.x != Velocity.x)
            Velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (upgrades.godMode)
        {
            hitPoints = 100;
            mana = 100;
        }

		if (mana < 0)
			mana = 0;

        //Debug turn, player rotation should always be at 0
        if(tr.localEulerAngles.y != 0)
        tr.localEulerAngles = new Vector3(0, 0, 0);
    }

    private void GetButtonInputs()
    {
        if (CanMove)
        {
            if (RewiredInput.GetButtonDown("A") && !InDialogTrigger)
                currentState.ButtonDownA();

            if (RewiredInput.GetButtonUp("A"))
                currentState.ButtonUpA();

            if (RewiredInput.GetButtonDown("X") && canAttack)
                currentState.ButtonDownX();

            if (RewiredInput.GetButtonUp("X"))
                currentState.ButtonUpX();

            if (RewiredInput.GetButtonDown("Y"))
                currentState.ButtonDownY();

            if (RewiredInput.GetButtonUp("Y"))
                currentState.ButtonUpY();

            if (RewiredInput.GetButtonDown("B"))
                currentState.ButtonDownB();

            if (RewiredInput.GetButtonDown("LeftBumper"))
                currentState.BumperDownL();

            if (RewiredInput.GetButtonDown("RightBumper"))
                currentState.BumperDownR();

            if (RewiredInput.GetButtonDown("LeftTrigger"))
                currentState.TriggerDownL();

            if (RewiredInput.GetButtonUp("LeftTrigger"))
                currentState.TriggerUpL();

            if (RewiredInput.GetButtonDown("RightTrigger"))
                currentState.TriggerDownR();

            if (RewiredInput.GetButtonUp("RightTrigger"))
                currentState.TriggerUpR();
        }
    }

    /// <summary>
    /// Int version of StopMovement, for use with animation events
    /// </summary>
    /// <param name="active"></param>
    public void StopMovement(int active)
    {
        CanMove = active == 0;
    }

    public void StopMovement(bool active)
    {
        CanMove = !active;
    }

    public void ResetVelocity()
    {
        Velocity = Vector3.zero;
        velocityXSmoothing = 0f;
    }

    /// <summary>
    /// Delegates information to attack state, called by animation events
    /// </summary>
    /// <param name="attackDirection"></param>
    public void Attack(int attackDirection)
    {
        attackState.AttackResolution((AttackType)attackDirection);
    }

    /// <summary>
    /// Permits use of attacks, called by animation event
    /// </summary>
    public void AttackIsFinished()
    {
        attackState.canAttack = true;
    }



    public Coroutine MoveToXInTime(Vector3 startPos, Vector3 endPos, float time, DelegatedFonction delegatedFonction)
    {
        return StartCoroutine(MoveTo(startPos, endPos, time, delegatedFonction));
    }
    public Coroutine MoveToXInTime(Vector3 startPos, Vector3 endPos, float time, AnimationCurve curve, DelegatedFonction delegatedFonction = null)
    {
        return StartCoroutine(MoveTo(startPos, endPos, time, curve, delegatedFonction));
    }
    IEnumerator MoveTo(Vector3 startPos, Vector3 endPos, float time, DelegatedFonction delegatedFonction)
    {

        float t = 0f;
        float startTime = 0;
        float currentTime = startTime;
        float endTime = time;

        while (currentTime < endTime){

            currentTime += Time.deltaTime;

            t = currentTime / endTime;

            if (t >= 1f)
            {
                if (delegatedFonction != null)
                {
                    delegatedFonction();
                }
                else
                tr.position = endPos;
            }
            tr.position = Vector3.Lerp(startPos, endPos, t);
            yield return new WaitForEndOfFrame();
        }
    }
    IEnumerator MoveTo(Vector3 startPos, Vector3 endPos, float time, AnimationCurve curve, DelegatedFonction delegatedFonction)
    {
        float startTime = 0;
        float currentTime = startTime;
        float endTime = time;

        while (currentTime < endTime)
        {
            currentTime += Time.deltaTime;
            float t = currentTime / endTime;
            float curvePercent = curve.Evaluate(t);

            if (t >= 1f)
            {
                if (delegatedFonction != null)
                    delegatedFonction();
                tr.position = endPos;
            }

            tr.position = Vector3.Lerp(startPos, endPos, curvePercent);
            yield return new WaitForEndOfFrame();
        }
    }

    public void Hit(int damage)
    {
        if (currentState == deathState)
            return;

        //Immune to damage if dashing
        else if (currentState == dashState)
            return;

        if (damage > 200)
        {
            Death();
            return;
        }

        Man_GameManager.Instance.overlay.ActivateOverlay(UI_Overlay.OverlayMode.hurt);
        Uti_GameEffect.ShakeDynamic(Man_GameManager.Instance.mainCamera.gameObject, 0.5f, 0.1f);

        SpawnHitFX();

        hitPoints -= damage;
        playerAnimationController.SetHurt();

        if (hitPoints <= 0)
            Death();
        

        if (Man_GameManager.main.hud != null)
        {
            Man_GameManager.main.hud.UpdateHpUI(hitPoints);
            Man_GameManager.main.hud.UpdateManaUICurved(mana);
        }

    }
    private void SpawnHitFX()
    {
        Vector3 posFX = tr.position;
        posFX.y += 1f;

        ParticleSystem temp = new ParticleSystem();

        if (CurrentMode == PlayerMode.light)
            temp = Tool_FXPooling.Instance.SpawnFX("HitAvatar", posFX, Quaternion.identity);
        else
            temp = Tool_FXPooling.Instance.SpawnFX("HitEnemyRange", posFX, Quaternion.identity);

        temp.transform.right = -tr.right;
    }

    public void DamageOverTime(int damage)
    {
        if (DamageZoneCoroutine == null)
        {
            if (upgrades.DoTresist && CurrentMode == PlayerMode.light) {
                Tool_FXPooling.Instance.SpawnFX("BubbleStart", playerAnimationController.spine1.transform.position, Quaternion.identity, playerAnimationController.spine1);
                BubbleFX = Tool_FXPooling.Instance.SpawnFX("BubbleLoop", playerAnimationController.spine1.transform.position, Quaternion.identity, playerAnimationController.spine1);
                SoundManager.PlayInAvatar(SoundManager.SoundName.shieldCast);
            }
            SoundManager.PlayInAvatar(SoundManager.SoundName.damageZone);
            DamageZoneCoroutine = StartCoroutine(DamageOverTimes(damage));
        }
    }

    IEnumerator DamageOverTimes(int damage)
    {
        while (true)
        {
            if (!(upgrades.DoTresist && CurrentMode == PlayerMode.light))
                Hit(damage);

            else
                BubbleManagement(true);

            yield return new WaitForSeconds(1f);
            if (!InDamageZone())
                break;
        }
        DamageZoneCoroutine = null;

        if (upgrades.DoTresist)
            BubbleManagement(true);
    }

    void BubbleManagement(bool activate)
    {
        if (activate)
        {
            if (BubbleFX != null)
                return;

            Tool_FXPooling.Instance.SpawnFX("BubbleStart", playerAnimationController.spine1.transform.position, Quaternion.identity, playerAnimationController.spine1);
            BubbleFX = Tool_FXPooling.Instance.SpawnFX("BubbleLoop", playerAnimationController.spine1.transform.position, Quaternion.identity, playerAnimationController.spine1);
            SoundManager.PlayInAvatar(SoundManager.SoundName.shieldCast);
        }

        else
        {
            if (BubbleFX == null)
                return;

            Tool_FXPooling.Instance.SpawnFX("BubbleStop", playerAnimationController.spine1.transform.position, Quaternion.identity, playerAnimationController.spine1);
            Tool_FXPooling.Instance.ForcedReturnToPool("BubbleLoop", BubbleFX);
            SoundManager.PlayInAvatar(SoundManager.SoundName.shieldRelease);
            BubbleFX = null;
        }
    }

    /// <summary>
    /// Checks for damage zones with the capsule colliders and returns true if there is any 
    /// </summary>
    /// <returns></returns>
    bool InDamageZone()
    {
        BoxCollider2D[] spike = new BoxCollider2D[10];

        ContactFilter2D contact = new ContactFilter2D
        {
            useTriggers = true
        };

        int a = capsuleCollider.OverlapCollider(contact, spike);
        List<BoxCollider2D> targets = new List<BoxCollider2D>(spike);
        targets.RemoveRange(a, 10 - a);

        foreach (BoxCollider2D box in targets)
        {
            Ing_DamageZone damageZone = box.GetComponent<Ing_DamageZone>();

            if (damageZone != null)
                return true;

            Ing_EnemyDamageZone temp = box.GetComponent<Ing_EnemyDamageZone>();

            if (temp != null)
                return true;
        }

        //Then not in a damage zone
        SoundManager.Instance.Stop(SoundManager.PlayerSoundEmitter);
        return false;
    }

    public void HeadButt(Vector3 direction, Vector2 impulsion)
    {
        if (currentState == idleState || currentState == locomotionState)
        {
            currentState.ButtonDownA();
            playerAnimationController.RemoveRootMotion();
            tr.Translate(new Vector3(0, 1f));
            ResetVelocity();
            velocity.x = impulsion.x * direction.x;
            velocity.y = impulsion.y;
        }
    }

    public void Death()
    {
        StopMovement(true);

        Man_GameManager.main.mainCamera.cinematic = true;

        currentState.ToDeath();
        Man_GameManager.main.mainCamera.isFixed = false;
        Man_GameManager.DeathTimeScale();

        SoundManager.PlayInAvatar(SoundManager.SoundName.playerDeath);

        playerAnimationController.SetDeath();
        playerAnimationController.Dissolve(true);
    }

    public void Respawn()
    {
        if (Man_GameManager.Instance.respawnDepth != controller.CurrentLayerDepth)
        {
            controller.ChangeLayerDepth(Man_GameManager.Instance.respawnDepth, Man_GameManager.Instance.respawnPosition);
            Man_GameManager.main.player.EnemyLayer = (Man_GameManager.Instance.respawnDepth == LayerDepth.front) ? Man_EnemyManager.Instance.enemyOne : Man_EnemyManager.Instance.enemyTwo;
        }
        else
            tr.position = Man_GameManager.Instance.respawnPosition;
        
        Man_LevelManager.currentLevel.DeathReset();

        Man_GameManager.main.mainCamera.cinematic = false;

        hitPoints = respawnHitPoints;

        if(upgrades.lightMode)
            mana = respawnMana;
        playerAnimationController.Dissolve(false);
        SoundManager.PlayInAvatar(SoundManager.SoundName.respawn);
    }

    /// <summary>
    /// Creates dust and footstep sounds. Called by animation event
    /// </summary>
    public void LeftFootDust()
    {
        Tool_FXPooling.Instance.SpawnFX("PuffSmoke", playerAnimationController.leftFoot.transform.position, Quaternion.identity);
        SoundManager.PlayInAvatar(SoundManager.SoundName.footstep);
    }

    /// <summary>
    /// Creates dust and footstep sounds. Called by animation event
    /// </summary>
    public void RightFootDust()
    {
        Tool_FXPooling.Instance.SpawnFX("PuffSmoke", playerAnimationController.rightFoot.transform.position, Quaternion.identity);
        SoundManager.PlayInAvatar(SoundManager.SoundName.footstep);
    }

    /// <summary>
    /// Checks if player capsule collider is in a gravity zone and applies gravity modifier is neccessary
    /// </summary>
    void CheckGravityZone()
    {
        ContactFilter2D contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = int.MaxValue ^ 1 << 13,
            useTriggers = true
        };

        PolygonCollider2D[] contacts = new PolygonCollider2D[10];

        int a = capsuleCollider.OverlapCollider(contactFilter, contacts);

        List<PolygonCollider2D> targets = new List<PolygonCollider2D>(contacts);
        targets.RemoveRange(a, 10 - a);

        bool inGravityZone = false;
        foreach (PolygonCollider2D polyColl in targets)
        {
            Ing_GravityZone gz = polyColl.GetComponent<Ing_GravityZone>();

            if (gz != null)
            {

                CurrentGravityZone = gz.onlyAffect;
                if (CurrentMode == gz.onlyAffect)
                {
                    if (LastGravityZoneMode != CurrentMode)
                    {
                        Man_GameManager.Instance.overlay.ActivateOverlay((CurrentMode == PlayerMode.light) ? UI_Overlay.OverlayMode.gravityZoneLight : UI_Overlay.OverlayMode.gravityZoneShadow);
                        LastGravityZoneMode = CurrentMode;
                    }
                    ChangeGravityModifier(gz.gravityModifier, gz.onlyAffect);
                    return;
                }
                inGravityZone = true;
            }
        }

        if (!inGravityZone)
        {
            InGravity = false;
            LastGravityZoneMode = PlayerMode.zero;
            CurrentGravityZone = PlayerMode.zero;
        }
        Man_GameManager.Instance.overlay.DeactivateOverlay();
        ResetGravityModifier();
    }

    /// <summary>
    /// Applies jump velocity to current velocity vector.
    /// </summary>
    public void Jump()
    {
        velocityXSmoothing = 0;
        velocity.x = 7f * PlayerInput.x;
        velocity.y = maxJumpVelocity;
    }

    /// <summary>
    /// Jump with a different force vector beased on proximity to a wall and input direction
    /// </summary>
    public void WallJump()
    {
        velocity.x = -wallDirX * wallJump.x;
        velocity.y = wallJump.y;
    }
    public void WallJump(Vector2 velocity)
    {
        this.velocity.x = -wallDirX * velocity.x;
        this.velocity.y = velocity.y;
    }

    public void LedgeJump()
    {
        SoundManager.PlayInAvatar(SoundManager.SoundName.jump);
        playerAnimationController.IsLedgeGrabbing();
        velocity.x = direction * ledgeJump.x;
        velocity.y = ledgeJump.y;
    }

    /// <summary>
    /// Handles the horizontal movement when without root motion
    /// </summary>
    public void HorizontalMove()
    {
        if (currentState != attackState)
        {
            if (PlayerInput.x != 0)
            {
                float targetVelocityX = PlayerInput.x * aerialMoveSpeed;
                velocity.x = Mathf.SmoothDamp(Velocity.x, targetVelocityX, ref velocityXSmoothing, accelerationTimeAirborne);
            }

            else if (Mathf.Abs(Velocity.x) > aerialMoveSpeed / 2)
            {
                float targetVelocityX = direction * (aerialMoveSpeed / 2);
                velocity.x = Mathf.SmoothDamp(Velocity.x, targetVelocityX, ref velocityXSmoothing, accelerationTimeAirborne);
            }
        }

        else if (currentState == attackState)
        {
            velocity.x = PlayerInput.x * 7f;
        }
    }

    /// <summary>
    /// Apply gravity on current velocity vector.
    /// </summary>
    public void ApplyGravity()
    {
        if (Mathf.Abs(Velocity.y) <= Mathf.Abs(terminalVelocity))
        {
            if(Velocity.y > -cheatVelocity)
                velocity.y += gravity * GravModifier * Time.deltaTime;

            else
                velocity.y += (gravity * cheatVelocityModifier) * GravModifier * Time.deltaTime;
        }
    }

    public void Wallrun()
    {
        playerAnimationController.WallRun();
        Vector2 raycastOrigin = tr.position;
        raycastOrigin.y += 1f;

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.right * direction, 1f, controller.currentCollisionMask);

        Vector3 endPos = tr.position;
        endPos.x = hit.point.x - (0.5f * direction);
        endPos.y += 0.5f;

        MoveToXInTime(tr.position, endPos, playerAnimationController.DureeWallRunStart, () => playerAnimationController.ApplyRootMotion());
    }

    /// <summary>
    /// Turns the character.
    /// </summary>
    public void Turn(bool inAir)
    {
        float time = (inAir) ? 0 : playerAnimationController.TurnAnimationDuration;

        playerAnimationController.RemoveRootMotion();
        StartCoroutine(TurnWithTime(time));
    }

    public void Turn()
    {
        ExecuteWithoutRootMotion(() => StartCoroutine(TurnWithTime(0)));
    }

    /// <summary>
    /// Turns the character in given time.
    /// </summary>
    public IEnumerator TurnWithTime(float duration)
    {
        canAttack = false;

        if(PlayerInput.x != 0)
            direction = (int)Mathf.Sign(PlayerInput.x);
        else
            direction = -direction;


        float timePassed = 0;
        float t = 0;

        float startAngle = (direction == -1) ? 0 : -180f;
        float endAngle = startAngle - 180f;

        float currentAngle = 0;

        if (direction != 0 && duration > 0)
            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                t = timePassed / duration;

                currentAngle = Mathf.Lerp(startAngle, endAngle, t);
                RotateChildren(new Vector3(0, currentAngle, 0));
                yield return null;
            }

        RotateChildren(new Vector3(0, (startAngle == 0) ? -180 : 0, 0));
        playerAnimationController.ApplyRootMotion();
        canAttack = true;
    }
    void RotateChildren(Vector3 rotation)
    {
        if(childrenTr == null)
        {
            childrenTr = new Transform[1];
            childrenTr[0] = tr.GetChild(0);
        }

        for (int i = 0; i < childrenTr.Length; i++)
        {
            tr.localEulerAngles = Vector3.zero;
            childrenTr[i].localEulerAngles = rotation;
        }
        attackCollisionManager.RotateCollider();
    }

    /// <summary>
    /// Exectutes a specified fonction with the root motion unaplied.
    /// </summary>
    /// <param name="delegatedFonction"></param>
    public void ExecuteWithoutRootMotion(DelegatedFonction delegatedFonction)
    {
        if (animator.applyRootMotion)
        {
            playerAnimationController.RemoveRootMotion();
            delegatedFonction();
            playerAnimationController.ApplyRootMotion();
        }
        else
            delegatedFonction();
    }

    private void ForceChangeMode()
    {
        playerAnimationController.ForceTransformation();
        CurrentMode = PlayerMode.shadow;

        if (Man_GameManager.main.hud != null)
            Man_GameManager.main.hud.lightFX.enabled = false;

        Man_GameManager.Instance.overlay.DeactivateOverlay();
        SoundManager.PlayInAvatar(SoundManager.SoundName.transformShadow);
        Tool_FXPooling.Instance.SpawnFX("ShadowTransformation", playerAnimationController.spine1.transform.position, Quaternion.identity, playerAnimationController.spine1);

        if (TrailFX != null)
        {
            Tool_FXPooling.Instance.ForcedReturnToPool("LightTrail", TrailFX);
            TrailFX = null;
        }

        playerAnimationController.ChangeEmission(false);
    }

    /// <summary>
    /// Changes the player mode, enabling new habilities and disabling others
    /// </summary>
    public void ChangeMode()
    {
        if (mana < minManaToTransform && CurrentMode == PlayerMode.light)
            return;
        
        if (!upgrades.lightMode)
            return;

        bool isLight = (CurrentMode == PlayerMode.light);
        CurrentMode = (isLight) ? PlayerMode.shadow : PlayerMode.light;
        playerAnimationController.IsTransforming = (CurrentMode == PlayerMode.light);

        Man_GameManager.Instance.overlay.DeactivateOverlay();

        if (!isLight)
        {
            if (TrailFX == null)
                TrailFX = Tool_FXPooling.Instance.SpawnFX("LightTrail", playerAnimationController.spine1.transform.position, Quaternion.identity, playerAnimationController.spine1);
            
            Tool_FXPooling.Instance.SpawnFX("LightTransformation", tr.position, Quaternion.identity, this.gameObject);
            SoundManager.PlayInAvatar(SoundManager.SoundName.transformLight);
        }
        else
        {
            if (TrailFX != null)
            {
                Tool_FXPooling.Instance.ForcedReturnToPool("LightTrail", TrailFX);
                TrailFX = null;
            }

            Tool_FXPooling.Instance.SpawnFX("ShadowTransformation", playerAnimationController.spine1.transform.position, Quaternion.identity, playerAnimationController.spine1);
            SoundManager.PlayInAvatar(SoundManager.SoundName.transformShadow);
        }

        playerAnimationController.ChangeEmission(!isLight);

        if (InGravity)
            Man_GameManager.Instance.overlay.ActivateOverlay((CurrentMode == PlayerMode.light) ? UI_Overlay.OverlayMode.gravityZoneLight : UI_Overlay.OverlayMode.gravityZoneShadow);

        if (Man_GameManager.main.hud != null)
            Man_GameManager.main.hud.lightFX.enabled = !isLight;
    }

    public void StartManaManagement()
    {
        StartCoroutine(ManageMana());
    }

    /// <summary>
    /// Manages mana regeneration
    /// </summary>
    /// <returns></returns>
    IEnumerator ManageMana()
    {
        while (true){
            yield return null;
            TimeToRegen += Time.deltaTime;

            if (CurrentMode == PlayerMode.light)
            {
                mana -= manaLoss * Time.deltaTime;

                if (mana <= 0)
                {
                    ForceChangeMode();
                }
            }

            if (mana < minimumMana)
            {
                mana += ((Detected)? manaRegen / inCombatManaModifier : manaRegen)* Time.deltaTime;
                if (Man_GameManager.main.hud != null)
                    Man_GameManager.main.hud.UpdateManaUI(mana);
            }
        }
    }

    public void ManageUltimate(int i)
    {
        ultimateCharge += i;

        if (ultimateCharge >= 100)
        {
            ultimateCharge = 100;
            Man_GameManager.Instance.hud.ActivateUltimateFeedback();
        }

        Man_GameManager.Instance.hud.UpdateUltimateUICurved(ultimateCharge);
    }

    public void ChangeGravityModifier(float gravModifier, PlayerMode ifInThisMode)
    {
        if(ifInThisMode == CurrentMode)
            GravModifier = gravModifier;
    }

    public void ResetGravityModifier()
    {
        GravModifier = 1f;
    }

    public void ResetYVelocity()
    {
        velocity.y = 0f;
    }

    //Scripted event sound calls
    public void PlaySound(int i)
    {
        switch (i)
        {
            case 1:
                SoundManager.PlayInAvatar(SoundManager.SoundName.footstep);
                break;
            case 2:
                SoundManager.PlayInAvatar(SoundManager.SoundName.dash);
                break;
            case 3:
                SoundManager.PlayInAvatar(SoundManager.SoundName.meleeSwoosh01);
                break;
            case 4: //miroir apparait
                break;
            default:
                break;
        }
    }

    #region Accesor
    public float Mana
    {
        get { return mana; }
        set
        {
            mana = value;

            if (mana > 100)
                mana = 100;

            if (mana < 0)
                mana = 0;

            Man_GameManager.main.hud.UpdateManaUI((int)mana);
        }
    }

    public int UltimateCharge
    {
        get { return ultimateCharge; }
        set{
            ultimateCharge = value;

            if (ultimateCharge > 100)
                ultimateCharge = 100;

            Man_GameManager.Instance.hud.UpdateUltimateUI(ultimateCharge);
        }
    }

    public int WallDirX
    {
        get { return wallDirX; }

        set
        {
            wallDirX = (int)Mathf.Sign(value);
        }
    }

    public int Direction
    {
        get { return direction; }

        set
        {
            direction = (int)Mathf.Sign(value);
        }
    }
    #endregion


    /// <summary>
    /// Do not use, can not delete because of existing animation event
    /// </summary>
    public void StartAOE()
    {
        //Deprecated, exists because of undeleted animation event
    }

    /// <summary>
    /// Do not use, can not delete because of existing animation event
    /// </summary>
    public void ComboIsFinished() { }
}
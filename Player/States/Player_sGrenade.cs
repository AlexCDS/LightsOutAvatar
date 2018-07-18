using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class Player_sGrenade : Player_sBase {

    #region variables
    private Grenade grenade;

    public float angleTurnSpeed = 0.05f;
    private float playerAngle = 45f * Mathf.Deg2Rad;
    public float projectileSpeed = 5f;
    public float velocityModifier = 10f;
    [SerializeField] private int resolution = 50;
    public float gravity = 3.5f;
    private float timeSinceLaunch;
    private Vector2 velocity;
    private Vector2 position;
    private float decal;
    private float maxAngle = 1.45f;
    private bool preparingToShoot = false;
    private Rewired.Player rightAxis;

    ParticleSystem ps;
    ParticleSystem psTeleport;


    #endregion

    [SerializeField]public bool canGrenade = true;

    private LineRenderer lineRenderer;
    List<Vector2> throwProjectionList = new List<Vector2>();

    public override void ToDeath()
    {
        base.ToDeath();
        lineRenderer.enabled = false;
        master.playerAnimationController.SetFlashBombMode(false);
        Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeInHand", ps);
        preparingToShoot = false;

        master.playerAnimationController.SetFlashBombAway(true);
    }

    public override void ToIdle()
    {
        base.ToIdle();
        lineRenderer.enabled = false;
        master.playerAnimationController.SetFlashBombMode(false);
        Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeInHand", ps);
        preparingToShoot = false;
        //if (teleportGrenade)
        //{
        //    psTeleport = Tool_FXPooling.Instance.SpawnFX("GrenadeTeleport", master.transform.position, Quaternion.identity);
        //}
        master.playerAnimationController.SetFlashBombAway(true);
    }


    // Update is called once per frame
    public override void Update()
    {
        if (rightAxis == null)
        {
            rightAxis = ReInput.players.GetPlayer(0);
        }

        if (grenade != null)
        {
            preparingToShoot = true;
        }

        if (grenade == null && !preparingToShoot)
        {
            ps = Tool_FXPooling.Instance.SpawnFX("GrenadeInHand", master.playerAnimationController.rightHand.transform.position, Quaternion.identity, master.playerAnimationController.rightHand);
            preparingToShoot = true;
        }

        if (grenade == null)
        {
            lineRenderer.enabled = true;
        }
        lineRenderer.widthMultiplier = 0.4f;
        ChangePlayerAngle();
        Aim();

        master.playerAnimationController.SetFlashBombAngle = playerAngle / maxAngle;


        /*
        for (int i = 0; i < throwProjectionList.Count; i++)
        {
            lineRenderer.SetPosition(i, throwProjectionList[i]);

        }*/
    }

    public override void Move(ref Vector3 velocity)
    {
        if (master.Direction != Mathf.Sign(master.PlayerInput.x) && master.PlayerInput.x != 0)
        {
            master.Direction = (int)Mathf.Sign(master.PlayerInput.x);
            master.Turn();
            return;
        }
        else if (master.Direction != Mathf.Sign(master.PlayerInput.x) && master.PlayerInput.x != 0)
        {
            master.Direction = (int)Mathf.Sign(master.PlayerInput.x);
            return;
        }

        master.playerAnimationController.SetHSpeed(Mathf.Abs(master.PlayerInput.x));
        CollisionsCheck();
        master.controller.VerticalCollisions(ref velocity);
        if (!master.controller.collisions.below)
        {
            master.playerAnimationController.SetFlashBombMode(false);
            Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeInHand", ps);
            preparingToShoot = false;
            lineRenderer.enabled = false;
            ToJump();
            return;
        }
        if (CollisionBehaviour(ref velocity))
            return;

    }

    public override void TriggerUpL()
    {
        if (!master.TeleportGrenade)
        {
            Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeInHand", ps);

            preparingToShoot = false;
            if (grenade == null)
            {
                grenade = MonoBehaviour.Instantiate(master.grenadeModel, master.playerAnimationController.rightHand.transform.position, master.tr.rotation);
                master.playerAnimationController.SetFlashBombAway(true);

                grenade.IsTeleport = master.TeleportGrenade;
                grenade.PlayerAngle = playerAngle;
                grenade.ProjectileSpeed = projectileSpeed;
                grenade.VelocityModifier = velocityModifier;
                grenade.Gravity = gravity;
                grenade.Direction = master.Direction;
                grenade.initialPosition = master.playerAnimationController.rightHand.transform.position;

                master.playerAnimationController.SetFlashBombThrow();
                grenade.destroyEvent.AddListener(() => GrenadeDestroy());
                SoundManager.PlayInAvatar(SoundManager.SoundName.flashbombThrow);
            }
            ToIdle();
        }
    }

    public override void TriggerUpR()
    {
        if (master.TeleportGrenade)
        {
            Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeInHand", ps);

            preparingToShoot = false;
            if (grenade == null)
            {
                grenade = MonoBehaviour.Instantiate(master.grenadeModel, master.playerAnimationController.rightHand.transform.position, master.tr.rotation);
                master.playerAnimationController.SetFlashBombAway(true);

                grenade.IsTeleport = master.TeleportGrenade;
                grenade.PlayerAngle = playerAngle;
                grenade.ProjectileSpeed = projectileSpeed;
                grenade.VelocityModifier = velocityModifier;
                grenade.Gravity = gravity;
                grenade.Direction = master.Direction;
                grenade.initialPosition = master.playerAnimationController.rightHand.transform.position;

                master.playerAnimationController.SetFlashBombThrow();
                grenade.destroyEvent.AddListener(() => GrenadeDestroy());
                SoundManager.PlayInAvatar(SoundManager.SoundName.flashbombThrow);
            }
            ToIdle();
        }
    }

    //Methods
    #region
    //sets a ballistic curve based on gravity, projectile speed and player angle
    public void GrenadeDestroy()
    {
        canGrenade = false;
        master.Invoke("CanGrenadeAgain", 2f);
    }

    public void Aim()
    {
        throwProjectionList.Clear();
        lineRenderer.positionCount = 0;
        
        for (int i = 0; i < resolution; i++)
        {
            float t =  ((float)i / resolution) * velocityModifier;
            float x = ((master.playerAnimationController.rightHand.transform.position.x) + (t * projectileSpeed * Mathf.Cos(playerAngle) * Man_GameManager.main.player.Direction));
            float y = (master.playerAnimationController.rightHand.transform.position.y) + (projectileSpeed * Mathf.Sin(playerAngle) * t) - (t*t * gravity * 0.5f); 
            Vector3 pos = new Vector3(x,y, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.back) ? Man_LevelManager.currentLevel.zDepthBack : Man_LevelManager.currentLevel.zDepthFront);

            throwProjectionList.Add(pos);
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(i, pos);
            
            if (i > 1)
            {

                //red cross to show where impact point will be
                #region
                RaycastHit2D hit = Physics2D.Raycast(throwProjectionList[i - 1], throwProjectionList[i] - throwProjectionList[i - 1], Vector3.Distance(throwProjectionList[i - 1], throwProjectionList[i]), (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.layerFront : Man_LevelManager.currentLevel.layerBack);
                if(hit)
                {
                    
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(hit.point.x, hit.point.y, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack));

                    Vector3 line = new Vector3(0f, 0f, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack);
                    line.x = hit.point.x - 1;
                    line.y = hit.point.y;
                    Vector3 line2 = new Vector3(0f, 0f, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack);
                    line2.x = hit.point.x + 1;
                    line2.y = hit.point.y;

                    line.x = hit.point.x;
                    line.y = hit.point.y -1 ;

                    line2.x = hit.point.x;

                    break;
                }
                #endregion
            }
        }
        
    }
    #endregion

    //changes player angle per frame based on input
    public void ChangePlayerAngle()
    {
        playerAngle -= rightAxis.GetAxisRaw("RightJoyX") * angleTurnSpeed * master.Direction;
        if(Mathf.Abs(playerAngle) > 90f * Mathf.Deg2Rad)
        {
            playerAngle = 90f * Mathf.Deg2Rad;
        }
        if (Mathf.Abs(playerAngle) < 45f * Mathf.Deg2Rad)
        {
            playerAngle = 45f * Mathf.Deg2Rad;
        }

        /* Alternative aiming method
        Vector2 temp = new Vector2(master.GetInput.x, master.GetInput.y);
        if(temp != Vector2.zero)
        {
            playerAngle = Mathf.Lerp(playerAngle, (Mathf.Deg2Rad * Vector2.SignedAngle(Vector2.right, temp.normalized)), 0.13f);
        }
        else
        {
            playerAngle = Mathf.Lerp(playerAngle, 45f * Mathf.Deg2Rad, 0.13f);
        }
        */
    }

    public void TeleportToGrenade()
    {
        if (grenade != null)
        {
            decal = grenade.CanTeleport();
            if (grenade.isOkay)
            {
                Vector3 tempAvatar = grenade.transform.position;
                tempAvatar.x += decal;
                tempAvatar.y -= 0.1f;
                master.tr.position = tempAvatar;
                master.playerAnimationController.SetFlashBombBlink();
                Tool_FXPooling.Instance.SpawnFX("GrenadeTeleport", master.tr.position);
                ToIdle();
            }
        }
    }

    private void CollisionsCheck()
    {
        Vector3 animationVelocity = new Vector3((7f * Time.deltaTime) * master.Direction, 0f, 0f);
        master.controller.HorizontalCollisions(ref animationVelocity);
    }

    private bool CollisionBehaviour(ref Vector3 velocity)
    {
        if (master.controller.collisions.left || master.controller.collisions.right)
        {
            ToIdle();
            ResetVelocity(ref velocity);
        }
        return false;
    }

    private void ResetVelocity(ref Vector3 velocity)
    {
        velocity = Vector3.zero;
    }

    public Player_sGrenade(Player master) : base(master)
    {
        this.master = master;
        lineRenderer = master.GetComponent<LineRenderer>();
    }
}

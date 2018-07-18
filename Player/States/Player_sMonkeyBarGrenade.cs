using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

/********************************************************************* 
 * By Alex
 * Monkey bar - Grenade state of the Player script. See Player_sState for more details
*********************************************************************/

public class Player_sMonkeyBarGrenade : Player_sBase
{
    #region variables
    private Grenade grenade;

    public float angleTurnSpeed = 0.1f;
    private float playerAngle = 1f * Mathf.Deg2Rad;
    public float projectileSpeed = 5f;
    public float velocityModifier = 10f;
    [SerializeField] private int resolution = 50;
    public float gravity = 3.5f;
    private float timeSinceLaunch;
    private Vector2 velocity;
    private Vector2 position;
    private float decal;
    private float maxAngle = -1.45f;
    private Rewired.Player rightAxis;

    private bool preparingToShoot = false;

    ParticleSystem ps;
    ParticleSystem psTeleport;


    #endregion

    [SerializeField] public bool canGrenade = true;

    private LineRenderer lineRenderer;
    List<Vector2> throwProjectionList = new List<Vector2>();

    public override void ToMonkeyBarIdle()
    {
        base.ToMonkeyBarLocomotion();
        lineRenderer.enabled = false;
        master.playerAnimationController.SetFlashBombMode(false);
        Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeInHand", ps);
        preparingToShoot = false;
        //if (teleportGrenade)
        //{
        //    psTeleport = Tool_FXPooling.Instance.SpawnFX("GrenadeTeleport", master.transform.position, Quaternion.identity);
        //}
        master.playerAnimationController.SetFlashBombAway(false);
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
        master.playerAnimationController.SetFlashBombAway(false);
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
    }

    public void GrenadeDestroy()
    {
        canGrenade = false;
        master.Invoke("CanGrenadeAgain", 2f);
    }

    public override void TriggerUpL()
    {
        if (!master.TeleportGrenade)
        {
            preparingToShoot = false;
            Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeInHand", ps);
            Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeTeleport", psTeleport);

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
            }
            ToMonkeyBarIdle();
        }
    }

    public override void TriggerUpR()
    {
        if (master.TeleportGrenade)
        {
            preparingToShoot = false;
            Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeInHand", ps);
            Tool_FXPooling.Instance.ForcedReturnToPool("GrenadeTeleport", psTeleport);

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
            }
            ToMonkeyBarIdle();
        }
    }

    #region Methods
    //sets a ballistic curve based on gravity, projectile speed and player angle
    public void Aim()
    {
        throwProjectionList.Clear();
        lineRenderer.positionCount = 0;

        for (int i = 0; i < resolution; i++)
        {
            float t = ((float)i / resolution) * velocityModifier;
            float x = ((master.playerAnimationController.rightHand.transform.position.x) + (t * projectileSpeed * Mathf.Sin(playerAngle) * Man_GameManager.main.player.Direction));
            float y = (master.playerAnimationController.rightHand.transform.position.y) + (projectileSpeed * Mathf.Cos(playerAngle) * t) - (t * t * gravity * 0.5f);
            Vector3 pos = new Vector3(x, y, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.back) ? Man_LevelManager.currentLevel.zDepthBack : Man_LevelManager.currentLevel.zDepthFront);

            throwProjectionList.Add(pos);
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(i, pos);


            if (i > 1)
            {
                //Debug.DrawLine(throwProjectionList[i - 1], throwProjectionList[i], Color.red);

                //red cross to show where impact point will be
                #region
                RaycastHit2D hit = Physics2D.Raycast(throwProjectionList[i - 1], throwProjectionList[i] - throwProjectionList[i - 1], Vector3.Distance(throwProjectionList[i - 1], throwProjectionList[i]), (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.layerFront : Man_LevelManager.currentLevel.layerBack);
                if (hit)
                {

                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(hit.point.x, hit.point.y, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack));

                    Vector3 line = new Vector3(0f, 0f, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack);
                    line.x = hit.point.x - 1;
                    line.y = hit.point.y;
                    Vector3 line2 = new Vector3(0f, 0f, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack);
                    line2.x = hit.point.x + 1;
                    line2.y = hit.point.y;

                    line.x = hit.point.x;
                    line.y = hit.point.y - 1;

                    line2.x = hit.point.x;
                    line2.y = hit.point.y + 1;

                    //Debug.Log(hit.point);
                    break;
                }
                #endregion
            }
        }

    }

    //changes player angle per frame based on input
    public void ChangePlayerAngle()
    {
        playerAngle -= rightAxis.GetAxisRaw("RightJoyX") * angleTurnSpeed * master.Direction;
        if (Mathf.Abs(playerAngle) > 290f * Mathf.Deg2Rad)
        {
            playerAngle = 290f * Mathf.Deg2Rad;
        }
        if (Mathf.Abs(playerAngle) < 70f * Mathf.Deg2Rad)
        {
            playerAngle = 70f * Mathf.Deg2Rad;
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
    #endregion

    public void TeleportToGrenade()
    {
        if (grenade != null)
        {
            decal = grenade.CanTeleport();
            if (grenade.isOkay)
            {
                Vector3 tempAvatar = grenade.transform.position;
                tempAvatar.x += decal;
                master.tr.position = tempAvatar;
                master.playerAnimationController.SetFlashBombBlink();

                ToIdle();
            }
        }
    }

    public Player_sMonkeyBarGrenade(Player master) : base(master)
    {
        this.master = master;
        lineRenderer = master.GetComponent<LineRenderer>();
    }
}

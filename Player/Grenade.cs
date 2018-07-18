using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Grenade : MonoBehaviour {

    public LayerMask layerFront;
    public LayerMask layerBack;
    public LayerMask explosionMaskFront;
    public LayerMask explosionMaskBack;

    public UnityEvent destroyEvent;


    private float playerAngle;
    private float projectileSpeed;
    private float velocityModifier;
    private float gravity;
    private float direction;
    private bool isTeleport;
    private float resolution = 50f;
    public Vector3 initialPosition;
    private float i = 0;
    public bool isColliding = false;
    private float playerBoxLenght;
    private float playerBoxHeight;
    public bool isOkay;
    private Ing_PlatformController myPlatform;
    private ParticleSystem ps;
    private Player player;

    //accessors
    #region
    public bool IsTeleport
    {
        get { return isTeleport; }

        set { isTeleport = value; }
    }

    public float PlayerAngle
    {
        get { return playerAngle; }

        set { playerAngle = value; }
    }

    public float Direction
    {
        get { return direction; }

        set { direction = value; }
    }

    public float ProjectileSpeed
    {
        get { return projectileSpeed; }

        set{ projectileSpeed = value; }
    }

    public float VelocityModifier
    {
        get { return velocityModifier; }

        set { velocityModifier = value; }
    }

    public float Gravity
    {
        get { return gravity; }

        set { gravity = value; }
    }
    #endregion

    // Use this for initialization
    void Start () {
        player = Man_GameManager.main.player;
        playerBoxLenght = Man_GameManager.main.player.GetComponent<Collider2D>().bounds.size.x * 0.5f;
        playerBoxHeight = Man_GameManager.main.player.GetComponent<Collider2D>().bounds.size.y - 0.25f;
        ps = Tool_FXPooling.Instance.SpawnFX("Grenade", initialPosition, Quaternion.identity, this.gameObject);
        Invoke("SelfDestruct", 3f);
    }

    // Update is called once per frame
    void Update()
    {
        

        //follow the curve
        if (!isColliding)
        {
            float t = ((float)i / resolution) * velocityModifier;
            float t2 = (((float)i + 0.5f) / resolution) * velocityModifier;
            float x = ((initialPosition.x) + (t * projectileSpeed * ((player.currentState == player.monkeyLocomotionState) ? Mathf.Sin(playerAngle) : Mathf.Cos(playerAngle)) * Direction));
            float x2 = ((initialPosition.x) + (t2 * projectileSpeed * ((player.currentState == player.monkeyLocomotionState) ? Mathf.Sin(playerAngle) : Mathf.Cos(playerAngle)) * Direction));
            float y = (initialPosition.y) + (projectileSpeed * ((player.currentState == player.monkeyLocomotionState) ? Mathf.Cos(playerAngle) : Mathf.Sin(playerAngle)) * t) - (t * t * gravity * 0.5f);
            float y2 = (initialPosition.y) + (projectileSpeed * ((player.currentState == player.monkeyLocomotionState) ? Mathf.Cos(playerAngle) : Mathf.Sin(playerAngle)) * t2) - (t2 * t2 * gravity * 0.5f);
            Vector3 pos = new Vector3(x, y, Man_GameManager.main.player.tr.position.z);
            Vector3 pos2 = new Vector3(x2, y2, Man_GameManager.main.player.tr.position.z);
            Vector3 mag = pos2 - pos;
            RaycastHit2D circle = Physics2D.CircleCast(pos, 0.15f, (pos2 - pos).normalized, mag.magnitude, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
            if (circle)
            {
                //Ray ray = new Ray(pos, pos2 - pos);
                //this.transform.position = ray.GetPoint(Vector2.Distance(pos2, pos) - circle.distance);

                Vector2 decalTheBall = circle.point;

                float NormalY = Vector2.Dot(circle.normal.normalized, Vector2.down);
                float NormalX = Vector2.Dot(circle.normal.normalized, Vector2.right);

                if (Mathf.Abs(NormalY) == 1)
                {
                    decalTheBall.y -= 0.075f * NormalY;
                }

                if(Mathf.Abs(NormalX) == 1)
                {
                    decalTheBall.x += 0.075f * NormalX;
                }

                myPlatform = circle.collider.GetComponent<Ing_PlatformController>();

                float z = (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.zDepthFront : Man_LevelManager.currentLevel.zDepthBack;
                this.transform.position = new Vector3(decalTheBall.x, decalTheBall.y, z);
                isColliding = true;
                if (IsTeleport)
                {
                    if (Man_GameManager.main.player.currentState == Man_GameManager.main.player.monkeyIdleState)
                    {
                        Man_GameManager.main.player.monkeyGrenadeState.TeleportToGrenade();
                    }
                    else
                    {
                        Man_GameManager.main.player.grenadeState.TeleportToGrenade();
                    }
                }
                else
                {
                    Explode();
                }
            }
            else
            {
                this.transform.position = pos;
            }
            i += 0.5f;
        }

        //on a platform I must move
        if (isColliding && myPlatform != null)
        {
            this.transform.parent = myPlatform.transform;
        }

    }

    private void SelfDestruct()
    {
        if(destroyEvent != null)
        {
            destroyEvent.Invoke();
        }
        if (isOkay)
        {
            SoundManager.PlayInAvatar(SoundManager.SoundName.flashbombTeleport);
        }
        Tool_FXPooling.Instance.ForcedReturnToPool("Grenade", ps);
        Tool_FXPooling.Instance.SpawnFX("GrenadeDissipate", transform.position, Quaternion.identity, 1f);
        Destroy(this.gameObject);
    }

    public void Explode()
    {
        Collider2D[] explosion = Physics2D.OverlapCircleAll(this.transform.position, (Man_GameManager.main.player.upgrades.flashBomb) ? 4f : 2f, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? explosionMaskFront : explosionMaskBack);
        foreach (Collider2D e in explosion)
        {
            e.GetComponent<Enemy>().Stun();
        }
        if (Man_GameManager.main.player.upgrades.flashBomb)
        {
            Tool_FXPooling.Instance.SpawnFX("GrenadeExplode2m", this.transform.position, Quaternion.identity, 1f);
        }
        else
        {
            Tool_FXPooling.Instance.SpawnFX("GrenadeExplode1m", this.transform.position, Quaternion.identity, 1f);
        }
        SoundManager.PlayInAvatar(SoundManager.SoundName.flashbombDetonate);
        SelfDestruct();
    }


    public float CanTeleport()
    {
        Vector2 right = new Vector2(this.transform.position.x + playerBoxLenght, this.transform.position.y);
        Vector2 left = new Vector2(this.transform.position.x - playerBoxLenght, this.transform.position.y);
        
        RaycastHit2D hitTeleportDownRight = Physics2D.Raycast(right, Vector2.down, 0.25f, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
        RaycastHit2D hitTeleportDownLeft = Physics2D.Raycast(left, Vector2.down, 0.25f, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);


        if (hitTeleportDownLeft || hitTeleportDownRight)
        {
            RaycastHit2D playerRight = Physics2D.Raycast(this.transform.position, Vector2.right, playerBoxLenght, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);

            /*
            Vector3 temp = transform.position;
            temp.x += 0.5f;
            Vector3 temp2 = transform.position;

            Vector2 line = Vector2.zero;
            line.x = playerRight.point.x - 1;
            line.y = playerRight.point.y;
            Vector2 line2 = Vector2.zero;
            line2.x = playerRight.point.x + 1;
            line2.y = playerRight.point.y;

            Debug.DrawLine(line, line2, Color.red, 5f);

            line.x = playerRight.point.x;
            line.y = playerRight.point.y - 1;

            line2.x = playerRight.point.x;
            line2.y = playerRight.point.y + 1;

            Debug.DrawLine(line, line2, Color.red, 5f);
            Debug.Log(playerRight.point);
            */
            
            if (playerRight)
            {
                left.x -= playerRight.distance;
                RaycastHit2D playerLeft = Physics2D.Raycast(this.transform.position, Vector2.left, playerBoxLenght + playerRight.distance, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                if (playerLeft)
                {
                    isOkay = false;
                    return 0f;
                }
                else
                {
                    right.x -= playerRight.distance;
                    RaycastHit2D hitTeleportUpRight = Physics2D.Raycast(right, Vector2.up, playerBoxHeight, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                    RaycastHit2D hitTeleportUpLeft = Physics2D.Raycast(left, Vector2.up, playerBoxHeight, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                    if (!hitTeleportUpLeft && !hitTeleportUpRight)
                    {
                        isOkay = true;
                        SelfDestruct();
                        return -playerRight.distance;
                    }
                    else
                    {
                        isOkay = false;
                        return 0f;
                    }
                }
            }
            else
            {
                RaycastHit2D playerLeft = Physics2D.Raycast(this.transform.position, Vector2.left, playerBoxLenght, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                if (playerLeft)
                {
                    right.x += playerLeft.distance;
                    playerRight = Physics2D.Raycast(this.transform.position, Vector2.right, playerBoxLenght + playerLeft.distance, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                    if (playerRight)
                    {
                        isOkay = false;
                        return 0f;
                    }
                    else
                    {
                        left.x += playerLeft.distance;
                        RaycastHit2D hitTeleportUpRight = Physics2D.Raycast(right, Vector2.up, playerBoxHeight, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                        RaycastHit2D hitTeleportUpLeft = Physics2D.Raycast(left, Vector2.up, playerBoxHeight, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                        if (!hitTeleportUpLeft && !hitTeleportUpRight)
                        {
                            isOkay = true;
                            SelfDestruct();
                            return playerLeft.distance;
                        }
                        else
                        {
                            isOkay = false;
                            return 0f;
                        }
                    }
                }
                else
                {
                    RaycastHit2D hitTeleportUpRight = Physics2D.Raycast(right, Vector2.up, playerBoxHeight, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                    RaycastHit2D hitTeleportUpLeft = Physics2D.Raycast(left, Vector2.up, playerBoxHeight, (Man_GameManager.main.player.controller.CurrentLayerDepth == LayerDepth.front) ? layerFront : layerBack);
                    if (!hitTeleportUpLeft && !hitTeleportUpRight)
                    {
                        isOkay = true;
                        SelfDestruct();
                        return 0f;
                    }
                    else
                    {
                        isOkay = false;
                        return 0f;
                    }
                }

            }
        }
        isOkay = false;
        return 0f;
    }

}

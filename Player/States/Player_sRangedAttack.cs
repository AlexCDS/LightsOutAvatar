using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


/********************************************************************* 
 * By Alex
 * Ranged Attack state for the Player script. See Player_sState for more details
 * Last modification 06/05/2018
*********************************************************************/


public class Player_sRangedAttack : Player_sBase
{
    ParticleSystem lazerHit;

    //Constructor set
    int lazerDamage;
    float startDelay;

    float timer = 0f;

    bool countDown = true;
    bool startFiring = false;
    
    ParticleSystem lazerStart;
    LineRenderer line;


    public override void ToIdle()
    {
        base.ToIdle();
        Tool_FXPooling.Instance.ForcedReturnToPool("LazerHit", lazerHit);
        lazerHit = null;
        line.enabled = false;
        master.playerAnimationController.SetAtkLaser = false;
        Reset();
        Tool_FXPooling.Instance.ForcedReturnToPool("LazerStart", lazerStart);
        SoundManager.PlayInAvatar(SoundManager.SoundName.laserFireStop);
    }

    public override void ToDeath()
    {
        base.ToDeath();
        Tool_FXPooling.Instance.ForcedReturnToPool("LazerHit", lazerHit);
        lazerHit = null;
        line.enabled = false;
        master.playerAnimationController.SetAtkLaser = false;
        Reset();
        Tool_FXPooling.Instance.ForcedReturnToPool("LazerStart", lazerStart);
        SoundManager.PlayInAvatar(SoundManager.SoundName.laserFireStop);
    }

    public override void Update() {
        if (startFiring)
        {
            int collisionMask = (master.controller.CurrentLayerDepth == LayerDepth.front) ? Man_LevelManager.currentLevel.layerFront : Man_LevelManager.currentLevel.layerBack;
            int layerMask = master.EnemyLayer | collisionMask;

            timer += Time.deltaTime;

            if (line.enabled != true)
                line.enabled = true;

            Vector3 raycastOrigin;
            List<RaycastHit2D> hits;
            CheckForTargets(layerMask, out raycastOrigin, out hits);

            for (int i = 0; i < hits.Count; i++)
            {
                Enemy temp = hits[i].collider.GetComponent<Enemy>();
                Ing_SwitchTrigger tempSw = hits[i].collider.GetComponent<Ing_SwitchTrigger>();

                if (temp != null)
                {
                    if (tempSw == null || timer > 1f)
                        temp.Hit((float)lazerDamage * Time.deltaTime);

                    if ((!master.upgrades.lazerPierce || tempSw != null) && !temp.isDead)
                    {
                        line.SetPosition(1, new Vector3(hits[i].point.x, hits[i].point.y, master.tr.position.z));
                        SpawnHitFX(hits[i].point);
                        break;
                    }
                }

                else
                {
                    line.SetPosition(1, new Vector3(hits[i].point.x, hits[i].point.y, master.tr.position.z));
                    SpawnHitFX(hits[i].point);
                    break;
                }
            }

            if (hits.Count == 0)
            {
                line.SetPosition(1, raycastOrigin + ((Vector3.right * master.Direction) * 100f));
            }

            master.mana -= master.lazerManaCost * Time.deltaTime;
            Man_GameManager.Instance.hud.UpdateManaUI(master.mana);

            if (master.mana <= 0)
            {
                ToIdle();
                Tool_FXPooling.Instance.ForcedReturnToPool("LazerStart", lazerStart);
            }

            if (timer >= 1f)
            {
                timer = 0f;
            }
        }


        if (countDown)
        {
            
            timer = Time.time + startDelay;
            countDown = false;
        }

        if (Time.time >= timer && !startFiring)
        {
            Vector3 raycastOrigin = master.playerAnimationController.leftHand.transform.position;
            line.widthMultiplier = 1.8f;
            startFiring = true;
            lazerStart = Tool_FXPooling.Instance.SpawnFX("LazerStart", master.playerAnimationController.leftHand.transform.position, Quaternion.identity, master.playerAnimationController.leftHand);// ForcedReturnToPool("LazerStart", lazerForming);
        }
    }

    private void CheckForTargets(int layerMask, out Vector3 raycastOrigin, out List<RaycastHit2D> hits)
    {
        raycastOrigin = (master.Direction == 1) ? master.playerAnimationController.leftHand.transform.position : master.playerAnimationController.rightHand.transform.position;
        raycastOrigin.y = master.playerAnimationController.leftHand.transform.position.y;

        line.positionCount = 2;
        line.SetPosition(0, raycastOrigin);

        hits = new List<RaycastHit2D>(Physics2D.RaycastAll(raycastOrigin, Vector2.right * master.Direction, 100f, layerMask));
        hits = hits.OrderBy(n => n.distance).ToList();
    }

    private void SpawnHitFX(Vector3 n)
    {
        if (lazerHit != null)
        {
            Tool_FXPooling.Instance.ForcedReturnToPool("LazerHit", lazerHit);
            lazerHit = null;
        }

        n.z = master.tr.position.z;
        lazerHit = Tool_FXPooling.Instance.SpawnFX("LazerHit", n);
        lazerHit.transform.rotation = Quaternion.Euler(new Vector3(0, (master.Direction == 1) ? 0 : 180, 0));
    }

    public override void Move(ref Vector3 velocity)
    {

        Vector3 testVelocity = new Vector3(master.PlayerInput.x, master.PlayerInput.x);
        master.controller.HorizontalCollisions(ref testVelocity);
        master.controller.VerticalCollisions(ref testVelocity);

        if(!master.controller.collisions.left && !master.controller.collisions.right)
            master.playerAnimationController.SetHSpeed(Mathf.Abs(master.PlayerInput.x));
    }

    public override void TriggerUpL()
    {
        ToIdle();
    }

    void Reset()
    {
        timer = 0f;
        countDown = true;
        startFiring = false;
    }

    public Player_sRangedAttack(Player master, LineRenderer line, int lazerDamage, float startDelay) : base(master)
    {
        this.master = master;
        this.lazerDamage = lazerDamage;
        this.startDelay = startDelay;
        this.line = line;
    }

}

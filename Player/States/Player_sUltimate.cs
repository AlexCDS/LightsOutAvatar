using System.Collections.Generic;
using UnityEngine;

/********************************************************************* 
 * By Alex
 * Ultimate state of the Player script. See Player_sState for more details
*********************************************************************/

public class Player_sUltimate : Player_sBase
{
    float timePassed = 0;
    bool started = false;

    public override void ToJump()
    {
        base.ToJump();
        timePassed = 0;
        started = false;

        master.ResetVelocity();
        Man_GameManager.Instance.hud.DeactivateUltimateFeedback();
    }

    public override void Update() {
        timePassed += Time.deltaTime;
        if (timePassed > 1.2f * master.playerAnimationController.UltimateAnimationDuration)
        {
            ToJump();
        }

        Uti_GameEffect.ShakeDynamic(Man_GameManager.Instance.mainCamera.gameObject, 0.1f * timePassed);

        if (!started)
        {
            Tool_FXPooling.Instance.SpawnFX("UltimateAttack", master.playerAnimationController.spine1.transform.position);
        }

        if (timePassed > master.playerAnimationController.UltimateAnimationDuration)
        {
            HitNearEnnemies();
        }
    }

    private void HitNearEnnemies()
    {
        Collider2D[] hits = new Collider2D[10];
        BoxCollider2D temp = new GameObject().AddComponent<BoxCollider2D>();
        int a = 0;
        temp.size = new Vector2(40f, 20f);
        temp.transform.position = master.tr.position;

        a = temp.OverlapCollider(master.AttackFilter, hits);

        //TODO cleaner. This is a hotfix
        ContactFilter2D contactFilter = master.AttackFilter;
        contactFilter.layerMask = (master.controller.CurrentLayerDepth == LayerDepth.front) ? Man_EnemyManager.Instance.enemyOne : Man_EnemyManager.Instance.enemyTwo;
        master.AttackFilter = contactFilter;

        if (hits[0] != null)
        {
            List<Collider2D> targets = new List<Collider2D>(hits);
            targets.RemoveRange(a, 10 - a);

            foreach (Collider2D n in targets)
            {
                Enemy nme = n.GetComponent<Enemy>();
                Ing_SwitchTrigger sw = n.GetComponent<Ing_SwitchTrigger>();

                if (nme != null && sw == null)
                    n.GetComponent<Enemy>().Hit(1000);
            }
        }
    }

    public Player_sUltimate(Player master) : base(master)
    {
        this.master = master;
    }
}

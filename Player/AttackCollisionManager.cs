using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AttackCollisionManager {

    [HideInInspector] public Collider2D[] attackCollider;

    PolygonCollider2D[] polCols;

    public Collider2D groundUp;
    public Collider2D groundForward;
    public Collider2D airUp;
    public Collider2D airForward;
    public Collider2D airDown;
    public Collider2D smallAoE;
    public Collider2D mediumAoE;
    public Collider2D largeAoE;

    public void InitCollisionManager()
    {
        attackCollider = new Collider2D[]
       {
            groundUp,
            groundForward,
            airUp,
            airForward,
            airDown,
            smallAoE,
            mediumAoE,
            largeAoE
       };
    }

    public void RotateCollider()
    {
        if (polCols == null)
        {
            polCols = new PolygonCollider2D[5]
            {
                (PolygonCollider2D)groundUp,
                (PolygonCollider2D)groundForward,
                (PolygonCollider2D)airUp,
                (PolygonCollider2D)airForward,
                (PolygonCollider2D)airDown
            };
        }


        foreach (PolygonCollider2D polCol in polCols)
            for (int i = 0; i < polCol.points.Length; i++)
            {
                Vector2 temp = polCol.points[i];
                temp.x = -polCol.points[i].x;
                polCol.points[i] = temp;
            }
    }
}   